using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Pipes;
using System.Threading;
using System.IO;
using UnityEngine.UI;
using System;
using Newtonsoft.Json;
using TMPro;
using System.Linq;
using UnityEngine.UI.Extensions;
using UnityEngine.UIElements;

public class webcamclient : MonoBehaviour
{
    //bool CAMSWAP = false;

    //CAM IMAGE
    private NamedPipeClientStream pipeClient1;
    private NamedPipeClientStream pipeClient2;
    private Thread receiveThread1;
    private Thread receiveThread2;
    private bool isRunning = true;
    private byte[] imageData1 = new byte[9999999];
    private byte[] imageData2 = new byte[9999999];
    private bool updateTexture1 = false;
    private bool updateTexture2 = false;
    private ManualResetEvent stopEvent = new ManualResetEvent(false);
    private object lockObject = new object();
    private volatile bool shouldStop = false;

    public Texture2D texture1;
    public Texture2D texture2;

    public RawImage rawImage1;  //프론트
    public RawImage rawImage2;  //측면

    [SerializeField] TMPro.TextMeshProUGUI FrontName;
    [SerializeField] TMPro.TextMeshProUGUI SideName;

    //2D LANDMARK
    private Thread pipe1ClientThread;
    private Thread pipe2ClientThread;
    public string PIPE1_NAME = "landmark_pipe1";
    public string PIPE2_NAME = "landmark_pipe2";
    [HideInInspector] public Dictionary<string, Landmark2D> poseData1 = new Dictionary<string, Landmark2D>();
    [HideInInspector] public Dictionary<string, Landmark2D> poseData2 = new Dictionary<string, Landmark2D>();
    private float displayWidth;
    private float displayHeight;
    [SerializeField] RectTransform poseHead_Left;    
    [SerializeField] RectTransform[] poseNode_Left;
    [SerializeField] UILineRenderer[] poseConnect_Left;
    [SerializeField] RectTransform poseHead_Right;
    [SerializeField] RectTransform[] poseNode_Right;
    [SerializeField] UILineRenderer[] poseConnect_Right;
    private const float originalWidth = 720f;
    private const float originalHeight = 1280f;
    float scaleX;
    float scaleY;
    readonly int[] POSE_LANDMARK = {16, 14, 12, 11, 13, 15, 24, 23, 26, 25, 28, 27 };
    readonly int[,] POSE_CONNECTION = { {11, 13}, {13, 15}, {11, 23}, {23, 25}, {25, 27},
                                        {11, 12}, {12, 24}, {23, 24}, {12, 14}, {14, 16},
                                        {24, 26}, {26, 28} };
    float visRate = 0;
    public bool drawSkeleton = false;
    [SerializeField] float lineThickness = 5f;

    private KalmanFilter[][] kalmanFilters1 = new KalmanFilter[33][];
    private KalmanFilter[][] kalmanFilters2 = new KalmanFilter[33][];

    float calSIdeCamRate;
    float calSideSkelRate;

    private void InitializeKalmanFilters()
    {
        for (int i = 0; i < 33; i++)
        {
            kalmanFilters1[i] = new KalmanFilter[2];
            kalmanFilters1[i][0] = new KalmanFilter();
            kalmanFilters1[i][1] = new KalmanFilter();

            kalmanFilters2[i] = new KalmanFilter[2];
            kalmanFilters2[i][0] = new KalmanFilter();
            kalmanFilters2[i][1] = new KalmanFilter();
        }
    }

    [SerializeField] TextMeshProUGUI txtDebug;

    public float visibilityFront;
    public float visibilitySide;


    public class Landmark2D
    {
        public float x;
        public float y;
        public float visibility;

        public Vector2 Position;
        /*{
            get
            {
                return new Vector2(x, y);
            }
        }*/
    }

