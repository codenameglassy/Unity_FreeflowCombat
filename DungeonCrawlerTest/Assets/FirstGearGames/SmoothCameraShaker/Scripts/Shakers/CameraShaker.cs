using UnityEngine;
using System.Collections.Generic;
using System;
using FirstGearGames.Utilities.Maths;

namespace FirstGearGames.SmoothCameraShaker
{
    public class CameraShaker : MonoBehaviour
    {
        #region Types.
        public enum ShakeTechniques
        {
            Matrix = 0,
            LocalSpace = 1
        }
        #endregion

        #region Public.
        /// <summary>
        /// Dispatched when shaking starts after previously being stopped.
        /// </summary>
        public event Action<CameraShaker> OnShakingStarted;
        /// <summary>
        /// Dispatched when shaking ends.
        /// </summary>
        public event Action<CameraShaker> OnShakingEnded;
        /// <summary>
        /// Dispatched every update a shake occurs.
        /// </summary>
        public event Action<CameraShaker, ShakeUpdate> OnShakeUpdate;
        /// <summary>
        /// Dispatched every fixed update a shake occurs. Contains the shake values from last update.
        /// </summary>
        public event Action<CameraShaker, ShakeUpdate> OnShakeFixedUpdate;
        /// <summary>
        /// Active instances which are shaking the camera.
        /// </summary>
        public List<ShakerInstance> ShakerInstances { get; private set; } = new List<ShakerInstance>();
        /// <summary>
        /// True if this CameraShaker is currently shaking.
        /// </summary>
        public bool Shaking { get; private set; }
        /// <summary>
        /// Current scale applied towards shakes on this CameraShaker. Acts as a multiplier towards ShakerInstances. 1f is normal scale.
        /// </summary>
        public float Scale { get; private set; } = 1f;
        /// <summary>
        /// Last shake values for camera after running UpdateShakers. For internal use only.
        /// </summary>
        internal ShakeValues FixedCamera { get; private set; } = null;
        #endregion

        #region Serialized.
        /// <summary>
        /// 
        /// </summary>
        [Tooltip("Technique used to shake the camera.")]
        [SerializeField]
        private ShakeTechniques _shakeTechnique = ShakeTechniques.Matrix;
        /// <summary>
        /// Technique used to shake the camera.
        /// </summary>
        public ShakeTechniques ShakeTechnique
        {
            get { return _shakeTechnique; }
            private set { _shakeTechnique = value; }
        }
        /// <summary>
        /// Sets the shake technique to use. Changing this value while shakes are occurring may create unwanted results.
        /// </summary>
        /// <param name="value"></param>
        public void SetShakeTechnique(ShakeTechniques value) { ShakeTechnique = value; }
        /// <summary>
        /// True for this CameraShaker to be set as the default shaker when enabled.
        /// </summary>
        [Tooltip("True for this CameraShaker to be set as the default shaker when enabled.")]
        [SerializeField]
        private bool _makeDefaultOnEnable = true;
        /// <summary>
        /// True to limit how much magnitude can be applied to this CameraShaker.
        /// </summary>
        [Tooltip("True to limit how much magnitude can be applied to this CameraShaker.")]
        [SerializeField]
        private bool _limitMagnitude = false;
        /// <summary>
        /// How much positional magnitude to limit this CameraShaker to.
        /// </summary>
        [Tooltip("How much positional magnitude to limit this CameraShaker to.")]
        [SerializeField]
        private float _positionalMagnitudeLimit = 10f;
        /// <summary>
        /// How much rotational manitude to limit this CameraShaker to.
        /// </summary>
        [Tooltip("How much rotational manitude to limit this CameraShaker to.")]
        [SerializeField]
        private float _rotationalMagnitudeLimit = 3f;
        #endregion

