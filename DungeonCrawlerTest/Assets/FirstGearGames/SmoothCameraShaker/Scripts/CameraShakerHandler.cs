
using FirstGearGames.Utilities.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker
{

    public class CameraShakerHandler : MonoBehaviour
    {
        #region Public.
        /// <summary>
        /// Dispatched after the default Shaker is changed.
        /// </summary>
        public static event Action<CameraShakerChange> OnDefaultShakerChanged;
        /// <summary>
        /// Dispatched after the default Shaker is changed. Obsolete: use OnDefaultShakerChanged instead.
        /// </summary>
        [Obsolete("Obsolete: use OnDefaultShakerChanged instead.")]
        public static event Action<CameraShakerChange> OnDefaultCameraShakerChanged
        {
            add { OnDefaultShakerChanged += value; }
            remove { OnDefaultShakerChanged -= value; }
        }
        /// <summary>
        /// Dispatched when shaking starts when previously stopped on all Shakers.
        /// </summary>
        public static event Action OnAllShakingStarted;
        /// <summary>
        /// Dispatched when shaking ends on all Shakers.
        /// </summary>
        public static event Action OnAllShakingEnded;
        /// <summary>
        /// Dispatched every update a shake occurs. This is the total values of InstantiatedShakers.
        /// </summary>
        public static event Action<ShakeUpdate> OnAllShakeUpdate;
        /// <summary>
        /// Dispatched every fixed update a shake occurs. Contains the shake values from last update of InstantiatedShakers.
        /// </summary>
        public static event Action<ShakeUpdate> OnAllShakeFixedUpdate;
        /// <summary>
        /// Dispatched when shaking starts on any Shaker.
        /// </summary>
        public static event Action<CameraShaker> OnShakingStarted;
        /// <summary>
        /// Dispatched when shaking ends on any Shaker.
        /// </summary>
        public static event Action<CameraShaker> OnShakingEnded;
        /// <summary>
        /// Dispatched every update a shake occurs on any Shaker.
        /// </summary>
        public static event Action<CameraShaker, ShakeUpdate> OnShakeUpdate;
        /// <summary>
        /// Dispatched every fixed updated a shake occurs on any Shaker. Contains the shake values from last update.
        /// </summary>
        public static event Action<CameraShaker, ShakeUpdate> OnShakeFixedUpdate;
        /// <summary>
        /// Dispatched after a Shaker is added to InstantiatedShakers.
        /// </summary>
        public static event Action<CameraShaker> OnShakerInstantiated;
        /// <summary>
        /// Dispatched after a Shaker is added to InstantiatedShakers. Obsolete: use OnShakerInstantiated instead.
        /// </summary>
        [Obsolete("Obsolete: use OnShakerInstantiated instead.")]
        public static event Action<CameraShaker> OnCameraShakerInstantiated
        {
            add { OnShakerInstantiated += value; }
            remove { OnShakerInstantiated -= value; }
        }
        /// <summary>
        /// Dispatched after a Shaker is removed from InstantiatedShakers.
        /// </summary>
        public static event Action<CameraShaker> OnShakerDestroyed;
        /// <summary>
        /// Dispatched after a Shaker is removed from InstantiatedShakers. Obsolete: use OnShakerDestroyed instead.
        /// </summary>
        [Obsolete("Obsolete: use OnShakerDestroyed instead.")]
        public static event Action<CameraShaker> OnCameraShakerDestroyed
        {
            add { OnShakerDestroyed += value; }
            remove { OnShakerDestroyed -= value; }
        }
        /// <summary>
        /// All instantiated Shaker scripts.
        /// </summary>
        public static List<CameraShaker> InstantiatedShakers = new List<CameraShaker>();
        /// <summary>
        /// All instantiated Shaker scripts. Obsolete: use InstantiatedShakers instead.
        /// </summary>
        [Obsolete("Obsolete: use InstantiatedShakers instead.")]
        public static List<CameraShaker> InstantiatedCameraShakers
        {
            get { return InstantiatedShakers; }
            set { InstantiatedShakers = value; }
        }
        /// <summary>
        /// 
        /// </summary>z
        private CameraShaker _defaultCameraShaker;
        /// <summary>
        /// Current default Shaker.
        /// </summary>
        public static CameraShaker DefaultCameraShaker
        {
            get
            {
                if (_instance == null)
                    return null;

                return _instance._defaultCameraShaker;
            }
            private set
            {
                if (_instance == null)
                    return;

                _instance._defaultCameraShaker = value;
            }
        }
        /// <summary>
        /// True if any CameraShaker is currently shaking.
        /// </summary>
        public static bool Shaking { get { return (_instance._shaking.Count > 0); } }
        #endregion

        #region Private.
        /// <summary>
        /// Collection of CameraShakers which are currently shaking.
        /// </summary>
        private List<CameraShaker> _shaking = new List<CameraShaker>();
        /// <summary>
        /// Singleton instance of this script.
        /// </summary>
        private static CameraShakerHandler _instance;
        #endregion

        private void Awake()
        {
            //Make sure there is only once instance.
            if (_instance != null && _instance != this)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("Multiple CameraShakerHandler scripts found. This script auto loads itself and does not need to be placed in your scenes.");
                Destroy(this);
                return;
            }
        }

        private void Update()
        {
            UpdateShakers();
        }

        private void FixedUpdate()
        {
            UpdateFixedShakers();
        }

        private void OnDestroy()
        {
            DisableAll();
        }

        /// <summary>
        /// Initializes this script for use. Should only be completed once.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void FirstInitialize()
        {
            DDOL ddol = DDOL.ReturnDDOL();

            GameObject obj = new GameObject();
            obj.name = "CameraShakerHandler";
            _instance = obj.AddComponent<CameraShakerHandler>();
            _instance.enabled = false;

            _instance.transform.SetParent(ddol.transform);
        }

        /// <summary>
        /// Disables activity on all camera shakers.
        /// </summary>
        private void DisableAll()
        {
            //Disable camera shakers.
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (InstantiatedShakers[i] != null)
                    InstantiatedShakers[i].Disable();
            }
        }

        /// <summary>
        /// Updates Shakers on standard time.
        /// </summary>
        private void UpdateShakers()
        {
            if (_shaking.Count == 0)
                return;

            ShakeValues totalCamera = new ShakeValues();
            ShakeValues totalCanvases = new ShakeValues();
            ShakeValues totalRigidbodies = new ShakeValues();

            //True if any shakers are running.
            bool anyShaking = false;
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                ShakeValues camera;
                ShakeValues canvases;
                ShakeValues rigidbodies;
                if (InstantiatedShakers[i].UpdateShakers(out camera, out canvases, out rigidbodies))
                {
                    anyShaking = true;

                    totalCamera.Position += camera.Position;
                    totalCamera.Rotation += camera.Rotation;
                    totalCanvases.Position += canvases.Position;
                    totalCanvases.Rotation += canvases.Rotation;
                    totalRigidbodies.Position += rigidbodies.Position;
                    totalRigidbodies.Rotation += rigidbodies.Rotation;
                }
            }

            //If any where shaking.
            if (anyShaking)
                OnAllShakeUpdate?.Invoke(new ShakeUpdate(totalCamera, totalCanvases, totalRigidbodies));
        }

        /// <summary>
        /// Updates Shakers on fixed time.
        /// </summary>
        private void UpdateFixedShakers()
        {
            /* Don't exit if total shaking is 0
             * as shaking may have stopped but still
             * have a fixed shake queued. */
            ShakeValues totalCamera = new ShakeValues();
            ShakeValues totalCanvases = new ShakeValues();
            ShakeValues totalRigidbodies = new ShakeValues();

            //True if any shakers are shaking a fixed value.
            bool anyShaking = false;
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                ShakeValues camera;
                ShakeValues canvases;
                ShakeValues rigidbodies;
                if (InstantiatedShakers[i].UpdateFixedShakers(out camera, out canvases, out rigidbodies))
                {
                    anyShaking = true;

                    totalCamera.Position += camera.Position;
                    totalCamera.Rotation += camera.Rotation;
                    totalCanvases.Position += canvases.Position;
                    totalCanvases.Rotation += canvases.Rotation;
                    totalRigidbodies.Position += rigidbodies.Position;
                    totalRigidbodies.Rotation += rigidbodies.Rotation;
                }
            }

            if (anyShaking)
                OnAllShakeFixedUpdate?.Invoke(new ShakeUpdate(totalCamera, totalCanvases, totalRigidbodies));
        }

        /// <summary>
        /// Returns if an action can be run on the specified Shaker using an All method.
        /// </summary>
        /// <param name="shaker"></param>
        /// <param name="includeDisabled"></param>
        /// <returns></returns>
        private static bool CanRunAllOn(CameraShaker shaker, bool includeDisabled)
        {
            if (shaker == null)
                return false;
            if (!shaker.gameObject.activeInHierarchy && !includeDisabled)
                return false;

            return true;
        }


        #region Shaker referencing handling.
        /// <summary>
        /// Adds CameraShaker to shaking. This is for internal use only.
        /// </summary>
        /// <param name="shaker"></param>
        internal static void AddShaking(CameraShaker shaker)
        {
            int startCount = _instance._shaking.Count;
            _instance._shaking.AddUnique(shaker);

            //Shaking just started.
            if (startCount == 0 && _instance._shaking.Count > 0)
            {
                _instance.enabled = true;
                OnAllShakingStarted?.Invoke();
            }
        }
        /// <summary>
        /// Removes CameraShaker from shaking. This is for internal use only.
        /// </summary>
        /// <param name="shaker"></param>
        internal static void RemoveShaking(CameraShaker shaker)
        {
            int startCount = _instance._shaking.Count;
            _instance._shaking.Remove(shaker);

            //Last shaker was removed.
            if (startCount > 0 && _instance._shaking.Count == 0)
            {
                /* Since this is the last shaker being removed send
                 * zero values. This has to be done before update otherwise these
                 * values would send after the OnAllShakingEnded. */
                OnAllShakeUpdate?.Invoke(new ShakeUpdate());
                OnAllShakeFixedUpdate?.Invoke(new ShakeUpdate());

                OnAllShakingEnded?.Invoke();
                _instance.enabled = false;

            }
        }
        /// <summary>
        /// Adds a CameraShaker to the InstantiatedCameraShakers field. This is for internal use only.
        /// </summary>
        /// <param name="value"></param>
        internal static void AddInstantiatedShaker(CameraShaker value)
        {
            int index = InstantiatedShakers.IndexOf(value);
            /* If shaker already exist then remove the current entry.
             * It will then be added to the end. */
            if (index != -1)
                InstantiatedShakers.RemoveAt(index);
            //First time being added, subscribe to events.
            else
                ChangeShakerSubscriptions(value, true);

            InstantiatedShakers.Add(value);
            OnShakerInstantiated?.Invoke(value);
        }

        /// <summary>
        /// Removes a CameraShaker from the InstantiatedCameraShakers field. This is for internal use only.
        /// </summary>
        /// <param name="value"></param>
        internal static void RemoveInstantiatedShaker(CameraShaker value)
        {
            ChangeShakerSubscriptions(value, false);

            InstantiatedShakers.Remove(value);
            /* If value was the current default shaker then try to change value
             * to the next most recently enabled shaker. This isn't ideal
             * but can occur with user error. */
            if (value == DefaultCameraShaker && InstantiatedShakers.Count > 0)
                SetFirstDefault();

            OnShakerDestroyed?.Invoke(value);
        }

        /// <summary>
        /// Iterates through CameraShakers and sets the most recently active instance as default.
        /// </summary>
        private static void SetFirstDefault()
        {
            for (int i = (InstantiatedShakers.Count - 1); i >= 0; i--)
            {
                if (InstantiatedShakers[i] != null && InstantiatedShakers[i].gameObject.activeInHierarchy)
                {
                    SetDefaultCameraShaker(InstantiatedShakers[i]);
                    return;
                }
            }

            //Fall through. Ideally won't happen but can depending on user setup.
            SetDefaultCameraShaker(null);
        }
        #endregion

        #region Relaying CameraShaker events.
        /// <summary>
        /// Changes subscriptions to a camera shaker.
        /// </summary>
        /// <param name="shaker"></param>
        /// <param name="subscribe"></param>
        private static void ChangeShakerSubscriptions(CameraShaker shaker, bool subscribe)
        {
            if (shaker == null)
                return;

            if (subscribe)
            {
                shaker.OnShakingStarted += Shaker_OnShakingStarted;
                shaker.OnShakingEnded += Shaker_OnShakingEnded;
                shaker.OnShakeUpdate += Shaker_OnShakeUpdate;
                shaker.OnShakeFixedUpdate += Shaker_OnFixedShakeUpdate;
            }
            else
            {
                shaker.OnShakingStarted -= Shaker_OnShakingStarted;
                shaker.OnShakingEnded -= Shaker_OnShakingEnded;
                shaker.OnShakeUpdate -= Shaker_OnShakeUpdate;
                shaker.OnShakeFixedUpdate -= Shaker_OnFixedShakeUpdate;
            }
        }

        /// <summary>
        /// Received when any instantiated CameraShaker stops shaking.
        /// </summary>
        /// <param name="obj"></param>
        private static void Shaker_OnShakingEnded(CameraShaker obj)
        {
            OnShakingEnded?.Invoke(obj);
        }

        /// <summary>
        /// Received when any instantiated CameraShaker starts shaking.
        /// </summary>
        /// <param name="obj"></param>
        private static void Shaker_OnShakingStarted(CameraShaker obj)
        {
            OnShakingStarted?.Invoke(obj);
        }

        /// <summary>
        /// Received when any instantiated CameraShaker calls OnShakeUpdate.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private static void Shaker_OnShakeUpdate(CameraShaker arg1, ShakeUpdate arg2)
        {
            OnShakeUpdate?.Invoke(arg1, arg2);
        }
        /// <summary>
        /// Received when any instantiated CameraShaker calls OnFixedShakeUpdate.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private static void Shaker_OnFixedShakeUpdate(CameraShaker arg1, ShakeUpdate arg2)
        {
            OnShakeFixedUpdate?.Invoke(arg1, arg2);
        }
        #endregion


        #region API.
        /// <summary>
        /// Copies ShakerInstances from one CameraShaker to another.
        /// </summary>
        /// <param name="from">CameraShaker copied from.</param>
        /// <param name="to">CameraShaker copied to.</param>
        /// <param name="copyOffset">True to copy the from cameras current offsets. Both CameraShakers must have the same ShakeTechnique for this to work.</param>
        public static void CopyShakerInstances(CameraShaker from, CameraShaker to, bool copyOffset = true)
        {
            //If neither shaker is null then add instances.
            if (from != null && to != null)
            {
                to.AddShakerInstances(from.ShakerInstances);

                //Also copy offsets when possible.
                if (copyOffset && from.ShakeTechnique == to.ShakeTechnique)
                {
                    /* Use the to camera shake technique. Since they are the same
                    * it really doesn't matter which one I read. */
                    CameraShaker.ShakeTechniques technique = to.ShakeTechnique;

                    //Matrix.
                    if (technique == CameraShaker.ShakeTechniques.Matrix)
                    {
                        /* Cannot copy the matrix because camera view will remain as last cameras view.
                         * If fixed values are known for from camera then use those. */
                        if (from.FixedCamera != null)
                            to.SetMatrixOffsets(from.FixedCamera.Position, from.FixedCamera.Rotation);
                    }
                    //LocalSpace.
                    else if (to.ShakeTechnique == CameraShaker.ShakeTechniques.LocalSpace)
                    {
                        to.SetLocalSpaceOffsets(from.transform.localPosition, from.transform.localEulerAngles);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the DefaultCamereaShaker field.
        /// </summary>
        /// <param name="value">New CameraShaker to use as default.</param>
        public static void SetDefaultCameraShaker(CameraShaker value)
        {
            CameraShaker old = DefaultCameraShaker;
            DefaultCameraShaker = value;
            OnDefaultShakerChanged?.Invoke(new CameraShakerChange(old, value));
        }

        /// <summary>
        /// Sets Scale value on the default CameraShaker.
        /// </summary>
        /// <param name="value">New scale to use.</param>
        public static void SetScale(float value)
        {
            if (DefaultCameraShaker == null)
                return;

            DefaultCameraShaker.SetScale(value);
        }
        /// <summary>
        /// Sets the Scale value of InstantiatedCameraShakers.
        /// </summary>
        /// <param name="value">New scale to use</param>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        public static void SetScaleAll(float value, bool includeDisabled = false)
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                InstantiatedShakers[i].SetScale(value);
            }
        }

        /// <summary>
        /// Shakes the default CameraShaker using data.
        /// </summary>
        /// <param name="data">ShakeData to use.</param>
        /// <returns>Instance generated using data.</returns>
        public static ShakerInstance Shake(ShakeData data)
        {
            if (DefaultCameraShaker == null)
                return null;

            return DefaultCameraShaker.Shake(data);
        }
        /// <summary>
        /// Shakes the all camera shakers using data.
        /// </summary>
        /// <param name="data">ShakeData to use.</param>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        /// <returns>Instances generated using data.</returns>
        public static List<ShakerInstance> ShakeAll(ShakeData data, bool includeDisabled = false)
        {
            List<ShakerInstance> results = new List<ShakerInstance>();

            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                results.Add(InstantiatedShakers[i].Shake(data));
            }

            return results;
        }


        /// <summary>
        /// Sets the paused state of all shaker instances on the default CameraShaker.
        /// </summary>
        /// <param name="value">New pause state.</param>
        public static void SetPaused(bool value)
        {
            if (DefaultCameraShaker == null)
                return;

            DefaultCameraShaker.SetPaused(value);
        }
        /// <summary>
        /// Sets the paused state of all shaker instances on the all CameraShakers.
        /// </summary>
        /// <param name="value">New pause state.</param>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        public static void SetPausedAll(bool value, bool includeDisabled = false)
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                InstantiatedShakers[i].SetPaused(value);
            }
        }

        /// <summary>
        /// Abruptly stops all instances on the default CameraShaker.
        /// </summary>
        public static void Stop()
        {
            if (DefaultCameraShaker == null)
                return;

            DefaultCameraShaker.Stop();
        }
        /// <summary>
        /// Abruptly stops all instances on InstantiatedCameraShakers.
        /// </summary>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        public static void StopAll(bool includeDisabled = false)
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                InstantiatedShakers[i].Stop();
            }
        }

        /// <summary>
        /// Fades out all instances on the default CameraShaker. This operation only works on instances not already fading out.
        /// </summary>
        /// <param name="durationOverride">Overrides instance fade out duration with a new value.</param>
        public static void FadeOut(float? durationOverride = null)
        {
            if (DefaultCameraShaker == null)
                return;

            DefaultCameraShaker.FadeOut(durationOverride);
        }
        /// <summary>
        /// Fades out all instances on all CameraShakers. This operation only works on instances not already fading out.
        /// </summary>
        /// <param name="durationOverride">Overrides instance fade out duration with a new value.</param>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        public static void FadeOutAll(float? durationOverride = null, bool includeDisabled = false)
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                InstantiatedShakers[i].FadeOut(durationOverride);
            }
        }


        /// <summary>
        /// Multiplies magnitude values for all instances on the defaut camera shaker.
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using movdRate unmodified.</param>
        public void MultiplyMagnitude(float multiplier, float moveRate, bool rateUsesDistance)
        {
            if (DefaultCameraShaker == null)
                return;

            DefaultCameraShaker.MultiplyMagnitude(multiplier, moveRate, rateUsesDistance);
        }
        /// <summary>
        /// Multiplies magnitude values for all instances on all camera shakers.
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using movdRate unmodified.</param>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        public void MultiplyMagnitudeAll(float multiplier, float moveRate, bool rateUsesDistance, bool includeDisabled = false)
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                InstantiatedShakers[i].MultiplyMagnitude(multiplier, moveRate, rateUsesDistance);
            }
        }

        /// <summary>
        /// Multiplies roughness values for all instances on the default camera shaker.
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using movdRate unmodified.</param>
        public void MultiplyRoughness(float multiplier, float moveRate, bool rateUsesDistance)
        {
            if (DefaultCameraShaker == null)
                return;

            DefaultCameraShaker.MultiplyRoughness(multiplier, moveRate, rateUsesDistance);
        }
        /// <summary>
        /// Multiplies roughness values for all instances on all camera shakers.
        /// </summary>
        /// <param name="multiplier">Value to multiply by. 1f is standard multiplication, which in result would be default values.</param>
        /// <param name="moveRate">How quickly per second to move towards new multiplier. Values 0f and lower are instant.</param>
        /// <param name="rateUsesDistance">True to modify move rate based on distance from multiplier. False to move towards goal using movdRate unmodified.</param>
        /// <param name="includeDisabled">True to issue call on disabled CameraShakers as well.</param>
        public void MultiplyRoughnessAll(float multiplier, float moveRate, bool rateUsesDistance, bool includeDisabled = false)
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
            {
                if (!CanRunAllOn(InstantiatedShakers[i], includeDisabled))
                    continue;

                InstantiatedShakers[i].MultiplyRoughness(multiplier, moveRate, rateUsesDistance);
            }
        }
        #endregion
    }


}