    void Start()
    {        
        InitializeKalmanFilters();

        //CAMSWAP = PlayerPrefs.GetInt("CAMSWAP", 0) == 0 ? false : true;        

        texture1 = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        texture2 = new Texture2D(1920, 1080, TextureFormat.RGB24, false);

        //측면 확대비율 
        float k = Mathf.Pow(Utillity.Instance.TwoCamDIsRate, -0.4965f);
        if (Utillity.Instance.TwoCamDIsRate < 1f) k = 1f;                      // 1 미만이면 그대로
        calSIdeCamRate = Utillity.Instance.TwoCamDIsRate * k;        
        rawImage2.transform.localScale = new Vector3(calSIdeCamRate, calSIdeCamRate, 1);
        calSideSkelRate = 1f / calSIdeCamRate;

        Debug.Log($"Utillity.Instance.TwoCamDIsRate:{Utillity.Instance.TwoCamDIsRate}" +
            $"\r\ncalSIdeCamRate:{calSIdeCamRate}" +
            $"\r\ncalSideSkelRate:{calSideSkelRate}");

        //string pName1 = CAMSWAP ? "webcam_pipe2" : "webcam_pipe1";
        //string pName2 = CAMSWAP ? "webcam_pipe1" : "webcam_pipe2";
        string pName1 = Utillity.Instance.frontCameraID == 0 ? "webcam_pipe1" : "webcam_pipe2";
        string pName2 = Utillity.Instance.sideCameraID == 0 ? "webcam_pipe1" : "webcam_pipe2";

        //string PIPE1_NAME = CAMSWAP ? "landmark_pipe2" : "landmark_pipe1";
        //string PIPE2_NAME = CAMSWAP ? "landmark_pipe1" : "landmark_pipe2";
        string PIPE1_NAME = Utillity.Instance.frontCameraID == 0 ? "landmark_pipe1" : "landmark_pipe2";
        string PIPE2_NAME = Utillity.Instance.sideCameraID == 0 ? "landmark_pipe1" : "landmark_pipe2";

        if (FrontName != null)
            FrontName.text = pName1;
        if (SideName != null)
            SideName.text = pName2;

        pipeClient1 = new NamedPipeClientStream(".", pName1, PipeDirection.In);
        pipeClient2 = new NamedPipeClientStream(".", pName2, PipeDirection.In);

        receiveThread1 = new Thread(ReceiveImage1);
        receiveThread2 = new Thread(ReceiveImage2);

        receiveThread1.Start();
        receiveThread2.Start();

        displayWidth = rawImage1.rectTransform.rect.width;
        displayHeight = rawImage1.rectTransform.rect.height;

        scaleX = displayWidth / originalWidth;
        scaleY = displayHeight / originalHeight;

        StartPipeClient();

    }