        #region Private.
        /// <summary>
        /// Camera on this gameObject.
        /// </summary>
        private Camera _camera;
        /// <summary>
        /// Last shake values for canvases after running UpdateShakers.
        /// </summary>
        private ShakeValues _fixedCanvases = null;
        /// <summary>
        /// Last shake values for rigidbodies after running UpdateShakers.
        /// </summary>
        private ShakeValues _fixedRigidbodies = null;
        /// <summary>
        /// Squared value of positional magnitude limit. This is used for faster calculations.
        /// </summary>
        private float _sqrPositionalMagnitudeLimit;
        /// <summary>
        /// Squared value of rotational magnitude limit. This is used for faster calculations.
        /// </summary>
        private float _sqrRotationalMagnitudeLimit;
        /// <summary>
        /// True if initialized.
        /// </summary>
        private bool _initialized = false;
        #endregion

        private void Awake()
        {
            FirstInitialize();
        }

        private void OnEnable()
        {
            if (_makeDefaultOnEnable)
                CameraShakerHandler.SetDefaultCameraShaker(this);
        }

        private void OnDisable()
        {
            Disable();
        }

        private void OnDestroy()
        {
            CameraShakerHandler.RemoveInstantiatedShaker(this);
        }

        /// <summary>
        /// Initializes this script for use. This should only be completed once.
        /// </summary>
        private void FirstInitialize()
        {
            if (_initialized)
                return;

            _camera = GetComponent<Camera>();
            CameraShakerHandler.AddInstantiatedShaker(this);
            CalculateSqrLimits();

            _initialized = true;
        }

        /// <summary>
        /// Calculates the squared limits.
        /// </summary>
        private void CalculateSqrLimits()
        {
            //Set squared values for faster calculations.
            if (_limitMagnitude)
            {
                _sqrPositionalMagnitudeLimit = Mathf.Pow(_positionalMagnitudeLimit, 2f);
                _sqrRotationalMagnitudeLimit = Mathf.Pow(_rotationalMagnitudeLimit, 2f);
            }
        }

