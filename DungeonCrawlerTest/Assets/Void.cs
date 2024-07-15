using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Void : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.CompareTag("Player") )
        {
            GameControl.instance.playerControl.Gameover();
          
        }

        if (other.transform.CompareTag("Enemy"))
        {
            other.transform.GetComponent<EnemyBase>().TakeDamage(100);
        }
    }
}
