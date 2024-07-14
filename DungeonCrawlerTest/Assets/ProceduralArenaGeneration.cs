using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralArenaGeneration : MonoBehaviour
{
    public List<GameObject> blocks = new List<GameObject>();
    public List<Blocks> survivedBlocks = new List<Blocks>();
    // Start is called before the first frame update
    void Start()
    {
        RandomArena();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomArena();
        }
    }

    private void RandomArena()
    {
        //set block
        for (int i = 0; i < blocks.Count; i++)
        {
            blocks[i].SetActive(true);
        }

        int index1 = Random.Range(0, blocks.Count);
        int index2 = Random.Range(0, blocks.Count);
        int index3 = Random.Range(0, blocks.Count);

        for (int i = 0; i < blocks.Count; i++)
        {
            if(i == index1 || i == index2 || i == index3)
            {
                blocks[i].SetActive(false);
            }
            else
            {
                survivedBlocks.Add(blocks[i].GetComponent<Blocks>());
            }
        }

        //set trap
        int index4 = Random.Range(0, survivedBlocks.Count);
        int index4_ = Random.Range(0, survivedBlocks.Count);
        //int index4__ = Random.Range(0, survivedBlocks.Count);

        for (int i = 0; i < blocks.Count; i++)
        {
            if(i == index4 || i == index4_)
            {
                survivedBlocks[i].SetTrap();
                survivedBlocks.Remove(survivedBlocks[i]);
            }
        }


        //set pillar
        int index5, index6;
        index5 = Random.Range(0, survivedBlocks.Count);
        index6 = Random.Range(0, survivedBlocks.Count);

        for (int i = 0; i < blocks.Count; i++)
        {
            if (i == index5 || i == index6)
            {
                survivedBlocks[i].SetPillar();
            }
        }
    }
}