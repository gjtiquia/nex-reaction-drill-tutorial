#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public class DirectionalSignalProducer : SignalProducer
    {
        [Header("Directional Signal Configuration")]
        [SerializeField] private SimplePose.NodeIndex targetNode;

        [SerializeField] private Vector2 detectionAxis;
        [SerializeField] [HideInInspector] private bool useReferenceNode;
        [SerializeField] [HideInInspector] private SimplePose.NodeIndex referenceNode;

        [SerializeField, HideInInspector] private bool scaleWithPpi;
        [SerializeField, HideInInspector] private BodyPoseController.PoseFlavor ppiFlavor;

        protected override float ComputeSignal(SimplePose bodyPose)
        {
            var target = bodyPose[targetNode].GetValueOrDefault();
            var reference = useReferenceNode ? bodyPose[referenceNode].GetValueOrDefault() : Vector2.zero;
            var difference = target - reference;

            if (!scaleWithPpi) return Vector2.Dot(difference, detectionAxis);

            var ppi = GetPpi(bodyPose);
            var scaledDifference = difference / ppi;
            return Vector2.Dot(scaledDifference, detectionAxis);
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
