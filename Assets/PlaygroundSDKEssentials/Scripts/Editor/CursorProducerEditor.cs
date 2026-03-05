#nullable enable

using Jazz;
using UnityEditor;
using UnityEngine;

namespace Nex.Essentials
{
    [CustomEditor(typeof(CursorProducer)), CanEditMultipleObjects]
    public class CursorProducerEditor : Editor
    {
        private SerializedProperty nodeIndexProperty = null!;
        private SerializedProperty centerReferenceNodeProperty = null!;
        private SerializedProperty centerOffsetProperty = null!;
        private SerializedProperty canvasHeightProperty = null!;

        private void OnEnable()
        {
            nodeIndexProperty = serializedObject.FindProperty("nodeIndex");
            centerReferenceNodeProperty = serializedObject.FindProperty("centerReferenceNode");
            centerOffsetProperty = serializedObject.FindProperty("centerOffset");
            canvasHeightProperty = serializedObject.FindProperty("canvasHeight");
        }

        private void OnDisable()
        {
            nodeIndexProperty = null!;
            centerOffsetProperty = null!;
            canvasHeightProperty = null!;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Common Presets:", MessageType.Info);
            if (GUILayout.Button("Right Wrist Cursor"))
            {
                centerReferenceNodeProperty.intValue = (int)BodyPose.NodeIndex.Chest;
                nodeIndexProperty.intValue = (int)BodyPose.NodeIndex.RightWrist;
                centerOffsetProperty.vector2Value = new Vector2(8, -5);
                canvasHeightProperty.floatValue = 17;
            }

            if (GUILayout.Button("Left Wrist Cursor"))
            {
                centerReferenceNodeProperty.intValue = (int)BodyPose.NodeIndex.Chest;
                nodeIndexProperty.intValue = (int)BodyPose.NodeIndex.LeftWrist;
                centerOffsetProperty.vector2Value = new Vector2(-8, -5);
                canvasHeightProperty.floatValue = 17;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
