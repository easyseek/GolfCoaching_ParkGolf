using UnityEngine;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
using System.Collections;

public class MocapManager : MonoBehaviourSingleton<MocapManager>
{

    private Queue<Vector3>[] positionHistory = new Queue<Vector3>[33];
    private const int historyLength = 5; // 이동 평균을 위한 히스토리 길이
    private Vector3[] lastValidPositions = new Vector3[33];
    private const float maxAllowedChange = 0.1f; // 한 프레임당 최대 허용 변화량

    [SerializeField] float smoothness = 0.1f;
    [SerializeField] private GameObject[] positionTest = new GameObject[33];

    //public int visibleCount = 0;

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
    private const string PIPE_NAME = "skeleton_pipe1";
    //private const string PIPE_NAME = "MediaPipePoseEstimation";
    private Thread pipeClientThread;
    private bool isRunning = false;
    private Dictionary<string, PoseLandmark> poseData = new Dictionary<string, PoseLandmark>();
    private bool isConnected = false;

    [Serializable]
    private class PoseLandmark
    {
        public float x;
        public float y;
        public float z;
        public float visibility;
    }

    private void Start()
    {
        InitializeKalmanFilters();
        StartPipeClient();
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

    private void StopPipeClient()
    {
        isRunning = false;
        if (pipeClientThread != null)
        {
            pipeClientThread.Join();
        }
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
                                //Debug.Log($"Raw received data: {message.Length}");
                                if (message.Length > 5000)
                                    continue;
                                //Debug.Log($"Raw received data: {message}");  // 추가된 로그
                                if (!string.IsNullOrEmpty(message))
                                {
                                    ProcessPoseData(message);
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
            //Debug.Log($"Received JSON data: {jsonData}");
            Dictionary<string, PoseLandmark> newPoseData = JsonConvert.DeserializeObject<Dictionary<string, PoseLandmark>>(jsonData);
            //Debug.Log($"Parsed pose data count: {newPoseData.Count}");
            lock (poseData)
            {
                poseData = newPoseData;
                //Debug.Log($"포즈 데이터 처리 성공: {jsonData}");
            }
        }
        catch (Exception e)
        {
            //Debug.LogError($"포즈 데이터 처리 오류: {e.Message}\n스택 트레이스: {e.StackTrace}");
            Debug.LogError($"포즈 데이터 처리 오류: {e.Message}\n스택 트레이스: {jsonData}");
        }
    }

    private void Update()
    {
        //UpdateCharacterPose();
        UpdateBodyTransform();
    }

    public Vector3[] updatedJointPositions = new Vector3[33];

    private void UpdateCharacterPose()
    {
        if (characterpose.Instance != null)
        {
            lock (poseData)
            {
                //int visibleCountTmp = 0;

                for (int i = 0; i < 33; i++)
                {
                    string key = $"landmark_{i}";
                    if (poseData.TryGetValue(key, out PoseLandmark landmark))
                    {
                        //visibleCountTmp += landmark.visibility > 0 ? 1: 0;

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
                }

                //visibleCount = visibleCountTmp;
            }

            UpdatePositionTest(updatedJointPositions);
            characterpose.Instance.UpdateBodyPositions(positions: updatedJointPositions);
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

    private void UpdatePositionTest(Vector3[] positions)
    {
        //Debug.Log($"UpdatePositionTest : {positionTest.Length}");
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
        lock (poseData)
        {
            for (int i = 0; i < 33; i++)
            {
                string key = $"landmark_{i}";
                if (poseData.TryGetValue(key, out PoseLandmark landmark))
                {
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
            }
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
}