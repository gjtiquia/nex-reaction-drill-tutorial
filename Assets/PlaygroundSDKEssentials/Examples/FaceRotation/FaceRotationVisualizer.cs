using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nex.Essentials.Examples.FaceLandmark
{
    public class FaceRotationVisualizer : MonoBehaviour
    {
        [SerializeField] FaceRotationController faceRotationController;

        public List<FaceRotationSettings> visualizationSettings;

        public FaceRotationController.FaceRotationControllerOption faceRotationControllerOption;

        [Serializable]
        public class FaceRotationSettings
        {
            [Tooltip("The player index for the face rotation")]
            public int playerIndex;

            [Tooltip("The visualizer for the face rotation, it will be rotated to the face rotation")]
            public Transform visualizer;

            [Tooltip("The particle system for the face rotation, it will be played when the face is detected")]
            public ParticleSystem particleSystem;

            [Tooltip("Warning label for debugging")]
            public TMP_Text warningLabel;
        }

        private void Update()
        {
            foreach (var visualizationSetting in visualizationSettings)
            {
                var rotation = faceRotationController.GetLatestRotation(visualizationSetting.playerIndex,
                    faceRotationControllerOption);

                if (rotation.HasValue)
                {
                    visualizationSetting.particleSystem.Play();
                    visualizationSetting.warningLabel.text = "";
                }
                else
                {
                    visualizationSetting.particleSystem.Stop();
                    visualizationSetting.warningLabel.text = "No Face Detected!";
                }

                if (rotation == null) return;

                visualizationSetting.visualizer.localRotation = rotation.Value;
            }
        }
    }
}
