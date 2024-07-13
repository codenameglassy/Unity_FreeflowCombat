using FIMSpace.FTools;
using System;
using System.Collections;
using UnityEngine;

namespace FIMSpace
{
    [System.Serializable]
    public partial class LeaningProcessor
    {
        public MonoBehaviour Owner { get; private set; }

        // Cofiguration Params
        //public bool ResetSwayWhenUngrounded = false;
        [Tooltip("When your character's animator have 'Update Mode' set to AniamtePhysics, then enable this option")]
        public AnimatorUpdateMode UpdateMode = AnimatorUpdateMode.Normal;
        [Tooltip("Refreshing bones to avoid twisting rotation if some animation don't have keyframes on the bones")]
        public bool Calibrate = true;

        // Main Params
        [Tooltip("Swaying to sides when character is moving and rotating main power")]
        [Range(0f, 2f)] public float SideSwayPower = 1f;
        [FPD_Suffix(0f, 75f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°")] public float ClampSideSway = 25;
        [Tooltip("Swaying forward or back when character is accelerating or braking")]
        [Range(0f, 1f)] public float ForwardSwayPower = 0.5f;
        [Tooltip("How rapid should be animation of swaying when braking")]
        [Range(0f, 2f)] public float BrakeRapidity = 0.485f;

        // Root Sway
        [Tooltip("Precentage blend amount of root transform sway effect (whole model sway)")]
        [FPD_Suffix(0f, 1f)] public float RootBlend = 1f;

        public Transform FootsOrigin;
        [Tooltip("Whole character sway animation rapidity parameter")]
        [Range(0f, 1f)] public float RootRapidity = .425f;
        [Space(7)]
        [Tooltip("Whole character sway to sides amount, when moving and rotating")]
        [Range(0f, 1f)] public float RootSideSway = 1f;
        [Tooltip("Whole character sway forward amount, when starting to move")]
        [Range(0f, 1f)] public float RootForwardSway = .5f;
        [Tooltip("Whole character sway left/right amount, when starting to move left/right while looking forward (strafing)")]
        [Range(0f, 2f)] public float RootStrafeSway = .75f;
        [Tooltip("Whole character sway forward amount, when braking")]
        [Range(0f, 2f)] public float RootBrakeSway = .5f;

        [Tooltip("Whole character sway amount when running in backward direction")]
        [Range(0f, 1f)] public float RootBackwardSway = .5f;

        // Spine Sway
        [Tooltip("Precentage blend amount of spine bones sway effect")]
        [FPD_Suffix(0f, 1f)] public float SpineBlend = 0.5f;
        [FPD_Suffix(0f, 75f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°")] public float ClampSpineSway = 25;
        [Tooltip("Spine bones sway animation rapidity parameter")]
        [Range(0f, 1f)] public float SpineRapidity = .5f;

        [Space(7)]
        [Tooltip("Angles of spine bones sway when starting to move or braking")]
        public Vector3 SpineForwardSway = new Vector3(5, 0f, 0f);
        [Tooltip("Roll rotation of spine bones when moving and rotating")]
        public float SpineSideLean = -.125f;
        [Tooltip("Additional sway angles of spine bones when moving and rotating")]
        public Vector3 SpineRightSway = new Vector3(0f, -0.75f, 1.25f);

        [Tooltip("Power of spine bones sway when braking")]
        [Range(0f, 2f)] public float SpineBrakeSway = 0.85f;

        // Extras
        [Tooltip("Faster reaction of bending (rapidity) in starting to run moment")]
        [Range(.5f, 2f)] public float StartRunPush = 1f;
        [Tooltip("When character is bending forward then head starts look on ground, you can use this parameters to slightly rotate head back up")]
        [Range(0f, 1f)] public float NeckCompensation = 0.75f;
        public Vector3 ConstantAddRotation = new Vector3(0f, 0f, 0f);

        // Arms motion
        [FPD_Suffix(0f, 1f)] public float ArmsBlend = 0.5f;
        [Tooltip("Rotating arms when character is rotating")]
        [Range(0f, 1f)] public float ArmsSideSway = 0.5f;
        [Tooltip("Rotating arms when character is accelerating or braking")]
        [Range(-.5f, 1f)] public float ArmsForwardSway = -0.225f;



        // Auto detection tool
        [Tooltip("Average speed of your movement controller when character is near to stop speed (check statistics on the bottom during playmode)")]
        public float ObjSpeedWhenBraking = 0.2f; // BrakeSpeed
        [Tooltip("Average speed of your movement controller when character is running (check statistics on the bottom during playmode)")]
        public float ObjSpeedWhenRunning = 3f; // HighSpeed

        public enum EMotionDetection { RigidbodyBased, TransformBased, CustomDetection_AutoDetectionOFF }
        [Tooltip("Automatic detection if character is idling or moving basing on object velocity (recommended doing it manually through leaningAnimator.SetAccelerating)")]
        public EMotionDetection AccelerationDetection = EMotionDetection.RigidbodyBased;
        [Tooltip("[Settings in 'Optional Ground Aligning' are used by automatic ground detection!] Automatic detection if character is standing on ground using raycast (recommended doing it manually through leaningAnimator.SetGrounded)")]
        public bool TryAutoDetectGround = true;

        [Tooltip("Advanced option for springy-like muscles effect")]
        public bool UseMuscles = false;
        public FMuscle_Vector3 MuscleSettings;

        [Tooltip("If you want to blend out component effects you can use this one variable, disables all calculations when value is equal zero")]
        [FPD_Suffix(0f, 1f)] public float AllEffectsBlend = 1f;

        /// <summary> Main blends excluding grounded blend </summary>
        public float GetFullBlend { get { return AllEffectsBlend * effectsBlend * fadeOffBlend; } }


        [Tooltip("Reading animator parameter bool/float value to define if character is accelerating / braking or stopping")]
        public string IsMovingAnimatorParam = "";
        private int _hash_acc = -1;
        private AnimatorControllerParameterType _accType = AnimatorControllerParameterType.Bool;

        [Tooltip("Reading animator parameter bool value to define if character is grounded")]
        public string IsGroundedAnimatorParam = "";
        private int _hash_grnd = -1;

        /// <summary> Disable leaning animation and use just motion detection module </summary>
        [NonSerialized] public bool UseOnlyDetecting = false;


        [Tooltip("Reading animator parameter bool value to define if leaning animator should fade to disabled state")]
        public string FadeOffLeaningParam = "";
        [Range(0f, 1f)]
        [Tooltip("How quickly / slow the fade off all leaning animation blend should change")]
        public float FadeOffSpeed = 0.5f;

        private int _hash_fadeOff = -1;
        private float fadeOffBlend = 1f;

        private FMuscle_Vector3 ms { get { return MuscleSettings; } }

        // Custom controls for outside scripts
        [NonSerialized] public bool IsCharacterGrounded = true;
        [NonSerialized] public bool IsCharacterAccelerating = false;
        [NonSerialized] public float CustomDeltaTime = -1f;
        private float dt;
        private Rigidbody rig;
        private bool forceTransformBased = false;
        Animator unityanim;
        public bool WasInitialized { get; private set; } = false;

        internal void Initialize(MonoBehaviour creator)
        {
            Owner = creator;

            if (UseOnlyDetecting == false)
            {
                if (FootsOrigin == null)
                {
                    GameObject origin = new GameObject("Generated Foots Origin");
                    FootsOrigin = origin.transform;

                    origin.transform.SetParent(BaseTransform);
                    origin.transform.localPosition = Vector3.zero;
                    origin.transform.localRotation = Quaternion.identity;

                    Transform p = SpineStart;
                    while (p != null && p.parent != BaseTransform)
                    {
                        p = p.parent;
                    }

                    if (!p) { UnityEngine.Debug.Log("[Leaning Animator] No Bones Assigned!"); return; }

                    p.SetParent(FootsOrigin, true);

                    UnityEngine.Debug.Log("[Leaning Animator] FootsOrigin wasn't assigned so generated custom one");
                }
                else
                {
                    float refDist = 0.025f;
                    if (SpineStart) refDist = Vector3.Distance(SpineStart.position, BaseTransform.position) * 0.05f;

                    // Generating corrected parent for foot origin
                    if (Vector3.Distance(FootsOrigin.transform.position, BaseTransform.position) > refDist)
                    {
                        GameObject origin = new GameObject("Generated Foots Origin");

                        origin.transform.SetParent(BaseTransform);
                        origin.transform.localPosition = Vector3.zero;
                        origin.transform.localRotation = Quaternion.identity;

                        FootsOrigin.SetParent(origin.transform, true);
                        FootsOrigin = origin.transform;
                    }
                }
            }

            accelerating = false;
            targetSideRot = 0f;
            rig = BaseTransform.GetComponentInChildren<Rigidbody>();

            PrepareBones();
            ResetPreVars();
            CheckForForcingTransformBasedVelocity();
            if (rig) prePos = rig.position; else prePos = BaseTransform.position;
            preVeloRaw = Vector3.zero;
            preRot = BaseTransform.eulerAngles.y;

            RefreshHashes();

            if (_hash_acc != -1 || _hash_grnd != -1 || _hash_fadeOff != -1)
            {
                unityanim = creator.transform.GetComponentInChildren<Animator>();
                if (!unityanim) unityanim = BaseTransform.GetComponent<Animator>();
                if (!unityanim) unityanim = BaseTransform.GetComponentInChildren<Animator>();
                if (!unityanim) unityanim = FTransformMethods.FindComponentInAllChildren<Animator>(creator.transform);
            }

            RefreshAnimiatorParams();

            WasInitialized = true;
        }

        public void RefreshAnimiatorParams()
        {
            RefreshHashes();

            if (_hash_acc != -1)
            {
                if (unityanim)
                {
                    for (int i = 0; i < unityanim.parameterCount; i++)
                    {
                        var param = unityanim.GetParameter(i);
                        if (param.nameHash == _hash_acc) _accType = (param.type);
                    }
                }
            }
        }

        void RefreshHashes()
        {
            if (!string.IsNullOrEmpty(IsMovingAnimatorParam)) _hash_acc = Animator.StringToHash(IsMovingAnimatorParam);
            if (!string.IsNullOrEmpty(IsGroundedAnimatorParam)) _hash_grnd = Animator.StringToHash(IsGroundedAnimatorParam);
            if (!string.IsNullOrEmpty(FadeOffLeaningParam)) _hash_fadeOff = Animator.StringToHash(FadeOffLeaningParam);
        }

        void CheckForForcingTransformBasedVelocity()
        {
            if (AccelerationDetection == EMotionDetection.TransformBased || rig == null) forceTransformBased = true;
            else forceTransformBased = false;
        }

        internal void Update()
        {
            if (AllEffectsBlend <= 0f) return;

            CheckForForcingTransformBasedVelocity();

            if (CustomDeltaTime < 0f)
            {
                if (UpdateMode == AnimatorUpdateMode.Normal) dt = Time.smoothDeltaTime;
                else if (UpdateMode == AnimatorUpdateMode.UnscaledTime) dt = Time.unscaledDeltaTime;
                else if (UpdateMode == AnimatorUpdateMode.AnimatePhysics) dt = Time.fixedDeltaTime;
            }
            else dt = CustomDeltaTime;

            if (UpdateMode != AnimatorUpdateMode.AnimatePhysics)
            {
                UpdateStack();
            }
        }


        private void CalibrateBones()
        {
            if (UseOnlyDetecting) return;
            if (Calibrate == false) return;
            if (spineStartBone != null) spineStartBone.PreCalibrate();
            if (spineMiddleBone != null) spineMiddleBone.PreCalibrate();
            if (chestBone != null) chestBone.PreCalibrate();
            if (neckBone != null) neckBone.PreCalibrate();
            if (lUpperarmBone != null) { lUpperarmBone.PreCalibrate(); if (lForearmBone != null) lForearmBone.PreCalibrate(); }
            if (rUpperarmBone != null) { rUpperarmBone.PreCalibrate(); if (rForearmBone != null) rForearmBone.PreCalibrate(); }
        }


        private void UpdateStack()
        {
            CalibrateFootsOrigin();
            CalibrateBones();

            if( AccelerationDetection == EMotionDetection.CustomDetection_AutoDetectionOFF ) // Blending out when jumping --------------------------
            {
                if( unityanim )
                {
                    if( _hash_acc != -1 )
                    {
                        if( _accType == AnimatorControllerParameterType.Bool )
                            IsCharacterAccelerating = unityanim.GetBool( _hash_acc );
                        else if( _accType == AnimatorControllerParameterType.Float )
                            IsCharacterAccelerating = unityanim.GetFloat( _hash_acc ) > 0.01f;
                        else if( _accType == AnimatorControllerParameterType.Int )
                            IsCharacterAccelerating = unityanim.GetInteger( _hash_acc ) > 0;

                        accelerating = IsCharacterAccelerating;
                    }
                }
            }

            if( unityanim )
            {
                if( !TryAutoDetectGround )
                    if( _hash_grnd != -1 ) IsCharacterGrounded = unityanim.GetBool( _hash_grnd );

                if( _hash_fadeOff != -1 )
                {
                    bool fadeOff = unityanim.GetBool( _hash_fadeOff );

                    if( fadeOff )
                    {
                        float spd = Mathf.LerpUnclamped( 3f, 16f, FadeOffSpeed );
                        fadeOffBlend = Mathf.MoveTowards( fadeOffBlend, 0f, dt * spd );
                    }
                    else
                    {
                        float spd = Mathf.LerpUnclamped( 2f, 11f, FadeOffSpeed );
                        fadeOffBlend = Mathf.MoveTowards( fadeOffBlend, 1f, dt * spd );
                    }
                }
            }

            if( !IsCharacterGrounded )
                effectsBlend = Mathf.Max( 0f, effectsBlend - dt * 4f );
            else
                effectsBlend = Mathf.Min( 1f, effectsBlend + dt * 4f );

            if( AllEffectsBlend <= 0f ) return;
            if( effectsBlend <= 0f ) return; // Not updating if not used -----------------------


            // Preparing reference factors ----------------------------------------------------

            //if (customDetection == false)
            //    velocity = Vector3.SmoothDamp(velocity, fixedVelocity, ref veloHelper, (1f - RootRapidity) * 0.1f, Mathf.Infinity, dt);

            if( AccelerationDetection == EMotionDetection.CustomDetection_AutoDetectionOFF )
            {
                if( customAccelerationVelocity != null )
                {
                    mainVeloMagnitude = customAccelerationVelocity.Value;
                    forwardVeloMagnitude = customAccelerationVelocity.Value;
                }

                if( IsCharacterAccelerating ) accelerating = true; else accelerating = false;
                brakingOrStop = !accelerating;
                if( forwardVeloMagnitude < ObjSpeedWhenBraking ) brakingOrStop = true;
            }
            else if( forceTransformBased )
            {
                VelocityDetectionTransform();
            }

            UpdateAngularVelocity();
        }

        public void LateUpdate_Bones()
        {
            if (UseOnlyDetecting) return;

            #region Support solution for animate physics mode -----

            if( UpdateMode == AnimatorUpdateMode.AnimatePhysics )
            {
                //if (!lateFixedIsRunning) { Owner.StartCoroutine(LateFixed()); }
                if( fixedAllow ) fixedAllow = false; else return;
            }
            //else if (lateFixedIsRunning) { Owner.StopCoroutine(LateFixed()); lateFixedIsRunning = false; }

            #endregion


            CalculateForwardSwayRotationValue();
            CalculateSideSwayRotationValue();

            UpdateRoot();
            UpdateSpine();
            UpdateArms();
            ComputeGroundAlign();
            UpdateSpineGroundAlign();
        }

        public void LateUpdate_Post()
        {
            if (forceTransformBased) if (UpdateMode != AnimatorUpdateMode.AnimatePhysics) ResetPreVars();
        }

        internal void LateUpdate()
        {
            LateUpdate_Bones();
            LateUpdate_Post();
        }




        internal void FixedUpdate() // Sync with physics --------------------------------------
        {
            if (AllEffectsBlend <= 0f) return;

            //if (AccelerationDetection == EMotionDetection.TransformBased) return;

            if( customDetection == false)
            {
                if (!forceTransformBased)
                {
                    fixedVelocity = rig.position - prePos;
                    localFixedVelocity = BaseTransform.InverseTransformDirection(fixedVelocity);
                }
            }

            if (UpdateMode == AnimatorUpdateMode.AnimatePhysics)
            {
                UpdateStack();
            }

            if (rig && AccelerationDetection != EMotionDetection.TransformBased) ComputeVelo();

            if (customDetection == false)
                if (!forceTransformBased || UpdateMode == AnimatorUpdateMode.AnimatePhysics)
                    ResetPreVars();

            if (TryAutoDetectGround || GroundAlignBlend > 0f ) AutoDetectGround();

            fixedAllow = true;
        }


        float zeroVeloTrTime = -1f;
        bool zeroVeloTrDetected = false;
        Vector3 smthTrVelo = Vector3.zero;
        Vector3 sd_smthTrVelo = Vector3.zero;
        void VelocityDetectionTransform()
        {
            Vector3 newVelo = BaseTransform.position - prePos;

            #region Zero Velocity Preventing Algorithm

            float treshold = ObjSpeedWhenRunning * 0.0004f;
            if (preVeloRaw.sqrMagnitude > treshold && newVelo.sqrMagnitude < treshold * 0.2f)
            {
                if (zeroVeloTrDetected == false)
                {
                    zeroVeloTrDetected = true;
                    zeroVeloTrTime = Time.time;
                    newVelo = preVeloRaw;
                }
            }
            else
            {
                zeroVeloTrDetected = false;
            }

            if (zeroVeloTrDetected)
            {
                if (Time.time - zeroVeloTrTime > 0.15f) { zeroVeloTrDetected = false; }// newVelo = Vector3.zero;
                else newVelo = preVeloRaw;
            }

            #endregion

            preVeloRaw = newVelo;

            fixedVelocity = newVelo / Mathf.Max(0.001f, dt);
            smthTrVelo = Vector3.SmoothDamp(smthTrVelo, fixedVelocity, ref sd_smthTrVelo, 0.125f, 3000f, dt);
            fixedVelocity = smthTrVelo;

            localFixedVelocity = BaseTransform.InverseTransformDirection(fixedVelocity);
            prePos = BaseTransform.position;

            ComputeVelo();
        }


        void ComputeVelo()
        {
            Vector3 noYVelo = fixedVelocity;
            noYVelo.y = 0f;

            if (AccelerationDetection != EMotionDetection.CustomDetection_AutoDetectionOFF) fixedVelocity.y = 0f;

            float fixedDeltaMul = 50f;
            if (forceTransformBased) fixedDeltaMul = 1f;

            if (customAccelerationVelocity == null)
            {
                mainVeloMagnitude = noYVelo.magnitude * fixedDeltaMul;
                forwardVeloMagnitude = localFixedVelocity.z * fixedDeltaMul;
            }
            else
            {
                mainVeloMagnitude = customAccelerationVelocity.Value;
                forwardVeloMagnitude = customAccelerationVelocity.Value;
            }

            sideVeloMagnitude = localFixedVelocity.x * fixedDeltaMul;

            if (AccelerationDetection != EMotionDetection.CustomDetection_AutoDetectionOFF) AutoDetectVelocity();
        }

        public void OnEnable()
        {
            if( WasInitialized == false ) return;
            AfterTeleportReset();
        }

        /// <summary> Coroutine for reset in update and fixed update which is required for the right sync </summary>
        public IEnumerator IEAfterTeleportReset()
        {
            AfterTeleportReset();
            yield return new WaitForFixedUpdate();
            AfterTeleportReset();
        }

        /// <summary> Restoring leaning properties after teleporting character to the new placement </summary>
        public void AfterTeleportReset()
        {
            accelerating = false;
            brakingOrStop = false;
            wasForward = true;

            fixedVelocity = Vector3.zero;
            localFixedVelocity = Vector3.zero;
            preVelo = Vector3.zero;
            preVeloRaw = Vector3.zero;
            TargetRotation = BaseTransform.rotation;
            preRot = BaseTransform.eulerAngles.y;

            if( rig ) prePos = rig.position; else prePos = BaseTransform.position;

            targetSideRot = 0f;
            slideLeanForw = 0f;
            sd_slideLeanForw = 0f;
            slideLeanSide = 0f;
            sd_slideLeanSide = 0f;
            smoothFade = 0f;
            sd_smoothFade = 0f;
            yawAxisAngularVelocity = 0f;
            accelerationProgress = 0f;
            sideAccelerationProgress = 0f;
            lastSideAccelerationSign = 0f;
            targetForwardRot = 0f;
            targetStrafeRot = 0f;
            lastForwardAccelerationSign = 0f;
            targetForwardRotSpine = 0f;
            mainVeloMagnitude = 0f;
            forwardVeloMagnitude = 0f;
            sideVeloMagnitude = 0f;
            upAxisVeloHelper = 0f;
            sideRotVeloHelper = 0f;
            accHelpVeloBrake = 0f;
            accHelpSideVeloBrake = 0f;
            spineAccHelpVelo = 0f;
            sd_targetForwardRot = 0f;
            sd_targetStrafeRot = 0f;
            brakingProgress = 0f;
            groundForwAngle = 0f;
            sd_groundForw = 0f;
            groundSideAngle = 0f;
            sd_groundSide = 0f;
        }

    }
}