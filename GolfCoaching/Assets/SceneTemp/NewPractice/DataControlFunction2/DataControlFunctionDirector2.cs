
using System.Globalization;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Net;
using System.Text.RegularExpressions;
using UnityEngine.Timeline;
using UnityEngine.SocialPlatforms;
using UnityEngine.Scripting;
using Unity.Collections;
using TMPro;
using UnityEngine.Video;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Jobs;
using Random = Unity.Mathematics.Random;
using System.Collections.Concurrent;
//using Michsky.LSS;
/* using UnityEngine.Windows.WebCam; */
//using Firebase;
//using Firebase.Firestore;
//using Firebase.Storage;
// using Newtonsoft.Json;
// using System.IO.Ports;
// using MEC;
// using MoreMountains.Feedbacks;
// using DG.Tweening;
// using UnityEngine.AdaptivePerformance;
// using UnityEngine.AddressableAssets;
// using UnityEngine.ResourceManagement.AsyncOperations;
public class DataControlFunctionDirector2 : MonoBehaviour
{
    private const string fileName = "testData.csv";

    // CSV 파일에 "test" 문자열을 저장하는 함수
    public void SaveTestToCsv()
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("test");
            }
            Debug.Log($"데이터가 성공적으로 저장되었습니다: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 저장 중 오류 발생: {e.Message}");
        }
    }

    // CSV 파일에서 첫 번째 셀의 데이터를 읽어오는 함수
    public string ReadTestFromCsv()
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            if (File.Exists(filePath))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line = reader.ReadLine();
                    if (!string.IsNullOrEmpty(line))
                    {
                        string[] values = line.Split(',');
                        if (values.Length > 0)
                        {
                            Debug.Log($"읽어온 데이터: {values[0]}");
                            return values[0];
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("파일이 존재하지 않습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 읽기 중 오류 발생: {e.Message}");
        }

        return string.Empty;
    }

    private void Start()
    {
        // 데이터 저장
        SaveTestToCsv();

        // 데이터 읽기
        string data = ReadTestFromCsv();
    }
}
