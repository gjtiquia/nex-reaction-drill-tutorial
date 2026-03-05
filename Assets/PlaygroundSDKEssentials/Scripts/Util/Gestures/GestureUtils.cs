#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    public static class GestureUtils
    {
        public static bool IsAnyHandRaised(SimplePose bodyPose, bool currentRaised)
        {
            return IsLeftHandRaised(bodyPose, currentRaised) || IsRightHandRaised(bodyPose, currentRaised);
        }

        public static bool AreBothHandsRaised(SimplePose bodyPose, bool currentRaised)
        {
            return IsLeftHandRaised(bodyPose, currentRaised) && IsRightHandRaised(bodyPose, currentRaised);
        }

        public static bool IsLeftHandRaised(SimplePose bodyPose, bool currentRaised)
        {
            return HandUtils.IsHandRaised(bodyPose, bodyPose.LeftWrist, currentRaised);
        }

        public static bool IsRightHandRaised(SimplePose bodyPose, bool currentRaised)
        {
            return HandUtils.IsHandRaised(bodyPose, bodyPose.RightWrist, currentRaised);
        }

        static class HandUtils
        {
            private const float activateRatio = 0.8f;
            private const float deactivateRatio = 0.2f;

            public static bool IsHandRaised(SimplePose bodyPose, Vector2? wrist, bool currentRaised)
            {
                // We use different threshold for debouncing.
                var chest = bodyPose.Chest;
                var nose = bodyPose.Nose;

                if (!chest.HasValue || !nose.HasValue || !wrist.HasValue) return false;

                var chestY = chest.Value.y;
                var noseY = nose.Value.y;
                if (currentRaised)
                {
                    var threshold = Mathf.Lerp(chestY, noseY, deactivateRatio);
                    return wrist.Value.y >= threshold;
                }
                else
                {
                    var threshold = Mathf.Lerp(chestY, noseY, activateRatio);
                    return wrist.Value.y >= threshold;
                }
            }
        }
    }
}
