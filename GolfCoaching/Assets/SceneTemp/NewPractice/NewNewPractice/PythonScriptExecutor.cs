using UnityEngine;
using System.Diagnostics;

public class PythonScriptExecutor : MonoBehaviour
{
    private Process pythonProcess;

    void Start()
    {
        StartPythonProcess();
    }

    void StartPythonProcess()
    {
        pythonProcess = new Process();
        pythonProcess.StartInfo.FileName = "python";
        pythonProcess.StartInfo.Arguments = Application.dataPath + "/mocap/mocap.py"; // 절대 경로 설정
        pythonProcess.StartInfo.UseShellExecute = false;
        pythonProcess.StartInfo.RedirectStandardOutput = true;
        pythonProcess.StartInfo.RedirectStandardError = true;
        pythonProcess.StartInfo.CreateNoWindow = true;
        pythonProcess.Start();
    }

    void OnApplicationQuit()
    {
        StopPythonProcess();
    }

    public void StopPythonProcess()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill();
            pythonProcess.WaitForExit();  // 프로세스 종료를 기다림
            pythonProcess = null;
        }
    }
}
