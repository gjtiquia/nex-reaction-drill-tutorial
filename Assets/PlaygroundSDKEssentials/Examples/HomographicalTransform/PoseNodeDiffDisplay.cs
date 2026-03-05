#nullable enable

using Nex.Essentials;
using TMPro;
using UnityEngine;

namespace PlaygroundSDKEssentials.Examples.HomographicalTransform
{
    public class PoseNodeDiffDisplay : MonoBehaviour
    {
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;

        [SerializeField] private TMP_Text smoothedLabel = null!;
        [SerializeField] private TMP_Text homographicalLabel = null!;

        private float? ComputeDiff(BodyPoseController.PoseFlavor flavor)
        {
            var bodyPose = bodyPoseController.GetBodyPose(poseIndex, flavor);
            if (bodyPose == null) return null;
            var noseNode = bodyPose.Value.Nose;
            var chestNode = bodyPose.Value.Chest;
            if (!noseNode.HasValue || !chestNode.HasValue) return null;
            var diff = noseNode.Value.x - chestNode.Value.x;
            var ppi = bodyPose.Value.pixelsPerInch;
            return diff / ppi;
        }

        private void Update()
        {
            var smoothedDiff = ComputeDiff(BodyPoseController.PoseFlavor.Smoothed);
            var homographicalDiff = ComputeDiff(BodyPoseController.PoseFlavor.SmoothHomographical);
            smoothedLabel.gameObject.SetActive(smoothedDiff != null);
            smoothedLabel.text = $"{smoothedDiff ?? 0f:F4}";
            homographicalLabel.gameObject.SetActive(homographicalDiff != null);
            homographicalLabel.text = $"{homographicalDiff ?? 0f:F4}";
        }
    }
}
