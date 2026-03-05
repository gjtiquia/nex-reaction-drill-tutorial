#nullable enable

using UnityEngine;

namespace Nex.Essentials
{
    [RequireComponent(typeof(Animator))]
    public class IKAvatarController : MonoBehaviour
    {
        private Animator animator = null!;

        [SerializeField] private BodyPoseController bodyPoseController = null!;
        [SerializeField] private int poseIndex;
        [SerializeField] private BodyPoseController.PoseFlavor flavor = BodyPoseController.PoseFlavor.Smoothed;

        [Header("Configurations")]
        [SerializeField] private bool trackLegs = true;

        [SerializeField] private float xMultiplier = 1f;
        [SerializeField] private float yMultiplier = 1f;
        [SerializeField] private float zMultiplier = 1f;
        [SerializeField] private float horizontalMovementMultiplier = 1f;
        [SerializeField] private float rotationMultiplier = 1f;

        private void Start()
        {
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!animator) return;

            if (!bodyPoseController.TryGetBodyPose(poseIndex, flavor, out var bodyPose)) return;
            var ppi = bodyPose.pixelsPerInch;

            // For moving the hands and elbows
            SetArmsIK(bodyPose, ppi);

            // For moving the feet and knees
            SetLegsIK(bodyPose, ppi);

            // For moving the whole avatar left and right
            if (horizontalMovementMultiplier != 0) SetSideWayPosition(bodyPose, ppi);

            // For leaning left and right (rotating along the z-axis)
            if (rotationMultiplier != 0) SetBodyRotation(bodyPose);
        }

        // Move arms to target position
        // Only move arm if the corresponding wrist is detected
        // Because IK use the hand as the goal
        private void SetArmsIK(SimplePose bodyPose, float ppi)
        {
            // Get the difference between the shoulder and chest
            // In MDK detection, the position of the chest is slightly higher than the upper chest bone in our avatar model
            var boneUpperChestY = animator.GetBoneTransform(HumanBodyBones.UpperChest).position.y;
            var boneLeftShoulderY = animator.GetBoneTransform(HumanBodyBones.LeftShoulder).position.y;
            var boneRightShoulderY = animator.GetBoneTransform(HumanBodyBones.RightShoulder).position.y;
            var chestYAdjust = (boneLeftShoulderY + boneRightShoulderY) / 2 - boneUpperChestY;

            SetArmIK(bodyPose: bodyPose, wristIndex: SimplePose.NodeIndex.LeftWrist,
                elbowIndex: SimplePose.NodeIndex.LeftElbow, shoulderIndex: SimplePose.NodeIndex.LeftShoulder, ppi: ppi,
                handGoal: AvatarIKGoal.LeftHand, elbowHint: AvatarIKHint.LeftElbow, chestYAdjust: chestYAdjust);
            SetArmIK(bodyPose: bodyPose, wristIndex: SimplePose.NodeIndex.RightWrist,
                elbowIndex: SimplePose.NodeIndex.RightElbow, shoulderIndex: SimplePose.NodeIndex.RightShoulder,
                ppi: ppi, handGoal: AvatarIKGoal.RightHand, elbowHint: AvatarIKHint.RightElbow,
                chestYAdjust: chestYAdjust);
        }

        private void SetArmIK(SimplePose bodyPose, SimplePose.NodeIndex wristIndex, SimplePose.NodeIndex elbowIndex,
            SimplePose.NodeIndex shoulderIndex, float ppi, AvatarIKGoal handGoal, AvatarIKHint elbowHint,
            float chestYAdjust)
        {
            if (!bodyPose.TryGetNode(wristIndex, out var wristPos)) return;
            if (!bodyPose.TryGetNode(elbowIndex, out var elbowPos)) return;
            if (!bodyPose.TryGetNode(shoulderIndex, out var shoulderPos)) return;
            if (!bodyPose.TryGetNode(SimplePose.NodeIndex.Chest, out var chestPos)) return;

            // Find out how far away the wrist and the elbow from the chest in inches
            // And use that to calculate how far the hand is from the chest in inches
            // Hand is from elbow to wrist extend 30%
            var wristChestDiff = (wristPos - chestPos) / ppi;
            var elbowChestDiff = (elbowPos - chestPos) / ppi;
            var handChestDiff = Vector2.LerpUnclamped(elbowChestDiff, wristChestDiff, 1.3f);

            // Calculate how far the elbow is in front of the body
            // The distance between shoulder and elbow (d) is assumed to be 10 inches
            // Find the z value until the distance between shoulder and elbow is 10 inches
            // d^2 = x^2 + y^2 + z^2
            // z = sqrt( 10^2 - (x^2 + y^2) )
            // But if for whatever reason the measured distance is longer than 10 inches, make z=0 rather than NaN
            // The calculation assumes the elbow is always at the same plane or in front of the body
            var elbowShoulderDiff = (elbowPos - shoulderPos) / ppi;
            const float elbowShoulderDistanceSquared = 100;
            var elbowZInInches =
                Mathf.Sqrt(Mathf.Max(elbowShoulderDistanceSquared - elbowShoulderDiff.sqrMagnitude, 0));

            // Similarly, calculate how far the hand is in front of the elbow
            // The distance between elbow and hand (d) is assumed to be 14 inches
            // Find the z value until the distance between elbow and hand is 14 inches
            // The ultimate z value for the hand in the world space is the z of elbow + the z calculated relative to the elbow
            // The calculation assumes the hand is always at the same plane or in front of the elbow
            var handElbowDiff = handChestDiff - elbowChestDiff;
            const float wristElbowDistanceSquared = 196;
            var handZInInches = elbowZInInches +
                                Mathf.Sqrt(Mathf.Max(wristElbowDistanceSquared - handElbowDiff.sqrMagnitude, 0));

            // Setting the position of hand and elbow based on the position of the chest
            // Upper chest is closer to the chest position in the body pose detection 
            var animatorChest = animator.GetBoneTransform(HumanBodyBones.UpperChest).position;

            var handTarget = new Vector3(animatorChest.x + ScaleX(handChestDiff.x),
                animatorChest.y + ScaleY(handChestDiff.y) + chestYAdjust, animatorChest.z + ScaleZ(handZInInches));

            var elbowTarget = new Vector3(animatorChest.x + ScaleX(elbowChestDiff.x),
                animatorChest.y + ScaleY(elbowChestDiff.y) + chestYAdjust, animatorChest.z + ScaleZ(elbowZInInches));

            animator.SetIKPositionWeight(handGoal, 1);
            animator.SetIKPosition(handGoal, handTarget);

            animator.SetIKHintPositionWeight(elbowHint, 1);
            animator.SetIKHintPosition(elbowHint, elbowTarget);
        }

        // Move arms to target position
        // Only move leg if the corresponding ankle is detected
        // Because IK use the foot as the goal
        private void SetLegsIK(SimplePose bodyPose, float ppi)
        {
            // If legs are not tracked, make it stand straight
            if (!trackLegs)
            {
                var animatorChest = animator.GetBoneTransform(HumanBodyBones.UpperChest).position;

                var ankleTarget = new Vector3(animatorChest.x - 0.1f, animatorChest.y - 2f, animatorChest.z);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
                animator.SetIKPosition(AvatarIKGoal.LeftFoot, ankleTarget);

                ankleTarget = new Vector3(animatorChest.x + 0.1f, animatorChest.y - 2f, animatorChest.z);
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
                animator.SetIKPosition(AvatarIKGoal.RightFoot, ankleTarget);

                return;
            }

            SetLegIK(bodyPose: bodyPose, ankleIndex: SimplePose.NodeIndex.LeftAnkle,
                kneeIndex: SimplePose.NodeIndex.LeftKnee, hipIndex: SimplePose.NodeIndex.LeftHip, ppi: ppi,
                footGoal: AvatarIKGoal.LeftFoot, kneeHint: AvatarIKHint.LeftKnee);
            SetLegIK(bodyPose: bodyPose, ankleIndex: SimplePose.NodeIndex.RightAnkle,
                kneeIndex: SimplePose.NodeIndex.RightKnee, hipIndex: SimplePose.NodeIndex.RightHip, ppi: ppi,
                footGoal: AvatarIKGoal.RightFoot, kneeHint: AvatarIKHint.RightKnee);
        }

