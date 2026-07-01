using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class AngleSync : MonoBehaviour
{

    //[SerializeField] mocapFront mocapFront;
    //[SerializeField] mocapSide mocapSide;
    [SerializeField] SensorProcess sensor;
    //[SerializeField] Transform debugAngleUser;
    //[SerializeField] Transform debugAngleModel;
    [SerializeField] Animator animator;
    [SerializeField] Slider slider;
    [SerializeField] TextMeshProUGUI txtAngle;
    [SerializeField] TextMeshProUGUI txtDebug;

    [SerializeField] Transform lShoulder;
    [SerializeField] Transform rShoulder;
    [SerializeField] Transform hand;

    bool reverse = false;
    float angleVaue = 0;
    float modelAngle = 0;
    float setVal = 0;
    bool isFinish = false;

    [SerializeField] float backwardValue = 1f;
    [SerializeField] float forwardValue = 1f;

    [Space(10)]
    [SerializeField] Transform BoneRightSoulder;
    [SerializeField] Transform DebugElbow;

    [SerializeField] Transform FSouder;
    [SerializeField] Transform FElbow;
    [SerializeField] Transform SSouder;
    [SerializeField] Transform SElbow;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator.Play("midiron_full");
    }

    /*private void FixedUpdate()
    {
        Vector3 FrontDIr = mocapFront.GetMocapPosition(14) - mocapFront.GetMocapPosition(12);// FElbow.position - FSouder.position;
        Vector3 SideDIr = mocapSide.GetMocapPosition(14) - mocapSide.GetMocapPosition(12);//SElbow.position - SSouder.position;
        FrontDIr.Normalize();
        SideDIr.Normalize();
        Vector3 retDir = new Vector3(FrontDIr.x, Mathf.Lerp(FrontDIr.y, SideDIr.y, 0.5f), SideDIr.z);
        txtDebug.text = $"{FrontDIr}\r\n{SideDIr}\r\n{retDir}";
        DebugElbow.position = BoneRightSoulder.position + (retDir * 0.3f);
    }*/

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Reset");
            reverse = false;
            isFinish = false;
            animator.SetFloat("SwingValue", 0);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            //mocapFront.StopPipeClient();
            Application.Quit();
        }

        Vector3 sCenter = (lShoulder.position + rShoulder.position) / 2f;
        Vector3 dir = hand.position - sCenter;

#if UNITY_EDITOR
        angleVaue = slider.value;
#else
        //angleVaue = mocapFront.GetHandDir();
        angleVaue = sensor.iGetHandDir;
#endif
        txtAngle.text = angleVaue.ToString("0");

        modelAngle = Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
        
        //debugAngleUser.transform.position = sCenter;
        //debugAngleModel.transform.position = sCenter;
        
        //debugAngleModel.transform.eulerAngles = new Vector3(0, 0, modelAngle);
        //debugAngleUser.transform.eulerAngles = new Vector3(0, 0, angleVaue);

        if (reverse == false && angleVaue < 60)
            reverse = true;


        //a :0 / top : 4.5, fend =  0.74
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(modelAngle, angleVaue));
        float t = Mathf.InverseLerp(0f, 10, angleDiff);
        
        if (reverse == false)
        {
            float back = backwardValue * t;
            if (angleDiff > 1)            
            {
                // 180 -> 60
                if (modelAngle > angleVaue)
                {
                    //Debug.Log($"+:{angleDiff} / {t} / {Time.deltaTime * back}");
                    setVal = Mathf.Clamp(animator.GetFloat("SwingValue") + Time.deltaTime * back, 0, 0.45f);
                    animator.SetFloat("SwingValue", Mathf.Lerp(animator.GetFloat("SwingValue"), setVal, t));
                }
                else if (modelAngle < angleVaue)
                {
                    //Debug.Log($"-:{angleDiff} / {t} / {Time.deltaTime * back}");
                    setVal = Mathf.Clamp(animator.GetFloat("SwingValue") - Time.deltaTime * back, 0, 0.45f);
                    animator.SetFloat("SwingValue", Mathf.Lerp(animator.GetFloat("SwingValue"), setVal, t));
                }
                
            }
        }
        else// if (reverse == false)
        {
            if (isFinish == true)
            {
                setVal = animator.GetFloat("SwingValue") + Time.deltaTime * forwardValue;
                animator.SetFloat("SwingValue", Mathf.Lerp(animator.GetFloat("SwingValue"), setVal, 0.85f));

                if (Mathf.Abs(Mathf.DeltaAngle(180, angleVaue)) < 10)
                {
                    Debug.Log("Reset");
                    reverse = false;
                    isFinish = false;
                    animator.SetFloat("SwingValue", 0);
                }
            }
            else
            {
                if (angleVaue > 285)
                {
                    isFinish = true;
                }
                else
                {
                    float forward = forwardValue * t;
                    if (angleDiff > 1)
                    {
                        //Debug.Log($"{angleDiff} / {t}");
                        // 60 -> 270
                        if (modelAngle > angleVaue)
                        {
                            setVal = Mathf.Clamp(animator.GetFloat("SwingValue") - Time.deltaTime * forward, 0.45f, 0.74f);
                            animator.SetFloat("SwingValue", Mathf.Lerp(animator.GetFloat("SwingValue"), setVal, t));
                        }
                        else if (modelAngle < angleVaue)
                        {
                            setVal = Mathf.Clamp(animator.GetFloat("SwingValue") + Time.deltaTime * forward, 0.45f, 0.74f);
                            animator.SetFloat("SwingValue", Mathf.Lerp(animator.GetFloat("SwingValue"), setVal, t));
                        }

                    }
                }                
            }
        }
        
    }
}
