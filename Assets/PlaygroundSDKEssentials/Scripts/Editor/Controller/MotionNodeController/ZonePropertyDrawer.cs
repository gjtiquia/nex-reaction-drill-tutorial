#nullable enable

using System;
using UnityEditor;
using UnityEngine;

namespace Nex.Essentials.MotionNode
{
    [CustomPropertyDrawer(typeof(Zone))]
    public class ZonePropertyDrawer: PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var shapeProperty = property.FindPropertyRelative(nameof(Zone.shape));
            var numFields = (Zone.ZoneShape)shapeProperty.enumValueIndex switch
            {
                Zone.ZoneShape.None => 2, // minValue, maxValue.
                Zone.ZoneShape.Rectangle => 2, // zoneCenter, zoneSize.
                Zone.ZoneShape.Circle => 2, // zoneCenter, zoneRadius.
                Zone.ZoneShape.RoundedRectangle => 3, // zoneCenter, zoneSize, cornerRadius.
                _ => throw new ArgumentOutOfRangeException()
            };
            return EditorGUIUtility.singleLineHeight * (numFields + 1);  // shape.
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            // First we need the label and the shape drop down.
            const float shapeDropdownWidth = 150f;
            const float shapeDropdownLabelWidth = 55f;
            var lineHeight = EditorGUIUtility.singleLineHeight;
            var labelRect = new Rect(position.x, position.y,
                Mathf.Max(position.width - shapeDropdownWidth - shapeDropdownLabelWidth, 10f), lineHeight);
            var shapeDropdownLabelRect = new Rect(labelRect.xMax, position.y, shapeDropdownLabelWidth, lineHeight);
            var shapeDropdownRect = new Rect(shapeDropdownLabelRect.xMax, position.y,
                position.xMax - shapeDropdownLabelRect.xMax, lineHeight);

            var shapeProperty = property.FindPropertyRelative(nameof(Zone.shape));
            var minValueProperty = property.FindPropertyRelative(nameof(Zone.minValue));
            var maxValueProperty = property.FindPropertyRelative(nameof(Zone.maxValue));
            var zoneCenterProperty = property.FindPropertyRelative(nameof(Zone.zoneCenter));
            var zoneSizeProperty = property.FindPropertyRelative(nameof(Zone.zoneSize));
            var zoneRadiusProperty = property.FindPropertyRelative(nameof(Zone.zoneRadius));
            var cornerRadiusProperty = property.FindPropertyRelative(nameof(Zone.cornerRadius));

            EditorGUI.LabelField(labelRect, label);
            EditorGUI.LabelField(shapeDropdownLabelRect, "Shape");
            EditorGUI.PropertyField(shapeDropdownRect, shapeProperty, GUIContent.none);

            const float indent = 16f;
            var propRect = new Rect(position.x + indent, labelRect.yMax, position.width - indent, lineHeight);

            switch ((Zone.ZoneShape)shapeProperty.enumValueIndex)
            {
                case Zone.ZoneShape.None:
                    EditorGUI.PropertyField(propRect, minValueProperty);
                    propRect.y += propRect.height;
                    EditorGUI.PropertyField(propRect, maxValueProperty);
                    break;
                case Zone.ZoneShape.Rectangle:
                    EditorGUI.PropertyField(propRect, zoneCenterProperty);
                    propRect.y += propRect.height;
                    EditorGUI.PropertyField(propRect, zoneSizeProperty);
                    break;
                case Zone.ZoneShape.Circle:
                    EditorGUI.PropertyField(propRect, zoneCenterProperty);
                    propRect.y += propRect.height;
                    EditorGUI.PropertyField(propRect, zoneRadiusProperty);
                    break;
                case Zone.ZoneShape.RoundedRectangle:
                    EditorGUI.PropertyField(propRect, zoneCenterProperty);
                    propRect.y += propRect.height;
                    EditorGUI.PropertyField(propRect, zoneSizeProperty);
                    propRect.y += propRect.height;
                    EditorGUI.PropertyField(propRect, cornerRadiusProperty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            EditorGUI.EndProperty();
        }
    }
}
