using UnityEngine;

namespace FirstGearGames.Utilities.Maths
{

    public static class Vectors
    {
        #region Vector3.
        /// <summary>
        /// Calculates the linear parameter t that produces the interpolant value within the range [a, b].
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static float InverseLerp(Vector3 a, Vector3 b, Vector3 value)
        {
            Vector3 ab = b - a;
            Vector3 av = value - a;
            return Mathf.Clamp01(Vector3.Dot(av, ab) / Vector3.Dot(ab, ab));
        }

        /// <summary>
        /// Returns if the target Vector3 is within variance of the source Vector3.
        /// </summary>
        /// <param name="a">Source vector.</param>
        /// <param name="b">Target vector.</param>
        /// <param name="tolerance">How close the target vector must be to be considered close.</param>
        /// <returns></returns>
        public static bool Near(this Vector3 a, Vector3 b, float tolerance = 0.01f)
        {
            return (Vector3.Distance(a, b) <= tolerance);
        }

        /// <summary>
        /// Returns if any values within a Vector3 are NaN.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsNan(this Vector3 source)
        {
            return (float.IsNaN(source.x) || float.IsNaN(source.y) || float.IsNaN(source.z));
        }

        /// <summary>
        /// Lerp between three Vector3 values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static Vector3 Lerp3(Vector3 a, Vector3 b, Vector3 c, float percent)
        {
            Vector3 r0 = Vector3.Lerp(a, b, percent);
            Vector3 r1 = Vector3.Lerp(b, c, percent);
            return Vector3.Lerp(r0, r1, percent);
        }

        /// <summary>
        /// Lerp between three Vector3 values.
        /// </summary>
        /// <param name="vectors"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static Vector3 Lerp3(Vector3[] vectors, float percent)
        {
            if (vectors.Length < 3)
            {
                Debug.LogWarning("Vectors -> Lerp3 -> Vectors length must be 3.");
                return Vector3.zero;
            }

            return Lerp3(vectors[0], vectors[1], vectors[2], percent);
        }

        /// <summary>
        /// Multiplies a Vector3 by another.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        public static Vector3 Multiply(this Vector3 src, Vector3 multiplier)
        {
            return new Vector3(src.x * multiplier.x, src.y * multiplier.y, src.z * multiplier.z);
        }
        #endregion

        #region Vector2.

        /// <summary>
        /// Lerp between three Vector2 values.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static Vector2 Lerp3(Vector2 a, Vector2 b, Vector2 c, float percent)
        {
            Vector2 r0 = Vector2.Lerp(a, b, percent);
            Vector2 r1 = Vector2.Lerp(b, c, percent);
            return Vector2.Lerp(r0, r1, percent);
        }

        /// <summary>
        /// Lerp between three Vector2 values.
        /// </summary>
        /// <param name="vectors"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static Vector2 Lerp2(Vector2[] vectors, float percent)
        {
            if (vectors.Length < 3)
            {
                Debug.LogWarning("Vectors -> Lerp3 -> Vectors length must be 3.");
                return Vector2.zero;
            }

            return Lerp3(vectors[0], vectors[1], vectors[2], percent);
        }


        /// <summary>
        /// Multiplies a Vector2 by another.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="multiplier"></param>
        /// <returns></returns>
        public static Vector2 Multiply(this Vector2 src, Vector2 multiplier)
        {
            return new Vector2(src.x * multiplier.x, src.y * multiplier.y);
        }
        #endregion

    }

}