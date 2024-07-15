using FirstGearGames.Utilities.Maths;
using System.Collections.Generic;
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker
{

    public class ShakableCanvas : ShakableBase
    {
        #region Types.
        private struct StartValues
        {
            public StartValues(Vector3 position, Vector3 rotation)
            {
                Position = position;
                Rotation = rotation;
            }
            /// <summary>
            /// Start position for an object.
            /// </summary>
            public readonly Vector3 Position;
            /// <summary>
            /// Start rotation for an object.
            /// </summary>
            public readonly Vector3 Rotation;
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// True to shake when the default camera shaker does. False to specify a camera shaker to use.
        /// </summary>
        [Tooltip("True to shake when the default camera shaker does. False to specify a camera shaker to use.")]
        [SerializeField]
        private bool _useDefaultCameraShaker = true;
        /// <summary>
        /// Camera shaker to monitor.
        /// </summary>
        [Tooltip("Camera shaker to monitor.")]
        [SerializeField]
        private CameraShaker _cameraShaker = null;
        /// <summary>
        /// Sets a new CameraShaker to use. This method will do nothing if using ShakableObject as the ShakerType.
        /// </summary>
        /// <param name="shaker"></param>
        public void SetCameraShaker(CameraShaker shaker)
        {
            if (base.ShakerType == ShakerTypes.ObjectShaker)
                return;

            if (_useDefaultCameraShaker)
            {
                if (Debug.isDebugBuild) Debug.LogWarning("Cannot set CameraShaker with UseDefaultCameraShaker set. If you wish to change CameraShaker at run-time set UseDefaultCameraShaker to false before entering play.");
            }
            else
            {
                ChangeCameraShakers(_cameraShaker, shaker, true);
            }
        }
        /// <summary>
        /// True to create a parent object and attach children to it. The parent object will be shaken instead of each individual canvas child. If your direct children move at all this value must be true. Setting value as false may incur extra cost as well.
        /// </summary>
        [Tooltip("True to create a parent object and attach children to it. The parent object will be shaken instead of each individual canvas child. If your direct children move at all this value must be true. Setting value as false may incur extra cost as well.")]
        [Space(10)]
        [SerializeField]
        private bool _encapsulateChildren = true;
        /// <summary>
        /// True to watch for additional children to encapsulate. This may be false if you do not add direct children to this canvas at runtime.
        /// </summary>
        [Tooltip("True to watch for additional children to encapsulate. This may be false if you do not add direct children to this canvas at runtime.")]
        [SerializeField]
        private bool _monitorEncapsulation = false;
        /// <summary>
        /// Positional shakes are multiplied by this value. Lower values will result in a lower positional magnitude.
        /// </summary>
        [Tooltip("Positional shakes are multiplied by this value. Lower values will result in a lower positional magnitude.")]
        [SerializeField]
        private float _positionalMultiplier = 1f;
        /// <summary>
        /// Rotational shakes are multiplied by this value. Lower values will result in lower ritational magnitude.
        /// </summary>
        [Tooltip("Rotational shakes are multiplied by this value. Lower values will result in lower rotational magnitude.")]
        [SerializeField]
        private float _rotationalMultiplier = 1f;
        /// <summary>
        /// True to randomly change influence direction when shaking starts.
        /// </summary>
        [Tooltip("True to randomly change influence direction when shaking starts.")]
        [Space(10)]
        [SerializeField]
        private bool _randomizeDirections = true;
        #endregion

        #region Private.
        /// <summary>
        /// Transform children are being attached to. This only exist if EncapsulateChildren is true.
        /// </summary>
        private RectTransform _parentRect;
        /// <summary>
        /// Start values for children of this transform.
        /// </summary>
        private Dictionary<Transform, StartValues> _childrenStartValues = new Dictionary<Transform, StartValues>();
        /// <summary>
        /// Next time to clean ChildrenStartValues.
        /// </summary>
        private float _nextCleanStartValuesTime;
        /// <summary>
        /// Current camera shaker this canvas is subscribed to.
        /// </summary>
        private CameraShaker _currentCameraShaker = null;
        /// <summary>
        /// ObjectShaker used for this object. May be null if not using ObjectShaker type.
        /// </summary>
        private ObjectShaker _objectShaker = null;
        /// <summary>
        /// Direction to multiply position by when shaking starts.
        /// </summary>
        private float _randomPositionMultiplier = 1f;
        /// <summary>
        /// Direction to multiply rotation by when shaking starts.
        /// </summary>
        private float _randomRotationMultiplier = 1f;
        #endregion

        private void Awake()
        {
            FirstInitialize();
        }

        private void OnEnable()
        {
            //Subscribe.
            ChangeSubscription(true);
        }

        private void Update()
        {
            /* If fails to encapsulate new children then remove script.
             * Something unrecoverable went wrong. */
            if (_monitorEncapsulation && !EncapsulateChildren(false))
            {
                DestroyImmediate(this);
                return;
            }

            CheckRemoveNullStartValues();
        }

        private void OnDisable()
        {
            //Unsubscribe.
            ChangeSubscription(false);

            ResetOffsets();
        }

        /// <summary>
        /// Initializes this script for use. Should only be cmpleted once.
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

            Canvas canvas = GetComponent<Canvas>();
            //Canvas null.
            if (canvas == null)
            {
                if (Debug.isDebugBuild) Debug.LogError("Canvas does not exist on this object, this script has been destroyed.");
                DestroyImmediate(this);
                return;
            }
            //World space canvases already shake.
            if (canvas.renderMode == RenderMode.WorldSpace)
            {
                if (Debug.isDebugBuild) Debug.LogError("ShakeableCanvas is not needed for Canvas RenderMode.WorldSpace");
                DestroyImmediate(this);
                return;
            }
            //Camera space canvases don't need this script when using matrix on the CameraShaker.
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                //Camera not set.
                if (canvas.worldCamera == null)
                {
                    if (Debug.isDebugBuild) Debug.LogWarning("WorldCamera is not set for this canvas. Cannot determine if this script is needed. If the CameraShaker for your intended WorldCamera is Matrix this script is not needed.");
                }
                //Camera known.
                else
                {
                    CameraShaker shaker = canvas.worldCamera.GetComponent<CameraShaker>();
                    if (shaker == null)
                    {
                        if (Debug.isDebugBuild) Debug.LogWarning("CameraShaker not found on WorldCamera. If the CameraShaker for your intended WorldCamera will use Matrix this script is not needed.");
                    }
                    else
                    {
                        if (shaker.ShakeTechnique == CameraShaker.ShakeTechniques.Matrix)
                            if (Debug.isDebugBuild) Debug.LogWarning("CameraShaker technique on WorldCamera is set to Matrix. This script is not needed for Matrix shake techniques. Ignore this message if you intend to change the ShakeTechnique.");
                    }
                }
            }

            //Subscribe to the CameraShaker if not using default.
            if (!_useDefaultCameraShaker)
                ChangeCameraShakers(null, _cameraShaker, false);

            //Encapsulation is enabled.
            if (_encapsulateChildren)
            {
                //Try to encapsulate children.
                if (!EncapsulateChildren(true))
                {
                    DestroyImmediate(this);
                    return;
                }
            }
            //Encapsulation is disabled, be sure to disable monitor as well.
            else
            {
                _monitorEncapsulation = false;
            }
        }

        /// <summary>
        /// Changes which CameraShaker to use when not using defualt CameraShaker.
        /// </summary>
        /// <param name="shaker"></param>
        /// <param name="subscribe"></param>
        private void ChangeCameraShakers(CameraShaker oldShaker, CameraShaker newShaker, bool resetOffsets = true)
        {
            //No change.
            if (oldShaker == newShaker)
                return;

            _currentCameraShaker = newShaker;

            //Since canvas subs and unsubs using OnEnable/Disable only change subscriptions if enabled.
            if (gameObject.activeInHierarchy)
            {
                //Offsets are automatically reset OnDisable, so only need to reset if active.
                if (resetOffsets)
                    ResetOffsets();

                ChangeCameraShakerSubscription(oldShaker, false);
                ChangeCameraShakerSubscription(newShaker, true);
            }
        }

        /// <summary>
        /// Encapsulate children transforms into a newly created transform.
        /// </summary>
        private bool EncapsulateChildren(bool initialization)
        {
            if (!_encapsulateChildren)
                return true;

            //If being run for the first time.
            if (initialization)
            {
                GameObject obj = new GameObject();
                //Shouldn't happen but just incase.
                if (obj == null)
                {
                    if (Debug.isDebugBuild) Debug.LogError("Encapsulation failed because parent object could not be created.");
                    return false;
                }
                //Add a rect since this is a UI object.
                _parentRect = obj.AddComponent<RectTransform>();
                //Shouldn't happen but just incase.
                if (_parentRect == null)
                {
                    if (Debug.isDebugBuild) Debug.LogError("Encapsulation failed because parentRect could not be created.");
                    return false;
                }

                //Setup parent rect to be full screen/stretched.
                _parentRect.name = "ShakableParentRect";
                _parentRect.SetParent(transform);
                _parentRect.anchorMin = new Vector2(0f, 0f);
                _parentRect.anchorMax = new Vector2(1f, 1f);
                _parentRect.offsetMin = Vector2.zero;
                _parentRect.offsetMax = Vector2.zero;
                _parentRect.localScale = Vector3.one;
                _parentRect.localPosition = Vector3.zero;
                _parentRect.localEulerAngles = Vector3.zero;
            }

            //If the parent rect somehow got destroyed, shouldn't be possible.
            if (_parentRect == null)
                return false;

            int childCount = transform.childCount;

            /* If parent rect is a child of this, and child count is 1 then no reason to go
             * further as there are no other children. This isn't considered a failure. */
            if (_parentRect.parent == transform && childCount == 1)
                return true;

            /* Since the child collection of this transform will change
             * as children are re-ordered a local copy is set first
             * and navigated to ensure all children objects are set
             * properly. */

            Transform[] children = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
                children[i] = transform.GetChild(i);

            //Child to rect parent if not rect parent.
            for (int i = 0; i < childCount; i++)
            {
                if (children[i] != _parentRect.transform && children[i].gameObject.activeInHierarchy)
                    children[i].SetParent(_parentRect, false);
            }

            return true;
        }

        #region ShakeUpdates.
        /// <summary>
        /// Received when shaking starts when previously stopped on all Shakers.
        /// </summary>
        private void OnShakingStarted()
        {
            RandomizeDirections();
        }
        /// <summary>
        /// Received when shaking starts when previously stopped on all Shakers.
        /// </summary>
        private void CameraShaker_OnShakingStarted(CameraShaker obj)
        {
            OnShakingStarted();
        }
        /// <summary>
        /// Received when shaking starts when previously stopped on ObjectShaker.
        /// </summary>
        private void ObjectShaker_OnShakingStarted(ObjectShaker obj)
        {
            OnShakingStarted();
        }
        /// <summary>
        /// Received every update a shake occurs.
        /// </summary>
        /// <param name="obj"></param>
        private void CameraShaker_OnShakeUpdate(CameraShaker shaker, ShakeUpdate obj)
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
            //No reason to shake if not active in scene.
            if (!gameObject.activeInHierarchy)
                return;

            Vector3 positionalOffset = obj.Canvases.Position * _positionalMultiplier;
            Vector3 rotationalOffset = obj.Canvases.Rotation * _rotationalMultiplier;

            //If using an encapsulation.
            if (_parentRect != null)
            {
                _parentRect.localPosition = positionalOffset;
                _parentRect.localEulerAngles = rotationalOffset;
            }
            //Not using encapsulation.
            else
            {
                foreach (Transform t in transform)
                {
                    Vector3 pos;
                    Vector3 rot;
                    StartValues startValues;

                    //If already in dictionary.
                    if (_childrenStartValues.TryGetValue(t, out startValues))
                    {
                        pos = startValues.Position + positionalOffset;
                        rot = startValues.Rotation + rotationalOffset;
                    }
                    //Not yet in dictionary.
                    else
                    {
                        _childrenStartValues.Add(t, new StartValues(t.localPosition, t.localEulerAngles));
                        pos = t.localPosition + positionalOffset;
                        rot = t.localEulerAngles + rotationalOffset;
                    }

                    t.localPosition = pos;
                    t.localEulerAngles = rot;
                }
            }
        }
        #endregion

        /// <summary>
        /// Updates random multipliers for shakable.
        /// </summary>
        private void RandomizeDirections()
        {
            if (!_randomizeDirections)
                return;

            _randomPositionMultiplier = Floats.RandomlyFlip(_randomPositionMultiplier);
            _randomRotationMultiplier = Floats.RandomlyFlip(_randomRotationMultiplier);
        }

        /// <summary>
        /// Resets the offsets on children.
        /// </summary>
        private void ResetOffsets()
        {
            //If using an encapsulation.
            if (_parentRect != null)
            {
                _parentRect.localPosition = Vector3.zero;
                _parentRect.localEulerAngles = Vector3.zero;
            }
            //Not using encapsulation.
            else
            {
                foreach (KeyValuePair<Transform, StartValues> dict in _childrenStartValues)
                {
                    if (dict.Key != null)
                    {
                        dict.Key.localPosition = dict.Value.Position;
                        dict.Key.localEulerAngles = dict.Value.Rotation;
                    }
                }
            }
        }

        /// <summary>
        /// Periodically removes null values from ChildrenStartValues. Should be called every frame.
        /// </summary>
        private void CheckRemoveNullStartValues()
        {
            //ParentRect is immune to this behaviour, not needed if using parent rect.
            if (_parentRect != null)
                return;

            //Only clean every 30 seconds. More than enough to prevent a memory leak.
            if (Time.unscaledTime < _nextCleanStartValuesTime)
                return;
            _nextCleanStartValuesTime = Time.unscaledTime + 30f;

            //Build a collection of null keys then remove them from the dictionary after.
            List<Transform> keysToRemove = new List<Transform>();
            foreach (KeyValuePair<Transform, StartValues> dict in _childrenStartValues)
            {
                if (dict.Key == null)
                    keysToRemove.Add(dict.Key);
            }
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                try
                {
                    _childrenStartValues.Remove(keysToRemove[i]);
                }
                catch { }
            }
        }

        #region Change subscriptions.
        /// <summary>
        /// Changes the subscription to a camera shaker.
        /// </summary>
        /// <param name="shaker"></param>
        /// <param name="subscribe"></param>
        private void ChangeCameraShakerSubscription(CameraShaker shaker, bool subscribe)
        {
            if (shaker == null)
                return;

            if (subscribe)
            {
                shaker.OnShakeUpdate += CameraShaker_OnShakeUpdate;
                shaker.OnShakingStarted += CameraShaker_OnShakingStarted;
            }
            else
            {
                shaker.OnShakeUpdate -= CameraShaker_OnShakeUpdate;
                shaker.OnShakingStarted -= CameraShaker_OnShakingStarted;
            }
        }
        /// <summary>
        /// Changes the subscription to the default camera shaker by using CameraShakerHandler.
        /// </summary>
        /// <param name="subscribe"></param>
        private void ChangeDefaultCameraShakerSubscription(bool subscribe)
        {
            if (subscribe)
            {
                CameraShakerHandler.OnShakeUpdate += CameraShaker_OnShakeUpdate;
                CameraShakerHandler.OnShakingStarted += CameraShaker_OnShakingStarted;
            }
            else
            {
                CameraShakerHandler.OnShakeUpdate -= CameraShaker_OnShakeUpdate;
                CameraShakerHandler.OnShakingStarted -= CameraShaker_OnShakingStarted;
            }
        }
        /// <summary>
        /// Changes subscriptions based on current settings and shaker type.
        /// </summary>
        /// <param name="subscribe"></param>
        private void ChangeSubscription(bool subscribe)
        {
            //CameraShaker type.
            if (base.ShakerType == ShakerTypes.CameraShaker)
            {
                //If using default camera shaker then subscribe to default on enable.
                if (_useDefaultCameraShaker)
                    ChangeDefaultCameraShakerSubscription(subscribe);
                else
                    ChangeCameraShakerSubscription(_currentCameraShaker, subscribe);
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
                    }
                    else
                    { 
                        _objectShaker.OnShakeUpdate -= ObjectShaker_OnShakeUpdate;
                        _objectShaker.OnShakingStarted -= ObjectShaker_OnShakingStarted;
                    }
                }
            }
        }
        #endregion

    }

}