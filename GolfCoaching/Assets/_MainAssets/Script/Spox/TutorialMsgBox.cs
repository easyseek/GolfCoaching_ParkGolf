using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialMsgBox : MonoBehaviour
{
    [SerializeField] Text txtMsg;
    float initSize = 0.5f;
    Color alpha = Color.white;
    [SerializeField] string audioName;
    IEnumerator Start()
    {
        alpha.a = 0;
        Transform target = GetComponent<Transform>();
        target.transform.localScale = Vector3.one * initSize;
        string msg = txtMsg.text;
        txtMsg.text = string.Empty;

        Sequence seq = DOTween.Sequence();
        seq.SetAutoKill(false);
        seq.Join(target.transform.DOScale(1f, 0.3f).From(initSize).SetEase(Ease.OutBack));
        seq.Join(target.GetComponent<Image>().DOFade(1, 0.3f).From(0));
        seq.Append(txtMsg.DOText(msg, msg.Length * 0.1f));

        yield return new WaitForSeconds(0.1f);

        seq.Play();

        if (string.IsNullOrEmpty(audioName) == false)
        {
            AudioManager.Instance.PlayTutorial(audioName);
        }
    }
}
