using UnityEngine;

namespace FIMSpace.FTools
{
    public class UniRotateBone
    {
        public Transform transform { get; protected set; }

        public Vector3 initialLocalPosition { get; protected set; }
        public Quaternion initialLocalRotation { get; protected set; }
        public Vector3 initialLocalPositionInRootSpace { get; protected set; }
        public Quaternion initialLocalRotationInRootSpace { get; protected set; }

        public Vector3 right { get; protected set; }
        public Vector3 up { get; protected set; }
        public Vector3 forward { get; protected set; }

        public Vector3 dright { get; protected set; }
        public Vector3 dup { get; protected set; }
        public Vector3 dforward { get; protected set; }

        public Vector3 fromParentForward { get; protected set; }
        public Vector3 fromParentCross { get; protected set; }

        public Vector3 keyframedPosition { get; protected set; }
        public Quaternion keyframedRotation { get; protected set; }
        public Quaternion mapping { get; protected set; }
        public Quaternion dmapping { get; protected set; }
        public Transform root { get; protected set; }


        public UniRotateBone(Transform t, Transform root)
        {
            transform = t;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;

            if (root)
            {
                initialLocalPositionInRootSpace = root.InverseTransformPoint(t.position);
                initialLocalRotationInRootSpace = FEngineering.QToLocal(root.rotation, t.rotation);
            }

            forward = transform.InverseTransformDirection(root.forward);
            up = transform.InverseTransformDirection(root.up);
            right = transform.InverseTransformDirection(root.right);

            dforward = Quaternion.FromToRotation(forward, Vector3.forward) * Vector3.forward;
            dup = Quaternion.FromToRotation(up, Vector3.up) * Vector3.up;
            dright = Quaternion.FromToRotation(right, Vector3.right) * Vector3.right;

            if (t.parent) fromParentForward = GetFromParentForward().normalized;
            else fromParentForward = forward;

            fromParentCross = -Vector3.Cross(fromParentForward, forward);

            mapping = Quaternion.FromToRotation(right, Vector3.right);
            mapping *= Quaternion.FromToRotation(up, Vector3.up);

            dmapping = Quaternion.FromToRotation(fromParentForward, Vector3.right);
            dmapping *= Quaternion.FromToRotation(up, Vector3.up);

            this.root = root;
        }


        public Vector3 GetFromParentForward()
        {
            return transform.InverseTransformDirection(transform.position - transform.parent.position);
        }

        #region Complex look target rotation


        public Vector3 forwardReference { get; private set; }
        public Vector3 upReference { get; private set; }
        public Vector3 rightCrossReference { get; private set; }
        private Vector3 dynamicUpReference = Vector3.up;

        public Quaternion GetRootCompensateRotation(Quaternion initPelvisInWorld, Quaternion currInWorld, float armsRootCompensate)
        {
            Quaternion pre;

            if (armsRootCompensate > 0f)
            {
                // Transforming wrinst rotation to local space of pelvis
                pre = FEngineering.QToLocal(currInWorld, transform.parent.rotation);
                pre = FEngineering.QToWorld(initPelvisInWorld, pre); // Transpose from pelvis local space but with init static rotation

                if (armsRootCompensate < 1f)
                    pre = Quaternion.Lerp(transform.parent.rotation, pre, armsRootCompensate);
            }
            else
                pre = transform.parent.rotation;

            return pre;
        }

        public void RefreshCustomAxis(Vector3 up, Vector3 forward)
        {
            if (transform == null) return;
            forwardReference = Quaternion.Inverse(transform.parent.rotation) * root.rotation * forward;
            upReference = Quaternion.Inverse(transform.parent.rotation) * root.rotation * up;
            rightCrossReference = Vector3.Cross(upReference, forwardReference);
        }

        public void RefreshCustomAxis(Vector3 up, Vector3 forward, Quaternion customParentRot)
        {
            forwardReference = Quaternion.Inverse(customParentRot) * root.rotation * forward;
            upReference = Quaternion.Inverse(customParentRot) * root.rotation * up;
            rightCrossReference = Vector3.Cross(upReference, forwardReference);
        }

