#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class PlayAreaDebugVisualizer : MaskableGraphic
    {
        [SerializeField] private PlayAreaController playAreaController = null!;
        [SerializeField] private float borderWidth = 2;

        private Rect region = new Rect(0, 0, 1, 1);

        private PlayAreaDebugVisualizer()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void Awake()
        {
            base.Awake();
            if (playAreaController == null) return;  // This is for editor.
            playAreaController.GetPlayAreaStream()
                .Subscribe(HandlePlayAreaUpdate, this.GetCancellationTokenOnDestroy());
        }

        private void HandlePlayAreaUpdate(Rect playArea)
        {
            // Assume playArea is drawn over the raw frame.
            var leftRatio = Mathf.InverseLerp(0, Constants.rawFrameAspectRatio, playArea.xMin);
            var rightRatio = Mathf.InverseLerp(0, Constants.rawFrameAspectRatio, playArea.xMax);
            var topRatio = playArea.yMax;
            var bottomRatio = playArea.yMin;
            region = Rect.MinMaxRect(leftRatio, bottomRatio, rightRatio, topRatio);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            // Find the corners of the region.
            var left = Mathf.Lerp(rect.xMin, rect.xMax, region.xMin);
            var right = Mathf.Lerp(rect.xMin, rect.xMax, region.xMax);
            var bottom = Mathf.Lerp(rect.yMin, rect.yMax, region.yMin);
            var top = Mathf.Lerp(rect.yMin, rect.yMax, region.yMax);

            var playAreaRect = new Rect(left, top, right - left, bottom - top);

            // Now we have a region.
            // We are going to draw a border.
            // Compute the vertices.
            var half = borderWidth * 0.5f;
            var inner = new Vector4(left + half, bottom + half, right - half, top - half);
            var outer = new Vector4(left - half, bottom - half, right + half, top + half);

            var positions = playAreaController.PlayerPositions;

            // Add the vertices.
            // 5-----------------------6
            // |\                     /|
            // | 1-------------------2 |
            // | |                   | |
            // | 0-------------------3 |
            // |/                     \|
            // 4-----------------------7
            var color32 = (Color32)color;
            vh.AddVert(new Vector3(inner.x, inner.y), color32, Vector4.zero);
            vh.AddVert(new Vector3(inner.x, inner.w), color32, Vector4.zero);
            vh.AddVert(new Vector3(inner.z, inner.w), color32, Vector4.zero);
            vh.AddVert(new Vector3(inner.z, inner.y), color32, Vector4.zero);
            vh.AddVert(new Vector3(outer.x, outer.y), color32, Vector4.zero);
            vh.AddVert(new Vector3(outer.x, outer.w), color32, Vector4.zero);
            vh.AddVert(new Vector3(outer.z, outer.w), color32, Vector4.zero);
            vh.AddVert(new Vector3(outer.z, outer.y), color32, Vector4.zero);

            vh.AddTriangle(0, 4, 5);
            vh.AddTriangle(0, 5, 1);
            vh.AddTriangle(1, 5, 6);
            vh.AddTriangle(1, 6, 2);
            vh.AddTriangle(2, 6, 7);
            vh.AddTriangle(2, 7, 3);
            vh.AddTriangle(3, 7, 4);
            vh.AddTriangle(3, 4, 0);

            var indicatorColor = color32;
            indicatorColor.a /= 2;
            foreach (var position in positions)
            {
                DrawVerticalLine(vh, playAreaRect, position, indicatorColor);
            }
        }

        private void DrawVerticalLine(VertexHelper vh, Rect playAreaRect, float ratio, Color32 color32)
        {
            var half = borderWidth * 0.5f;
            var top = playAreaRect.yMax - half;
            var bottom = playAreaRect.yMin + half;

            var mid = playAreaRect.x + playAreaRect.width * ratio;
            var left = mid - half;
            var right = mid + half;

            var vertCount = vh.currentVertCount;
            vh.AddVert(new Vector3(left, bottom), color32, Vector4.zero);
            vh.AddVert(new Vector3(left, top), color32, Vector4.zero);
            vh.AddVert(new Vector3(right, top), color32, Vector4.zero);
            vh.AddVert(new Vector3(right, bottom), color32, Vector4.zero);
            
            vh.AddTriangle(vertCount + 0, vertCount + 1, vertCount + 2);
            vh.AddTriangle(vertCount + 0, vertCount + 2, vertCount + 3);
        }
    }
}
