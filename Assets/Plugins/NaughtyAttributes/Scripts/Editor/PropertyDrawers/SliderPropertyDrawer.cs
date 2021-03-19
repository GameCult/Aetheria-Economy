﻿using UnityEngine;
using UnityEditor;

namespace NaughtyAttributes.Editor
{
    [PropertyDrawer(typeof(SliderAttribute))]
    public class SliderPropertyDrawer : PropertyDrawer
    {
        public override void DrawProperty(SerializedProperty property)
        {
            SliderAttribute sliderAttribute = PropertyUtility.GetAttributes<SliderAttribute>(property)[0];

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                EditorGUILayout.IntSlider(property, (int)sliderAttribute.MinValue, (int)sliderAttribute.MaxValue);
            }
            else if (property.propertyType == SerializedPropertyType.Float)
            {
                EditorGUILayout.Slider(property, sliderAttribute.MinValue, sliderAttribute.MaxValue);
            }
            else
            {
                string warning = sliderAttribute.GetType().Name + " can be used only on int or float fields";
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
                Debug.LogWarning(warning, PropertyUtility.GetTargetObject(property));

                EditorGUILayout.PropertyField(property, true);
            }
        }
    }
}
