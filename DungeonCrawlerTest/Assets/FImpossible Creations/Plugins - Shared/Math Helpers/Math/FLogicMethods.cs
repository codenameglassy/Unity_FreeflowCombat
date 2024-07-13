using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace
{
    /// <summary>
    /// FM: Class which contains many helpful logic methods
    /// </summary>
    public static class FLogicMethods
    {

        #region Lerping

        public static float Lerp(this float from, float to, float value)
        {
            if (to != from) // Prevent from dividing by zero
                return Mathf.Clamp((value - from) / (to - from), -1f, 1f);

            return 0;
        }

        /// <summary>
        /// Inverse Lerp without clamping
        /// </summary>
        public static float InverseLerp(float from, float to, float value)
        {
            if (to != from) // Prevent from dividing by zero
                return Mathf.Clamp((value - from) / (to - from), -1f, 1f);

            return 0;
        }


        public static float InverseLerpUnclamped(float xx, float yy, float value)
        {
            if (yy - xx == 0f) return 0f; else return (value - xx) / (yy - xx);
        }

        /// <summary>
        /// Lerp which finishing on b value, factor means value which will be added / substracted to which lerp will reaching
        /// </summary>
        public static float FLerp(float a, float b, float t, float factor = 0.01f)
        {
            float preB = b; if (preB > a) b += factor; else b -= factor;
            float val = Mathf.LerpUnclamped(a, b, t);
            if (preB > a) { if (val >= preB) return preB; } else if (val <= preB) return preB;
            return val;
        }


        public static int IntLerp(int a, int b, float t)
        {
            int lerp = 0;
            IntLerp(ref lerp, a, b, t);
            return lerp;
        }


        public static void IntLerp(ref int source, int a, int b, float t)
        {
            source = Mathf.RoundToInt(a * (1f - t)) + Mathf.RoundToInt(b * t);
        }


        public static void IntLerp(ref int source, int b, float t)
        {
            IntLerp(ref source, source, b, t);
        }


        #endregion


        /// <summary> 
        /// Behaves like Mathf.Abs but in performance tests this one is much quicker 
        /// </summary>
        public static float FAbs(this float value)
        {
            if (value < 0) value = -value;
            return value;
        }

        public static float HyperCurve(this float value)
        {
            return -(1f / (3.2f * value - 4)) - 0.25f;
        }


        #region Calculate Distance Related


        /// <summary>
        /// For detecting difference in position (very cheap) but less precise -> Ignoring Y Axis value
        /// </summary>
        public static float TopDownDistanceManhattan(this Vector3 a, Vector3 b)
        {
            float diff = 0f;
            diff += FAbs(a.x - b.x);
            diff += FAbs(a.z - b.z);
            return diff;
        }

        /// <summary>
        /// Calculating 2D distance -> ignoring Y axis value
        /// </summary>
        public static float TopDownDistance(this Vector3 a, Vector3 b)
        {
            a.y = a.z;
            b.y = b.z;
            return Vector2.Distance(a, b);
        }

        /// <summary>
        /// For detecting difference in position (very cheap) but less precise
        /// </summary>
        public static float DistanceManhattan(this Vector3 a, Vector3 b)
        {
            float diff = 0f;
            diff += FAbs(a.x - b.x);
            diff += FAbs(a.y - b.y);
            diff += FAbs(a.z - b.z);
            return diff;
        }


        #endregion


        #region Wrapping angles


        /// <summary>
        /// Wrapping angle (clamping in +- 360)
        /// </summary>
        public static float WrapAngle(float angle)
        {
            angle %= 360;

            if (angle > 180) return angle - 360;

            return angle;
        }

        /// <summary>
        /// Wrapping all angles (clamping in +- 360)
        /// </summary>
        public static Vector3 WrapVector(Vector3 angles)
        {
            return new Vector3(WrapAngle(angles.x), WrapAngle(angles.y), WrapAngle(angles.z));
        }

        /// <summary>
        /// Unwrapping angle
        /// </summary>
        public static float UnwrapAngle(float angle)
        {
            if (angle >= 0) return angle;

            angle = -angle % 360;

            return 360 - angle;
        }

        /// <summary>
        /// Unwrapping all angles
        /// </summary>
        public static Vector3 UnwrapVector(Vector3 angles)
        {
            return new Vector3(UnwrapAngle(angles.x), UnwrapAngle(angles.y), UnwrapAngle(angles.z));
        }


        #endregion


        /// <summary>
        /// Detects if variable is very near to target value
        /// </summary>
        public static bool IsAlmostEqual(float val, float to, int afterComma = 2, float addRange = 0f)
        {
            float commaVal = 1 / Mathf.Pow(10, afterComma) + addRange;

            if ((val > to - commaVal && val < to + commaVal) || val == to)
            {
                return true;
            }

            return false;
        }


        #region Angles

        /// <summary>
        /// Calculating rotation to target object ignoring Y axis
        /// </summary>
        public static Quaternion TopDownAngle(Vector3 from, Vector3 to)
        {
            from.y = 0f;
            to.y = 0f;

            return Quaternion.LookRotation(to - from);
        }

        /// <summary>
        /// Math formula to calculate direction in 2D space
        /// </summary>
        public static Quaternion TopDownAnglePosition2D(Vector2 from, Vector2 to, float offset = 0f)
        {
            Vector2 dir = to - from;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + offset;
            return Quaternion.AngleAxis(angle, Vector3.forward);
        }

        #endregion



        public static bool ContainsIndex<T>(this List<T> list, int i, bool falseIfNull = true) where T : class
        {
            if (list == null) return false;
            if (i < 0) return false;
            if (i >= list.Count) return false;
            if (falseIfNull) if (list[i] == null) return false;
            return true;
        }

        public static bool ContainsIndex<T>(this List<T> list, int i) where T : struct
        {
            if (list == null) return false;
            if (i < 0) return false;
            if (i >= list.Count) return false;
            return true;
        }

        public static bool ContainsIndex<T>(this T[] list, int i, bool falseIfNull) where T : class
        {
            if (list == null) return false;
            if (i < 0) return false;
            if (i >= list.Length) return false;
            if (falseIfNull) if (list[i] == null) return false;
            return true;
        }

    }
}