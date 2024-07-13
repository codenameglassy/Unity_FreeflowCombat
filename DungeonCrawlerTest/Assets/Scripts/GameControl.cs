using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class GameControl : MonoBehaviour
{

    public static GameControl instance;

    [Header("Components")]
    public PlayerControl playerControl;
    private StarterAssetsInputs _input;

    [Header("Scene")]
    public List<Transform> allTargetsInScene = new List<Transform>();
    public List<Transform> allTargetsInRange = new List<Transform>();
    private int currentIndex = 0;
    public PlayerControl playerInScene;
    //public float range = 10f; // Set this to the desired range


    [Space]
    [Header("TargetDetection")]
    private Vector3 inputDirection;
    public LayerMask whatIsEnemy;
    public bool canChangeTarget;
    [Range(0f, 10f)] public float detectionRange;
    [Range(0f, 5f)] public float sphereCastRadius;

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
      
        _input = FindObjectOfType<StarterAssetsInputs>();
        playerInScene = FindObjectOfType<PlayerControl>();

        PopulateTargetInScene();


        //PopluateTargetInRange();
       // StartCoroutine(RunEvery200ms());
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

    // Update is called once per frame
    void Update()
    {
        DetectEnemyTopDown();
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
        var cam = Camera.main;

        // Forward direction for top-down camera is along the camera's z-axis (projected onto the xz-plane)
        var forward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;

        // Right direction for top-down camera is along the camera's x-axis (projected onto the xz-plane)
        var right = new Vector3(cam.transform.right.x, 0f, cam.transform.right.z).normalized;

        // Get the input direction in the xz-plane
        inputDirection = forward * _input.move.y + right * _input.move.x;
        inputDirection = inputDirection.normalized;

        RaycastHit info;
        if (Physics.SphereCast(checkPos.position, sphereCastRadius, inputDirection, out info, detectionRange, whatIsEnemy) && canChangeTarget)
        {
            // playerControl.target = info.collider.transform;
            if(info.collider.transform.GetComponent<EnemyBase>())
                playerControl.ChangeTarget(info.collider.transform);
        }
       
    }

    private IEnumerator RunEvery200ms()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.15f); // Wait for 200 milliseconds
            DetectEnemyTopDown();
            // Call your function here
            /* RepopulateTargetInRange();
             playerInScene.CheckTargetInRange();*/
        }
    }

  
    public void RepopulateTargetInRange() //Clear the list, repopluate and Get Next Target
    {
        allTargetsInRange.Clear();
        PopluateTargetInRange();
    }

    public void PopluateTargetInRange()
    {
        if(playerInScene == null)
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

        if(allTargetsInRange.Count <= 0)
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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawRay(checkPos.position, inputDirection);
        Gizmos.DrawWireSphere(checkPos.position, 1);

        if (playerControl.target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(playerControl.target.transform.position, 0.5f);
        }
            
    }
}
