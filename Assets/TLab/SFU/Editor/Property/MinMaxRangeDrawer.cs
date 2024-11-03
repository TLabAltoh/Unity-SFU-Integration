using UnityEngine;
using UnityEditor;

namespace TLab.SFU.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        const float kPrefixPaddingRight = 2;
        const float kSpacing = 5;
        const float kAdjust = 15;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            EditorGUI.BeginChangeCheck();

            EditorGUIUtility.labelWidth -= kAdjust;
            EditorGUIUtility.fieldWidth += kAdjust;

            var range = attribute as MinMaxRangeAttribute;
            float minValue = property.vector2Value.x;
            float maxValue = property.vector2Value.y;

            var labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
            EditorGUI.LabelField(labelPosition, label);

            var sliderPosition = new Rect(
                position.x + EditorGUIUtility.labelWidth + kPrefixPaddingRight + EditorGUIUtility.fieldWidth + kSpacing - kAdjust,
                position.y,
                position.width - EditorGUIUtility.labelWidth - 2 * (EditorGUIUtility.fieldWidth + kSpacing) - kPrefixPaddingRight + 2 * kAdjust,
                position.height
            );
            EditorGUI.MinMaxSlider(sliderPosition, ref minValue, ref maxValue, range.min, range.max);

            var minPosition = new Rect(position.x + EditorGUIUtility.labelWidth + kPrefixPaddingRight, position.y, EditorGUIUtility.fieldWidth, position.height);
            minValue = EditorGUI.FloatField(minPosition, minValue);

            var maxPosition = new Rect(position.xMax - EditorGUIUtility.fieldWidth, position.y, EditorGUIUtility.fieldWidth, position.height);
            maxValue = EditorGUI.FloatField(maxPosition, maxValue);

            if (EditorGUI.EndChangeCheck())
                property.vector2Value = new Vector2(minValue, maxValue);

            EditorGUI.EndProperty();
        }
    }
}
