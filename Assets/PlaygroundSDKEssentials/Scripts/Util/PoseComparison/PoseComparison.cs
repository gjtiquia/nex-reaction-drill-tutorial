#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pose = Jazz.BodyPose;

namespace Nex.Essentials
{
    public class PoseComparison
    {
        private static readonly List<float> standardWeights = new()
        {
            1, // 0.Nose
            0, // 1.Chest
            1, // 2.RightShoulder
            1, // 3.RightElbow
            1, // 4.RightWrist
            1, // 5.LeftShoulder
            1, // 6.LeftElbow
            1, // 7.LeftWrist
            1, // 8.RightHip
            1, // 9.RightKnee
            1, // 10.RightAnkle
            1, // 11.LeftHip
            1, // 12.LeftKnee
            1, // 13.LeftAnkle
            1, // 14.RightEye
            1, // 15.LeftEye
            1, // 16.RightEar
            1, // 17.LeftEar
        };

        private readonly IReadOnlyList<float> weights;

        public readonly struct BodyComponent
        {
            internal readonly int originNodeIndex;
            internal readonly int[] nodeIndices;

            public BodyComponent(int originNodeIndex, int[] nodeIndices)
            {
                this.originNodeIndex = originNodeIndex;
                this.nodeIndices = nodeIndices;
            }
        }

        private class BodyComponentSnapshot
        {
            protected readonly BodyComponent definition;
            protected internal readonly List<Vector2> nodeDiffs;

            public BodyComponentSnapshot(BodyComponent definition)
            {
                this.definition = definition;
                nodeDiffs = new List<Vector2>(definition.nodeIndices.Length);
                foreach (var _ in definition.nodeIndices)
                {
                    nodeDiffs.Add(Vector2.zero);
                }
            }

            public void PopulateFrom(IReadOnlyList<Vector2> nodePositions)
            {
                var origin = nodePositions[definition.originNodeIndex];
                for (var i = 0; i < definition.nodeIndices.Length; ++i)
                {
                    if (nodePositions[definition.nodeIndices[i]] == Vector2.zero)
                    {
                        nodeDiffs[i] = i * (origin - nodePositions[1]);
                    }
                    else
                    {
                        nodeDiffs[i] = nodePositions[definition.nodeIndices[i]] - origin;
                    }
                }
            }

            public void PopulateTo(Vector2[] output)
            {
                var origin = output[definition.originNodeIndex];
                for (var i = 0; i < definition.nodeIndices.Length; ++i)
                {
                    output[definition.nodeIndices[i]] = origin + nodeDiffs[i];
                }
            }
        }

        private class BaseComponentWeightedSnapshot : BodyComponentSnapshot
        {
            private double graceLevel;
            private readonly float[] weights;
            internal double similarity;

            public BaseComponentWeightedSnapshot(BodyComponent definition) : base(definition)
            {
                weights = new float[definition.nodeIndices.Length];
            }

            public void PopulateFrom(
                IReadOnlyList<Vector2> nodePositions,
                IReadOnlyList<float> confidences,
                IReadOnlyList<float> nodeWeights)
            {
                graceLevel = 1;
                PopulateFrom(nodePositions);
                for (var i = 0; i < definition.nodeIndices.Length; ++i)
                {
                    var nodeIndex = definition.nodeIndices[i];
                    weights[i] = nodeWeights[nodeIndex];
                    graceLevel += confidences[nodeIndex] == 0 ? 6 : 0;
                }
            }

            public void ComputeSimilarity(BodyComponentSnapshot target)
            {
                PoseComparisonUtils.FixRotationAndScale(weights, nodeDiffs, target.nodeDiffs, graceLevel);
                similarity = PoseComparisonUtils.PoseDistanceToPercentage(weights, nodeDiffs, target.nodeDiffs);
            }

            public double weight
            {
                get
                {
                    var sumSquareRoots = weights.Aggregate(1.0, (acc, w) => acc + Math.Sqrt(w));
                    var avgSquareRoots = sumSquareRoots / weights.Length;
                    return avgSquareRoots * avgSquareRoots;
                }
            }
        }

        public static readonly BodyComponent[] components = new[]
        {
            new BodyComponent((int)Pose.NodeIndex.Chest, new []
            {
                (int)Pose.NodeIndex.Nose,
                (int)Pose.NodeIndex.LeftShoulder,
                (int)Pose.NodeIndex.RightShoulder,
                (int)Pose.NodeIndex.LeftHip,
                (int)Pose.NodeIndex.RightHip,
            }),
            new BodyComponent((int)Pose.NodeIndex.Nose, new []
            {
                (int)Pose.NodeIndex.LeftEye,
                (int)Pose.NodeIndex.RightEye,
                (int)Pose.NodeIndex.LeftEar,
                (int)Pose.NodeIndex.RightEar,
            }),
            new BodyComponent((int)Pose.NodeIndex.LeftShoulder, new []
            {
                (int)Pose.NodeIndex.LeftElbow,
                (int)Pose.NodeIndex.LeftWrist,
            }),
            new BodyComponent((int)Pose.NodeIndex.RightShoulder, new []
            {
                (int)Pose.NodeIndex.RightElbow,
                (int)Pose.NodeIndex.RightWrist,
            }),
            new BodyComponent((int)Pose.NodeIndex.LeftHip, new []
            {
                (int)Pose.NodeIndex.LeftKnee,
                (int)Pose.NodeIndex.LeftAnkle,
            }),
            new BodyComponent((int)Pose.NodeIndex.RightHip, new []
            {
                (int)Pose.NodeIndex.RightKnee,
                (int)Pose.NodeIndex.RightAnkle,
            }),
        };

