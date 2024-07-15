#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker.EditorDesign
{

    [CustomEditor(typeof(ObjectShaker))]
    public class ObjectShakerEditor : Editor
    {
        private MonoScript _script;
        private SerializedProperty _removeFromManagerOnDisable;
        private SerializedProperty _limitMagnitude;
        private SerializedProperty _positionalMagnitudeLimit;
        private SerializedProperty _rotationalMagnitudeLimit;
        private SerializedProperty _shakeOnEnable;
        private void OnEnable()
        {
            _script = MonoScript.FromMonoBehaviour((ObjectShaker)target);
            _removeFromManagerOnDisable = serializedObject.FindProperty("_removeFromManagerOnDisable");
            _limitMagnitude = serializedObject.FindProperty("_limitMagnitude");
            _positionalMagnitudeLimit = serializedObject.FindProperty("_positionalMagnitudeLimit");
            _rotationalMagnitudeLimit = serializedObject.FindProperty("_rotationalMagnitudeLimit");
            _shakeOnEnable = serializedObject.FindProperty("_shakeOnEnable");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            _script = EditorGUILayout.ObjectField("Script:", _script, typeof(MonoScript), false) as MonoScript;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(_removeFromManagerOnDisable, new GUIContent("Remove From Manager On Disable", "True to remove this shakers reference from it's handler when disabled. Useful if you have a lot of these shakers in your scene, but objects are not always enabled."));

            //Limit magnitude.  
            EditorGUILayout.PropertyField(_limitMagnitude, new GUIContent("Limit Magnitude", "True to limit how much magnitude can be applied to this CameraShaker."));
            if (_limitMagnitude.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_positionalMagnitudeLimit, new GUIContent("Positional Magnitude Limit", "How much positional magnitude to limit this CameraShaker to."));
                EditorGUILayout.PropertyField(_rotationalMagnitudeLimit, new GUIContent("Rotational Magnitude Limit", "How much rotational magnitude to limit this CameraShaker to."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_shakeOnEnable, new GUIContent("Shake On Enable", "ShakeData to run when enabled. Leave empty to not use this feature."));
            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif