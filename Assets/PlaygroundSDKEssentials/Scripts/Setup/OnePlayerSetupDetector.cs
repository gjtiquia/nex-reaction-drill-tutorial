#nullable enable

using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    public class OnePlayerSetupDetector : MonoBehaviour
    {
        [SerializeField, ReadOnly, Tooltip("Initialize in runtime")]
        private int playerIndex;

        [SerializeField, ReadOnly, Tooltip("Initialize in runtime")]
        private PlayAreaController playAreaController = null!;

        [SerializeField, ReadOnly, Tooltip("Initialize in runtime")]
        private BodyPoseController bodyPoseController = null!;

        [SerializeField, ReadOnly, Tooltip("Initialize in runtime")]
        private PlayAreaPreviewFrameProvider playAreaPreviewFrameProvider = null!;

        public enum SetupError
        {
            None,
            PoseNotFound,
            PoseNotCentered,
            PoseTooClose,
            PoseTooFar,
            HandNotRaised,
        }

        [SerializeField] private float setupThresholdInSeconds = 1f;
        [SerializeField] private float fillProgressSpeed = 1.2f;
        [SerializeField] private float dropProgressSpeed = 1f;
        [SerializeField] private float minPpi = 0.007f;
        [SerializeField] private float maxPpi = 0.016f;
        [SerializeField] private RectTransform progressBar = null!;
        private float progress;

        [SerializeField, Tooltip("How wide the chest detection can deviate from the player position")]
        public float chestDetectionMarginInInches = 6;

        [SerializeField] private TimeDebouncedBoolean.DebounceConfig debounceConfig = new(0.2f, 0.1f);

        private TimeDebouncedBoolean isHandRaisedDebounced = null!;
        public bool IsReady { get; private set; }

        [HideInInspector] public SimplePose.NodeIndex handIndex;
        [HideInInspector] public SetupError error;

        [SerializeField] private PlayAreaMaskedPreviewFrameProvider playAreaMaskedPreviewFrameProvider = null!;
        [SerializeField] private bool skipSetup;

        private Rect latestPlayArea;

        public void Start()
        {
            isHandRaisedDebounced = new TimeDebouncedBoolean(debounceConfig);
        }

        /// <summary>
        /// Initialize the setup controller for detection and where the object RectTransform should be
        /// </summary>
        public void Initialize(PlayAreaController aPlayAreaController, BodyPoseController aBodyPoseController,
            int aPlayerIndex, Vector2 position, PlayAreaPreviewFrameProvider aPreviewFrameProvider)
        {
            playAreaController = aPlayAreaController;
            bodyPoseController = aBodyPoseController;
            playerIndex = aPlayerIndex;
            playAreaPreviewFrameProvider = aPreviewFrameProvider;

            var playerPosition = aPlayAreaController.PlayerPositions[playerIndex];
            playAreaMaskedPreviewFrameProvider.Initialize(playAreaPreviewFrameProvider, playerPosition);

            var rectTransform = transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = position;
            }

            playAreaController.GetPlayAreaStream()
                .Subscribe(HandlePlayAreaUpdate, this.GetCancellationTokenOnDestroy());
        }

        private void Update()
        {
            // If already completed setup, keep it as completed
            if (IsReady) return;

            if (!bodyPoseController) return;

            if (!bodyPoseController.TryGetBodyPose(playerIndex, BodyPoseController.PoseFlavor.Smoothed, out var pose))
            {
                error = SetupError.PoseNotFound;
                return;
            }

            var poseOkay = DetectPose(pose);
            var isHandRaised = poseOkay && DetectRaiseHand(pose);
            isHandRaisedDebounced.Update(isHandRaised);

            // If the pose is not ok, it means there is already an error, no need to override it with the HandNotRaised error
            if (!isHandRaisedDebounced.Value && poseOkay)
            {
                error = SetupError.HandNotRaised;
            }

            // Fill up if all ready, else drop.
            if (isHandRaisedDebounced.Value || skipSetup)
            {
                progress += Time.deltaTime * fillProgressSpeed;
            }
            else
            {
                progress = Mathf.Max(0f, progress - Time.deltaTime * dropProgressSpeed);
            }

            progressBar.anchorMax = new Vector2(1f, Mathf.Clamp01(progress));

            // progress <- distance
            // fillProgressSpeed <- speed
            // setupThresholdInSeconds <- time
            // setupProgressThreshold <- distance (distance = time * speed)
            var setupProgressThreshold = setupThresholdInSeconds * fillProgressSpeed;
            IsReady = progress >= setupProgressThreshold;
        }

        private bool DetectPose(SimplePose pose)
        {
            if (!pose.TryGetNode(SimplePose.NodeIndex.Chest, out var chest))
            {
                error = SetupError.PoseNotFound;
                return false;
            }

            if (pose.pixelsPerInch < minPpi)
            {
                error = SetupError.PoseTooFar;
                return false;
            }

            if (pose.pixelsPerInch > maxPpi)
            {
                error = SetupError.PoseTooClose;
                return false;
            }

            var chestX = Mathf.InverseLerp(latestPlayArea.xMin, latestPlayArea.xMax, chest.x);
            var chestDetectionMargin = chestDetectionMarginInInches * pose.pixelsPerInch;
            var playerPosition = playAreaController.PlayerPositions[playerIndex];
            if (chestX < playerPosition - chestDetectionMargin || chestX > playerPosition + chestDetectionMargin)
            {
                error = SetupError.PoseNotCentered;
                return false;
            }

            return true;
        }

        private bool DetectRaiseHand(SimplePose pose)
        {
            var isLeftHandRaised = GestureUtils.IsLeftHandRaised(pose, isHandRaisedDebounced.Value);
            var isRightHandRaised = GestureUtils.IsRightHandRaised(pose, isHandRaisedDebounced.Value);

            if (!isLeftHandRaised && !isRightHandRaised)
            {
                return false;
            }

            handIndex = isRightHandRaised ? SimplePose.NodeIndex.RightHand : SimplePose.NodeIndex.LeftHand;

            error = SetupError.None;
            return true;
        }

        private void HandlePlayAreaUpdate(Rect playArea)
        {
            latestPlayArea = playArea;
        }

        public async UniTask<SimplePose.NodeIndex> WaitUntilIsReady(CancellationToken cancellationToken)
        {
            await UniTask.WaitUntil(() => IsReady, cancellationToken: cancellationToken);
            return handIndex;
        }
    }
}
