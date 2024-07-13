using UnityEditor;
using UnityEngine;

namespace FIMSpace.FEditor
{
    [CustomPropertyDrawer(typeof(FPD_HeaderAttribute))]
    public class FPD_Header : DecoratorDrawer
    {
        public static GUIStyle HeaderStyle { get { if (_headerStyle == null) { _headerStyle = new GUIStyle(EditorStyles.helpBox); _headerStyle.fontStyle = FontStyle.Bold; _headerStyle.alignment = TextAnchor.MiddleCenter; _headerStyle.fontSize = 11;  } return _headerStyle; } }
        private static GUIStyle _headerStyle;

        public override void OnGUI(Rect position)
        {
            FPD_HeaderAttribute att = (FPD_HeaderAttribute)base.attribute;

            Rect pos = position; pos.height = base.GetHeight() + att.Height;

            pos.y += att.UpperPadding;

            GUI.Label(pos, new GUIContent(att.HeaderText), HeaderStyle);
        }

        public override float GetHeight()
        {
            FPD_HeaderAttribute att = (FPD_HeaderAttribute)base.attribute;
            return base.GetHeight() + att.Height + att.BottomPadding + att.UpperPadding;
        }
    }

}

