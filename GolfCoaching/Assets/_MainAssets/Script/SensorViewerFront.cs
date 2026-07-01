using Enums;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SensorViewerFront : MonoBehaviour
{
    [SerializeField] SensorProcess sensor;
    [SerializeField] TextMeshProUGUI txtViewer;
    string snap;
    string get;

    bool isPause = false;

    //테스트
    /*int iGetHandDir; //각도 0~360
    int iGetHandDistance;
    int iGetShoulderDistance;
    int iGetSpineDir;
    int iGetShoulderAngle;
    int iGetWeight;
    int iGetFootDisRate;
    int iGetForearmAngle; //각도
    int iGetElbowFrontDir; //각도

    [SerializeField] TMP_InputField gapDegree;
    public ProSwingStepData swingStepData;
    */
    private void Awake()
    {
        if(txtViewer == null)
            txtViewer = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isPause)
            return;
        /*
        get = Comapare("iGetHandDistance", iGetHandDistance,  sensor.iGetHandDistance);
        get += Comapare("iGetShoulderDistance", iGetShoulderDistance, sensor.iGetShoulderDistance);
        get += "\r\n";
        get += Comapare("iGetHandDir", iGetHandDir, sensor.iGetHandDir);
        get += Comapare("iGetSpineDir", iGetSpineDir, sensor.iGetSpineDir);
        get += Comapare("iGetShoulderAngle", iGetShoulderAngle, sensor.iGetShoulderAngle);
        get += Comapare("iGetWeight", iGetWeight, sensor.iGetWeight);
        get += Comapare("iGetFootDisRate", iGetFootDisRate, sensor.iGetFootDisRate);
        get += Comapare("iGetForearmAngle", iGetForearmAngle, sensor.iGetForearmAngle);
        get += Comapare("iGetElbowFrontDir", iGetElbowFrontDir, sensor.iGetElbowFrontDir);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            iGetHandDir = sensor.iGetHandDir;
            iGetHandDistance = sensor.iGetHandDistance;
            iGetShoulderDistance = sensor.iGetShoulderDistance;
            iGetSpineDir = sensor.iGetSpineDir;
            iGetShoulderAngle = sensor.iGetShoulderAngle;
            iGetWeight = sensor.iGetWeight;
            iGetFootDisRate = sensor.iGetFootDisRate;
            iGetForearmAngle = sensor.iGetForearmAngle;
            iGetElbowFrontDir = sensor.iGetElbowFrontDir;
        }
        else if (Input.GetKeyDown(KeyCode.Delete))
        {
            iGetHandDir = -1;
            iGetHandDistance = -1;
            iGetShoulderDistance = -1;
            iGetSpineDir = -1;
            iGetShoulderAngle = -1;
            iGetWeight = -1;
            iGetFootDisRate = -1;
            iGetForearmAngle = -1;
            iGetElbowFrontDir = -1;
        }
        else if (Input.GetKeyDown(KeyCode.P))
        {
            isPause = !isPause;
        }

        get += $"\r\n\r\nHAND VP : {(sensor.handVectorIsLeft ? "LEFT" : "RIGHT")}"
            + $"\r\nS DIS : {sensor.DistanceAdressCenterShoulder}";
            //+ $"\r\nL Shoulder : {sensor.GetLandmarkPosition(true, 11).ToString()}"
            //+ $"\r\nR Shoulder : {sensor.GetLandmarkPosition(true, 12).ToString()}";
        */
        get = $"iGetHandDistance:{sensor.iGetHandDistance}\r\n";
        get += $"iGetShoulderDistance:{sensor.iGetShoulderDistance}\r\n";
        get += "\r\n";
        get += $"iGetHandDir:{sensor.iGetHandDir}\r\n";
        get += $"iGetSpineDir:{sensor.iGetSpineDir}\r\n";
        get += $"iGetShoulderAngle:{sensor.iGetShoulderAngle}\r\n";
        get += $"iGetWeight:{sensor.iGetWeight}\r\n";
        get += $"iGetFootDisRate:{sensor.iGetFootDisRate}\r\n";
        get += $"iGetForearmAngle:{sensor.iGetForearmAngle}\r\n";
        get += $"iGetElbowFrontDir:{sensor.iGetElbowFrontDir}\r\n";
        get += $"iGetShoulderFrontDirWorld:{sensor.iGetShoulderFrontDirWorld}\r\n";
        get += $"iGetPelvisFrontDirWorld:{sensor.iGetPelvisFrontDirWorld}\r\n";
        get += $"iGetNoseDir:{sensor.iGetNoseDir}\r\n";
        get += $"\r\n";
        get += $"handVectorIsLeft:{sensor.handVectorIsLeft}\r\n";
        get += $"IsAddressHand():{sensor.IsAddressHand()}\r\n";
        get += $"\r\n";
        get += $"MediaPipe:{Utillity.Instance.mediapipeMode}\r\n";
        txtViewer.text = get;
        
        if (Input.GetKeyDown(KeyCode.P))
        {
            isPause = !isPause;
        }
    }
    public void SetGetProValue(SWINGSTEP step)
    { }
    /*
    public void SetGetProValue(SWINGSTEP step)
    {
        Dictionary<string, int> dicStep = new Dictionary<string, int>();
        if(step == SWINGSTEP.ADDRESS)
            dicStep = swingStepData.dicAddress;
        else if (step == SWINGSTEP.TAKEBACK)
            dicStep = swingStepData.dicTakeback;
        else if (step == SWINGSTEP.BACKSWING)
            dicStep = swingStepData.dicBackswing;
        else if (step == SWINGSTEP.TOP)
            dicStep = swingStepData.dicTop;
        else if (step == SWINGSTEP.DOWNSWING)
            dicStep = swingStepData.dicDownswing;
        else if (step == SWINGSTEP.IMPACT)
            dicStep = swingStepData.dicImpact;
        else if (step == SWINGSTEP.FOLLOW)
            dicStep = swingStepData.dicFollow;
        else if (step == SWINGSTEP.FINISH)
            dicStep = swingStepData.dicFinish;
        else
            dicStep = swingStepData.dicAddress;

        iGetHandDir = dicStep["GetHandDir"];
        iGetHandDistance = dicStep["GetHandDistance"];
        iGetShoulderDistance = dicStep["GetShoulderDistance"];
        iGetSpineDir = dicStep["GetSpineDir"];
        iGetShoulderAngle = dicStep["GetShoulderAngle"];
        iGetWeight = dicStep["GetWeight"];
        iGetFootDisRate = dicStep["GetFootDisRate"];
        iGetForearmAngle = dicStep["GetForearmAngle"];
        iGetElbowFrontDir = dicStep["GetElbowFrontDir"];
    }

    string Comapare(string title, int post, int org)
    {
        
        try
        {
            int val = int.Parse(gapDegree.text);
            string postCol = "";
            if ((org <= post + val) && (org >= post - val))
                postCol = "green";
            else
                postCol = "red";
            return $"<color={postCol}>{title} : {post} / {org}</color>\r\n";
        }
        catch { return title; }
    }
    */
}
