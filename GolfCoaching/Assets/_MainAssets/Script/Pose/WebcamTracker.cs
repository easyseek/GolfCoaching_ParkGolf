using Mediapipe;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System;
using Mediapipe.Unity.CoordinateSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Stopwatch = System.Diagnostics.Stopwatch;
using Enums;
using TMPro;
using System.Runtime.InteropServices;
using Unity.VisualScripting;

public class WebcamTracker : MonoBehaviour
{
    [DllImport("libv4l2cam")] 
    static extern IntPtr OpenCamera(string device, int width, int height, int fps);

    [DllImport("libv4l2cam")] 
    static extern int GetFrame(IntPtr cam, out IntPtr data, out int size);

    [DllImport("libv4l2cam")] 
    static extern void CloseCamera(IntPtr cam);

    string devicePath;
    IntPtr camHandle;
    Texture2D texture;
    byte[] mjpegBuffer;

    public RawImage screen;
    public enum CAMPOSITION
    {
        FRONT,
        SIDE
    }
    public CAMPOSITION camPosition;
    [SerializeField] private int fps = 30;
    //[HideInInspector] public WebCamTexture webCamTexture;

    [HideInInspector] public bool isInit = false;


    public bool Track = false;
    public Landmark2D[] Landmark;// = new Landmark2D[33];
    public Landmark3D[] WorldLandmark;// = new Landmark3D[33];

    //주요 랜드마크 필터
    private KalmanFilter[][] kalmanFilters;
    const int  kalmanFiltersCount = 11;
    public FilteredPosition KalmanPositions;

    public float visibilityAvg;

    //[SerializeField] Toggle ToggleUseFilter;
    //public bool useFilter = true;

    public bool isTrackReady = false;

    public bool isImageTrack = false;

    Stopwatch stopwatch;
    PoseLandmarker poseLandmarker;
    Mediapipe.Unity.Experimental.TextureFrame textureFrame;
    Mediapipe.Unity.Experimental.TextureFrame sharedFrame;
    UnityEngine.Rect screenRect;

    //[SerializeField] TextMeshProUGUI txtDebug;

    private IEnumerator Start()
    {
        InitializeKalmanFilters();        
        KalmanPositions = new FilteredPosition();

        //if (ToggleUseFilter != null)
        //    ToggleUseFilter.onValueChanged.AddListener(OnValueChanged_UseFilter);

        yield return null;

        // 중요: Ubuntu에서 /dev/video0, /dev/video2 ... 직접 사용
        devicePath = $"/dev/video{(camPosition == CAMPOSITION.FRONT ? Utillity.Instance.frontCameraID : Utillity.Instance.sideCameraID)}";

/* --- WebCamTexture ---
        if (WebCamTexture.devices.Length == 0)
        {
            throw new System.Exception("Web Camera devices are not found");
        }
        var webCamDevice = WebCamTexture.devices[camPosition == CAMPOSITION.FRONT ? Utillity.Instance.frontCameraID : Utillity.Instance.sideCameraID];
        webCamTexture = new WebCamTexture(webCamDevice.name, Utillity.Instance.resolution_width, Utillity.Instance.resolution_height, fps);
        webCamTexture.Play();

        // NOTE: On macOS, the contents of webCamTexture may not be readable immediately, so wait until it is readable
        yield return new WaitUntil(() => webCamTexture.width > 16);
        
        //screen.rectTransform.sizeDelta = new Vector2(width, height);
        screen.texture = webCamTexture;
*/

        // --- Open V4L2 Camera ---
        camHandle = OpenCamera(devicePath, Utillity.Instance.resolution_width, Utillity.Instance.resolution_height, fps);
        if (camHandle == IntPtr.Zero)
        {
            Debug.LogError($"Failed to open {devicePath}");
            yield break;
        }

        texture = new Texture2D(Utillity.Instance.resolution_width, Utillity.Instance.resolution_height, TextureFormat.RGB24, false);
        screen.texture = texture;

        mjpegBuffer = new byte[Utillity.Instance.resolution_width * Utillity.Instance.resolution_height * 3]; 

        if (isImageTrack)
        {
            InitRunnerIfNeeded(Utillity.Instance.resolution_width, Utillity.Instance.resolution_height);
            StartCoroutine(CoTrackPose_Webcam());
        }

        /*
        if (Track)
        {
            StartCoroutine(CoTrackPose());
        }
        */

        isInit = true;
    }

