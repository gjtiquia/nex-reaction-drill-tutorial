#nullable enable

using Jazz;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

namespace Nex.Essentials
{
    public class PinchSignalProducer : MonoBehaviour
    {
        public UnityEvent<bool> onStateChange = new();

#if MDK_HAND_3_1_UP && MIN_PLAYOS_API_2

        [Header("Data Source")]
        public HandDetectionManager handDetectionManager = null!;

        [Tooltip("The player index to track (0 for the first player, 1 for the second, etc.)")]
        public int poseIndex;

        [Tooltip("The handedness to track (Left or Right)")]
        public Handedness handedness;

        [Header("Pinch Thresholds (Hysteresis)")]
        [SerializeField] private float pinchThreshold = 1f; // distance in inch to trigger pinch start

        [SerializeField]
        private float releaseThreshold = 1.5f; // distance to trigger pinch release (should be > pinchThreshold)

        [Header("Motion Rejection")]
        [Tooltip("Threshold for ignoring state transitions when the hand moves by this amount relative to palm size.")]
        [SerializeField]
        private float motionThreshold = 0.15f;

        [Tooltip("Time window (in seconds) for smoothing the wrist position to calculate motion.")] [SerializeField]
        private float motionSmoothWindow = 0.2f;

        [Header("Smoothing")]
        [Tooltip("Smoothing factor for position filtering (0 = no smoothing, 1 = instant)")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float smoothingFactor = 0.3f;

        // Z axis not as reliable as X/Y, so we weight it less
        private Vector3 axisWeights = new(1f, 1f, 0.8f);

        [Header("Debug")]
        [SerializeField] private TMP_Text? debugText;

        private bool isPinching;
        private Vector3 smoothedThumbTip;
        private Vector3 smoothedIndexTip;
        private bool isInitialized;

        // Motion tracking
        private TimeMovingAverageFilterVector3 wristPositionFilter;
        private Vector3 prevWristPosition;

        private void Awake()
        {
            handDetectionManager.captureHandPoseDetection += CaptureHandPoseDetection;
            wristPositionFilter = new TimeMovingAverageFilterVector3(motionSmoothWindow);
        }

        private void Start()
        {
            if (debugText != null)
            {
                debugText.text = "Pinch detector started";
            }
        }

        private void OnValidate()
        {
            // Ensure releaseThreshold is always greater than pinchThreshold
            if (releaseThreshold <= pinchThreshold)
            {
                releaseThreshold = pinchThreshold + 1f;
            }
        }

        private void CaptureHandPoseDetection(HandPoseDetection detection)
        {
            // Evaluate if pinching based on thumb and index fingertips distance
            var pose = detection.GetPlayerHandByIndexAndHandedness(poseIndex, handedness);

            if (pose == null)
            {
                return;
            }

            var rawThumbTip = pose.metricScaleNodes[4].ToVector3();
            var rawIndexTip = pose.metricScaleNodes[8].ToVector3();

            // Get the plane of palm by wrist, index palm and pinky palm
            var wristPos = pose.metricScaleNodes[0].ToVector3();
            var indexPalmPos = pose.metricScaleNodes[5].ToVector3();
            var pinkyPalmPos = pose.metricScaleNodes[17].ToVector3();

            // Calculate palm size for motion normalization
            var palmSize = Vector3.Distance(wristPos, indexPalmPos);

            // Track motion using smoothed wrist position
            var smoothedWristPos = wristPositionFilter.Update(wristPos, Time.unscaledTime);
            var motion = prevWristPosition == Vector3.zero
                ? 0f
                : (smoothedWristPos - prevWristPosition).magnitude / palmSize;
            prevWristPosition = smoothedWristPos;

            // Get the normal of the palm plane
            var palmNormal = Vector3.Cross(indexPalmPos - wristPos, pinkyPalmPos - wristPos).normalized;

            // The weight of Z axis is adjusted based on the angle between palm normal(horizontal only) and camera forward
            // When palm is perpendicular to camera (90°), Z depth is less reliable
            // When palm faces camera (0°) or faces away (180°), Z depth is more reliable
            // By measuring distance from 90°, this logic works for both left and right hands
            var angleToCamera = 90f;
            if (Camera.main != null)
            {
                angleToCamera = Vector3.Angle(palmNormal, Camera.main.transform.forward);
            }

            // Calculate how far the angle is from 90° (0 = perpendicular, 90 = facing/away)
            var distanceFrom90 = Mathf.Abs(angleToCamera - 90f);
            var zWeight = distanceFrom90 / 90f; // Normalize to 0-1
            axisWeights.z = 0.2f + 0.6f * zWeight; // Vary between 0.2 to 0.8

            // Reject if the hand is moving too fast
            if (motion > motionThreshold)
            {
                if (debugText != null)
                {
                    debugText.text = "Rejecting due to motion";
                }

                return;
            }

            // Apply exponential smoothing to reduce jitter
            if (!isInitialized)
            {
                smoothedThumbTip = rawThumbTip;
                smoothedIndexTip = rawIndexTip;
                isInitialized = true;
            }
            else
            {
                smoothedThumbTip = Vector3.Lerp(smoothedThumbTip, rawThumbTip, smoothingFactor);
                smoothedIndexTip = Vector3.Lerp(smoothedIndexTip, rawIndexTip, smoothingFactor);
            }

            var distance = CalculateWeightedDistance(smoothedThumbTip, smoothedIndexTip);

            // Apply hysteresis: use different thresholds for pinch and release
            var wasPinching = isPinching;

            if (isPinching)
            {
                // Currently pinching - only release if distance exceeds releaseThreshold
                if (distance > releaseThreshold)
                {
                    isPinching = false;
                }
            }
            else
            {
                // Not pinching - only start pinch if distance is below pinchThreshold
                if (distance < pinchThreshold)
                {
                    isPinching = true;
                }
            }

            // Invoke event when state changes
            if (wasPinching != isPinching)
            {
                onStateChange.Invoke(isPinching);
            }

            if (debugText != null)
            {
                debugText.color = isPinching ? Color.green : Color.red;
                debugText.text =
                    $"isPinching: {isPinching}\nDistance: {distance:F3} in\nThresholds: {pinchThreshold:F1}/{releaseThreshold:F1}\nMotion: {motion:F2}\nPalm Angle: {angleToCamera:F1}\nZ Weight: {zWeight:F3}";
            }
        }

        /// <summary>
        /// Calculate weighted distance between two points, allowing different axes to contribute differently.
        /// Convert to inches for easier thresholding and alignment with other measurements.
        /// Useful when depth (Z) accuracy is lower than X/Y accuracy.
        /// </summary>
        private const float CmToInch = 0.393701f;

        private float CalculateWeightedDistance(Vector3 a, Vector3 b)
        {
            return Vector3.Scale(a - b, axisWeights).magnitude * CmToInch;
        }

        private void OnDestroy()
        {
            handDetectionManager.captureHandPoseDetection -= CaptureHandPoseDetection;
        }
#else
        void Start()
        {
            Debug.LogError("Failed to initialize PinchSignalProducer. Nex MDK Hand module is below 3.1 or minimum PlayOS API level is below 2.");
        }
#endif
    }
}