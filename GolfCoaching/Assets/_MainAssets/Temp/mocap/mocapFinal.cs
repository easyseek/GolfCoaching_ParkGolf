using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Threading;
using UnityEngine;
using static mocapDebug;
using Newtonsoft.Json;
using TMPro;
using UnityEngine.UIElements;
using System.Xml.Linq;

public class mocapFinal : MonoBehaviour
{
    private Dictionary<string, mocapDebug.PoseLandmark> poseData_Front = new Dictionary<string, mocapDebug.PoseLandmark>();
    private Dictionary<string, mocapDebug.PoseLandmark> poseData_Side = new Dictionary<string, mocapDebug.PoseLandmark>();

    [SerializeField] private GameObject[] positionTest = new GameObject[33];
    private Queue<Vector3>[] positionHistory = new Queue<Vector3>[33];
    public Vector3[] updatedJointPositions = new Vector3[33];

    private Vector3[] lastValidPositions = new Vector3[33];
    private const int historyLength = 5; // 이동 평균을 위한 히스토리 길이
    private const float maxAllowedChange = 0.1f; // 한 프레임당 최대 허용 변화량

    //public mocapDebug mocapDebug_Front;
    //public mocapDebug mocapDebug_Side;

    [SerializeField] float smoothness = 0.1f;

    private void InitializeKalmanFilters()
    {
        for (int i = 0; i < 33; i++)
        {
            kalmanFilters[i] = new KalmanFilter[3];
            for (int j = 0; j < 3; j++)
            {
                kalmanFilters[i][j] = new KalmanFilter();
            }
        }
    }
    private KalmanFilter[][] kalmanFilters = new KalmanFilter[33][];

    bool CAMSWAP = false;
    public string PIPE_NAME_F = "skeleton_pipe1";
    public string PIPE_NAME_S = "skeleton_pipe2";
    public float AdjustAngleX = 0;
    public float AdjustAngleY = 0;
    Quaternion rotationX;
    Quaternion rotationY;
    private NamedPipeClientStream pipeClientF;
    private NamedPipeClientStream pipeClientS;
    private Thread pipeClientThreadF;
    private Thread pipeClientThreadS;
    private bool isRunningF = false;
    private bool isConnectedF = false;
    private bool isRunningS = false;
    private bool isConnectedS = false;


    [SerializeField] TextMeshProUGUI txtHandVIsibleF;
    [SerializeField] TextMeshProUGUI txtHandVIsibleS;

    [SerializeField] TMP_InputField txtAngleX;
    [SerializeField] TMP_InputField txtAngleY;
    string format = "RH:{0}\r\nLH:{1}";

    [SerializeField] TextMeshProUGUI txtFrontName;
    [SerializeField] TextMeshProUGUI txtSideName;

    [SerializeField] TextMeshProUGUI txtHandAngle;

    [SerializeField] Transform LeftHand;
    [SerializeField] Transform RightHand;

    [SerializeField] Transform LeftShoulder;
    [SerializeField] Transform RightShoulder;

    public float HandAngle = 0;
    public bool isBackSwing = true;

    [SerializeField] bool OnSideCam = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeKalmanFilters();

        CAMSWAP = PlayerPrefs.GetInt("CAMSWAP", 0) == 0 ? false : true;

        PIPE_NAME_F = CAMSWAP ? "skeleton_pipe2" : "skeleton_pipe1";
        PIPE_NAME_S = CAMSWAP ? "skeleton_pipe1" : "skeleton_pipe2";

        txtFrontName.text = PIPE_NAME_F;
        txtSideName.text = PIPE_NAME_S;

        txtAngleX.text = PlayerPrefs.GetInt("AdjustAngleX", 0).ToString();
        txtAngleY.text = PlayerPrefs.GetInt("AdjustAngleY", -90).ToString();

        OnClick_Angle();

        StartPipeClientF();
        StartPipeClientS();

