using System;
using TMPro;
using UnityEngine;

namespace Nex.Essentials
{
    public class OnePlayerSetupError : MonoBehaviour
    {
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private OnePlayerSetupDetector detector;

        private void Update()
        {
            errorText.text = detector.error switch
            {
                OnePlayerSetupDetector.SetupError.None => "",
                OnePlayerSetupDetector.SetupError.PoseNotFound => "Where are you?",
                OnePlayerSetupDetector.SetupError.PoseNotCentered => "Stand in the middle",
                OnePlayerSetupDetector.SetupError.HandNotRaised => "Raise your hand",
                OnePlayerSetupDetector.SetupError.PoseTooClose => "Too close",
                OnePlayerSetupDetector.SetupError.PoseTooFar => "Too far",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
