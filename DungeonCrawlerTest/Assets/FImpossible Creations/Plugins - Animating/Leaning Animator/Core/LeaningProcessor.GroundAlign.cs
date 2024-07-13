using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {
        [Tooltip("Set to zero to nto use ground align, bring up to blend it's amount")]
        [FPD_Suffix(0f, 1f)] public float GroundAlignBlend = 0f;
        [Tooltip("Set how rapid should be alignment to ground animation")]
        [Range(0f, 1f)] public float GroundAlignRapidity = 0.6f;
        [Tooltip("If you want to rotate character model to ground align only when it's moving - pretty useful")]
        public bool AlignOnlyWhenMoving = false;
        [FPD_Suffix(0f, 75f, FPD_SuffixAttribute.SuffixMode.FromMinToMaxRounded, "°")] public float ClampGroundAlign = 25;
        [Tooltip("x is forward/back rotation | y is sides align rotation")] public Vector2 MutiplyGroundAxisRotation = Vector2.one;
        [Tooltip("With this curve you can make character align a bit when standing in place but align more when walking and more when running etc. time = 1 means align when object move with running speed")] [FPD_FixedCurveWindow(0f, 0f, 1f, 2f)] public AnimationCurve AlignAmountOnSpeed = AnimationCurve.EaseInOut(0.1f, 1f, 0.3f, 1f);//AnimationCurve.EaseInOut(0.1f, 0f, 0.3f, 1f);

        [Space(5)]
        [Tooltip("Check which collision layers should be treated as ground")]
        public LayerMask GroundMask = ~(0 << 0);
        [Tooltip("Set length of raycasting below character's foots")]
        public float GroundCastLength = 0.2f;
        [Tooltip("If in your game single raycast can be not enough then box cast can check ground area more precisely if raycast below character not found any ground hit")]
        public float BoxCastSize = 0f;

        public bool _UserUseCustomRaycast = false;
        internal RaycastHit _UserCustomRaycast;

        RaycastHit latestGroundHit;
        void AutoDetectGround()
        {
            if (_UserUseCustomRaycast)
            {
                latestGroundHit = _UserCustomRaycast;

                if (TryAutoDetectGround)
                {
                    IsCharacterGrounded = latestGroundHit.transform != null;
                }
            }
            else
            {
                Vector3 castOrigin = BaseTransform.position + BaseTransform.up * GroundCastLength;
                if (Physics.Raycast(castOrigin, -BaseTransform.up, out latestGroundHit, GroundCastLength * 2f, GroundMask, QueryTriggerInteraction.Ignore))
                {
                    if (TryAutoDetectGround) IsCharacterGrounded = true;
                }
                else
                {
                    if (TryAutoDetectGround) IsCharacterGrounded = false;

                    if (BoxCastSize > 0f)
                    {
                        float boxHalf = BoxCastSize * 0.5f;
                        Vector3 boxExt = new Vector3(boxHalf, boxHalf, boxHalf);

                        if (Physics.BoxCast(castOrigin + BaseTransform.up * boxHalf, boxExt, -BaseTransform.up, out latestGroundHit, BaseTransform.rotation, GroundCastLength * 2f, GroundMask, QueryTriggerInteraction.Ignore))
                        {
                            if (TryAutoDetectGround) IsCharacterGrounded = true;
                        }
                    }
                }
            }
        }


        float groundForwAngle = 0f;
        float sd_groundForw = 0f;
        float groundSideAngle = 0f;
        float sd_groundSide = 0f;

        void ComputeGroundAlign()
        {
            if (GroundAlignBlend <= 0.01f) return;

            if (footOriginBone != null)
            {
                float targetF;
                float targetS;

                bool alignNow = true;

                if (latestGroundHit.transform == null) alignNow = false;
                if (AlignOnlyWhenMoving) if (alignNow) if (accelerating == false) alignNow = false;
                

                if (alignNow)
                {
                    Quaternion alignRot = Quaternion.FromToRotation(Vector3.up, BaseTransform.InverseTransformDirection( latestGroundHit.normal) );
                    Vector3 nAngles = alignRot.eulerAngles;
                    targetF = Mathf.Clamp( FEngineering.WrapAngle( nAngles.x), -ClampGroundAlign, ClampGroundAlign);
                    targetS = Mathf.Clamp(FEngineering.WrapAngle(nAngles.z), -ClampGroundAlign, ClampGroundAlign);
                    targetF *= MutiplyGroundAxisRotation.x;
                    targetS *= MutiplyGroundAxisRotation.y;
                }
                else
                {
                    targetF = 0f;
                    targetS = 0f;
                }

                float curveMul = AlignAmountOnSpeed.Evaluate(mainVeloMagnitude / ObjSpeedWhenRunning);
                targetF *= curveMul;
                targetS *= curveMul;

                groundForwAngle = Mathf.SmoothDampAngle(groundForwAngle, targetF, ref sd_groundForw, (1f - GroundAlignRapidity) * 0.25f + 0.05f, SDMAX, dt);
                groundSideAngle = Mathf.SmoothDampAngle(groundSideAngle, targetS, ref sd_groundSide, (1f - GroundAlignRapidity) * 0.25f + 0.05f, SDMAX, dt);

                targetF = groundForwAngle * GroundAlignBlend * GetFullBlend;
                targetS = groundSideAngle * GroundAlignBlend * GetFullBlend;

                footOriginBone.RotateBy(targetF, 0f, targetS);
            }
        }

    }
}