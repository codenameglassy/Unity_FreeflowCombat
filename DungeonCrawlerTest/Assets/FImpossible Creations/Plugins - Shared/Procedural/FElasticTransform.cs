using UnityEngine;

namespace FIMSpace.FTools
{
    public class FElasticTransform
    {
        public Transform transform;

        private FElasticTransform elChild;
        private FElasticTransform elParent;

        // Position Muscle -----------
        [FPD_Suffix(0f, 1f)] public float RotationRapidness = 0.1f;
        /// <summary> Bounce effect muscle </summary>
        public FMuscle_Vector3 PositionMuscle { get; private set; }

        public Vector3 ProceduralPosition { get; private set; }
        private Quaternion proceduralRotation;

        /// <summary> Used for blending </summary>
        public Vector3 sourceAnimationPosition { get; private set; }

        private float delta = 0.01f;

        public void Initialize(Transform transform)
        {
            if (transform == null) return;

            this.transform = transform;

            ProceduralPosition = transform.position;
            proceduralRotation = transform.rotation;

            sourceAnimationPosition = transform.position;

            PositionMuscle = new FMuscle_Vector3();
            PositionMuscle.Initialize(transform.position);
        }

        public void OverrideProceduralPosition(Vector3 newPos)
        { ProceduralPosition = newPos; }

        public void OverrideProceduralPositionHard(Vector3 newPos)
        { ProceduralPosition = newPos; PositionMuscle.OverrideProceduralPosition(newPos); sourceAnimationPosition = newPos; }

        public void OverrideProceduralRotation(Quaternion newRot)
        { proceduralRotation = newRot; }

        public void CaptureSourceAnimation()
        { sourceAnimationPosition = transform.position; }

        public void SetChild(FElasticTransform child)
        { elChild = child; }

        public FElasticTransform GetElasticChild()
        { return elChild; }

        public void SetParent(FElasticTransform parent)
        { elParent = parent; }

        public void UpdateElasticPosition(float delta)
        {
            this.delta = delta;

            if (elParent != null)
            {
                FElasticTransform parent = elParent.transform == null ? elParent.elParent : elParent;
                Quaternion referenceRotation = parent.transform.rotation;

                // Target position for elastic bones
                Vector3 targetPos = parent.ProceduralPosition + referenceRotation * transform.localPosition;
                PositionMuscle.Update(delta, targetPos);

                ProceduralPosition = PositionMuscle.ProceduralPosition;
            }
            else
                ProceduralPosition = transform.position;
        }


        public void UpdateElasticPosition(float delta, Vector3 influenceOffset)
        {
            this.delta = delta;

            if (elParent != null)
            {
                PositionMuscle.MotionInfluence(influenceOffset);
                UpdateElasticPosition(delta);
            }
            else
                ProceduralPosition = transform.position;
        }

        
        public void UpdateElasticRotation(float blending)
        {
            if (elChild != null) // We have child - procedural mixed with source animator local pos
            {
                Quaternion targetRotation;

                if (blending < 1f)
                    targetRotation = GetTargetRotation(elChild.BlendVector(elChild.ProceduralPosition, blending), transform.TransformDirection(elChild.transform.localPosition), blending);
                else
                    targetRotation = GetTargetRotation(elChild.ProceduralPosition, transform.TransformDirection(elChild.transform.localPosition), ProceduralPosition);

                if (RotationRapidness < 1f)
                {
                    proceduralRotation = Quaternion.Lerp(proceduralRotation, targetRotation, Mathf.Min(1f, delta * (10f + RotationRapidness * 50f)));
                    transform.rotation = proceduralRotation;
                }
                else
                    transform.rotation = targetRotation;
            }
        }


        public Vector3 BlendVector(Vector3 target, float blend)
        {
            return Vector3.LerpUnclamped(sourceAnimationPosition, target, blend);
        }

        public Quaternion GetTargetRotation(Vector3 lookPos, Vector3 localOffset, float blending)
        {
            return Quaternion.FromToRotation(localOffset, (lookPos - BlendVector(ProceduralPosition, blending)).normalized) * transform.rotation;
        }

        public Quaternion GetTargetRotation(Vector3 lookPos, Vector3 localOffset, Vector3 pos)
        {
            return Quaternion.FromToRotation(localOffset, (lookPos - pos).normalized) * transform.rotation;
        }
    }
}