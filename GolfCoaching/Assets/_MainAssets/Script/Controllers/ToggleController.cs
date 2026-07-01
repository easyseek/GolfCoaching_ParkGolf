using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ToggleController : MonoBehaviour
{
    [SerializeField] private Toggle m_Toggle = null;
    [SerializeField] private Image m_Background = null;
    [SerializeField] private RectTransform m_Handle = null;
    private Image m_HandleImg = null;

    [SerializeField] private Color m_OnColor;
    [SerializeField] private Color m_OffColor;
    [SerializeField] private Color m_HandleColor;

    [SerializeField] private Vector2 onPosition = Vector2.zero;
    [SerializeField] private Vector2 offPosition = Vector2.zero;

    void Start()
    {
        if(m_Handle != null)
            m_HandleImg = m_Handle.GetComponent<Image>();

        m_Toggle.onValueChanged.AddListener(isOn => UpdateToggle(isOn));
        UpdateToggle(m_Toggle.isOn, true);
    }

    void UpdateToggle(bool isOn, bool instant = false)
    {
        float duration = instant ? 0 : 0.25f;

        m_Background.DOColor(isOn ? m_OnColor : m_OffColor, duration);
        m_HandleImg.DOColor(isOn ? m_HandleColor : Color.white, duration);
        m_Handle.DOAnchorPos(isOn ? onPosition : offPosition, duration).SetEase(Ease.OutQuad);
    }
}