        StartMatchPose();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
    }

    public void OnClick_Angle()
    {
        
        AdjustAngleX = float.Parse(txtAngleX.text);
        AdjustAngleY = float.Parse(txtAngleY.text);

        PlayerPrefs.SetInt("AdjustAngleX", (int)AdjustAngleX);
        PlayerPrefs.SetInt("AdjustAngleY", (int)AdjustAngleY);

        rotationX = Quaternion.AngleAxis(AdjustAngleX, Vector3.right);
        rotationY = Quaternion.AngleAxis(AdjustAngleY, Vector3.up);
    }

    public void OnClick_CamSwap()
    {
        CAMSWAP = !CAMSWAP;
        PlayerPrefs.SetInt("CAMSWAP", CAMSWAP ? 1 : 0);
    }


    public void StartMatchPose()
    {
        StartCoroutine(CoStartMatchPose());
    }

    IEnumerator CoStartMatchPose()
    {
        while (true)
        {
            UpdateBodyTransform();
            yield return null;
        }
    }

    private void OnDestroy()
    {
        StopPipeClient();
    }

    void StartPipeClientF()
    {
        isRunningF = true;

        pipeClientF = new NamedPipeClientStream(".", PIPE_NAME_F, PipeDirection.In);
        pipeClientThreadF = new Thread(PipeClientThreadF);
        pipeClientThreadF.Start();
    }

    void StartPipeClientS()
    {
        isRunningS = true;

        pipeClientS = new NamedPipeClientStream(".", PIPE_NAME_S, PipeDirection.In);
        pipeClientThreadS = new Thread(PipeClientThreadS);
        pipeClientThreadS.Start();
    }

    private void StopPipeClient()
    {
        isRunningF = false;
        isRunningS = false;
        if (pipeClientThreadF != null)
        {
            pipeClientThreadF.Join();
        }
        if (pipeClientThreadS != null)
        {
            pipeClientThreadS.Join();
        }
    }

    private void PipeClientThreadF()
    {
        while (isRunningF)
        {
            try
            {
                Debug.Log($"서버 연결을 대기 중입니다...{PIPE_NAME_F}");
                //using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME_F, PipeDirection.In))
                {
                    pipeClientF.Connect(5000); // 5초 타임아웃
                    isConnectedF = true;
                    Debug.Log("서버와 연결되었습니다.");
                    using (StreamReader sr = new StreamReader(pipeClientF))
                    {
                        Debug.Log("4");
                        while (isRunningF)
                        {
                            if (pipeClientF.IsConnected)
                            {
                                string message = sr.ReadLine();
                                //Debug.Log($"Raw received data: {message}");  // 추가된 로그
                                if (message.Length > 5000)
                                    continue;

                                if (!string.IsNullOrEmpty(message))
                                {
                                    lock (poseData_Front)
                                    {
                                        //poseData_Front = ProcessPoseData(message);
                                        poseData_Front = JsonConvert.DeserializeObject<Dictionary<string, PoseLandmark>>(message);
                                    }                                    
                                }
                                else
                                {
                                    //Debug.LogWarning("Received empty message");  // 추가된 로그
                                }
                            }
                            else
                            {
                                Debug.LogWarning("파이프 연결이 끊어졌습니다. 재연결을 시도합니다.");
                                //isConnected = false;
                                break;
                            }
                        } 
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"파이프 클라이언트 오류: {e.Message}\n스택 트레이스: {e.StackTrace}");
                if (isConnectedF)
                {
                    Debug.Log("서버와의 연결이 끊어졌습니다.");
                    isConnectedF = false;
                }
                Thread.Sleep(1000); // 재연결 전 1초 대기
            }
        }
    }

    private void PipeClientThreadS()
    {
        while (isRunningS)
        {
            try
            {
                Debug.Log($"서버 연결을 대기 중입니다...{PIPE_NAME_S}");
                //using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME_S, PipeDirection.In))
                {
                    pipeClientS.Connect(5000); // 5초 타임아웃
                    isConnectedS = true;
                    Debug.Log("서버와 연결되었습니다.");

                    using (StreamReader sr = new StreamReader(pipeClientS))
                    {
                        while (isRunningS)
                        {
                            if (pipeClientS.IsConnected)
                            {
                                string message = sr.ReadLine();
                                if (message.Length > 5000)
                                    continue;
                                //Debug.Log($"Raw received data: {message}");  // 추가된 로그
                                if (!string.IsNullOrEmpty(message))
                                {
                                    lock (poseData_Side)
                                    {
                                        //poseData_Side = ProcessPoseData(message);
                                        poseData_Side = JsonConvert.DeserializeObject<Dictionary<string, PoseLandmark>>(message);
                                    }
                                }
                                else
                                {
                                    //Debug.LogWarning("Received empty message");  // 추가된 로그
                                }
                            }
                            else
                            {
                                Debug.LogWarning("파이프 연결이 끊어졌습니다. 재연결을 시도합니다.");
                                //isConnected = false;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"파이프 클라이언트 오류: {e.Message}\n스택 트레이스: {e.StackTrace}");
                if (isConnectedS)
                {
                    Debug.Log("서버와의 연결이 끊어졌습니다.");
                    isConnectedS = false;
                }
                Thread.Sleep(1000); // 재연결 전 1초 대기
            }
        }
    }

    void UpdateBodyTransform()
    {
        lock (poseData_Front)
        {
            string rhf = "", lhf = "";
            string rhs = "", lhs = "";
            for (int i = 0; i < 33; i++)
            {
                string key = $"landmark_{i}";

                bool p1 = poseData_Front.TryGetValue(key, out mocapDebug.PoseLandmark landmark);
                bool p2 = poseData_Side.TryGetValue(key, out mocapDebug.PoseLandmark landmarkS);
                if (i == 18) rhf = landmark.visibility.ToString("0.00");
                if (i == 17) lhf = landmark.visibility.ToString("0.00");
                if (i == 18) rhs = landmarkS.visibility.ToString("0.00");
                if (i == 17) lhs = landmarkS.visibility.ToString("0.00");
                if (p1)
                {
                    if(landmark.visibility < 0.5f)
                    {
                        if(p2)
                        {
                            if(landmarkS.visibility > 0.5f)
                            {
                                Vector3 vlandmark = new Vector3(landmarkS.x, landmarkS.y, landmarkS.z);
                                Vector3 rotatedPosition = rotationX * vlandmark;
                                rotatedPosition = rotationY * rotatedPosition;

                                landmark.x = rotatedPosition.x;
                                landmark.y = rotatedPosition.y;
                                landmark.z = rotatedPosition.z;
                            }
                            else
                            {
                                Vector3 vlandmark = new Vector3(landmarkS.x, landmarkS.y, landmarkS.z);
                                Vector3 rotatedPosition = rotationX * vlandmark;
                                rotatedPosition = rotationY * rotatedPosition;

                                Vector3 vlandmarkN = integrate_landmarks(new Vector3(landmark.x, landmark.y, landmark.z),
                                rotatedPosition, landmark.visibility, landmarkS.visibility);

                                landmark.x = vlandmarkN.x;
                                landmark.y = vlandmarkN.y;
                                landmark.z = vlandmarkN.z;
                            }
                        }
                    }

                    Vector3 newPosition = new Vector3(
                            kalmanFilters[i][0].Update(landmark.x),
                            -kalmanFilters[i][1].Update(landmark.y),
                            kalmanFilters[i][2].Update(landmark.z)
                        );

                    // 초기화
                    if (positionHistory[i] == null)
                    {
                        positionHistory[i] = new Queue<Vector3>();
                        lastValidPositions[i] = newPosition;
                    }

                    // 급격한 변화 제한
                    Vector3 limitedPosition = LimitPositionChange(lastValidPositions[i], newPosition, maxAllowedChange);

                    // 이동 평균 필터 적용
                    positionHistory[i].Enqueue(limitedPosition);
                    if (positionHistory[i].Count > historyLength)
                    {
                        positionHistory[i].Dequeue();
                    }

                    Vector3 averagePosition = CalculateAverage(positionHistory[i]);

                    updatedJointPositions[i] = averagePosition;
                    lastValidPositions[i] = averagePosition;
                }
                /*
                if (poseData_Front.TryGetValue(key, out mocapDebug.PoseLandmark landmark))
                {
                    if (i == 18) rhf = landmark.visibility.ToString("0.00");
                    if (i == 17) lhf = landmark.visibility.ToString("0.00");

                    if (landmark.visibility < 0.5f)
                    {
                        lock (poseData_Side)
                        {
                            if (poseData_Side.TryGetValue(key, out mocapDebug.PoseLandmark landmarkS))
                            {
                                if (i == 18) rhs = landmarkS.visibility.ToString("0.00");
                                if (i == 17) lhs = landmarkS.visibility.ToString("0.00");
                                if (landmarkS.visibility > landmark.visibility)
                                {
                                    Vector3 vlandmark = new Vector3(landmarkS.x, landmarkS.y, landmarkS.z);
                                    Vector3 rotatedPosition = rotationX * vlandmark;
                                    rotatedPosition = rotationY * rotatedPosition;

                                    //landmark = landmarkS;
                                    landmark.x = rotatedPosition.x;
                                    landmark.y = rotatedPosition.y;
                                    landmark.z = rotatedPosition.z;
                                }
            }
                        }
                    }

                    Vector3 newPosition = new Vector3(
                        kalmanFilters[i][0].Update(landmark.x),
                        -kalmanFilters[i][1].Update(landmark.y),
                        kalmanFilters[i][2].Update(landmark.z)
                    );

                    // 초기화
                    if (positionHistory[i] == null)
                    {
                        positionHistory[i] = new Queue<Vector3>();
                        lastValidPositions[i] = newPosition;
                    }

                    // 급격한 변화 제한
                    Vector3 limitedPosition = LimitPositionChange(lastValidPositions[i], newPosition, maxAllowedChange);

                    // 이동 평균 필터 적용
                    positionHistory[i].Enqueue(limitedPosition);
                    if (positionHistory[i].Count > historyLength)
                    {
                        positionHistory[i].Dequeue();
                    }

                    Vector3 averagePosition = CalculateAverage(positionHistory[i]);

                    updatedJointPositions[i] = averagePosition;
                    lastValidPositions[i] = averagePosition;
                }*/
            }
            txtHandVIsibleF.text = string.Format(format, rhf, lhf);
            txtHandVIsibleS.text = string.Format(format, rhs, lhs);
        }

        for (int i = 0; i < positionTest.Length && i < updatedJointPositions.Length; i++)
        {
            if (positionTest[i] != null)
            {
                // 좌우 반전 및 위아래 뒤집기
                Vector3 flippedPosition = new Vector3(-updatedJointPositions[i].x, updatedJointPositions[i].y, updatedJointPositions[i].z);
                positionTest[i].transform.position = Vector3.Lerp(positionTest[i].transform.position, flippedPosition, this.smoothness);
                updatedJointPositions[i] = flippedPosition;
            }
        }

        GetHandDir();
    }

    Vector3 integrate_landmarks(Vector3 P1, Vector3 P2, float visibility1, float visibility2, float threshold = 0.5f)
    {
        if (visibility1 < threshold)
            return P2;
        else if (visibility2 < threshold)
            return P1;
        else
        {
            var w1 = visibility1;
            var w2 = visibility2;
            return (w1 * P1 + w2 * P2) / (w1 + w2);
        }
    }

    private Vector3 LimitPositionChange(Vector3 oldPosition, Vector3 newPosition, float maxChange)
    {
        Vector3 change = newPosition - oldPosition;
        float magnitude = change.magnitude;
        if (magnitude > maxChange)
        {
            change = change.normalized * maxChange;
        }
        return oldPosition + change;
    }

    private Vector3 CalculateAverage(Queue<Vector3> positions)
    {
        Vector3 sum = Vector3.zero;
        foreach (Vector3 pos in positions)
        {
            sum += pos;
        }
        return sum / positions.Count;
    }

    void GetHandDir()
    {
        // 어꺠중심과 손중심을 기준
        Vector3 shoulderVector = (RightShoulder.position + LeftShoulder.position) / 2;
        Vector3 handVector = (RightHand.position + LeftHand.position) / 2;
        Vector3 dir = handVector - shoulderVector;
        dir.z = 0;
        HandAngle = 360 - Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
        // 어깨 벡터와 손 벡터 간의 각도 계산
        //if(isBackSwing)
        //    HandAngle = Quaternion.FromToRotation(Vector3.down, dir).eulerAngles.z;
        //else
        //    HandAngle = 360 - Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
        //Debug.Log($"Hand Angle : {HandAngle}");
        Debug.DrawLine(shoulderVector, handVector, Color.green);
        txtHandAngle.text = HandAngle.ToString("0");
    }
}
