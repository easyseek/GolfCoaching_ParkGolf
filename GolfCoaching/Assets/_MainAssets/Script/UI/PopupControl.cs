using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

public class PopupControl : MonoBehaviour
{
    [Header("TopPanel")]
    [SerializeField] RectTransform m_TopPanel;

    [SerializeField] TextMeshProUGUI m_TopPanelText;

    [Header("Popup")]
    [SerializeField] GameObject m_Popup;

    [SerializeField] TextMeshProUGUI m_PopupText;

    [SerializeField] Button yesButton;
    [SerializeField] Button noButton;

    [SerializeField] Image yesButtonImage;

    private Vector2 topPanelClosedPos;

    private Action yesAction;
    private Action noAction;

    private Tween topPanelTween;

    private void Start()
    {
        topPanelClosedPos = m_TopPanel != null ? m_TopPanel.anchoredPosition : Vector2.zero;
    }

    public void ShowTopPanel(string message)
    {
        if (topPanelTween != null && topPanelTween.IsActive())
            topPanelTween.Kill();

        m_TopPanelText.text = message;

        m_TopPanel.anchoredPosition = topPanelClosedPos;

        topPanelTween = m_TopPanel
            .DOAnchorPosY(0.0f, 0.5f).SetEase(Ease.OutCubic).OnComplete(() => {
                topPanelTween = m_TopPanel.DOAnchorPosY(topPanelClosedPos.y, 0.5f)
                .SetEase(Ease.InCubic).SetDelay(1.0f);
    });
    }
    
    public void ShowPopup(string message, Color yesColor, Action onYes = null, Action onNo = null)
    {
        m_Popup.SetActive(true);

        m_PopupText.text = message;
        yesButtonImage.color = yesColor;

        yesAction = onYes;
        noAction = onNo;

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() => {
            yesAction?.Invoke();
            PopupClose();
        });

        noButton.onClick.AddListener(() => {
            noAction?.Invoke();
            PopupClose();
        });
    }

    public void PopupClose()
    {
        m_Popup.SetActive(false);
    }
}