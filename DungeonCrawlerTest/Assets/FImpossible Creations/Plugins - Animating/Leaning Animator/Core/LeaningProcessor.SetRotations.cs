using UnityEngine;

namespace FIMSpace
{
    public partial class LeaningProcessor
    {
        float smoothFade = 0f;
        float sd_smoothFade = 0f;

        void UpdateRoot() // Root transform rotation animation -----------------------------
        {
            if (RootBlend <= 0.001f) return;

            //float sideMildBrake = Mathf.Lerp(0.1f, 1f, Mathf.InverseLerp(ObjSpeedWhenBraking * 0.1f, ObjSpeedWhenBraking, mainVeloMagnitude));
            //smoothFade = Mathf.SmoothDamp(smoothFade, sideMildBrake, ref sd_smoothFade, 0.1f, 10000f, dt);
            //smoothFade = 1f;
            float forwardRotation = targetForwardRot * (ForwardSwayPower * 5f);

            if (CheckIfIsNull(rootMuscle) == false) // Muscles part
            {
                rootMuscle.Update(dt, rootMuscle.OutValue, forwardRotation, ms.Acceleration, ms.AccelerationLimit, ms.Damping, ms.BrakePower);
                forwardRotation = rootMuscle.OutValue;
            }

            float sideRotation = (targetSideRot + targetStrafeRot * RootStrafeSway * 2f  /* * smoothFade*/) * RootSideSway;

            // Smooth going back to zero rotation instead of rapid move
            float sideMildBrake = Mathf.Lerp(0.1f, 0f, Mathf.InverseLerp(ObjSpeedWhenBraking * 0.1f, ObjSpeedWhenBraking, mainVeloMagnitude));

            if (sideMildBrake > 0.01f)
            {
                float mul = Mathf.InverseLerp(1.5f, 0.5f, RootRapidity);
                smoothFade = Mathf.SmoothDamp(smoothFade, sideRotation, ref sd_smoothFade, sideMildBrake * mul, 10000f, dt);
            }
            else smoothFade = sideRotation;

            sideRotation = smoothFade;

            footOriginBone.RotateBy(forwardRotation * RootForwardSway * RootBlend * GetFullBlend, 0f, sideRotation * RootBlend * GetFullBlend);
            //FootsOrigin.localRotation = Quaternion.Euler(forwardRotation * RootForwardSway * RootBlend, 0f, sideRotation * RootBlend);
        }


        // Spine transforms animation -----------------------------------------------------

        float smoothTargetSpineRot = 0f;
        float smoothTgtSpnVelo = 0f;

        void UpdateSpine()
        {
            if (SpineBlend <= 0.001f) return;

            smoothTargetSpineRot = Mathf.SmoothDamp(smoothTargetSpineRot, targetForwardRotSpine, ref smoothTgtSpnVelo, 0.075f, SDMAX, dt);

            if (SpineStart)
            {
                // Side
                Vector3 startSpineRotOffset = SpineRightSway * targetSideRot * GetFullBlend;
                startSpineRotOffset.x += SpineSideLean * Mathf.Abs(targetSideRot) * GetFullBlend;

                Vector3 tgtRotOffs = startSpineRotOffset;

                if (CheckIfIsNull(spineTargetMuscle) == false) // Muscles Part
                    if (spineTargetMuscle.Initialized)
                    {
                        spineTargetMuscle.Update(dt, tgtRotOffs, ms.Acceleration, ms.AccelerationLimit, ms.Damping, ms.BrakePower);
                        tgtRotOffs = spineTargetMuscle.ProceduralPosition;
                    }

                tgtRotOffs.x = Mathf.Clamp(tgtRotOffs.x, -ClampSpineSway, ClampSpineSway);
                tgtRotOffs += _UserStartBoneAddAngles + spineStartGroundAdjust;

                spineStartBone.RotateByDynamic(tgtRotOffs * SpineBlend * AllEffectsBlend * fadeOffBlend, 1f);

                // Forward
                Vector3 additionalRotOffsets = (SpineForwardSway * smoothTargetSpineRot + ConstantAddRotation) * GetFullBlend * SpineBlend;
                additionalRotOffsets.x = Mathf.Clamp(additionalRotOffsets.x, -ClampSpineSway, ClampSpineSway);

                Vector3 addRotOffs = additionalRotOffsets;

                if (CheckIfIsNull(spineAdditionalMuscle) == false) // Muscles Part
                    if (spineAdditionalMuscle.Initialized)
                    {
                        spineAdditionalMuscle.Update(dt, addRotOffs, ms.Acceleration, ms.AccelerationLimit, ms.Damping, ms.BrakePower);
                        addRotOffs = spineAdditionalMuscle.ProceduralPosition;
                    }

                if (SpineMiddle && Chest) // Start spine, middle spine and chest
                {
                    spineStartBone.RotateByDynamic(addRotOffs * 0.65f, 1f);
                    spineMiddleBone.RotateByDynamic(addRotOffs * 0.225f + _UserMidBoneAddAngles + spineMidGroundAdjust, 1f);
                    chestBone.RotateByDynamic(addRotOffs * 0.175f, 1f);
                }
                else
                {
                    if (Chest) // Start spine and chest
                    {
                        spineStartBone.RotateByDynamic(addRotOffs * 0.6f, 1f);
                        chestBone.RotateByDynamic(addRotOffs * 0.3f, 1f);
                    }
                    else // Just Start spine
                    {
                        spineStartBone.RotateByDynamic(addRotOffs, 1f);
                    }
                }

                if (neckBone != null)
                {
                    Vector3 neckCompensRotOffset = (SpineForwardSway * NeckCompensation * SpineBlend * -smoothTargetSpineRot - ConstantAddRotation) * GetFullBlend;
                    neckBone.RotateByDynamic(neckCompensRotOffset, 1f);
                }
            }

        }

