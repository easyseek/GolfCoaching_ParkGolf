using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FilterItemController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmp;
    [SerializeField] private LayoutElement layoutElement;

    [SerializeField] private float padding = 0.0f;

    private bool isUpdating = false;

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnTextChanged(Object obj)
    {
        if (isUpdating)
            return;

        if (obj == tmp)
        {
            isUpdating = true;
            UpdateTmpSize();
            isUpdating = false;
        }
    }

    public void UpdateStringAndSize(string str)
    {
        tmp.text = str;
    }

    private void UpdateTmpSize()
    {
        tmp.ForceMeshUpdate();

        float tmpWidth = tmp.rectTransform.sizeDelta.x;
        float tmpPosX = tmp.rectTransform.anchoredPosition.x;

        layoutElement.preferredWidth = tmpWidth + tmpPosX + padding;
    }
}