    void ReceiveImage1()
    {
        Debug.Log("ReceiveImage1 쓰레드가 시작되었습니다.");
        try
        {
            pipeClient1.Connect();
            Debug.Log("webcamclient.cs - Line 46\n1번 웹캠 서버에 연결되었습니다.");
            
            while (!shouldStop)
            {
                if (pipeClient1.IsConnected)
                {
                    byte[] buffer = new byte[9999999];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int bytesRead;
                        int totalBytesRead = 0;
                        while ((bytesRead = pipeClient1.Read(buffer, 0, buffer.Length)) > 0 && pipeClient1.IsConnected)
                        {
                            ms.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (bytesRead < buffer.Length) break;
                        }
                        lock (lockObject)
                        {
                            imageData1 = ms.ToArray();
                            updateTexture1 = true;
                        }
                        // Debug.Log($"웹캠 1번: {totalBytesRead} 바이트 수신");
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch (System.Threading.ThreadAbortException)
        {
            Debug.Log("ReceiveImage1 쓰레드가 중단되었습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ReceiveImage1: {e.Message}");
        }
        finally
        {
            pipeClient1?.Close();
            Debug.Log("ReceiveImage1 쓰레드가 종료되었습니다.");
        }
    }

    void ReceiveImage2()
    {
        Debug.Log("ReceiveImage2 쓰레드가 시작되었습니다.");
        try
        {
            pipeClient2.Connect();
            UnityEngine.Debug.Log("webcamclient.cs - Line 78\n2번 웹캠 서버에 연결되었습니다.");
            while (!shouldStop)
            {
                if (pipeClient2.IsConnected)
                {
                    byte[] buffer = new byte[9999999];
                    using (MemoryStream ms = new MemoryStream())
                    {
                        int bytesRead;
                        int totalBytesRead = 0;
                        while ((bytesRead = pipeClient2.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;
                            if (bytesRead < buffer.Length) break;
                        }
                        lock (lockObject)
                        {
                            imageData2 = ms.ToArray();
                            updateTexture2 = true;
                        }
                        // Debug.Log($"웹캠 2번: {totalBytesRead} 바이트 수신");
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }
        catch (System.Threading.ThreadAbortException)
        {
            Debug.Log("ReceiveImage2 쓰레드가 중단되었습니다.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ReceiveImage2: {e.Message}");
        }
        finally
        {
            pipeClient2?.Close();
            Debug.Log("ReceiveImage2 쓰레드가 종료되었습니다.");
        }
    }

    private void StartPipeClient()
    {
        pipe1ClientThread = new Thread(Pipe1ClientThread);
        pipe1ClientThread.Start();

        pipe2ClientThread = new Thread(Pipe2ClientThread);
        pipe2ClientThread.Start();

        StartCoroutine(CoDrawSkeleton());
    }

    private void Pipe1ClientThread()
    {
        while (isRunning)
        {
            try
            {
                Debug.Log("2D 랜드마크 서버 연결을 대기 중입니다...");
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE1_NAME, PipeDirection.In))
                {
                    pipeClient.Connect(5000); // 5초 타임아웃
                    //isConnected = true;
                    Debug.Log("2D 랜드마크 서버와 연결되었습니다.");

                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        while (isRunning)
                        {
                            if (pipeClient.IsConnected)
                            {
                                try
                                {
                                    string message = sr.ReadLine();
                                    if (!string.IsNullOrEmpty(message))
                                    {
                                        if (message.IndexOf("heartbeat") > -1)
                                            continue;
                                        ProcessPoseData(message, Utillity.Instance.frontCameraID == 0 ? true : false);
                                    }
                                    else
                                    {
                                        //Debug.Log("1Empty:" + message + "; ");
                                        if (Utillity.Instance.frontCameraID == 0 ? true : false)
                                            poseData2.Clear();
                                        else
                                            poseData1.Clear();
                                    }
                                }
                                catch (Exception e)
                                {
                                    //Debug.LogError($"포즈 데이터 처리 오류: {e.Message}\n스택 트레이스: {jsonData}");
                                }
                            }
                            else if (!pipeClient.IsConnected)
                            {
                                Debug.LogWarning("파이프 연결이 끊어졌습니다. 재연결을 시도합니다.");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"파이프 클라이언트 오류: {e.Message}\n스택 트레이스: {e.StackTrace}");
                //if (isConnected)
                //{
                //    Debug.Log("서버와의 연결이 끊어졌습니다.");
                //    isConnected = false;
                //}
                Thread.Sleep(1000); // 재연결 전 1초 대기
            }
        }
    }

    private void ProcessPoseData(string jsonData, bool isFront)
    {
        try
        {
            float vis;

            Dictionary<string, Landmark2D> newPoseData = JsonConvert.DeserializeObject<Dictionary<string, Landmark2D>>(jsonData);
            if (!isFront)
            {
                lock (poseData2)
                {
                    poseData2 = newPoseData;
                    visibilitySide = 0;// (poseData2[$"landmark_7"].visibility + poseData2[$"landmark_8"].visibility) / 2f;

                    for (int i = 0; i < poseData2.Count; i++)
                    {
                        poseData2[$"landmark_{i}"].Position = new Vector2(poseData2[$"landmark_{i}"].x, poseData2[$"landmark_{i}"].y);
                        //kalmanFilters2[i][0].Update(poseData2[$"landmark_{i}"].x), 
                        //kalmanFilters2[i][1].Update(poseData2[$"landmark_{i}"].y));
                        visibilitySide += poseData2[$"landmark_{i}"].visibility;
                    }
                    visibilitySide = visibilitySide / poseData2.Count;
                }
            }
            else
            {
                lock (poseData1)
                {
                    poseData1 = newPoseData;
                    visibilityFront = 0;// (poseData1[$"landmark_7"].visibility + poseData1[$"landmark_8"].visibility) / 2f;

                    for (int i = 0; i < poseData1.Count; i++)
                    {
                        poseData1[$"landmark_{i}"].Position = new Vector2(poseData1[$"landmark_{i}"].x, poseData1[$"landmark_{i}"].y);
                        //kalmanFilters1[i][0].Update(poseData1[$"landmark_{i}"].x),
                        //kalmanFilters1[i][1].Update(poseData1[$"landmark_{i}"].y));
                        visibilityFront += poseData1[$"landmark_{i}"].visibility;
                    }
                    visibilityFront = visibilityFront / poseData1.Count;
                }
            }
        }
        catch (Exception e)
        {
            if (!isFront)
            {
                lock (poseData2)
                {
                    poseData2.Clear();
                    visibilitySide = 0;
                }
            }
            else
            {
                lock (poseData1)
                {
                    poseData1.Clear();
                    visibilityFront = 0;
                }
            }
            //Debug.LogError($"포즈 데이터 처리 오류: {e.Message}\n스택 트레이스: {jsonData}");
        }
        
    }

    private void Pipe2ClientThread()
    {
        while (isRunning)
        {
            try
            {
                Debug.Log("2D 랜드마크 서버 연결을 대기 중입니다...");
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE2_NAME, PipeDirection.In))
                {
                    pipeClient.Connect(5000); // 5초 타임아웃
                    //isConnected = true;
                    Debug.Log("2D 랜드마크 서버와 연결되었습니다.");

                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        while (isRunning)
                        {
                            if (pipeClient.IsConnected)
                            {
                                string message = sr.ReadLine();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    if (message.IndexOf("heartbeat") > -1)
                                        continue;
                                    ProcessPoseData(message, Utillity.Instance.sideCameraID == 0 ? true : false);
                                }
                                else
                                {
                                    //Debug.Log("2Empty:" + message + "; ");
                                    if (Utillity.Instance.sideCameraID == 0 ? true : false)
                                        poseData2.Clear();
                                    else
                                        poseData1.Clear();
                                }
                            }
                            else if (!pipeClient.IsConnected)
                            {
                                Debug.LogWarning("파이프 연결이 끊어졌습니다. 재연결을 시도합니다.");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"파이프 클라이언트 오류: {e.Message}\n스택 트레이스: {e.StackTrace}");
                //if (isConnected)
                //{
                //    Debug.Log("서버와의 연결이 끊어졌습니다.");
                //    isConnected = false;
                //}
                Thread.Sleep(1000); // 재연결 전 1초 대기
            }
        }
    }

    float lens = 1f;
    void Update()
    {
        if (updateTexture1 && rawImage1 != null)
        {
            lock (lockObject)
            {
                texture1.LoadImage(imageData1);
                rawImage1.texture = texture1;
                updateTexture1 = false;
            }
        }
        if (updateTexture2 && rawImage2 != null)
        {
            lock (lockObject)
            {
                texture2.LoadImage(imageData2);
                rawImage2.texture = texture2;
                updateTexture2 = false;
            }
        }
        
        
    }

    IEnumerator CoDrawSkeleton()
    {
        float rVis= 0;
        while (true)
        {
            if (drawSkeleton)
            {
                if (poseData1.Count > 0)
                {
                    try
                    {
                        // 머리
                        if (poseData1[$"landmark_7"].visibility > 0.5f || poseData1[$"landmark_8"].visibility > 0.5f)
                        {
                            //float pixelX = ((poseData1[$"landmark_7"].x + poseData1[$"landmark_8"].x) / 2f) * scaleX;
                            //float pixelY = ((poseData1[$"landmark_7"].y + poseData1[$"landmark_8"].y) / 2f) * scaleY;
                            float pixelX = ((poseData1[$"landmark_7"].Position.x + poseData1[$"landmark_8"].Position.x) / 2f) * scaleX;
                            float pixelY = ((poseData1[$"landmark_7"].Position.y + poseData1[$"landmark_8"].Position.y) / 2f) * scaleY;
                            poseHead_Left.anchoredPosition = new Vector2(pixelX, -pixelY);
                            poseHead_Left.gameObject.SetActive(true);
                        }
                        else
                        {
                            poseHead_Left.gameObject.SetActive(false);
                        }
                    }
                    catch
                    {
                        poseHead_Left.gameObject.SetActive(false);
                    }
                    

                    int cIdx = 0;
                    DrawLine_Left();
                    DrawCircle_Left();
                }
                else
                {
                    poseHead_Left.gameObject.SetActive(false);
                    for (int i = 0; i < poseNode_Left.Length; i++)
                    {
                        poseNode_Left[i].gameObject.SetActive(false);
                        poseConnect_Left[i].enabled = false;
                    }
                }

                if (poseData2.Count > 0)
                {
                    // 머리
                    try
                    { 
                        if (poseData2[$"landmark_7"].visibility > 0.5f || poseData2[$"landmark_8"].visibility > 0.5f)
                        {
                            float pixelX = ((poseData2[$"landmark_7"].x + poseData2[$"landmark_8"].x) / 2f) * scaleX;
                            float pixelY = ((poseData2[$"landmark_7"].y + poseData2[$"landmark_8"].y) / 2f) * scaleY;
                            poseHead_Right.anchoredPosition = new Vector2(pixelX, -pixelY);
                            poseHead_Right.gameObject.SetActive(true);
                            poseHead_Right.localScale = Vector3.one * calSideSkelRate;
                        }
                        else
                        {
                            poseHead_Right.gameObject.SetActive(false);
                        }
                    }
                    catch
                    {
                        poseHead_Right.gameObject.SetActive(false);
                    }

                    int cIdx = 0;
                    DrawLine_Right();
                    DrawCircle_Right();
                }
                else
                {
                    poseHead_Right.gameObject.SetActive(false);
                    for (int i = 0; i < poseNode_Right.Length; i++)
                    {
                        poseNode_Right[i].gameObject.SetActive(false);
                        poseConnect_Right[i].enabled = false;
                    }
                }
            }

            yield return null;
        }
    }

    void DrawLine_Left()
    {
        int Idx = 0;
        for (int i = 0; i < POSE_CONNECTION.GetLength(0); i++)
        {
            try
            {

                if (poseData1[$"landmark_{POSE_CONNECTION[i, 0]}"].visibility > 0.5f && poseData1[$"landmark_{POSE_CONNECTION[i, 1]}"].visibility > 0.5f)
                {

                    Vector2 startPos = new Vector2(poseData1[$"landmark_{POSE_CONNECTION[i, 0]}"].x * scaleX, -poseData1[$"landmark_{POSE_CONNECTION[i, 0]}"].y * scaleY);
                    Vector2 endPos = new Vector2(poseData1[$"landmark_{POSE_CONNECTION[i, 1]}"].x * scaleX, -poseData1[$"landmark_{POSE_CONNECTION[i, 1]}"].y * scaleY);

                    poseConnect_Left[Idx].LineThickness = lineThickness;
                    poseConnect_Left[Idx].Points[0] = startPos;
                    poseConnect_Left[Idx].Points[1] = endPos;
                    poseConnect_Left[Idx].enabled = true;
                }
                else
                {
                    poseConnect_Left[Idx].LineThickness = 0;
                    poseConnect_Left[Idx].Points[0] = Vector2.zero;
                    poseConnect_Left[Idx].Points[1] = Vector2.zero;
                    poseConnect_Left[Idx].enabled = false;
                }
            }
            catch (Exception e)
            {
                poseConnect_Left[Idx].LineThickness = 0;
                poseConnect_Left[Idx].Points[0] = Vector2.zero;
                poseConnect_Left[Idx].Points[1] = Vector2.zero;
                poseConnect_Left[Idx].enabled = false;
            }
            Idx++;
        }
    }
    
    void DrawCircle_Left()
    {
        int Idx = 0;
        //몸
        for (int i = 0; i < POSE_LANDMARK.Length; i++)
        {
            try
            {
                if (poseData1[$"landmark_{POSE_LANDMARK[i]}"].visibility > 0.5f)
                {
                    float pixelX = poseData1[$"landmark_{POSE_LANDMARK[i]}"].x * scaleX;
                    float pixelY = poseData1[$"landmark_{POSE_LANDMARK[i]}"].y * scaleY;
                    poseNode_Left[Idx].anchoredPosition = new Vector2(pixelX, -pixelY);
                    poseNode_Left[Idx].gameObject.SetActive(true);

                }
                else
                {
                    poseNode_Left[Idx].gameObject.SetActive(false);
                }
            }
            catch
            {
                poseNode_Left[Idx].gameObject.SetActive(false);
            }

            Idx++;
        }

    }

    void DrawLine_Right()
    {
        int Idx = 0;
        for (int i = 0; i < POSE_CONNECTION.GetLength(0); i++)
        {
            try
            {

                if (poseData2[$"landmark_{POSE_CONNECTION[i, 0]}"].visibility > 0.5f && poseData2[$"landmark_{POSE_CONNECTION[i, 1]}"].visibility > 0.5f)
                {

                    Vector2 startPos = new Vector2(poseData2[$"landmark_{POSE_CONNECTION[i, 0]}"].x * scaleX, -poseData2[$"landmark_{POSE_CONNECTION[i, 0]}"].y * scaleY);
                    Vector2 endPos = new Vector2(poseData2[$"landmark_{POSE_CONNECTION[i, 1]}"].x * scaleX, -poseData2[$"landmark_{POSE_CONNECTION[i, 1]}"].y * scaleY);

                    poseConnect_Right[Idx].LineThickness = lineThickness * calSideSkelRate;
                    poseConnect_Right[Idx].Points[0] = startPos;
                    poseConnect_Right[Idx].Points[1] = endPos;
                    poseConnect_Right[Idx].enabled = true;
                }
                else
                {
                    poseConnect_Right[Idx].LineThickness = 0;
                    poseConnect_Right[Idx].Points[0] = Vector2.zero;
                    poseConnect_Right[Idx].Points[1] = Vector2.zero;
                    poseConnect_Right[Idx].enabled = false;
                }
            }
            catch (Exception e)
            {
                poseConnect_Right[Idx].LineThickness = 0;
                poseConnect_Right[Idx].Points[0] = Vector2.zero;
                poseConnect_Right[Idx].Points[1] = Vector2.zero;
                poseConnect_Right[Idx].enabled = false;
            }
            Idx++;
        }
    }

    void DrawCircle_Right()
    {
        int Idx = 0;
        //몸
        for (int i = 0; i < POSE_LANDMARK.Length; i++)
        {
            try
            {
                if (poseData2[$"landmark_{POSE_LANDMARK[i]}"].visibility > 0.5f)
                {
                    float pixelX = poseData2[$"landmark_{POSE_LANDMARK[i]}"].x * scaleX;
                    float pixelY = poseData2[$"landmark_{POSE_LANDMARK[i]}"].y * scaleY;
                    poseNode_Right[Idx].anchoredPosition = new Vector2(pixelX, -pixelY);
                    poseNode_Right[Idx].gameObject.SetActive(true);
                    poseNode_Right[Idx].transform.localScale = Vector3.one * calSideSkelRate;

                }
                else
                {
                    poseNode_Right[Idx].gameObject.SetActive(false);
                }
            }
            catch
            {
                poseNode_Right[Idx].gameObject.SetActive(false);
            }
            Idx++;
        }

    }



    void OnDisable()
    {
        StopPipeClient();
    }

    public void StopPipeClient()
    {
        StopAllCoroutines();

        shouldStop = true;
        stopEvent.Set();
        isRunning = false;

        if (receiveThread1 != null && receiveThread1.IsAlive)
        {
            receiveThread1.Join(2000);
            if (receiveThread1.IsAlive)
            {
                Debug.LogWarning("ReceiveImage1 쓰레드가 2초 내에 종료되지 않았습니다.");
            }
            else
            {
                Debug.Log("ReceiveImage1 쓰레드가 정상적으로 종료되었습니다.");
            }
        }

        if (receiveThread2 != null && receiveThread2.IsAlive)
        {
            receiveThread2.Join(2000);
            if (receiveThread2.IsAlive)
            {
                Debug.LogWarning("ReceiveImage2 쓰레드가 2초 내에 종료되지 않았습니다.");
            }
            else
            {
                Debug.Log("ReceiveImage2 쓰레드가 정상적으로 종료되었습니다.");
            }
        }

        pipeClient1?.Close();
        pipeClient2?.Close();

        if (pipe1ClientThread != null)
        {
            pipe1ClientThread.Join(2000);
        }

        if (pipe2ClientThread != null)
        {
            pipe2ClientThread.Join(2000);
        }

        Debug.Log("모든 리소스가 해제되었습니다.");
    }
}
