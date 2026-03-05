#nullable enable

using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    public class RawPreviewFrameProvider : PreviewFrameProvider
    {
        protected override void HandleFrameInformation(FrameInformation frameInfo)
        {
            Publish(new PreviewFrameConfig(frameInfo.texture,
                frameInfo.shouldMirror ? new Rect(1, 0, -1, 1) : new Rect(0, 0, 1, 1)));
        }
    }
}
