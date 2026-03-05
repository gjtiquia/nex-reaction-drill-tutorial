#nullable enable

using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SilhouetteGridRenderer: MaskableGraphic
    {
        [SerializeField] private Vector2Int aspectRatio;
        [SerializeField, Min(0)] private int blockResolution;
        [SerializeField] private SilhouetteVisualizer silhouetteVisualizer = null!;

        protected SilhouetteGridRenderer()
        {
            useLegacyMeshGeneration = false;
        }

        public override Texture mainTexture => silhouetteVisualizer.ActiveTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            if (silhouetteVisualizer.ActiveTexture == null) return;

            var rect = GetPixelAdjustedRect();
            if (rect.width < 0.01f || rect.height < 0.01f) return;

            var columns = aspectRatio.x * blockResolution;
            var rows = aspectRatio.y * blockResolution;

            // We are going to draw blockResolution * aspectRatio over the rect area.
            var vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.position = new Vector3(rect.xMin, rect.yMin);
            vertex.uv0 = new Vector4(0, 0, columns, rows);
            vertex.uv1 = new Vector4(0, 0, 0, 0);
            vh.AddVert(vertex);
            vertex.position = new Vector3(rect.xMin, rect.yMax);
            vertex.uv0 = new Vector4(0, 1, columns, rows);
            vertex.uv1 = new Vector4(0, rows, 0, 0);
            vh.AddVert(vertex);
            vertex.position = new Vector3(rect.xMax, rect.yMax);
            vertex.uv0 = new Vector4(1, 1, columns, rows);
            vertex.uv1 = new Vector4(columns, rows, 0, 0);
            vh.AddVert(vertex);
            vertex.position = new Vector3(rect.xMax, rect.yMin);
            vertex.uv0 = new Vector4(1, 0, columns, rows);
            vertex.uv1 = new Vector4(columns, 0, 0, 0);
            vh.AddVert(vertex);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(0, 2, 3);
        }
    }
}
