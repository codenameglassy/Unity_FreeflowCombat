using FIMSpace.FEditor;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.BipedAnimator
{
    [UnityEditor.CustomEditor(typeof(LeaningAnimator))]
    [CanEditMultipleObjects]
    public partial class LeaningAnimatorEditor : UnityEditor.Editor
    {
        public LeaningAnimator Get { get { if (_get == null) _get = (LeaningAnimator)target; return _get; } }
        private LeaningAnimator _get;

        private SerializedProperty sp_Leaning;

        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        private void OnEnable()
        {
            sp_Leaning = serializedObject.FindProperty("Leaning");
        }

        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "LeaningAnimator");

            serializedObject.Update();

            LeaningProcessor.Editor_DrawTweakFullGUI(sp_Leaning, ref Get._EditorDrawSetup, (LeaningAnimator)target);

            serializedObject.ApplyModifiedProperties();


            if (Application.isPlaying)
            {
                GUILayout.Space(4);
                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyleH);
                EditorGUILayout.LabelField("Current Speed: " + System.Math.Round(Get.Parameters.AverageVelocity, 2));
                EditorGUILayout.LabelField("Current Rotation Speed: " + System.Math.Round(Get.Parameters.AverageAngularVelocity, 2));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Is Grounded: " + (Get.CheckIfIsGrounded));
                EditorGUILayout.LabelField("Accelerating: " + (Get.CheckIfIsAcccelerating));
                EditorGUILayout.EndHorizontal();


                if (!string.IsNullOrWhiteSpace(Get.Parameters.FadeOffLeaningParam))
                {
                    Animator anim = Get.GetComponentInChildren<Animator>();
                    if (anim) EditorGUILayout.LabelField("Off Parameter = " + anim.GetBool(Get.Parameters.FadeOffLeaningParam));
                }

                EditorGUILayout.LabelField("Main Blend: " + (Get.Parameters.GetFullBlend));

                Get.Parameters._EditorDebugGUI();
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Space(4);
                EditorGUILayout.BeginVertical(FGUI_Resources.BGInBoxStyleH);
                EditorGUILayout.LabelField("In playmode there will be helpful statistics of object speed", EditorStyles.helpBox);
                EditorGUILayout.EndVertical();
            }
        }

    }
}
