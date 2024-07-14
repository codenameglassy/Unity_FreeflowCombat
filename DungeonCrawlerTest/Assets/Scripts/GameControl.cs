using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GameControl : MonoBehaviour
{

    public static GameControl instance;

    [Header("Components")]
    public PlayerControl playerControl;

    [Header("Scene")]
    public List<Transform> allTargetsInScene = new List<Transform>();
    public PlayerControl playerInScene;

    [Space]
    [Header("TargetDetection")]
    public LayerMask whatIsEnemy;
    public bool canChangeTarget;
    [Range(0f, 10f)] public float detectionRange;
    [Tooltip("Dot Product Threshold \nHigher Values: More strict alignment required \nLower Values: Allows for broader targeting")]
    [Range(0f, 1f)] public float dotProductThreshold = 0.1f;

    [Space]
    [Header("debug")]
    public bool debug;
    public Transform checkPos;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        playerInScene = FindObjectOfType<PlayerControl>();

        PopulateTargetInScene();
        StartCoroutine(RunEvery200ms());
    }

    private void PopulateTargetInScene()
    {
        // Find all active GameObjects in the scene
        EnemyBase[] allGameObjects = FindObjectsOfType<EnemyBase>();

        // Convert the array to a list
        List<EnemyBase> gameObjectList = new List<EnemyBase>(allGameObjects);

        // Output the number of GameObjects found
        if (debug)
            Debug.Log("Number of targets found: " + gameObjectList.Count);

        // Optionally, iterate over the list and do something with each GameObject
        foreach (EnemyBase obj in gameObjectList)
        {
            allTargetsInScene.Add(obj.transform);
        }
    }

    private IEnumerator RunEvery200ms()
    {
        while (true)
        {
            yield return new WaitForSeconds(.1f); // Wait for 'x' milliseconds
            GetEnemyInInputDirection();
        }
    }

    #region Get Enemy In Input Direction

    public void GetEnemyInInputDirection()
    {
        if (canChangeTarget)
        {
            Vector3 inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;

            if (inputDirection != Vector3.zero)
            {
                inputDirection = Camera.main.transform.TransformDirection(inputDirection);
                inputDirection.y = 0;
                inputDirection.Normalize();


                Transform closestEnemy = GetClosestEnemyInDirection(inputDirection);

                if (closestEnemy != null && (Vector3.Distance(transform.position, closestEnemy.position)) <= detectionRange)
                {
                    playerControl.ChangeTarget(closestEnemy);
                    // Do something with the closest enemy in the input direction
                    Debug.Log("Closest enemy in direction: " + closestEnemy.name);
                }
            }

        }
    }
    
    Transform GetClosestEnemyInDirection(Vector3 inputDirection)
    {
        Transform closestEnemy = null;
        float maxDotProduct = dotProductThreshold; // Start with the threshold value

        foreach (Transform enemy in allTargetsInScene)
        {
            Vector3 enemyDirection = (enemy.position - transform.position).normalized;
            float dotProduct = Vector3.Dot(inputDirection, enemyDirection);

            if (dotProduct > maxDotProduct)
            {
                maxDotProduct = dotProduct;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    #endregion

    #region Unused Code/ Might Delete Later


    #endregion
}
