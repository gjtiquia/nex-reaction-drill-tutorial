#nullable enable

using UnityEditor;
using UnityEngine;

namespace Nex.Essentials
{
    [CustomEditor(typeof(SignalPolarityDetector))]
    public class SignalPolarityDetectorEditor: Editor
    {
        private SerializedProperty isAsymmetricProperty = null!;
        private SerializedProperty influenceTauProperty = null!;
        private SerializedProperty influenceTauPositiveProperty = null!;
        private SerializedProperty influenceTauNegativeProperty = null!;
        private SerializedProperty thresholdProperty = null!;
        private SerializedProperty thresholdPositiveProperty = null!;
        private SerializedProperty thresholdNegativeProperty = null!;
        private SerializedProperty signalMarginProperty = null!;
        private SerializedProperty signalMarginPositiveProperty = null!;
        private SerializedProperty signalMarginNegativeProperty = null!;

        private void OnEnable()
        {
            EditorApplication.update += UpdateInspector;

            isAsymmetricProperty = serializedObject.FindProperty("isAsymmetric");
            influenceTauProperty = serializedObject.FindProperty("influenceTau");
            influenceTauPositiveProperty = serializedObject.FindProperty("influenceTauPositive");
            influenceTauNegativeProperty = serializedObject.FindProperty("influenceTauNegative");
            thresholdProperty = serializedObject.FindProperty("threshold");
            thresholdPositiveProperty = serializedObject.FindProperty("thresholdPositive");
            thresholdNegativeProperty = serializedObject.FindProperty("thresholdNegative");
            signalMarginProperty = serializedObject.FindProperty("signalMargin");
            signalMarginPositiveProperty = serializedObject.FindProperty("signalMarginPositive");
            signalMarginNegativeProperty = serializedObject.FindProperty("signalMarginNegative");
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateInspector;
            isAsymmetricProperty = null!;
            influenceTauProperty = null!;
            influenceTauPositiveProperty = null!;
            influenceTauNegativeProperty = null!;
            thresholdProperty = null!;
            thresholdPositiveProperty = null!;
            thresholdNegativeProperty = null!;
            signalMarginProperty = null!;
            signalMarginPositiveProperty = null!;
            signalMarginNegativeProperty = null!;
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

            EditorGUILayout.PropertyField(isAsymmetricProperty);
            if (isAsymmetricProperty.boolValue)
            {
                EditorGUILayout.PropertyField(influenceTauPositiveProperty);
                EditorGUILayout.PropertyField(influenceTauNegativeProperty);
                EditorGUILayout.PropertyField(thresholdPositiveProperty);
                EditorGUILayout.PropertyField(thresholdNegativeProperty);
                EditorGUILayout.PropertyField(signalMarginPositiveProperty);
                EditorGUILayout.PropertyField(signalMarginNegativeProperty);
            }
            else
            {
                EditorGUILayout.PropertyField(influenceTauProperty);
                EditorGUILayout.PropertyField(thresholdProperty);
                EditorGUILayout.PropertyField(signalMarginProperty);
            }

            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1 || !Application.isPlaying) return;
            var detector = (SignalPolarityDetector)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField("Signal", $"{detector.Signal}");
            }

            if (GUILayout.Button("Clear Data"))
            {
                detector.ClearData();
            }
        }
    }
}
