#nullable enable

using UnityEngine;
using Pose = Jazz.BodyPose;

namespace Nex.Essentials
{
    [CreateAssetMenu(fileName = "NewCoachPoseConfig", menuName = "Coach Pose Config")]
    public class CoachPoseConfig : ScriptableObject
    {
        public Vector2[] nodePositions = new Vector2[Pose.nodeNumber];
        public float[] weights = new float[Pose.nodeNumber];

        private const float defaultScale = 1.7f / 28;
        public static readonly Vector2[] defaultNodePositions =
        {
            // kNoseNodeIndex = 0;
            new Vector2(0, 25) * defaultScale,
            // kChestNodeIndex = 1;
            new Vector2(0, 22) * defaultScale,
            // kRightShoulderNodeIndex = 2;
            new Vector2(-2, 22) * defaultScale,
            // kRightElbowNodeIndex = 3;
            new Vector2(-4, 18) * defaultScale,
            // kRightWristNodeIndex = 4;
            new Vector2(-6, 13) * defaultScale,
            // kLeftShoulderNodeIndex = 5;
            new Vector2(2, 22) * defaultScale,
            // kLeftElbowNodeIndex = 6;
            new Vector2(4, 18) * defaultScale,
            // kLeftWristNodeIndex = 7;
            new Vector2(6, 13) * defaultScale,
            // kRightHipNodeIndex = 8;
            new Vector2(-3, 14) * defaultScale,
            // kRightKneeNodeIndex = 9;
            new Vector2(-3, 8) * defaultScale,
            // kRightAnkleNodeIndex = 10;
            new Vector2(-3, 1) * defaultScale,
            // kLeftHipNodeIndex = 11;
            new Vector2(3, 14) * defaultScale,
            // kLeftKneeNodeIndex = 12;
            new Vector2(3, 8) * defaultScale,
            // kLeftAnkleNodeIndex = 13;
            new Vector2(3, 1) * defaultScale,
            // kRightEyeNodeIndex = 14;
            new Vector2(-1, 26) * defaultScale,
            // kLeftEyeNodeIndex = 15;
            new Vector2(1, 26) * defaultScale,
            // kRightEarNodeIndex = 16;
            new Vector2(-1.5f, 25) * defaultScale,
            // kLeftEarNodeIndex = 17;
            new Vector2(1.5f, 25) * defaultScale,
        };

        public static readonly float[] defaultNodeWeights =
        {
            // kNoseNodeIndex = 0;
            0.5f,
            // kChestNodeIndex = 1;
            1.0f,
            // kRightShoulderNodeIndex = 2;
            1.0f,
            // kRightElbowNodeIndex = 3;
            1.0f,
            // kRightWristNodeIndex = 4;
            1.5f,
            // kLeftShoulderNodeIndex = 5;
            1.0f,
            // kLeftElbowNodeIndex = 6;
            1.0f,
            // kLeftWristNodeIndex = 7;
            1.5f,
            // kRightHipNodeIndex = 8;
            1.0f,
            // kRightKneeNodeIndex = 9;
            1.2f,
            // kRightAnkleNodeIndex = 10;
            1.4f,
            // kLeftHipNodeIndex = 11;
            1.0f,
            // kLeftKneeNodeIndex = 12;
            1.2f,
            // kLeftAnkleNodeIndex = 13;
            1.4f,
            // kRightEyeNodeIndex = 14;
            0.1f,
            // kLeftEyeNodeIndex = 15;
            0.1f,
            // kRightEarNodeIndex = 16;
            0.05f,
            // kLeftEarNodeIndex = 17;
            0.05f,
        };

        public void Reset()
        {
            for (var i = 0; i < Pose.nodeNumber; ++i)
            {
                nodePositions[i] = defaultNodePositions[i];
                weights[i] = defaultNodeWeights[i];
            }
        }
    }
}
