using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SpikeTrap : MonoBehaviour
{
    public List<GameObject> spikeRows = new List<GameObject>();
    public Vector2 spikeUp, spikeDown;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Enum_Spike());
    }

    IEnumerator Enum_Spike()
    {
        yield return new WaitForSeconds(1f);

        spikeRows[0].transform.DOLocalMoveY(spikeUp.y, 1f).SetEase(Ease.OutBounce).
            OnComplete(() => spikeRows[0].transform.DOLocalMoveY(spikeDown.y, .5f).SetEase(Ease.InBounce));
        yield return new WaitForSeconds(.5f);
        spikeRows[1].transform.DOLocalMoveY(spikeUp.y, 1f).SetEase(Ease.OutBounce).
           OnComplete(() => spikeRows[1].transform.DOLocalMoveY(spikeDown.y, .5f).SetEase(Ease.InBounce));
        yield return new WaitForSeconds(.5f);
        spikeRows[2].transform.DOLocalMoveY(spikeUp.y, 1f).SetEase(Ease.OutBounce).
           OnComplete(() => spikeRows[2].transform.DOLocalMoveY(spikeDown.y, .5f).SetEase(Ease.InBounce));
        yield return new WaitForSeconds(.5f);
        spikeRows[3].transform.DOLocalMoveY(spikeUp.y, 1f).SetEase(Ease.OutBounce).
           OnComplete(() => spikeRows[3].transform.DOLocalMoveY(spikeDown.y, .5f).SetEase(Ease.InBounce));
        yield return new WaitForSeconds(.5f);
        spikeRows[4].transform.DOLocalMoveY(spikeUp.y, 1f).SetEase(Ease.OutBounce).
           OnComplete(() => spikeRows[4].transform.DOLocalMoveY(spikeDown.y, .5f).SetEase(Ease.InBounce));

        yield return new WaitForSeconds(3f);
        StartCoroutine(Enum_Spike());
    }

   
}