        /// <summary> Execute RefreshCustomAxis() before this  |  Do rotation = RotateCustomAxis() * transform.rotation </summary>
        public Quaternion RotateCustomAxis(float x, float y, UniRotateBone oRef)
        {
            // With calculated angles we can get rotation by rotating around desired axes
            Vector3 lookDirectionParent = Quaternion.AngleAxis(y, oRef.upReference) * Quaternion.AngleAxis(x, rightCrossReference) * oRef.forwardReference;

            // Making look and up direction perpendicular
            Vector3 upDirGoal = oRef.upReference;
            Vector3.OrthoNormalize(ref lookDirectionParent, ref upDirGoal);

            // Look and up directions in lead's parent space
            Vector3 lookDir = lookDirectionParent;
            dynamicUpReference = upDirGoal;
            Vector3.OrthoNormalize(ref lookDir, ref dynamicUpReference);

            // Finally getting look rotation
            Quaternion lookRot = transform.parent.rotation * Quaternion.LookRotation(lookDir, dynamicUpReference);
            lookRot *= Quaternion.Inverse(transform.parent.rotation * Quaternion.LookRotation(oRef.forwardReference, oRef.upReference));
            return lookRot;
        }

        internal Quaternion GetSourcePoseRotation()
        {
            return FEngineering.QToWorld(root.rotation, initialLocalRotationInRootSpace);
        }

        /// <summary> Execute RefreshCustomAxis() before this </summary>
        public Vector2 GetCustomLookAngles(Vector3 direction, UniRotateBone orientationsReference)
        {
            // Target look rotation equivalent for LeadBone's parent
            Vector3 lookDirectionParent = Quaternion.Inverse(transform.parent.rotation) * (direction).normalized;
            Vector2 angles = Vector2.zero;

            // Getting angle offset in y axis - horizontal rotation
            angles.y = AngleAroundAxis(orientationsReference.forwardReference, lookDirectionParent, orientationsReference.upReference);

            Vector3 targetRight = Vector3.Cross(orientationsReference.upReference, lookDirectionParent);
            Vector3 horizontalPlaneTarget = lookDirectionParent - Vector3.Project(lookDirectionParent, orientationsReference.upReference);

            angles.x = AngleAroundAxis(horizontalPlaneTarget, lookDirectionParent, targetRight);

            return angles;
        }


        /// <summary>
        /// Calculate angle between two directions around defined axis
        /// </summary>
        public static float AngleAroundAxis(Vector3 firstDirection, Vector3 secondDirection, Vector3 axis)
        {
            // Projecting to orthogonal target axis plane
            firstDirection -= Vector3.Project(firstDirection, axis);
            secondDirection -= Vector3.Project(secondDirection, axis);

            float angle = Vector3.Angle(firstDirection, secondDirection);

            return angle * (Vector3.Dot(axis, Vector3.Cross(firstDirection, secondDirection)) < 0 ? -1 : 1);
        }

        #endregion


        public Quaternion DynamicMapping()
        {
            Quaternion dMap = Quaternion.FromToRotation(right, transform.InverseTransformDirection(root.right));
            dMap *= Quaternion.FromToRotation(up, transform.InverseTransformDirection(root.up));
            return dMap;
        }

        public void CaptureKeyframeAnimation()
        {
            keyframedPosition = transform.position;
            keyframedRotation = transform.rotation;
        }

        public void RotateBy(float x, float y, float z)
        {
            Quaternion rot = transform.rotation;
            if (x != 0f) rot *= Quaternion.AngleAxis(x, right);
            if (y != 0f) rot *= Quaternion.AngleAxis(y, up);
            if (z != 0f) rot *= Quaternion.AngleAxis(z, forward);
            transform.rotation = rot;
        }

        public void RotateBy(Vector3 angles)
        {
            RotateBy(angles.x, angles.y, angles.z);
        }

        public void RotateBy(Vector3 angles, float blend)
        {
            RotateBy(BlendAngle(angles.x, blend), BlendAngle(angles.y, blend), BlendAngle(angles.z, blend));
        }

        public void RotateByDynamic(Vector3 angles)
        {
            RotateByDynamic(angles.x, angles.y, angles.z);
        }

        public void RotateByDynamic(float x, float y, float z)
        {
            Quaternion rot = transform.rotation;
            if (x != 0f) rot *= Quaternion.AngleAxis(x, transform.InverseTransformDirection(root.right));
            if (y != 0f) rot *= Quaternion.AngleAxis(y, transform.InverseTransformDirection(root.up));
            if (z != 0f) rot *= Quaternion.AngleAxis(z, transform.InverseTransformDirection(root.forward));
            transform.rotation = rot;
        }

        public Quaternion GetAngleRotation(float x, float y, float z)
        {
            Quaternion rot = Quaternion.identity;
            if (x != 0f) rot *= Quaternion.AngleAxis(x, right);
            if (y != 0f) rot *= Quaternion.AngleAxis(y, up);
            if (z != 0f) rot *= Quaternion.AngleAxis(z, forward);
            return rot;
        }

        public Quaternion GetAngleRotationDynamic(float x, float y, float z)
        {
            Quaternion rot = Quaternion.identity;
            if (x != 0f) rot *= Quaternion.AngleAxis(x, transform.InverseTransformDirection(root.right));
            if (y != 0f) rot *= Quaternion.AngleAxis(y, transform.InverseTransformDirection(root.up));
            if (z != 0f) rot *= Quaternion.AngleAxis(z, transform.InverseTransformDirection(root.forward));
            return rot;
        }

        public Quaternion GetAngleRotationDynamic(Vector3 angles)
        {
            return GetAngleRotationDynamic(angles.x, angles.y, angles.z);
        }

        public void RotateByDynamic(Vector3 angles, float blend)
        {
            RotateByDynamic(BlendAngle(angles.x, blend), BlendAngle(angles.y, blend), BlendAngle(angles.z, blend));
        }

        public void RotateByDynamic(float x, float y, float z, float blend)
        {
            RotateByDynamic(BlendAngle(x, blend), BlendAngle(y, blend), BlendAngle(z, blend));
        }

        public void RotateByDynamic(float x, float y, float z, Quaternion orientation)
        {
            Quaternion rot = transform.rotation;
            if (x != 0f) rot *= Quaternion.AngleAxis(x, transform.InverseTransformDirection(orientation * Vector3.right));
            if (y != 0f) rot *= Quaternion.AngleAxis(y, transform.InverseTransformDirection(orientation * Vector3.up));
            if (z != 0f) rot *= Quaternion.AngleAxis(z, transform.InverseTransformDirection(orientation * Vector3.forward));
            transform.rotation = rot;
        }

        public void RotateXBy(float angle) { transform.rotation *= Quaternion.AngleAxis(angle, right); }
        public void RotateYBy(float angle) { transform.rotation *= Quaternion.AngleAxis(angle, up); }
        public void RotateZBy(float angle) { transform.rotation *= Quaternion.AngleAxis(angle, forward); }

        public void PreCalibrate()
        {
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
        }

        /// <summary> Bone must have parent </summary>
        public Quaternion RotationTowards(Vector3 toDir)
        {
            Quaternion fromTo = Quaternion.FromToRotation
            (
                transform.TransformDirection(fromParentForward).normalized, // Forward direction
                (toDir).normalized // Look direction
            );

            return fromTo * transform.rotation;
        }

        /// <summary> Bone must have parent </summary>
        public Quaternion RotationTowardsDynamic(Vector3 toDir)
        {
            Quaternion fromTo = Quaternion.FromToRotation
            (
                (transform.position - transform.parent.position).normalized, // Forward direction
                (toDir).normalized // Look direction
            );

            return fromTo * transform.rotation;
        }

        public static float BlendAngle(float angle, float blend) { return Mathf.LerpAngle(0f, angle, blend); }

        public Vector3 Dir(Vector3 forward) { return transform.TransformDirection(forward); }
        public Vector3 IDir(Vector3 forward) { return transform.InverseTransformDirection(forward); }
    }
}