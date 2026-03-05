#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials
{
    public abstract class SignalProducer : MonoBehaviour
    {
        [Header("Body Pose")]
        [SerializeField] protected BodyPoseController bodyPoseController = null!;

        [SerializeField] protected int poseIndex;
        [SerializeField] protected BodyPoseController.PoseFlavor poseFlavor = BodyPoseController.PoseFlavor.Raw;

        private readonly AsyncReactiveProperty<float> signalStream = new(0f);
        public IUniTaskAsyncEnumerable<float> SignalStream => signalStream.AsUniTaskAsyncEnumerable();

        public float Signal => signalStream.Value;

        private void Update()
        {
            if (!bodyPoseController.TryGetBodyPose(poseIndex, poseFlavor, out var bodyPose)) return;

            var signal = ComputeSignal(bodyPose);
            signalStream.Value = signal;
        }

        protected abstract float ComputeSignal(SimplePose bodyPose);
    }
}
