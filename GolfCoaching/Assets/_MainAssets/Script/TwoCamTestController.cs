using UnityEngine;
using UnityEngine.UI;

public class TwoCamTestController : MonoBehaviour
{
    //[SerializeField] Transform RawImage_Top;
    //[SerializeField] Transform RawImage_Bottom;

    [SerializeField] RawImage RawImage_Top;
    [SerializeField] RawImage RawImage_Bottom;
    [SerializeField] RenderTexture RenderTexture_Front;
    [SerializeField] RenderTexture RenderTexture_Side;



    bool _isFrontTop = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _isFrontTop = true;
    }

    public void OnClick_SwapViwerTopBottom()
    {
        _isFrontTop = !_isFrontTop;

        RawImage_Top.texture = _isFrontTop ? RenderTexture_Front : RenderTexture_Side;
        RawImage_Bottom.texture = _isFrontTop ? RenderTexture_Side : RenderTexture_Front;

        RawImage_Top.transform.localScale = _isFrontTop ? new Vector3(-1, 1, 1) : Vector3.one;
        RawImage_Bottom.transform.localScale = _isFrontTop ? Vector3.one : new Vector3(-1, 1, 1); 
    }
}
