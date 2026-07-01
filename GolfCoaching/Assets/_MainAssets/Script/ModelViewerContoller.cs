using TMPro;
using UnityEngine;
using Enums;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class ModelViewerContoller : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI[] txtPoseNames;
    [SerializeField] Transform camera;
    [SerializeField] Transform resetPosition;
    [SerializeField] Animator ani3DModel;

    float[] poseKeys = { 0, 0.23f, 0.35f, 0.5f, 0.61f, 0.661f, 0.76f, 0.99f };
    SWINGSTEP swingStep = SWINGSTEP.ADDRESS;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetPoseNameColor();
        swingStep = SWINGSTEP.ADDRESS;
        SetPoseNameColor((int)swingStep, Color.green);
        SetPose();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            camera.position = resetPosition.position;
            camera.rotation = resetPosition.rotation;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
            SetStep(SWINGSTEP.ADDRESS);
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetStep(SWINGSTEP.TAKEBACK);
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetStep(SWINGSTEP.BACKSWING);
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetStep(SWINGSTEP.TOP);
        else if (Input.GetKeyDown(KeyCode.Alpha5))
            SetStep(SWINGSTEP.DOWNSWING);
        else if (Input.GetKeyDown(KeyCode.Alpha6))
            SetStep(SWINGSTEP.IMPACT);
        else if (Input.GetKeyDown(KeyCode.Alpha7))
            SetStep(SWINGSTEP.FOLLOW);
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            SetStep(SWINGSTEP.FINISH);
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    void SetStep(SWINGSTEP step)
    {
        SetPoseNameColor((int)swingStep, Color.white);
        swingStep = step;
        SetPoseNameColor((int)swingStep, Color.green);
        SetPose();
    }

    void SetPose()
    {
        ani3DModel.SetFloat("SwingValue", poseKeys[(int)swingStep]);

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
