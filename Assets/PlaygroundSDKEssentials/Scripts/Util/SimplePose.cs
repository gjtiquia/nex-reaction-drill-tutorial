#nullable enable

using System;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    public readonly struct SimplePose
    {
        public enum NodeIndex
        {
            Nose = 0,
            Chest = 1,
            LeftShoulder = 2,
            LeftElbow = 3,
            LeftWrist = 4,
            RightShoulder = 5,
            RightElbow = 6,
            RightWrist = 7,
            LeftHip = 8,
            LeftKnee = 9,
            LeftAnkle = 10,
            RightHip = 11,
            RightKnee = 12,
            RightAnkle = 13,
            LeftEye = 14,
            RightEye = 15,
            LeftEar = 16,
            RightEar = 17,
            LeftHand = 18,
            RightHand = 19,
            CenterHip = 20,
            CenterAnkle = 21,
        }

        private const int numExtendedNodes = 22;

        public enum DistanceUnit
        {
            AspectNormalized = 0,
            Inch = 1,
            Meter = 2,
        }

        public readonly float pixelsPerInch;

        public readonly Vector2?[] nodes;
        public Vector2? Nose => nodes[(int)BodyPose.NodeIndex.Nose];
        public Vector2? Chest => nodes[(int)BodyPose.NodeIndex.Chest];
        public Vector2? LeftShoulder => nodes[(int)BodyPose.NodeIndex.LeftShoulder];
        public Vector2? LeftElbow => nodes[(int)BodyPose.NodeIndex.LeftElbow];
        public Vector2? LeftWrist => nodes[(int)BodyPose.NodeIndex.LeftWrist];
        public Vector2? RightShoulder => nodes[(int)BodyPose.NodeIndex.RightShoulder];
        public Vector2? RightElbow => nodes[(int)BodyPose.NodeIndex.RightElbow];
        public Vector2? RightWrist => nodes[(int)BodyPose.NodeIndex.RightWrist];
        public Vector2? LeftHip => nodes[(int)BodyPose.NodeIndex.LeftHip];
        public Vector2? LeftKnee => nodes[(int)BodyPose.NodeIndex.LeftKnee];
        public Vector2? LeftAnkle => nodes[(int)BodyPose.NodeIndex.LeftAnkle];
        public Vector2? RightHip => nodes[(int)BodyPose.NodeIndex.RightHip];
        public Vector2? RightKnee => nodes[(int)BodyPose.NodeIndex.RightKnee];
        public Vector2? RightAnkle => nodes[(int)BodyPose.NodeIndex.RightAnkle];
        public Vector2? LeftEye => nodes[(int)BodyPose.NodeIndex.LeftEye];
        public Vector2? RightEye => nodes[(int)BodyPose.NodeIndex.RightEye];
        public Vector2? LeftEar => nodes[(int)BodyPose.NodeIndex.LeftEar];
        public Vector2? RightEar => nodes[(int)BodyPose.NodeIndex.RightEar];
        public Vector2? LeftHand => nodes[(int)NodeIndex.LeftHand];
        public Vector2? RightHand => nodes[(int)NodeIndex.RightHand];
        public Vector2? CenterAnkle => nodes[(int)NodeIndex.CenterAnkle];

        public Vector2? this[NodeIndex index] => nodes?[(int)index];

        public bool TryGetNode(NodeIndex index, out Vector2 node)
        {
            var maybeNode = nodes[(int)index];
            if (maybeNode == null)
            {
                node = default;
                return false;
            }

            node = maybeNode.Value;
            return true;
        }

        public Vector2? ComputeExtendedNode(NodeIndex baseIndex, NodeIndex nodeIndex, float ratio)
        {
            var baseNode = nodes[(int)baseIndex];
            if (baseNode == null) return null;
            var node = nodes[(int)nodeIndex];
            if (node == null) return null;
            return Vector2.LerpUnclamped(baseNode.Value, node.Value, ratio);
        }

        public float? ComputeDistance(NodeIndex nodeIndex, NodeIndex baseNodeIndex = NodeIndex.Chest,
            DistanceUnit unit = DistanceUnit.Inch)
        {
            var node = nodes[(int)nodeIndex];
            if (node == null) return null;
            var baseNode = nodes[(int)baseNodeIndex];
            if (baseNode == null) return null;
            var distance = Vector2.Distance(node.Value, baseNode.Value);

            const float metersPerInch = 0.0254f;
            var scale = unit switch
            {
                DistanceUnit.AspectNormalized => 1f,
                DistanceUnit.Inch => 1f / pixelsPerInch,
                DistanceUnit.Meter => metersPerInch / pixelsPerInch,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
            };

            return distance * scale;
        }

        private SimplePose(Vector2?[] nodes, float ppi)
        {
            this.nodes = nodes;
            pixelsPerInch = ppi;
            FillExtendedNodes(nodes);
        }

        private static Vector2? ComputeExtendedNode(Vector2?[] nodes, NodeIndex baseIndex, NodeIndex nodeIndex,
            float ratio)
        {
            var baseNode = nodes[(int)baseIndex];
            if (baseNode == null) return null;
            var node = nodes[(int)nodeIndex];
            if (node == null) return null;
            return Vector2.LerpUnclamped(baseNode.Value, node.Value, ratio);
        }

        private static void FillExtendedNodes(Vector2?[] nodes)
        {
            nodes[(int)NodeIndex.LeftHand] = ComputeExtendedNode(nodes, NodeIndex.LeftElbow, NodeIndex.LeftWrist, 1.3f);
            nodes[(int)NodeIndex.RightHand] = ComputeExtendedNode(nodes, NodeIndex.RightElbow,
                NodeIndex.RightWrist, 1.3f);
            nodes[(int)NodeIndex.CenterHip] = ComputeExtendedNode(nodes, NodeIndex.LeftHip, NodeIndex.RightHip, 0.5f);
            nodes[(int)NodeIndex.CenterAnkle] = ComputeExtendedNode(nodes, NodeIndex.LeftAnkle,
                NodeIndex.RightAnkle, 0.5f);
        }

        public static SimplePose CreateWithBodyPose(BodyPose bodyPose, float? ppi = null)
        {
            var resolvedPpi = ppi ?? bodyPose.pixelsPerInch;
            var nodes = new Vector2?[numExtendedNodes];

            for (var i = 0; i < bodyPose.nodes.Length; ++i)
            {
                nodes[i] = bodyPose.nodes[i].isDetected ? bodyPose.nodes[i].ToVector2() : null;
            }

            return new SimplePose(nodes, resolvedPpi);
        }
    }
}
