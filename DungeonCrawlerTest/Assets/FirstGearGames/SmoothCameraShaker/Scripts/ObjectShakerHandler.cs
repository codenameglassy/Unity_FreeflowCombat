
using FirstGearGames.Utilities.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker
{

    public class ObjectShakerHandler : MonoBehaviour
    {
        #region Public.
        ///// <summary>
        ///// Dispatched after a Shaker is added to InstantiatedShakers.
        ///// </summary>
        //public static event Action<ObjectShaker> OnShakerInstantiated;
        ///// <summary>
        ///// Dispatched after a Shaker is removed from InstantiatedShakers.
        ///// </summary>
        //public static event Action<ObjectShaker> OnShakerDestroyed;
        /// <summary>
        /// All instantiatedShaker scripts.
        /// </summary>
        public static List<ObjectShaker> InstantiatedShakers = new List<ObjectShaker>();
        #endregion

        #region Private.
        /// <summary>
        /// Collection of Shakers which are currently shaking.
        /// </summary>
        private List<ObjectShaker> _shaking = new List<ObjectShaker>();
        /// <summary>
        /// Singleton instance of this script.
        /// </summary>
        private static ObjectShakerHandler _instance;
        #endregion

        private void Awake()
        {
            //Make sure there is only once instance.
            if (_instance != null && _instance != this)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("Multiple ObjectShakerHandler scripts found. This script auto loads itself and does not need to be placed in your scenes.");
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
            obj.name = "ObjectShakerHandler";
            _instance = obj.AddComponent<ObjectShakerHandler>();
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

            for (int i = 0; i < InstantiatedShakers.Count; i++)
                InstantiatedShakers[i].UpdateShakers();
        }

        /// <summary>
        /// Updates Shakers on fixed time.
        /// </summary>
        private void UpdateFixedShakers()
        {
            for (int i = 0; i < InstantiatedShakers.Count; i++)
                InstantiatedShakers[i].UpdateFixedShakers();
        }

        /// <summary>
        /// Returns if an action can be run on the specified Shaker using an All method.
        /// </summary>
        /// <param name="shaker"></param>
        /// <param name="includeDisabled"></param>
        /// <returns></returns>
        private static bool CanRunAllOn(ObjectShaker shaker, bool includeDisabled)
        {
            if (shaker == null)
                return false;
            if (!shaker.gameObject.activeInHierarchy && !includeDisabled)
                return false;

            return true;
        }


        #region Shaker referencing handling.
        /// <summary>
        /// Adds Shaker to shaking. This is for internal use only.
        /// </summary>
        /// <param name="shaker"></param>
        internal static void AddShaking(ObjectShaker shaker)
        {
            _instance._shaking.AddUnique(shaker);
            _instance.enabled = (_instance._shaking.Count > 0);
        }
        /// <summary>
        /// Removes Shaker from shaking. This is for internal use only.
        /// </summary>
        /// <param name="shaker"></param>
        internal static void RemoveShaking(ObjectShaker shaker)
        {
            _instance._shaking.Remove(shaker);
            _instance.enabled = (_instance._shaking.Count > 0);
        }
        /// <summary>
        /// Adds a Shaker to the InstantiatedShakers field. This is for internal use only.
        /// </summary>
        /// <param name="value"></param>
        internal static void AddInstantiatedShaker(ObjectShaker value)
        {
            InstantiatedShakers.AddUnique(value);
            //OnShakerInstantiated?.Invoke(value);
        }

        /// <summary>
        /// Removes a Shaker from the InstantiatedShakers field. This is for internal use only.
        /// </summary>
        /// <param name="value"></param>
        internal static void RemoveInstantiatedShaker(ObjectShaker value)
        {
            InstantiatedShakers.Remove(value);
            //OnShakerDestroyed?.Invoke(value);
        }
        #endregion

        #region API.
        /// <summary>
        /// Copies ShakerInstances from one CameraShaker to another.
        /// </summary>
        /// <param name="from">CameraShaker copied from.</param>
        /// <param name="to">CameraShaker copied to.</param>
        public static void CopyShakerInstances(ObjectShaker from, ObjectShaker to)
        {
            //If neither shaker is null then add instances.
            if (from != null && to != null)
                to.AddShakerInstances(from.ShakerInstances);
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