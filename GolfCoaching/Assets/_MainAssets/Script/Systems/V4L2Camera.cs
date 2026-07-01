using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class V4L2Camera : MonoBehaviour
{
    [DllImport("libv4l2cam")] 
    static extern IntPtr OpenCamera(string device, int width, int height, int fps);

    [DllImport("libv4l2cam")] 
    static extern int GetFrame(IntPtr cam, out IntPtr data, out int size);

    [DllImport("libv4l2cam")] 
    static extern void CloseCamera(IntPtr cam);

    public RawImage screen;

    public enum CAMPOSITION { FRONT, SIDE }
    public CAMPOSITION camPosition;

    [SerializeField] private int fps = 30;
    [SerializeField] TextMeshProUGUI txtDebug;
    [SerializeField] int CameraID = 0;

    IntPtr camHandle;

    Texture2D texture;
    byte[] mjpegBuffer;

    bool isInit = false;
    string devicePath;

    int updCount = 0;
    int skipCount = 0;


    private IEnumerator Start()
    {
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;

        // ★ 중요: Ubuntu에서 /dev/video0, /dev/video2 ... 직접 사용
        devicePath = $"/dev/video{CameraID}";

        Debug.Log($"Opening {devicePath}");

        yield return new WaitForSeconds(1);

        int width = 1280;
        int height = 720;

        // --- Open V4L2 Camera ---
        camHandle = OpenCamera(devicePath, width, height, fps);
        if (camHandle == IntPtr.Zero)
        {
            Debug.LogError($"Failed to open {devicePath}");
            yield break;
        }

        texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        screen.texture = texture;

        mjpegBuffer = new byte[width * height * 3]; // 넉넉하게

        isInit = true;
    }


    void Update()
    {
        if (isInit == false) return;

        // ---- 프레임 가져오기 ----
        if (GetFrame(camHandle, out IntPtr framePtr, out int frameSize) == 1)
        {
            // 프레임 정상 수신
            updCount++;

            // MJPEG → RGB 변환 (Unity에서 LoadImage 사용)
            if (mjpegBuffer.Length < frameSize)
                mjpegBuffer = new byte[frameSize];

            Marshal.Copy(framePtr, mjpegBuffer, 0, frameSize);

            texture.LoadImage(mjpegBuffer);
        }
        else
        {
            // 프레임 없음
            skipCount++;
        }

        // ---- 디버그 출력 ----
        if (txtDebug != null && updCount > 0)
        {
            float vidFps = (float)updCount / (updCount + skipCount) * fps;
            txtDebug.text = $"{updCount}/{skipCount}\nVid:{vidFps:F2}";
        }
    }


    public void resetCount()
    {
        updCount = 0;
        skipCount = 0;
    }


    private void OnDestroy()
    {
        if (camHandle != IntPtr.Zero)
            CloseCamera(camHandle);
    }
}
