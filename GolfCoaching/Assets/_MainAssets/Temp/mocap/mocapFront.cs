using UnityEngine;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Collections;
using TMPro;

public class mocapFront : MonoBehaviour
{
    private Queue<Vector3>[] positionHistory = new Queue<Vector3>[33];
    private float[] positionVisibilty = new float[33];
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
    public float AdjustAngleZ = 0;
    Quaternion rotationZ;
    private Thread pipeClientThread;
    private bool isRunning = false;
    private Dictionary<string, PoseLandmark> poseData = new Dictionary<string, PoseLandmark>();
    private bool isConnected = false;

    [SerializeField] TextMeshProUGUI txtHandVIsible;

    [SerializeField] TMP_InputField txtAngleZ;

    [SerializeField] TextMeshProUGUI txtVisRate;

    string format = "RH:{0}\r\nLH:{1}\r\nHD:{2}\r\nSA:{3}\r\nPA:{4}\r\nbackboneA:{5}\r\nshoulderA:{6}\r\npelvisA:{7}\r\nspine:{8}\r\nShoulderAngle:{9}\r\nGetHeadDir:{10}";

    [SerializeField] TextMeshProUGUI PipeName;

    [Serializable]
    public class PoseLandmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
    }

    Vector3 handVector = Vector3.zero;
    float _lastHandDir = 180f;

    public float PelvisValue = 0;
    public float ShoulderValue = 0;

    public float AvgVisibility = 0;

    [SerializeField] TextMeshProUGUI txtDebug;

    private void Start()
    {
        InitializeKalmanFilters();

        txtVisRate.text = "00%";

        CAMSWAP = PlayerPrefs.GetInt("CAMSWAP", 0) == 0 ? false : true;

        if (!object.ReferenceEquals(txtAngleZ, null))
            txtAngleZ.text = PlayerPrefs.GetInt("AdjustAngleZF", 0).ToString();

        PIPE_NAME = CAMSWAP ? "skeleton_pipe2" : "skeleton_pipe1";

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
        if (!object.ReferenceEquals(txtAngleZ, null))
        {
            AdjustAngleZ = float.Parse(txtAngleZ.text);
            PlayerPrefs.SetInt("AdjustAngleZF", (int)AdjustAngleZ);
        }

        rotationZ = Quaternion.AngleAxis(AdjustAngleZ, Vector3.right);
    }

    public void OnClick_CamSwap()
    {
        CAMSWAP = !CAMSWAP;
        PlayerPrefs.SetInt("CAMSWAP", CAMSWAP ? 1 : 0);
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
            //pipeClientThread.Abort();
            pipeClientThread.Join(2000);
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
        float rHandVis = 0, lHandVis = 0;
        float rElbowVis = 0, lElbowVis = 0;
        float SumVisibility = 0;
        lock (poseData)
        {
            //string rh = "", lh = "";
            //string rk = "", lk = "";

            for (int i = 0; i < 33; i++)
            {
                string key = $"landmark_{i}";
                if (poseData.TryGetValue(key, out PoseLandmark landmark))
                {
                    SumVisibility += landmark.visibility;

                    //if (i == 22) rHandVis = landmark.visibility;
                    //else if (i == 21) lHandVis = landmark.visibility;
                    if (i == 16) rHandVis = landmark.visibility;
                    else if (i == 15) lHandVis = landmark.visibility;
                    else if (i == 14) rElbowVis = landmark.visibility;
                    else if (i == 13) lElbowVis = landmark.visibility;
                    
                    Vector3 vlandmark = new Vector3(landmark.x, landmark.y, landmark.z);
                    Vector3 rotatedPosition = rotationZ * vlandmark;

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

                    positionVisibilty[i] = landmark.visibility;
                    //txtDebug.text = $"rElbowVis:{rElbowVis}\r\nGetForearmAngle:{GetForearmAngle()}";

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
            //txtVisRate.text = $"{(AvgVisibility * 100f).ToString("00")}%";

            /*txtDebug.text = $"L:{lHandVis.ToString("0.00")}+{lElbowVis.ToString("0.00")}={(lHandVis+lElbowVis).ToString("0.00")}" +
                $"\r\nR:{rHandVis.ToString("0.00")}+{rElbowVis.ToString("0.00")}={(rHandVis + rElbowVis).ToString("0.00")}" +
                $"\r\nC:{((lHandVis + lElbowVis) - (rHandVis + rElbowVis)).ToString("0.00")}";
            */
            //txtDebug.text = $"{GetSpineDir().ToString("0.00")}";
        }
        if ((lHandVis + lElbowVis) - (rHandVis + rElbowVis) < -0.1f)
            handVector = positionTest[22].transform.position;
        else
            handVector = positionTest[21].transform.position;
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
        while (true)
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

    //스윙 각도
    public float GetHandDir()
    {
        // 어꺠중심과 손중심을 기준
        Vector3 shoulderVector = (positionTest[12].transform.position + positionTest[11].transform.position) / 2;

        float shDis = Vector3.Distance(shoulderVector, Utillity.Instance.GetCenter(GetMocapPosition(15), GetMocapPosition(16)));
        
        if (shDis < 0.28f)
            return _lastHandDir;
        else
        {
            Vector3 dir = handVector - shoulderVector;
            dir.z = 0;

            _lastHandDir = Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
            // 어깨 벡터와 손 벡터 간의 각도 계산
            return _lastHandDir;
        }
    }

    //양손 거리(손목)
    public float GetHandDistance()
    {
        return Vector3.Distance(positionTest[15].transform.position, positionTest[16].transform.position);
    }

    //어께 회전
    public float GetShoulderDir()
    {
        // 골반의 중앙을 기준으로 하는 벡터 계산
        Vector3 PelvisVector = (new Vector3(positionTest[24].transform.position.x, 0, positionTest[24].transform.position.z)
            - new Vector3(positionTest[23].transform.position.x, 0, positionTest[23].transform.position.z)).normalized;
        Vector3 shoulderVector = (new Vector3(positionTest[12].transform.position.x, 0, positionTest[12].transform.position.z)
            - new Vector3(positionTest[11].transform.position.x, 0, positionTest[11].transform.position.z)).normalized;

        // 어깨 벡터와 골반 벡터 간의 회전 각도 계산
        float angle = Vector3.SignedAngle(PelvisVector, shoulderVector, Vector3.up);
        ShoulderValue = angle / 180f;
        return ShoulderValue * 180f;
    }

    //골반 회전
    public float GetPelvisDir()
    {
        Vector3 PelvisVector = (new Vector3(positionTest[24].transform.position.x, 0, positionTest[24].transform.position.z)
            - new Vector3(positionTest[23].transform.position.x, 0, positionTest[23].transform.position.z)).normalized;
        Vector3 FootVector = (new Vector3(positionTest[28].transform.position.x, 0, positionTest[28].transform.position.z)
            - new Vector3(positionTest[27].transform.position.x, 0, positionTest[27].transform.position.z)).normalized;

        // 어깨 벡터와 골반 벡터 간의 회전 각도 계산
        float angle = Vector3.SignedAngle(FootVector, PelvisVector, Vector3.up);
        PelvisValue = angle / 180f;
        return PelvisValue * 180f;
    }

    public float GetSpineDir()
    {
        Vector3 shoulder = Utillity.Instance.GetCenter(positionTest[11].transform.position, positionTest[12].transform.position);
        Vector3 pelvice = Utillity.Instance.GetCenter(positionTest[23].transform.position, positionTest[24].transform.position);

        Vector3 localX = (positionTest[23].transform.position - positionTest[24].transform.position).normalized;
        localX.y = 0;
        Vector3 localY = Vector3.ProjectOnPlane(Vector3.up, localX).normalized;
        Vector3 localZ = Vector3.Cross(localX, localY);

        Vector3 directionWorld = shoulder - pelvice;

        float xLocal = Vector3.Dot(directionWorld, localX);
        float yLocal = Vector3.Dot(directionWorld, localY);

        float localZRotation = Mathf.Atan2(yLocal, xLocal) * Mathf.Rad2Deg;

        return localZRotation - 90;

    }

    public float GetBackboneDir()
    {
        Vector3 shoulderCenter = Utillity.Instance.GetCenter(positionTest[11].transform.position, positionTest[12].transform.position);
        Vector3 hipCenter = Utillity.Instance.GetCenter(positionTest[23].transform.position, positionTest[24].transform.position);

        return Utillity.Instance.GetAngle(shoulderCenter, hipCenter, Enums.EDirection.Up);
    }

    public float GetShoulderDir_Other()
    {
        Vector3 leftShoulder = positionTest[11].transform.position;
        leftShoulder = new Vector3(leftShoulder.x, 0, leftShoulder.z);
        Vector3 rightShoulder = positionTest[12].transform.position;
        rightShoulder = new Vector3(rightShoulder.x, 0, rightShoulder.z);

        float shoulderAngle = Utillity.Instance.GetAngle(leftShoulder, rightShoulder, Enums.EDirection.Right);
        return shoulderAngle * GetRotationDirection(leftShoulder, rightShoulder);
    }

    public float GetPelvisDir_Other()
    {
        Vector3 leftHip = positionTest[23].transform.position;
        leftHip = new Vector3(leftHip.x, 0, leftHip.z);
        Vector3 rightHip = positionTest[24].transform.position;
        rightHip = new Vector3(rightHip.x, 0, rightHip.z);

        float pelvisAngle = Utillity.Instance.GetAngle(leftHip, rightHip, Enums.EDirection.Right);
        return Mathf.Abs(pelvisAngle * GetRotationDirection(leftHip, rightHip));
    }

    int GetRotationDirection(Vector3 left, Vector3 right)
    {
        return (left.z > right.z) ? -1 : 1;
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

    public Quaternion GetLookRotationPelvis()
    {
        return Quaternion.LookRotation(positionTest[24].transform.position - positionTest[23].transform.position);
    }

    public Quaternion GetLookRotationShoulder()
    {
        return Quaternion.LookRotation(positionTest[12].transform.position - positionTest[11].transform.position);
    }

    public float GetPelvisFootDis()
    {
        float RDis = Math.Abs(positionTest[28].transform.position.y - positionTest[24].transform.position.y); //Right
        float LDis = Math.Abs(positionTest[27].transform.position.y - positionTest[23].transform.position.y); //Left
        return RDis > LDis ? RDis : LDis;        
    }

    public Vector3[] GetKneeDir()
    {
        return new Vector3[2] { positionTest[25].transform.position - positionTest[27].transform.position,
            positionTest[26].transform.position - positionTest[28].transform.position};
    }

    public Vector3 GetForearmDir()
    {
        return positionTest[14].transform.position - positionTest[12].transform.position;
    }

    public float GetForearmAngle()
    {
        Vector3 v1 = positionTest[14].transform.position; // 손
        Vector3 v2 = positionTest[12].transform.position; // 어깨

        Vector3 v = v1 - v2;

        return Mathf.Atan2(-v.x, -v.y) * Mathf.Rad2Deg;

    }

    public float GetShoulderAngle()
    {
        Vector3 shoulder = positionTest[11].transform.position - positionTest[12].transform.position;
        shoulder.z = 0;

        float angle = Vector2.SignedAngle(shoulder.normalized, Vector2.right);

        return angle;

    }

    public float GetHeadDir()
    {
        Vector3 eyeCenter = (positionTest[2].transform.position + positionTest[5].transform.position) / 2f;
        Vector3 eyeRight = (positionTest[5].transform.position - positionTest[2].transform.position).normalized;

        Vector3 headForward = Vector3.Cross(Vector3.up, eyeRight).normalized;

        float yawAngle = Vector3.SignedAngle(Vector3.forward, headForward, Vector3.up);

        return yawAngle;
    }

    public float GetFootDisRate()
    {
        float shoulderDis = Vector3.Distance(positionTest[11].transform.position, positionTest[12].transform.position);
        float footDis = Vector3.Distance(positionTest[27].transform.position, positionTest[28].transform.position);

        return (footDis / shoulderDis) * 100f;

    }

    public float GetWeight()
    {
        Vector3 foot = FootCenter();
        Vector3 pelvis = PelvisCenter();

        Vector3 dir = foot - pelvis;

        dir.Normalize();

        return dir.x;
    }

    public Vector3 GetMocapPosition(int idx)
    {
        return positionTest[idx].transform.position;
    }

    public float GetMocapVisibilty(int idx)
    {
        return positionVisibilty[idx];
    }
}