using UnityEngine;
using UnityEngine.UI;

namespace FirstGearGames.SmoothCameraShaker.Demo
{

    /// <summary>
    /// ************************
    /// THIS IS FOR DEMO PORPOISES ONLY; IT'S UGLY, YOU'VE BEEN WARNED.
    /// ************************
    /// </summary>
    public class CanvasButtonActions : MonoBehaviour
    {
        public static bool ShakeCanvas { get; private set; } = true;
        public static bool ShakeRigidbodies { get; private set; } = true;

        [SerializeField]
        private GameObject _2dObject = null;
        [SerializeField]
        private CameraShaker[] _2dCameras = new CameraShaker[2];
        [SerializeField]
        private GameObject _3dObject = null;
        [SerializeField]
        private CameraShaker[] _3dCameras = new CameraShaker[2];
        [SerializeField]
        private Slider _scaleSlider = null;

        [SerializeField]
        private ShakeData _earthquakeData = null;
        [SerializeField]
        private ShakeData _offRoadData = null;

        private Resettable[] _resettables = new Resettable[0];

        private ShakerInstance _earthQuakeInstance;
        private ShakerInstance _offRoadInstance;

        private void Awake()
        {
            _scaleSlider.onValueChanged.AddListener(ScaleSlider_ValueChanged);
            _resettables = FindObjectsOfType<Resettable>();
        }

        private void Start()
        {
            OnClick_3D(true);
        }

        public void OnClick_Reset()
        {
            ResetAll();
        }

        public void OnClick_2D()
        {
            if (_2dObject.activeInHierarchy)
                return;

            _3dObject.SetActive(false);
            _2dObject.SetActive(true);
            ResetAll();
        }

        public void OnClick_3D(bool forced = false)
        {
            if (_3dObject.activeInHierarchy && !forced)
                return;

            _2dObject.SetActive(false);
            _3dObject.SetActive(true);
            ResetAll();
        }

        public void OnClick_ChangeCamera()
        {
            CameraShaker from, to;
            //If currently in 2d.
            if (_2dObject.activeInHierarchy)
            {                
                if (_2dCameras[0].gameObject.activeInHierarchy)
                {
                    from = _2dCameras[0];
                    to = _2dCameras[1];
                }
                else
                {
                    from = _2dCameras[1];
                    to = _2dCameras[0];
                }
            }
            //Currently in 3d.
            else
            {
                if (_3dCameras[0].gameObject.activeInHierarchy)
                {
                    from = _3dCameras[0];
                    to = _3dCameras[1];
                }
                else
                {
                    from = _3dCameras[1];
                    to = _3dCameras[0];
                }
            }

            to.gameObject.SetActive(true);
            CameraShakerHandler.CopyShakerInstances(from, to);
            from.gameObject.SetActive(false);
        }

        public void OnClick_ShakeCanvas(Toggle toggle)
        {
            ShakeCanvas = toggle.isOn;

            if (_earthQuakeInstance != null)
                _earthQuakeInstance.Data.SetShakeCanvases(ShakeCanvas);
            if (_offRoadInstance != null)
                _offRoadInstance.Data.SetShakeCanvases(ShakeCanvas);
        }

        public void OnClick_ShakeRigidbodies(Toggle toggle)
        {
            ShakeRigidbodies = toggle.isOn;

            if (_earthQuakeInstance != null)
                _earthQuakeInstance.Data.SetShakeRigidbodies(ShakeRigidbodies);
            if (_offRoadInstance != null)
                _offRoadInstance.Data.SetShakeRigidbodies(ShakeRigidbodies);
        }


        private void ResetAll()
        {

            foreach (Resettable r in _resettables)
            {
                r.PerformReset();
                //Suspend rocks.
                if (r is Rock)
                    MakeKinematic(r.gameObject, true);
            }

            CameraShakerHandler.StopAll();
            _offRoadInstance = null;
            _earthQuakeInstance = null;

            if (_scaleSlider.value != 1f)
                _scaleSlider.value = 1f;
            else
                ScaleSlider_ValueChanged(1f);
        }

        public void OnClick_Boulders()
        {
            foreach (Resettable r in _resettables)
            {
                if (r is Rock)
                { 
                    r.PerformReset();
                    MakeKinematic(r.gameObject, false);
                }
            }
        }

        public void OnClick_OffRoad()
        {
            if (_offRoadInstance != null)
            {
                _offRoadInstance.FadeOut();
                _offRoadInstance = null;
            }
            else
            {
                _offRoadInstance = CameraShakerHandler.Shake(_offRoadData);
                _offRoadInstance.Data.SetShakeCanvases(ShakeCanvas);
                _offRoadInstance.Data.SetShakeRigidbodies(ShakeRigidbodies);
            }
        }

        public void OnClick_Earthquake()
        {
            if (_earthQuakeInstance != null)
            { 
                _earthQuakeInstance.FadeOut();
                _earthQuakeInstance = null;
            }
            else
            {
                _earthQuakeInstance = CameraShakerHandler.Shake(_earthquakeData);
                _earthQuakeInstance.Data.SetShakeCanvases(ShakeCanvas);
                _earthQuakeInstance.Data.SetShakeRigidbodies(ShakeRigidbodies);
            }
        }


        private void MakeKinematic(GameObject obj, bool kinematic)
        {
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null)
            { 
                rb.isKinematic = kinematic;
            }
            else
            {
                Rigidbody2D rb2d = obj.GetComponent<Rigidbody2D>();
                rb2d.simulated = !kinematic;
            }
        }

        public void ScaleSlider_ValueChanged(float value)
        {
            /* 2D camera moves more because of orthographic size/ppu.
             * Normally you would design your shakes based on if using 3D
             * or 2D, but since I'm using 3D shakes for both I can easily
             * make them of appropriate values by reducing their scale. */
            if (_2dObject.activeInHierarchy)
                value *= 0.65f;

            CameraShakerHandler.SetScaleAll(value, true);
        }
    }


}