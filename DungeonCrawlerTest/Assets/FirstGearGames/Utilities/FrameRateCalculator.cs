using UnityEngine;

namespace FirstGearGames.Utilities.Maths
{

    public class FrameRateCalculator
    {
        #region Private.
        /// <summary>
        /// Time used to generate Frames.
        /// </summary>
        private float _timePassed = 0f;
        /// <summary>
        /// Frames performed in TimePassed. Float is used to reduce casting.
        /// </summary>
        private float _frames = 0;
        #endregion

        #region Const.
        /// <summary>
        /// How many frames to pass before slicing calculation values.
        /// </summary>
        private const int RESET_FRAME_COUNT = 60;
        /// <summary>
        /// Percentage to slice calculation values by. Higher percentages result in smoother frame rate adjustments.
        /// </summary>
        private const float CALCULATION_SLICE_PERCENT = 0.7f;
        #endregion

        /// <summary>
        /// Gets the current frame rate.
        /// </summary>
        /// <returns></returns>
        public int GetIntFrameRate()
        {
            return Mathf.CeilToInt((_frames / _timePassed));
        }
        /// <summary>
        /// Gets the current frame rate.
        /// </summary>
        /// <returns></returns>
        public float GetFloatFrameRate()
        {
            return (_frames / _timePassed);
        }

        /// <summary>
        /// Updates frame count and time passed.
        /// </summary>
        /// <returns>True if this update hit reset frame.</returns>
        public bool Update(float unscaledDeltaTime)
        {
            _timePassed += unscaledDeltaTime;
            _frames++;

            if (_frames > RESET_FRAME_COUNT)
            {
                _frames *= CALCULATION_SLICE_PERCENT;
                _timePassed *= CALCULATION_SLICE_PERCENT;
                return true;
            }
            else
            {
                return false;
            }
        }
    }


}