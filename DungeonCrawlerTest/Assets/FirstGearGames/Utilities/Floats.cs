using System;
using UnityEngine;

namespace FirstGearGames.Utilities.Maths
{


    public static class Floats
    {
        private static System.Random _random = new System.Random();

        /// <summary>
        /// Provides a random inclusive int within a given range. Preferred over Unity's Random to eliminate confusion as Unity uses inclusive for floats max, and exclusive for int max. 
        /// </summary>
        /// <param name="minimum">Inclusive minimum value.</param>
        /// <param name="maximum">Inclusive maximum value.</param>
        /// <returns></returns>
        public static float RandomInclusiveRange(float minimum, float maximum)
        {
            double min = Convert.ToDouble(minimum);
            double max = Convert.ToDouble(maximum);

            double result = (_random.NextDouble() * (max - min)) + min;
            return Convert.ToSingle(result);
        }

        /// <summary>
        /// Returns a random float between 0f and 1f.
        /// </summary>
        /// <returns></returns>
        public static float Random01()
        {
            return RandomInclusiveRange(0f, 1f);
        }

        /// <summary>
        /// Returns if a target float is within variance of the source float.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        public static bool Near(this float a, float b, float tolerance = 0.01f)
        {
            return (Mathf.Abs(a - b) <= tolerance);
        }

        /// <summary>
        /// Clamps a float and returns if the float required clamping.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="clamped"></param>
        /// <returns></returns>
        public static float Clamp(float value, float min, float max, ref bool clamped)
        {
            clamped = (value < min);
            if (clamped)
                return min;

            clamped = (value > min);
            if (clamped)
                return max;

            clamped = false;
            return value;
        }

        /// <summary>
        /// Returns a float after being adjusted by the specified variance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="variance"></param>
        /// <returns></returns>
        public static float Variance(this float source, float variance)
        {
            float pickedVariance = RandomInclusiveRange(1f - variance, 1f + variance);
            return (source * pickedVariance);
        }
        /// <summary>
        /// Sets a float value to result after being adjusted by the specified variance.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="variance"></param>
        /// <returns></returns>
        public static void Variance(this float source, float variance, ref float result)
        {
            float pickedVariance = RandomInclusiveRange(1f - variance, 1f + variance);
            result = (source * pickedVariance);
        }

        /// <summary>
        /// Returns negative-one, zero, or postive-one of a value instead of just negative-one or positive-one.
        /// </summary>
        /// <param name="value">Value to sign.</param>
        /// <returns>Precise sign.</returns>
        public static float PreciseSign(float value)
        {
            if (value == 0f)
                return 0f;
            else
                return (Mathf.Sign(value));
        }

        /// <summary>
        /// Returns if a float is within a range.
        /// </summary>
        /// <param name="source">Value of float.</param>
        /// <param name="rangeMin">Minimum of range.</param>
        /// <param name="rangeMax">Maximum of range.</param>
        /// <returns></returns>
        public static bool InRange(this float source, float rangeMin, float rangeMax)
        {
            return (source >= rangeMin && source <= rangeMax);
        }

        /// <summary>
        /// Randomly flips a float value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float RandomlyFlip(float value)
        {
            if (Ints.RandomInclusiveRange(0, 1) == 0)
                return value;
            else
                return (value *= -1f);
        }
    }


}