using UnityEngine;


namespace FirstGearGames.SmoothCameraShaker.Demo
{


    public abstract class Resettable : MonoBehaviour
    {
        protected Rigidbody Rb;
        protected Rigidbody2D Rb2d;
        protected Vector3 StartPosition;
        protected Quaternion StartRotation;

        protected virtual void Awake()
        {
            Rb = GetComponent<Rigidbody>();
            Rb2d = GetComponent<Rigidbody2D>();
            StartPosition = transform.position;
            StartRotation = transform.rotation;
        }

        public virtual void PerformReset()
        {
            if (Rb != null)
            { 
                Rb.velocity = Vector3.zero;
                Rb.angularVelocity = Vector3.zero;
            }
            if (Rb2d != null)
            {
                Rb2d.velocity = Vector2.zero;
                Rb2d.angularVelocity = 0f;
            }

            transform.position = StartPosition;
            transform.rotation = StartRotation;
        }
    }


}