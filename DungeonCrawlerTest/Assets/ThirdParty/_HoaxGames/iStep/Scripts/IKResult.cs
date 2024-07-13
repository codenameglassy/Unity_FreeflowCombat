using UnityEngine;

namespace HoaxGames
{
    // Copyright © Kreshnik Halili

    public class IKResult
    {
        public IKResult(Vector3 _ikPos, Vector3 _gluedIkPos, bool _isGlued, Vector3 _bottomPoint, Vector3 _surfacePoint, Vector3 _normal, Transform _primaryHitTransform, Transform _secondaryHitTransform)
        {
            init(_ikPos, _gluedIkPos, _isGlued, _bottomPoint, _surfacePoint, _normal, _primaryHitTransform, _secondaryHitTransform);
        }

        public void init(Vector3 _ikPos, Vector3 _gluedIkPos, bool _isGlued, Vector3 _bottomPoint, Vector3 _surfacePoint, Vector3 _normal, Transform _primaryHitTransform, Transform _secondaryHitTransform)
        {
            ikPos = _ikPos;
            gluedIKPos = _gluedIkPos;
            isGlued = _isGlued;
            bottomPoint = _bottomPoint;
            surfacePoint = _surfacePoint;
            normal = _normal;
            primaryHitTransform = _primaryHitTransform;
            secondaryHitTransform = _secondaryHitTransform;
        }

        public Vector3 ikPos;
        public Vector3 gluedIKPos;
        public bool isGlued;
        public Vector3 bottomPoint;
        public Vector3 surfacePoint;
        public Vector3 normal;
        public Transform primaryHitTransform;
        public Transform secondaryHitTransform;
    }

    [System.Serializable]
    public class IKResultEvent : UnityEngine.Events.UnityEvent<IKResult>
    {
    }
}