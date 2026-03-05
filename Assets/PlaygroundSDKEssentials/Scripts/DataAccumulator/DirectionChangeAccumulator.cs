#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public class DirectionChangeAccumulator : MonoBehaviour
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;
        [SerializeField] private SimplePose.NodeIndex nodeIndex;
        [SerializeField] private BodyPoseController.PoseFlavor flavor = BodyPoseController.PoseFlavor.Raw;

        [SerializeField, Tooltip("Seconds of history to count")]
        private float movingWindowInSeconds = 1;

        private History<Vector2> history = null!;
        private Vector2? prevNode;

        [Header("Reps")]
        [SerializeField, Tooltip("Seconds to back track to determine a new rep, must be smaller than maxAge")]
        private float repCalculationWindow = 0.2f;

        [SerializeField, Tooltip("Minimum distance in inches traveled to count as a new rep")]
        private float repDistanceThresholdInInches = 6f;

        private int xDirection;
        private int yDirection;
        private History<float> xDirectionHistory = null!;
        private History<float> yDirectionHistory = null!;

        public int CumulatedRepsX { get; private set; }
        public int CumulatedRepsY { get; private set; }

        public int RepsX => xDirectionHistory.Count;
        public int RepsY => yDirectionHistory.Count;

        private void Start()
        {
            history = new History<Vector2>(repCalculationWindow);
            xDirectionHistory = new History<float>(movingWindowInSeconds);
            yDirectionHistory = new History<float>(movingWindowInSeconds);
        }

        private void Update()
        {
            CleanUpHistory();
            var ppi = AddDataToHistory();
            if (ppi.HasValue) UpdateRepsCount(ppi.Value);
        }

        [ContextMenu("Reset All History")]
        public void Reset()
        {
            history.Clear();
            xDirectionHistory.Clear();
            yDirectionHistory.Clear();
            xDirection = 0;
            yDirection = 0;
            CumulatedRepsX = 0;
            CumulatedRepsY = 0;
        }

        private void CleanUpHistory()
        {
            var frameTime = Time.time;
            history.CleanUp(frameTime);
            xDirectionHistory.CleanUp(frameTime);
            yDirectionHistory.CleanUp(frameTime);
        }

        private float? AddDataToHistory()
        {
            // Get body pose
            var optionalBodyPose = bodyPoseController.GetBodyPose(poseIndex, flavor);

            if (!optionalBodyPose.HasValue) return null;
            var bodyPose = optionalBodyPose.Value;
            var ret = bodyPose.pixelsPerInch;

            do
            {
                var node = optionalBodyPose.Value[nodeIndex];
                if (!node.HasValue) continue;
                var currNodeCoordinate = node.Value;
                if (history.LatestValue == currNodeCoordinate) continue;
                history.Add(currNodeCoordinate, Time.time);
            } while (false);

            return ret;
        }

        private void UpdateRepsCount(float ppi)
        {
            // Find the total displacement in the past repCalculationWindow
            // If the displacement is different from the current direction, increase the rep count
            var xDisplacement = (history.LatestValue.x - history.EarliestValue.x) / ppi;
            var yDisplacement = (history.LatestValue.y - history.EarliestValue.y) / ppi;

            // If there is a movement, check if there is a change in direction
            // If there is a change in direction, increase the reps count
            if (Mathf.Abs(xDisplacement) > repDistanceThresholdInInches)
            {
                if (xDirection == 0)
                {
                    xDirection = xDisplacement > 0 ? 1 : -1;
                    CumulatedRepsX++;
                    xDirectionHistory.Add(1, Time.time);
                }

                if (xDisplacement * xDirection < 0)
                {
                    xDirection = -xDirection;
                    CumulatedRepsX++;
                    xDirectionHistory.Add(1, Time.time);
                }
            }

            if (Mathf.Abs(yDisplacement) > repDistanceThresholdInInches)
            {
                if (yDirection == 0)
                {
                    yDirection = yDisplacement > 0 ? 1 : -1;
                    CumulatedRepsY++;
                    yDirectionHistory.Add(1, Time.time);
                }

                if (yDisplacement * yDirection < 0)
                {
                    yDirection = -yDirection;
                    CumulatedRepsY++;
                    yDirectionHistory.Add(1, Time.time);
                }
            }
        }
    }
}
