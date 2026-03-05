#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PreviewFrameImage : MaskableGraphic
    {
        [SerializeField] private PreviewFrameProvider provider = null!;

        private Texture? activeTexture; private Rect uvRect;

        protected PreviewFrameImage()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void Awake()
        {
            base.Awake();
            if (provider != null)  // The null check is for temporary editor state.
            {
                provider.GetPreviewFrameStream().Subscribe(HandlePreviewFrame, this.GetCancellationTokenOnDestroy());
            }
        }

        private void HandlePreviewFrame(PreviewFrameProvider.PreviewFrameConfig config)
        {
            activeTexture = config.texture;
            uvRect = config.uvRect;
            SetAllDirty();
        }

        public override Texture? mainTexture => activeTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var color32 = (Color32) color;
            var rectCoords = new Vector4(rect.x, rect.y, rect.x + rect.width, rect.y + rect.height);
            var uvCoords = new Vector4(uvRect.x, uvRect.y, uvRect.x + uvRect.width, uvRect.y + uvRect.height);
            vh.AddVert(new Vector3(rectCoords.x, rectCoords.y), color32, new Vector4(uvCoords.x, uvCoords.y));
            vh.AddVert(new Vector3(rectCoords.x, rectCoords.w), color32, new Vector4(uvCoords.x, uvCoords.w));
            vh.AddVert(new Vector3(rectCoords.z, rectCoords.w), color32, new Vector4(uvCoords.z, uvCoords.w));
            vh.AddVert(new Vector3(rectCoords.z, rectCoords.y), color32, new Vector4(uvCoords.z, uvCoords.y));
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
