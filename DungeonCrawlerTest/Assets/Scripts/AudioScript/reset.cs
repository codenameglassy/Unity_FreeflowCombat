using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class reset : MonoBehaviour
{
    // Start is called before the first frame update
    private void Awake()
    {
        FindObjectOfType<audiosettings>().Reset_();
        Time.timeScale = 1;
    }

  
  



}
