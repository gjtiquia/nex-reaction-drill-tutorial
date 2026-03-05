#nullable enable

using System.Collections.Generic;
using UnityEngine;

namespace Nex.Essentials
{
    public class GroupDirectionalSignalProducer : SignalProducer
    {
        [Header("Directional Signal Configuration")]
        [SerializeField] private List<SimplePose.NodeIndex> targetNodes = new();

        [SerializeField] private Vector2 detectionAxis;
        [SerializeField] [HideInInspector] private bool useReferenceNode;
        [SerializeField] [HideInInspector] private SimplePose.NodeIndex referenceNode;

        [SerializeField, HideInInspector] private bool scaleWithPpi;
        [SerializeField, HideInInspector] private BodyPoseController.PoseFlavor ppiFlavor;

        protected override float ComputeSignal(SimplePose bodyPose)
        {
            var sumDifference = Vector2.zero;
            var count = 0;

            foreach (var nodeIndex in targetNodes)
            {
                if (!bodyPose.TryGetNode(nodeIndex, out var target)) continue;

                var reference = useReferenceNode ? bodyPose[referenceNode].GetValueOrDefault() : Vector2.zero;

                count++;
                var difference = target - reference;
                if (!scaleWithPpi)
                {
                    sumDifference += difference;
                    continue;
                }

                var ppi = GetPpi(bodyPose);
                difference /= ppi;
                sumDifference += difference;
            }

            if (count == 0)
            {
                return 0;
            }

            return Vector2.Dot(sumDifference / count, detectionAxis);
        }

        private float GetPpi(SimplePose bodyPose)
        {
            var ppi = bodyPose.pixelsPerInch;

            do
            {
                if (poseFlavor == ppiFlavor) continue;
                if (!bodyPoseController.TryGetBodyPose(poseIndex, ppiFlavor, out var ppiPose)) continue;
                ppi = ppiPose.pixelsPerInch;
            } while (false);

            return ppi;
        }
    }
}
