using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
    [CustomPropertyDrawer(typeof(FPD_SuffixAttribute))]
    public class FPD_Suffix : PropertyDrawer
    {
        FPD_SuffixAttribute Attribute { get { return ((FPD_SuffixAttribute)base.attribute); } }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            float sliderVal = property.floatValue;

            GUIContent suff = new GUIContent(Attribute.Suffix);
            Vector2 fieldS = EditorStyles.label.CalcSize(suff);

            float fieldSize = 34 + fieldS.x + Attribute.widerField;
            var percField = new Rect(position.x + position.width - fieldSize + 5, position.y, fieldSize, position.height);
            Rect floatField = position;

            bool editable = Attribute.editableValue;
            if (GUI.enabled == false) editable = false;

            if (editable)
            {
                floatField = new Rect(position.x + position.width - fieldSize + 2, position.y, fieldSize - (fieldS.x + 4), position.height);
                percField.position = new Vector2(position.x + position.width - fieldS.x, percField.position.y);
                percField.width = fieldS.x;
            }

            position.width -= fieldSize + 3;
            sliderVal = GUI.HorizontalSlider(position, property.floatValue, Attribute.Min, Attribute.Max);

            float pre, value;
            int indent;

            indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            switch (Attribute.Mode)
            {
                case FPD_SuffixAttribute.SuffixMode.From0to100:

                    if (!editable)
                        EditorGUI.LabelField(percField, Mathf.Round(sliderVal / Attribute.Max * 100f).ToString() + Attribute.Suffix);
                    else
                    {
                        pre = Mathf.Round(sliderVal / Attribute.Max * 100f);
                        value = EditorGUI.FloatField(floatField, Mathf.Round(sliderVal / Attribute.Max * 100f));
                        if (value != pre) sliderVal = value / 100f;

                        EditorGUI.LabelField(percField, Attribute.Suffix);
                    }

                    break;

                case FPD_SuffixAttribute.SuffixMode.PercentageUnclamped:

                    if (!editable)
                        EditorGUI.LabelField(percField, Mathf.Round(sliderVal * 100f).ToString() + Attribute.Suffix);
                    else
                    {
                        pre = Mathf.Round(sliderVal * 100f);
                        value = EditorGUI.FloatField(floatField, Mathf.Round(sliderVal * 100f));
                        if (value != pre) sliderVal = value / 100f;

                        EditorGUI.LabelField(percField, Attribute.Suffix);
                    }

                    break;


                case FPD_SuffixAttribute.SuffixMode.FromMinToMax:

                    pre = sliderVal;
                    value = EditorGUI.FloatField(floatField, sliderVal);
                    if (value != pre) sliderVal = value;

                    EditorGUI.LabelField(percField, Attribute.Suffix);

                    break;

                case FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded:

                    pre = Mathf.Round(sliderVal);
                    value = EditorGUI.FloatField(floatField, Mathf.Round(sliderVal));
                    if (value != pre) sliderVal = value;

                    EditorGUI.LabelField(percField, Attribute.Suffix);

                    break;
            }

            property.floatValue = sliderVal;
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();

        }
    }

}

