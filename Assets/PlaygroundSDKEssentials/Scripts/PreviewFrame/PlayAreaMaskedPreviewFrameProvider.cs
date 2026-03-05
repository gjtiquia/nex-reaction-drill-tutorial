#nullable enable

using System;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    /**
     * Crops an area of the play area frame.
     * The cropped area is a fixed area in the play area. It does not move or track the player.
     * This is useful during setup because we want to teach the player to stand at a specific location.
     */
    public class PlayAreaMaskedPreviewFrameProvider : PreviewFrameProvider
    {
        public PlayAreaPreviewFrameProvider previewFrameProvider = null!;

        [SerializeField, Tooltip("The horizontal player position in the play area to preference from"), Range(0, 1)]
        public float playerPosition;

        [SerializeField, Tooltip("How wide the setup frame in the play area"), Range(0, 1)]
        public float horizontalMargin = 0.15f;

        [SerializeField, Tooltip("The bottom of the preview frame in the play area"), Range(0, 1)]
        public float frameMaskYMin = 0.1f;

        [SerializeField, Tooltip("The top of the preview frame in the play area"), Range(0, 1)]
        public float frameMaskYMax = 0.9f;

        [ReadOnly, Tooltip("The final frame mask computed and applied")]
        public Rect frameMask = new(0, 0, 1, 1);

        [ReadOnly,
         Tooltip(
             "Given the above inputs, the aspect ratio of the preview frame (width / height) should be the same as this ratio (x / y).")]
        public Vector2 outputAspectRatio;

        /// <summary>
        /// Initialize the MaskedFrameProvider with the play area frame and the target player position in the play area
        /// </summary>
        public void Initialize(PlayAreaPreviewFrameProvider aPreviewFrameProvider, float aPlayerPosition)
        {
            previewFrameProvider = aPreviewFrameProvider;
            playerPosition = aPlayerPosition;
            UpdateFrameMask();

            previewFrameProvider.GetPreviewFrameStream()
                .Subscribe(HandlePlayAreaFrame, this.GetCancellationTokenOnDestroy());
        }

        private void UpdateFrameMask()
        {
            frameMask = Rect.MinMaxRect(playerPosition - horizontalMargin, frameMaskYMin,
                playerPosition + horizontalMargin, frameMaskYMax);
        }

        private void HandlePlayAreaFrame(PreviewFrameConfig config)
        {
            var playAreaRect = config.uvRect;
            var newUvRect = Rect.MinMaxRect(Mathf.Lerp(playAreaRect.xMin, playAreaRect.xMax, frameMask.xMin),
                Mathf.Lerp(playAreaRect.yMin, playAreaRect.yMax, frameMask.yMin),
                Mathf.Lerp(playAreaRect.xMin, playAreaRect.xMax, frameMask.xMax),
                Mathf.Lerp(playAreaRect.yMin, playAreaRect.yMax, frameMask.yMax));

            Publish(new PreviewFrameConfig(config.texture, newUvRect));
        }

        protected override void HandleFrameInformation(FrameInformation frameInfo)
        {
        }

        public void OnValidate()
        {
            UpdateFrameMask();

            // The frame mask is from 0 to 1 for both width and height
            // Scale that numbers to fit the 16:9 aspect ratio
            // widthFloat : heightFloat is the aspect ratio in the real frame
            var widthFloat = Mathf.Abs(frameMask.width * 16);
            var heightFloat = Mathf.Abs(frameMask.height * 9);

            outputAspectRatio = SolveFraction(widthFloat, heightFloat);
        }

        private static Vector2Int SolveFraction(float numerator, float denominator)
        {
            if (numerator < 1e-3f && denominator < 1e-3f) return Vector2Int.zero;
            if (denominator < 1e-3f) return new Vector2Int(1, 0);
            if (numerator < 1e-3f) return new Vector2Int(0, 1);

            return SolveFraction(numerator / denominator);
        }

        private static Vector2Int SolveFraction(double target, int iterations = 0)
        {
            var integral = (int)(Math.Floor(target) + 0.1);
            var remainder = target - integral;

            if (iterations >= 10 || remainder < 1e-3)
            {
                return new Vector2Int(integral, 1);
            }

            var res = SolveFraction(1 / remainder, iterations + 1);

            return new Vector2Int(integral * res.x + res.y, res.x);
        }
    }
}
