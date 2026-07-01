using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class VKInputOpener : MonoBehaviour, ISelectHandler, IPointerDownHandler
{
    [SerializeField] private TMP_InputField input;
    [SerializeField] private bool openOnPointerDown = true;

    private void Reset()
    {
        input = GetComponent<TMP_InputField>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (input == null)
        {
            input = GetComponent<TMP_InputField>();
        }

        if (VirtualKeyboard.Instance != null)
        {
            VirtualKeyboard.Instance.ShowFor(input);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!openOnPointerDown)
        {
            return;
        }

        if (input == null)
        {
            input = GetComponent<TMP_InputField>();
        }

        if (VirtualKeyboard.Instance != null)
        {
            VirtualKeyboard.Instance.ShowFor(input);
        }
    }
}
