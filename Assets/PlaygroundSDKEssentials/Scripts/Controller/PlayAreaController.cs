#nullable enable

using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nex.Essentials
{
    public class PlayAreaController : MonoBehaviour
    {
        [SerializeField] private MdkController mdkController = null!;

        public MdkController MdkController
        {
            get => mdkController;
            set => mdkController = value;
        }

        [SerializeField] private BodyPoseController bodyPoseController = null!;

        public BodyPoseController BodyPoseController
        {
            get => bodyPoseController;
            set => bodyPoseController = value;
        }

        [SerializeField] private float[] playerPositions = { 0.5f };

        [SerializeField,
         Tooltip("At minimum, how small can the play area be, express in the percentage of the raw frame")]
        private float minimumScale = 0.15f;

        public float MinimumScale
        {
            get => minimumScale;
            set => minimumScale = value;
        }

        [SerializeField] private float aspectRatio = 16f / 9f;

        public float AspectRatio
        {
            get => aspectRatio;
            set => aspectRatio = value;
        }

        [SerializeField] private float zoomInSmoothTime = 0.5f;

        public float ZoomInSmoothTime
        {
            get => zoomInSmoothTime;
            set => zoomInSmoothTime = value;
        }

        [SerializeField] private float zoomOutSmoothTime = 1f;

        public float ZoomOutSmoothTime
        {
            get => zoomOutSmoothTime;
            set => zoomOutSmoothTime = value;
        }

        public enum MarginScope
        {
            Individual,
            Group,
        }

        [Serializable]
        public class Margins
        {
            public float horizontalMarginInInches = 32;
            public float topMarginInInches = 24;
            public float bottomMarginInInches = 34;

            [Tooltip("Determine if the margins are applied to each individual or the whole group")]
            public MarginScope marginScope = MarginScope.Group;
        }

        [SerializeField] private Margins margins = null!;

        public float HorizontalMarginInInches
        {
            get => margins.horizontalMarginInInches;
            set => margins.horizontalMarginInInches = value;
        }

        public float TopMarginInInches
        {
            get => margins.topMarginInInches;
            set => margins.topMarginInInches = value;
        }

        public float BottomMarginInInches
        {
            get => margins.bottomMarginInInches;
            set => margins.bottomMarginInInches = value;
        }

        public MarginScope ScopeOfMargins
        {
            get => margins.marginScope;
            set => margins.marginScope = value;
        }

        [SerializeField] private float smoothWindow = 1f;

        public float SmoothWindow
        {
            get => smoothWindow;
            set
            {
                smoothWindow = value;
                smoothedPlayArea.SmoothWindow = smoothWindow;
            }
        }

        public enum LossFunction
        {
            L1_MAE,
            L2_MSE,
            RMSE
        }

        [Serializable]
        public class PlayAreaSearchConfig
        {
            [Tooltip("The maximum scale allowed to enlarge the play area to find a best fit.")]
            public float maxSearchScale = 2f;

            [Tooltip("The maximum iterations to search for the play area.")]
            public float maxIterations = 8;

            [Tooltip("Terminate if the error between the two candidate play area is smaller than this tolerance.")]
            public float errorToleranceInInches = 0.01f;

            [Tooltip("The loss function algorithm.")]
            public LossFunction lossFunction = LossFunction.RMSE;
        }

        [SerializeField] private PlayAreaSearchConfig playerPositionSearchConfig = null!;
        private const float InvPhi = 0.6180339887f;

        private readonly AsyncReactiveProperty<Rect> playAreaProperty = new(Rect.zero);

        public IReadOnlyAsyncReactiveProperty<Rect> GetPlayAreaStream() => playAreaProperty;

        [SerializeField] private bool locked;

        private TimeMovingAverageFilterRect smoothedPlayArea = new(1);
        private SmoothDampedRect zoomingPlayArea = new(1);
        private Vector2[] positions = { };

        public float GetAspectRatio() => aspectRatio;

        public float[] PlayerPositions
        {
            get => playerPositions.ToArray();
            set
            {
                playerPositions = value.ToArray();
                UpdatePlayerPositions(RectifyPlayArea(zoomingPlayArea.CurrentValue));
            }
        }

        public bool Locked
        {
            get => locked;
            set => locked = value;
        }

        private void OnEnable()
        {
            smoothedPlayArea.SmoothWindow = smoothWindow;
            smoothedPlayArea.Reset();
            var fullRect = new Rect(0, 0, 1, 1);
            smoothedPlayArea.Update(fullRect);
            zoomingPlayArea.Reset(fullRect);
            playAreaProperty.Value = zoomingPlayArea.CurrentValue;
            UpdatePlayerPositions(fullRect);
        }

        private Rect RectifyPlayArea(Rect candidate)
        {
            // Make sure the area is still center aligned and with the proper aspect ratio.
            var centerY = candidate.y + candidate.height / 2;
            const float centerX = 0.5f * Constants.rawFrameAspectRatio;
            var width = Mathf.Min(Constants.rawFrameAspectRatio, candidate.width);
            var height = Mathf.Min(1, candidate.height);
            if (width > height * aspectRatio)
            {
                height = width / aspectRatio;
            }
            else
            {
                width = height * aspectRatio;
            }

            return new Rect(centerX - 0.5f * width, centerY - 0.5f * height, width, height);
        }

        private void UpdatePlayerPositions(Rect playArea)
        {
            var n = playerPositions.Length;
            if (n != positions.Length)
            {
                positions = new Vector2[n];
            }

            // Update tracking params.
            for (var i = 0; i < n; ++i)
            {
                positions[i] = new Vector2(
                    Mathf.InverseLerp(0, Constants.rawFrameAspectRatio,
                        playArea.x + playerPositions[i] * playArea.width), playArea.y + playArea.height * 0.5f);
            }

            mdkController.SetPlayerPositions(positions);
        }

        private void Update()
        {
            if (locked) return;

            // We go through the poses and figure out a good play area for the players.
            var candidatePlayArea = ComputePlayArea();
            var targetRect = smoothedPlayArea.Update(candidatePlayArea);

            var currentRect = zoomingPlayArea.CurrentValue;
            var currentArea = currentRect.width * currentRect.height;
            var targetArea = targetRect.width * targetRect.height;
            zoomingPlayArea.SetTargetValue(targetRect);
            // See if we want to zoom out or zoom in.
            var newRect =
                RectifyPlayArea(
                    zoomingPlayArea.Update(targetArea > currentArea ? zoomOutSmoothTime : zoomInSmoothTime));
            UpdatePlayerPositions(newRect);
            playAreaProperty.Value = newRect;
        }

        // First find the tightest crop area
        // Then try enlarging the play area if one can fit the players to the list of player positions better
        private Rect ComputePlayArea()
        {
            var marginTop = 1f;
            var marginBottom = 1f;
            var marginLeft = Constants.rawFrameAspectRatio;
            var marginRight = Constants.rawFrameAspectRatio;

            var n = playerPositions.Length;
            var playerCount = mdkController.PlayerCount;
            var playerChestXPos = new float?[playerCount];
            var ppiSum = 0f;
            var detectedChestCount = 0;
            var hasActivePlayer = false;

            // Loop through the list of players to find the tightest crop height
            for (var playerIndex = 0; playerIndex < n && playerIndex < playerCount; ++playerIndex)
            {
                if (!bodyPoseController.TryGetBodyPose(playerIndex, BodyPoseController.PoseFlavor.Smoothed,
                        out var pose))
                    continue;
                hasActivePlayer = true;
                var ppi = pose.pixelsPerInch;
                ppiSum += ppi;
                if (!pose.TryGetNode(SimplePose.NodeIndex.Chest, out var chest)) continue;

                // Store their chest position to fit play area to player positions later
                detectedChestCount++;
                playerChestXPos[playerIndex] = chest.x;

                // Define the rect for the pose.
                var poseLeft = chest.x - ppi * margins.horizontalMarginInInches;
                var poseRight = chest.x + ppi * margins.horizontalMarginInInches;
                var poseTop = chest.y + ppi * margins.topMarginInInches;
                var poseBottom = chest.y - ppi * margins.bottomMarginInInches;

                // Convert them to margins.
                var poseMarginLeft = Mathf.Max(0, poseLeft);
                var poseMarginRight = Mathf.Max(0, Constants.rawFrameAspectRatio - poseRight);
                var poseMarginBottom = Mathf.Max(0, poseBottom);
                var poseMarginTop = Mathf.Max(0, 1 - poseTop);

                if (poseMarginLeft < marginLeft) marginLeft = poseMarginLeft;
                if (poseMarginRight < marginRight) marginRight = poseMarginRight;
                if (poseMarginTop < marginTop) marginTop = poseMarginTop;
                if (poseMarginBottom < marginBottom) marginBottom = poseMarginBottom;
            }

            if (!hasActivePlayer)
            {
                marginBottom = marginTop = marginLeft = marginRight = 0;
            }

            // First find the tightest crop height and crop width separately first
            // Crop height is the frame height minus the top and bottom margin
            var cropHeight = Mathf.Max(1 - marginTop - marginBottom, minimumScale);

            var ppiAvg = ppiSum / detectedChestCount;

            // If apply margin on the group, consider the margin around the left & right player
            var horizMargin = Mathf.Min(marginLeft, marginRight);
            var widthByGroupMargin = Constants.rawFrameAspectRatio - 2 * horizMargin;

            // Crop width depends on the scope that player margins apply to
            float cropWidth;
            if (margins.marginScope == MarginScope.Group)
            {
                cropWidth = widthByGroupMargin;
            }
            else
            {
                // If apply margin per player, consider the width as (left + right margin) * num of players
                var widthByIndividualMargin = margins.horizontalMarginInInches * ppiAvg * playerCount * 2;

                // But also consider the width calculated by group margin
                // So that the play area always contain all players
                var widthByMargin = Mathf.Max(widthByIndividualMargin, widthByGroupMargin);
                cropWidth = widthByMargin;
            }

            // Clamp crop width so it is within the camera frame bounds
            cropWidth = Mathf.Clamp(cropWidth, minimumScale * Constants.rawFrameAspectRatio,
                Constants.rawFrameAspectRatio);

            // Adjust the width and height to match the aspect ratio
            if (cropWidth > cropHeight * Constants.rawFrameAspectRatio)
            {
                // Crop height is tighter than crop width after scaled by the aspect ratio
                // The horizontal space will be cropped too much if crop height is not increased
                cropHeight = cropWidth / Constants.rawFrameAspectRatio;
            }
            else
            {
                // Crop width is tighter than crop height after scaled by the aspect ratio
                // The vertical space will be cropped too much if crop width is not increased
                cropWidth = cropHeight * Constants.rawFrameAspectRatio;
            }

            // Find the center of the rectangle
            // Horizontally must be center aligned
            // Vertically is the middle point between the top and bottom margin
            var center = new Vector2(Constants.rawFrameAspectRatio / 2,
                marginBottom + (1 - marginBottom - marginTop) / 2);
            var playAreaByMargin = new Rect(center.x - cropWidth / 2, center.y - cropHeight / 2, cropWidth, cropHeight);

            // Cannot enlarge a Rect past height = 1
            var maxScale = Mathf.Clamp(1 / cropHeight, 1, playerPositionSearchConfig.maxSearchScale);

            // Skip golden section search if the number of player == 1
            // The calculated bounding box by margin is already the best fit for single player
            var bestScale = playerCount != 1
                ? GoldenSectionSearch(playAreaByMargin, playerChestXPos, 1, maxScale, ppiAvg)
                : 1;
            var bestRect = EnlargeRect(playAreaByMargin, bestScale);

            // The best rect from GoldenSectionSearch may be outside the screen
            // But the maxScale calculation already ensure that best rect cannot be taller than 1
            // So we can just move the best rect up/down if it moves outside the bound
            // The rect is already guaranteed to be center aligned horizontally
            if (bestRect.yMax > 1)
            {
                // Too high, shift down
                bestRect.y -= bestRect.yMax - 1;
            }
            else if (bestRect.yMin < 0)
            {
                // Too low, shift up
                bestRect.y += 0 - bestRect.yMin;
            }

            return bestRect;
        }

        // The error is calculating the difference in inches between the player current position and the ideal position
        private float FindPlayerPositionsError(Rect playArea, float?[] chestXPositions, float ppiAvg)
        {
            var error = 0f;
            var n = chestXPositions.Length;
            var lostFunc = playerPositionSearchConfig.lossFunction;

            for (var index = 0; index < n; ++index)
            {
                var chestX = chestXPositions[index];
                if (!chestX.HasValue) continue;

                // Find the position in aspect normalised coordinate
                // Find the difference in inches to calculate the lost
                var aspectNormalisedPosition = playArea.x + playArea.width * playerPositions[index];
                var diff = (chestX.Value - aspectNormalisedPosition) / ppiAvg;

                error += lostFunc switch
                {
                    LossFunction.L1_MAE => Mathf.Abs(diff),
                    LossFunction.L2_MSE or LossFunction.RMSE => diff * diff,
                    _ => throw new ArgumentOutOfRangeException()
                };
            }

            var meanError = error / n;
            return lostFunc switch
            {
                LossFunction.L1_MAE => meanError,
                LossFunction.L2_MSE => meanError,
                LossFunction.RMSE => Mathf.Sqrt(meanError),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private static Rect EnlargeRect(Rect rect, float scale)
        {
            if (Mathf.Approximately(scale, 1f)) return rect;
            var newSize = rect.size * scale;
            var diff = newSize - rect.size;
            return new Rect(rect.xMin - diff.x / 2, rect.yMin - diff.y / 2, newSize.x, newSize.y);
        }

        private float GoldenSectionSearch(Rect playArea, float?[] chestXPos, float start, float end, float ppiAvg)
        {
            var iterationsCount = 0;
            float? aErrorFromLastIteration = null;
            float? bErrorFromLastIteration = null;
            while (true)
            {
                var ratio = (end - start) * InvPhi;
                var a = end - ratio;
                var b = start + ratio;

                var aError = aErrorFromLastIteration ??
                             FindPlayerPositionsError(EnlargeRect(playArea, a), chestXPos, ppiAvg);
                var bError = bErrorFromLastIteration ??
                             FindPlayerPositionsError(EnlargeRect(playArea, b), chestXPos, ppiAvg);

                if (Mathf.Abs(aError - bError) < playerPositionSearchConfig.errorToleranceInInches)
                {
                    // Always return the tightest crop, because it best fit the margin
                    // For the first iteration, can return the start as the result
                    // Otherwise, return the tighter crop as the result
                    return iterationsCount == 0 ? start : a;
                }

                if (iterationsCount >= playerPositionSearchConfig.maxIterations)
                {
                    return aError < bError ? a : b;
                }

                if (aError < bError)
                {
                    end = b;
                    aErrorFromLastIteration = null;
                    bErrorFromLastIteration = aError;
                }
                else
                {
                    start = a;
                    aErrorFromLastIteration = bError;
                    bErrorFromLastIteration = null;
                }

                iterationsCount += 1;
            }
        }
    }
}
