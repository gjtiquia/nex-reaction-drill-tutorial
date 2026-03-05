#nullable enable
using System;
using UnityEngine;

namespace Nex.Essentials.Examples.Runner
{
    public class TwoHandsSliceDetector : MonoBehaviour
    {
        [SerializeField] private SlashDetector leftHandSlashDetector = null!;
        [SerializeField] private SlashDetector rightHandSlashDetector = null!;

        [SerializeField] private GameObject leftHandIndicator = null!;
        [SerializeField] private GameObject rightHandIndicator = null!;

        [Tooltip("Time window in seconds to consider two slashes as a two hand slice")] [SerializeField]
        private float overlapWindow = 0.5f;

        // Record the last time a left/right slash was detected
        private float lastLeftSlashTime = -Mathf.Infinity;
        private float lastRightSlashTime = -Mathf.Infinity;

        [SerializeField]
        [Tooltip("Threshold for horizontal slashes, to prevent detecting slashes that are not horizontal enough")]
        private float horizontalThreshold = 0.7f;

        public event Action? OnTwoHandSliceDetected;

        // Start is called before the first frame update
        private void Start()
        {
            leftHandIndicator.SetActive(false);
            rightHandIndicator.SetActive(false);
            leftHandSlashDetector.OnSlashDetected += HandleLeftSlashDetected;
            rightHandSlashDetector.OnSlashDetected += HandleRightSlashDetected;
        }

        private void OnDestroy()
        {
            leftHandSlashDetector.OnSlashDetected -= HandleLeftSlashDetected;
            rightHandSlashDetector.OnSlashDetected -= HandleRightSlashDetected;
        }

        // To deactivate the indicators after a short time
        [SerializeField] private float indicatorDisplayTime = 0.5f;
        private float leftIndicatorDeactivateTime = -Mathf.Infinity;
        private float rightIndicatorDeactivateTime = -Mathf.Infinity;

        private void Update()
        {
            if (Time.time > leftIndicatorDeactivateTime)
            {
                leftHandIndicator.SetActive(false);
            }

            if (Time.time > rightIndicatorDeactivateTime)
            {
                rightHandIndicator.SetActive(false);
            }
        }

        private void HandleLeftSlashDetected(Vector2 direction)
        {
            // Only detect if the left hand slash is horizontal from right to left
            var horizontalMagnitude = Vector2.Dot(direction.normalized, Vector2.left);
            if (!(horizontalMagnitude > horizontalThreshold)) return;
            
            // Left slash detected
            leftIndicatorDeactivateTime = Time.time + indicatorDisplayTime;
            lastLeftSlashTime = Time.time;
            leftHandIndicator.SetActive(true);
            CheckForTwoHandSlice();
        }

        private void HandleRightSlashDetected(Vector2 direction)
        {
            // Only detect if the right hand slash is horizontal from left to right
            var horizontalMagnitude = Vector2.Dot(direction.normalized, Vector2.right);
            if (!(horizontalMagnitude > horizontalThreshold)) return;
            
            // Right slash detected
            rightIndicatorDeactivateTime = Time.time + indicatorDisplayTime;
            lastRightSlashTime = Time.time;
            rightHandIndicator.SetActive(true);
            CheckForTwoHandSlice();
        }

        private void CheckForTwoHandSlice()
        {
            // Check if both slashes occurred within the overlap window
            var slashesAreSynchronized = Mathf.Abs(lastLeftSlashTime - lastRightSlashTime) < overlapWindow;

            if (!slashesAreSynchronized) return;
            lastLeftSlashTime = -Mathf.Infinity;
            lastRightSlashTime = -Mathf.Infinity;
            OnTwoHandSliceDetected?.Invoke();
        }
    }
}
