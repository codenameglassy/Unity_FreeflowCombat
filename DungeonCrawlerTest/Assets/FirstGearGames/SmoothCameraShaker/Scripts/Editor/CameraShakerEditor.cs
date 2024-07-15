#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker.EditorDesign
{

    [CustomEditor(typeof(CameraShaker))]
    public class CameraShakerEditor : Editor
    {
        private MonoScript _script;
        private SerializedProperty _shakeTechnique;
        private SerializedProperty _makeDefaultOnEnable;
        private SerializedProperty _limitMagnitude;
        private SerializedProperty _positionalMagnitudeLimit;
        private SerializedProperty _rotationalMagnitudeLimit;

        private void OnEnable()
        {
            _script = MonoScript.FromMonoBehaviour((CameraShaker)target); 
            _shakeTechnique = serializedObject.FindProperty("_shakeTechnique");
            _makeDefaultOnEnable = serializedObject.FindProperty("_makeDefaultOnEnable");
            _limitMagnitude = serializedObject.FindProperty("_limitMagnitude");
            _positionalMagnitudeLimit = serializedObject.FindProperty("_positionalMagnitudeLimit");
            _rotationalMagnitudeLimit = serializedObject.FindProperty("_rotationalMagnitudeLimit");
        }
        public override void OnInspectorGUI()
        {

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            _script = EditorGUILayout.ObjectField("Script:", _script, typeof(MonoScript), false) as MonoScript;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(_shakeTechnique, new GUIContent("Shake Technique", "Sets the shake technique to use. Changing this value while shakes are occurring may create unwanted results."));
            EditorGUILayout.PropertyField(_makeDefaultOnEnable, new GUIContent("Make Default On Enable", "True for this CameraShaker to be set as the default shaker when enabled."));
            //Limit magnitude.  
            EditorGUILayout.PropertyField(_limitMagnitude, new GUIContent("Limit Magnitude", "True to limit how much magnitude can be applied to this CameraShaker."));
            if (_limitMagnitude.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_positionalMagnitudeLimit, new GUIContent("Positional Magnitude Limit", "How much positional magnitude to limit this CameraShaker to."));
                EditorGUILayout.PropertyField(_rotationalMagnitudeLimit, new GUIContent("Rotational Magnitude Limit", "How much rotational magnitude to limit this CameraShaker to."));
                EditorGUI.indentLevel--;
            }
            //EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif