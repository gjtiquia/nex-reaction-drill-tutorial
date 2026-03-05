using System;
using System.Collections.Generic;
using System.Linq;
using Jazz;
using UnityEngine;

namespace Nex.Essentials
{
    /**
     * Wraps the face landmarks API and return a Quaternion only
     * This abstraction hides the face landmarks API in case the face detection package is not installed
     * If you decided to use face landmark, you may directly use its API instead of a wrapper
     */
    public class FaceLandmarkWrapper : MonoBehaviour
    {
        /**
         * Captures the head pose rotations
         * The list of quaternions is the head pose rotations for each player
         * The quaternions are in the order of the players
         */
        public event Action<List<Quaternion?>> captureHeadPoseRotations;

        /**
         * The latest head pose rotations
         * The quaternions are in the order of the players
         */
        public List<Quaternion?> LatestRotations { get; private set; }

#if MDK_FACE_ENABLED
        [SerializeField] private HeadPoseDetector headPoseDetector;

        private void OnEnable()
        {
            headPoseDetector.captureHeadPoseDetection += HandleHeadPoseDetection;
        }

        private void OnDisable()
        {
            headPoseDetector.captureHeadPoseDetection -= HandleHeadPoseDetection;
        }

        private void HandleHeadPoseDetection(HeadPoseDetection headPoseDetection)
        {
            LatestRotations = headPoseDetection.playerHeadPoses.Select(pose => pose?.rotation).ToList();
            captureHeadPoseRotations?.Invoke(LatestRotations);
        }
#endif
    }
}
