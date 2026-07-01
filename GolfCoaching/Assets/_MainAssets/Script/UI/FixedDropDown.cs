using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class FixedDropDown : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public float fixedHeight = 300f;
    public string defaultCaption = "선택";

    private bool isResizing = false;

    void Start()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        StartCoroutine(SetCaptionText());

        dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    void Update()
    {
        if (dropdown != null && dropdown.IsExpanded && !isResizing)
        {
            isResizing = true;
            StartCoroutine(ResizeDropdownList());
        }

        if (dropdown != null && !dropdown.IsExpanded && isResizing)
        {
            isResizing = false;
        }
    }

    IEnumerator SetCaptionText()
    {
        yield return null;

        if (dropdown.captionText != null)
        {
            dropdown.captionText.text = defaultCaption;
        }
    }

    IEnumerator ResizeDropdownList()
    {
        yield return null;

        Transform parent = dropdown.template.parent;
        if (parent == null) yield break;

        Transform list = parent.Find("Dropdown List");
        if (list == null) yield break;

        RectTransform rt = list.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, fixedHeight);
        }
    }

    void OnDropdownValueChanged(int index)
    {
        dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
    }
}
