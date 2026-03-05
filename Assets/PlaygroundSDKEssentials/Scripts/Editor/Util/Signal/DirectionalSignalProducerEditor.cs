#nullable enable

using UnityEditor;
using UnityEngine;

namespace Nex.Essentials
{
    [CustomEditor(typeof(DirectionalSignalProducer))]
    public class DirectionalSignalProducerEditor : SignalProducerEditor
    {
        private SerializedProperty useReferenceNodeProperty = null!;
        private SerializedProperty referenceNodeProperty = null!;
        private SerializedProperty scaleWithPpiProperty = null!;
        private SerializedProperty ppiFlavorProperty = null!;

        private static readonly GUIContent UseReferenceNodeLabel = new("With respect to node",
            "If enabled, the signal will be computed relative to the reference node. Otherwise, it will be computed relative to the origin (0,0).");

        private static readonly GUIContent ScaleWithPpiLabel = new("Scale with PPI",
            "If enabled, the signal will be scaled with PPI. The flavor for PPI can be different from the pose flavor.");

        protected override void OnEnable()
        {
            base.OnEnable();
            useReferenceNodeProperty = serializedObject.FindProperty("useReferenceNode");
            referenceNodeProperty = serializedObject.FindProperty("referenceNode");
            scaleWithPpiProperty = serializedObject.FindProperty("scaleWithPpi");
            ppiFlavorProperty = serializedObject.FindProperty("ppiFlavor");
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            useReferenceNodeProperty = null!;
            referenceNodeProperty = null!;
            scaleWithPpiProperty = null!;
            ppiFlavorProperty = null!;
        }

        protected override void DrawInspector()
        {
            base.DrawInspector();
            if (useReferenceNodeProperty.boolValue)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(useReferenceNodeProperty, UseReferenceNodeLabel);
                    EditorGUILayout.PropertyField(referenceNodeProperty, GUIContent.none);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(useReferenceNodeProperty, UseReferenceNodeLabel);
            }

            if (scaleWithPpiProperty.boolValue)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(scaleWithPpiProperty, ScaleWithPpiLabel);
                    EditorGUILayout.PropertyField(ppiFlavorProperty, GUIContent.none);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(scaleWithPpiProperty, ScaleWithPpiLabel);
            }
        }
    }
}
