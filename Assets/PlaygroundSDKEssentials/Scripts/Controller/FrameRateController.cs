using UnityEngine;

namespace Nex.Essentials
{
    public class FrameRateController : MonoBehaviour
    {
        [Tooltip("The required target frame rate should be at least 60.")]
        public int frameRate = 60;

        private void Start()
        {
            // Target 60 FPS according to the guideline
            // https://developer.nex.inc/docs/playground-adaptation-guide/guidelines/#graphics--performance
            Application.targetFrameRate = frameRate;
        }
    }
}
