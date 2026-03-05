#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nex.Essentials.MotionNode
{
    public abstract class ValueManipulatorBasePropertyDrawerBase : PropertyDrawer
    {
        private static readonly Color BackgroundColor = new(0.1f, 0.5f, 0.9f, 1f);
        private static readonly Color ErrorColor = new(0.9f, 0.1f, 0.1f, 1f);
        private const float CheckboxWidth = 15;
        private const float ButtonWidth = 150;

        protected abstract Type GetBaseType();

        // To accomodate the change in height when the property is expanded
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var labelWidth = position.width - (ButtonWidth +
                                               3 * EditorGUIUtility.standardVerticalSpacing + CheckboxWidth);
            var checkboxPosition = new Rect(position.x + EditorGUIUtility.standardVerticalSpacing, position.y,
                CheckboxWidth, EditorGUIUtility.singleLineHeight);
            var labelPosition =
                new Rect(position.x + checkboxPosition.width + 2 * EditorGUIUtility.standardVerticalSpacing, position.y,
                    labelWidth, EditorGUIUtility.singleLineHeight);

            // Show enabled on top
            var enabledProperty = property.FindPropertyRelative(nameof(ValueManipulatorBase.enabled));
            if (enabledProperty != null)
            {
                EditorGUI.PropertyField(checkboxPosition, enabledProperty, GUIContent.none);
            }

            // Show description next to enable
            var descriptionProperty = property.FindPropertyRelative(nameof(ValueManipulatorBase.description));
            var description = descriptionProperty != null ? descriptionProperty.stringValue : "";
            if (string.IsNullOrEmpty(description)) description = label.text;
            EditorGUI.LabelField(labelPosition, new GUIContent(description));

            DrawMenuButton(property, position);

            EditorGUI.PropertyField(position, property, GUIContent.none, true);
            EditorGUI.EndProperty();
        }

        private void DrawMenuButton(SerializedProperty property, Rect position)
        {
            var buttonPosition = new Rect(position.x + position.width - ButtonWidth, position.y, ButtonWidth,
                EditorGUIUtility.singleLineHeight);

            var storedColor = GUI.backgroundColor;
            GUI.backgroundColor = BackgroundColor;

            string className;
            var tooltip = "";
            const string nullLabel = "Null (Assign)";

            var objValue = property.managedReferenceValue;
            if (objValue != null)
            {
                var objType = objValue.GetType();
                className = objType.Name;
                tooltip = objType.AssemblyQualifiedName;
            }
            else
            {
                className = nullLabel;
                GUI.backgroundColor = ErrorColor;
            }

            if (GUI.Button(buttonPosition, new GUIContent(className, tooltip)))
            {
                ShowContextMenuForManagedReference(property, buttonPosition);
            }

            GUI.backgroundColor = storedColor;
        }

        /// Generic selection menu
        private void ShowContextMenuForManagedReference(SerializedProperty property, Rect position)
        {
            var context = new GenericMenu();
            FillContextMenu(context, property);
            context.DropDown(position);
        }

        private void FillContextMenu(GenericMenu contextMenu, SerializedProperty property)
        {
            // Adds "Make Null" menu command
            contextMenu.AddItem(new GUIContent("Null"), false, property.SetManagedReferenceToNull);

            // Get types for manage reference and adds them to the menu
            GetTypesForManagedReference()
                .ForEach(type => contextMenu.AddItem(new GUIContent(type.Name), false, AssignNewInstanceCommand,
                    new SerializeReferenceMenuParam(type, property)));
        }

        /// Collects appropriate types based on managed reference field type and filters. Filters all derive
        private List<Type> GetTypesForManagedReference()
        {
            var fieldType = GetBaseType();
            var derivedTypes = TypeCache.GetTypesDerivedFrom(fieldType);

            // Skips unity engine Objects (because they are not serialized by SerializeReference)
            // Skip abstract classes because they should not be instantiated
            // Skip generic classes because they can not be instantiated
            // Skip types that has no public empty constructors (activator can not create them)    
            return (from type in derivedTypes
                where !type.IsSubclassOf(typeof(UnityEngine.Object))
                where !type.IsAbstract
                where !type.ContainsGenericParameters
                where !type.IsClass || type.GetConstructor(Type.EmptyTypes) != null
                select type).ToList();
        }

        private static void AssignNewInstanceCommand(object objectGenericMenuParameter)
        {
            var parameter = (SerializeReferenceMenuParam)objectGenericMenuParameter;
            var type = parameter.type;
            var property = parameter.property;

            property.SetManagedReferenceToInstance(type);
            property.isExpanded = true;
        }

        private readonly struct SerializeReferenceMenuParam
        {
            public readonly Type type;
            public readonly SerializedProperty property;

            public SerializeReferenceMenuParam(Type type, SerializedProperty property)
            {
                this.type = type;
                this.property = property;
            }
        }
    }

    [CustomPropertyDrawer(typeof(ValueTransformer), true)]
    public class ValueTransformPropertyDrawer : ValueManipulatorBasePropertyDrawerBase
    {
        protected override Type GetBaseType() => typeof(ValueTransformer);
    }

    [CustomPropertyDrawer(typeof(ValueDestination), true)]
    public class ValueDestinationPropertyDrawer : ValueManipulatorBasePropertyDrawerBase
    {
        protected override Type GetBaseType() => typeof(ValueDestination);
    }
}
