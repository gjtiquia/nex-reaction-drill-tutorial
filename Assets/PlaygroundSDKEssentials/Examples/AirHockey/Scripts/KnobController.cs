#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials.Examples.AirHockey
{
    public class KnobController : MonoBehaviour
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int playerIndex;
        [SerializeField] private SimplePose.NodeIndex handIndex = SimplePose.NodeIndex.RightHand;
        [SerializeField] private Transform player = null!;
        [SerializeField] private Vector3 basePosition;
        [SerializeField] private float xScale = 1;
        [SerializeField] private float xBorder = 19.5f;
        [SerializeField] private float zScale = 1;
        [SerializeField] private float zMin;
        [SerializeField] private float zMax = 40;
        [SerializeField] private float maxSpeed = 1;

        private bool gameStarted;
        private float zOffset;
        private new Rigidbody rigidbody = null!;
        private Vector3 targetPosition;

        private void Start()
        {
            targetPosition = basePosition;
            UpdateZOffset();

            rigidbody = GetComponent<Rigidbody>();
        }

        private void UpdateZOffset()
        {
            zOffset = (basePosition.z, handIndex) switch
            {
                (var z and < 0, SimplePose.NodeIndex.LeftHand) => -z,
                (< 0, SimplePose.NodeIndex.RightHand) => 0,
                (_, SimplePose.NodeIndex.LeftHand) => 0,
                (var z, SimplePose.NodeIndex.RightHand) => -z,
                _ => 0
            };
        }

        private void HandlePose(SimplePose pose)
        {
            if (!gameStarted) return;

            if (!pose.TryGetNode(SimplePose.NodeIndex.Chest, out var chestPos) ||
                !pose.TryGetNode(handIndex, out var handPos))
                return;

            var diff = (handPos - chestPos) / pose.pixelsPerInch;
            var x = Mathf.Clamp(diff.y * xScale, -xBorder, xBorder);
            var z = Mathf.Clamp(diff.x * zScale + zOffset, zMin, zMax);
            targetPosition = new Vector3(basePosition.x + x, basePosition.y, basePosition.z + z);
        }

        private void FixedUpdate()
        {
            var diff = targetPosition - player.localPosition;

#if UNITY_6000_0_OR_NEWER
            rigidbody.linearVelocity = Vector3.ClampMagnitude(diff / Time.fixedDeltaTime, maxSpeed);
#else
            rigidbody.velocity = Vector3.ClampMagnitude(diff / Time.fixedDeltaTime, maxSpeed);
#endif
        }

        public void StartKnobTracking(int poseIndex, SimplePose.NodeIndex hand, Vector3 basePos, float zPosMin,
            float zPosMax)
        {
            playerIndex = poseIndex;
            handIndex = hand;
            basePosition = basePos;
            zMin = zPosMin;
            zMax = zPosMax;

            UpdateZOffset();

            gameStarted = true;

            bodyPoseController.GetBodyPoseStream(playerIndex, BodyPoseController.PoseFlavor.Smoothed)
                .Subscribe(HandlePose, this.GetCancellationTokenOnDestroy());
        }
    }
}
