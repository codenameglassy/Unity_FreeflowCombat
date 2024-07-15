using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCam : MonoBehaviour
{

    public Vector3 rotationOffset; // Rotation offset in Euler angles

    private void Start()
    {
        // Get the main camera
        Camera mainCamera = Camera.main;

        // Ensure there is a main camera
        if (mainCamera != null)
        {
            // Make the object look at the camera
            transform.LookAt(mainCamera.transform);

            // Apply the rotation offset
            transform.rotation = transform.rotation * Quaternion.Euler(rotationOffset);
        }

    }
    void Update()
    {
        // Get the main camera
        Camera mainCamera = Camera.main;

        // Ensure there is a main camera
        if (mainCamera != null)
        {
            // Make the object look at the camera
            transform.LookAt(mainCamera.transform);

            // Apply the rotation offset
            transform.rotation = transform.rotation * Quaternion.Euler(rotationOffset);
        }
    }
}
