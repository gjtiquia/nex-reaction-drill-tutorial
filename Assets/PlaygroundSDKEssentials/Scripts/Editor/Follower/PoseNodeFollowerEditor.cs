#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Nex.Essentials
{
    [CustomEditor(typeof(PoseNodeFollower))]
    public class PoseNodeFollowerEditor : Editor
    {
        private SerializedProperty modeSerializedProperty = null!;
        private SerializedProperty extensionNodeProperty = null!;
        private SerializedProperty weightProperty = null!;
        private SerializedProperty enableFallbackProperty = null!;
        private SerializedProperty fallbackValueProperty = null!;

        private void OnEnable()
        {
            modeSerializedProperty = serializedObject.FindProperty("mode");
            extensionNodeProperty = serializedObject.FindProperty("extensionNode");
            weightProperty = serializedObject.FindProperty("weight");
            enableFallbackProperty = serializedObject.FindProperty("enableFallback");
            fallbackValueProperty = serializedObject.FindProperty("fallbackValue");
        }

        private void OnDisable()
        {
            modeSerializedProperty = null!;
            extensionNodeProperty = null!;
            weightProperty = null!;
            enableFallbackProperty = null!;
            fallbackValueProperty = null!;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            if (modeSerializedProperty.intValue == 1)
            {
                EditorGUILayout.PropertyField(extensionNodeProperty);
                EditorGUILayout.PropertyField(weightProperty);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(enableFallbackProperty);
                if (enableFallbackProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(fallbackValueProperty, GUIContent.none);
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.HelpBox("Common Presets:", MessageType.Info);
            var presets = Enum.GetValues(typeof(PoseNodeFollower.Preset));
            foreach (var item in presets)
            {
                var preset = (PoseNodeFollower.Preset)item;
                if (GUILayout.Button(preset.ToString()))
                {
                    foreach (var tar in targets)
                    {
                        if (tar is PoseNodeFollower follower)
                        {
                            follower.SetPreset(preset);
                        }
                    }
                }
            }
        }
    }
}
