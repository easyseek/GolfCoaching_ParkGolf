using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CaptureCardUI : MonoBehaviour
{
    [SerializeField] private RawImage cardImg;

    [SerializeField] private TextMeshProUGUI nameText;

    public Button exitButton;

    private bool isCenter;
    
    public bool IsCenter {
        get { return isCenter; }
        set { isCenter = value; }
    }
    
    public void SetCard(Texture2D texture, string name)
    {
        cardImg.texture = texture;
        nameText.text = name;
    }

    public void AnimateTo(Vector2 pos, Vector3 scale, float duration, bool isCenter)
    {
        exitButton.gameObject.SetActive(isCenter);

        transform.DOLocalMove(pos, duration).SetEase(Ease.OutCubic);
        transform.DOScale(scale, duration).SetEase(Ease.OutCubic);
    }

    public void SetTransform(Vector2 pos, Vector3 scale, bool isCenter)
    {
        transform.localPosition = pos;
        transform.localScale = scale;

        exitButton.gameObject.SetActive(isCenter);
    }
}
