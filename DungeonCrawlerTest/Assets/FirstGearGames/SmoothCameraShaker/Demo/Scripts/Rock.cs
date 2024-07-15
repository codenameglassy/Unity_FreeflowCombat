using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker.Demo
{


    public class Rock : Resettable
    {
        #region Serialized.
        [SerializeField]
        private ShakeData _shakeData = null;
        #endregion

        private float _mass;
        private bool _shook = false;

        protected override void Awake()
        {
            base.Awake();
            _mass = (transform.localScale.x + transform.localScale.y + transform.localScale.z) / 3f;
        }

        private void OnCollisionEnter(Collision collision) { DoCollisionEntered(); }
        private void OnCollisionEnter2D(Collision2D collision) { DoCollisionEntered(); }

        private void DoCollisionEntered()
        {
            if (_shook)
                return;
            _shook = true;

            ShakerInstance instance = CameraShakerHandler.Shake(_shakeData);
            instance.Data.SetShakeCanvases(CanvasButtonActions.ShakeCanvas);
            instance.Data.SetShakeRigidbodies(CanvasButtonActions.ShakeRigidbodies);
            instance.MultiplyMagnitude(_mass, -1);

        }

        public override void PerformReset()
        {
            base.PerformReset();
            _shook = false;
        }
    }


}