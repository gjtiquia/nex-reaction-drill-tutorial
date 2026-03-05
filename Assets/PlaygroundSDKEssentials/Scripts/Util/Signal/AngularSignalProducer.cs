#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public class AngularSignalProducer : SignalProducer
    {
        [Header("Angular Signal Configuration")]
        [SerializeField] private SimplePose.NodeIndex targetNode;

        [SerializeField] private SimplePose.NodeIndex referenceNode;
        [SerializeField] private Vector2 referenceAxis;

        protected override float ComputeSignal(SimplePose bodyPose)
        {
            var target = bodyPose[targetNode].GetValueOrDefault();
            var reference = bodyPose[referenceNode].GetValueOrDefault();
            var difference = target - reference;

            // Calculate the angle between the difference vector and the reference vector
            return Vector2.SignedAngle(referenceAxis, difference);
        }
    }
}
