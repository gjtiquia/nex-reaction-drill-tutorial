#nullable enable

using System;
using Cysharp.Threading.Tasks.Linq;
using UnityEngine;

namespace Nex.Essentials
{
    public class PoseNodeFollower : MonoBehaviour
    {
        [SerializeField] private PlayAreaController playAreaController = null!;
        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;
        [SerializeField] private BodyPoseController.PoseFlavor flavor = BodyPoseController.PoseFlavor.Smoothed;

        [SerializeField] private Vector2 playAreaPivot = new(0.5f, 0.5f);
        [SerializeField] private float scale = 1f;
        [SerializeField] private Vector2 offset = Vector2.zero;

        // There are two modes.
        private enum PoseNodeMode
        {
            Base = 0,
            Extension = 1,
        }

        [SerializeField] private PoseNodeMode mode = PoseNodeMode.Base;
        [SerializeField] private SimplePose.NodeIndex baseNode;

        [SerializeField, HideInInspector] private SimplePose.NodeIndex extensionNode;

        [SerializeField, HideInInspector] private float weight;

        [SerializeField]
        [HideInInspector]
        [Tooltip("If true, fallback to a value when the selected node(s) is not detected. " +
                 "If false, do nothing when not detected.")]
        public bool enableFallback;

        [SerializeField]
        [HideInInspector]
        [Tooltip("Fallback position if pose node is not detected. Only applicable when Enable Fallback is true.")]
        public Vector2 fallbackValue;

        private Rect playArea;
        private IDisposable? playAreaSubscription;

        private void OnEnable()
        {
            playAreaSubscription?.Dispose();
            playAreaSubscription = playAreaController.GetPlayAreaStream().Subscribe(UpdatePlayArea);
        }

        private void OnDisable()
        {
            playAreaSubscription?.Dispose();
            playAreaSubscription = null;
        }

        private void UpdatePlayArea(Rect area)
        {
            playArea = area;
        }

        private void Update()
        {
            var bodyPose = bodyPoseController.GetBodyPose(poseIndex, flavor);
            if (!bodyPose.HasValue) return;

            var basePoseNode = bodyPose.Value[baseNode];
            Vector2 nodePosition;
            switch (mode)
            {
                case PoseNodeMode.Base:
                    if (!basePoseNode.HasValue && !enableFallback) return;

                    nodePosition = basePoseNode ?? fallbackValue;
                    break;
                case PoseNodeMode.Extension:
                {
                    var extensionPoseNode = bodyPose.Value[extensionNode];

                    if (!enableFallback && (!basePoseNode.HasValue || !extensionPoseNode.HasValue)) return;

                    nodePosition = basePoseNode.HasValue && extensionPoseNode.HasValue
                        ? Vector2.LerpUnclamped(basePoseNode.Value, extensionPoseNode.Value, weight)
                        : fallbackValue;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Now compute where the node position should be.
            var playAreaSize = playArea.size;
            var anchor = playArea.position + playAreaSize * playAreaPivot;
            var playAreaPosition = (nodePosition - anchor) / playAreaSize;
            playAreaPosition.x *= playAreaController.GetAspectRatio();
            transform.localPosition = playAreaPosition * scale + offset;
        }

        public enum Preset
        {
            LeftHand,
            RightHand,
        }

        private void SetPresetWeight(SimplePose.NodeIndex baseIndex, SimplePose.NodeIndex extensionIndex,
            float presetWeight)
        {
            mode = PoseNodeMode.Extension;
            baseNode = baseIndex;
            extensionNode = extensionIndex;
            weight = presetWeight;
        }

        public void SetPreset(Preset preset)
        {
            switch (preset)
            {
                case Preset.LeftHand:
                    SetPresetWeight(SimplePose.NodeIndex.LeftElbow, SimplePose.NodeIndex.LeftWrist, 1.3f);
                    break;
                case Preset.RightHand:
                    SetPresetWeight(SimplePose.NodeIndex.RightElbow, SimplePose.NodeIndex.RightWrist, 1.3f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(preset), preset, null);
            }
        }
    }
}