        /// <summary>
        /// Checks if shakers need to be updated. This is for internal use only.
        /// </summary>
        /// <param name="position">Position offset after a shake.</param>
        /// <param name="rotation">Rotation offset after a shake.</param>
        /// <returns>True if a shaker was updated.</returns>
        internal bool UpdateShakers(out ShakeValues camera, out ShakeValues canvases, out ShakeValues rigidbodies)
        {
            /* Only check if not shaking. Instances can still run when paused.
             * This is intentional behavior so that shakes can be calculated
             * on cameras which are currently disabled, but may become enabled during
             * the middle of the shake. */
            if (!Shaking)
            {
                camera = new ShakeValues();
                canvases = new ShakeValues();
                rigidbodies = new ShakeValues();
                return false;
            }
            //Shaking
            else
            {
                //Shaking.
                if (TryUpdateShakers(out camera, out canvases, out rigidbodies))
                {
                    return true;
                }
                //Shaking ended last frame.
                else
                {
                    Disable();
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks if shakers need to be fixed updated. This is for internal use only.
        /// </summary>
        /// <param name="position">Position offset after a shake.</param>
        /// <param name="rotation">Rotation offset after a shake.</param>
        /// <returns>True if a shaker was updated.</returns>
        internal bool UpdateFixedShakers(out ShakeValues camera, out ShakeValues canvases, out ShakeValues rigidbodies)
        {
            //No fixed values.
            if (FixedCamera == null || _fixedCanvases == null || _fixedRigidbodies == null)
            {
                camera = new ShakeValues();
                canvases = new ShakeValues();
                rigidbodies = new ShakeValues();
                return false;
            }
            //Fixed values.
            else
            {
                //Set out values.
                camera = FixedCamera;
                canvases = _fixedCanvases;
                rigidbodies = _fixedRigidbodies;
                //Dispatch and nullify.
                OnShakeFixedUpdate?.Invoke(this, new ShakeUpdate(FixedCamera, _fixedCanvases, _fixedRigidbodies));
                FixedCamera = null;
                _fixedCanvases = null;
                _fixedRigidbodies = null;

                return true;
            }
        }

        /// <summary>
        /// Updates shakers and returns true if a shaker was updated.
        /// </summary>
        /// <returns></returns>
        private bool TryUpdateShakers(out ShakeValues camera, out ShakeValues canvases, out ShakeValues rigidbodies)
        {
            camera = new ShakeValues();
            canvases = new ShakeValues();
            rigidbodies = new ShakeValues();

            bool instanceProcessed = false;
            for (int i = 0; i < ShakerInstances.Count; i++)
            {
                //Out of bounds. Shouldn't be possible, sanity check.
                if (i >= ShakerInstances.Count)
                    break;

                ShakerInstance instance = ShakerInstances[i];
                //Instance went null. Also shouldn't be possible, sanity check.
                if (instance == null)
                {
                    ShakerInstances.RemoveAt(i);
                    i--;
                    continue;
                }
                //Shaker has ended.
                if (instance.ShakerOver())
                {
                    instanceProcessed = true;
                    ShakerInstances.RemoveAt(i);
                    i--;
                    continue;
                }
                //Shaker paused.
                if (instance.Paused)
                {
                    instanceProcessed = true;
                    continue;
                }

                /* Get new offset from instance and add alterations
                 * to position and rotation. This is done for each instance so that
                 * instances can stack. */
                Vector3 offset = instance.UpdateOffset();
                if (instance.Data.PositionalInfluence != Vector3.zero)
                {
                    //Camera.
                    if (instance.Data.ShakeCameras)
                        camera.Position += offset.Multiply(instance.Data.PositionalInfluence);
                    //Canvases.
                    if (instance.Data.ShakeCanvases)
                        canvases.Position += offset.Multiply(instance.Data.PositionalInfluence);
                    //Rigidbodies.
                    if (instance.Data.ShakeObjects)
                        rigidbodies.Position += offset.Multiply(instance.Data.PositionalInfluence);
                }
                /* Multiply rotational influence by 2.77f so that the rotational
                * amount is accurate to what the user sees in the influence field. */
                if (instance.Data.RotationalInfluence != Vector3.zero)
                {
                    //Camera.
                    if (instance.Data.ShakeCameras)
                        camera.Rotation += offset.Multiply(instance.Data.RotationalInfluence * 2.77f);
                    //Canvases.
                    if (instance.Data.ShakeCanvases)
                        canvases.Rotation += offset.Multiply(instance.Data.RotationalInfluence * 2.77f);
                    //Rigidbodies
                    if (instance.Data.ShakeObjects)
                        rigidbodies.Rotation += offset.Multiply(instance.Data.RotationalInfluence * 2.77f);
                }

                instanceProcessed = true;
            }

            //Limit positional and rotation magnitudes.
            if (_limitMagnitude)
            {
                //Camera.
                if (camera.Position.sqrMagnitude > _sqrPositionalMagnitudeLimit)
                    camera.Position = camera.Position.normalized * _positionalMagnitudeLimit;
                if (camera.Rotation.sqrMagnitude > _sqrRotationalMagnitudeLimit)
                    camera.Rotation = camera.Rotation.normalized * _rotationalMagnitudeLimit;
                //Canvases.
                if (canvases.Position.sqrMagnitude > _sqrPositionalMagnitudeLimit)
                    canvases.Position = canvases.Position.normalized * _positionalMagnitudeLimit;
                if (canvases.Rotation.sqrMagnitude > _sqrRotationalMagnitudeLimit)
                    canvases.Rotation = canvases.Rotation.normalized * _rotationalMagnitudeLimit;
                //Rigidbodies.
                if (rigidbodies.Position.sqrMagnitude > _sqrPositionalMagnitudeLimit)
                    rigidbodies.Position = rigidbodies.Position.normalized * _positionalMagnitudeLimit;
                if (rigidbodies.Rotation.sqrMagnitude > _sqrRotationalMagnitudeLimit)
                    rigidbodies.Rotation = rigidbodies.Rotation.normalized * _rotationalMagnitudeLimit;
            }

            //If anything was changed.
            if (instanceProcessed)
            {
                //Apply scale.
                camera.Position *= Scale;
                camera.Rotation *= Scale;
                canvases.Position *= Scale;
                canvases.Rotation *= Scale;
                rigidbodies.Position *= Scale;
                rigidbodies.Rotation *= Scale;

                /* Set position and rotation to accumulated values.
                 * If no values are set then camera will result to
                 * v3.zero position and rotation. */
                switch (ShakeTechnique)
                {
                    //Matrix.
                    case ShakeTechniques.Matrix:
                        SetMatrixOffsets(camera.Position, camera.Rotation);
                        break;
                    //Local.
                    case ShakeTechniques.LocalSpace:
                        SetLocalSpaceOffsets(camera.Position, camera.Rotation);
                        break;
                }

                FixedCamera = camera;
                _fixedCanvases = canvases;
                _fixedRigidbodies = rigidbodies;
                OnShakeUpdate?.Invoke(this, new ShakeUpdate(camera, canvases, rigidbodies));
            }

            return instanceProcessed;
        }

        /// <summary>
        /// Disables this CameraShaker to save cycles. This is for internal use only.
        /// </summary>
        internal void Disable()
        {
            /* If shaking then update / reset before ending shaking
             * so that updates are sent before shaking ending does. */
            if (Shaking)
            {
                //Reset values and broadcast zero offset.
                if (ShakeTechnique == ShakeTechniques.Matrix)
                    _camera.ResetWorldToCameraMatrix();
                else if (ShakeTechnique == ShakeTechniques.LocalSpace)
                    SetLocalSpaceOffsets(Vector3.zero, Vector3.zero);

                OnShakeUpdate?.Invoke(this, new ShakeUpdate());
                //Send zero on fixed, and nullify fixed values.
                OnShakeFixedUpdate?.Invoke(this, new ShakeUpdate(new ShakeValues(), new ShakeValues(), new ShakeValues()));
                FixedCamera = null;
                _fixedCanvases = null;
                _fixedRigidbodies = null;
            }

            UpdateShaking(false);
        }

        /// <summary>
        /// Adds a ShakerInstance to ShakerInstances.
        /// </summary>
        /// <param name="instance"></param>
        private void AddShakerInstance(ShakerInstance instance)
        {
            ShakerInstances.Add(instance);
            UpdateShaking(true);
        }

        /// <summary>
        /// Adds shaker instances to this cameras ShakerInstances. For internal use only.
        /// </summary>
        /// <param name="instances"></param>
        internal void AddShakerInstances(List<ShakerInstance> instances)
        {
            if (instances.Count == 0)
                return;

            ShakerInstances.AddRange(instances);
            UpdateShaking(true);
        }

        /// <summary>
        /// Updates the shaking value.
        /// </summary>
        /// <param name="shaking"></param>
        private void UpdateShaking(bool shaking)
        {
            bool changed = (shaking != Shaking);
            Shaking = shaking;

            if (changed)
            {
                if (Shaking)
                {
                    OnShakingStarted?.Invoke(this);
                    CameraShakerHandler.AddShaking(this);
                }
                else
                {
                    ShakerInstances.Clear();
                    OnShakingEnded?.Invoke(this);
                    CameraShakerHandler.RemoveShaking(this);
                }
            }
        }

        #region Offset setting.
        /// <summary>
        /// Sets LocalSpace and LocalEulerAngle values for this transform. For internal use only.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        internal void SetLocalSpaceOffsets(Vector3 pos, Vector3 rot)
        {
            transform.localPosition = pos;
            transform.localEulerAngles = rot;
        }
        /// <summary>
        /// Sets Matrix values for this transform. For internal use only.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="rot"></param>
        internal void SetMatrixOffsets(Vector3 pos, Vector3 rot)
        {
            Matrix4x4 m = Matrix4x4.TRS(pos, Quaternion.Euler(rot), new Vector3(1, 1, -1));
            _camera.worldToCameraMatrix = m * _camera.transform.worldToLocalMatrix;
        }
        #endregion

        #region API.
        /// <summary>
        /// Sets Scale value.
        /// </summary>
        /// <param name="value">New scale to use.</param>
        public void SetScale(float value)
        {
            Scale = value;
        }

        /// <summary>
        /// Shakes the camera using data.
        /// </summary>
        /// <param name="data">ShakeData to use.</param>
        /// <returns>Instance generated using data.</returns>
        public ShakerInstance Shake(ShakeData data)
        {
            FirstInitialize();

            if (data.TotalDuration == 0f && data.FadeInDuration == 0f && data.FadeOutDuration == 0f)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("No durations are specified in data; cannot generate a ShakerInstance.");
                return null;
            }
            if (data.PositionalInfluence == Vector3.zero && data.RotationalInfluence == Vector3.zero)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("No influences are specified in data; cannot generate a ShakerInstance.");
                return null;
            }

            /* Make an instance of the data if it's on disk so that values aren't serialized
             * over in the editor when changing values at runtime such as total time, fade out, and more. 
             * Research suggest objects with a positive instance ID are prefabs, or are placed in the scene,
             * while negative values are instantiated. */
            ShakeData dataInstance = data.Instanced ? data : data.CreateInstance();
            dataInstance.Initialize();

            ShakerInstance shakerInstance = new ShakerInstance(dataInstance);
            AddShakerInstance(shakerInstance);

            return shakerInstance;
        }
        /// <summary>
        /// Sets the paused state of all shaker instances on this CameraShaker.
        /// </summary>
        /// <param name="value">New paused state.</param>
        public void SetPaused(bool value)
        {
            foreach (ShakerInstance instance in ShakerInstances)
            {
                if (instance != null)
                    instance.SetPaused(value);
            }
        }

        /// <summary>
        /// Fades out all instances on this CameraShaker. This operation only works on instances not already fading out.
        /// </summary>
        /// <param name="durationOverride">Overrides instance fade out duration with a new value.</param>
        public void FadeOut(float? durationOverride = null)
        {
            foreach (ShakerInstance instance in ShakerInstances)
            {
                if (instance != null)
                    instance.FadeOut(durationOverride);
            }
        }

        /// <summary>
        /// Abruptly stops all instances on this camera shaker.
        /// </summary>
        public void Stop()
        {
            foreach (ShakerInstance instance in ShakerInstances)
            {
                if (instance != null)
                    instance.Stop();
            }
        }

        /// <summary>
        /// Multiplies magnitude values for all instances on this CameraShaker.
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using moveRate unmodified.</param>
        public void MultiplyMagnitude(float multiplier, float moveRate, bool rateUsesDistance)
        {
            foreach (ShakerInstance instance in ShakerInstances)
            {
                if (instance != null)
                    instance.MultiplyMagnitude(multiplier, moveRate, rateUsesDistance);
            }
        }

        /// <summary>
        /// Multiplies roughness values for all instances on this CameraShaker.
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using moveRate unmodified.</param>
        public void MultiplyRoughness(float multiplier, float moveRate, bool rateUsesDistance)
        {
            foreach (ShakerInstance instance in ShakerInstances)
            {
                if (instance != null)
                    instance.MultiplyRoughness(multiplier, moveRate, rateUsesDistance);
            }
        }


        #endregion

        #region Editor checks.
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_limitMagnitude)
            {
                _positionalMagnitudeLimit = Mathf.Max(_positionalMagnitudeLimit, 0.01f);
                _rotationalMagnitudeLimit = Mathf.Max(_rotationalMagnitudeLimit, 0.01f);

                CalculateSqrLimits();
            }
        }
#endif
        #endregion

    }
}