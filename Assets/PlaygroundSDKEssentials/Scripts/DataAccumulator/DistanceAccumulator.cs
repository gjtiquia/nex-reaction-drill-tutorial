#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public class DistanceAccumulator : MonoBehaviour
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;
        [SerializeField] private SimplePose.NodeIndex nodeIndex;
        [SerializeField] private BodyPoseController.PoseFlavor flavor = BodyPoseController.PoseFlavor.Raw;

        [SerializeField, Tooltip("Seconds of history to count")]
        private float movingWindowInSeconds = 1;

        public float DistanceTotal { get; private set; }

        public float DistanceX { get; private set; }

        public float DistanceY { get; private set; }

        private struct Displacement
        {
            public float Magnitude;
            public float XOnly;
            public float YOnly;

            public Displacement(float magnitude, float xOnly, float yOnly)
            {
                Magnitude = magnitude;
                XOnly = xOnly;
                YOnly = yOnly;
            }
        }

        private Deque<(Displacement value, float time)> history = null!;
        private Vector2? prevNode;
        private Displacement sum;

        private void Start()
        {
            history = new Deque<(Displacement value, float time)>();
        }

        private void Update()
        {
            CleanUpHistory();
            AddDataToHistory();
            UpdateEnergy();
        }

        [ContextMenu("Reset All History")]
        public void Reset()
        {
            history.Clear();
            sum = new Displacement(0, 0, 0);
        }

        private void CleanUpHistory()
        {
            var expiredTime = Time.time - movingWindowInSeconds;
            while (history.TryPeekFront(out var item) && item.time < expiredTime)
            {
                sum.Magnitude -= item.value.Magnitude;
                sum.XOnly -= Mathf.Abs(item.value.XOnly);
                sum.YOnly -= Mathf.Abs(item.value.YOnly);
                history.PopFront();
            }
        }

        private void AddDataToHistory()
        {
            // Get body pose
            var bodyPose = bodyPoseController.GetBodyPose(poseIndex, flavor);
            var node = bodyPose?[nodeIndex];
            if (!bodyPose.HasValue || !node.HasValue) return;
            var currNodeCoordinate = node.Value;

            if (prevNode == null)
            {
                prevNode = currNodeCoordinate;
                return;
            }

            // Add new data point
            var ppi = bodyPose.Value.pixelsPerInch;

            var diff = currNodeCoordinate - prevNode.Value;

            if (diff == Vector2.zero) return;

            var diffTotal = diff.magnitude / ppi;
            var diffInX = diff.x / ppi;
            var diffInY = diff.y / ppi;
            prevNode = currNodeCoordinate;

            sum.Magnitude += diffTotal;
            sum.XOnly += Mathf.Abs(diffInX);
            sum.YOnly += Mathf.Abs(diffInY);

            var displacement = new Displacement(diffTotal, diffInX, diffInY);
            history.PushBack((displacement, Time.time));
        }

        private void UpdateEnergy()
        {
            DistanceTotal = sum.Magnitude / movingWindowInSeconds;
            DistanceX = sum.XOnly / movingWindowInSeconds;
            DistanceY = sum.YOnly / movingWindowInSeconds;
        }
    }
}