    void Update()
    {
        if (isInit == false) return;

        // ---- 프레임 가져오기 ----
        if (GetFrame(camHandle, out IntPtr framePtr, out int frameSize) == 1)
        {
            // MJPEG → RGB 변환 (Unity에서 LoadImage 사용)
            if (mjpegBuffer.Length < frameSize)
                mjpegBuffer = new byte[frameSize];

            Marshal.Copy(framePtr, mjpegBuffer, 0, frameSize);

            texture.LoadImage(mjpegBuffer);
        }
    }

/*
    int updCount = 0;
    int skipCount = 0;
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
*/

    /*
    public void SetTrack(bool isOn)
    {
        if (Track == false && isOn == true)
        {
            Track = isOn;
            StartCoroutine(CoTrackPose());
        }
        Track = isOn;
    }*/

    private void InitializeKalmanFilters()
    {
        kalmanFilters = new KalmanFilter[kalmanFiltersCount][];

        for (int i = 0; i < kalmanFilters.Length; i++)
        {
            kalmanFilters[i] = new KalmanFilter[2];
            kalmanFilters[i][0] = new KalmanFilter();
            kalmanFilters[i][1] = new KalmanFilter();
        }
    }

    public void SetTrackPose()
    {
        TextAsset modelAsset = Resources.Load<TextAsset>("pose_landmarker_full");

        var options = new PoseLandmarkerOptions(
          baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
            //Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
            Utillity.Instance.mediapipeMode.Equals("GPU") ? Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU : Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
            modelAssetBuffer: modelAsset.bytes
             ),
          runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO
        );

        poseLandmarker = PoseLandmarker.CreateFromOptions(options);

        stopwatch = new Stopwatch();
        stopwatch.Start();

        //textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32);
        textureFrame = new Mediapipe.Unity.Experimental.TextureFrame(Utillity.Instance.resolution_width, Utillity.Instance.resolution_height, TextureFormat.RGBA32);
        screenRect = screen.rectTransform.rect;
        
        if (Landmark == null)
        {
            Landmark = new Landmark2D[33];
            WorldLandmark = new Landmark3D[33];
            for (int i = 0; i < 33; i++)
                Landmark[i] = new Landmark2D();
            for (int i = 0; i < 33; i++)
                WorldLandmark[i] = new Landmark3D();
        }

