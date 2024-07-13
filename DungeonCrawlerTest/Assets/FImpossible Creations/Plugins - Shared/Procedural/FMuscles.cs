using System;
using System.Collections;
using UnityEngine;

namespace FIMSpace.FTools
{
    public abstract class FMuscle_Motor
    {
        public float OutValue { get; protected set; }

        protected float proceduralValue = 0f;
        protected float dampingAcceleration;
        protected float dynamicAcceleration;
        protected float accelerationSign;

        public bool IsWorking()
        {
            return dynamicAcceleration != 0f;
        }

        /// <summary> Value should be high (500f to 10000f) </summary>
        public void Push(float value) { dynamicAcceleration += value; }

        public void Initialize(float initValue)
        {
            OutValue = initValue;
            proceduralValue = initValue;
            dampingAcceleration = 0f;
            dynamicAcceleration = 0f;
            accelerationSign = 0f;
        }

        protected abstract float GetDiff(float current, float desired);

        public void Update(float delta, float current, float desired, float acceleration, float accelerationLimit, float damping, float brakePower)
        {
            float towards = GetDiff(current, desired);
            accelerationSign = Mathf.Sign(towards);

            // Linear fitting
            dampingAcceleration = towards;
            dampingAcceleration = Mathf.Clamp(dampingAcceleration, -damping, damping) * damping;

            float incr = dampingAcceleration * delta;

            if (towards > 0f) { if (incr > towards) incr = towards; } else { if (incr < towards) incr = towards; }
            proceduralValue += incr;

            // Conditions for acceleration
            float mul = 1f;
            if (Mathf.Sign(dynamicAcceleration) != accelerationSign)
            {
                mul = 1f + Mathf.Abs(towards) / ((1f - brakePower) * 10f + 8f);
            }

            // Difference towards target
            float difference = towards;
            if (difference < 0f) difference = -difference;

            // Braking when near
            float brakeFactor = 5f + (1f - brakePower) * 85f;
            if (difference < brakeFactor) mul *= Mathf.Min(1f, difference / brakeFactor);
            if (mul < 0f) mul = -mul;

            // Acceleration fitting
            if (delta > 0.04f) delta = 0.04f;
            dynamicAcceleration += acceleration * accelerationSign * delta * mul; // Increase acceleration
            dynamicAcceleration = Mathf.Clamp(dynamicAcceleration, -accelerationLimit, accelerationLimit); // Limit acceleration
            if (dynamicAcceleration < 0.000005f && dynamicAcceleration > -0.000005f) dynamicAcceleration = 0f;

            proceduralValue += dynamicAcceleration * delta;

            OutValue = proceduralValue;
        }

        public void OverrideValue(float newValue)
        {
            proceduralValue = newValue;
        }

        public void OffsetValue(float off)
        {
            proceduralValue += off;
        }
    }

    public class FMuscle_Float : FMuscle_Motor
    {
        protected override float GetDiff(float current, float desired)
        {
            return desired - current;
        }
    }

    public class FMuscle_Angle : FMuscle_Motor
    {

        protected override float GetDiff(float current, float desired)
        {
            return Mathf.DeltaAngle(current, desired);
        }
    }


    [System.Serializable]
    public class FMuscle_Vector3
    {
        [HideInInspector] public Vector3 DesiredPosition;
        public Vector3 ProceduralPosition { get; private set; }
        public bool Initialized { get; private set; }

        private FMuscle_Float x;
        private FMuscle_Float y;
        private FMuscle_Float z;

        public void Initialize(Vector3 initPosition)
        {
            x = new FMuscle_Float();
            y = new FMuscle_Float();
            z = new FMuscle_Float();

            x.Initialize(initPosition.x);
            y.Initialize(initPosition.y);
            z.Initialize(initPosition.z);

            ProceduralPosition = initPosition;
            Initialized = true;
        }

        public bool IsWorking()
        {
            return x.IsWorking() || y.IsWorking() || z.IsWorking();
        }

        public void Push(Vector3 value)
        {
            x.Push(value.x);
            y.Push(value.y);
            z.Push(value.z);
        }

        public void Reset(Vector3 value)
        {
            x.Initialize(value.x);
            y.Initialize(value.y);
            z.Initialize(value.z);
        }

        public void Push(float v)
        {
            x.Push(v);
            y.Push(v);
            z.Push(v);
        }

