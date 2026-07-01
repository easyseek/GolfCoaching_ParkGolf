using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(LayoutElement ))]
public class TMPSizeFitter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmp;
    [SerializeField] private LayoutElement layoutElement;
    
    [SerializeField] private float maxWidth = 755f;
    [SerializeField] private float maxHeight = 105f;
    [SerializeField] private Vector2 padding = Vector2.zero;

    private Vector2 preSize = Vector2.zero;

    private bool isUpdating = false;

    private void Awake()
    {
        if(tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();

        if(layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();
    }

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
            UpdateSize();
            isUpdating = false;
        }
    }

    // Update is called once per frame
    void UpdateSize()
    {
        float oriWidth = tmp.rectTransform.rect.width;

        tmp.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth - padding.x);

        tmp.ForceMeshUpdate();

        Bounds textBounds = tmp.textBounds;

        float newWidth = Mathf.Min(textBounds.size.x + padding.x, maxWidth);
        float newHeight = Mathf.Min(textBounds.size.y + padding.y, maxHeight);
        
        Vector2 newSize = new Vector2(newWidth, newHeight);

        if(Vector2.Distance(newSize, preSize) > 0.1f)
        {
            layoutElement.preferredWidth = newWidth;
            layoutElement.preferredHeight = newHeight;
            preSize = newSize;
        }
        
        tmp.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oriWidth);
    }
}
