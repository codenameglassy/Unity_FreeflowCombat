using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {
        [Tooltip("Control how much effect of spine ground align you want to apply")] [FPD_Suffix(0f, 3f, FPD_SuffixAttribute.SuffixMode.PercentageUnclamped)] public float SpineGroundAlign = 0f;
        [Tooltip("Lower value will result in more sudden change of spine rotation")] [Range(0f, .4f)] public float SpineGrAdjustDuration = 0.15f;
        [Tooltip("With this curve you can make character bend with custom multiplication on bigger or smaller slopes, time goes from 0 to 90 slope")] [FPD_FixedCurveWindow(0f, 0f, 90f, 2f)] public AnimationCurve SlopeSpineLean = AnimationCurve.EaseInOut(10f, 1f, 75f, 1.25f);
        [Tooltip("Multiplication for adjusting spine rotating forward (x) and to sizes (y). \nUseful when you don't want rotate spine to sides so much.")] public Vector2 AdjustForwAndSide = new Vector2(1f, 0.75f);

        float slideLeanForw = 0f;
        float sd_slideLeanForw = 0f;

        float slideLeanSide = 0f;
        float sd_slideLeanSide = 0f;

        internal float? _UserCustomSlopeAngle = null;

        internal Vector3 _UserStartBoneAddAngles = Vector3.zero;
        internal Vector3 _UserMidBoneAddAngles = Vector3.zero;

        private Vector3 spineStartGroundAdjust = Vector3.zero;
        private Vector3 spineMidGroundAdjust = Vector3.zero;

        /// <summary> Very big value for smooth damp limit </summary>
        const float SDMAX = 10000000f;

        void UpdateSpineGroundAlign()
        {
            float spineBlend = SpineGroundAlign;
            if (spineBlend <= 0f) return;

            float slopeAngle = 0f;
            if (_UserCustomSlopeAngle == null)
            {
                if (latestGroundHit.transform)
                {
                    slopeAngle = Vector3.Angle(latestGroundHit.normal, Vector3.up);
                }
            }
            else slopeAngle = _UserCustomSlopeAngle.Value;

            Vector3 slopeNormal = latestGroundHit.normal;

            bool alignSpine = true;
            if (AlignOnlyWhenMoving) if (IsCharacterAccelerating == false) alignSpine = false;
            if (slopeAngle < 5f) alignSpine = false;

            if (alignSpine)
            {
                float slopeOff = slopeAngle * SlopeSpineLean.Evaluate(slopeAngle);
                Vector3 slopeSlideDir = Vector3.ProjectOnPlane(new Vector3(slopeNormal.x, 0f, slopeNormal.z).normalized, Vector3.up);

                float slopeFDot = Vector3.Dot(BaseTransform.forward, slopeSlideDir);
                slideLeanForw = Mathf.SmoothDamp(slideLeanForw, slopeOff * slopeFDot * effectsBlend, ref sd_slideLeanForw, SpineGrAdjustDuration, SDMAX, dt);

                float slopeRDot = Vector3.Dot(BaseTransform.right, slopeSlideDir);
                slideLeanSide = Mathf.SmoothDamp(slideLeanSide, slopeOff * slopeRDot * effectsBlend, ref sd_slideLeanSide, SpineGrAdjustDuration, SDMAX, dt);

            }
            else
            {
                slideLeanForw = Mathf.SmoothDamp(slideLeanForw, 0f, ref sd_slideLeanForw, SpineGrAdjustDuration, SDMAX, dt);
                slideLeanSide = Mathf.SmoothDamp(slideLeanSide, 0f, ref sd_slideLeanSide, SpineGrAdjustDuration, SDMAX, dt);
            }

            spineStartGroundAdjust = new Vector3(slideLeanForw * SpineGroundAlign * AdjustForwAndSide.x, 0f, -slideLeanSide * SpineGroundAlign * AdjustForwAndSide.y);
            spineStartGroundAdjust = new Vector3(-slideLeanForw * 0.5f * SpineGroundAlign * AdjustForwAndSide.x, 0f, slideLeanSide * 0.5f * SpineGroundAlign * AdjustForwAndSide.y);
        }
    }
}