        public void MotionInfluence(Vector3 offset)
        {
            x.OffsetValue(offset.x);
            y.OffsetValue(offset.y);
            z.OffsetValue(offset.z);
            ProceduralPosition += offset;
        }

        public void Update(float delta, Vector3 desired, float acceleration, float accelerationLimit, float damping, float brakePower)
        {
            x.Update(delta, ProceduralPosition.x, desired.x, acceleration, accelerationLimit, damping, brakePower);
            y.Update(delta, ProceduralPosition.y, desired.y, acceleration, accelerationLimit, damping, brakePower);
            z.Update(delta, ProceduralPosition.z, desired.z, acceleration, accelerationLimit, damping, brakePower);

            ProceduralPosition = new Vector3(x.OutValue, y.OutValue, z.OutValue);
        }


        [FPD_Suffix(0f, 10000)] public float Acceleration = 10000f;
        [FPD_Suffix(0f, 10000)] public float AccelerationLimit = 5000f;
        [FPD_Suffix(0f, 50f)] public float Damping = 10f;
        [FPD_Suffix(0f, 1f)] public float BrakePower = 0.2f;
        public Vector3 Update(float delta, Vector3 desired)
        {
            x.Update(delta, ProceduralPosition.x, desired.x, Acceleration, AccelerationLimit, Damping, BrakePower);
            y.Update(delta, ProceduralPosition.y, desired.y, Acceleration, AccelerationLimit, Damping, BrakePower);
            z.Update(delta, ProceduralPosition.z, desired.z, Acceleration, AccelerationLimit, Damping, BrakePower);

            ProceduralPosition = new Vector3(x.OutValue, y.OutValue, z.OutValue);
            return ProceduralPosition;
        }

