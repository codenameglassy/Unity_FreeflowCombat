#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker.EditorDesign
{

    [CustomEditor(typeof(ShakeData))]
    public class ShakeDataEditor : Editor
    {
        private MonoScript _script;

        private SerializedProperty _scaledTime;
        private SerializedProperty _shakeCameras;
        private SerializedProperty _shakeCanvases;
        private SerializedProperty _shakeObjects;
        private SerializedProperty _magnitude;
        private SerializedProperty _magnitudeNoise;
        private SerializedProperty _magnitudeCurve;
        private SerializedProperty _roughness;
        private SerializedProperty _roughnessNoise;
        private SerializedProperty _roughnessCurve;
        private SerializedProperty _totalDuration;
        private SerializedProperty _unlimitedDuration;
        private SerializedProperty _fadeInDuration;
        private SerializedProperty _fadeOutDuration;
        private SerializedProperty _positionalInfluence;
        private SerializedProperty _positionalInverts;
        private SerializedProperty _rotationalInfluence;
        private SerializedProperty _rotationalInverts;
        private SerializedProperty _randomSeed;

        private void OnEnable()
        {
            _script = MonoScript.FromScriptableObject((ShakeData)target);

            //Shakables.
            _shakeCameras = serializedObject.FindProperty("_shakeCameras");
            _shakeCanvases = serializedObject.FindProperty("_shakeCanvases");
            _shakeObjects = serializedObject.FindProperty("_shakeObjects");
            //Timing.
            _scaledTime = serializedObject.FindProperty("_scaledTime");
            _unlimitedDuration = serializedObject.FindProperty("_unlimitedDuration");
            _totalDuration = serializedObject.FindProperty("_totalDuration");
            _fadeInDuration = serializedObject.FindProperty("_fadeInDuration");
            _fadeOutDuration = serializedObject.FindProperty("_fadeOutDuration");
            //Force.
            _magnitude = serializedObject.FindProperty("_magnitude");
            _magnitudeNoise = serializedObject.FindProperty("_magnitudeNoise");
            _magnitudeCurve = serializedObject.FindProperty("_magnitudeCurve");
            _roughness = serializedObject.FindProperty("_roughness");
            _roughnessNoise = serializedObject.FindProperty("_roughnessNoise");
            _roughnessCurve = serializedObject.FindProperty("_roughnessCurve");
            //Influence.
            _positionalInfluence = serializedObject.FindProperty("_positionalInfluence");
            _positionalInverts = serializedObject.FindProperty("_positionalInverts");
            _rotationalInfluence = serializedObject.FindProperty("_rotationalInfluence");
            _rotationalInverts = serializedObject.FindProperty("_rotationalInverts");
            //Seed.
            _randomSeed = serializedObject.FindProperty("_randomSeed");
        }

        public override void OnInspectorGUI()
        {
            ShakeData data = (ShakeData)target;

            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(true);
            _script = EditorGUILayout.ObjectField("Script:", _script, typeof(MonoScript), false) as MonoScript;
            EditorGUI.EndDisabledGroup();

            //Scaled time and unlimited duraiton.
            EditorGUILayout.PropertyField(_scaledTime, new GUIContent("Scaled Time", "True to use scaled time, false to use unscaled."));
            EditorGUILayout.Space();

            //Affected shakables.
            EditorGUILayout.LabelField("Shakables To Affect");
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_shakeCameras, new GUIContent("Cameras", "True to shake cameras."));
            EditorGUILayout.PropertyField(_shakeCanvases, new GUIContent("Canvases", "True to shake canvases. Canvases must have a ShakableCanvas component attached."));
            EditorGUILayout.PropertyField(_shakeObjects, new GUIContent("Objects", "True to shake objects such as rigidbodies. Rigidbodies must have a ShakableRigidbody or ShakableRigidbody2D component attached."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_unlimitedDuration, new GUIContent("Unlimited Duration", "True to shake until stopped."));
            //Total duration.
            if (_unlimitedDuration.boolValue == false)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_totalDuration, new GUIContent("Total Duration", "How long the shake will last. If duration is less than fade out and fade in time combined then duration is adjusted to the total of those values."));
                EditorGUI.indentLevel--;
            }

            //Fade in/out.
            EditorGUILayout.PropertyField(_fadeInDuration, new GUIContent("Fade In Duration", "How long after the start of the shake until it reaches full magnitude. Used to ease into shakes. Works independently from curves. This value is not in addition to TotalDuration."));
            EditorGUILayout.PropertyField(_fadeOutDuration, new GUIContent("Fade Out Duration", "How long at the end of the shake to ease out of shake. Works independently from curves. This value is not in addition to TotalDuration."));
            EditorGUILayout.Space();

            //Magnitude
            EditorGUILayout.PropertyField(_magnitude, new GUIContent("Magnitude", "A multiplier to apply towards configured settings."));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_magnitudeNoise, new GUIContent("Magnitude Noise", "Larger noise values will result in more drastic ever-changing magnitude levels during the shake."));
            EditorGUILayout.PropertyField(_magnitudeCurve, new GUIContent("Magnitude Curve", "Percentage curve applied to magnitude over the shake duration."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            //Roughness.
            EditorGUILayout.PropertyField(_roughness, new GUIContent("Roughness", "How quickly to transition between shake offsets. Higher values will result in more violent shakes."));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_roughnessNoise, new GUIContent("Roughness Noise", "Larger noise values will result in more drastic ever-changing roughness levels during the shake."));
            EditorGUILayout.PropertyField(_roughnessCurve, new GUIContent("Roughness Curve", "Percentage curve applied to roughness over the shake duration."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            //Influence.
            EditorGUILayout.PropertyField(_positionalInfluence, new GUIContent("Positional Influence", "Values in either sign which the shake positioning will occur."));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_positionalInverts, new GUIContent("Invertible Axes", "Positional axes which may be randomly inverted when this ShakeData is instanced."));
            EditorGUI.indentLevel--;
            EditorGUILayout.PropertyField(_rotationalInfluence, new GUIContent("Rotational Influence", "Values in either sign which the shake rotation will occur."));
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_rotationalInverts, new GUIContent("Invertible Axes", "Rotational axes which may be randomly inverted when this ShakeData is instanced."));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();

            //RandomSeed.
            EditorGUILayout.PropertyField(_randomSeed, new GUIContent("Random Seed", "While checked a new starting position and direction is used with every shake; shakes are more randomized. If unchecked shakes are guaranteed to start at the same position, and move the same direction with every shake; configured curves and noise are still applied."));

            if (EditorGUI.EndChangeCheck())
            {
                data.PositionalInverts = (InvertibleAxes)_positionalInverts.intValue;
                data.RotationalInverts = (InvertibleAxes)_rotationalInverts.intValue;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif