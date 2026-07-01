using UnityEngine;
using UnityEngine.UI;
using DG;
using DG.Tweening;
using System.Collections;


public class WaitProcessPanel : MonoBehaviour
{
    [SerializeField] Image[] imgCircleC;
    [SerializeField] Image[] imgCircleB;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {

        for (int i = 0; i < imgCircleC.Length; i++)
        {
            Sequence seq = DOTween.Sequence();
            seq.SetAutoKill(false).SetLoops(-1, LoopType.Restart)
                .Join(imgCircleC[i].DOFade(0, 2f).From(1).SetEase(Ease.Linear))
                .Join(imgCircleC[i].transform.DOScale(2.3f, 2f).From(1).SetEase(Ease.Linear))
                .Join(imgCircleB[i].DOFade(0, 2f).From(1).SetEase(Ease.Linear))
                .Join(imgCircleB[i].transform.DOScale(4.7f, 2f).From(1).SetEase(Ease.Linear))
                .AppendInterval(2f);
            /*int cnt = 0;
            while(cnt < 20)
            {
                cnt++;
                yield return null;
            }*/
            seq.Play();
            yield return new WaitForSeconds(0.35f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
