#nullable enable

using Jazz;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Nex.Essentials
{
    public class OpenClosedSignalProducer : MonoBehaviour
    {
        public UnityEvent<bool> onStateChange = new();

        public enum DetectionMethod
        {
            HandPose,
            GestureDetection,
        }

        public DetectionMethod detectionMethod = DetectionMethod.HandPose;

#if MDK_HAND_ENABLED && MIN_PLAYOS_API_2
        // Common definitions

        [System.Serializable]
        public struct HandState
        {
            public FingerState thumb;
            public FingerState indexFinger;
            public FingerState middleFinger;
            public FingerState ringFinger;
            public FingerState pinky;

            public readonly bool SatisfiesTarget(HandState target)
            {
                return (target.thumb == FingerState.None || thumb == target.thumb)
                       && (target.indexFinger == FingerState.None || indexFinger == target.indexFinger)
                       && (target.middleFinger == FingerState.None || middleFinger == target.middleFinger)
                       && (target.ringFinger == FingerState.None || ringFinger == target.ringFinger)
                       && (target.pinky == FingerState.None || pinky == target.pinky);
            }
        }

        public enum GestureState
        {
            None,
            Closed,
            Open
        }

        [Tooltip("The HandDetectionManager instance to use for hand tracking (Hand Pose)")]
        public HandDetectionManager handDetectionManager = null!;

        [Tooltip("The player index to track (0 for the first player, 1 for the second, etc.)")]
        public int poseIndex;

        [Tooltip("The handedness to track (Left or Right)")]
        public Handedness handedness;

        [ReadOnly] public GestureState currentGestureState;

        [ReadOnly] public HandState currentHandState;

        [SerializeField] private HandState closedHandState = new()
        {
            thumb = FingerState.None,
            indexFinger = FingerState.None,
            middleFinger = FingerState.Closed,
            ringFinger = FingerState.Closed,
            pinky = FingerState.None
        };

        [SerializeField] private HandState openHandState = new()
        {
            thumb = FingerState.Open,
            indexFinger = FingerState.None,
            middleFinger = FingerState.Open,
            ringFinger = FingerState.Open,
            pinky = FingerState.None
        };

        // Hand pose detection settings

        [Tooltip(
            "The minimum duration (in seconds) the hand must remain in the closed state to confirm a closed gesture (Hand Pose)")]
        public double closedHandStateDebounce = 0.05;

        [Tooltip(
            "The minimum duration (in seconds) the hand must remain in the open state to confirm an open gesture (Hand Pose)")]
        public double openHandStateDebounce = 0.1;

        [SerializeField] private float thumbOpenThresholdAngle = 40f;
        [SerializeField] private float thumbClosedThresholdAngle = 60f;
        [SerializeField] private float fingerOpenThresholdAngle = 50f;
        [SerializeField] private float fingerClosedThresholdAngle = 70f;

#if MDK_HAND_3_1_UP
        // Hand gesture detection settings
        [Tooltip("The HandGestureDetectionManager instance to use for hand gesture detection (Gesture Detection)")]
        public HandGestureDetectionManager handGestureDetectionManager = null!;

        [Header("Hysteresis Thresholds")]
        [Tooltip("Threshold to transition from Open to Closed state. Higher value filters open-hand noise spikes.")]
        public float openToClosedThreshold = 0.3f;

        [Tooltip("Threshold to transition from Closed to Open state. Lower value filters closed-hand noise dips.")]
        public float closedToOpenThreshold = 0.2f;

        [Header("Score Smoothing")]
        [Tooltip("Time window (in seconds) for smoothing the probability score. Filters short noise spikes.")]
        public float probSmoothWindow = 0.1f;

        [Header("Time-based Debounce")]
        [Tooltip("Minimum time (in seconds) the signal must stay above threshold to transition to Closed.")]
        public float openToClosedDebounce = 0.05f;

        [Tooltip("Minimum time (in seconds) the signal must stay below threshold to transition to Open.")]
        public float closedToOpenDebounce = 0.03f;

        [Header("Other Settings")]
        [Tooltip("Threshold for classifying if hand is present. (Gesture Detection)")]
        public float handPresenceThreshold = 0.9f;

        [Tooltip(
            "Threshold for ignoring state transitions when the hand moves by this amount relative to the hand box. (Gesture Detection)")]
        public float motionThreshold = 0.4f;

        [Tooltip(
            "Time in seconds to wait before resetting the hand state when the hand is not detected (Gesture Detection)")]
        public float handMissingTimeout = 1f;

        [SerializeField] private TMP_Text? handGestureDebugText;
#endif

        // Private states

        double currentStreakStartTime;
        GestureState currentStreakGestureState = GestureState.None;

        TimeMovingAverageFilterRect handBoxFilter = new(0.2f);
        float lastHandVisibleTime;

        // Gesture detection state
        private TimeMovingAverageFilterFloat probFilter;
        private float gestureDebounceStartTime;
        private bool pendingClosedSignal;

        void OnValidate()
        {
            if (detectionMethod == DetectionMethod.HandPose && handDetectionManager == null)
            {
                Debug.LogError("HandDetectionManager is not assigned. Please assign it in the inspector.");
            }

#if MDK_HAND_3_1_UP
            else if (detectionMethod is DetectionMethod.GestureDetection && handGestureDetectionManager == null)
            {
                Debug.LogError("HandGestureDetectionManager is not assigned. Please assign it in the inspector.");
            }
#endif
        }

        // Start is called before the first frame update
        void Start()
        {
            currentGestureState = GestureState.None;
            if (handDetectionManager)
            {
                handDetectionManager.captureHandPoseDetection += CaptureHandPoseDetection;
            }

#if MDK_HAND_3_1_UP
            if (handGestureDetectionManager)
            {
                handGestureDetectionManager.captureHandGestureDetection += CaptureHandGestureDetection;
            }

            probFilter = new TimeMovingAverageFilterFloat(probSmoothWindow);
#endif
        }

        void OnDestroy()
        {
            if (handDetectionManager)
            {
                handDetectionManager.captureHandPoseDetection -= CaptureHandPoseDetection;
            }

#if MDK_HAND_3_1_UP
            if (handGestureDetectionManager)
            {
                handGestureDetectionManager.captureHandGestureDetection -= CaptureHandGestureDetection;
            }
#endif
        }

        private void CaptureHandPoseDetection(HandPoseDetection detection)
        {
            if (detectionMethod != DetectionMethod.HandPose)
            {
                return;
            }

            var handPose = detection.GetPlayerHandByIndexAndHandedness(poseIndex, handedness);

            if (handPose == null)
            {
                return;
            }

            currentHandState = new HandState
            {
                thumb = EvaluateThumbState(handPose),
                indexFinger = EvaluateIndexFingerState(handPose),
                middleFinger = EvaluateMiddleFingerState(handPose),
                ringFinger = EvaluateRingFingerState(handPose),
                pinky = EvaluatePinkyFingerState(handPose)
            };

            // Evaluate if the hand state matches the closed or open conditions
            var detectedGestureState = currentHandState.SatisfiesTarget(closedHandState) ? GestureState.Closed :
                currentHandState.SatisfiesTarget(openHandState) ? GestureState.Open :
                GestureState.None;

            var now = BasicUtils.NowMs();

            var gestureChanged = detectedGestureState != currentGestureState;

            if (!gestureChanged) return;
            // Reset streak if any gesture state changed
            if (currentStreakGestureState != detectedGestureState || detectedGestureState == GestureState.None)
            {
                currentStreakGestureState = detectedGestureState;
                currentStreakStartTime = now;
            }

            if (detectedGestureState == GestureState.None)
            {
                return; // No gesture detected, do nothing
            }

            if ((now - currentStreakStartTime) / 1000 >= (detectedGestureState == GestureState.Closed
                    ? closedHandStateDebounce
                    : openHandStateDebounce)) // Streak duration passed threshold, confirm a gesture change
            {
                SetCurrentGestureState(detectedGestureState);
            }
        }

#if MDK_HAND_3_1_UP
        private void CaptureHandGestureDetection(HandGestureDetection detection)
        {
            if (detectionMethod is not DetectionMethod.GestureDetection)
            {
                if (handGestureDebugText != null)
                {
                    handGestureDebugText.text = "";
                }

                return;
            }

            HandGesture? gesture = null;
            detection.gestures.ForEach(g =>
            {
                if (g.playerIndex == poseIndex && g.handedness == handedness)
                {
                    gesture = g;
                }
            });

            if (gesture == null)
            {
                HandleHandMissing();
                return;
            }

            if (gesture.handProb < handPresenceThreshold)
            {
                HandleHandMissing();
                return;
            }

            // Hand is treated as visible even if it may be rejected
            lastHandVisibleTime = Time.unscaledTime;

            var curHandBoxInt = gesture.handBox;
            var curHandBox = new Rect(curHandBoxInt.x, curHandBoxInt.y, curHandBoxInt.width, curHandBoxInt.height);
            var prevHandBox = handBoxFilter.CurrentValue;
            handBoxFilter.Update(curHandBox);

            var curCenter = curHandBox.center;
            var prevCenter = prevHandBox.center;
            var handBoxHeight = prevHandBox.height;
            var motion = prevHandBox == Rect.zero ? 1 : (curCenter - prevCenter).magnitude / handBoxHeight;

            // Reject if the hand is moving too fast.
            if (motion > motionThreshold)
            {
                if (handGestureDebugText != null)
                {
                    handGestureDebugText.text = "Rejecting due to motion";
                }

                return;
            }

            // Smooth the raw probability score
            var rawProb = gesture.prob;
            var smoothedProb = probFilter.Update(rawProb, Time.unscaledTime);

            // Apply hysteresis thresholds based on current state
            bool isClosedSignal;
            if (currentGestureState == GestureState.Closed)
            {
                // Currently closed - need to drop below closedToOpenThreshold to open
                isClosedSignal = smoothedProb >= closedToOpenThreshold;
            }
            else
            {
                // Currently open/none - need to exceed openToClosedThreshold to close
                isClosedSignal = smoothedProb >= openToClosedThreshold;
            }

            if (handGestureDebugText != null)
            {
                handGestureDebugText.text = $"Hand Prob: {gesture.handProb:F2}\n" +
                                            $"Raw Prob: {rawProb:F2}\n" +
                                            $"Smoothed: {smoothedProb:F2}\n" +
                                            $"Signal: {(isClosedSignal ? "Closed" : "Open")}\n" +
                                            $"Motion: {motion:F2}";
            }

            // Time-based debounce for state transitions
            var now = Time.unscaledTime;

            if (isClosedSignal != pendingClosedSignal)
            {
                // Signal changed, reset debounce timer
                pendingClosedSignal = isClosedSignal;
                gestureDebounceStartTime = now;
            }

            var requiredDebounce = isClosedSignal ? openToClosedDebounce : closedToOpenDebounce;
            var debounceElapsed = now - gestureDebounceStartTime;

            if (debounceElapsed >= requiredDebounce)
            {
                // Debounce passed, apply state change
                if (isClosedSignal && currentGestureState != GestureState.Closed)
                {
                    SetCurrentGestureState(GestureState.Closed);
                }
                else if (!isClosedSignal && currentGestureState != GestureState.Open)
                {
                    SetCurrentGestureState(GestureState.Open);
                }
            }
        }

        private void HandleHandMissing()
        {
            var time = Time.unscaledTime;
            if (time - lastHandVisibleTime > handMissingTimeout && currentGestureState != GestureState.None)
            {
                if (handGestureDebugText != null)
                {
                    handGestureDebugText.text = $"Hand lost {handedness}";
                }

                SetCurrentGestureState(GestureState.None);
                probFilter.Reset();
            }
        }
#endif

        private void SetCurrentGestureState(GestureState detectedGestureState)
        {
            if (currentGestureState == detectedGestureState) return;

            currentGestureState = detectedGestureState;
            onStateChange.Invoke(currentGestureState == GestureState.Closed);

#if MDK_HAND_3_1_UP
            // Update debug text color based on gesture state
            if (handGestureDebugText != null)
            {
                handGestureDebugText.color = detectedGestureState switch
                {
                    GestureState.Closed => Color.green,
                    GestureState.Open => Color.red,
                    _ => Color.white
                };
            }
#endif
        }

        private FingerState EvaluateThumbState(HandPose handPose)
        {
            var thumbAngle = handPose.fingerAngles.thumbAngle;
            return thumbAngle > thumbClosedThresholdAngle ? FingerState.Closed :
                thumbAngle < thumbOpenThresholdAngle ? FingerState.Open : FingerState.None;
        }

        private FingerState EvaluateIndexFingerState(HandPose handPose)
        {
            return EvaluateFingerStateByAngle(handPose.fingerAngles.indexFingerAngle);
        }

        private FingerState EvaluateMiddleFingerState(HandPose handPose)
        {
            return EvaluateFingerStateByAngle(handPose.fingerAngles.middleFingerAngle);
        }

        private FingerState EvaluateRingFingerState(HandPose handPose)
        {
            return EvaluateFingerStateByAngle(handPose.fingerAngles.ringFingerAngle);
        }

        private FingerState EvaluatePinkyFingerState(HandPose handPose)
        {
            return EvaluateFingerStateByAngle(handPose.fingerAngles.pinkyAngle);
        }

        private FingerState EvaluateFingerStateByAngle(double angle)
        {
            return angle > fingerClosedThresholdAngle ? FingerState.Closed :
                angle < fingerOpenThresholdAngle ? FingerState.Open : FingerState.None;
        }

#else
        void Start()
        {
            Debug.LogError("Failed to initialize OpenClosedSignalProducer. Nex MDK Hand module is not enabled or minimum PlayOS API level is below 2.");
        }
#endif
    }
}