        /// <summary>
        /// Adding push force to vector
        /// </summary>
        public IEnumerator PushImpulseCoroutine(Vector3 power, float duration, bool fadeOutPower = false, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            Push(0.0001f);
            while (elapsed / duration < 1f)
            {
                if (!fadeOutPower) Push(power * Time.deltaTime * 60f); else Push(power * (1f - elapsed / duration) * Time.deltaTime * 60f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield break;
        }

        public static void Lerp(ref FMuscle_Vector3 source, FMuscle_Vector3 a, FMuscle_Vector3 b, float t)
        {
            if (a == null || b == null || source == null) return;

            source.Acceleration = Mathf.LerpUnclamped(a.Acceleration, b.Acceleration, t);
            source.AccelerationLimit = Mathf.LerpUnclamped(a.AccelerationLimit, b.AccelerationLimit, t);
            source.BrakePower = Mathf.LerpUnclamped(a.BrakePower, b.BrakePower, t);
            source.Damping = Mathf.LerpUnclamped(a.Damping, b.Damping, t);
        }

        public void OverrideProceduralPosition(Vector3 newPos)
        {
            ProceduralPosition = newPos;
            DesiredPosition = newPos;
            x.OverrideValue(newPos.x);
            y.OverrideValue(newPos.y);
            z.OverrideValue(newPos.z);
        }
    }


    [System.Serializable]
    public class FMuscle_Quaternion
    {
        [HideInInspector] public Quaternion DesiredRotation;
        public Quaternion ProceduralRotation { get; private set; }
        public bool IsCorrect { get { return x != null; } }

        private FMuscle_Float x;
        private FMuscle_Float y;
        private FMuscle_Float z;
        private FMuscle_Float w;

        public void Initialize(Quaternion initRotation)
        {
            x = new FMuscle_Float();
            y = new FMuscle_Float();
            z = new FMuscle_Float();
            w = new FMuscle_Float();

            x.Initialize(initRotation.x);
            y.Initialize(initRotation.y);
            z.Initialize(initRotation.z);
            w.Initialize(initRotation.w);

            ProceduralRotation = initRotation;
        }

        public bool IsWorking()
        {
            return x.IsWorking() || y.IsWorking() || z.IsWorking() || w.IsWorking();
        }

        public void Push(Quaternion value)
        {
            x.Push(value.x);
            y.Push(value.y);
            z.Push(value.z);
            w.Push(value.w);
        }

        public void Push(float v)
        {
            x.Push(v);
            y.Push(v);
            z.Push(v);
            w.Push(v);
        }

        public void Push(Quaternion value, float multiply)
        {
            x.Push(value.x * multiply);
            y.Push(value.y * multiply);
            z.Push(value.z * multiply);
            w.Push(value.w * multiply);
        }

        public void Update(float delta, Quaternion desired, float acceleration, float accelerationLimit, float damping, float brakePower)
        {
            x.Update(delta, ProceduralRotation.x, desired.x, acceleration, accelerationLimit, damping, brakePower);
            y.Update(delta, ProceduralRotation.y, desired.y, acceleration, accelerationLimit, damping, brakePower);
            z.Update(delta, ProceduralRotation.z, desired.z, acceleration, accelerationLimit, damping, brakePower);
            w.Update(delta, ProceduralRotation.w, desired.w, acceleration, accelerationLimit, damping, brakePower);

            ProceduralRotation = new Quaternion(x.OutValue, y.OutValue, z.OutValue, w.OutValue);
        }

        [FPD_Suffix(0f, 10000)] public float Acceleration = 5000f;
        [FPD_Suffix(0f, 10000)] public float AccelerationLimit = 1000f;
        [FPD_Suffix(0f, 50f)] public float Damping = 10f;
        [FPD_Suffix(0f, 1f)] public float BrakePower = 0.2f;
        public void Update(float delta, Quaternion desired)
        {
            x.Update(delta, ProceduralRotation.x, desired.x, Acceleration, AccelerationLimit, Damping, BrakePower);
            y.Update(delta, ProceduralRotation.y, desired.y, Acceleration, AccelerationLimit, Damping, BrakePower);
            z.Update(delta, ProceduralRotation.z, desired.z, Acceleration, AccelerationLimit, Damping, BrakePower);
            w.Update(delta, ProceduralRotation.w, desired.w, Acceleration, AccelerationLimit, Damping, BrakePower);

            ProceduralRotation = new Quaternion(x.OutValue, y.OutValue, z.OutValue, w.OutValue);
        }

        public void UpdateEnsured(float delta, Quaternion desired)
        {
            Update(delta, EnsureQuaternionContinuity(ProceduralRotation, desired));
        }

        public static Quaternion EnsureQuaternionContinuity(Quaternion latestRot, Quaternion targetRot)
        {
            Quaternion flipped = new Quaternion(-targetRot.x, -targetRot.y, -targetRot.z, -targetRot.w);
            Quaternion midQ = new Quaternion ( Mathf.LerpUnclamped(latestRot.x, targetRot.x, 0.5f), Mathf.LerpUnclamped(latestRot.y, targetRot.y, 0.5f), Mathf.LerpUnclamped(latestRot.z, targetRot.z, 0.5f), Mathf.LerpUnclamped(latestRot.w, targetRot.w, 0.5f) );
            Quaternion midQFlipped = new Quaternion(Mathf.LerpUnclamped(latestRot.x, flipped.x, 0.5f),Mathf.LerpUnclamped(latestRot.y, flipped.y, 0.5f),Mathf.LerpUnclamped(latestRot.z, flipped.z, 0.5f),Mathf.LerpUnclamped(latestRot.w, flipped.w, 0.5f));

            float angle = Quaternion.Angle(latestRot, midQ);
            float angleTreshold = Quaternion.Angle(latestRot, midQFlipped);

            return angleTreshold < angle ? flipped : targetRot;
        }

        /// <summary>
        /// Adding push force to quaternion
        /// </summary>
        public IEnumerator PushImpulseCoroutine(Quaternion power, float duration, bool fadeOutPower = false, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            Push(0.001f);

            while (elapsed / duration < 1f)
            {
                if (!fadeOutPower) Push(power, Time.deltaTime * 60f); else Push(power, (1f - elapsed / duration) * Time.deltaTime * 60f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield break;
        }

        public static void Lerp(ref FMuscle_Quaternion source, FMuscle_Quaternion a, FMuscle_Quaternion b, float t)
        {
            if (a == null || b == null || source == null) return;

            source.Acceleration = Mathf.LerpUnclamped(a.Acceleration, b.Acceleration, t);
            source.AccelerationLimit = Mathf.LerpUnclamped(a.AccelerationLimit, b.AccelerationLimit, t);
            source.BrakePower = Mathf.LerpUnclamped(a.BrakePower, b.BrakePower, t);
            source.Damping = Mathf.LerpUnclamped(a.Damping, b.Damping, t);
        }

        public void OverrideProceduralRotation(Quaternion rotation)
        {
            ProceduralRotation = rotation;
            DesiredRotation = rotation;
            x.OverrideValue(rotation.x);
            y.OverrideValue(rotation.y);
            z.OverrideValue(rotation.z);
            w.OverrideValue(rotation.w);
        }
    }



    [System.Serializable]
    public class FMuscle_Eulers
    {
        [HideInInspector] public Vector3 DesiredEulerAngles;
        public Vector3 ProceduralEulerAngles { get; private set; }
        public Quaternion ProceduralRotation { get { return Quaternion.Euler(ProceduralEulerAngles); } }

        private FMuscle_Angle x;
        private FMuscle_Angle y;
        private FMuscle_Angle z;

        public void Initialize(Vector3 initEulerAngles)
        {
            x = new FMuscle_Angle();
            y = new FMuscle_Angle();
            z = new FMuscle_Angle();

            x.Initialize(initEulerAngles.x);
            y.Initialize(initEulerAngles.y);
            z.Initialize(initEulerAngles.z);

            ProceduralEulerAngles = initEulerAngles;
        }

        public void Initialize(Quaternion initRotation)
        {
            Initialize(initRotation.eulerAngles);
        }

        public bool IsWorking()
        {
            return x.IsWorking() || y.IsWorking() || z.IsWorking();
        }

        public void Push(Vector3 value)
        {
            x.Push(value.x);
            y.Push(value.y);
            z.Push(value.z);
        }

        public void Push(float v)
        {
            x.Push(v);
            y.Push(v);
            z.Push(v);
        }

        public void Push(Vector3 value, float multiply)
        {
            x.Push(value.x * multiply);
            y.Push(value.y * multiply);
            z.Push(value.z * multiply);
        }

        public void Update(float delta, Vector3 desired, float acceleration, float accelerationLimit, float damping, float brakePower)
        {
            x.Update(delta, ProceduralEulerAngles.x, desired.x, acceleration, accelerationLimit, damping, brakePower);
            y.Update(delta, ProceduralEulerAngles.y, desired.y, acceleration, accelerationLimit, damping, brakePower);
            z.Update(delta, ProceduralEulerAngles.z, desired.z, acceleration, accelerationLimit, damping, brakePower);

            ProceduralEulerAngles = new Vector3(x.OutValue, y.OutValue, z.OutValue);
        }

        [FPD_Suffix(0f, 10000)] public float Acceleration = 5000f;
        [FPD_Suffix(0f, 10000)] public float AccelerationLimit = 1000f;
        [FPD_Suffix(0f, 50f)] public float Damping = 10f;
        [FPD_Suffix(0f, 1f)] public float BrakePower = 0.2f;
        public Vector3 Update(float delta, Vector3 desired)
        {
            x.Update(delta, ProceduralEulerAngles.x, desired.x, Acceleration, AccelerationLimit, Damping, BrakePower);
            y.Update(delta, ProceduralEulerAngles.y, desired.y, Acceleration, AccelerationLimit, Damping, BrakePower);
            z.Update(delta, ProceduralEulerAngles.z, desired.z, Acceleration, AccelerationLimit, Damping, BrakePower);

            ProceduralEulerAngles = new Vector3(x.OutValue, y.OutValue, z.OutValue);
            return ProceduralEulerAngles;
        }

        public void Update(float delta, Quaternion desired)
        {
            Update(delta, desired.eulerAngles);
        }

        /// <summary>
        /// Adding push force to quaternion
        /// </summary>
        public IEnumerator PushImpulseCoroutine(Vector3 power, float duration, bool fadeOutPower = false, float delay = 0f)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float elapsed = 0f;
            Push(0.001f);

            while (elapsed / duration < 1f)
            {
                if (!fadeOutPower) Push(power, Time.deltaTime * 60f); else Push(power, (1f - elapsed / duration) * Time.deltaTime * 60f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            yield break;
        }

        public static void Lerp(ref FMuscle_Eulers source, FMuscle_Eulers a, FMuscle_Eulers b, float t)
        {
            if (a == null || b == null || source == null) return;

            source.Acceleration = Mathf.LerpUnclamped(a.Acceleration, b.Acceleration, t);
            source.AccelerationLimit = Mathf.LerpUnclamped(a.AccelerationLimit, b.AccelerationLimit, t);
            source.BrakePower = Mathf.LerpUnclamped(a.BrakePower, b.BrakePower, t);
            source.Damping = Mathf.LerpUnclamped(a.Damping, b.Damping, t);
        }
    }

}