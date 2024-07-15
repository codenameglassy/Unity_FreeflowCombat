using FirstGearGames.Utilities.Maths;
using FirstGearGames.Utilities.Objects;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstGearGames.SmoothCameraShaker
{


    public class ShakableTransform : ShakableBase
    {
        #region Types.
        /// <summary>
        /// Data about how to update rigidbodies with shakes.
        /// </summary>
        private class TransformData
        {
            public TransformData(Transform t)
            {
                Transform = t;
            }

            /// <summary>
            /// Transform on this object.
            /// </summary>
            public readonly Transform Transform;
            /// <summary>
            /// Direction to multiply position by when shaking starts.
            /// </summary>
            public float RandomPositionMultiplier { get; private set; } = 1f;
            /// <summary>
            /// Sets the value for RandomPositionMultiplier.
            /// </summary>
            /// <param name="value"></param>
            public void SetRandomPositionMultiplier(float value)
            {
                RandomPositionMultiplier = value;
            }
            /// <summary>
            /// Direction to multiply rotation by when shaking starts.
            /// </summary>
            public float RandomRotationMultiplier { get; private set; } = 1f;
            /// <summary>
            /// Sets the value for RandomRotationMultiplier.
            /// </summary>
            /// <param name="value"></param>
            public void SetRandomRotationMultiplier(float value)
            {
                RandomRotationMultiplier = value;
            }
            /// <summary>
            /// Positional values last time shake offsets were received.
            /// </summary>
            public Vector3 LastPositional { get; private set; } = Vector3.zero;
            /// <summary>
            /// Sets the value for LastPositional.
            /// </summary>
            /// <param name="value"></param>
            public void SetLastPositional(Vector3 value)
            {
                LastPositional = value;
            }
            /// <summary>
            /// Rotational values last time shake offsets were received.
            /// </summary>
            public Vector3 LastRotational { get; private set; } = Vector3.zero;
            /// <summary>
            /// Sets the value for LastRotational.
            /// </summary>
            /// <param name="value"></param>
            public void SetLastRotational(Vector3 value)
            {
                LastRotational = value;
            }
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// Multiplier to apply towards position.
        /// </summary>
        [Tooltip("Multiplier to apply towards position.")]
        [Space(10f)]
        [SerializeField]

        private float _positionalMultiplier = 1f;
        /// <summary>
        /// Multipplier to apply towards rotation.
        /// </summary>
        [Tooltip("Multipplier to apply towards rotation.")]
        [SerializeField]
        [FormerlySerializedAs("_rotationMultiplier")]
        private float _rotationalMultiplier = 1f;
        /// <summary>
        /// Only shake when in view of a camera.
        /// </summary>
        [Tooltip("Only shake when in view of a camera.")]
        [Space(10f)]
        [SerializeField]
        private bool _requireInView = true;
        /// <summary>
        /// True to find transforms in children too. This allows you to use one ShakableTransform on the parent if all children transforms should shake as well.
        /// </summary>
        [Tooltip("True to find transforms in children too. This allows you to use one ShakableTransform on the parent if all children transforms should shake as well.")]
        [SerializeField]
        private bool _includeChildren = false;
        /// <summary>
        /// True to ignore the transform this component resides, and only shake children.
        /// </summary>
        [Tooltip("True to ignore the transform this component resides, and only shake children.")]
        [SerializeField]
        private bool _ignoreSelf = false;
        /// <summary>
        /// True to also find inactive children.
        /// </summary>
        [Tooltip("True to also find inactive children.")]
        [SerializeField]
        private bool _includeInactive = false;
        /// <summary>
        /// True to convert forces to influence space before applying.
        /// </summary>
        [Tooltip("True to convert influence to local space before applying.")]
        [Space(10f)]
        [SerializeField]
        private bool _localizeShake = false;
        /// <summary>
        /// True to randomly change influence direction. Best used with bulk objects so they do not all shake the same direction.
        /// </summary>
        [Tooltip("True to randomly change influence direction. Best used with bulk objects so they do not all shake the same direction.")]
        [SerializeField]
        private bool _randomizeDirections = true;
        #endregion

        #region Private.        
        /// <summary>
        /// Data about each rigidbody to shake.
        /// </summary>
        private TransformData[] _tData;
        /// <summary>
        /// True if currently in view.
        /// </summary>
        private bool _inView = false;
        /// <summary>
        /// ObjectShaker used for this object. May be null if not using ObjectShaker type.
        /// </summary>
        private ObjectShaker _objectShaker = null;
        #endregion

        protected virtual void Awake()
        {
            FirstInitialize();
        }

        protected virtual void OnEnable()
        {
            if (_requireInView && _inView)
                ChangeSubscription(true);
            else if (!_requireInView)
                ChangeSubscription(true);
        }

        protected virtual void OnDisable()
        {
            if (_requireInView && _inView)
                ChangeSubscription(false);
            else if (!_requireInView)
                ChangeSubscription(false);
        }

        /// <summary>
        /// Initializes this script for use. Should only be completed once.
        /// </summary>
        private void FirstInitialize()
        {
            //If using ObjectShaker type.
            if (base.ShakerType == ShakerTypes.ObjectShaker)
            {
                _objectShaker = GetComponentInParent<ObjectShaker>();
                if (_objectShaker == null)
                {
                    Debug.LogError("ObjectShaker could not be found on or above object " + gameObject.name + ". Shakable will be destroyed.", this);
                    DestroyImmediate(this);
                    return;
                }
            }

            //If not including children.
            if (!_includeChildren)
            {
                Transform t = transform;
                if (t == null)
                {
                    Debug.LogWarning("Transform is empty on " + gameObject.name + ". Shakable will be destroyed.", this);
                    DestroyImmediate(this);
                    return;
                }

                _tData = new TransformData[1];
                _tData[0] = new TransformData(t);
            }
            //Include children.
            else
            {
                List<Transform> ts = new List<Transform>();
                Transforms.GetComponentsInChildren(transform, ts, !_ignoreSelf,_includeInactive);
                if (ts.Count == 0)
                {
                    Debug.LogWarning("No transforms exist on parent or children of " + gameObject.name + ". Shakable will be destroyed.", this);
                    DestroyImmediate(this);
                    return;
                }

                _tData = new TransformData[ts.Count];
                for (int i = 0; i < ts.Count; i++)
                    _tData[i] = new TransformData(ts[i]);
            }

            /* Try to find a renderer. One is required on this object for OnBecameVisible and OnBecameInvisible
             * to call. If a renderer doesn't exist then add a low cost renderer. */
            if (_requireInView)
            {
                if (GetComponent<Renderer>() == null)
                {
                    if (Debug.isDebugBuild)
                        Debug.Log("Renderer not found on object. Adding renderer so RequireInView works properly. Added renderer may be smaller than actual object, and sometimes may not be detected as in view. To resolve add your own renderer to the object this script resides.", this);
                    SpriteRenderer r = gameObject.AddComponent<SpriteRenderer>();
                    r.sprite = null;
                }
            }
        }

        #region OnShakeUpdate.
        /// <summary>
        /// Received when shaking starts when previously stopped on all Shakers.
        /// </summary>
        private void CameraShakerHandler_OnAllShakingStarted()
        {
            RandomizeDirections();
        }

        /// <summary>
        /// Received when shaking starts when previously stopped on all Shakers.
        /// </summary>
        /// <param name="obj"></param>
        private void ObjectShaker_OnShakingStarted(ObjectShaker obj)
        {
            RandomizeDirections();
        }

        /// <summary>
        /// Received every fixed update a shake occurs. Contains the shake values from last update.
        /// </summary>
        private void CameraShakerHandler_OnShakeUpdate(ShakeUpdate obj)
        {
            ShakeUpdateOccurred(obj);
        }
        /// <summary>
        /// Received every fixed update a shake occurs. Contains the shake values from last update.
        /// </summary>
        private void ObjectShaker_OnShakeUpdate(ObjectShaker arg1, ShakeUpdate arg2)
        {
            ShakeUpdateOccurred(arg2);
        }
        /// <summary>
        /// Called when a shake update occurs, wether it be from CameraShaker or ObjectShaker.
        /// </summary>
        /// <param name="obj"></param>
        private void ShakeUpdateOccurred(ShakeUpdate obj)
        {
            if (!_inView && _requireInView)
                return;

            for (int i = 0; i < _tData.Length; i++)
            {
#if UNITY_EDITOR
                //Transform can go null when exiting playmode, and shaker may try to send one last update when exiting play mode.
                if (_tData[i].Transform == null)
                    return;
#endif
                //Calculate new offsets.
                Vector3 newPos = obj.Objects.Position * _tData[i].RandomPositionMultiplier * _positionalMultiplier;
                Vector3 newRot = obj.Objects.Rotation * _tData[i].RandomRotationMultiplier * _rotationalMultiplier;
                //If to localize force.
                if (_localizeShake)
                {
                    newPos = _tData[i].Transform.transform.TransformDirection(newPos);
                    newRot = _tData[i].Transform.transform.TransformDirection(newRot);
                }

                //Apply changes.
                _tData[i].Transform.localPosition += (newPos - _tData[i].LastPositional);
                _tData[i].Transform.localEulerAngles += (newRot - _tData[i].LastRotational);
                //Set last values.
                _tData[i].SetLastPositional(newPos);
                _tData[i].SetLastRotational(newRot);
            }
        }
        #endregion

        /// <summary>
        /// Updates random multipliers for all transform datas.
        /// </summary>
        private void RandomizeDirections()
        {
            if (!_randomizeDirections)
                return;

            for (int i = 0; i < _tData.Length; i++)
                RandomizeDirections(_tData[i]);
        }
        /// <summary>
        /// Updates random multipliers for data.
        /// </summary>
        private void RandomizeDirections(TransformData data)
        {
            if (!_randomizeDirections)
                return;

            data.SetRandomPositionMultiplier(Floats.RandomlyFlip(data.RandomPositionMultiplier));
            data.SetRandomRotationMultiplier(Floats.RandomlyFlip(data.RandomRotationMultiplier));
        }

        /// <summary>
        /// Received when visible in any camera.
        /// </summary>
        protected virtual void OnBecameVisible()
        {
            _inView = true;
            if (_requireInView)
                ChangeSubscription(true);
        }
        /// <summary>
        /// Received when no longer visible in any camera.
        /// </summary>
        protected virtual void OnBecameInvisible()
        {
            _inView = false;
            if (_requireInView)
                ChangeSubscription(false);
        }

        /// <summary>
        /// Changes the subscription to the camera shaker.
        /// </summary>
        /// <param name="subscribe"></param>
        private void ChangeSubscription(bool subscribe)
        {
            /* Must subscribe to starting so that random directions
             * can be reset before a shake begins. */

            //CameraShaker type.
            if (base.ShakerType == ShakerTypes.CameraShaker)
            {
                if (subscribe)
                {
                    CameraShakerHandler.OnAllShakeUpdate += CameraShakerHandler_OnShakeUpdate;
                    CameraShakerHandler.OnAllShakingStarted += CameraShakerHandler_OnAllShakingStarted;
                }
                else
                {
                    CameraShakerHandler.OnAllShakeUpdate -= CameraShakerHandler_OnShakeUpdate;
                    CameraShakerHandler.OnAllShakingStarted -= CameraShakerHandler_OnAllShakingStarted;
                }
            }
            //ObjectShaker type.
            else if (base.ShakerType == ShakerTypes.ObjectShaker)
            {
                if (_objectShaker != null)
                {
                    if (subscribe)
                    {
                        _objectShaker.OnShakeUpdate += ObjectShaker_OnShakeUpdate;
                        _objectShaker.OnShakingStarted += ObjectShaker_OnShakingStarted;
                        /* If already shaking then randomize directions.
                         * This can occur when ObjectShaker has a ShakeOnEnable
                         * shake and is active before this shakable. */
                        if (_objectShaker.Shaking)
                            RandomizeDirections();
                    }
                    else
                    {
                        _objectShaker.OnShakeUpdate -= ObjectShaker_OnShakeUpdate;
                        _objectShaker.OnShakingStarted -= ObjectShaker_OnShakingStarted;
                    }
                }
            }
        }

    }
}