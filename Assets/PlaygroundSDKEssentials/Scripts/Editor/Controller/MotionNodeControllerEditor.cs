#nullable enable

using UnityEditor;

namespace Nex.Essentials
{
    [CustomEditor(typeof(MotionNodeController)), CanEditMultipleObjects]
    public class MotionNodeControllerEditor : Editor
    {
        private SerializedProperty sourceProperty = null!;
        private SerializedProperty bodyPoseControllerProperty = null!;
        private SerializedProperty poseIndexProperty = null!;
        private SerializedProperty poseFlavorProperty = null!;
        private SerializedProperty nodeIndexProperty = null!;
        private SerializedProperty originProperty = null!;
        private SerializedProperty originNodeIndexProperty = null!;
        private SerializedProperty frameOriginProperty = null!;
        private SerializedProperty unitProperty = null!;
        private SerializedProperty useSmoothedPpiProperty = null!;
        private SerializedProperty ppiSmoothingConfigProperty = null!;
        private SerializedProperty sourceMotionNodeControllerProperty = null!;
        private SerializedProperty valueTransformationsProperty = null!;
        private SerializedProperty valueDestinationsProperty = null!;

        private void OnEnable()
        {
            sourceProperty = serializedObject.FindProperty("source");
            bodyPoseControllerProperty = serializedObject.FindProperty("bodyPoseController");
            poseIndexProperty = serializedObject.FindProperty("poseIndex");
            poseFlavorProperty = serializedObject.FindProperty("poseFlavor");
            nodeIndexProperty = serializedObject.FindProperty("nodeIndex");
            originProperty = serializedObject.FindProperty("origin");
            originNodeIndexProperty = serializedObject.FindProperty("originNodeIndex");
            frameOriginProperty = serializedObject.FindProperty("frameOrigin");
            unitProperty = serializedObject.FindProperty("unit");
            useSmoothedPpiProperty = serializedObject.FindProperty("useSmoothedPpi");
            ppiSmoothingConfigProperty = serializedObject.FindProperty("ppiSmoothingConfig");
            sourceMotionNodeControllerProperty = serializedObject.FindProperty("sourceMotionNodeController");
            valueTransformationsProperty = serializedObject.FindProperty("valueTransformations");
            valueDestinationsProperty = serializedObject.FindProperty("valueDestinations");
        }

        private void OnDisable()
        {
            sourceProperty = null!;
            bodyPoseControllerProperty = null!;
            poseIndexProperty = null!;
            poseFlavorProperty = null!;
            nodeIndexProperty = null!;
            originProperty = null!;
            originNodeIndexProperty = null!;
            frameOriginProperty = null!;
            unitProperty = null!;
            useSmoothedPpiProperty = null!;
            ppiSmoothingConfigProperty = null!;
            sourceMotionNodeControllerProperty = null!;
            valueTransformationsProperty = null!;
            valueDestinationsProperty = null!;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(sourceProperty);
            if (sourceProperty.intValue == 0)
            {
                EditorGUILayout.PropertyField(bodyPoseControllerProperty);
                EditorGUILayout.PropertyField(poseIndexProperty);
                EditorGUILayout.PropertyField(poseFlavorProperty);
                EditorGUILayout.PropertyField(nodeIndexProperty);
                EditorGUILayout.PropertyField(originProperty);
                if (!originProperty.hasMultipleDifferentValues)
                {
                    if (originProperty.enumValueIndex == (int)MotionNodeController.OriginType.Node)
                    {
                        EditorGUILayout.PropertyField(originNodeIndexProperty);
                    }
                    else if (originProperty.enumValueIndex == (int)MotionNodeController.OriginType.Frame)
                    {
                        EditorGUILayout.PropertyField(frameOriginProperty);
                    }
                }
                EditorGUILayout.PropertyField(unitProperty);
                EditorGUILayout.PropertyField(useSmoothedPpiProperty);
                ppiSmoothingConfigProperty.isExpanded = useSmoothedPpiProperty.boolValue;
                if (useSmoothedPpiProperty.boolValue)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(ppiSmoothingConfigProperty);
                    }
                }
            }
            else if (sourceProperty.intValue == 1)
            {
                EditorGUILayout.PropertyField(sourceMotionNodeControllerProperty);
            }
            EditorGUILayout.PropertyField(valueTransformationsProperty, true);
            EditorGUILayout.PropertyField(valueDestinationsProperty, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
