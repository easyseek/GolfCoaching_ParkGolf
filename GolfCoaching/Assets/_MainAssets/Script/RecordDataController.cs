using DG.Tweening.Plugins.Core.PathCore;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using Enums;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RecordDataController : MonoBehaviour
{
    [SerializeField] mocapFront mocapFront;
    [SerializeField] mocapSide mocapSide;

    [SerializeField] TextMeshProUGUI[] txtPoseNames;
    [SerializeField] TextMeshProUGUI txtDebug;
    [SerializeField] TextMeshProUGUI txtGetHandDirCenter;
    [SerializeField] Button btnRecFull;
    [SerializeField] TextMeshProUGUI txtRecFull;
    [SerializeField] Toggle tglCheck;

    SWINGSTEP swingStep = SWINGSTEP.ADDRESS;
    float _timer = 0;
    bool isRecord = false;
    bool isFullProcess = false;

    //Front
    List<int> saveData_GetHandDir = new List<int>();
    List<float> saveData_GetHandDistance = new List<float>();
    List<int> saveData_GetShoulderDir = new List<int>();
    List<int> saveData_GetPelvisDir = new List<int>();
    List<int> saveData_GetBackboneDir = new List<int>();
    List<int> saveData_GetShoulderDir_Other = new List<int>();
    List<int> saveData_GetPelvisDir_Other = new List<int>();
    List<int> saveData_GetSpineDir = new List<int>();
    List<int> saveData_GetShoulderAngle = new List<int>();
    List<int> saveData_GetHeadDir = new List<int>();
    List<int> saveData_GetFootDisRate = new List<int>();
    List<float> saveData_GetWeight = new List<float>();
    List<int> saveData_GetForearmAngle = new List<int>();

    //Side
    List<int> saveData_GetHandSideDir = new List<int>();
    List<int> saveData_GetWaistSideDir = new List<int>();
    List<int> saveData_GetKneeSideDir = new List<int>();
    List<int> saveData_GetElbowSideDir = new List<int>();
    List<int> saveData_GetArmpitDir = new List<int>();

    Dictionary<string, float[]> ResultProData = new Dictionary<string, float[]>();

    int _iGetHandDir;
    float _fGetHandDistance;
    int _iGetShoulderDir;
    int _iGetPelvisDir;
    int _iGetBackboneDir;
    int _iGetShoulderDir_Other;
    int _iGetPelvisDir_Other;
    int _iGetSpineDir;
    int _iGetShoulderAngle;
    int _iGetHeadDir;
    int _iGetFootDisRate;
    float _fGetWeight;
    int _iGetForearmAngle;

    //Side
    int _iGetHandSideDir;
    int _iGetWaistSideDir;
    int _iGetKneeSideDir;
    int _iGetElbowSideDir;
    int _iGetArmpitDir;

    [Header("* FRONT TEXT")]
    [SerializeField] TextMeshProUGUI txtGetHandDir;
    [SerializeField] TextMeshProUGUI txtGetHandDistance;
    [SerializeField] TextMeshProUGUI txtGetShoulderDir;
    [SerializeField] TextMeshProUGUI txtGetPelvisDir;
    [SerializeField] TextMeshProUGUI txtGetBackboneDir;
    [SerializeField] TextMeshProUGUI txtGetShoulderDir_Other;
    [SerializeField] TextMeshProUGUI txtGetPelvisDir_Other;
    [SerializeField] TextMeshProUGUI txtGetSpineDir;
    [SerializeField] TextMeshProUGUI txtGetShoulderAngle;
    [SerializeField] TextMeshProUGUI txtGetHeadDir;
    [SerializeField] TextMeshProUGUI txtGetFootDisRate;
    [SerializeField] TextMeshProUGUI txtGetWeight;
    [SerializeField] TextMeshProUGUI txtGetForearmAngle;

    [Header("* SIDE TEXT")]
    [SerializeField] TextMeshProUGUI txtGetHandSideDir;
    [SerializeField] TextMeshProUGUI txtGetWaistSideDir;
    [SerializeField] TextMeshProUGUI txtGetKneeSideDir;
    [SerializeField] TextMeshProUGUI txtGetElbowSideDir;
    [SerializeField] TextMeshProUGUI txtGetArmpitDir;


    string path;

    // Start is called once before the first execution of Update after the MonoBehaviour is create
    void Start()
    {
        ResetPoseNameColor();
        swingStep = SWINGSTEP.ADDRESS;
        SetPoseNameColor((int)swingStep, Color.yellow);
        //StartCoroutine(PoseNameProcess());

        path = Application.dataPath + "/Record";
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        Debug.Log("path:" + path);
    }

    // Update is called once per frame
    void Update()
    {
        txtGetHandDirCenter.text = mocapFront.GetHandDir().ToString("00");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(CoPoseRecord());
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if ((int)swingStep == 0 || isRecord)
                return;

            SetPoseNameColor((int)swingStep, Color.white);
            swingStep = swingStep - 1;
            SetPoseNameColor((int)swingStep, Color.yellow);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if ((int)swingStep == (int)SWINGSTEP.FINISH || isRecord)
                return;

            SetPoseNameColor((int)swingStep, Color.white);
            swingStep = swingStep + 1;
            SetPoseNameColor((int)swingStep, Color.yellow);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"Quit()");
            mocapFront.StopPipeClient();
            mocapSide.StopPipeClient();
            Application.Quit();
        }

        //front
        _iGetHandDir = (int)mocapFront.GetHandDir();
        _fGetHandDistance = (float)mocapFront.GetHandDistance();
        _iGetShoulderDir = (int)mocapFront.GetShoulderDir();
        _iGetPelvisDir = (int)mocapFront.GetPelvisDir();
        _iGetBackboneDir = (int)mocapFront.GetBackboneDir();
        _iGetShoulderDir_Other = (int)mocapFront.GetShoulderDir_Other();
        _iGetPelvisDir_Other = (int)mocapFront.GetPelvisDir_Other();
        _iGetSpineDir = (int)mocapFront.GetSpineDir();
        _iGetShoulderAngle = (int)mocapFront.GetShoulderAngle();
        _iGetHeadDir = (int)mocapFront.GetHeadDir();
        _iGetFootDisRate = (int)mocapFront.GetFootDisRate();
        _fGetWeight = (float)mocapFront.GetWeight();
        _iGetForearmAngle = (int)mocapFront.GetForearmAngle();

        //Side
        _iGetHandSideDir = (int)mocapSide.GetHandSideDir();
        _iGetWaistSideDir = (int)mocapSide.GetWaistSideDir();
        _iGetKneeSideDir = (int)mocapSide.GetKneeSideDir();
        _iGetElbowSideDir = (int)mocapSide.GetElbowSideDir();
        _iGetArmpitDir = (int)mocapSide.GetArmpitDir();

        txtGetHandDir.text = "[GetHandDir] " + (tglCheck.isOn? ResultProData["GetHandDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetHandDir.ToString();
        txtGetHandDistance.text = "[GetHandDistance] " + (tglCheck.isOn ? ResultProData["GetHandDistance"][(int)swingStep].ToString("0.0") + "|" : "") + _fGetHandDistance.ToString("0.0");
        txtGetShoulderDir.text = "[GetShoulderDir] " + (tglCheck.isOn ? ResultProData["GetShoulderDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetShoulderDir.ToString();
        txtGetPelvisDir.text = "[GetPelvisDir] " + (tglCheck.isOn ? ResultProData["GetPelvisDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetPelvisDir.ToString();
        txtGetBackboneDir.text = "[GetBackboneDir] " + (tglCheck.isOn ? ResultProData["GetBackboneDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetBackboneDir.ToString();
        txtGetShoulderDir_Other.text = "[GetShoulderDir_Other] " + (tglCheck.isOn ? ResultProData["GetShoulderDir_Other"][(int)swingStep].ToString("0") + "|" : "") + _iGetShoulderDir_Other.ToString();
        txtGetPelvisDir_Other.text = "[GetPelvisDir_Other] " + (tglCheck.isOn ? ResultProData["GetPelvisDir_Other"][(int)swingStep].ToString("0") + "|" : "") + _iGetPelvisDir_Other.ToString();
        txtGetSpineDir.text = "[GetSpineDir] " + (tglCheck.isOn ? ResultProData["GetSpineDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetSpineDir.ToString();
        txtGetShoulderAngle.text = "[GetShoulderAngle] " + (tglCheck.isOn ? ResultProData["GetShoulderAngle"][(int)swingStep].ToString("0") + "|" : "") + _iGetShoulderAngle.ToString();
        txtGetHeadDir.text = "[GetHeadDir] " + (tglCheck.isOn ? ResultProData["GetHeadDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetHeadDir.ToString();
        txtGetFootDisRate.text = "[GetFootDisRate] " + (tglCheck.isOn ? ResultProData["GetFootDisRate"][(int)swingStep].ToString("0") + "|" : "") + _iGetFootDisRate.ToString();
        txtGetWeight.text = "[GetWeight] " + (tglCheck.isOn ? ResultProData["GetWeight"][(int)swingStep].ToString("0.0") + "|" : "") + _fGetWeight.ToString("0.0");
        txtGetForearmAngle.text = "[GetForearmAngle] " + (tglCheck.isOn ? ResultProData["GetForearmAngle"][(int)swingStep].ToString("0.0") + "|" : "") + _iGetForearmAngle.ToString("0.0");

        //Side
        txtGetHandSideDir.text = (tglCheck.isOn ? ResultProData["GetHandSideDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetHandSideDir.ToString() + " [GetHandSideDir]";
        txtGetWaistSideDir.text = (tglCheck.isOn ? ResultProData["GetWaistSideDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetWaistSideDir.ToString() + " [GetWaistSideDir]";
        txtGetKneeSideDir.text = (tglCheck.isOn ? ResultProData["GetKneeSideDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetKneeSideDir.ToString() + " [GetKneeSideDir]";
        txtGetElbowSideDir.text = (tglCheck.isOn ? ResultProData["GetElbowSideDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetElbowSideDir.ToString() + " [GetElbowSideDir]";
        txtGetArmpitDir.text = (tglCheck.isOn ? ResultProData["GetArmpitDir"][(int)swingStep].ToString("0") + "|" : "") + _iGetArmpitDir.ToString() + " [GetArmpitDir]";
    }

    public void OnClick_RecFULL()
    {
        if (isFullProcess == false)
            StartCoroutine(CoRecFull());
        else
        {
            isFullProcess = false;
        }
    }

    public void OnClick_Quit()
    {
        StartCoroutine(CoQuit());
    }

    IEnumerator CoQuit()
    {
        mocapFront.StopPipeClient();
        mocapSide.StopPipeClient();

        yield return new WaitUntil(() => (mocapFront.IsAlive() == false && mocapSide.IsAlive() == false));

        GameManager.Instance.Mode = EStep.Realtime;
        GameManager.Instance.SelectedSceneName = string.Empty;
        SceneManager.LoadScene("ModeSelect");
    }

    IEnumerator CoRecFull()
    {
        isFullProcess = true;
        txtRecFull.text = "CANCEL";

        ResultProData.Clear();
        //Front
        ResultProData.Add("GetHandDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetHandDistance", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetShoulderDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetPelvisDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetBackboneDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetShoulderDir_Other", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetPelvisDir_Other", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetSpineDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetShoulderAngle", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetHeadDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetFootDisRate", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetWeight", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetForearmAngle", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        //Side
        ResultProData.Add("GetHandSideDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetWaistSideDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetKneeSideDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetElbowSideDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        ResultProData.Add("GetArmpitDir", new float[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

        ResetPoseNameColor();
        swingStep = SWINGSTEP.ADDRESS;
        SetPoseNameColor((int)swingStep, Color.yellow);

        txtDebug.text = "READY POSITION\r\n";
        yield return new WaitForSeconds(0.5f);
        txtDebug.text += "5\r\n";
        yield return new WaitForSeconds(1);
        txtDebug.text += "4\r\n";
        yield return new WaitForSeconds(1);
        txtDebug.text += "3\r\n";
        yield return new WaitForSeconds(1);
        txtDebug.text += "2\r\n";
        yield return new WaitForSeconds(1);
        txtDebug.text += "1\r\n";
        yield return new WaitForSeconds(1);

        yield return StartCoroutine(CoPoseRecord());

        while (isFullProcess)
        {
            SetPoseNameColor((int)swingStep, Color.white);
            swingStep = swingStep + 1;
            SetPoseNameColor((int)swingStep, Color.yellow);

            yield return StartCoroutine(CoPoseRecord());
        }

        SaveCsvFull();

        isFullProcess = false;
        txtRecFull.text = "FULL";
    }


    IEnumerator CoPoseRecord()
    {
        txtDebug.text = swingStep.ToString() + " Set Pose\r\n";
        isRecord = true;
        SetPoseNameColor((int)swingStep, Color.red);
        int frame = 0;
        SaveData_Clear();

        txtDebug.text += "3\r\n";
        yield return new WaitForSeconds(1);
        txtDebug.text += "2\r\n";
        yield return new WaitForSeconds(1);
        txtDebug.text += "1\r\n";
        yield return new WaitForSeconds(1);

        txtDebug.text += "Record Start\r\n";
        
        txtDebug.text += "0%";

        while (frame < 30)
        {
            
            //1프레임 데이터 저장
            //Front
            saveData_GetHandDir.Add(_iGetHandDir);
            saveData_GetHandDistance.Add(_fGetHandDistance);
            saveData_GetShoulderDir.Add(_iGetShoulderDir);
            saveData_GetPelvisDir.Add(_iGetPelvisDir);
            saveData_GetBackboneDir.Add(_iGetBackboneDir);
            saveData_GetShoulderDir_Other.Add(_iGetShoulderDir_Other);
            saveData_GetPelvisDir_Other.Add(_iGetPelvisDir_Other);
            saveData_GetSpineDir.Add(_iGetSpineDir);
            saveData_GetShoulderAngle.Add(_iGetShoulderAngle);
            saveData_GetHeadDir.Add(_iGetHeadDir);
            saveData_GetFootDisRate.Add(_iGetFootDisRate);
            saveData_GetWeight.Add(_fGetWeight);
            saveData_GetForearmAngle.Add(_iGetForearmAngle);
            //Side
            saveData_GetHandSideDir.Add(_iGetHandSideDir);
            saveData_GetWaistSideDir.Add(_iGetWaistSideDir);
            saveData_GetKneeSideDir.Add(_iGetKneeSideDir);
            saveData_GetElbowSideDir.Add(_iGetElbowSideDir);
            saveData_GetArmpitDir.Add(_iGetArmpitDir);

            frame++;
            if(frame == 14)
                txtDebug.text += "50%";
            if (frame % 2 == 0)
                txtDebug.text += "-";

            yield return null;
        }
        txtDebug.text += "100%";
        AudioManager.Instance.PlayNext();

        txtDebug.text += "\r\n";
        txtDebug.text += "Record End\r\n";

        ResultDataAdd((int)swingStep);
        //SaveCsv();
        

        //txtDebug.text += "Saved\r\n";

        SetPoseNameColor((int)swingStep, Color.green);

        txtDebug.text += swingStep.ToString() + " End\r\n";
        yield return new WaitForSeconds(0.5f);
        isRecord = false;
        if (swingStep == SWINGSTEP.FINISH)
            isFullProcess = false;
    }

    void ResultDataAdd(int step)
    {
        //Front
        ResultProData["GetHandDir"][step] = (float)saveData_GetHandDir.Average();
        ResultProData["GetHandDistance"][step] = (float)saveData_GetHandDistance.Average();
        ResultProData["GetShoulderDir"][step] = (float)saveData_GetShoulderDir.Average();
        ResultProData["GetPelvisDir"][step] = (float)saveData_GetPelvisDir.Average();
        ResultProData["GetBackboneDir"][step] = (float)saveData_GetBackboneDir.Average();
        ResultProData["GetShoulderDir_Other"][step] = (float)saveData_GetShoulderDir_Other.Average();
        ResultProData["GetPelvisDir_Other"][step] = (float)saveData_GetPelvisDir_Other.Average();
        ResultProData["GetSpineDir"][step] = (float)saveData_GetSpineDir.Average();
        ResultProData["GetShoulderAngle"][step] = (float)saveData_GetShoulderAngle.Average();
        ResultProData["GetHeadDir"][step] = (float)saveData_GetHeadDir.Average();
        ResultProData["GetFootDisRate"][step] = (float)saveData_GetFootDisRate.Average();
        ResultProData["GetWeight"][step] =  saveData_GetWeight.Average();
        ResultProData["GetForearmAngle"][step] = (float)saveData_GetForearmAngle.Average();

        //Side
        ResultProData["GetHandSideDir"][step] = (float)saveData_GetHandSideDir.Average();
        ResultProData["GetWaistSideDir"][step] = (float)saveData_GetWaistSideDir.Average();
        ResultProData["GetKneeSideDir"][step] = (float)saveData_GetKneeSideDir.Average();
        ResultProData["GetElbowSideDir"][step] = (float)saveData_GetElbowSideDir.Average();
        ResultProData["GetArmpitDir"][step] = (float)saveData_GetArmpitDir.Average();
        
    }

    void SaveCsvFull()
    {
        try
        {
            string output = string.Empty;
            output += "NAME,ADDRESS,TACKEBACK,BACKSWING,TOP,DOWNSWING,IMPACT,FOLLOW,FINISH\r\n";
            //Front
            output += "GetHandDir," + string.Join(",", ResultProData["GetHandDir"]) + "\r\n";
            output += "GetHandDistance," + string.Join(",", ResultProData["GetHandDistance"]) + "\r\n";
            output += "GetShoulderDir," + string.Join(",", ResultProData["GetShoulderDir"]) + "\r\n";
            output += "GetPelvisDir," + string.Join(",", ResultProData["GetPelvisDir"]) + "\r\n";
            output += "GetBackboneDir," + string.Join(",", ResultProData["GetBackboneDir"]) + "\r\n";
            output += "GetShoulderDir_Other," + string.Join(",", ResultProData["GetShoulderDir_Other"]) + "\r\n";
            output += "GetPelvisDir_Other," + string.Join(",", ResultProData["GetPelvisDir_Other"]) + "\r\n";
            output += "GetSpineDir," + string.Join(",", ResultProData["GetSpineDir"]) + "\r\n";
            output += "GetShoulderAngle," + string.Join(",", ResultProData["GetShoulderAngle"]) + "\r\n";
            output += "GetHeadDir," + string.Join(",", ResultProData["GetHeadDir"]) + "\r\n";
            output += "GetFootDisRate," + string.Join(",", ResultProData["GetFootDisRate"]) + "\r\n";
            output += "GetWeight," + string.Join(",", ResultProData["GetWeight"]) + "\r\n";
            output += "GetForearmAngle," + string.Join(",", ResultProData["GetForearmAngle"]) + "\r\n";
            //Side
            output += "GetHandSideDir," + string.Join(",", ResultProData["GetHandSideDir"]) + "\r\n";
            output += "GetWaistSideDir," + string.Join(",", ResultProData["GetWaistSideDir"]) + "\r\n";
            output += "GetKneeSideDir," + string.Join(",", ResultProData["GetKneeSideDir"]) + "\r\n";
            output += "GetElbowSideDir," + string.Join(",", ResultProData["GetElbowSideDir"]) + "\r\n";
            output += "GetArmpitDir," + string.Join(",", ResultProData["GetArmpitDir"]);

            //Debug.Log(output);
            string filepath = path + "/" + "FULL_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            File.WriteAllText(filepath, output);
            Debug.Log("Save File : " + filepath);
        }
        catch (Exception e)
        {
            txtDebug.text = "Failed:" + e.Message;
        };

    }

    void SaveCsv()
    {
        try
        {
            string output = string.Empty;
            //Front
            output += "GetHandDir," + string.Join(",", saveData_GetHandDir) + "\r\n";
            output += "GetHandDistance," + string.Join(",", saveData_GetHandDistance) + "\r\n";
            output += "GetShoulderDir," + string.Join(",", saveData_GetShoulderDir) + "\r\n";
            output += "GetPelvisDir," + string.Join(",", saveData_GetPelvisDir) + "\r\n";
            output += "GetBackboneDir," + string.Join(",", saveData_GetBackboneDir) + "\r\n";
            output += "GetShoulderDir_Other," + string.Join(",", saveData_GetShoulderDir_Other) + "\r\n";
            output += "GetPelvisDir_Other," + string.Join(",", saveData_GetPelvisDir_Other) + "\r\n";
            output += "GetSpineDir," + string.Join(",", saveData_GetSpineDir) + "\r\n";
            output += "GetShoulderAngle," + string.Join(",", saveData_GetShoulderAngle) + "\r\n";
            output += "GetHeadDir," + string.Join(",", saveData_GetHeadDir) + "\r\n";
            output += "GetFootDisRate," + string.Join(",", saveData_GetFootDisRate) + "\r\n";
            output += "GetWeight," + string.Join(",", saveData_GetWeight) + "\r\n";
            //Side
            output += "GetHandSideDir," + string.Join(",", saveData_GetHandSideDir) + "\r\n";
            output += "GetWaistSideDir," + string.Join(",", saveData_GetWaistSideDir) + "\r\n";
            output += "GetKneeSideDir," + string.Join(",", saveData_GetKneeSideDir) + "\r\n";
            output += "GetElbowSideDir," + string.Join(",", saveData_GetElbowSideDir) + "\r\n";
            output += "GetArmpitDir," + string.Join(",", saveData_GetArmpitDir);

            //Debug.Log(output);
            string filepath = path + "/" + swingStep.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".csv";
            File.WriteAllText(filepath, output);
            Debug.Log("Save File : " + filepath);
        }
        catch (Exception e)
        {
            txtDebug.text = "Failed:"+ e.Message;
        };      

    }

    void SaveData_Clear()
    {
        //Front
        saveData_GetHandDir.Clear();
        saveData_GetHandDistance.Clear();
        saveData_GetShoulderDir.Clear();
        saveData_GetPelvisDir.Clear();
        saveData_GetBackboneDir.Clear();
        saveData_GetShoulderDir_Other.Clear();
        saveData_GetPelvisDir_Other.Clear();
        saveData_GetSpineDir.Clear();
        saveData_GetShoulderAngle.Clear();
        saveData_GetHeadDir.Clear();
        saveData_GetFootDisRate.Clear();
        saveData_GetWeight.Clear();
        saveData_GetForearmAngle.Clear();
        //Side
        saveData_GetHandSideDir.Clear();
        saveData_GetWaistSideDir.Clear();
        saveData_GetKneeSideDir.Clear();
        saveData_GetElbowSideDir.Clear();
        saveData_GetArmpitDir.Clear();
    }

    void ResetPoseNameColor()
    {
        for (int i = 0; i < txtPoseNames.Length; i++)
        {
            SetPoseNameColor(i, Color.white);
        }
    }

    void SetPoseNameColor(int poseIdx, Color col)
    {
        txtPoseNames[poseIdx].color = col;
    }

}