        float armsSmoothSideRot = 0f;
        float sd_armsSmoothSideRot = 0f;
        float armsSmoothFRot = 0f;
        float sd_armsSmoothFRot = 0f;

        void UpdateArms()
        {
            if (ArmsBlend <= 0.001f) return;

            armsSmoothFRot = Mathf.SmoothDamp(armsSmoothFRot, targetForwardRot, ref sd_armsSmoothFRot, 0.15f, 10000f, dt);
            armsSmoothSideRot = Mathf.SmoothDamp(armsSmoothSideRot, targetSideRot, ref sd_armsSmoothSideRot, 0.07f, 10000f, dt);

            float armsForw = armsSmoothFRot * -10f * ArmsBlend * ArmsForwardSway * effectsBlend * AllEffectsBlend;
            float armsSide = armsSmoothSideRot * -10f * ArmsBlend * ArmsSideSway * effectsBlend * AllEffectsBlend;
            float armsSideABS = -Mathf.Abs(armsSide);

            if (rUpperarmBone != null)
            {
                if (rForearmBone != null)
                {
                    rUpperarmBone.RotateYBy(armsForw);
                    rForearmBone.RotateYBy(-armsForw);

                    float side = -Mathf.Min(0f, armsSide);
                    if (side == 0) side = -armsSideABS * 0.35f;

                    if (CheckIfIsNull(rArmMuscle) == false) // Muscles part
                        {
                            rArmMuscle.Update(dt, rArmMuscle.OutValue, side, ms.Acceleration, ms.AccelerationLimit, ms.Damping, ms.BrakePower);
                            side = rArmMuscle.OutValue;
                        }

                    rUpperarmBone.RotateXBy(-side * 0.2f);
                    rUpperarmBone.RotateYBy(-side * 0.2f);
                    rUpperarmBone.RotateZBy(side * 0.3f);
                    rForearmBone.RotateYBy(side * .4f);
                }
            }

            if (lUpperarmBone != null)
            {
                if (lForearmBone != null)
                {
                    lUpperarmBone.RotateYBy(-armsForw);
                    lForearmBone.RotateYBy(armsForw);

                    float side = -Mathf.Max(0f, armsSide);
                    if (side == 0) side = armsSideABS * 0.35f;

                    if (CheckIfIsNull(lArmMuscle) == false) // Muscles part
                        {
                            lArmMuscle.Update(dt, lArmMuscle.OutValue, side, ms.Acceleration, ms.AccelerationLimit, ms.Damping, ms.BrakePower);
                            side = lArmMuscle.OutValue;
                        }

                    lUpperarmBone.RotateXBy(side * 0.2f);
                    lUpperarmBone.RotateYBy(-side * 0.2f);
                    lUpperarmBone.RotateZBy(side * 0.3f);
                    lForearmBone.RotateYBy(side * 0.5f);
                }
            }


        }
    }
}