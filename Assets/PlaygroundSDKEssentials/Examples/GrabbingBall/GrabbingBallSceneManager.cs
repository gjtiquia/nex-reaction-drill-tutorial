#nullable enable

using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PlaygroundSDKEssentials.Examples.GrabbingBall
{
    public class GrabbingBallSceneManager : MonoBehaviour
    {
        public UnityEvent<DetectionMode> onDetectionModeChanged = new();

        public enum DetectionMode
        {
            HandPose2D,
            PinchSignal,
            OpenClosedHand,
        }

        [SerializeField] private DetectionMode detectionMode;
        [SerializeField] private TMP_Text modeDisplayText = null!;
        [SerializeField] private GameObject pinchDebugLabelGroup = null!;
        [SerializeField] private GameObject openClosedDebugLabelGroup = null!;

        public DetectionMode CurrentDetectionMode
        {
            get => detectionMode;
            set => detectionMode = value;
        }

        private void Awake()
        {
            modeDisplayText.text = CurrentDetectionMode.ToString();
        }

        private void Update()
        {
            var modeCount = Enum.GetValues(typeof(DetectionMode)).Length;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                detectionMode = (DetectionMode)(((int)detectionMode - 1 + modeCount) % modeCount);
                onDetectionModeChanged.Invoke(detectionMode);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                detectionMode = (DetectionMode)(((int)detectionMode + 1) % modeCount);
                onDetectionModeChanged.Invoke(detectionMode);
            }

            pinchDebugLabelGroup.SetActive(detectionMode == DetectionMode.PinchSignal);
            openClosedDebugLabelGroup.SetActive(detectionMode == DetectionMode.OpenClosedHand);

            modeDisplayText.text = CurrentDetectionMode.ToString();
            if (CurrentDetectionMode != DetectionMode.HandPose2D)
            {
                modeDisplayText.text += " (MDK 3.1+)";
            }
        }
    }
}