        isTrackReady = true;
        Track = true;
    }

    public void ResetTrackPose()
    {
        Landmark = null;
        WorldLandmark = null;
        visibilityAvg = 0;

        stopwatch = null;
        poseLandmarker = null;
        textureFrame = null;

        isTrackReady = false;
        Track = false;
    }
    
    public void GetTrackPose()
    {
        if (!isInit)
            return;

        if (!isTrackReady)
            return;

        if (poseLandmarker == null || textureFrame == null || stopwatch == null)
            return;

        float avg = 0;

        //textureFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
        textureFrame.ReadTextureOnCPU(texture, flipHorizontally: false, flipVertically: true);
        using (var image = textureFrame.BuildCPUImage())
        {
            var result = poseLandmarker.DetectForVideo(image, stopwatch.ElapsedMilliseconds);
            if (result.poseLandmarks?.Count > 0)
            {
                for (int i = 0; i < 33; i++)
                {                    
                    var lm = result.poseLandmarks[0].landmarks;
                    Landmark[i].visibility = (float)lm[i].visibility;
                    Landmark[i].positionOrg = new Vector2(lm[i].x, lm[i].y);
                    Landmark[i].position = screenRect.GetPoint(lm[i]);

                    var wlm = result.poseWorldLandmarks[0].landmarks;
                    WorldLandmark[i].visibility = (float)wlm[i].visibility;
                    WorldLandmark[i].position = new Vector3(-wlm[i].y, -wlm[i].x, wlm[i].z);

                    avg += Landmark[i].visibility;
                }

                visibilityAvg = avg / 33f;
                avg = 0;

                SetKalmanPosition();
            }
            else
            {
                for (int i = 0; i < 33; i++)
                    Landmark[i].visibility = 0;
                for (int i = 0; i < 33; i++)
                    WorldLandmark[i].visibility = 0;
                visibilityAvg = 0;
            }
        }
    }

    public int GetHandDir()
    {
        try
        {
            Vector2 handVector = Vector2.zero;

            if (Landmark[14].position.x > Landmark[13].position.x)
            {
                handVector = Landmark[16].position;
            }
            else
            {
                handVector = Landmark[15].position;
            }

            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            Vector2 dir = handVector - shoulderVector;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += 180f;

            return (int)angle;
        }
        catch 
        { 
            return -1; 
        }
    }

    public int GetHandDistance()
    {
        int value = 0;

        try
        {
            value = (int)Vector2.Distance(Landmark[15].position, Landmark[16].position);

            value = (int)(value * Utillity.Instance.frontPixelDistanceRate);
        }
        catch 
        { 
            return -1; 
        }

        return value;
    }

    private void InitRunnerIfNeeded(int w, int h)
    {
        if (poseLandmarker == null)
        {
            Debug.Log($"THIS 3");
            TextAsset modelAsset = Resources.Load<TextAsset>("pose_landmarker_full");
            var options = new PoseLandmarkerOptions(
                baseOptions: new Mediapipe.Tasks.Core.BaseOptions(
                    Utillity.Instance.mediapipeMode.Equals("GPU") ? Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU : Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: modelAsset.bytes
                ),
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO
            );
            poseLandmarker = PoseLandmarker.CreateFromOptions(options);

            stopwatch = new Stopwatch();
            stopwatch.Start();
        }

        if (sharedFrame == null || sharedFrame.width != w || sharedFrame.height != h)
        {
            sharedFrame?.Dispose();
            sharedFrame = new Mediapipe.Unity.Experimental.TextureFrame(w, h, TextureFormat.RGBA32);
        }

        if (Landmark == null || Landmark.Length != 33)
            Landmark = new Landmark2D[33];

        if (screen != null)
            screenRect = screen.rectTransform.rect;
        else
            screenRect = new UnityEngine.Rect(0, 0, w, h);
    }

    private IEnumerator CoTrackPose_Webcam()
    {
        var waitForEndOfFrame = new WaitForEndOfFrame();

        float avg = 0;

        while (true)
        {
            InitRunnerIfNeeded(Utillity.Instance.resolution_width, Utillity.Instance.resolution_height);

            //sharedFrame.ReadTextureOnCPU(webCamTexture, flipHorizontally: false, flipVertically: true);
            sharedFrame.ReadTextureOnCPU(texture, flipHorizontally: false, flipVertically: true);

            using (var image = sharedFrame.BuildCPUImage())
            {
                var result = poseLandmarker.DetectForVideo(image, stopwatch.ElapsedMilliseconds);
                if (result.poseLandmarks?.Count > 0)
                {
                    if (Landmark == null) Landmark = new Landmark2D[33];

                    avg = 0f;
                    for (int i = 0; i < 33; i++)
                    {
                        if (Landmark[i] == null) Landmark[i] = new Landmark2D();
                        Landmark[i].visibility = (float)result.poseLandmarks[0].landmarks[i].visibility;

                        Vector2 pos = screenRect.GetPoint(result.poseLandmarks[0].landmarks[i]); 
                        
                        Landmark[i].position = pos;
                        avg += Landmark[i].visibility;
                    }

                    visibilityAvg = avg / 33f;
                }
                else
                {
                    Landmark = null;
                    visibilityAvg = 0f;
                }
            }

            yield return waitForEndOfFrame;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("WebcamTracker.OnDestroy()");
        /*if (webCamTexture != null)
        {
            webCamTexture.Stop();
        }*/
        if (camHandle != IntPtr.Zero)
            CloseCamera(camHandle);

        if (sharedFrame != null)
        {
            sharedFrame.Dispose();
        }

        if (poseLandmarker != null)
        {
            poseLandmarker.Close();
        }
    }

    /*
    public void OnValueChanged_UseFilter(bool use)
    {
        useFilter = use;
    }*/



    //=========================================================
    // 필터링 포지션
    //=========================================================
    void SetKalmanPosition()
    {
        KalmanPositions.Nose = UpdateFilter(0, 0);
        KalmanPositions.LeftShoulder = UpdateFilter(1, 11);
        KalmanPositions.RightShoulder = UpdateFilter(2, 12);
        KalmanPositions.CenterShoulder = (KalmanPositions.LeftShoulder + KalmanPositions.RightShoulder) / 2f;
        KalmanPositions.LeftPelvis = UpdateFilter(3, 23);
        KalmanPositions.RightPelvis = UpdateFilter(4, 24);
        KalmanPositions.CenterPelvis = (KalmanPositions.LeftPelvis + KalmanPositions.RightPelvis) / 2f;
        KalmanPositions.CenterSpine = (KalmanPositions.CenterShoulder + KalmanPositions.CenterPelvis) / 2f;
        KalmanPositions.LeftHand = UpdateFilter(5, 15);
        KalmanPositions.RightHand = UpdateFilter(6, 16);
        KalmanPositions.LeftFoot = UpdateFilter(7, 27);
        KalmanPositions.RightFoot = UpdateFilter(8, 28);
        KalmanPositions.LeftKnee = UpdateFilter(9, 25);
        KalmanPositions.RightKnee = UpdateFilter(10, 26);
    }

    Vector2 UpdateFilter(int kalIndex, int LandmarkIndex)
    {
        if (Landmark == null || Landmark.Length < 1 || Landmark[LandmarkIndex] == null)
            return Vector2.zero;

        return new Vector2(kalmanFilters[kalIndex][0].Update(Landmark[LandmarkIndex].position.x),
            kalmanFilters[kalIndex][1].Update(Landmark[LandmarkIndex].position.y));
    }
}

