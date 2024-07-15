#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker.EditorDesign
{

    [CustomEditor(typeof(ShakableCanvas))]
    public class ShakableCanvasEditor : Editor
    {
        private MonoScript _script;
        private SerializedProperty _shakerType;
        private SerializedProperty _useDefaultCameraShaker;
        private SerializedProperty _cameraShaker;
        private SerializedProperty _encapsulateChildren;
        private SerializedProperty _monitorEncapsulation;
        private SerializedProperty _positionalMultiplier;
        private SerializedProperty _rotationalMultiplier;
        private SerializedProperty _randomizeDirections;

        private void OnEnable()
        {
            _script = MonoScript.FromMonoBehaviour((ShakableCanvas)target);
            //ShakableBase.
            _shakerType = serializedObject.FindProperty("_shakerType");
            //ShakableCanvas.
            _positionalMultiplier = serializedObject.FindProperty("_positionalMultiplier");
            _rotationalMultiplier = serializedObject.FindProperty("_rotationalMultiplier");
            _useDefaultCameraShaker = serializedObject.FindProperty("_useDefaultCameraShaker");
            _cameraShaker = serializedObject.FindProperty("_cameraShaker");
            _encapsulateChildren = serializedObject.FindProperty("_encapsulateChildren");
            _monitorEncapsulation = serializedObject.FindProperty("_monitorEncapsulation");
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

            //If using CameraShaker type.
            if ((ShakableBase.ShakerTypes)_shakerType.intValue == ShakableBase.ShakerTypes.CameraShaker)
            {
                EditorGUILayout.PropertyField(_useDefaultCameraShaker, new GUIContent("Use Default CameraShaker", "True to shake when the default camera shaker does. False to specify a camera shaker to monitor."));
                if (_useDefaultCameraShaker.boolValue == false)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_cameraShaker, new GUIContent("Camera Shaker", "Camera shaker to monitor."));
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.PropertyField(_encapsulateChildren, new GUIContent("Encapsulate Children", "True to create a parent object and attach children to it. The parent object will be shaken instead of each individual canvas child. If your direct children move at all this value must be true. Setting value as false may incur extra cost as well."));
            if (_encapsulateChildren.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_monitorEncapsulation, new GUIContent("Monitor Encapsulation", "True to watch for additional children to encapsulate. This may be false if you do not add direct children to this canvas at runtime."));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_positionalMultiplier, new GUIContent("Positional Multiplier", "Positional shakes are multiplied by this value. Lower values will result in a lower positional magnitude."));
            EditorGUILayout.PropertyField(_rotationalMultiplier, new GUIContent("Rotational Multiplier", "Rotational shakes are multiplied by this value. Lower values will result in a lower rotational magnitude."));

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_randomizeDirections, new GUIContent("Randomize Directions", "True to randomly change influence direction when shaking starts."));
            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif