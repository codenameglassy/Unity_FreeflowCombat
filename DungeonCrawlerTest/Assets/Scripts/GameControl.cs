using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameControl : MonoBehaviour
{
    public static GameControl instance;


    [Header("Cinemachine")]
    public CinemachineVirtualCamera navigateVCam;
    public CinemachineVirtualCamera combatVcam;
    public enum CameraState
    {
        Navigate,
        Combat
    }
    public CameraState cameraState;

    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       
    }

    private void FixedUpdate()
    {
        SwitchCamera();
    }

    public void SwitchCamera()
    {
        switch (cameraState)
        {
            case CameraState.Navigate:
                navigateVCam.Priority = 10;
                combatVcam.Priority = 0;

                break;
            case CameraState.Combat:
                combatVcam.Priority = 10;
                navigateVCam.Priority = 0;
                break;
        }
    }

    
}
