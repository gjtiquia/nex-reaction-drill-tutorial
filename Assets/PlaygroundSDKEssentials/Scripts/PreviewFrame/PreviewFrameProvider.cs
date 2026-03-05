#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Jazz;
using UnityEngine;
using UnityEngine.Events;

namespace Nex.Essentials
{
    public abstract class PreviewFrameProvider : MonoBehaviour
    {
        [SerializeField] protected MdkController mdkController = null!;

        public readonly struct PreviewFrameConfig
        {
            public readonly Texture texture;
            public readonly Rect uvRect;

            public PreviewFrameConfig(Texture texture, Rect uvRect)
            {
                this.texture = texture;
                this.uvRect = uvRect;
            }

            public static PreviewFrameConfig defaultConfig = new(null!, Rect.zero);
        }

        private readonly UnityEvent<PreviewFrameConfig> previewFramePublisher = new();

        public IUniTaskAsyncEnumerable<PreviewFrameConfig> GetPreviewFrameStream() =>
            previewFramePublisher.OnInvokeAsAsyncEnumerable(this.GetCancellationTokenOnDestroy());

        protected virtual void Awake()
        {
            if (!mdkController) return;

            mdkController
                .GetFrameInformationStream()
                .Where(info => info != null)
                .Subscribe(HandleFrameInformation, this.GetCancellationTokenOnDestroy());
        }

        protected void Publish(PreviewFrameConfig config)
        {
            previewFramePublisher.Invoke(config);
        }

        protected abstract void HandleFrameInformation(FrameInformation frameInfo);
    }
}
