using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(FPD_FixedCurveWindowAttribute))]
public class FPD_FixedCurveWindow : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        FPD_FixedCurveWindowAttribute att = attribute as FPD_FixedCurveWindowAttribute;

        if (property.propertyType == SerializedPropertyType.AnimationCurve)
        {
            EditorGUI.CurveField(position, property, att.Color, new Rect(att.StartTime, att.StartValue, att.EndTime - att.StartTime , att.EndValue - att.StartValue ), label );
        }
    }
}
