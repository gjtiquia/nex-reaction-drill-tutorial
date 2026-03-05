#nullable enable

using UnityEditor;

namespace Nex.Essentials
{
    [CustomEditor(typeof(SlashDetector))]
    public class SlashDetectorEditor : Editor
    {
        private SerializedProperty requireTriggerFromChestProperty = null!;
        private SerializedProperty chestDistanceLimitProperty = null!;
        
        private void OnEnable()
        {
            EditorApplication.update += UpdateInspector;

            requireTriggerFromChestProperty = serializedObject.FindProperty("requireTriggerFromChest");
            chestDistanceLimitProperty = serializedObject.FindProperty("chestDistanceLimit");
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateInspector;
            requireTriggerFromChestProperty = null!;
            chestDistanceLimitProperty = null!;
        }

        private void UpdateInspector()
        {
            // Only repaint continuously if Unity is playing
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            EditorGUILayout.PropertyField(requireTriggerFromChestProperty);
            if (requireTriggerFromChestProperty.boolValue)
            {
                EditorGUILayout.PropertyField(chestDistanceLimitProperty);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}