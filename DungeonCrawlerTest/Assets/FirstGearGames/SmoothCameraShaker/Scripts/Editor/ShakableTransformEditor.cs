#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker.EditorDesign
{

    [CustomEditor(typeof(ShakableTransform))]
    public class ShakableTransformEditor : Editor
    {
        private MonoScript _script;
        private SerializedProperty _shakerType;
        private SerializedProperty _positionalMultiplier;
        private SerializedProperty _rotationalMultiplier;
        private SerializedProperty _requireInView;
        private SerializedProperty _includeChildren;
        private SerializedProperty _ignoreSelf;
        private SerializedProperty _includeInactive;
        private SerializedProperty _localizeShake;
        private SerializedProperty _randomizeDirections;

        private void OnEnable()
        {
            _script = MonoScript.FromMonoBehaviour((ShakableTransform)target);
            //ShakableBase.
            _shakerType = serializedObject.FindProperty("_shakerType");
            //ShakableTransform
            _positionalMultiplier = serializedObject.FindProperty("_positionalMultiplier");
            _rotationalMultiplier = serializedObject.FindProperty("_rotationalMultiplier");
            _requireInView = serializedObject.FindProperty("_requireInView");
            _includeChildren = serializedObject.FindProperty("_includeChildren");
            _ignoreSelf = serializedObject.FindProperty("_ignoreSelf");
            _includeInactive = serializedObject.FindProperty("_includeInactive");
            _localizeShake = serializedObject.FindProperty("_localizeShake");
            _randomizeDirections = serializedObject.FindProperty("_randomizeDirections");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            _script = EditorGUILayout.ObjectField("Script:", _script, typeof(MonoScript), false) as MonoScript;
            EditorGUI.EndDisabledGroup();

            //Only allow changing when not in play mode. This will be done for all scripts once their editors are made.
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            EditorGUILayout.PropertyField(_shakerType, new GUIContent("Shaker Type", "Shaker type to use. CameraShaker will subscribe to your current or otherwise configured CameraShaker. ObjectShaker will subscribe to the first ObjectShaker found on or in parented objects."));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_positionalMultiplier, new GUIContent("Positional Multiplier", "Positional shakes are multiplied by this value. Lower values will result in a lower positional magnitude."));
            EditorGUILayout.PropertyField(_rotationalMultiplier, new GUIContent("Rotational Multiplier", "Rotational shakes are multiplied by this value. Lower values will result in a lower rotational magnitude."));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_requireInView, new GUIContent("Require In View", "Only shake when in view of a camera."));
            EditorGUILayout.PropertyField(_includeChildren, new GUIContent("Include Children", "True to find transforms in children too. This allows you to use one ShakableTransform on the parent if all children transforms should shake as well."));
            if (_includeChildren.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_ignoreSelf, new GUIContent("Ignore Self", "True to ignore the transform this component resides, and only shake children."));
                EditorGUILayout.PropertyField(_includeInactive, new GUIContent("Include Inactive", "True to also find inactive children."));
                EditorGUI.indentLevel--;
            }

            //EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_localizeShake, new GUIContent("Localize Shake", "True to convert influences to local space before applying."));
            EditorGUILayout.PropertyField(_randomizeDirections, new GUIContent("Randomize Directions", "True to randomly change influence direction when shaking starts."));
            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif