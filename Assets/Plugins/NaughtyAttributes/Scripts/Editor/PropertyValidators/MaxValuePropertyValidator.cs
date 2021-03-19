﻿using UnityEditor;
using UnityEngine;

namespace NaughtyAttributes.Editor
{
    [PropertyValidator(typeof(MaxValueAttribute))]
    public class MaxValuePropertyValidator : PropertyValidator
    {
        public override void ValidateProperty(SerializedProperty property)
        {
            MaxValueAttribute maxValueAttribute = PropertyUtility.GetAttributes<MaxValueAttribute>(property)[0];

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                if (property.intValue > maxValueAttribute.MaxValue)
                {
                    property.intValue = (int)maxValueAttribute.MaxValue;
                }
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                if (property.floatValue > maxValueAttribute.MaxValue)
                {
                    property.floatValue = maxValueAttribute.MaxValue;
                }
            }
            else
            {
                string warning = maxValueAttribute.GetType().Name + " can be used only on int or float fields";
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
                Debug.LogWarning(warning, PropertyUtility.GetTargetObject(property));
            }
        }
    }
}
