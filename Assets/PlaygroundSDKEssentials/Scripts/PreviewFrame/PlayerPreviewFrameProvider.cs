#nullable enable

using System;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    public class PlayerPreviewFrameProvider : PreviewFrameProvider
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private SimplePose.NodeIndex nodeIndex;
        [SerializeField] private int playerIndex;
        [SerializeField] private BodyPoseController.PoseFlavor poseFlavor;

        [Serializable]
        public class Margins
        {
            public float horizontalMarginInInches = 6;
            public float topMarginInInches = 6;
            public float bottomMarginInInches = 12;
        }

        [SerializeField] private Margins margins = null!;

        // Determine if this preview frame image has rendered something before
        // If it has not, and the body pose is not detected, render something in the player position as a fallback
        private bool hasRendered;

        [SerializeField, Tooltip("Width of the fallback frame if not player is found")]
        private float fallbackFrameWidth = 0.5f;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.1f;

        [SerializeField] private float maxSpeed = float.PositiveInfinity;

        private Rect currentFrameRect;
        private float xMinVelocity, yMinVelocity, xMaxVelocity, yMaxVelocity;

        protected override void HandleFrameInformation(FrameInformation frameInfo)
        {
            if (!bodyPoseController.TryGetBodyPose(playerIndex, poseFlavor, out var pose))
            {
                MaybeRenderFallback(frameInfo);
                return;
            }

            var ppi = pose.pixelsPerInch;
            if (!pose.TryGetNode(nodeIndex, out var node))
            {
                MaybeRenderFallback(frameInfo);
                return;
            }

            var poseLeft = node.x - ppi * margins.horizontalMarginInInches;
            var poseRight = node.x + ppi * margins.horizontalMarginInInches;
            var poseTop = node.y + ppi * margins.topMarginInInches;
            var poseBottom = node.y - ppi * margins.bottomMarginInInches;

            var frameRect = Rect.MinMaxRect(poseLeft, poseBottom, poseRight, poseTop);
            var shrunkRect = ShrinkRectIfOversize(frameRect);
            var finalRect = AdjustCenterIfOutOfBound(shrunkRect);

            hasRendered = true;
            PublishWithRect(frameInfo, finalRect);
        }

        // If the PIP is too tall or too wide
        // Shrink it till it fit in the camera frame while maintaining the aspect ratio
        private static Rect ShrinkRectIfOversize(Rect rect)
        {
            var aspectRatio = rect.width / rect.height;

            // too tall
            if (rect.height > 1)
            {
                var diff = rect.height - 1;
                var halfDiff = diff / 2;
                rect = new Rect(rect.x + halfDiff * aspectRatio, rect.y + halfDiff, rect.width - diff * aspectRatio,
                    rect.height - diff);
            }

            // too wide
            if (rect.width > Constants.rawFrameAspectRatio)
            {
                var diff = rect.width - Constants.rawFrameAspectRatio;
                var halfDiff = diff / 2;
                rect = new Rect(rect.x + halfDiff, rect.y + halfDiff / aspectRatio, rect.width - diff,
                    rect.height - diff / aspectRatio);
            }

            return rect;
        }

        // If the PIP window is outside the raw camera frame
        // Translate the Rect till it fit in the camera frame
        // The input rect is assumed to be able to fit inside the camera frame with only translation
        private static Rect AdjustCenterIfOutOfBound(Rect rect)
        {
            if (rect.yMin < 0)
            {
                rect.y += -rect.yMin;
            }
            else if (rect.yMax > 1)
            {
                rect.y -= rect.yMax - 1;
            }

            if (rect.xMin < 0)
            {
                rect.x += -rect.xMin;
            }
            else if (rect.xMax > Constants.rawFrameAspectRatio)
            {
                rect.x -= rect.xMax - Constants.rawFrameAspectRatio;
            }

            return rect;
        }

        private void MaybeRenderFallback(FrameInformation frameInfo)
        {
            if (hasRendered)
            {
                Publish(new PreviewFrameConfig(frameInfo.texture, currentFrameRect));
                return;
            }

            if (mdkController.positions.Length <= playerIndex) return;

            var playerPosition = mdkController.positions[playerIndex];
            var playerXPosInFrame = playerPosition.x * Constants.rawFrameAspectRatio;
            var heightToWidth = (margins.topMarginInInches + margins.bottomMarginInInches) /
                                (margins.horizontalMarginInInches * 2);

            var halfWidth = fallbackFrameWidth * 0.5f;
            var halfHeight = halfWidth * heightToWidth;
            var frameRect = Rect.MinMaxRect(playerXPosInFrame - halfWidth, 0.5f - halfHeight,
                playerXPosInFrame + halfWidth, 0.5f + halfHeight);
            var finalRect = ShrinkRectIfOversize(frameRect);

            PublishWithRect(frameInfo, finalRect);
        }

        private void PublishWithRect(FrameInformation frameInfo, Rect finalRect)
        {
            var minX = frameInfo.shouldMirror ? Constants.rawFrameAspectRatio : 0;
            var maxX = frameInfo.shouldMirror ? 0 : Constants.rawFrameAspectRatio;

            var uvRect = Rect.MinMaxRect(Mathf.InverseLerp(minX, maxX, finalRect.xMin),
                Mathf.InverseLerp(0, 1, finalRect.yMin), Mathf.InverseLerp(minX, maxX, finalRect.xMax),
                Mathf.InverseLerp(0, 1, finalRect.yMax));

            var xMin = Mathf.SmoothDamp(currentFrameRect.xMin, uvRect.xMin, ref xMinVelocity, smoothTime, maxSpeed);
            var yMin = Mathf.SmoothDamp(currentFrameRect.yMin, uvRect.yMin, ref yMinVelocity, smoothTime, maxSpeed);
            var xMax = Mathf.SmoothDamp(currentFrameRect.xMax, uvRect.xMax, ref xMaxVelocity, smoothTime, maxSpeed);
            var yMax = Mathf.SmoothDamp(currentFrameRect.yMax, uvRect.yMax, ref yMaxVelocity, smoothTime, maxSpeed);

            var smoothedUvRect = hasRendered ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : uvRect;
            currentFrameRect = smoothedUvRect;

            Publish(new PreviewFrameConfig(frameInfo.texture, smoothedUvRect));
        }
    }
}
