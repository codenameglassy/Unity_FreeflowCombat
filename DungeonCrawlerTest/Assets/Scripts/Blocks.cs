using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Blocks : MonoBehaviour
{
    public List<GameObject> traps = new List<GameObject>();
    public List<GameObject> pillars = new List<GameObject>();
    public Transform spawnPos;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SetTrap()
    {
        int index = Random.Range(0, traps.Count);

        traps[index].SetActive(true);
    }

    public void SetPillar()
    {
        int index = Random.Range(0, pillars.Count);

        pillars[index].SetActive(true);
    }

  
}
