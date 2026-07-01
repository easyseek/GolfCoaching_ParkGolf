using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialTouch : MonoBehaviour
{
    [SerializeField] Image target;
    [SerializeField] Image finger;
    Color alpha = Color.white;

    void Start()
    {
        alpha.a = 0;
        Sequence seq = DOTween.Sequence();
        seq.SetAutoKill(false);
        seq.Join(finger.transform.DOLocalMoveY(0, 0.25f).From(-30f));

        seq.Append(target.DOFade(0f, 0.4f).From(1f));
        seq.Join(target.transform.DOScale(2f, 0.4f).From(0f).SetEase(Ease.OutBack));

        seq.Join(finger.transform.DOLocalMoveY(-30f, 0.4f).From(0f));
        seq.OnStart(() => { target.color = alpha; });


        //while (true)
        //{
            seq.Restart();

        //    yield return new WaitForSeconds(2.5f);

        //}

    }
}