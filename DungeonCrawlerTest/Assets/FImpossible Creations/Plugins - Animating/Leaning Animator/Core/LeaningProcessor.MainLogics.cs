using System;
using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {

        #region Calculations Variables

        public bool accelerating { get; private set; }
        bool brakingOrStop = false;
        public bool BrakingOrStopped { get { return brakingOrStop; } }  

        Vector3 prePos = Vector3.zero;
        float preRot;

        float yawAxisAngularVelocity;
        public float YawAngularVelocity { get { return yawAxisAngularVelocity; } }

        float accelerationProgress = 0f;
        public float AccelerationProgress { get { return accelerationProgress; } }
        float sideAccelerationProgress = 0f;
        public float SideAccelerationProgress { get { return sideAccelerationProgress; } }
        float lastSideAccelerationSign = 0f;

        float targetForwardRot = 0f;
        float targetStrafeRot = 0f;
        float lastForwardAccelerationSign = 0f;

        float targetForwardRotSpine = 0f;
        float targetSideRot;

        Vector3 preVeloRaw = Vector3.zero;
        Vector3 preVelo = Vector3.zero;
        public Vector3 PreviousVelocity { get { return preVelo; } }
        Vector3 fixedVelocity = Vector3.zero;
        public Vector3 FixedVelocity { get { return fixedVelocity; } }
        Vector3 localFixedVelocity = Vector3.zero;
        public Vector3 LocalFixedVelocity { get { return localFixedVelocity; } }

        float mainVeloMagnitude = 0f;
        float forwardVeloMagnitude = 0f;
        float sideVeloMagnitude = 0f;
        public float MainMagnitude { get { return mainVeloMagnitude; } }
        public float ForwardMagnitude { get { return forwardVeloMagnitude; } }
        public float SideMagnitude { get { return sideVeloMagnitude; } }

        [NonSerialized] public Quaternion TargetRotation;

        //Vector3 veloHelper = Vector3.zero;
        float upAxisVeloHelper = 0f;
        float sideRotVeloHelper = 0f;

        float accHelpVeloBrake = 0f;
        float accHelpSideVeloBrake = 0f;
        float spineAccHelpVelo = 0f;
        float sd_targetForwardRot = 0f;
        float sd_targetStrafeRot = 0f;

        float effectsBlend = 1f;
        bool  wasForward = true;
        float brakingProgress = 0f;

        /// <summary> When braking and average speed low - near to brake power then brake sway will be more mild </summary>
        float brakePowerMulOnAcceler = 0f;
        float sd_brakePowerMulOnAcceler = 0f;
        public float? customAccelerationVelocity = null;

        #endregion

        bool customDetection = false;
        public void SetCustomParameters(float yawVelo, Vector3 velocity, Vector3 fixedVelocity, Vector3 localVelo)
        {
            customDetection = true;
            yawAxisAngularVelocity = yawVelo;
            //this.velocity = velocity;
            this.fixedVelocity = fixedVelocity;
            this.localFixedVelocity = localVelo;
        }

        public float AverageVelocity { get { return mainVeloMagnitude; } }
        public float AverageAngularVelocity { get { return yawAxisAngularVelocity; } }

        [Tooltip("If leaning animator should reset it's acceleration lean algorithm when being ungrounded (making lean when character is grounded again)")]
        public bool ResetOnUngrounded = true;

        private void CalculateForwardSwayRotationValue()
        {
            bool decelerateEffect = brakingOrStop;
            if (!decelerateEffect) if (ResetOnUngrounded) decelerateEffect = !IsCharacterGrounded;

            if (accelerating && (IsCharacterGrounded || !ResetOnUngrounded)) // Acceleration Logics --------------------
            {

                #region Forward/Back Acceleration

                float fabsMagn = Mathf.Abs(forwardVeloMagnitude);
                float lowSpdRef = ObjSpeedWhenBraking * 0.001f;

                float forwardSign = Mathf.Sign(forwardVeloMagnitude);
                if (lastForwardAccelerationSign != forwardSign)
                {
                    lastForwardAccelerationSign = forwardSign;
                    accelerationProgress = 0f;
                }

                if (fabsMagn > lowSpdRef)
                {
                    float boost = 1f;
                    if (fabsMagn < ObjSpeedWhenBraking) boost = StartRunPush * 2f; // Faster sway when starting to move

                    float sensitivity = Mathf.Lerp(.6f, 1f, (forwardVeloMagnitude / ObjSpeedWhenRunning) * boost);
                    float target = Mathf.Lerp(15f * sensitivity, 0f, accelerationProgress) * (forwardVeloMagnitude / ObjSpeedWhenRunning) * boost;
                    if (target > 45f) target = 45f;

                    if (localFixedVelocity.z < -lowSpdRef) target *= RootBackwardSway; // Going back

                    targetForwardRot = Mathf.SmoothDamp(targetForwardRot, target, ref sd_targetForwardRot, (1.2f - SpineRapidity) * 0.2f / boost, 1000f, dt);
                    targetForwardRotSpine = targetForwardRot; // Calculated in different way when braking


                    if (accelerationProgress < 1f) if (fabsMagn > ObjSpeedWhenBraking || AccelerationDetection == EMotionDetection.CustomDetection_AutoDetectionOFF)
                        {
                            if (fabsMagn < ObjSpeedWhenRunning / 2f) accelerationProgress += dt * 1.25f * SpineRapidity;
                            else accelerationProgress += dt * 3f * SpineRapidity;
                            accelerationProgress = Mathf.Clamp01(accelerationProgress);
                        }


                    if (localFixedVelocity.z > lowSpdRef) wasForward = true; else wasForward = false;
                }

                #endregion


                #region Left/Right - Strafe Acceleration

                float sideSign = Mathf.Sign(sideVeloMagnitude);
                if ( lastSideAccelerationSign != sideSign)
                {
                    lastSideAccelerationSign = sideSign;
                    sideAccelerationProgress = 0f;
                }

                float sideVeloMagnABS = Mathf.Abs(sideVeloMagnitude);

                if (sideVeloMagnABS > lowSpdRef)
                {
                    float boost = 1f;
                    if (sideVeloMagnABS < ObjSpeedWhenBraking) boost = StartRunPush * 2.25f; // Faster sway when starting to move

                    float sensitivity = Mathf.Lerp(.6f, 1f, (sideVeloMagnABS / ObjSpeedWhenRunning) * boost);

                    float target = Mathf.Lerp(15f * sensitivity, 0f, sideAccelerationProgress) * (-sideVeloMagnitude / (ObjSpeedWhenRunning * 0.75f)) * boost;
                    target = Mathf.Clamp(target, -ClampSideSway, ClampSideSway);

                    targetStrafeRot = Mathf.SmoothDamp(targetStrafeRot, target * 0.7f, ref sd_targetStrafeRot, ((1.2f - SpineRapidity) * 0.4f) / boost, 1000f, dt);

                    if (sideAccelerationProgress < 1f) if (sideVeloMagnABS > ObjSpeedWhenBraking || AccelerationDetection == EMotionDetection.CustomDetection_AutoDetectionOFF)
                        {
                            if (sideVeloMagnABS < ObjSpeedWhenRunning / 2f) sideAccelerationProgress += dt * 1.5f * SpineRapidity;
                            else sideAccelerationProgress += dt * 4f * SpineRapidity;
                            sideAccelerationProgress = Mathf.Clamp01(sideAccelerationProgress);
                        }
                }

                if (sideVeloMagnABS < lowSpdRef * 2f)
                {
                    sideAccelerationProgress = Mathf.MoveTowards(sideAccelerationProgress, 0f, dt * 10f);
                }

                #endregion

                brakingProgress = 0f;
                accHelpVeloBrake = 0f;
                accHelpSideVeloBrake = 0f;

            }
            else if (decelerateEffect) // Braking logics ---------------------
            {
                brakingProgress += dt * (25f + BrakeRapidity);
                if (brakingProgress > 1f) brakingProgress = 1f;

                float sensitivity = Mathf.Lerp(0.6f, 1f, forwardVeloMagnitude / ObjSpeedWhenRunning);
                float target = Mathf.Lerp(0f, sensitivity * -10f * brakingProgress, accelerationProgress);
                if (target < -17f) target = -17f;

                float forwardMilding = wasForward ? 1f : -RootBackwardSway;

                targetForwardRot = Mathf.SmoothDamp(targetForwardRot, target * RootBrakeSway * brakePowerMulOnAcceler * forwardMilding, ref accHelpVeloBrake, .08f + (1.2f - SpineRapidity) * 0.22f * (1f - BrakeRapidity), 1000f, dt);
                targetForwardRotSpine = Mathf.SmoothDamp(targetForwardRotSpine, target * SpineBrakeSway * brakePowerMulOnAcceler * forwardMilding, ref spineAccHelpVelo, .08f + (1.2f - SpineRapidity) * 0.24f * (1f - BrakeRapidity), 1000f, dt);

                accelerationProgress -= dt * 7.5f * RootRapidity * BrakeRapidity;
                accelerationProgress = Mathf.Clamp01(accelerationProgress);

                #region Strafe - Left/Right milding

                targetStrafeRot = Mathf.SmoothDamp(targetStrafeRot, 0f, ref sd_targetStrafeRot, ((1.2f - SpineRapidity) * 0.3f), 1000f, dt);

                sensitivity = Mathf.Lerp(0.6f, 1f, sideVeloMagnitude / ObjSpeedWhenRunning);
                target = Mathf.Lerp(0f, sensitivity * -10f * brakingProgress, sideAccelerationProgress);
                if (target < -17f) target = -17f;

                targetSideRot = Mathf.SmoothDamp(targetSideRot, target * RootBrakeSway * brakePowerMulOnAcceler, ref accHelpSideVeloBrake, .08f + (1.2f - SpineRapidity) * 0.22f * (1f - BrakeRapidity), 1000f, dt);

                sideAccelerationProgress -= dt * 7.5f * RootRapidity * BrakeRapidity;
                sideAccelerationProgress = Mathf.Clamp01(sideAccelerationProgress);

                #endregion
            }

            brakePowerMulOnAcceler = Mathf.SmoothDamp(brakePowerMulOnAcceler, Mathf.InverseLerp(ObjSpeedWhenBraking, ObjSpeedWhenRunning, AverageVelocity * 1.35f), ref sd_brakePowerMulOnAcceler, 0.3f, SDMAX, dt);
            brakePowerMulOnAcceler = Mathf.Clamp(brakePowerMulOnAcceler, 0.2f, 1f);
        }

        void CalculateSideSwayRotationValue() // Swaying to sides ------------------------
        {
            float target = yawAxisAngularVelocity * (SideSwayPower * 5f);
            target *= Mathf.Lerp(0.05f, 1.1f, Mathf.InverseLerp(ObjSpeedWhenBraking / 2f, ObjSpeedWhenRunning, forwardVeloMagnitude));
            target = Mathf.Clamp(target, -ClampSideSway, ClampSideSway);

            targetSideRot = Mathf.SmoothDamp(targetSideRot, target, ref sideRotVeloHelper, (1.2f - RootRapidity) * 0.245f, 35f + 3f * (SideSwayPower * 5f), dt);
        }

        public void CalibrateFootsOrigin()
        {
            if (footOriginBone != null) footOriginBone.PreCalibrate();
        }

        internal void UpdateAngularVelocity()
        {
            if (customDetection == false)
            {
                float safeDelta = dt; if (safeDelta < 0.0005f) safeDelta = 0.0005f;
                float nowEulerY = BaseTransform.eulerAngles.y;
                yawAxisAngularVelocity = Mathf.SmoothDamp(yawAxisAngularVelocity, (Mathf.DeltaAngle(nowEulerY, preRot) / safeDelta) * 0.02f, ref upAxisVeloHelper, (1.2f - RootRapidity) * 0.1f, 1000f, dt);
                preRot = nowEulerY;
            }
        }

        // Helper logics ----------------------------------------------------

        void ResetPreVars()
        {
            preVelo = fixedVelocity;
            if (rig && AccelerationDetection != EMotionDetection.TransformBased) prePos = rig.position;
        }

        void AutoDetectVelocity()
        {
            float refVelo = mainVeloMagnitude;

            // Checking if slowing down
            if ((refVelo < ObjSpeedWhenBraking * 1.25f && preVelo.magnitude * 51f > refVelo) || refVelo < ObjSpeedWhenBraking * 1.25f)
                brakingOrStop = true;
            else brakingOrStop = false;

            float deltaMul = 49f;
            if (forceTransformBased) deltaMul = 0.95f;

            if (refVelo > ObjSpeedWhenBraking * 0.05f && refVelo > preVelo.magnitude * deltaMul)
                accelerating = true;
            else accelerating = false;
        }


    }
}