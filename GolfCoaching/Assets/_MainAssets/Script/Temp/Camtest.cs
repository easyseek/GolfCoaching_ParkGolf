using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Camtest : MonoBehaviour
{
    public RawImage screen;
    public enum CAMPOSITION
    {
        FRONT,
        SIDE
    }
    public CAMPOSITION camPosition;
    [SerializeField] private int fps = 30;
    [HideInInspector] public WebCamTexture webCamTexture;
     [SerializeField] TextMeshProUGUI txtDebug;
     [SerializeField] int CameraID;

     bool isInit =false;

    private IEnumerator Start()
    {
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;

        yield return new WaitForSeconds(5f);

        if (WebCamTexture.devices.Length == 0)
        {
            throw new System.Exception("Web Camera devices are not found");
        }
        Debug.Log(string.Join("\r\n", WebCamTexture.devices));

        var webCamDevice = WebCamTexture.devices[CameraID];
        webCamTexture = new WebCamTexture(webCamDevice.name, 1280, 720, fps);
        webCamTexture.Play();

        // NOTE: On macOS, the contents of webCamTexture may not be readable immediately, so wait until it is readable
        yield return new WaitUntil(() => webCamTexture.width > 16);
        
        //screen.rectTransform.sizeDelta = new Vector2(width, height);
        screen.texture = webCamTexture;
        isInit = true;
    }

    // Update is called once per frame
    
    int updCuunt = 0;
    int skipCount = 0;
    void Update()
    {
        if(isInit == false)
            return;

        if(txtDebug != null)
        {
            if(webCamTexture.didUpdateThisFrame)
                updCuunt++;
            else    
                skipCount++;

            txtDebug.text = $"{updCuunt}/{skipCount}\r\nVid:{30f / (1f+((float)skipCount/updCuunt)):F2}";

            if(skipCount > 300)
                resetCount();
        }
    }

    public void resetCount()
    {
        updCuunt = 0;
        skipCount = 0;
    }

}
