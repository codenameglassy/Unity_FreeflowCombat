using FirstGearGames.Utilities.Editors;
using FirstGearGames.Utilities.Maths;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstGearGames.SmoothCameraShaker
{

    [CreateAssetMenu(fileName = "NewCameraShake", menuName = "FirstGearGames/Smooth Camera Shaker/Shake Data", order = 1)]
    public class ShakeData : ScriptableObject
    {
        #region Public.
        /* Be sure to add [System.NonSerialized] to public fields
        * which should not be serialized. ScriptableObjects have a tendacy
        * of serializing fields that aren't explicitly marked as non serialized. */

        /// <summary>
        /// True if this data is instanced.
        /// </summary>
        public bool Instanced { get; private set; }
        #endregion

        #region Serialized.
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to use scaled time, false to use unscaled.")]
        [SerializeField]
        private bool _scaledTime = true;
        /// <summary>
        /// True to use scaled time, false to use unscaled.
        /// </summary>
        public bool ScaledTime
        {
            get { return _scaledTime; }
            private set { _scaledTime = value; }
        }

        #region Shakables.
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to shake cameras.")]
        [SerializeField]
        private bool _shakeCameras = true;
        /// <summary>
        /// True to shake cameras.
        /// </summary>
        public bool ShakeCameras
        {
            get { return _shakeCameras; }
            private set { _shakeCameras = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to shake canvases. Canvases must have a ShakableCanvas component attached.")]
        [SerializeField]
        private bool _shakeCanvases = true;
        /// <summary>
        /// True to shake canvases. Canvases must have a ShakableCanvas component attached.
        /// </summary>
        public bool ShakeCanvases
        {
            get { return _shakeCanvases; }
            private set { _shakeCanvases = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to shake objects such as rigidbodies. Rigidbodies must have a ShakableRigidbody or ShakableRigidbody2D component attached.")]
        [FormerlySerializedAs("_shakeRigidbodies")]
        [SerializeField]
        private bool _shakeObjects = true;
        /// <summary>
        /// True to shake objects such as rigidbodies. Rigidbodies must have a ShakableRigidbody or ShakableRigidbody2D component attached.
        /// </summary>
        public bool ShakeObjects
        {
            get { return _shakeObjects; }
            private set { _shakeObjects = value; }
        }
        #endregion

        /// <summary>
        /// Percentage of a single iteration to perform. Using 1f would be the same as SingleCompletion, where 0.5f would only complete half of a SingleCompletion.
        /// </summary>
        [Tooltip("Percentage of a single iteration to perform. Using 1f would be the same as SingleCompletion, where 0.5f would only complete half of a SingleCompletion.")]
        [SerializeField]
        private float _iterationPercent = 1f;
        /// <summary>
        /// Percentage of a single iteration to perform. Using 1f would be the same as SingleCompletion, where 0.5f would only complete half of a SingleCompletion.
        /// </summary>
        public float IterationPercent
        {
            get { return _iterationPercent; }
            private set { _iterationPercent = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("True to shake until stopped.")]
        [SerializeField]
        private bool _unlimitedDuration = false;
        /// <summary>
        /// True to shake until stopped.
        /// </summary>
        public bool UnlimitedDuration
        {
            get { return _unlimitedDuration; }
            private set { _unlimitedDuration = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("How long the shake will last. If duration is less than fade out and fade in time combined then duration is adjusted to the total of those values.")]
        [SerializeField]
        private float _totalDuration = 4f;
        /// <summary>
        /// How long the shake will last. If duration is less than fade out and fade in time combined then duration is adjusted to the total of those values.
        /// </summary>
        public float TotalDuration
        {
            get { return _totalDuration; }
            private set { _totalDuration = value; }
        }
        /// <summary>
        /// Sets TotalDuration to a value and adjust it if neccesary.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="alterUnlimitedDuration">True to change unlimited duration based on the new value.</param>
        private void ValidateTotalDuration(float value, bool alterUnlimitedDuration)
        {
            if (alterUnlimitedDuration)
                UnlimitedDuration = (value < 0f);

            if (!UnlimitedDuration)
                TotalDuration = Mathf.Max(value, FadeInDuration + FadeOutDuration);
            else
                TotalDuration = -1f;
        }

        /// <summary>
        /// 
        /// </summary>
        [Tooltip("How long after the start of the shake until it reaches full magnitude. Used to ease into shakes. Works independently from curves. This value is not in addition to TotalDuration.")]
        [SerializeField]
        private float _fadeInDuration = 0.5f;
        /// <summary>
        /// How long after the start of the shake until it reaches full magnitude. Used to ease into shakes. Works independently from curves. This value is not in addition to TotalDuration.
        /// </summary>
        public float FadeInDuration
        {
            get { return _fadeInDuration; }
            private set { _fadeInDuration = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("How long at the end of the shake to ease out of shake. Works independently from curves. This value is not in addition to TotalDuration.")]
        [SerializeField]
        private float _fadeOutDuration = 0.5f;
        /// <summary>
        /// How long at the end of the shake to ease out of shake. Works independently from curves. This value is not in addition to TotalDuration.
        /// </summary>
        public float FadeOutDuration
        {
            get { return _fadeOutDuration; }
            private set { _fadeOutDuration = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("A multiplier to apply towards configured settings.")]
        [SerializeField]
        private float _magnitude = 1f;
        /// <summary>
        /// A multiplier to apply towards configured settings.
        /// </summary>
        public float Magnitude
        {
            get { return _magnitude; }
            private set { _magnitude = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Larger noise values will result in more drastic ever-changing magnitude levels during the shake.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _magnitudeNoise = 0.1f;
        /// <summary>
        /// Larger noise values will result in more drastic ever-changing magnitude levels during the shake.
        /// </summary>
        public float MagnitudeNoise
        {
            get { return _magnitudeNoise; }
            private set { _magnitudeNoise = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Percentage curve applied to magnitude over the shake duration.")]
        [SerializeField]
        private AnimationCurve _magnitudeCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0f, 1f), new Keyframe(1f, 1f) });
        /// <summary>
        /// Percentage curve applied to magnitude over the shake duration.
        /// </summary>
        public AnimationCurve MagnitudeCurve
        {
            get { return _magnitudeCurve; }
            private set { _magnitudeCurve = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("How quickly to transition between shake offsets. Higher values will result in more violent shakes.")]
        [SerializeField]
        private float _roughness = 7.5f;
        /// <summary>
        /// How quickly to transition between shake offsets. Higher values will result in more violent shakes.
        /// </summary>
        public float Roughness
        {
            get { return _roughness; }
            private set { _roughness = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Larger noise values will result in more drastic ever-changing roughness levels during the shake.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float _roughnessNoise = 0.3f;
        /// <summary>
        /// Larger noise values will result in more drastic ever-changing roughness levels during the shake.
        /// </summary>
        public float RoughnessNoise
        {
            get { return _roughnessNoise; }
            private set { _roughnessNoise = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Percentage curve applied to roughness over the shake duration.")]
        [SerializeField]
        private AnimationCurve _roughnessCurve = new AnimationCurve(new Keyframe[] { new Keyframe(0f, 1f), new Keyframe(1f, 1f) });
        /// <summary>
        /// Percentage curve applied to roughness over the total duration.
        /// </summary>
        public AnimationCurve RoughnessCurve
        {
            get { return _roughnessCurve; }
            private set { _roughnessCurve = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Values in either sign which the shake positioning will occur.")]
        [SerializeField]
        private Vector3 _positionalInfluence = new Vector3(1f, 1f, 0f);
        /// <summary>
        /// Values in either sign which the shake positioning will occur.
        /// </summary>
        public Vector3 PositionalInfluence
        {
            get { return _positionalInfluence; }
            private set { _positionalInfluence = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Positional axes which may be randomly inverted when this ShakeData is instanced.")]
        [SerializeField]
        [BitMask(typeof(InvertibleAxes))]
        private InvertibleAxes _positionalInverts;
        /// <summary>
        /// Positional axes which may be randomly inverted when this ShakeData is instanced.
        /// </summary>
        public InvertibleAxes PositionalInverts
        {
            get { return _positionalInverts; }
            set { _positionalInverts = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Values in either sign which the shake rotation will occur.")]
        [SerializeField]
        private Vector3 _rotationalInfluence = new Vector3(0f, 0f, 1f);
        /// <summary>
        /// Values in either sign which the shake rotation will occur.
        /// </summary>
        public Vector3 RotationalInfluence
        {
            get { return _rotationalInfluence; }
            private set { _rotationalInfluence = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Rotational axes which may be randomly inverted when this ShakeData is instanced.")]
        [SerializeField]
        [BitMask(typeof(InvertibleAxes))]
        private InvertibleAxes _rotationalInverts = 0;
        /// <summary>
        /// Rotational axes which may be randomly inverted when this ShakeData is instanced.
        /// </summary>
        public InvertibleAxes RotationalInverts
        {
            get { return _rotationalInverts; }
            set { _rotationalInverts = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("While checked a new starting position and direction is used with every shake; shakes are more randomized. If unchecked shakes are guaranteed to start at the same position, and move the same direction with every shake; configured curves and noise are still applied.")]
        [SerializeField]
        private bool _randomSeed = true;
        /// <summary>
        /// While checked a new starting position and direction is used with every shake; shakes are more randomized. If unchecked shakes are guaranteed to start at the same position, and move the same direction with every shake; configured curves and noise are still applied.
        /// </summary>
        public bool RandomSeed
        {
            get { return _randomSeed; }
            private set { _randomSeed = value; }
        }
        #endregion

        #region Private.
        /* Be sure to add [System.NonSerialized] to private fields
         * which should not be serialized. ScriptableObjects have a tendacy
         * of serializing fields that aren't explicitly marked as non serialized. */
        #endregion

        #region Const.
        /// <summary>
        /// Text to display when an action cannot be performed due to the ShakeData not being instanced.
        /// </summary>
        private const string ACTION_INSTANCE_REQUIRED = "ShakeData is not instanced. You must use ShakeData.CreateInstance to create an instance before using this action.";
        /// <summary>
        /// Minimum time to fade a shake in to force smoothing.
        /// </summary>
        private const float SMOOTH_IN_DURATION = 0.05f;
        /// <summary>
        /// Minimum time to fade a shake out to force smoothing.
        /// </summary>
        private const float SMOOTH_OUT_DURATION = 0.125f;
        #endregion

        /// <summary>
        /// Initializes this data to ensure values make sense. This method is for internal use only.
        /// </summary>
        internal void Initialize()
        {
            Magnitude = Mathf.Max(0.01f, Magnitude);
            Roughness = Mathf.Max(0.01f, Roughness);
            SetFadeInDuration(FadeInDuration);
            SetFadeOutDuration(FadeOutDuration);
            ValidateTotalDuration(TotalDuration, false);
        }

        #region API.
        /// <summary>
        /// Sets a new fade out duration.
        /// </summary>
        /// <param name="value">New fade out duration.</param>
        public void SetFadeOutDuration(float value)
        {
            if (!InstancedWithDebug())
                return;

            /* Code not used as it needs further testing. Additionally,
             * results acheived via this modification likely won't prove noticeable. */
            //bool curveFadesOut;
            ////Only compare against curve if has multiple keys.
            //if (MagnitudeCurve.length > 1)
            //{
            //    float curveDuration  = MagnitudeCurve.keys[MagnitudeCurve.length - 1].time;
            //    float endPercent = MagnitudeCurve.Evaluate(curveDuration);
            //    //Consider 5% or less at end fading out.
            //    curveFadesOut = (endPercent <= 0.05f);
            //}
            ////Not enough curve data.
            //else
            //{
            //    curveFadesOut = false;
            //}

            ///* If curve already fades out then use whatever value
            // * is set. */
            //if (curveFadesOut)
            //    FadeOutDuration = value;
            ///* If curve does not fade out then determine if
            // * fading in needs to be applied. */
            //else
            //    FadeOutDuration = Mathf.Max(SMOOTH_OUT_DURATION, value);

            FadeOutDuration = Mathf.Max(SMOOTH_OUT_DURATION, value);
        }


        /// <summary>
        /// Sets a new fade in duration.
        /// </summary>
        /// <param name="value">New fade in duration.</param>
        public void SetFadeInDuration(float value)
        {
            if (!InstancedWithDebug())
                return;

            /* Code not used as it needs further testing. Additionally,
            * results acheived via this modification likely won't prove noticeable. */
            //bool curveFadesIn;
            ////Only compare against curve if has multiple keys.
            //if (MagnitudeCurve.length > 1)
            //{
            //    float startPercent = MagnitudeCurve.Evaluate(0f);
            //    //Consider 10% or less at start fading in.
            //    curveFadesIn = (startPercent <= 0.1f);
            //}
            ////Not enough curve data.
            //else
            //{
            //    curveFadesIn = false;
            //}

            ///* If curve already fades in then use whatever value
            // * is set. */
            //if (curveFadesIn)
            //    FadeInDuration = value;
            ///* If curve does not fade in then determine if
            // * fading in needs to be applied. */
            //else
            //    FadeInDuration = Mathf.Max(SMOOTH_IN_DURATION, value);

            FadeInDuration = Mathf.Max(SMOOTH_IN_DURATION, value);
        }


        /// <summary>
        /// Sets a new TotalDuration value. Setting this value to 0 or greater removes unlimited duration; just as setting it to less than 0 sets unlimited duration.
        /// </summary>
        /// <param name="value">New total duration. Using values 0f and lower will make duration unlimited.</param>
        public void SetTotalDuration(float value)
        {
            if (!InstancedWithDebug())
                return;
            ValidateTotalDuration(value, true);
        }

        /// <summary>
        /// Sets a new ShakeCameras value.
        /// </summary>
        /// <param name="value"></param>
        public void SetShakeCameras(bool value)
        {
            if (!InstancedWithDebug())
                return;
            ShakeCameras = value;
        }

        /// <summary>
        /// Sets a new ShakeCanvases value.
        /// </summary>
        /// <param name="value"></param>
        public void SetShakeCanvases(bool value)
        {
            if (!InstancedWithDebug())
                return;
            ShakeCanvases = value;
        }

        /// <summary>
        /// Sets a new ShakeRigidbodies value.
        /// </summary>
        /// <param name="value"></param>
        public void SetShakeRigidbodies(bool value)
        {
            if (!InstancedWithDebug())
                return;
            ShakeObjects = value;
        }

        /// <summary>
        /// Creates and returns an instance of this data.
        /// </summary>
        /// <returns></returns>
        public ShakeData CreateInstance()
        {
            ShakeData data = ScriptableObject.CreateInstance<ShakeData>();
            data.SetInstancedWithProperties(ScaledTime, ShakeCameras, ShakeCanvases, ShakeObjects, UnlimitedDuration, Magnitude, MagnitudeNoise, MagnitudeCurve,
                Roughness, RoughnessNoise, RoughnessCurve, TotalDuration, FadeInDuration, FadeOutDuration,
                PositionalInfluence, PositionalInverts, RotationalInfluence, RotationalInverts, RandomSeed);

            return data;
        }

        /// <summary>
        /// Inversts specified positional axes. Using this in the middle of a shake may create jarring the next frame.
        /// </summary>
        /// <param name="axes">Axes to invert.</param>
        public void InvertPositionalAxes(InvertibleAxes axes)
        {
            if (axes.Contains(InvertibleAxes.X))
                _positionalInfluence.x *= -1f;
            if (axes.Contains(InvertibleAxes.Y))
                _positionalInfluence.y *= -1f;
            if (axes.Contains(InvertibleAxes.Z))
                _positionalInfluence.z *= -1f;
        }
        /// <summary>
        /// Inverts specified rotational axes. Using this in the middle of a shake may create jarring the next frame.
        /// </summary>
        /// <param name="axes">Axes to invert.</param>
        public void InvertRotationalAxes(InvertibleAxes axes)
        {
            if (axes.Contains(InvertibleAxes.X))
                _rotationalInfluence.x *= -1f;
            if (axes.Contains(InvertibleAxes.Y))
                _rotationalInfluence.y *= -1f;
            if (axes.Contains(InvertibleAxes.Z))
                _rotationalInfluence.z *= -1f;
        }
        /// <summary>
        /// Randomizes inversion for specified positional axes. Using this in the middle of a shake may create jarring the next frame.
        /// </summary>
        /// <param name="axes">Axes to randomly invert.</param>
        public void RandomlyInvertPositionalAxes(InvertibleAxes axes)
        {
            //If there is anything to invert.
            if ((int)axes != 0)
            {
                //X
                if (axes.Contains(InvertibleAxes.X))
                {
                    float multiplier = Floats.RandomlyFlip(1f);
                    _positionalInfluence.x *= multiplier;
                }
                //Y
                if (axes.Contains(InvertibleAxes.Y))
                {
                    float multiplier = Floats.RandomlyFlip(1f);
                    _positionalInfluence.y *= multiplier;
                }
                //Z
                if (axes.Contains(InvertibleAxes.Z))
                {
                    float multiplier = Floats.RandomlyFlip(1f);
                    _positionalInfluence.z *= multiplier;
                }
            }
        }
        /// <summary>
        /// Randomizes inversion for specified rotational axes. Using this in the middle of a shake may create jarring the next frame.
        /// </summary>
        /// <param name="axes">Axes to randomly invert.</param>
        public void RandomlyInvertRotationalAxes(InvertibleAxes axes)
        {
            //If there is anything to invert.
            if ((int)axes != 0)
            {
                //X
                if (axes.Contains(InvertibleAxes.X))
                {
                    float multiplier = Floats.RandomlyFlip(1f);
                    _rotationalInfluence.x *= multiplier;
                }
                //Y
                if (axes.Contains(InvertibleAxes.Y))
                {
                    float multiplier = Floats.RandomlyFlip(1f);
                    _rotationalInfluence.y *= multiplier;
                }
                //Z
                if (axes.Contains(InvertibleAxes.Z))
                {
                    float multiplier = Floats.RandomlyFlip(1f);
                    _rotationalInfluence.z *= multiplier;
                }
            }
        }
        #endregion

        /// <summary>
        /// Sets instanced to true, and sets serialized properties. This method is for internal use only.
        /// </summary>
        /// <param name="scaledTime"></param>
        /// <param name="magnitude"></param>
        /// <param name="roughness"></param>
        /// <param name="totalDuration"></param>
        /// <param name="fadeInDuration"></param>
        /// <param name="fadeOutDuration"></param>
        /// <param name="positionalInfluence"></param>
        /// <param name="rotationalInfluence"></param>
        internal void SetInstancedWithProperties(bool scaledTime, bool shakeCameras, bool shakeCanvases, bool shakeRigidbodies, bool unlimitedDuration, float magnitude, float magnitudeNoise, AnimationCurve magnitudeCurve, float roughness, float roughnessNoise, AnimationCurve roughnessCurve, float totalDuration, float fadeInDuration, float fadeOutDuration, Vector3 positionalInfluence, InvertibleAxes positionalInverts, Vector3 rotationalInfluence, InvertibleAxes rotationalInverts, bool randomSeed)
        {
            ScaledTime = scaledTime;
            ShakeCameras = shakeCameras;
            ShakeCanvases = shakeCanvases;
            ShakeObjects = shakeRigidbodies;
            UnlimitedDuration = unlimitedDuration;
            Magnitude = magnitude;
            MagnitudeNoise = magnitudeNoise;
            MagnitudeCurve = magnitudeCurve;
            Roughness = roughness;
            RoughnessNoise = roughnessNoise;
            RoughnessCurve = roughnessCurve;
            FadeInDuration = fadeInDuration;
            FadeOutDuration = fadeOutDuration;
            TotalDuration = totalDuration;
            PositionalInfluence = positionalInfluence;
            PositionalInverts = positionalInverts;
            RotationalInfluence = rotationalInfluence;
            RotationalInverts = rotationalInverts;
            RandomSeed = randomSeed;

            RandomlyInvertPositionalAxes(PositionalInverts);
            RandomlyInvertRotationalAxes(RotationalInverts);

            Instanced = true;
        }
        /// <summary>
        /// Returns if instanced and outputs debug when not.
        /// </summary>
        /// <returns></returns>
        private bool InstancedWithDebug()
        {
            if (!Instanced && Debug.isDebugBuild)
                Debug.LogError(ACTION_INSTANCE_REQUIRED);

            return Instanced;
        }

        #region Editor checks.
        private void OnValidate()
        {
            if (!UnlimitedDuration && _totalDuration <= 0f)
                _totalDuration = 1f;
        }
        #endregion


    }

}