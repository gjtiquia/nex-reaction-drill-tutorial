#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using Nex.Essentials;
using TMPro;
using UnityEngine;

namespace PlaygroundSDKEssentials.Examples.HomographicalTransform
{
    public class PoseNodeCoordinatesDisplay : MonoBehaviour
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;
        [SerializeField] private SimplePose.NodeIndex nodeIndex;

        [SerializeField] private TMP_Text smoothedLabel = null!;
        [SerializeField] private TMP_Text homographicalLabel = null!;

        private void Start()
        {
            bodyPoseController.GetBodyPoseStream(poseIndex, BodyPoseController.PoseFlavor.Smoothed)
                .Subscribe(HandleSmoothedBodyPoseDetection, this.GetCancellationTokenOnDestroy());
            bodyPoseController.GetBodyPoseStream(poseIndex, BodyPoseController.PoseFlavor.SmoothHomographical)
                .Subscribe(HandleHomographicalBodyPoseDetection, this.GetCancellationTokenOnDestroy());
        }

        private void HandleSmoothedBodyPoseDetection(SimplePose bodyPose)
        {
            var smoothed = bodyPose[nodeIndex];
            HandleDetection(smoothed, smoothedLabel);
        }

        private void HandleHomographicalBodyPoseDetection(SimplePose bodyPoses)
        {
            var homographical = bodyPoses[nodeIndex];
            HandleDetection(homographical, homographicalLabel);
        }

        private void HandleDetection(Vector2? pos, TMP_Text label)
        {
            if (pos != null)
            {
                label.gameObject.SetActive(true);
                label.text = $"({pos.Value.x:F4}, {pos.Value.y:F4})";
            }
            else
            {
                label.gameObject.SetActive(false);
            }
        }
    }
}
