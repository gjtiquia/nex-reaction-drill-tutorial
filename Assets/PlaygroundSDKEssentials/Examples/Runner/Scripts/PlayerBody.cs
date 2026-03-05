#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using PoseFlavor = Nex.Essentials.BodyPoseController.PoseFlavor;
using SignalPolarity = Nex.Essentials.SignalPolarityDetector.SignalPolarity;

namespace Nex.Essentials.Examples.Runner
{
    public class PlayerBody : MonoBehaviour
    {
        [SerializeField] private MdkController mdkController = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int playerIndex;

        [Serializable]
        public enum HorizontalMovementMode
        {
            Lean,
            Absolute
        }

        [Serializable]
        public class LeanMovementConfig
        {
            public float scale = 50;
            public float deadZone = 6f;
            public float smoothTime = 0.15f;
            public float maxVelocity = 10f;
            public bool squareMode = true;
            public bool lockXDuringJump = true;
        }

        [Serializable]
        public class AbsoluteMovementConfig
        {
            public float scale = 0.5f;
            public float smoothTime = 0.15f;
            public bool lockXDuringJump;
            public bool snapToLanes;
            public List<float> lanes = new();
            public float laneBuffer = 0.4f;

            public bool clampAbsoluteMovement;
            public float clampMin = -7;
            public float clampMax = 7;
        }

        [Serializable]
        public class JumpAnimationConfig
        {
            public float delay = 0.6f;
            public float transitionDuration = 0.3f;
        }

        [Serializable]
        public class CrouchAnimationConfig
        {
            public float transitionDuration = 0.25f;
            public float minTime = 1;
            public float maxTime = 2;
        }

        [Serializable]
        public class ShieldConfig
        {
            public TimeDebouncedBoolean.DebounceConfig debounceConfig;
            public Renderer rend = null!;
            public Color baseColor;
            public Color flashColor;
            public float shieldFlashSpeed = 1f;
        }

        [Header("Horizontal Movement")]
        [SerializeField] private HorizontalMovementMode xMode = HorizontalMovementMode.Absolute;

        [SerializeField] private AbsoluteMovementConfig absoluteMovementConfig = null!;
        [SerializeField] private LeanMovementConfig leanMovementConfig = null!;
        private float xVelocity;
        private float lastDeltaX;
        private int currentLane = 1;

        private Rigidbody rb = null!;

        [Header("Jumping")]
        [SerializeField] private float jumpYPosition = 5f;

        [SerializeField] private JumpAnimationConfig jumpConfig = null!;

        [Header("Crouching")]
        [SerializeField] private float crouchYPosition = -0.5f;

        [SerializeField] private CrouchAnimationConfig crouchConfig = null!;

        [Header("Vertical Movement")]
        [SerializeField] private SignalPolarityDetector verticalSignalDetector = null!;

        [SerializeField] private float neutralYPosition = 1f;
        [SerializeField] private bool useKeyboardDebug;

        private SignalPolarity currentPolarity = SignalPolarity.Neutral;
        private float lastVerticalMove;
        private float lastCrouchTime;

        [Header("Shield")]
        [SerializeField] private ShieldConfig shieldConfig = null!;

        public bool IsShielded => shieldDebounced.Value;
        private TimeDebouncedBoolean shieldDebounced = null!;
        private float shieldStartTime;

        [Header("Slash Detection")]
        [SerializeField] private TwoHandsSliceDetector twoHandsSliceDetector = null!;

        [SerializeField] private ParticleSystem slashEffect = null!;

        [SerializeField] private bool isShadow;
        [SerializeField] private Color shadowColor;

        public bool IsLeaningEnabled { get; set; } = false;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            shieldDebounced = new TimeDebouncedBoolean(shieldConfig.debounceConfig);

            if (!isShadow)
            {
                RunShieldDetection(this.GetCancellationTokenOnDestroy()).Forget();
                twoHandsSliceDetector.OnTwoHandSliceDetected += HandleSlashDetected;
            }

            UpdateIsShadowColor();
        }

        private void FixedUpdate()
        {
            var newX = AdjustXPosition();
            var newY = isShadow ? neutralYPosition : AdjustYPosition();

            rb.MovePosition(new Vector3(newX, newY, rb.position.z));
        }

        private void Update()
        {
            JumpDetection();
        }

        private bool IsJumping()
        {
            return currentPolarity == SignalPolarity.Positive && Time.time <= lastVerticalMove + jumpConfig.delay;
        }

        private bool IsCrouching()
        {
            return currentPolarity == SignalPolarity.Negative && Time.time <= lastVerticalMove + crouchConfig.maxTime &&
                   Time.time <= lastCrouchTime + crouchConfig.minTime;
        }

