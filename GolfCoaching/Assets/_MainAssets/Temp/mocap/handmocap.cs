using UnityEngine;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;

public class handmocap : MonoBehaviour
{
    private const string PIPE_NAME = "MediaPipeHandEstimation";
    private Thread pipeClientThread;
    private bool isRunning = false;
    private Dictionary<string, HandLandmark> handsData = new Dictionary<string, HandLandmark>();

    [Serializable]
    private class HandLandmark
    {
        public float x;
        public float y;
        public float z;
        public int type;
    }

    private void Start()
    {
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
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.In))
                {
                    Debug.Log("손 서버 연결을 대기 중입니다...");
                    pipeClient.Connect(5000); // 5초 타임아웃
                    Debug.Log("손 서버와 연결되었습니다.");

                    using (StreamReader sr = new StreamReader(pipeClient))
                    {
                        while (isRunning)
                        {
                            string message = sr.ReadLine();
                            if (!string.IsNullOrEmpty(message))
                            {
                                ProcessHandsData(message);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"손 파이프 클라이언트 오류: {e.Message}");
                Thread.Sleep(1000); // 재연결 전 1초 대기
            }
        }
    }

    private void ProcessHandsData(string jsonData)
    {
        try
        {
            List<HandLandmark> handLandmarks = JsonConvert.DeserializeObject<List<HandLandmark>>(jsonData);
            lock (handsData)
            {
                handsData.Clear();
                for (int i = 0; i < handLandmarks.Count; i++)
                {
                    handsData[$"landmark_{i}"] = handLandmarks[i];
                }
            }
            Debug.Log($"파싱된 손 데이터 개수: {handLandmarks.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"손 데이터 처리 오류: {e.Message}\n{e.StackTrace}");
        }
    }

    // private void Update()
    // {
    //     UpdateHandPositions();
    // }

    private void UpdateHandPositions()
    {
        lock (handsData)
        {
            if (handsData.Count == 0)
            {
                Debug.Log("손 데이터가 비어있습니다.");
            }
            else
            {
                foreach (var kvp in handsData)
                {
                    Debug.Log($"손 랜드마크 {kvp.Key}: X={kvp.Value.x}, Y={kvp.Value.y}, Z={kvp.Value.z}");
                }
            }
        }
    }
}