public class Landmark2D
{
    public float x;
    public float y;
    public float visibility;

    public Vector2 position
    {
        set { x = value.x; y = value.y; }
        get
        {
            return new Vector2(x, y);
        }
    }
    public Vector2 positionOrg;
}

public class Landmark3D
{
    public float x;
    public float y;
    public float z;
    public float visibility;

    public Vector3 position
    {
        set { x = value.x; y = value.y; z = value.z; }
        get
        {
            return new Vector3(x, y, z);
        }
    }


}

public class FilteredPosition
{
    public Vector2 Nose;
    public Vector2 LeftShoulder;
    public Vector2 RightShoulder;
    public Vector2 CenterShoulder;
    public Vector2 LeftPelvis;
    public Vector2 RightPelvis;
    public Vector2 CenterPelvis;
    public Vector2 CenterSpine;
    public Vector2 LeftHand;
    public Vector2 RightHand;
    public Vector2 LeftFoot;
    public Vector2 RightFoot;
    public Vector2 LeftKnee;
    public Vector2 RightKnee;

    public Vector2 GetIndexPosition(int index)
    {
        if (index == 0)
            return Nose;
        else if (index == 11)
            return LeftShoulder;
        else if (index == 12)
            return RightShoulder;
        else if (index == 23)
            return LeftPelvis;
        else if (index == 24)
            return RightPelvis;
        else if (index == 15)
            return LeftHand;
        else if (index == 16)
            return RightHand;
        else if (index == 27)
            return LeftFoot;
        else if (index == 28)
            return RightFoot;
        else if (index == 25)
            return LeftKnee;
        else if (index == 26)
            return RightKnee;
        else
            return Vector2.zero;

    }
}
