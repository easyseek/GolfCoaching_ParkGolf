using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Toggle))]
public class ToggleUIAddon : MonoBehaviour
{
    public GameObject m_ExternalObj;
    public GameObject[] m_OnOffObjs;

    public Vector2[] m_OnSize;
    public Vector2[] m_OffSize;

    public TextMeshProUGUI targetText;
    public Image targetImage;

    public Color onTextColor = Color.white;
    public Color offTextColor = Color.gray;

    public Color onImageColor = Color.green;
    public Color offImageColor = Color.red;

    public List<Toggle> otherToggles = new List<Toggle>();

    public bool useSelfToggle = true;

    private Toggle myToggle;
    private bool isEnabled = true;

    void Awake()
    {
        myToggle = GetComponent<Toggle>();
        myToggle.onValueChanged.AddListener(OnSelfToggleChanged);

        foreach (var toggle in otherToggles)
        {
            if (toggle != null)
                toggle.onValueChanged.AddListener(OnExternalToggleChanged);
        }

        if (useSelfToggle)
            UpdateVisualState(myToggle.isOn);
    }

    void OnDestroy()
    {
        myToggle.onValueChanged.RemoveListener(OnSelfToggleChanged);

        foreach (var toggle in otherToggles)
        {
            if (toggle != null)
                toggle.onValueChanged.RemoveListener(OnExternalToggleChanged);
        }
    }

    private void OnSelfToggleChanged(bool isOn)
    {
        if (isEnabled && useSelfToggle) // 외부에 의해 비활성화된 경우는 무시
            UpdateVisualState(isOn);
    }

    private void OnExternalToggleChanged(bool isOn)
    {
        ForceDisable();
    }

    private void ForceDisable()
    {
        isEnabled = false;

        if (targetText != null)
            targetText.color = offTextColor;

        if (targetImage != null)
            targetImage.color = offImageColor;

        myToggle.interactable = false;
    }

    private void UpdateVisualState(bool isOn)
    {
        if (targetText != null)
            targetText.color = isOn ? onTextColor : offTextColor;

        if (targetImage != null)
            targetImage.color = isOn ? onImageColor : offImageColor;

        if (m_OnOffObjs != null)
        {
            if (m_OnOffObjs.Length > 0)
            {
                for (int i = 0; i < m_OnOffObjs.Length; i++)
                {
                    m_OnOffObjs[i].SetActive(isOn);
                }
            }
        }
    }

    public void SetVisualState(bool on)
    {
        isEnabled = on;
        UpdateVisualState(on);
    }

    public bool GetState()
    {
        return isEnabled;
    }
}