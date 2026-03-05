#nullable enable

using Jazz;
using UnityEditor;
using UnityEngine;

namespace Nex.Essentials
{
    [CustomEditor(typeof(PlayerPreviewFrameProvider)), CanEditMultipleObjects]
    public class PlayerPreviewFrameProviderEditor : Editor
    {
        private SerializedProperty nodeIndexProperty = null!;
        private SerializedProperty horizontalMarginProperty = null!;
        private SerializedProperty topMarginProperty = null!;
        private SerializedProperty bottomMarginProperty = null!;

        private void OnEnable()
        {
            nodeIndexProperty = serializedObject.FindProperty("nodeIndex");
            var marginsProperty = serializedObject.FindProperty("margins");
            horizontalMarginProperty = marginsProperty.FindPropertyRelative("horizontalMarginInInches");
            topMarginProperty = marginsProperty.FindPropertyRelative("topMarginInInches");
            bottomMarginProperty = marginsProperty.FindPropertyRelative("bottomMarginInInches");
        }

        private void OnDisable()
        {
            nodeIndexProperty = null!;
            horizontalMarginProperty = null!;
            topMarginProperty = null!;
            bottomMarginProperty = null!;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Common Presets:", MessageType.Info);
            if (GUILayout.Button("Face Only (1:1)"))
            {
                const int squareMargin = 8;
                nodeIndexProperty.intValue = (int)BodyPose.NodeIndex.Nose;
                horizontalMarginProperty.floatValue = squareMargin;
                topMarginProperty.floatValue = squareMargin;
                bottomMarginProperty.floatValue = squareMargin;
            }

            if (GUILayout.Button("Upper Body (3:4)"))
            {
                nodeIndexProperty.intValue = (int)BodyPose.NodeIndex.Nose;
                horizontalMarginProperty.floatValue = 9;
                topMarginProperty.floatValue = 9;
                bottomMarginProperty.floatValue = 15;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
