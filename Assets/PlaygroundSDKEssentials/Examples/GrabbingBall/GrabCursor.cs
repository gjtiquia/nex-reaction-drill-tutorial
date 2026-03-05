#nullable enable

using System;
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks;
using Nex.Essentials;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PlaygroundSDKEssentials.Examples.GrabbingBall
{
    public class GrabCursor : UIBehaviour
    {
        [SerializeField] private CursorProducer cursorProducer = null!;
        [SerializeField] private GrabbingBallSceneManager sceneManager = null!;
        [SerializeField] private OpenClosedSignalProducer openClosedSignalProducer = null!;
        [SerializeField] private PinchSignalProducer pinchSignalProducer = null!;
        [SerializeField] private Graphic graphic = null!;
        private float scale = 1080f;
        public event Action<bool>? OnGrabStateChanged;

        public RectTransform RectTransform { get; set; } = null!;

        // Debug
        [Tooltip("(Optional) The text to display the debug information")]
        public TMP_Text debugText = null!;

        protected override void Awake()
        {
            base.Awake();
            RectTransform = (RectTransform)transform;
        }

        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();
            scale = Camera.main!.pixelHeight;
            cursorProducer.CursorPositionStream.Subscribe(HandleCursorPosition, this.GetCancellationTokenOnDestroy());
            sceneManager.onDetectionModeChanged.AddListener(HandleDetectionModeChanged);

            // Subscribe to both signal producers
            openClosedSignalProducer.onStateChange.AddListener(HandleHandPoseGrab);
            pinchSignalProducer.onStateChange.AddListener(HandlePinchGrab);
        }

        private void HandleDetectionModeChanged(GrabbingBallSceneManager.DetectionMode mode)
        {
            switch (mode)
            {
                case GrabbingBallSceneManager.DetectionMode.HandPose2D:
                    openClosedSignalProducer.detectionMethod = OpenClosedSignalProducer.DetectionMethod.HandPose;
                    break;
                case GrabbingBallSceneManager.DetectionMode.OpenClosedHand:
                    openClosedSignalProducer.detectionMethod = OpenClosedSignalProducer.DetectionMethod.GestureDetection;
                    break;
            }
        }

        private void HandleHandPoseGrab(bool isGrabbing)
        {
            if (sceneManager.CurrentDetectionMode == GrabbingBallSceneManager.DetectionMode.HandPose2D ||
                sceneManager.CurrentDetectionMode == GrabbingBallSceneManager.DetectionMode.OpenClosedHand)
            {
                HandleGrab(isGrabbing);
            }
        }

        private void HandlePinchGrab(bool isGrabbing)
        {
            if (sceneManager.CurrentDetectionMode == GrabbingBallSceneManager.DetectionMode.PinchSignal)
            {
                HandleGrab(isGrabbing);
            }
        }

        private void HandleGrab(bool isGrabbing)
        {
            var currentColor = graphic.color;
            if (isGrabbing)
            {
                OnGrabStateChanged?.Invoke(true);
                currentColor.a = 1f;
            }
            else
            {
                OnGrabStateChanged?.Invoke(false);
                currentColor.a = 0.5f;
            }

            graphic.color = currentColor;

            if (debugText != null)
            {
                debugText.text = isGrabbing ? "Grabbing" : "Released";
            }
        }

        private void HandleCursorPosition(Vector2? position)
        {
            if (position == null)
            {
                return;
            }

            // Update rect transform position
            RectTransform.anchoredPosition = position.Value * scale;
        }
    }
}