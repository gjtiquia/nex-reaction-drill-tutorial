#nullable enable

using System;
using System.Collections.Generic;
using Jazz;
using UnityEditor;
using UnityEngine;
using Pose = Jazz.BodyPose;

namespace Nex.Essentials
{
    [CustomEditor(typeof(CoachPoseConfig))]
    public class CoachPoseConfigEditor : Editor
    {
        private enum EditingMode
        {
            Position,
            Weight,
        }

        private static EditingMode _editingMode = EditingMode.Position;

        private static readonly Color32[] SpPoseNodeColors = {
            new Color32(0x00, 0x00, 0xFF, 0xFF),
            new Color32(0x00, 0x55, 0xFF, 0xFF),
            new Color32(0x00, 0xAA, 0xFF, 0xFF),
            new Color32(0x00, 0xFF, 0xFF, 0xFF),
            new Color32(0x00, 0xFF, 0xAA, 0xFF),
            new Color32(0x00, 0xFF, 0x55, 0xFF),
            new Color32(0x00, 0xFF, 0x00, 0xFF),
            new Color32(0x55, 0xFF, 0x00, 0xFF),
            new Color32(0xAA, 0xFF, 0x00, 0xFF),
            new Color32(0xFF, 0xFF, 0x00, 0xFF),
            new Color32(0xFF, 0xAA, 0x00, 0xFF),
            new Color32(0xFF, 0x55, 0x00, 0xFF),
            new Color32(0xFF, 0x00, 0x00, 0xFF),
            new Color32(0xFF, 0x00, 0x55, 0xFF),
            new Color32(0xFF, 0x00, 0xAA, 0xFF),
            new Color32(0xFF, 0x00, 0xFF, 0xFF),
            new Color32(0xAA, 0x00, 0xFF, 0xFF),
            new Color32(0x55, 0x00, 0xFF, 0xFF),
        };

        private static readonly Vector3 SpPositionSnap = Vector3.one * 0.1f;
        private const float WeightNodeSizeOffset = 0.01f;
        private const float WeightNodeSizeScale = 0.01f;

        private List<SerializedProperty> nodePositionProperties = null!;
        private List<SerializedProperty> weightProperties = null!;

        private static float HandleSizeFromWeight(float weight)
        {
            return Mathf.Max(weight, 0) * WeightNodeSizeScale + WeightNodeSizeOffset;
        }

        private static float WeightFromHandleSize(float handleSize)
        {
            return Mathf.Max(handleSize - WeightNodeSizeOffset, 0f) / WeightNodeSizeScale;
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += RenderSceneGUI;
            var nodePositionsProperty = serializedObject.FindProperty("nodePositions");
            var weightsProperty = serializedObject.FindProperty("weights");
            nodePositionProperties = new List<SerializedProperty>();
            weightProperties = new List<SerializedProperty>();
            for (var i = 0; i < Pose.nodeNumber; ++i)
            {
                nodePositionProperties.Add(nodePositionsProperty.GetArrayElementAtIndex(i));
                weightProperties.Add(weightsProperty.GetArrayElementAtIndex(i));
            }
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= RenderSceneGUI;
        }

        private void ResetWeights()
        {
            for (var i = 0; i < Pose.nodeNumber; ++i)
            {
                weightProperties[i].floatValue = CoachPoseConfig.defaultNodeWeights[i];
            }
        }

        private void ResetNodePositions()
        {
            for (var i = 0; i < Pose.nodeNumber; ++i)
            {
                nodePositionProperties[i].vector2Value = CoachPoseConfig.defaultNodePositions[i];
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var newEditingMode = (EditingMode)EditorGUILayout.EnumPopup("Editing Mode", _editingMode);
            if (_editingMode != newEditingMode)
            {
                _editingMode = newEditingMode;
                HandleUtility.Repaint();
            }

            if (GUILayout.Button("Reset Weights"))
            {
                ResetWeights();
            }
            if (GUILayout.Button("Reset Node Positions"))
            {
                ResetNodePositions();
            }
            if (GUILayout.Button("Reset All"))
            {
                // We are going to repopulate all the pose weights and pose.
                ResetWeights();
                ResetNodePositions();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void RenderPoseNode(int index)
        {
            Color handleColor = SpPoseNodeColors[index];
            const float maxSaturationWeight = 3f;
            const float minOpacity = 0.3f;
            var clampedWeight = Mathf.Clamp(weightProperties[index].floatValue, 0, maxSaturationWeight);
            handleColor.a = clampedWeight / maxSaturationWeight * (1 - minOpacity) + minOpacity;
            Handles.color = handleColor;

            switch (_editingMode)
            {
                case EditingMode.Position:
                    var fmh_137_21_638297796625534560 = Quaternion.identity; nodePositionProperties[index].vector2Value = Handles.FreeMoveHandle(
                        nodePositionProperties[index].vector2Value,
                        0.015f,
                        SpPositionSnap,
                        Handles.DotHandleCap);
                    break;
                case EditingMode.Weight:
                    Handles.Label(
                        nodePositionProperties[index].vector2Value,
                        weightProperties[index].floatValue.ToString("N2"));
                    weightProperties[index].floatValue = WeightFromHandleSize(
                        Handles.RadiusHandle(
                            Quaternion.identity,
                            nodePositionProperties[index].vector2Value,
                            HandleSizeFromWeight(weightProperties[index].floatValue)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RenderSceneGUI(SceneView sceneView)
        {
            // First draw all the limbs.
            for (var l = 0; l < Pose.limbNumber; ++l)
            {
                var fromIndex = BodyPoseConfig.limbToNodeIndexes[l, 0];
                var toIndex = BodyPoseConfig.limbToNodeIndexes[l, 1];
                // Use the color of the to node.
                Handles.color = SpPoseNodeColors[toIndex];
                Handles.DrawLine(nodePositionProperties[fromIndex].vector2Value, nodePositionProperties[toIndex].vector2Value);
            }
            for (var i = 0; i < Pose.nodeNumber; ++i)
            {
                RenderPoseNode(i);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
