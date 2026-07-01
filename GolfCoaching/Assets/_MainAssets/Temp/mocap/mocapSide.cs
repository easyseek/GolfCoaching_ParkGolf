using UnityEngine;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using TMPro;
using Unity.VisualScripting;

public class mocapSide : MonoBehaviour
{
    private Queue<Vector3>[] positionHistory = new Queue<Vector3>[33];
    private const int historyLength = 5; // 이동 평균을 위한 히스토리 길이
    private Vector3[] lastValidPositions = new Vector3[33];
    private const float maxAllowedChange = 0.1f; // 한 프레임당 최대 허용 변화량

    [SerializeField] float smoothness = 0.1f;
    [SerializeField] private GameObject[] positionTest = new GameObject[33];
    // 생성자 또는 Start 메서드에서 칼만 필터 초기화
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
    public string PIPE_NAME = "skeleton_pipe1";
    bool CAMSWAP = false;
    public float AdjustAngleX = 0;
    public float AdjustAngleY = 0;
    public float AdjustAngleZ = 0;
    Quaternion rotationX;
    Quaternion rotationY;
    Quaternion rotationZ;
    private Thread pipeClientThread;
    private bool isRunning = false;
    private Dictionary<string, PoseLandmark> poseData = new Dictionary<string, PoseLandmark>();
    private bool isConnected = false;

    [SerializeField] TextMeshProUGUI txtHandVIsible;

    [SerializeField] TMP_InputField txtAngleX;
    [SerializeField] TMP_InputField txtAngleY;
    [SerializeField] TMP_InputField txtAngleZ;

    [SerializeField] TextMeshProUGUI txtVisRate;

    string format = "SHD:{0}\r\nWaistA:{1}\r\nKneeA:{2}\r\nElbowA:{3}\r\nArmpitA:{4}\r\nPFDis:{5}";

    [SerializeField] TextMeshProUGUI PipeName;

    private float _lHandVis = 0.0f;
    public float LHandVis
        { get { return _lHandVis; } }


    public float AvgVisibility = 0;

