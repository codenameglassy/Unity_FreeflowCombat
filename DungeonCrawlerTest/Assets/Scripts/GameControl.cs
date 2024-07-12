using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameControl : MonoBehaviour
{

    public static GameControl instance;

    [Header("Scene")]
    public List<Transform> allTargetsInScene = new List<Transform>();
    public List<Transform> allTargetsInRange = new List<Transform>();
    private int currentIndex = 0;
    public float range = 10f; // Set this to the desired range
    public PlayerControl playerInScene;
    [Space]
    [Header("Scene")]
    public bool debug;

    void Awake()
    {
        instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {

        playerInScene = FindObjectOfType<PlayerControl>();

        PopulateTargetInScene();

        PopluateTargetInRange();
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

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator RunEvery200ms()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.15f); // Wait for 200 milliseconds

            // Call your function here
            RepopulateTargetInRange();
            playerInScene.CheckTargetInRange();
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
            if (Vector3.Distance(playerInScene.transform.position, obj.transform.position) <= range)
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
}
