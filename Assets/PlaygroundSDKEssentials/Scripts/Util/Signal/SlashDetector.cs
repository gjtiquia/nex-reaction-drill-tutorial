#nullable enable

using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using NodeIndex = Nex.Essentials.SimplePose.NodeIndex;
using System.Threading;

namespace Nex.Essentials
{
    public class SlashDetector : MonoBehaviour
    {
        [Tooltip("Player index (0 for Player 1, 1 for Player 2, etc.)")]
        [SerializeField]
        private int playerIndex;

        [SerializeField] private NodeIndex nodeIndex;
        [SerializeField] private BodyPoseController bodyPoseController = null!;

        [Tooltip("Minimum speed to consider a slash gesture in inches/second")]
        [SerializeField]
        private float slashSpeedThreshold = 70f;

        [Tooltip("Time window to detect the slash gesture in seconds")]
        [SerializeField]
        private float slashDetectionWindow = 0.3f;

        [Tooltip("Cooldown time after a slash is detected in seconds")]
        [SerializeField]
        public float slashCooldown = 1f;

        [Tooltip("Identify a slash only if the hand starts within chestDistanceLimit inches from chest")]
        [SerializeField]
        [HideInInspector]
        private bool requireTriggerFromChest;

        [Tooltip("If requireTriggerFromChest is true, this is the max distance from chest in inches to consider the slash valid.")]
        [SerializeField]
        [HideInInspector]
        private float chestDistanceLimit = 10f;

        public event Action<Vector2>? OnSlashDetected;

        private History<Vector2> handPositionHistory = null!;
        
        private DateTime lastDetectionTime = DateTime.MinValue;

        private void Start()
        {
            handPositionHistory = new History<Vector2>(slashDetectionWindow);
            SlashDetectionLoop(destroyCancellationToken).Forget();
        }

        private async UniTaskVoid SlashDetectionLoop(CancellationToken cancellationToken)
        {
            while (isActiveAndEnabled)
            {
                // Detect slash per frame
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken: cancellationToken);

                // Get the latest hand and chest positions
                bodyPoseController.TryGetBodyPose(playerIndex, BodyPoseController.PoseFlavor.Raw, out var bodyPose);
                var trackNode = bodyPose[nodeIndex];
                var chestNode = bodyPose[NodeIndex.Chest];
                
                // Skip if either node is missing
                if (!trackNode.HasValue) continue;
                if (!chestNode.HasValue) continue;

                // Store the hand position relative to the chest
                handPositionHistory.Add(trackNode.Value - chestNode.Value, Time.time);
                
                if (handPositionHistory.Count < 2) 
                    continue; // Not enough data yet
                
                if ((DateTime.Now - lastDetectionTime).TotalSeconds < slashCooldown) 
                    continue; // Still in cooldown
                
                // Calculate the slash vector
                Vector2 oldVector = handPositionHistory.EarliestItem;
                Vector2 newVector = handPositionHistory.LatestItem;
                var deltaTime = handPositionHistory.LatestItem.timestamp - handPositionHistory.EarliestItem.timestamp;
                if (deltaTime <= 0.9f * slashDetectionWindow) 
                    continue; // Not enough time elapsed
                var slashVector = (newVector - oldVector) / deltaTime / bodyPose.pixelsPerInch;

                if (slashVector.magnitude < slashSpeedThreshold) 
                    continue; // Not a fast enough slash

                // If required, check that the slash started close enough to the chest
                if (requireTriggerFromChest && oldVector.magnitude / bodyPose.pixelsPerInch > chestDistanceLimit)
                    continue;  

                // Handle a valid slash gesture
                handPositionHistory.Clear(); 
                lastDetectionTime = DateTime.Now;
                OnSlashDetected?.Invoke(slashVector);
            }
        }
    }
}
