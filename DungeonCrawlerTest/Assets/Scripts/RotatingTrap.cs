using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RotatingTrap : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
        //rotate randomly
        int index = Random.Range(1, 3);

        switch (index)
        {
            case 1:
                transform.DORotate(new Vector3(0, 360, 0), 2.0f, RotateMode.FastBeyond360).SetLoops(-1, LoopType.Yoyo);
                break;

            case 2:
                transform.DORotate(new Vector3(0, 360, 0), 2.0f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
                break;
        }
    }

}
