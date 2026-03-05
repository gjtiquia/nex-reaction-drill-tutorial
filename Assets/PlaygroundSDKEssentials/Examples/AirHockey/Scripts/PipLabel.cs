using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.AirHockey
{
    public class PipLabel : MonoBehaviour
    {
        [SerializeField] private int playerIndex;
        [SerializeField] private BodyPoseController bodyPoseController;

        [SerializeField] private TMP_Text label;
        [SerializeField] private float minPpi;
        [SerializeField] private float maxPpi;

        private void Update()
        {
            var bodyPose = bodyPoseController.GetBodyPose(playerIndex, BodyPoseController.PoseFlavor.Smoothed);

            if (bodyPose == null)
            {
                label.text = "You there?";
                return;
            }

            var ppi = bodyPose.Value.pixelsPerInch;

            label.text = ppi switch
            {
                _ when ppi > maxPpi => "Too Close!",
                _ when ppi < minPpi => "Too Far!",
                _ => ""
            };
        }
    }
}
