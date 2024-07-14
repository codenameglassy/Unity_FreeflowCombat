using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class GameControl : MonoBehaviour
{

    public static GameControl instance;

    [Header("Components")]
    public PlayerControl playerControl;
    private StarterAssetsInputs _input; //not used

    [Header("Scene")]
    public List<Transform> allTargetsInScene = new List<Transform>();
    public List<Transform> allTargetsInRange = new List<Transform>(); //not used
    private int currentIndex = 0;
    public PlayerControl playerInScene;

    [Space]
    [Header("TargetDetection")]
    private Vector3 inputDirection;
    public LayerMask whatIsEnemy;
    public bool canChangeTarget;
    [Range(0f, 10f)] public float detectionRange;
    [Range(0f, 5f)] public float sphereCastRadius;
    [Range(0f, 1f)] public float dotProductThreshold = 0.7f;

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

    public void RepopulateTargetInRange() //Clear the list, repopluate and Get Next Target
    {
        allTargetsInRange.Clear();
        PopluateTargetInRange();
    }
    public void PopluateTargetInRange()
    {
        if (playerInScene == null)
        {
            if (debug)
                Debug.LogError("Player is not found in scene");

            return;
        }

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
            if (Vector3.Distance(playerInScene.transform.position, obj.transform.position) <= detectionRange)
            {
                /* if (debug)
                     Debug.Log("Found target in range: " + obj.name);*/

                allTargetsInRange.Add(obj.transform);
            }
        }

        if (allTargetsInRange.Count <= 0)
        {
            playerInScene.target = null;
        }

    }
    public Transform GetNextTargetInRange()
    {
        if (allTargetsInRange == null || allTargetsInRange.Count == 0)
        {
            if (debug)
                Debug.LogWarning("No GameObjects found.");
            return null;
        }

        // wrap around if necessary
        currentIndex = (currentIndex) % allTargetsInRange.Count;

        // Get the current GameObject
        Transform currentTarget = allTargetsInRange[currentIndex];

        // Output the name of the current GameObject
        if (debug)
            Debug.Log("Current GameObject: " + currentTarget.name);

        // Increment the index and wrap around if necessary
        currentIndex = (currentIndex + 1) % allTargetsInRange.Count;

        if (debug)
        {
            Debug.Log(currentIndex + " current target index");
        }

        return currentTarget;
    }

    void DetectEnemy3d()
    {
        var cam = Camera.main;
        var forward = cam.transform.forward;
        var right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        inputDirection = forward * _input.move.y + right * _input.move.x;
        inputDirection = inputDirection.normalized;

        RaycastHit info;
        if (Physics.SphereCast(checkPos.position, sphereCastRadius, inputDirection, out info, detectionRange, whatIsEnemy))
        {
            // if (info.collider.transform.GetComponent<EnemyBase>())
            if (canChangeTarget)
                playerControl.target = info.collider.transform;
            //info.collider.transform.GetComponent<EnemyBase>().ActiveTarget(true);
        }
    }

    void DetectEnemyTopDown()
    {
        if (!canChangeTarget)
        {
            return;
        }
        var cam = Camera.main;

        // Forward direction for top-down camera is along the camera's z-axis (projected onto the xz-plane)
        var forward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;

        // Right direction for top-down camera is along the camera's x-axis (projected onto the xz-plane)
        var right = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;

        // Get the input direction in the xz-plane
        inputDirection = forward * _input.move.y + right * _input.move.x;
        inputDirection = inputDirection.normalized;

        RaycastHit info;
        if (Physics.SphereCast(checkPos.position, sphereCastRadius, inputDirection, out info, detectionRange, whatIsEnemy))
        {
            // playerControl.target = info.collider.transform;
            if (info.collider.transform.GetComponent<EnemyBase>())
                playerControl.ChangeTarget(info.collider.transform);
        }

    }

    void DetectEnemyNewMethod()
    {
        if (!canChangeTarget)
        {
            return;
        }

        // Get input direction
        Vector3 inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        if (inputDirection.sqrMagnitude < 0.1f)
        {
            return;
        }

        // Normalize the direction to get a unit vector
        inputDirection.Normalize();

        // Perform the raycast
        Ray ray = new Ray(checkPos.position, inputDirection);
        // Debug the ray
        Debug.DrawRay(ray.origin, ray.direction * detectionRange, Color.red);

        RaycastHit[] hits;
        hits = Physics.SphereCastAll(ray, sphereCastRadius, detectionRange, whatIsEnemy);

        if (hits.Length > 0)
        {
            // Find the closest enemy
            GameObject closestEnemy = null;
            float closestDistance = Mathf.Infinity;

            foreach (RaycastHit hit in hits)
            {
                float distance = Vector3.Distance(transform.position, hit.point);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEnemy = hit.collider.gameObject;
                }
            }

            if (closestEnemy != null)
            {
                playerControl.ChangeTarget(closestEnemy.transform);
                Debug.Log("Closest enemy detected: " + closestEnemy.name);
            }
        }


    }


    #endregion
}
