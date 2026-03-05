#if MDK_HAND_ENABLED && MIN_PLAYOS_API_2
using UnityEditor;
using UnityEngine;

namespace Nex.Essentials.CustomEditors
{
    [CustomEditor(typeof(OpenClosedSignalProducer))]
    public class OpenClosedSignalProducerEditor : Editor
    {
        // Script field
        private SerializedProperty scriptProperty;
        
        // Common fields
        private SerializedProperty onStateChange;
        private SerializedProperty detectionMethod;
        private SerializedProperty handDetectionManager;
        private SerializedProperty poseIndex;
        private SerializedProperty handedness;
        private SerializedProperty currentGestureState;

        // Hand Pose specific fields
        private SerializedProperty currentHandState;
        private SerializedProperty closedHandState;
        private SerializedProperty openHandState;
        private SerializedProperty closedHandStateDebounce;
        private SerializedProperty openHandStateDebounce;
        private SerializedProperty thumbOpenThresholdAngle;
        private SerializedProperty thumbClosedThresholdAngle;
        private SerializedProperty fingerOpenThresholdAngle;
        private SerializedProperty fingerClosedThresholdAngle;

#if MDK_HAND_3_1_UP
        // Gesture Detection specific fields
        private SerializedProperty handGestureDetectionManager;
        private SerializedProperty openToClosedThreshold;
        private SerializedProperty closedToOpenThreshold;
        private SerializedProperty probSmoothWindow;
        private SerializedProperty openToClosedDebounce;
        private SerializedProperty closedToOpenDebounce;
        private SerializedProperty handPresenceThreshold;
        private SerializedProperty motionThreshold;
        private SerializedProperty handMissingTimeout;
        private SerializedProperty handGestureDebugText;
#endif

        private void OnEnable()
        {
            // Script field
            scriptProperty = serializedObject.FindProperty("m_Script");
            
            // Common fields
            onStateChange = serializedObject.FindProperty("onStateChange");
            detectionMethod = serializedObject.FindProperty("detectionMethod");
            handDetectionManager = serializedObject.FindProperty("handDetectionManager");
            poseIndex = serializedObject.FindProperty("poseIndex");
            handedness = serializedObject.FindProperty("handedness");
            currentGestureState = serializedObject.FindProperty("currentGestureState");

            // Hand Pose specific fields
            currentHandState = serializedObject.FindProperty("currentHandState");
            closedHandState = serializedObject.FindProperty("closedHandState");
            openHandState = serializedObject.FindProperty("openHandState");
            closedHandStateDebounce = serializedObject.FindProperty("closedHandStateDebounce");
            openHandStateDebounce = serializedObject.FindProperty("openHandStateDebounce");
            thumbOpenThresholdAngle = serializedObject.FindProperty("thumbOpenThresholdAngle");
            thumbClosedThresholdAngle = serializedObject.FindProperty("thumbClosedThresholdAngle");
            fingerOpenThresholdAngle = serializedObject.FindProperty("fingerOpenThresholdAngle");
            fingerClosedThresholdAngle = serializedObject.FindProperty("fingerClosedThresholdAngle");

#if MDK_HAND_3_1_UP
            // Gesture Detection specific fields
            handGestureDetectionManager = serializedObject.FindProperty("handGestureDetectionManager");
            openToClosedThreshold = serializedObject.FindProperty("openToClosedThreshold");
            closedToOpenThreshold = serializedObject.FindProperty("closedToOpenThreshold");
            probSmoothWindow = serializedObject.FindProperty("probSmoothWindow");
            openToClosedDebounce = serializedObject.FindProperty("openToClosedDebounce");
            closedToOpenDebounce = serializedObject.FindProperty("closedToOpenDebounce");
            handPresenceThreshold = serializedObject.FindProperty("handPresenceThreshold");
            motionThreshold = serializedObject.FindProperty("motionThreshold");
            handMissingTimeout = serializedObject.FindProperty("handMissingTimeout");
            handGestureDebugText = serializedObject.FindProperty("handGestureDebugText");
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            // Script field (read-only)
            GUI.enabled = false;
            EditorGUILayout.PropertyField(scriptProperty);
            GUI.enabled = true;
            EditorGUILayout.Space();

            // Common fields - always shown
            EditorGUILayout.PropertyField(onStateChange);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(detectionMethod);
            EditorGUILayout.Space();

            var isHandPose = detectionMethod.enumValueIndex == (int)OpenClosedSignalProducer.DetectionMethod.HandPose;
            var isGestureDetection = detectionMethod.enumValueIndex == (int)OpenClosedSignalProducer.DetectionMethod.GestureDetection;

            // Common tracking settings
            EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(poseIndex);
            EditorGUILayout.PropertyField(handedness);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("State (Read Only)", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.PropertyField(currentGestureState);
            GUI.enabled = true;

            EditorGUILayout.Space();

            // Hand Pose specific fields
            if (isHandPose)
            {
                EditorGUILayout.LabelField("Hand Pose Detection", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(handDetectionManager);

                EditorGUILayout.Space();
                GUI.enabled = false;
                EditorGUILayout.PropertyField(currentHandState);
                GUI.enabled = true;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Target Hand States", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(closedHandState);
                EditorGUILayout.PropertyField(openHandState);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debounce Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(closedHandStateDebounce);
                EditorGUILayout.PropertyField(openHandStateDebounce);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Finger Angle Thresholds", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(thumbOpenThresholdAngle);
                EditorGUILayout.PropertyField(thumbClosedThresholdAngle);
                EditorGUILayout.PropertyField(fingerOpenThresholdAngle);
                EditorGUILayout.PropertyField(fingerClosedThresholdAngle);
            }

#if MDK_HAND_3_1_UP
            // Gesture Detection specific fields
            if (isGestureDetection)
            {
                EditorGUILayout.LabelField("Gesture Detection", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(handGestureDetectionManager);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Hysteresis Thresholds", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(openToClosedThreshold);
                EditorGUILayout.PropertyField(closedToOpenThreshold);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Score Smoothing", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(probSmoothWindow);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Time-based Debounce", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(openToClosedDebounce);
                EditorGUILayout.PropertyField(closedToOpenDebounce);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(handPresenceThreshold);
                EditorGUILayout.PropertyField(motionThreshold);
                EditorGUILayout.PropertyField(handMissingTimeout);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(handGestureDebugText);
            }
#endif

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
