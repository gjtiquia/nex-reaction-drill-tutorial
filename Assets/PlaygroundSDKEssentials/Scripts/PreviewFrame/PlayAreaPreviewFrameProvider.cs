#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    public class PlayAreaPreviewFrameProvider : PreviewFrameProvider
    {
        [SerializeField] private PlayAreaController playAreaController = null!;

        private Texture latestRawFrameTexture = null!;
        private Rect rawFrameRect = new(0, 0, 1, 1);
        private Rect latestPlayArea = new(0, 0, 1, 1);

        protected override void Awake()
        {
            base.Awake();
            playAreaController.GetPlayAreaStream()
                .Subscribe(HandlePlayAreaUpdate, this.GetCancellationTokenOnDestroy());
        }

        protected override void HandleFrameInformation(FrameInformation frameInfo)
        {
            latestRawFrameTexture = frameInfo.texture;
            rawFrameRect = frameInfo.shouldMirror ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1);
            PublishIfValid();
        }

        private void HandlePlayAreaUpdate(Rect playArea)
        {
            latestPlayArea = playArea;
            PublishIfValid();
        }

        private void PublishIfValid()
        {
            if (latestRawFrameTexture == null) return;

            var rawMinX = rawFrameRect.xMin;
            var rawMaxX = rawFrameRect.xMax;
            var rawMinY = rawFrameRect.yMin;
            var rawMaxY = rawFrameRect.yMax;

            // Now compute the normalized bounds of the play area.
            var leftRatio = Mathf.InverseLerp(0, Constants.rawFrameAspectRatio, latestPlayArea.xMin);
            var rightRatio = Mathf.InverseLerp(0, Constants.rawFrameAspectRatio, latestPlayArea.xMax);
            var bottomRatio = latestPlayArea.yMin;
            var topRatio = latestPlayArea.yMax;

            var uvRect = Rect.MinMaxRect(
                Mathf.Lerp(rawMinX, rawMaxX, leftRatio),
                Mathf.Lerp(rawMinY, rawMaxY, bottomRatio),
                Mathf.Lerp(rawMinX, rawMaxX, rightRatio),
                Mathf.Lerp(rawMinY, rawMaxY, topRatio));

            Publish(new PreviewFrameConfig(latestRawFrameTexture, uvRect));
        }
    }
}
