using UnityEngine;
using System.Collections.Generic;
using System;
using FirstGearGames.Utilities.Maths;

namespace FirstGearGames.SmoothCameraShaker
{
    public class ObjectShaker : MonoBehaviour
    {

        #region Public.
        /// <summary>
        /// Dispatched when shaking starts after previously being stopped.
        /// </summary>
        public event Action<ObjectShaker> OnShakingStarted;
        /// <summary>
        /// Dispatched when shaking ends.
        /// </summary>
        public event Action<ObjectShaker> OnShakingEnded;
        /// <summary>
        /// Dispatched every update a shake occurs.
        /// </summary>
        public event Action<ObjectShaker, ShakeUpdate> OnShakeUpdate;
        /// <summary>
        /// Dispatched every fixed update a shake occurs. Contains the shake values from last update.
        /// </summary>
        public event Action<ObjectShaker, ShakeUpdate> OnShakeFixedUpdate;
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
        #endregion

        #region Serialized.
        /// <summary>
        /// True to remove this shakers reference from it's handler when disabled. Useful if you have a lot of these shakers in your scene, but objects are not always enabled.
        /// </summary>
        [Tooltip("True to remove this shakers reference from it's handler when disabled. Useful if you have a lot of these shakers in your scene, but objects are not always enabled.")]
        [SerializeField]
        private bool _removeFromManagerOnDisable = true;
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
        /// <summary>
        /// ShakeData to run when enabled. Leave empty to not use this feature.
        /// </summary>
        [Tooltip("ShakeData to run when enabled. Leave empty to not use this feature.")]
        [Space(10)]
        [SerializeField]
        private ShakeData _shakeOnEnable = null;
        #endregion

        #region Private.
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
            if (_removeFromManagerOnDisable)
                ObjectShakerHandler.AddInstantiatedShaker(this);
            if (_shakeOnEnable != null)
                Shake(_shakeOnEnable);
        }

        private void OnDisable()
        {
            Disable();
            if (_removeFromManagerOnDisable)
                ObjectShakerHandler.RemoveInstantiatedShaker(this);
        }

        private void OnDestroy()
        {
            ObjectShakerHandler.RemoveInstantiatedShaker(this);
        }

        /// <summary>
        /// Initializes this script for use. This should only be completed once.
        /// </summary>
        private void FirstInitialize()
        {
            if (_initialized)
                return;

            CalculateSqrLimits();

            //Only add to handler if always active.
            if (!_removeFromManagerOnDisable)
                ObjectShakerHandler.AddInstantiatedShaker(this);
           
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
        internal void UpdateShakers()
        {
            /* Only check if not shaking. Instances can still run when paused.
             * This is intentional behavior so that shakes can be calculated
             * on cameras which are currently disabled, but may become enabled during
             * the middle of the shake. */
            if (!Shaking)
                return;


            /* Try to update shakers.
             * If cannot then disable. */
            if (!TryUpdateShakers())
                Disable();
        }

        /// <summary>
        /// Checks if shakers need to be fixed updated. This is for internal use only.
        /// </summary>
        /// <param name="position">Position offset after a shake.</param>
        /// <param name="rotation">Rotation offset after a shake.</param>
        /// <returns>True if a shaker was updated.</returns>
        internal void UpdateFixedShakers()
        {
            //No fixed values.
            if (_fixedRigidbodies == null || _fixedCanvases == null)
                return;

            //Dispatch and nullify.
            OnShakeFixedUpdate?.Invoke(this, new ShakeUpdate(null, _fixedCanvases, _fixedRigidbodies));
            _fixedCanvases = null;
            _fixedRigidbodies = null;
        }

        /// <summary>
        /// Updates shakers and returns true if a shaker was updated.
        /// </summary>
        /// <returns></returns>
        private bool TryUpdateShakers()
        {
            ShakeValues rigidbodies = new ShakeValues();
            ShakeValues canvases = new ShakeValues();

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
                canvases.Position *= Scale;
                canvases.Rotation *= Scale;
                rigidbodies.Position *= Scale;
                rigidbodies.Rotation *= Scale;

                _fixedCanvases = canvases;
                _fixedRigidbodies = rigidbodies;
                OnShakeUpdate?.Invoke(this, new ShakeUpdate(null, canvases, rigidbodies));
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
                OnShakeUpdate?.Invoke(this, new ShakeUpdate());
                //Send zero on fixed, and nullify fixed values.
                OnShakeFixedUpdate?.Invoke(this, new ShakeUpdate(new ShakeValues(), new ShakeValues(), new ShakeValues()));
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
                    ObjectShakerHandler.AddShaking(this);
                }
                else
                {
                    ShakerInstances.Clear();
                    OnShakingEnded?.Invoke(this);
                    ObjectShakerHandler.RemoveShaking(this);
                }
            }
        }

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