        private void JumpDetection()
        {
            // Cannot change direction during jumping
            if (IsJumping()) return;

            var latestPolarity = useKeyboardDebug ? GetKeyboardJump() : verticalSignalDetector.Signal;
            if (IsCrouching())
            {
                if (latestPolarity == SignalPolarity.Negative)
                {
                    lastCrouchTime = Time.time;
                }

                return;
            }

            // Not jumping or crouching, update the state
            currentPolarity = latestPolarity;
            switch (latestPolarity)
            {
                case SignalPolarity.Positive:
                    lastVerticalMove = Time.time;
                    break;
                case SignalPolarity.Negative:
                    lastVerticalMove = Time.time;
                    lastCrouchTime = Time.time;
                    break;
            }
        }

        private SignalPolarity GetKeyboardJump()
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                return SignalPolarity.Positive;
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                return SignalPolarity.Negative;
            }

            return SignalPolarity.Neutral;
        }

        private float AdjustXPosition()
        {
            if (playerIndex >= mdkController.positions.Length) return rb.position.x;

            var newX = xMode switch
            {
                HorizontalMovementMode.Absolute => GetXPositionFromAbsolutePosition(),
                HorizontalMovementMode.Lean => GetXPositionFromLeanPosition(),
                _ => throw new ArgumentOutOfRangeException()
            };

            return newX ?? rb.position.x;
        }

        private float? GetXPositionFromLeanPosition()
        {
            if (leanMovementConfig.lockXDuringJump && (IsJumping() || IsCrouching()) ||
                !bodyPoseController.TryGetBodyPose(playerIndex, PoseFlavor.SmoothHomographical, out var pose) ||
                !pose.TryGetNode(SimplePose.NodeIndex.Nose, out var nosePos) ||
                !pose.TryGetNode(SimplePose.NodeIndex.Chest, out var chestPos))
            {
                return null;
            }

            // Get the difference in x position
            var xDiff = (nosePos.x - chestPos.x) / pose.pixelsPerInch;

            // Don't move if within the dead zone
            if (Mathf.Abs(xDiff) < leanMovementConfig.deadZone)
            {
                return null;
            }

            // Remove the dead zone magnitude, so that it won't jump when it leaves the dead zone
            xDiff -= xDiff > 0 ? leanMovementConfig.deadZone : -leanMovementConfig.deadZone;

            float targetXVelocity;
            if (leanMovementConfig.squareMode)
            {
                var direction = xDiff > 0 ? 1 : -1;
                targetXVelocity = direction * xDiff * xDiff * leanMovementConfig.scale * Time.fixedDeltaTime;
            }
            else
            {
                targetXVelocity = xDiff * leanMovementConfig.scale * Time.fixedDeltaTime;
            }

            // Use the target velocity to compute the new x position
            var targetDeltaX = targetXVelocity * Time.fixedDeltaTime;
            var newDeltaX = Mathf.SmoothDamp(lastDeltaX, targetDeltaX, ref xVelocity, leanMovementConfig.smoothTime);
            lastDeltaX = newDeltaX;
            var clampedDeltaX = Mathf.Clamp(newDeltaX, -leanMovementConfig.maxVelocity * Time.fixedDeltaTime,
                leanMovementConfig.maxVelocity * Time.fixedDeltaTime);

            return rb.position.x + clampedDeltaX;
        }

        private float? GetXPositionFromAbsolutePosition()
        {
            if (absoluteMovementConfig.lockXDuringJump && (IsJumping() || IsCrouching())) return null;
            if (!bodyPoseController.TryGetBodyPose(playerIndex, PoseFlavor.Smoothed, out var pose)) return null;
            var nodeIndex = IsLeaningEnabled ? SimplePose.NodeIndex.Chest : SimplePose.NodeIndex.CenterHip;
            if (!pose.TryGetNode(nodeIndex, out var chestPos)) return null;

            // Get the target player position
            var centerPos = mdkController.positions[playerIndex];
            var centerPosX = Mathf.Lerp(0, Constants.rawFrameAspectRatio, centerPos.x);

            // Get the difference between the target position and the chest node
            var horizontalDiff = (chestPos.x - centerPosX) / pose.pixelsPerInch;
            var targetX = horizontalDiff * absoluteMovementConfig.scale;

            if (absoluteMovementConfig.clampAbsoluteMovement)
            {
                targetX = Mathf.Clamp(targetX, absoluteMovementConfig.clampMin, absoluteMovementConfig.clampMax);
            }

            if (absoluteMovementConfig.snapToLanes)
            {
                var lanes = absoluteMovementConfig.lanes;
                for (var i = 0; i < lanes.Count; i++)
                {
                    if (i == lanes.Count - 1)
                    {
                        targetX = lanes[i];
                        currentLane = i;
                        break;
                    }

                    // Find the threshold between lane i and lane i+1
                    var mid = (lanes[i] + lanes[i + 1]) / 2;
                    if (i == currentLane - 1)
                    {
                        mid -= absoluteMovementConfig.laneBuffer;
                    }
                    else if (i == currentLane)
                    {
                        mid += absoluteMovementConfig.laneBuffer;
                    }

                    if (targetX < mid)
                    {
                        targetX = lanes[i];
                        currentLane = i;
                        break;
                    }
                }
            }

            // Smooth the transition of the x position
            var currentX = rb.position.x;
            var newX = Mathf.SmoothDamp(currentX, targetX, ref xVelocity, absoluteMovementConfig.smoothTime);
            return newX;
        }

        // Running animation in code
        private float AdjustYPosition()
        {
            var newY = neutralYPosition;

            var timeSinceLastMove = Time.time - lastVerticalMove;
            if (IsJumping())
            {
                if (timeSinceLastMove < jumpConfig.transitionDuration)
                {
                    // Jumping up, squared path for more natural feel
                    var t = Mathf.InverseLerp(0, jumpConfig.transitionDuration, timeSinceLastMove);
                    var pos = 2 * t - t * t;
                    newY = Mathf.Lerp(neutralYPosition, jumpYPosition, pos);
                }
                else if (timeSinceLastMove > jumpConfig.delay - jumpConfig.transitionDuration)
                {
                    // Falling down
                    var a = Mathf.InverseLerp(jumpConfig.delay - jumpConfig.transitionDuration, jumpConfig.delay,
                        timeSinceLastMove);
                    var t = 1 + a;
                    var pos = 2 * t - t * t;
                    newY = Mathf.Lerp(neutralYPosition, jumpYPosition, pos);
                }
                else
                {
                    newY = jumpYPosition;
                }
            }

            if (IsCrouching())
            {
                var crouchEnd = Mathf.Min(lastVerticalMove + crouchConfig.maxTime,
                    lastCrouchTime + crouchConfig.minTime);
                if (timeSinceLastMove < crouchConfig.transitionDuration)
                {
                    // Crouching down
                    var t = Mathf.InverseLerp(0, crouchConfig.transitionDuration, timeSinceLastMove);
                    newY = Mathf.Lerp(neutralYPosition, crouchYPosition, t);
                }
                else if (Time.time > crouchEnd - crouchConfig.transitionDuration)
                {
                    // Standing back up
                    var t = Mathf.InverseLerp(crouchEnd - crouchConfig.transitionDuration, crouchEnd, Time.time);
                    newY = Mathf.Lerp(crouchYPosition, neutralYPosition, t);
                }
                else
                {
                    newY = crouchYPosition;
                }
            }

            return newY;
        }

        private async UniTaskVoid RunShieldDetection(CancellationToken cancellationToken)
        {
            await foreach (var _ in UniTaskAsyncEnumerable.EveryUpdate())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!bodyPoseController.TryGetBodyPose(playerIndex, PoseFlavor.Smoothed, out var pose))
                {
                    shieldDebounced.Update(false);
                }
                else
                {
                    var handRaised = GestureUtils.AreBothHandsRaised(pose, shieldDebounced.Value);
                    if (!shieldDebounced.Update(handRaised) && shieldDebounced.Value)
                    {
                        shieldStartTime = Time.time;
                    }
                }

                if (shieldDebounced.Value)
                {
                    // Oscillates between 0 and 1
                    var t = (Mathf.Sin((Time.time - shieldStartTime) * shieldConfig.shieldFlashSpeed) + 1f) / 2f;
                    shieldConfig.rend.material.color = Color.Lerp(shieldConfig.baseColor, shieldConfig.flashColor, t);
                }
                else
                {
                    shieldConfig.rend.material.color = shieldConfig.baseColor;
                }
            }
        }

        private void HandleSlashDetected()
        {
            slashEffect.Play();
        }

        public bool SetXMode(HorizontalMovementMode mode, bool snapToLanes = false)
        {
            var oldMode = xMode;
            xMode = mode;

            if (mode == HorizontalMovementMode.Absolute)
            {
                absoluteMovementConfig.snapToLanes = snapToLanes;
            }

            return xMode != oldMode;
        }

        public void SetAbsoluteXScale(float scale)
        {
            absoluteMovementConfig.scale = scale;
        }

        private void UpdateIsShadowColor()
        {
            shieldConfig.rend.material.color = isShadow ? shadowColor : shieldConfig.baseColor;
        }
    }
}