    [Serializable]
    public class PoseLandmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
    }

    [SerializeField] TextMeshProUGUI txtDebug;

    private void Start()
    {
        InitializeKalmanFilters();

        txtVisRate.text = "00%";

        CAMSWAP = PlayerPrefs.GetInt("CAMSWAP", 0) == 0 ? false : true;

        if (!object.ReferenceEquals(txtAngleX, null))
            txtAngleX.text = PlayerPrefs.GetInt("AdjustAngleX", 0).ToString();
        if (!object.ReferenceEquals(txtAngleY, null))
            txtAngleY.text = PlayerPrefs.GetInt("AdjustAngleY", -90).ToString();
        if (!object.ReferenceEquals(txtAngleZ, null))
            txtAngleZ.text = PlayerPrefs.GetInt("AdjustAngleZ", 0).ToString();

        PIPE_NAME = CAMSWAP ? "skeleton_pipe1" : "skeleton_pipe2";

        OnClick_Angle();

        Debug.Log($"{gameObject.name} CAMSWAP : {CAMSWAP} / PIPE : {PIPE_NAME}");

        PipeName.text = PIPE_NAME;

        StartPipeClient();

        //mocap씬
        StartMatchPose();
    }

    private void OnDestroy()
    {
        StopPipeClient();
    }

    private void StartPipeClient()
    {
        isRunning = true;
        pipeClientThread = new Thread(PipeClientThread);
        pipeClientThread.Start();
    }

    public void OnClick_Angle()
    {
        if (!object.ReferenceEquals(txtAngleX, null))
        {
            AdjustAngleX = float.Parse(txtAngleX.text);
            PlayerPrefs.SetInt("AdjustAngleX", (int)AdjustAngleX);
        }
            
        if (!object.ReferenceEquals(txtAngleY, null))
        {
            AdjustAngleY = float.Parse(txtAngleY.text);
            PlayerPrefs.SetInt("AdjustAngleY", (int)AdjustAngleY);
        }
            
        if (!object.ReferenceEquals(txtAngleZ, null))
        {
            AdjustAngleZ = float.Parse(txtAngleZ.text);
            PlayerPrefs.SetInt("AdjustAngleZ", (int)AdjustAngleZ);
        }

        rotationZ = Quaternion.AngleAxis(AdjustAngleZ, Vector3.forward);
        rotationX = Quaternion.AngleAxis(AdjustAngleX, Vector3.right);
        rotationY = Quaternion.AngleAxis(AdjustAngleY, Vector3.up);
        
    }

    public void OnClick_CamSwap()
    {
        CAMSWAP = !CAMSWAP;
        PlayerPrefs.SetInt("CAMSWAP", CAMSWAP? 1 : 0);
    }

    private void OnDisable()
    {
        StopPipeClient();
    }

    public void StopPipeClient()
    {
        isRunning = false;
        if (pipeClientThread != null)
        {
            pipeClientThread.Join(2000);
            //pipeClientThread.Abort();
        }
    }

    public bool IsAlive()
    {
        return pipeClientThread.IsAlive;
    }

    private void PipeClientThread()
    {
        while (isRunning)
        {
            try
            {
                Debug.Log("서버 연결을 대기 중입니다...");
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.In))
                {
                    pipeClient.Connect(5000); // 5초 타임아웃
                    isConnected = true;
                    Debug.Log("서버와 연결되었습니다.");

                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        while (isRunning)
                        {
                            if (pipeClient.IsConnected)
                            {
                                string message = sr.ReadLine();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    ProcessPoseData(message);
                                }
                                else
                                {
                                    poseData.Clear();
                                }
                            }
                            else
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
                if (isConnected)
                {
                    Debug.Log("서버와의 연결이 끊어졌습니다.");
                    isConnected = false;
                }
                Thread.Sleep(1000); // 재연결 전 1초 대기
            }
        }
    }

    private void ProcessPoseData(string jsonData)
    {
        try
        {
            Dictionary<string, PoseLandmark> newPoseData = JsonConvert.DeserializeObject<Dictionary<string, PoseLandmark>>(jsonData);

            lock (poseData)
            {
                poseData = newPoseData;
            }
        }
        catch (Exception e)
        {
            //Debug.LogError($"포즈 데이터 처리 오류: {e.Message}\n스택 트레이스: {jsonData}");
        }
    }

    public Vector3[] updatedJointPositions = new Vector3[33];

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

    private void UpdatePositionTest(Vector3[] positions)
    {
        for (int i = 0; i < positionTest.Length && i < positions.Length; i++)
        {
            if (positionTest[i] != null)
            {
                // 좌우 반전 및 위아래 뒤집기
                Vector3 flippedPosition = new Vector3(-positions[i].x, positions[i].y, positions[i].z);
                positionTest[i].transform.position = Vector3.Lerp(positionTest[i].transform.position, flippedPosition, characterpose.Instance.smoothness);
                updatedJointPositions[i] = flippedPosition;
            }
        }
        characterpose.Instance.UpdateBodyPositions(updatedJointPositions);
    }
    

    void UpdateBodyTransform()
    {
        float SumVisibility = 0;

        lock (poseData)
        {
            for (int i = 0; i < 33; i++)
            {
                string key = $"landmark_{i}";
                if (poseData.TryGetValue(key, out PoseLandmark landmark))
                {
                    SumVisibility += landmark.visibility;

                    if (i == 13) _lHandVis = landmark.visibility;

                    Vector3 vlandmark = new Vector3(landmark.x, landmark.y, landmark.z);
                    Vector3 rotatedPosition = rotationZ * vlandmark;
                    rotatedPosition = rotationX * rotatedPosition;
                    rotatedPosition = rotationY * rotatedPosition;

                    Vector3 newPosition = new Vector3(rotatedPosition.x, -rotatedPosition.y, rotatedPosition.z
                    //kalmanFilters[i][0].Update(rotatedPosition.x),
                    //-kalmanFilters[i][1].Update(rotatedPosition.y),
                    //kalmanFilters[i][2].Update(rotatedPosition.z)
                    );          

                    // 초기화
                    if (positionHistory[i] == null)
                    {
                        positionHistory[i] = new Queue<Vector3>();
                        lastValidPositions[i] = newPosition;
                    }

                    // 급격한 변화 제한
                    //Vector3 limitedPosition = LimitPositionChange(lastValidPositions[i], newPosition, maxAllowedChange);

                    // 이동 평균 필터 적용
                    //positionHistory[i].Enqueue(limitedPosition);
                    positionHistory[i].Enqueue(newPosition);
                    if (positionHistory[i].Count > historyLength)
                    {
                        positionHistory[i].Dequeue();
                    }

                    Vector3 averagePosition = CalculateAverage(positionHistory[i]);

                    updatedJointPositions[i] = averagePosition;
                    positionTest[i].transform.position = averagePosition;
                    lastValidPositions[i] = averagePosition;
                }
            }

            AvgVisibility = SumVisibility / 32f;
        }
    }


    private void PrintPoseData()
    {
        UnityEngine.Debug.Log
            (
                message: "mocap.cs - Line 268\n" +
                $@"poseData.Count : {poseData.Count}"
            );
        lock (poseData)
        {
            if (poseData.Count > 0)
            {
                Debug.Log("현재 포즈 데이터:");
                foreach (var kvp in poseData)
                {
                    Debug.Log($"{kvp.Key}: X={kvp.Value.x}, Y={kvp.Value.y}, Z={kvp.Value.z}, Visibility={kvp.Value.visibility}");
                }
            }
        }
    }

    IEnumerator CoStartMatchPose()
    {
        while(true)
        {
            if (poseData.Count < 32)
            {
                AvgVisibility = 0;
                txtVisRate.text = "00%";
            }
            else
                UpdateBodyTransform();
            yield return null;
        }
    }

    public void StartMatchPose()
    {
        StartCoroutine(CoStartMatchPose());
    }

    public void StopMatchPose()
    {
        StopAllCoroutines();
        Reset();
    }


    public void Reset()
    {
        foreach (var data in poseData)
        {
            data.Value.x = 0;
            data.Value.y = 0;
            data.Value.z = 0;
            data.Value.visibility = 0;
        }
        
        for (int i = 0; i < positionTest.Length; i++)
        {
            positionTest[i].transform.position = Vector3.zero;
        }
    }

    public Dictionary<string, PoseLandmark> GetPoseData()
    {
        return poseData;
    }

    public void PositionTestShow(bool isShow)
    {
        for (int i = 0; i < positionTest.Length; i++)
        {
            positionTest[i].GetComponent<MeshRenderer>().enabled = isShow;
        }
    }

    public float GetHandSideDir()
    {

        Vector3 v1 = positionTest[18].transform.position; // 손
        Vector3 v2 = positionTest[12].transform.position; // 어깨

        Vector3 v = v1 - v2;

        return Mathf.Atan2(-v.y, -v.z) * Mathf.Rad2Deg;

    }

   

    public float GetWaistSideDir()
    {
        return 180.0f - Utillity.Instance.CalculateVectorAngle(positionTest[12].transform.position, positionTest[24].transform.position, positionTest[26].transform.position); // 어깨, 허리, 무릎
    }

    public float GetKneeSideDir()
    {
        return 180.0f - Utillity.Instance.CalculateVectorAngle(positionTest[24].transform.position, positionTest[26].transform.position, positionTest[28].transform.position); // 허리, 무릎, 발목
    }

    public float GetElbowSideDir()
    {
        return 180.0f - Utillity.Instance.CalculateVectorAngle(positionTest[12].transform.position, positionTest[14].transform.position, positionTest[16].transform.position); // 어깨, 팔꿈치, 손목
    }

    public float GetArmpitDir()
    {
        return 180.0f - Utillity.Instance.CalculateVectorAngle(positionTest[14].transform.position, positionTest[12].transform.position, positionTest[24].transform.position); // 팔꿈치, 어깨, 허리
    }

    public float GetPelvisFootDis()
    {
        float RDis = Math.Abs(positionTest[28].transform.position.y - positionTest[24].transform.position.y); //Right
        float LDis = Math.Abs(positionTest[27].transform.position.y - positionTest[23].transform.position.y); //Left
        return RDis > LDis ? RDis : LDis;
    }

    public Vector3 PelvisCenter()
    {
        return Utillity.Instance.GetCenter(positionTest[23].transform.position, positionTest[24].transform.position);
    }

    public Vector3 FootCenter()
    {
        return Utillity.Instance.GetCenter(positionTest[27].transform.position, positionTest[28].transform.position);
    }

    public Vector3 ShoulderCenter()
    {
        return Utillity.Instance.GetCenter(positionTest[11].transform.position, positionTest[12].transform.position);
    }

    public Vector3 GetForearmDir()
    {
        return positionTest[14].transform.position - positionTest[12].transform.position;
    }

    public float GetForearmAndgle()
    {
        Vector3 v1 = positionTest[14].transform.position; // 손
        Vector3 v2 = positionTest[12].transform.position; // 어깨

        Vector3 v = v1 - v2;

        return Mathf.Atan2(v.z, -v.y) * Mathf.Rad2Deg;

    }

    public Vector3 GetMocapPosition(int idx)
    {
        return positionTest[idx].transform.position;
    }
}