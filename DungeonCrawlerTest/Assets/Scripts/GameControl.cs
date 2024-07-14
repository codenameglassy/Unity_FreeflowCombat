using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;
using Cinemachine;

public class GameControl : MonoBehaviour
{
    public static GameControl instance;


    [Header("Cinemachine")]
    public CinemachineVirtualCamera navigateVCam;
    public CinemachineVirtualCamera combatVcam;
    public CinemachineVirtualCamera startVcam;
    public Transform playerCameraRoot;


    [Header("Game-Play")]
    public bool isGameStarted;
    public bool isGameOver;
    public PlayerControl playerControl;
    public ThirdPersonController thirdPersonController;

    public enum CameraState
    {
        none,
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
        GameControl.instance.cameraState = GameControl.CameraState.none;
        GameControl.instance.SwitchCamera();
        StartCoroutine(Enum_StartGame());
    }

    private void FixedUpdate()
    {
        SwitchCamera();
    }

    public void SwitchCamera()
    {
        switch (cameraState)
        {
            case CameraState.none:
                navigateVCam.Priority = 0;
                combatVcam.Priority = 0;
                startVcam.Priority = 10;
                break;

            case CameraState.Navigate:
                navigateVCam.Priority = 10;
                combatVcam.Priority = 0;
                startVcam.Priority = 0;

                break;
            case CameraState.Combat:
                combatVcam.Priority = 10;
                navigateVCam.Priority = 0;
                startVcam.Priority = 0;
                break;
        }
    }

    IEnumerator Enum_StartGame()
    {
        yield return new WaitForSeconds(3f);
        isGameStarted = true;
        navigateVCam.Follow = playerCameraRoot;
        combatVcam.Follow = playerCameraRoot;

        //enable player scripts
        thirdPersonController.enabled = true;
        playerControl.enabled = true;

        //GameControl.instance.cameraState = GameControl.CameraState.Navigate;
        //GameControl.instance.SwitchCamera();
    }
    
}
