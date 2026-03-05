#nullable enable

using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nex.Essentials
{
    public class CursorProducer : MonoBehaviour
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;

        [SerializeField] private int poseIndex;
        [SerializeField] private BodyPoseController.PoseFlavor flavor = BodyPoseController.PoseFlavor.Smoothed;

        [SerializeField, Tooltip("The node controlling the cursor")]
        private SimplePose.NodeIndex nodeIndex;

        [Header("Virtual Cursor Canvas Definition")]
        [SerializeField] private SimplePose.NodeIndex centerReferenceNode = SimplePose.NodeIndex.Chest;

        [SerializeField, Tooltip("Measurements in inches, relative to reference node, which is by default chest.")]
        private Vector2 centerOffset = new(8, -5);

        [SerializeField, Tooltip("Measurement in inches.")]
        private float canvasHeight = 17;

        public BodyPoseController BodyPoseController
        {
            get => bodyPoseController;
            set => bodyPoseController = value;
        }

        public int PoseIndex
        {
            get => poseIndex;
            set => poseIndex = value;
        }

        public BodyPoseController.PoseFlavor Flavor
        {
            get => flavor;
            set => flavor = value;
        }

        public SimplePose.NodeIndex NodeIndex
        {
            get => nodeIndex;
            set => nodeIndex = value;
        }

        public SimplePose.NodeIndex CenterReferenceNode
        {
            get => centerReferenceNode;
            set => centerReferenceNode = value;
        }

        public Vector2 CenterOffset
        {
            get => centerOffset;
            set => centerOffset = value;
        }

        public float CanvasHeight
        {
            get => canvasHeight;
            set => canvasHeight = value;
        }

        // The output position is relative to the center.
        // The visible x range is [-0.5 * aspectRatio, 0.5 * aspectRatio].
        // The visible y range is [-0.5, 0.5].

        private readonly AsyncReactiveProperty<Vector2?> cursorPosition = new(null);

        /// <summary>
        /// Gets the current cursor output value.
        /// </summary>
        public Vector2? CursorPosition => cursorPosition.Value;

        /// <summary>
        /// Gets a stream of cursor output values that updates whenever the input changes.
        /// </summary>
        public IUniTaskAsyncEnumerable<Vector2?> CursorPositionStream => cursorPosition;

        private void Update()
        {
            cursorPosition.Value = ComputeCursorPosition();
        }

        private Vector2? ComputeCursorPosition()
        {
            var maybeBodyPose = bodyPoseController.GetBodyPose(poseIndex, flavor);
            if (!maybeBodyPose.HasValue) return null;
            var bodyPose = maybeBodyPose.Value;

            var centerReference = bodyPose[centerReferenceNode];
            var nodePosition = bodyPose[nodeIndex];

            var diff = (nodePosition - centerReference) / bodyPose.pixelsPerInch;

            return (diff - centerOffset) / canvasHeight;
        }
    }
}
