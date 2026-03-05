#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class BodyPoseVisualizer : MaskableGraphic
    {
        [SerializeField] private PreviewFrameProvider previewFrameProvider = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int playerIndex;
        [SerializeField] private BodyPoseController.PoseFlavor poseFlavor = BodyPoseController.PoseFlavor.Raw;
        [SerializeField] private float nodeSize = 1;

        private Rect referenceUVRect = Rect.zero;

        protected BodyPoseVisualizer()
        {
            useLegacyMeshGeneration = false;
        }

        protected override void Awake()
        {
            base.Awake();
            if (previewFrameProvider != null) // null checking for unity editor.
            {
                previewFrameProvider.GetPreviewFrameStream()
                    .Subscribe(HandlePreviewFrameConfig, this.GetCancellationTokenOnDestroy());
            }
        }

        private void Update()
        {
            // We are going to redraw.
            SetVerticesDirty();
        }

        private const float rawFrameAspectRatio = 16f / 9f;

        private void HandlePreviewFrameConfig(PreviewFrameProvider.PreviewFrameConfig config)
        {
            // The uv rect may be flipping horizontally, but the pose would not. Rectify it.
            referenceUVRect = config.uvRect;
            if (referenceUVRect.width < 0)
            {
                referenceUVRect.x = 1 - referenceUVRect.x;
                referenceUVRect.width = -referenceUVRect.width;
            }

            // The x and width should be scaled by the aspect ratio of the raw frame.
            referenceUVRect.x *= rawFrameAspectRatio;
            referenceUVRect.width *= rawFrameAspectRatio;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            if (playerIndex < 0) return;

            // Don't render anything if the reference UV Rect is not well defined.
            if (referenceUVRect.width < 1e-3f || referenceUVRect.height < 1e-3f) return;

            if (!bodyPoseController.TryGetBodyPose(playerIndex, poseFlavor, out var pose)) return;
            // Otherwise, we take all the pose and draw the nodes.

            var rect = GetPixelAdjustedRect();
            if (rect.width < 1e-3f || rect.height < 1e-3f) return; // Don't render anything if they are too small.

            var nosePoint = ComputeCoordinate(pose, SimplePose.NodeIndex.Nose, rect);

            var chestPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.Chest, rect);

            var leftShoulderPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.LeftShoulder, rect);
            var rightShoulderPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.RightShoulder, rect);
            var leftElbowPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.LeftElbow, rect);
            var rightElbowPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.RightElbow, rect);
            var leftWristPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.LeftWrist, rect);
            var rightWristPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.RightWrist, rect);

            var leftHipPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.LeftHip, rect);
            var rightHipPoint = ComputeCoordinate(pose, SimplePose.NodeIndex.RightHip, rect);
            var leftKneePoint = ComputeCoordinate(pose, SimplePose.NodeIndex.LeftKnee, rect);
            var rightKneePoint = ComputeCoordinate(pose, SimplePose.NodeIndex.RightKnee, rect);
            var leftAnklePoint = ComputeCoordinate(pose, SimplePose.NodeIndex.LeftAnkle, rect);
            var rightAnklePoint = ComputeCoordinate(pose, SimplePose.NodeIndex.RightAnkle, rect);

            Color32 nodeColor = color;
            Color32 lineColor = color * new Color(1, 1, 1, 0.5f);
            var lineMeshSize = pose.pixelsPerInch / referenceUVRect.height * rect.height * 0.5f * nodeSize;
            var nodeMeshSize = lineMeshSize * 2f;

            DrawLine(vh, lineColor, chestPoint, nosePoint, lineMeshSize);

            DrawLine(vh, lineColor, chestPoint, leftShoulderPoint, lineMeshSize);
            DrawLine(vh, lineColor, chestPoint, rightShoulderPoint, lineMeshSize);
            DrawLine(vh, lineColor, leftShoulderPoint, leftElbowPoint, lineMeshSize);
            DrawLine(vh, lineColor, rightShoulderPoint, rightElbowPoint, lineMeshSize);
            DrawLine(vh, lineColor, leftElbowPoint, leftWristPoint, lineMeshSize);
            DrawLine(vh, lineColor, rightElbowPoint, rightWristPoint, lineMeshSize);

            DrawLine(vh, lineColor, leftHipPoint, leftKneePoint, lineMeshSize);
            DrawLine(vh, lineColor, rightHipPoint, rightKneePoint, lineMeshSize);
            DrawLine(vh, lineColor, leftKneePoint, leftAnklePoint, lineMeshSize);
            DrawLine(vh, lineColor, rightKneePoint, rightAnklePoint, lineMeshSize);

            DrawNode(vh, nodeColor, nosePoint, nodeMeshSize);
            DrawNode(vh, nodeColor, chestPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, leftShoulderPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, rightShoulderPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, leftElbowPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, rightElbowPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, leftWristPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, rightWristPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, leftHipPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, rightHipPoint, nodeMeshSize);
            DrawNode(vh, nodeColor, leftKneePoint, nodeMeshSize);
            DrawNode(vh, nodeColor, rightKneePoint, nodeMeshSize);
            DrawNode(vh, nodeColor, leftAnklePoint, nodeMeshSize);
            DrawNode(vh, nodeColor, rightAnklePoint, nodeMeshSize);
        }

        private Vector2? ComputeCoordinate(SimplePose pose, SimplePose.NodeIndex nodeIndex, Rect rect)
        {
            var node = pose[nodeIndex];
            if (!node.HasValue) return null;
            var normalizedPosition = node.Value;
            return (normalizedPosition - referenceUVRect.position) * (rect.height / referenceUVRect.height) +
                   rect.position;
        }

        private static void DrawLine(VertexHelper vh, Color32 color, Vector2? optionalFrom, Vector2? optionalTo,
            float width)
        {
            if (!optionalFrom.HasValue || !optionalTo.HasValue) return;
            var from = optionalFrom.Value;
            var to = optionalTo.Value;
            var dir = to - from;
            var length = dir.magnitude;
            if (length < 1e-3f) return; // Ignore segments that are too short.
            dir /= length;
            var normal = new Vector2(-dir.y, dir.x);

            var offset = vh.currentVertCount;
            var halfWidth = width * 0.5f;
            vh.AddVert(from + normal * halfWidth, color, Vector4.zero);
            vh.AddVert(to, color, Vector4.zero);
            vh.AddVert(from - normal * halfWidth, color, Vector4.zero);
            vh.AddTriangle(offset + 0, offset + 1, offset + 2);
        }

        private static void DrawNode(VertexHelper vh, Color32 color, Vector2? optionalCenter, float size)
        {
            if (!optionalCenter.HasValue) return;
            var center = optionalCenter.Value;
            var offset = vh.currentVertCount;
            vh.AddVert(new Vector3(center.x, center.y - size), color, Vector4.zero);
            vh.AddVert(new Vector3(center.x - size, center.y), color, Vector4.zero);
            vh.AddVert(new Vector3(center.x, center.y + size), color, Vector4.zero);
            vh.AddVert(new Vector3(center.x + size, center.y), color, Vector4.zero);
            vh.AddTriangle(offset + 0, offset + 1, offset + 2);
            vh.AddTriangle(offset + 0, offset + 2, offset + 3);
        }
    }
}
