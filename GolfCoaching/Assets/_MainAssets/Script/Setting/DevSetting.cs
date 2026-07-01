using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class DevSetting : MonoBehaviour
{
    [SerializeField] TMP_InputField inputFrontCamID;
    [SerializeField] TMP_InputField inputSideCamID;

    [SerializeField] TMP_InputField inputFrontDisVal;
    [SerializeField] TMP_InputField inputSideDisVal;
    [SerializeField] TextMeshProUGUI txtFrontDisRate;
    [SerializeField] TextMeshProUGUI txtSideDisRate;
    [SerializeField] TextMeshProUGUI txtTwoCamRate;

    [SerializeField] TMP_InputField inputSideAngleOffset;

    [SerializeField] TMP_InputField inputMirrorTimeout;

    [SerializeField] Toggle toggleLesson;
    [SerializeField] Toggle togglePractice;
    [SerializeField] Toggle toggleAICoaching;
    [SerializeField] Toggle toggleMirror;
    [SerializeField] Toggle toggleRange;

    [SerializeField] Toggle toggleStudio;

    [SerializeField] TextMeshProUGUI txtDevList;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputFrontCamID.onValueChanged.AddListener(OnValueChanged_FrontID);
        LoadValue();

        if (WebCamTexture.devices.Length > 0)
        {
            txtDevList.text  = "";
            for (int i = 0; i < WebCamTexture.devices.Length; i++)
            {
                txtDevList.text  += $"{i} - {WebCamTexture.devices[i].name}\r\n";
            }
            
        }
        
    }

    // Update is called once per frame
    public void LoadValue()
    {
        inputFrontCamID.text = Utillity.Instance.frontCameraID.ToString();
        inputSideCamID.text = Utillity.Instance.sideCameraID.ToString();

        inputFrontDisVal.text = Utillity.Instance.frontPixelDistance.ToString();
        inputSideDisVal.text = Utillity.Instance.sidePixelDistance.ToString();
        txtFrontDisRate.text = Utillity.Instance.frontPixelDistanceRate.ToString();
        txtSideDisRate.text = Utillity.Instance.sidePixelDistanceRate.ToString();
        txtTwoCamRate.text = Utillity.Instance.TwoCamDIsRate.ToString();
        inputSideAngleOffset.text = Utillity.Instance.sideAngleOffset.ToString();

        inputMirrorTimeout.text = Utillity.Instance.mirrorModeTimeout.ToString();

        toggleLesson.isOn = Utillity.Instance.lessonUse;
        togglePractice.isOn = Utillity.Instance.PracticeUse;
        toggleAICoaching.isOn = Utillity.Instance.aiCoachingUse;
        toggleMirror.isOn = Utillity.Instance.mirrorUse;
        toggleRange.isOn = Utillity.Instance.RangeUse;

        toggleStudio.isOn = Utillity.Instance.studioUse;
    }

    void OnValueChanged_FrontID(string val)
    {
        /*
        if (int.TryParse(inputFrontCamID.text, out int frontID))
        {
            if (frontID == 0)
            {
                inputSideCamID.text = "1";
            }
            else if (frontID == 1)
            {
                inputSideCamID.text = "0";
            }
            else
            {
                inputFrontCamID.SetTextWithoutNotify("0");
                inputSideCamID.SetTextWithoutNotify("1");
                return;
            }
        }
        */
    }

    public void OnClick_Apply()
    {
        Utillity.Instance.frontCameraID = int.Parse(inputFrontCamID.text);
        Utillity.Instance.sideCameraID = int.Parse(inputSideCamID.text);

        Utillity.Instance.frontPixelDistance = float.Parse(inputFrontDisVal.text);
        Utillity.Instance.sidePixelDistance = float.Parse(inputSideDisVal.text);
        Utillity.Instance.CalDIstanceRate();

        Utillity.Instance.sideAngleOffset = int.Parse(inputSideAngleOffset.text);

        Utillity.Instance.mirrorModeTimeout = int.Parse(inputMirrorTimeout.text);

        /*
        Utillity.Instance.lessonUse = toggleLesson.isOn;
        Utillity.Instance.PracticeUse = togglePractice.isOn;
        Utillity.Instance.aiCoachingUse = toggleAICoaching.isOn;
        Utillity.Instance.mirrorUse = toggleMirror.isOn;
        Utillity.Instance.RangeUse = toggleRange.isOn;

        Utillity.Instance.studioUse = toggleStudio.isOn;
        */

        Utillity.Instance.SetConfig();

        //LoadValue();

        gameObject.SetActive(false);
    }
}
