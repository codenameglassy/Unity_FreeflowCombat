using FirstGearGames.Utilities.Maths;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker
{
    public class ShakerInstance
    {
        #region Types.
        private class NoiseData
        {
            public NoiseData(float variance)
            {
                _variance = variance;
                RandomizeGoal(true);
            }

            #region Private.
            private readonly float _variance;
            /// <summary>
            /// Current noise value.
            /// </summary>
            private float _currentNoise;
            /// <summary>
            /// Goal to move towards.
            /// </summary>
            private float _goal;
            #endregion

            #region Const.
            /// <summary>
            /// How much to multiply distance checks by when moving CurrentNoise towards Goal.
            /// </summary>
            private const float DISTANCE_MULTIPLIER = 3f;
            /// <summary>
            /// How quickly to move CurrentNoise towards Goal.
            /// </summary>
            private const float UPDATE_RATE = 0.5f;
            #endregion

            /// <summary>
            /// Randomly sets a new goal using variance.
            /// </summary>
            private void RandomizeGoal(bool setCurrent)
            {
                _goal = 1f.Variance(_variance);
                if (setCurrent)
                    _currentNoise = _goal;
            }
            /// <summary>
            /// Returns the current noise. This is for internal use only.
            /// </summary>
            /// <returns></returns>
            /// <param name="deltaTime">DeltaTime to use.</param>
            internal float UpdateCurrentNoise(float deltaTime)
            {
                //No variance.
                if (_variance == 0f)
                    return 1f;

                //At goal, make a new one.
                if (_currentNoise == _goal)
                    RandomizeGoal(false);

                float distance = Mathf.Max(1f, Mathf.Abs(_goal - _currentNoise) * DISTANCE_MULTIPLIER);
                _currentNoise = Mathf.MoveTowards(_currentNoise, _goal, UPDATE_RATE * distance * deltaTime);

                return _currentNoise;
            }
        }

        private class ModifierMultiplier
        {
            public ModifierMultiplier() { }
            public ModifierMultiplier(float multiplier, float moveRate, bool rateUsesDistance)
            {
                SetValues(multiplier, moveRate, rateUsesDistance);
            }

            /// <summary>
            /// Value Multiplier should move towards.
            /// </summary>
            private float _multiplierGoal = 1f;
            /// <summary>
            /// Current value to multiply by.
            /// </summary>
            public float Multiplier { get; private set; } = 1f;
            /// <summary>
            /// Rate to move towards multipliers. Use -1f for instant changes.
            /// </summary>
            private float _moveRate = -1f;
            /// <summary>
            /// True for the rate value to change with distance. This to a degree normalizes changes.
            /// </summary>
            private bool _rateUsesDistance = true;

            /// <summary>
            /// Sets new values to use.
            /// </summary>
            /// <param name="multiplier"></param>
            /// <param name="moveRate"></param>
            /// <param name="rateUsesDistance"></param>
            public void SetValues(float multiplier, float moveRate, bool rateUsesDistance)
            {
                _multiplierGoal = multiplier;
                _moveRate = moveRate;
                _rateUsesDistance = rateUsesDistance;
            }

            /// <summary>
            /// Updates the multipliers to move towards their goals. This is for internal use only.
            /// </summary>
            /// <param name="deltaTime"></param>
            internal void Update(float deltaTime)
            {
                //If magnitude isn't at goal yet.
                if (Multiplier != _multiplierGoal)
                {
                    //No move rate, move instantly.
                    if (_moveRate <= 0f)
                    {
                        Multiplier = _multiplierGoal;
                    }
                    else
                    {
                        float distance = 1f;
                        if (_rateUsesDistance)
                            distance = Mathf.Max(distance, Mathf.Abs(_multiplierGoal - Multiplier));

                        Multiplier = Mathf.MoveTowards(Multiplier, _multiplierGoal, distance * _moveRate * deltaTime);
                    }
                }
            }
        }
        #endregion

        #region Constructors.
        public ShakerInstance(ShakeData data)
        {
            Data = data;

            if (data.RandomSeed)
                _seed = Floats.Random01();
            else
                _seed = 0.5f;

            _magnitudeNoise = new NoiseData(data.MagnitudeNoise);
            _roughnessNoise = new NoiseData(data.RoughnessNoise);
        }
        #endregion

        #region Public.
        /// <summary>
        /// Data being used for this instance.
        /// </summary>
        public ShakeData Data { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        private bool _paused;
        /// <summary>
        /// True if this shaker instance is paused.
        /// </summary>
        public bool Paused
        {
            get
            {
                if (_paused)
                    return true;
                if (Time.timeScale == 0f && Data.ScaledTime)
                    return true;

                return false;
            }
            private set { _paused = value; }
        }
        #endregion

        #region Private.
        /// <summary>
        /// Seed to generate perlin noise.
        /// </summary>
        private float _seed = 0;
        /// <summary>
        /// Time passed since the shaker has started.
        /// </summary>
        private float _timePassed = 0f;
        /// <summary>
        /// Value to multiply roughness by.
        /// </summary>
        private ModifierMultiplier _roughnessMultiplier = new ModifierMultiplier();
        /// <summary>
        /// Value to multiply magnitude by.
        /// </summary>
        private ModifierMultiplier _magnitudeMultiplier = new ModifierMultiplier();
        /// <summary>
        /// Time in the magnitude curve when using infinite shake.
        /// </summary>
        private float _magnitudeCurveTime = 0f;
        /// <summary>
        /// Time in the roughness curve when using infinite shake.
        /// </summary>
        private float _roughnessCurveTime = 0f;
        /// <summary>
        /// Noise data for magnitude.
        /// </summary>
        private NoiseData _magnitudeNoise;
        /// <summary>
        /// Noise data for roughness.
        /// </summary>
        private NoiseData _roughnessNoise;
        /// <summary>
        /// Last offset for instance.
        /// </summary>
        private Vector3 _offset;
        #endregion

        /// <summary>
        /// Creates a Vector3 perlin noise.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        private Vector3 PerlinNoise(float seed)
        {
            return new Vector3(
                Mathf.PerlinNoise(seed, seed) - 0.5f,
                Mathf.PerlinNoise(seed, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, seed) - 0.5f
                );
        }

        private bool _first = true;
        /// <summary>
        /// Returns a new offset for this instnace.
        /// </summary>
        /// <returns></returns>
        internal Vector3 UpdateOffset()
        {
            float deltaTime = (Data.ScaledTime) ? Time.deltaTime : Time.unscaledDeltaTime;

            if (DurationOver())
            {
                _offset = Vector3.zero;
            }
            else
            {
                //Updates the multipliers which can be set at runtime.
                _magnitudeMultiplier.Update(deltaTime);
                _roughnessMultiplier.Update(deltaTime);

                //If RandomSeed is used then also use perlin noise, creating a more randomized shake.
                if (Data.RandomSeed)
                    _offset = PerlinNoise(_seed);
                //Without a random seed the offset changes predictably.
                else
                    _offset = new Vector3(1f, 1f, 1f) * (Mathf.PingPong(_seed, 1f) - 0.5f);

                _seed += deltaTime * Data.Roughness * ReturnRoughnessCurveMultiplier(deltaTime) * _roughnessMultiplier.Multiplier * _roughnessNoise.UpdateCurrentNoise(deltaTime);
            }

            //Multiplier from fading which is applied to overall values.
            float fadeMultiplier = ReturnFadeMultiplier();
            //Increase time passed to handle fading and ending shake.
            _timePassed += deltaTime;

            return _offset * Data.Magnitude * ReturnMagnitudeCurveMultiplier(deltaTime) * _magnitudeMultiplier.Multiplier * fadeMultiplier * _magnitudeNoise.UpdateCurrentNoise(deltaTime);
        }

        /// <summary>
        /// Returns a multiplier to use based on if fading in or out.
        /// </summary>
        /// <returns></returns>
        private float ReturnFadeMultiplier()
        {
            //Fading in check.
            if (Data.FadeInDuration > 0f)
            {
                float percent = _timePassed / Data.FadeInDuration;
                //If not fully faded in then return fade in multiplier.
                if (percent < 1f)
                    return percent;
            }

            //Fading out check.
            if (!Data.UnlimitedDuration && Data.FadeOutDuration > 0f)
            {
                float remaining = Data.TotalDuration - _timePassed;
                float percent = remaining / Data.FadeOutDuration;
                if (percent < 1f)
                    return percent;
            }

            //Default multiplier if no fade is occurring.
            return 1f;
        }

        /// <summary>
        /// Returns a magnitude mulitplier based on the magnitude curve and time passed.
        /// </summary>
        /// <returns></returns>
        private float ReturnMagnitudeCurveMultiplier(float deltaTime)
        {
            float curveDuration = Data.RoughnessCurve.keys[Data.RoughnessCurve.length - 1].time;

            //Unlimited shake, loop curve.
            if (Data.UnlimitedDuration)
            {
                _magnitudeCurveTime += deltaTime;
                if (_magnitudeCurveTime > curveDuration)
                    _magnitudeCurveTime = (curveDuration - _magnitudeCurveTime);

                return Data.MagnitudeCurve.Evaluate(_magnitudeCurveTime);
            }
            //Limited time, use curve normally.
            else
            {
                float percent = _timePassed / Data.TotalDuration;
                return Data.MagnitudeCurve.Evaluate(percent * curveDuration);
            }
        }
        /// <summary>
        /// Returns a roughness mulitplier based on the roughness curve and time passed.
        /// </summary>
        /// <returns></returns>
        private float ReturnRoughnessCurveMultiplier(float deltaTime)
        {
            float curveDuration = Data.RoughnessCurve.keys[Data.RoughnessCurve.length - 1].time;

            //Unlimited shake, loop curve.
            if (Data.UnlimitedDuration)
            {
                _roughnessCurveTime += deltaTime;
                if (_roughnessCurveTime > curveDuration)
                    _roughnessCurveTime = (curveDuration - _roughnessCurveTime);

                return Data.RoughnessCurve.Evaluate(_roughnessCurveTime);
            }
            //Limited time, use curve normally.
            else
            {
                float percent = _timePassed / Data.TotalDuration;
                return Data.RoughnessCurve.Evaluate(percent * curveDuration);
            }
        }

        /// <summary>
        /// Returns if the instance shaker is over.
        /// </summary>
        /// <returns></returns>
        public bool ShakerOver()
        {
            return (DurationOver() && (_offset == Vector3.zero));
        }

        /// <summary>
        /// Returns true if the total duration has expired, and not unlimited duration.
        /// </summary>
        /// <returns></returns>
        private bool DurationOver()
        {
            if (Data.UnlimitedDuration)
                return false;

            return (_timePassed >= Data.TotalDuration);
        }

        /// <summary>
        /// Sets the paused state of this shaker.
        /// </summary>
        /// <param name="value">New paused state.</param>
        public void SetPaused(bool value)
        {
            Paused = value;
        }

        /// <summary>
        /// Stops this instance abruptly.
        /// </summary>
        public void Stop()
        {
            Data.SetTotalDuration(0f);
        }

        /// <summary>
        /// Fades out this instance. This operation only works if not already fading out.
        /// </summary>
        /// <param name="durationOverride">Overrides instance Data fade out duration with a new value.</param>
        public void FadeOut(float? durationOverride = null)
        {
            float fadeDuration = (durationOverride == null) ? Data.FadeOutDuration : durationOverride.Value;

            bool canChange = false;
            //If unlimited duration then fading out is okay, no checks needed.
            if (Data.UnlimitedDuration)
            {
                canChange = true;
            }
            //Not unlimited total duration, check if already fading out.
            else
            {
                float remaining = Data.TotalDuration - _timePassed;
                /* If remaining is less than new fade
                 * duration then let remaining end unmodified. 
                 * Otherwise snap to new fade out duration.
                 * In the future rather than abrutply
                 * skipping time fade out will be speed up to match
                 * new time. */
                if (remaining > fadeDuration)
                    canChange = true;
            }

            if (canChange)
            {
                Data.SetFadeOutDuration(fadeDuration);
                Data.SetTotalDuration(_timePassed + fadeDuration);
            }
        }

        /// <summary>
        /// Multiplies magnitude values in data by a set amount. 
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using moveRate unmodified.</param>
        public void MultiplyMagnitude(float multiplier, float moveRate, bool rateUsesDistance = true)
        {
            _magnitudeMultiplier.SetValues(multiplier, moveRate, rateUsesDistance);
        }
        /// <summary>
        /// Multiplies roughness values in data by a set amount. 
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using moveRate unmodified.</param>
        public void MultiplyRoughness(float multiplier, float moveRate, bool rateUsesDistance = true)
        {
            _roughnessMultiplier.SetValues(multiplier, moveRate, rateUsesDistance);
        }

    }
}