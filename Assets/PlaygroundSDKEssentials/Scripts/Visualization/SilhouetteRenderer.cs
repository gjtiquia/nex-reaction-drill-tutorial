#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Nex.Essentials
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SilhouetteRenderer: MaskableGraphic
    {
        [SerializeField] private float halfStrokeWidthInches = 2f;
        [SerializeField] private float headRadiusInches = 3.5f;
        private readonly List<SimplePose?> activePoses = new();

        SilhouetteRenderer()
        {
            useLegacyMeshGeneration = false;
        }

        public void SetPose(int index, SimplePose? pose)
        {
            while (activePoses.Count <= index)
            {
                activePoses.Add(null);
            }

            activePoses[index] = pose;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            var color32 = (Color32) color;

            foreach (var pose in activePoses)
            {
                if (pose != null) DrawSinglePose(vh, pose.Value, rect, color32);
            }
        }

        private void DrawSinglePose(VertexHelper vh, SimplePose pose, Rect rect, Color32 color32)
        {
            if (!pose.Chest.HasValue) return;
            var chest = pose.Chest.Value;
            var halfStrokeWidth = pose.pixelsPerInch * halfStrokeWidthInches * rect.height;
            var chestIndex = DrawKnob(vh, color32, rect, chest, halfStrokeWidth);

            if (pose.Nose.HasValue)
            {
                var nosePosition = pose.Nose.Value;
                DrawLine(vh, color32, rect, chest, nosePosition, halfStrokeWidth);
                DrawKnob(vh, color32, rect, nosePosition, headRadiusInches * pose.pixelsPerInch * rect.height);
            }

            var leftShoulderIndex = -1;
            var rightShoulderIndex = -1;
            var leftHipIndex = -1;
            var rightHipIndex = -1;

            if (pose.LeftShoulder.HasValue)
            {
                DrawLine(vh, color32, rect, chest, pose.LeftShoulder.Value, halfStrokeWidth);
                leftShoulderIndex = DrawKnob(vh, color32, rect, pose.LeftShoulder.Value, halfStrokeWidth);
                if (pose.LeftElbow.HasValue)
                {
                    DrawLine(vh, color32, rect, pose.LeftShoulder.Value, pose.LeftElbow.Value, halfStrokeWidth);
                    DrawKnob(vh, color32, rect, pose.LeftElbow.Value, halfStrokeWidth);
                    if (pose.LeftWrist.HasValue)
                    {
                        DrawLine(vh, color32, rect, pose.LeftElbow.Value, pose.LeftWrist.Value, halfStrokeWidth);
                        DrawKnob(vh, color32, rect, pose.LeftWrist.Value, halfStrokeWidth);
                    }
                }

                if (pose.LeftHand.HasValue)
                {
                    DrawKnob(vh, color32, rect, pose.LeftHand.Value, halfStrokeWidth);
                }

                if (pose.LeftHip.HasValue)
                {
                    DrawLine(vh, color32, rect, pose.LeftShoulder.Value, pose.LeftHip.Value, halfStrokeWidth);
                    leftHipIndex = DrawKnob(vh, color32, rect, pose.LeftHip.Value, halfStrokeWidth);
                }
            }

            if (pose.RightShoulder.HasValue)
            {
                DrawLine(vh, color32, rect, chest, pose.RightShoulder.Value, halfStrokeWidth);
                rightShoulderIndex = DrawKnob(vh, color32, rect, pose.RightShoulder.Value, halfStrokeWidth);
                if (pose.RightElbow.HasValue)
                {
                    DrawLine(vh, color32, rect, pose.RightShoulder.Value, pose.RightElbow.Value, halfStrokeWidth);
                    DrawKnob(vh, color32, rect, pose.RightElbow.Value, halfStrokeWidth);
                    if (pose.RightWrist.HasValue)
                    {
                        DrawLine(vh, color32, rect, pose.RightElbow.Value, pose.RightWrist.Value, halfStrokeWidth);
                        DrawKnob(vh, color32, rect, pose.RightWrist.Value, halfStrokeWidth);
                    }
                }

                if (pose.RightHand.HasValue)
                {
                    DrawKnob(vh, color32, rect, pose.RightHand.Value, halfStrokeWidth);
                }

                if (pose.RightHip.HasValue)
                {
                    DrawLine(vh, color32, rect, pose.RightShoulder.Value, pose.RightHip.Value, halfStrokeWidth);
                    rightHipIndex = DrawKnob(vh, color32, rect, pose.RightHip.Value, halfStrokeWidth);
                }
            }

            // Now try to fill the area, if possible.
            SafeAddTriangle(vh, chestIndex, rightShoulderIndex, rightHipIndex);
            SafeAddTriangle(vh, chestIndex, rightHipIndex, leftHipIndex);
            SafeAddTriangle(vh, chestIndex, leftHipIndex, leftShoulderIndex);
            SafeAddTriangle(vh, chestIndex, leftShoulderIndex, rightShoulderIndex);
        }

        private static void DrawLine(VertexHelper vh, Color32 color32, Rect bounds, Vector2 from, Vector2 to, float halfStrokeWidth)
        {
            from = from * bounds.height + bounds.position;
            to = to * bounds.height + bounds.position;
            // Now compute the normal so that we know the points.
            var diff = to - from;
            // If the diff is too small, skip it.
            if (diff.sqrMagnitude < 0.1f) return;
            var normal = new Vector2(-diff.y, diff.x).normalized * halfStrokeWidth;
            var fromLeft = from + normal;
            var fromRight = from - normal;
            var toRight = to - normal;
            var toLeft = to + normal;
            var n = vh.currentVertCount;
            vh.AddVert(fromRight, color32, Vector4.zero);
            vh.AddVert(fromLeft, color32, Vector4.zero);
            vh.AddVert(toLeft, color32, Vector4.zero);
            vh.AddVert(toRight, color32, Vector4.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }

        private const float Sqrt2 = 0.7071067812f;

        private static readonly Vector2[] OctagonalVertices =
            {new(0, -1), new(-Sqrt2, -Sqrt2), new(-1, 0), new(-Sqrt2, Sqrt2), new(0, 1), new(Sqrt2, Sqrt2), new(1, 0), new(Sqrt2, -Sqrt2)};

        private static int DrawKnob(VertexHelper vh, Color32 color32, Rect bounds, Vector2 center, float radius)
        {
            center = center * bounds.height + bounds.position;
            var n = vh.currentVertCount;
            vh.AddVert(center, color32, Vector4.zero);
            // We are going to draw an octagon.
            foreach (var vertex in OctagonalVertices)
            {
                vh.AddVert(center + vertex * radius, color32, Vector4.zero);
            }

            // We are going to draw a triangle for each triplet of vertices.
            for (var i = 0; i < 8; ++i)
            {
                vh.AddTriangle(n, n + i + 1, n + 1 + (i + 1) % 8);
            }

            return n;
        }

        private static void SafeAddTriangle(VertexHelper vh, int a, int b, int c)
        {
            if (a == -1 || b == -1 || c == -1) return;
            vh.AddTriangle(a, b, c);
        }

    }
}
