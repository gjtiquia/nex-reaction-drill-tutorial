#nullable enable

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.PoseComparison
{
    public class Driver : MonoBehaviour
    {
        [SerializeField] private CoachPoseConfig[] poseConfigs = null!;
        [SerializeField] private TMP_Text[] scoreLabels = null!;

        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;
        [SerializeField] private BodyPoseController.PoseFlavor poseFlavor;

        private double[] scores = null!;

        private void Awake()
        {
            scores = new double[poseConfigs.Length];
        }

        private void Start()
        {
            bodyPoseController.GetBodyPoseStream(poseIndex, poseFlavor)
                .Subscribe(HandleBodyPose, this.GetCancellationTokenOnDestroy());
        }

        private void HandleBodyPose(SimplePose simplePose)
        {
            for (var i = 0; i < poseConfigs.Length; i++)
            {
                var score = new Essentials.PoseComparison().RegisterTarget(poseConfigs[i].nodePositions).SetCandidate(simplePose)
                    .OverallScore;
                scores[i] = score;
                scoreLabels[i].text = $"{score:0.00}";
            }
        }
    }

}