        private void SetLegIK(SimplePose bodyPose, SimplePose.NodeIndex ankleIndex, SimplePose.NodeIndex kneeIndex,
            SimplePose.NodeIndex hipIndex, float ppi, AvatarIKGoal footGoal, AvatarIKHint kneeHint)
        {
            if (!bodyPose.TryGetNode(ankleIndex, out var anklePos)) return;
            if (!bodyPose.TryGetNode(kneeIndex, out var kneePos)) return;
            if (!bodyPose.TryGetNode(hipIndex, out var hipPos)) return;
            if (!bodyPose.TryGetNode(SimplePose.NodeIndex.Chest, out var chestPos)) return;

            // Find how far the ankle and the knee are away from the chest in inches
            // Then find how far the foot is away from the chest in inches
            // The foot is from knee to ankle extend 30%
            var ankleChestDiff = (anklePos - chestPos) / ppi;
            var kneeChestDiff = (kneePos - chestPos) / ppi;
            var footChestDiff = Vector2.LerpUnclamped(kneeChestDiff, ankleChestDiff, 1.3f);

            // Similar to elbow & hand calculation, calculate how far the knee is in front of the elbow
            // The distance between hip and knee (d) is assumed to be 16 inches
            // Find the z value until the distance between hip and knee is 16 inches
            // The calculation assumes the knee is always at the same plane or in front of the body
            var kneeHipDiff = (hipPos - kneePos) / ppi;
            const float kneeHipDistanceSquared = 256;
            var kneeZInInches = Mathf.Sqrt(Mathf.Max(kneeHipDistanceSquared - kneeHipDiff.sqrMagnitude, 0));

            // Similarly, calculate how far the foot is BEHIND the knee
            // The distance between hip and knee (d) is assumed to be 16 inches
            // Find the z value until the distance between knee and foot is 16 inches
            // The ultimate z value for the foot in the world space is the z of knee - the z calculated relative to the knee
            // The calculation assumes the foot is always at the same plane or behind the knee
            var footKneeDiff = footChestDiff - kneeChestDiff;
            const float footKneeDistanceSquared = 256;
            var footZInInches = kneeZInInches -
                                Mathf.Sqrt(Mathf.Max(footKneeDistanceSquared - footKneeDiff.sqrMagnitude, 0));

            // Setting the position of foot and knee based on the position of the chest
            // Using the chest assuming that is the anchor of the body
            // Upper chest is closer to the chest position in the body pose detection 
            var animatorChest = animator.GetBoneTransform(HumanBodyBones.UpperChest).position;

            var footTarget = new Vector3(animatorChest.x + ScaleX(footChestDiff.x),
                animatorChest.y + ScaleY(footChestDiff.y), animatorChest.z + ScaleZ(footZInInches));

            var kneeTarget = new Vector3(animatorChest.x + ScaleX(kneeChestDiff.x),
                animatorChest.y + ScaleY(kneeChestDiff.y), animatorChest.z + ScaleZ(kneeZInInches));

            animator.SetIKPositionWeight(footGoal, 1);
            animator.SetIKPosition(footGoal, footTarget);

            animator.SetIKHintPositionWeight(kneeHint, 1);
            animator.SetIKHintPosition(kneeHint, kneeTarget);

            // Assuming the legs are always facing front
            animator.SetIKRotationWeight(footGoal, 1);
            animator.SetIKRotation(footGoal, Quaternion.identity);
        }

        // Move the avatar left and right when the player is moving left or right in the camera frame
        private void SetSideWayPosition(SimplePose bodyPose, float ppi)
        {
            if (!bodyPose.TryGetNode(SimplePose.NodeIndex.Chest, out var chestPos)) return;
            var inchesOffFromCenter = chestPos.x / ppi;

            transform.localPosition = new Vector3(ScaleX(inchesOffFromCenter) * horizontalMovementMultiplier,
                transform.localPosition.y, transform.localPosition.z);
        }

        // Rotate the avatar when the player lean left or right
        private void SetBodyRotation(SimplePose bodyPose)
        {
            if (!bodyPose.TryGetNode(SimplePose.NodeIndex.Chest, out var chest)) return;

            // If hip is detected use the middle position between two hip and the chest for calculation
            // If not use the nose and the chest for calculation
            if (bodyPose.TryGetNode(SimplePose.NodeIndex.CenterHip, out var hip))
            {
                var body = chest - hip;
                var angle = Vector2.SignedAngle(Vector2.up, body);

                transform.localRotation = Quaternion.Euler(0, 0, angle * rotationMultiplier);
            }
            else
            {
                if (!bodyPose.TryGetNode(SimplePose.NodeIndex.Nose, out var nose)) return;
                var body = nose - chest;
                var angle = Vector2.SignedAngle(Vector2.up, body);

                transform.localRotation = Quaternion.Euler(0, 0, angle * rotationMultiplier);
            }
        }

        // Scale anything in the horizontal direction from inches to the avatar body scale
        // The scale factor 60 is an approximation based on the ppi ideal model
        private float ScaleX(float inputXInInches)
        {
            return inputXInInches / 60 * xMultiplier;
        }

        // Scale anything in the vertical direction from inches to the avatar body scale
        // The scale factor 60 is an approximation based on the ppi ideal model
        private float ScaleY(float inputYInInches)
        {
            return inputYInInches / 60 * yMultiplier;
        }

        // Scale anything in the z direction from inches to the avatar body scale
        // The scale factor 60 is an approximation based on the ppi ideal model
        private float ScaleZ(float inputZInInches)
        {
            return inputZInInches / 60 * zMultiplier;
        }
    }
}
