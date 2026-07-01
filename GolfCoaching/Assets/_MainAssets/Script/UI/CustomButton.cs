using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ToggleDisplayMode
{
    Object,
    Color
}

public class CustomButton : Button
{
    [Header("Toggle Mode")]
    [SerializeField] private bool toggleMode = false;
    [SerializeField] private ToggleDisplayMode toggleDisplayMode = ToggleDisplayMode.Object;

    [Header("Use Object")]
    [SerializeField] private GameObject frameImage;

    [Header("Use Color")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color pressedColor = Color.gray;

    [Header("Any Mode (Optional)")]
    [SerializeField] private Graphic targetText;

    private bool isToggled = false;

    public bool IsToggled {
        get { return isToggled; }
        set {
            if(toggleMode)
            {
                isToggled = value;
                ApplyState(isToggled);
            }
        }
    }

    public GameObject FrameImage {
        get { return frameImage; }
        set { frameImage = value; }
    }

    public bool ToggleMode {
        get { return toggleMode; }
        set { toggleMode = value; }
    }

    public ToggleDisplayMode ToggleDisplayMode {
        get { return toggleDisplayMode; }
        set { ToggleDisplayMode = value; }
    }

    public Graphic TargetGraphic {
        get { return targetGraphic; }
        set { targetGraphic = value; }
    }

    public Graphic TargetText {
        get { return targetText; }
        set { targetText = value; }
    }

    public Color NormalColor {
        get { return normalColor; }
        set {  normalColor = value; }
    }

    public Color PressedColor {
        get { return pressedColor; }
        set {  pressedColor = value; }
    }


    protected override void Start()
    {
        base.Start();

        if(toggleDisplayMode == ToggleDisplayMode.Color && targetGraphic == null)
        {
            targetGraphic = base.targetGraphic;
        }

        ApplyState(toggleMode ? isToggled : false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (!toggleMode)
        {
            ApplyState(true);
        }
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (!toggleMode)
        {
            ApplyState(false);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        base.OnPointerClick(eventData);

        if(toggleMode)
        {
            isToggled = !isToggled;
            ApplyState(isToggled);
        }
    }

    private void ApplyState(bool active)
    {
        if(toggleDisplayMode == ToggleDisplayMode.Object)
        {
            if (FrameImage != null)
                frameImage.SetActive(active);
        }
        else
        {
            if (targetGraphic != null)
                targetGraphic.color = active ? pressedColor : normalColor;
        }

        if (targetText != null)
            targetText.color = active ? pressedColor : normalColor;
    }

    public void ForceClick()
    {
        if(toggleMode)
        {
            isToggled = !isToggled;
            ApplyState(isToggled);
        }
        else
        {
            ApplyState(true);
            ApplyState(false);
        }

        onClick.Invoke();
    }

    public void SetState(bool active)
    {
        if (toggleMode)
            isToggled = active;
        ApplyState(active);
    }
}