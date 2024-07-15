using FirstGearGames.Utilities.Maths;
using FirstGearGames.Utilities.Objects;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstGearGames.SmoothCameraShaker
{


    public class ShakableRigidbody : ShakableBase
    {
        #region Types.
        /// <summary>
        /// Data about how to update rigidbodies with shakes.
        /// </summary>
        private class RigidbodyData
        {
            public RigidbodyData(Rigidbody rb)
            {
                Rigidbody = rb;
            }

            /// <summary>
            /// Rigidbody on this object.
            /// </summary>
            public readonly Rigidbody Rigidbody;
            /// <summary>
            /// Direction to multiply position by at random intervals.
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
            /// Direction to multiply rotation by at random intervals.
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
            /// Next time to update randomMultipliers.
            /// </summary>
            public float NextRandomizeTime { get; private set; } = -1f;
            /// <summary>
            /// Sets the value for NextRandomizeTime.
            /// </summary>
            /// <param name="value"></param>
            public void SetNextRandomizeTime(float value)
            {
                NextRandomizeTime = value;
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
        [FormerlySerializedAs("_positionMultiplier")]
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
        /// True to find rigidbodies in children too. This allows you to use one ShakableRigidbody on the parent if all children rigidbodies should shake as well.
        /// </summary>
        [Tooltip("True to find rigidbodies in children too. This allows you to use one ShakableRigidbody on the parent if all children rigidbodies should shake as well.")]
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
        /// True to convert forces to local space before applying.
        /// </summary>
        [Tooltip("True to convert forces to local space before applying.")]
        [Space(10f)]
        [SerializeField]
        private bool _localizeShake = false;
        /// <summary>
        /// True to randomly change force direction. Best used with bulk objects so they do not all shake the same direction.
        /// </summary>
        [Tooltip("True to randomly change force direction. Best used with bulk objects so they do not all shake the same direction.")]
        [SerializeField]
        private bool _randomizeDirections = true;
        #endregion

        #region Private.        
        /// <summary>
        /// Data about each rigidbody to shake.
        /// </summary>
        private RigidbodyData[] _rbData;
        /// <summary>
        /// True if currently in view.
        /// </summary>
        private bool _inView = false;
        /// <summary>
        /// ObjectShaker used for this object. May be null if not using ObjectShaker type.
        /// </summary>
        private ObjectShaker _objectShaker = null;
        #endregion

        private void Awake()
        {
            FirstInitialize();
        }

        private void OnEnable()
        {
            if (_requireInView && _inView)
                ChangeSubscription(true);
            else if (!_requireInView)
                ChangeSubscription(true);
        }

        private void OnDisable()
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
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogWarning("Rigidbody is empty on " + gameObject.name + ". Shakable will be destroyed.", this);
                    DestroyImmediate(this);
                    return;
                }

                _rbData = new RigidbodyData[1];
                _rbData[0] = new RigidbodyData(rb);
            }
            //Include children.
            else
            {
                List<Rigidbody> rbs = new List<Rigidbody>();
                Transforms.GetComponentsInChildren(transform, rbs, !_ignoreSelf, _includeInactive);
                if (rbs.Count == 0)
                {
                    Debug.LogWarning("No rigidbodies exist on parent or children of " + gameObject.name + ". Shakable will be destroyed.", this);
                    DestroyImmediate(this);
                    return;
                }

                _rbData = new RigidbodyData[rbs.Count];
                for (int i = 0; i < rbs.Count; i++)
                    _rbData[i] = new RigidbodyData(rbs[i]);
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
        /// Received every fixed update a shake occurs. Contains the shake values from last update.
        /// </summary>
        private void CameraShakerHandler_OnShakeFixedUpdate(ShakeUpdate obj)
        {
            ShakeUpdateOccurred(obj);
        }
        /// <summary>
        /// Received every fixed update a shake occurs. Contains the shake values from last update.
        /// </summary>
        private void ObjectShaker_OnShakeFixedUpdate(ObjectShaker arg1, ShakeUpdate arg2)
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

            for (int i = 0; i < _rbData.Length; i++)
            {
#if UNITY_EDITOR
                //Rigidbody can go null when exiting playmode, and camerashaker may try to send one last update when exiting play mode.
                if (_rbData[i].Rigidbody == null)
                    return;
#endif
                //Check if random multipliers should be updated.
                CheckRandomizeRandomers(_rbData[i]);

                //Calculate new offsets.
                Vector3 newPos = obj.Objects.Position * _rbData[i].RandomPositionMultiplier * _positionalMultiplier;
                Vector3 newRot = obj.Objects.Rotation * _rbData[i].RandomRotationMultiplier * _rotationalMultiplier;
                //If to localize force.
                if (_localizeShake)
                {
                    newPos = _rbData[i].Rigidbody.transform.TransformDirection(newPos);
                    newRot = _rbData[i].Rigidbody.transform.TransformDirection(newRot);
                }

                //Apply force.
                _rbData[i].Rigidbody.AddForce(newPos - _rbData[i].LastPositional, ForceMode.Impulse);
                _rbData[i].Rigidbody.AddTorque(newRot - _rbData[i].LastRotational, ForceMode.Impulse);
                //Set last values.
                _rbData[i].SetLastPositional(newPos);
                _rbData[i].SetLastRotational(newRot);
            }
        }
        #endregion

        /// <summary>
        /// Updates random multipliers if they are in need. What a fun name.
        /// </summary>
        private void CheckRandomizeRandomers(RigidbodyData data)
        {
            if (!_randomizeDirections)
                return;

            //Becomes true if enough time has passed to make new random multipliers.
            bool newRandomize = (Time.time > data.NextRandomizeTime);
            if (newRandomize)
                data.SetNextRandomizeTime(Time.time + Random.Range(3f, 7f));

            /* If new random multipliers or velocity is zero then randomize multipliers. */
            if (newRandomize || data.Rigidbody.velocity == Vector3.zero)
                data.SetRandomPositionMultiplier(Floats.RandomlyFlip(data.RandomPositionMultiplier));
            if (newRandomize || data.Rigidbody.angularVelocity == Vector3.zero)
                data.SetRandomRotationMultiplier(Floats.RandomlyFlip(data.RandomRotationMultiplier));
        }

        /// <summary>
        /// Received when visible in any camera.
        /// </summary>
        private void OnBecameVisible()
        {
            _inView = true;
            if (_requireInView)
                ChangeSubscription(true);
        }
        /// <summary>
        /// Received when no longer visible in any camera.
        /// </summary>
        private void OnBecameInvisible()
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
            //CameraShaker type.
            if (base.ShakerType == ShakerTypes.CameraShaker)
            {
                if (subscribe)
                    CameraShakerHandler.OnAllShakeFixedUpdate += CameraShakerHandler_OnShakeFixedUpdate;
                else
                    CameraShakerHandler.OnAllShakeFixedUpdate -= CameraShakerHandler_OnShakeFixedUpdate;
            }
            //ObjectShaker type.
            else if (base.ShakerType == ShakerTypes.ObjectShaker)
            {
                if (_objectShaker != null)
                {
                    if (subscribe)
                        _objectShaker.OnShakeFixedUpdate += ObjectShaker_OnShakeFixedUpdate;
                    else
                        _objectShaker.OnShakeFixedUpdate -= ObjectShaker_OnShakeFixedUpdate;
                }
            }
        }

    }
}