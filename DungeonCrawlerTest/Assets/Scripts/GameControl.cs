using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using StarterAssets;
using Cinemachine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class GameControl : MonoBehaviour
{
    public static GameControl instance;


    [Header("Cinemachine")]
    public CinemachineVirtualCamera navigateVCam;
    public CinemachineVirtualCamera combatVcam;
    public CinemachineVirtualCamera startVcam;
    public Transform playerCameraRoot;

    public enum CameraState
    {
        none,
        Navigate,
        Combat
    }
    public CameraState cameraState;

   
    public bool isGameStarted;
    public bool isGameOver;
    public PlayerControl playerControl;
    public ThirdPersonController thirdPersonController;

    [Header("Canvas")]
    public CanvasGroup bloodOverlay;
    public CanvasGroup fadeCanvas;
    public CanvasGroup gameoverOverlay;



    public GameObject enemy;
    public List<Transform> spawnPosList = new List<Transform>();
    private void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        fadeCanvas.alpha = 1.0f;
        fadeCanvas.DOFade(0, 1f);
        GameControl.instance.cameraState = GameControl.CameraState.none;
        GameControl.instance.SwitchCamera();
        StartCoroutine(Enum_StartGame());

    }

    public void SpawnEnemy()
    {
        StartCoroutine(Enum_SpawnWave());
    }

    IEnumerator Enum_SpawnWave()
    {
        yield return new WaitForSeconds(1f);

        for (int i = 0; i < spawnPosList.Count; i++)
        {
            Instantiate(enemy, spawnPosList[i].position, Quaternion.identity);
        }

        yield return new WaitForSeconds(15f);

        StartCoroutine(Enum_SpawnWave());
    }

    private void FixedUpdate()
    {
        SwitchCamera();
    }

    public void BloodOverlay()
    {
        bloodOverlay.DOFade(1, 0.2f).OnComplete(() => 
        bloodOverlay.DOFade(0, 0.5f));
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

        Cursor.lockState = CursorLockMode.Locked;//
        Cursor.visible = false;//

        yield return new WaitForSeconds(3f);
        isGameStarted = true;
        navigateVCam.Follow = playerCameraRoot;
        combatVcam.Follow = playerCameraRoot;

        //enable player scripts
        thirdPersonController.enabled = true;
        playerControl.enabled = true;

    }
    
    public void GameOver()
    {
        StartCoroutine(Enum_Gameover());
    }

    IEnumerator Enum_Gameover()
    {
        Debug.Log("SubmitingScore");
        string currentUsername = PlayerPrefs.GetString("InputFieldValue");
        Leaderboard.instance.SetLeaderboardEntry(currentUsername, ScoreManager.instance.GetCurrentScore());
        for (int i = 0; i < TargetDetectionControl.instance.allTargetsInScene.Count; i++)
        {
            TargetDetectionControl.instance.allTargetsInScene[i].GetComponent<EnemyBase>().Gameover();
        }

        yield return new WaitForSeconds(1f);
        //Leaderboard.instance.GetLeaderBoard();
        fadeCanvas.DOFade(1, 2f);

        //reset cursor
        Cursor.lockState = CursorLockMode.None;//
        Cursor.visible = true;//

        yield return new WaitForSeconds(1.5f);
        gameoverOverlay.gameObject.SetActive(true);
        gameoverOverlay.DOFade(1, 2f);

    }
}
