#if UNITY_EDITOR

using System;
using UnityEditor;

namespace Nex.Essentials.MotionNode
{
    public static class Utils
    {
        /// Creates an instance of the given type and assigns it to managed reference
        public static object SetManagedReferenceToInstance(this SerializedProperty serializedProperty, Type type)
        {
            var instance = Activator.CreateInstance(type);

            serializedProperty.serializedObject.Update();
            serializedProperty.managedReferenceValue = instance;
            serializedProperty.serializedObject.ApplyModifiedProperties();

            return instance;
        }

        /// Sets managed reference to null
        public static void SetManagedReferenceToNull(this SerializedProperty serializedProperty)
        {
            serializedProperty.serializedObject.Update();
            serializedProperty.managedReferenceValue = null;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