        public static readonly BodyComponent[] autoWeightComponents = new[]
        {
            new BodyComponent((int)Pose.NodeIndex.Chest, new []
            {
                (int)Pose.NodeIndex.LeftShoulder,
                (int)Pose.NodeIndex.RightShoulder,
                (int)Pose.NodeIndex.LeftHip,
                (int)Pose.NodeIndex.RightHip,
            }),
            new BodyComponent((int)Pose.NodeIndex.LeftShoulder, new []
            {
                (int)Pose.NodeIndex.LeftElbow,
            }),
            new BodyComponent((int)Pose.NodeIndex.LeftElbow, new []
            {
                (int)Pose.NodeIndex.LeftWrist,
            }),
            new BodyComponent((int)Pose.NodeIndex.RightElbow, new []
            {
                (int)Pose.NodeIndex.RightShoulder,
            }),
            new BodyComponent((int)Pose.NodeIndex.RightWrist, new []
            {
                (int)Pose.NodeIndex.RightElbow,
            }),
            new BodyComponent((int)Pose.NodeIndex.LeftHip, new []
            {
                (int)Pose.NodeIndex.LeftKnee,
            }),
            new BodyComponent((int)Pose.NodeIndex.LeftKnee, new []
            {
                (int)Pose.NodeIndex.LeftAnkle,
            }),
            new BodyComponent((int)Pose.NodeIndex.RightKnee, new []
            {
                (int)Pose.NodeIndex.RightHip,
            }),
            new BodyComponent((int)Pose.NodeIndex.RightAnkle, new []
            {
                (int)Pose.NodeIndex.RightKnee,
            }),
        };

        private readonly List<BodyComponentSnapshot> targetSnapshots = components.Select(
            com => new BodyComponentSnapshot(com)).ToList();
        private readonly List<BaseComponentWeightedSnapshot> inputSnapshots = components.Select(
            com => new BaseComponentWeightedSnapshot(com)).ToList();

        public PoseComparison() : this(standardWeights)
        {

        }

        public PoseComparison(IReadOnlyList<float> weights)
        {
            this.weights = weights;
        }

        public PoseComparison RegisterTarget(IReadOnlyList<Vector2> nodePositions)
        {
            foreach (var snapshot in targetSnapshots)
            {
                snapshot.PopulateFrom(nodePositions);
            }

            return this;
        }

        public PoseComparison SetCandidate(SimplePose pose)
        {
            var nodePositions = new List<Vector2>();
            var nodeConfidences = new List<float>();
            foreach (var optionalNode in pose.nodes)
            {
                if (optionalNode.HasValue)
                {
                    nodePositions.Add(optionalNode.Value);
                    nodeConfidences.Add(1f);
                }
                else
                {
                    nodePositions.Add(Vector2.zero);
                    nodeConfidences.Add(0f);
                }
            }
            for (var i = 0; i < components.Length; ++i)
            {
                inputSnapshots[i].PopulateFrom(nodePositions, nodeConfidences, weights);
                inputSnapshots[i].ComputeSimilarity(targetSnapshots[i]);
            }
            return this;
        }

        public List<double> ComponentSimilarities => inputSnapshots.Select(snapshot => snapshot.similarity).ToList();

        public double OverallScore
        {
            get
            {
                var sumWeights = 0d;
                var sumScores = 0d;
                foreach (var snapshot in inputSnapshots)
                {
                    var weight = snapshot.weight;
                    sumWeights += weight;
                    sumScores += weight * snapshot.similarity;
                }

                return sumScores / sumWeights;
            }
        }

        private static Vector2[] GetNodePositions(IReadOnlyList<BodyComponentSnapshot> snapshots)
        {
            var ret = new Vector2[Pose.nodeNumber];
            ret[(int)Pose.NodeIndex.Chest] = Vector2.zero;
            foreach (var snapshot in snapshots)
            {
                snapshot.PopulateTo(ret);
            }

            return ret;
        }

        public Vector2[] MatchingNodePositions => GetNodePositions(inputSnapshots);

        #region Auto Weight Components

        public List<double> GetAutoWeightComponentSimilarities(IReadOnlyList<Vector2> nodePositions)
        {
            var similarities = new List<double>();
            foreach (var component in autoWeightComponents)
            {
                var defaultSnapshot = new BodyComponentSnapshot(component);
                defaultSnapshot.PopulateFrom(CoachPoseConfig.defaultNodePositions);

                // Create a copy of nodePositions with flipped Y coordinates
                var flippedPositions = nodePositions.Select(pos => new Vector2(pos.x, -pos.y)).ToList();
                var currentSnapshot = new BodyComponentSnapshot(component);
                currentSnapshot.PopulateFrom(flippedPositions);

                var weightedSnapshot = new BaseComponentWeightedSnapshot(component);
                weightedSnapshot.PopulateFrom(flippedPositions, Enumerable.Repeat(1.0f, nodePositions.Count).ToList(), weights);
                weightedSnapshot.ComputeSimilarity(defaultSnapshot);
                similarities.Add(weightedSnapshot.similarity);
            }
            return similarities;
        }

        #endregion
    }
}
