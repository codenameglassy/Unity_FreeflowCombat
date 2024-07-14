using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{

    [SerializeField] private GameObject hitVfx;
    [SerializeField] private GameObject activeTargetObject;

    // Start is called before the first frame update
    void Start()
    {
        ActiveTarget(false);
    }

  
    public void SpawnHitVfx(Vector3 Pos_)
    {
        Instantiate(hitVfx, Pos_, Quaternion.identity);
    }

    public void ActiveTarget(bool bool_)
    {
        activeTargetObject.SetActive(bool_);
    }


}
