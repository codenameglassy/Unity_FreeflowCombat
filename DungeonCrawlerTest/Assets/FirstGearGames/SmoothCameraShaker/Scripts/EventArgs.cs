
using UnityEngine;

namespace FirstGearGames.SmoothCameraShaker
{
    public class ShakeUpdate
    {
        public ShakeUpdate()
        {
            Camera = new ShakeValues();
            Canvases = new ShakeValues();
            Objects = new ShakeValues();
        }
        public ShakeUpdate(ShakeValues camera, ShakeValues canvases, ShakeValues objects)
        {
            Camera = camera;
            Canvases = canvases;
            Objects = objects;
        }
        /// <summary>
        /// ShakeValues for the camera.
        /// </summary>
        public readonly ShakeValues Camera;
        /// <summary>
        /// ShakeValues for canvases.
        /// </summary>
        public readonly ShakeValues Canvases;
        /// <summary>
        /// ShakeValues for rigidbodies.
        /// </summary>
        public readonly ShakeValues Objects;
    }

    public class ShakeValues
    {
        public ShakeValues()
        {
            Position = Vector3.zero;
            Rotation = Vector3.zero;
        }
        public ShakeValues(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
        }
        /// <summary>
        /// Position value of the shake.
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// Rotation value of the shake.
        /// </summary>
        public Vector3 Rotation;
    }

    public struct CameraShakerChange
    {
        public CameraShakerChange(CameraShaker oldShaker, CameraShaker newShaker)
        {
            OldShaker = oldShaker;
            NewShaker = newShaker;
        }

        /// <summary>
        /// Old CameraShaker.
        /// </summary>
        public readonly CameraShaker OldShaker;
        /// <summary>
        /// New CameraShaker.
        /// </summary>
        public readonly CameraShaker NewShaker;
    }
}