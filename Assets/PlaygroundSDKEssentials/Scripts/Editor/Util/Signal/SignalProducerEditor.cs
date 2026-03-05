#nullable enable

using UnityEditor;
using UnityEngine;

namespace Nex.Essentials
{
    [CustomEditor(typeof(SignalProducer), true)]
    public class SignalProducerEditor : Editor
    {
        protected virtual void OnEnable()
        {
            EditorApplication.update += UpdateInspector;
        }

        protected virtual void OnDisable()
        {
            EditorApplication.update -= UpdateInspector;
        }

        private void UpdateInspector()
        {
            // Only repaint continuously if Unity is playing
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        protected virtual void DrawInspector()
        {
            DrawDefaultInspector();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawInspector();
            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1 || !Application.isPlaying) return;
            var producer = (SignalProducer)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Signal", producer.Signal);
            }
        }
    }
}
