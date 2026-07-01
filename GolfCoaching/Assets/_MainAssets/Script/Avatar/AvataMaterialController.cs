using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Rendering;
using static UnityEngine.Rendering.DebugUI;
using RootMotion.FinalIK;
using Enums;

public class AvataMaterialController : MonoBehaviour
{
    //CheckPart Area
    /*
    0 - nose
    2 - left eye
    5 - right eye
    1 - chest
    2 ~ 13 name
    14 - spine B
    15 - spine T
    16 - Pelvis
     */
    [SerializeField] Transform[] ProParts;
    [SerializeField] Transform[] UserParts;
    [SerializeField] Color DefaultPart = Color.white;
    [SerializeField] Color DefaultPartB = Color.black;
    [SerializeField] Color WrongPart = Color.gray;
    //[SerializeField] Color HidePart = Color.white;
    bool _showToast = false;
    //[SerializeField] mocapFront mcFront;
    //[SerializeField] mocapSide mcSide;
    [SerializeField] SensorProcess sensorProcess;
    [SerializeField] LimbIK limbIK;

    [SerializeField] GameObject WeightLeftRoot;
    [SerializeField] GameObject WeightRightRoot;
    [SerializeField] Image WeightLeft;
    [SerializeField] Image WeightRight;
    [SerializeField] TextMeshProUGUI txtWeightLeft;
    [SerializeField] TextMeshProUGUI txtWeightRight;
    [SerializeField] CanvasGroup cgBalance;


    //public Check Value - not use
    /*[HideInInspector] public float PelvisValue;
    [HideInInspector] public Vector3 PelvisVectorValue;
    [HideInInspector] public float PelvisRotValue;
    [HideInInspector] public float RightElbowValue;
    [HideInInspector] public float ChestValue;
    [HideInInspector] public float ChestHandValue;
    [HideInInspector] public float WaistValue;
    [HideInInspector] public float HeadValue;
    [HideInInspector] public float ShinLValue;
    [HideInInspector] public float ShinRValue;
    [HideInInspector] public float LeftArmVisibilityValue;
    [HideInInspector] public float AddressHandValue;
    [HideInInspector] public float AddressSpineValue;
    [HideInInspector] public float AddressKneeValue;
    [HideInInspector] public float ShoulderRotateValue;
    [HideInInspector] public float BackSwingPelvisValue;
    [HideInInspector] public float ShoulerHeightAngleValue;
    [HideInInspector] public float StanceRateValue;
    [HideInInspector] public float BalanceValue;
    */

    // front
    [HideInInspector] public float HandDir;
    [HideInInspector] public float SpineDir;
    [HideInInspector] public float ShoulderAngle;
    [HideInInspector] public float Weight;
    [HideInInspector] public float FootDisRate;
    [HideInInspector] public float ForearmAngle;
    [HideInInspector] public float ElbowFrontDir;
    [HideInInspector] public float ElbowRightFrontDir;
    //Side
    [HideInInspector] public float WaistSideDir;
    [HideInInspector] public float HandSideDir;
    [HideInInspector] public float KneeSideDir;
    [HideInInspector] public float ElbowSideDir;
    [HideInInspector] public float ArmpitDir;
    [HideInInspector] public float LeftElbowSideVis;

    //Combine
    [HideInInspector] public float ShoulderDir;
    [HideInInspector] public float PelvisDir;




    //Material Area
    [SerializeField] GameObject Body_User;
    const int _PelvisMatIndex = 9;
    const int _ForearmRMatIndex = 8;
    const int _UpperarmRMatIndex = 24;
    const int _ForearmLMatIndex = 7;
    const int _UpperarmLMatIndex = 23;
    const int _ChestMatIndex = 26;
    const int _HeadMatIndex = 0;
    const int _LHandMatIndex = 15;
    const int _RHandMatIndex = 20;
    const int _ThighLMatIndext = 3;
    const int _ThighRMatIndext = 4;
    const int _ShinLMatIndext = 5;
    const int _FootLMatIndext = 1;
    const int _ShinRMatIndext = 6;
    const int _FootRMatIndext = 2;
    Material MatPelvis;
    Material MatUpperArmR;
    Material MatForeArmR;
    Material MatUpperArmL;
    Material MatForeArmL;
    Material MatHead;
    Material MatChest;
    Material MatThighL;
    Material MatThighR;
    Material MatLHand;
    Material MatRHand;
    Material MatLShin;
    Material MatLFoot;
    Material MatRShin;
    Material MatRFoot;

    const float adjVal = 0.1f;
    private int _balanceValue;

    //On/Off
    //public bool CheakParts = false;
    bool _avatarOn = false;

    [SerializeField] GameObject UserClub;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MatPelvis = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_PelvisMatIndex];
        MatForeArmR = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ForearmRMatIndex];
        MatUpperArmR = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_UpperarmRMatIndex];
        MatForeArmL = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ForearmLMatIndex];
        MatUpperArmL = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_UpperarmLMatIndex];
        MatChest = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ChestMatIndex];
        MatHead = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_HeadMatIndex];
        MatLHand = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_LHandMatIndex];
        MatRHand = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_RHandMatIndex];
        MatThighL = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ThighLMatIndext];
        MatThighR = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ThighRMatIndext];
        MatLShin = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ShinLMatIndext];
        MatLFoot = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_FootLMatIndext];
        MatRShin = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_ShinRMatIndext];
        MatRFoot = Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[_FootRMatIndext];

        ShowAvatar(false);

        // StartCoroutine(CoCheackPart());
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.UpArrow))
        {
            ShowAvatar(true);
        }
        else if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            ShowAvatar(false);
        }
    }

    //측정값과 기준값 비교 공통함수
    float CalAngleDiff(int bas, int allow, int val, bool signed = false)
    {
        float ret;
        if (val <= bas)
        {
            ret = Mathf.InverseLerp(bas - (allow * 3), bas - allow, val);
            if (signed)
            {
                ret = -ret;
                if (Mathf.Approximately(ret, 0)) ret = -0.01f;
            }
            else if (Mathf.Approximately(ret, 0)) ret = 0.01f;

        }
        else
        {
            ret = Mathf.InverseLerp(bas + (allow * 3), bas + allow, val);
            if(Mathf.Approximately(ret, 0)) ret = 0.01f;
        }

        return ret;
    }


    public void Reset()
    {
        SetColor(MatPelvis);
        SetColor(MatForeArmR);
        SetColor(MatUpperArmR);
        SetColor(MatForeArmL);
        SetColor(MatUpperArmL);
        SetColor(MatChest);
        SetColor(MatThighL);
        SetColor(MatThighR);
        SetColor(MatHead);        
        SetColor(MatLHand, DefaultPartB);
        SetColor(MatRHand, DefaultPartB);

        SetColor(MatLShin);
        SetColor(MatLFoot);
        SetColor(MatRShin);
        SetColor(MatRFoot);

        _showToast = false;
    }



    //=============================================
    // CHECK
    //=============================================

    public void CheckGetHandDir(int bas, int allow)// 4
    {
        HandDir = CalAngleDiff(bas, allow, sensorProcess.iGetHandDir, true);
    }

    public void CheckGetSpineDir(int bas, int allow)// 5
    {
        SpineDir = CalAngleDiff(bas, allow, sensorProcess.iGetSpineDir, true);
    }

    public void CheckGetShoulderAngle(int bas, int allow)// 6
    {
        ShoulderAngle = CalAngleDiff(bas, allow, sensorProcess.iGetShoulderAngle, true);
    }

    public void CheckGetWeight(int bas, int allow)// 7
    {
        Weight = CalAngleDiff(bas, allow, sensorProcess.iGetWeight, true);//백분률 체크 부분 확인
    }

    public void CheckGetFootDisRate(int bas, int allow)// 8
    {
        FootDisRate = CalAngleDiff(bas, allow, sensorProcess.iGetFootDisRate, true);
    }

    public void CheckGetForearmAngle(int bas, int allow)// 9
    {
        ForearmAngle = CalAngleDiff(bas, allow, sensorProcess.iGetForearmAngle);
    }

    public void CheckGetElbowFrontDir(int bas, int allow)// 10
    {
        ElbowFrontDir = CalAngleDiff(bas, allow, sensorProcess.iGetElbowFrontDir);
    }

    public void CheckGetElbowRightFrontDir(int bas, int allow)// 20
    {
        ElbowRightFrontDir = CalAngleDiff(bas, allow, sensorProcess.iGetElbowRightFrontDir);
    }


    public void CheckGetWaistSideDir(int bas, int allow)// 11
    {
        WaistSideDir = CalAngleDiff(bas, allow, sensorProcess.iGetWaistSideDir, true);
    }

    public void CheckGetHandSideDir(int bas, int allow)// 12
    {
        HandSideDir = CalAngleDiff(bas, allow, sensorProcess.iGetHandSideDir, true);
    }

    public void CheckGetKneeSideDir(int bas, int allow)// 13
    {
        KneeSideDir = CalAngleDiff(bas, allow, sensorProcess.iGetKneeSideDir, true);
    }

    public void CheckGetElbowSideDir(int bas, int allow)// 14
    {
        ElbowSideDir = CalAngleDiff(bas, allow, sensorProcess.iGetElbowSideDir, true);
    }

    public void CheckGetArmpitDir(int bas, int allow)// 15
    {
        ArmpitDir = CalAngleDiff(bas, allow, sensorProcess.iGetArmpitDir, true);
    }


    /*
    public void CheckGetShoulderDir(int bas, int allow)// 16
    {
        ShoulderDir = CalAngleDiff(bas, allow, sensorProcess.iGetShoulderDir, true);
    }

    public void CheckGetPelvisDir(int bas, int allow)// 17
    {
        PelvisDir = CalAngleDiff(bas, allow, sensorProcess.iGetPelvisDir, true);
    }
    */

    public void CheckLeftElbowSideVis()
    {
        LeftElbowSideVis = sensorProcess.fLeftElbowSideVis;
    }




    //=============================================
    // VISIBLE - ALL
    //=============================================
    public void VisibleWeight_All()
    {
        if (Mathf.Abs(Weight) > 0.7f)//Utillity.Instance.dicCheckFollow["CheckBalanceC"])
        {
            //밸런스 정상
            SetColor(MatLShin);
            SetColor(MatRShin);
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
        }
        else
        {
            if (Weight < 0) //너무 왼쪽
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB08"));
                Utillity.Instance.ShowGuideArrow("TB08");
                SetColor(MatRShin, Color.Lerp(WrongPart, DefaultPart, -Weight));
            }
            else // 너무 오른쪽
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB07"));
                Utillity.Instance.ShowGuideArrow("TB07");
                SetColor(MatLShin, Color.Lerp(WrongPart, DefaultPart, Weight));
            }
        }
    }

    
    public void VisibleGetForearmAngle_All(float CheckValue) //오른팔일치
    {
        if (ForearmAngle > CheckValue)
        {
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
        }
        else
        {
            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
        }

    }
    
    public void VisibleWeight()
    {
        float weightRate = Mathf.Clamp(sensorProcess.iGetWeight, -25, 25);
        _balanceValue = (int)(weightRate * 2f);

        txtWeightLeft.text = $"{50 - _balanceValue}<size=30>%</size>";
        txtWeightRight.text = $"{50 + _balanceValue}<size=30>%</size>";
        WeightLeft.fillAmount = (0.5f - (_balanceValue / 100f));
        WeightRight.fillAmount = (0.5f + (_balanceValue / 100f));
    }

    public void VisibleWeightReset()
    {
        //BalanceValue = 0;
        txtWeightLeft.text = $"50<size=30>%</size>";
        txtWeightRight.text = $"50<size=30>%</size>";
        WeightLeft.fillAmount = 0.5f;
        WeightRight.fillAmount = 0.5f;
    }





    //=============================================
    // VISIBLE - ADDRESS
    //=============================================
    public void VisibleGetShoulderAngle_Address()
    {
        if (Mathf.Abs(ShoulderAngle) < 1f)
        {
            if (ShoulderAngle > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD02"));
                Utillity.Instance.ShowGuideArrow("AD02");
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD01"));
                Utillity.Instance.ShowGuideArrow("AD01");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));            
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            SetColor(MatChest);
        }
    }

    public void VisibleGetHandDir_Address() //수정필요
    {
        if (Mathf.Abs(HandDir) < 1f)
        {
            if (HandDir > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD04"));
                Utillity.Instance.ShowGuideArrow("AD03");
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD03"));
                Utillity.Instance.ShowGuideArrow("AD03");
            }

            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            SetColor(MatForeArmR);
            SetColor(MatForeArmL);
        }
    }

    public void VisibleGetWaistSideDir_Address() 
    {
        if (Mathf.Abs(WaistSideDir) < 1f)
        {
            if (WaistSideDir < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD05"));
                Utillity.Instance.ShowGuideArrow("AD05");
            }
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD06"));
                Utillity.Instance.ShowGuideArrow("AD06");
            }
            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(WaistSideDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
    }

    public void VisibleGetKneeSideDir_Address() //
    {
        if (Mathf.Abs(KneeSideDir) < 1f)
        {
            if (KneeSideDir > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD08"));
                Utillity.Instance.ShowGuideArrow("AD08");
            }
            else //if (AddressKneeValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD07"));
                Utillity.Instance.ShowGuideArrow("AD07");
            }

            MatThighL.color = Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(KneeSideDir));
            MatThighR.color = Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(KneeSideDir));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatThighL);
            SetColor(MatThighR);
        }
    }


    //=============================================
    // VISIBLE - TAKEBACK
    //=============================================
    public void VisibleGetForearmAngle_Takeback()//float CheckValue) //오른팔일치
    {
        if (ForearmAngle < 1f)
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB01"));
            Utillity.Instance.ShowGuideArrow("TB01");

            //SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
            //SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            //SetColor(MatUpperArmR);
            //SetColor(MatForeArmR);
        }
        //limbIK.solver.IKPositionWeight = 1f - RightElbow;
    }

    public void VisibleGetShoulderAngle_Takeback()
    {
        if (Mathf.Abs(ShoulderAngle) < 1f)
        {
            if (ShoulderAngle > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB04"));
                Utillity.Instance.ShowGuideArrow("TB04");
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB03"));
                Utillity.Instance.ShowGuideArrow("TB03");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            SetColor(MatChest);
        }
    }

    public void VisibleLeftElbowSideVis_Takeback()
    {
        if (LeftElbowSideVis < 0.4f)//Utillity.Instance.dicCheckTakeback["CheckLeftArmVisibilityC"])
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            SetColor(MatForeArmL);
            SetColor(MatUpperArmL);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB05"));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, LeftElbowSideVis));
            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, LeftElbowSideVis));

        }
    }

    //=============================================
    // VISIBLE - BACKSWING
    //=============================================
    public void VisibleGetForearmAngle_Backswing()//float CheckValue) //오른팔일치
    {
        if (ForearmAngle < 1f)
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS01"));
            Utillity.Instance.ShowGuideArrow("BS01");

            //SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
            //SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            //SetColor(MatUpperArmR);
            //SetColor(MatForeArmR);
        }
        //limbIK.solver.IKPositionWeight = 1f - RightElbow;
    }

    public void VisibleGetShoulderAngle_Backswing()
    {
        if (Mathf.Abs(ShoulderAngle) < 1f)
        {
            if (ShoulderAngle > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS04"));
                Utillity.Instance.ShowGuideArrow("BS04");
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS03"));
                Utillity.Instance.ShowGuideArrow("BS03");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            SetColor(MatChest);
        }
    }

    public void VisibleGetPelvisDir_Backswing() //
    {
        if (Mathf.Abs(PelvisDir) < 1f)
        {
            if (PelvisDir > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS06"));
                Utillity.Instance.ShowGuideArrow("BS06");
            }
            else //if (PelvisDir < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS05"));
                Utillity.Instance.ShowGuideArrow("BS05");
            }

            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(PelvisDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatPelvis);
        }
    }


    //=============================================
    // VISIBLE - TOP
    //=============================================
    public void VisibleGetShoulderDir_Top() //
    {
        if (Mathf.Abs(ShoulderDir) < 1f) 
        {
            
            if (ShoulderDir > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TP02"));
                Utillity.Instance.ShowGuideArrow("TP02");
            }
            else //if (ShoulderRotateValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TP01"));
                Utillity.Instance.ShowGuideArrow("TP01");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
    }

    public void VisibleGetHandDir_Top() //수정필요
    {
        if (Mathf.Abs(HandDir) < 1f)
        {
            if (HandDir > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TP04"));
                Utillity.Instance.ShowGuideArrow("TP04");
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TP04"));
                Utillity.Instance.ShowGuideArrow("TP03");
            }

            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderAngle)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            SetColor(MatForeArmR);
            SetColor(MatForeArmL);
        }
    }

    public void VisibleGetForearmAngle_Top()//float CheckValue) //오른팔일치
    {
        if (ForearmAngle < 1f)
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TP05"));
            Utillity.Instance.ShowGuideArrow("TP05");

            //SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
            //SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, ForearmAngle));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            //SetColor(MatUpperArmR);
            //SetColor(MatForeArmR);
        }
    }

    //=============================================
    // VISIBLE - DOWNSWING
    //=============================================
    public void VisibleGetShoulderDir_Downswing() //
    {
        if (Mathf.Abs(ShoulderDir) < 1f)
        {
            if (ShoulderDir > 0)
            {
                //Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS02"));
                //Utillity.Instance.ShowGuideArrow("DS02");
            }
            else //if (ShoulderRotateValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS01"));
                Utillity.Instance.ShowGuideArrow("DS01");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderDir)));            
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
    }

    public void VisibleGetHandSideDir_Downswing()
    {
        if (HandSideDir < 1f)
        {
            //Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS03"));
            if (ShoulderDir > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS03"));
                Utillity.Instance.ShowGuideArrow("DS03");
            }
            else //if (ShoulderRotateValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS04"));
                Utillity.Instance.ShowGuideArrow("DS04");
            }
            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, HandSideDir));
            //SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, HandSideDir)); 
            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, HandSideDir));
            //SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, HandSideDir));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            SetColor(MatUpperArmR);
            //SetColor(MatForeArmR);
            SetColor(MatUpperArmL);
            //SetColor(MatForeArmL);
        }
    }

    public void VisibleGetSpineDir_Downswing() //척추기울기
    {
        if (Mathf.Abs(SpineDir) < 1f)
        {
            if (SpineDir < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS05"));
                Utillity.Instance.ShowGuideArrow("DS05");
            }
            else //if (SpineDir < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS06"));
                Utillity.Instance.ShowGuideArrow("DS06");
            }
            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(SpineDir)));            
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
    }

    //=============================================
    // VISIBLE - IMPACT
    //=============================================
    public void VisibleGetSpineDir_Impact() //척추기울기
    {
        if (Mathf.Abs(SpineDir) < 1f)
        {
            if (SpineDir < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP02"));
                Utillity.Instance.ShowGuideArrow("IP02");
            }
            else //if (SpineDir < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP01"));
                Utillity.Instance.ShowGuideArrow("IP01");
            }
            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(SpineDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
    }

    public void VisibleGetElbowRightFrontDir_Impact() //
    {
        if (Mathf.Abs(ElbowRightFrontDir) < 1f)
        {
            
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP04"));
            Utillity.Instance.ShowGuideArrow("IP04");

            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowRightFrontDir)));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowRightFrontDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
        }
    }

    public void VisibleGetElbowFrontDir_Impact() //
    {
        if (Mathf.Abs(ElbowFrontDir) < 1f)
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP03"));
            Utillity.Instance.ShowGuideArrow("IP03");
            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowFrontDir)));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowFrontDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatUpperArmL);
            SetColor(MatForeArmL);
        }
    }



    //=============================================
    // VISIBLE - FOLLOW
    //=============================================
    public void VisibleGetElbowRightFrontDir_Follow() //
    {
        if (Mathf.Abs(ElbowRightFrontDir) < 1f)
        {
            
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL04"));
            Utillity.Instance.ShowGuideArrow("FL04");

            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowRightFrontDir)));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowRightFrontDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
        }
    }

    public void VisibleGetElbowFrontDir_Follow() //
    {
        if (Mathf.Abs(ElbowFrontDir) < 1f)
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL03"));
            Utillity.Instance.ShowGuideArrow("FL03");
            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowFrontDir)));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ElbowFrontDir)));
        }
        else
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatUpperArmL);
            SetColor(MatForeArmL);
        }
    }













    /*
    //=============================================
    // 골반
    //=============================================
    public float CheckPelvis(float min, float max) //골반일치
    {
        //프로와 유저 각각 골반 거리 검사
        float disL = Mathf.Clamp(Vector3.Distance(ProParts[8].position, UserParts[8].position), 0, 0.2f);
        float disR = Mathf.Clamp(Vector3.Distance(ProParts[11].position, UserParts[11].position), 0, 0.2f);
        float dis = (disL + disR) / 2f;
        float ret = (0.2f - dis) / 0.2f;

        //골반 방향 검사
        Vector3 dir = UserParts[16].position - ProParts[16].position;
        dir.y = 0;
        dir.Normalize();

        float dotForward = Vector3.Dot(dir, Vector3.forward);
        float dotRight = Vector3.Dot(dir, Vector3.right);
        if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
        {
            if (dotForward > 0)
                PelvisVectorValue = Vector3.forward;
            else
                PelvisVectorValue = Vector3.back;
        }
        else
        {
            if (dotRight > 0)
                PelvisVectorValue = Vector3.right;
            else
                PelvisVectorValue = Vector3.left;
        }

        PelvisValue = ret;
        return ret;
        
    }

    public void VisiblePelvis( ) //골반일치표시
    {
        if (PelvisValue > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisC"])
        {
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            if (_showToast == false)
            {
                _showToast = true;
                if(PelvisVectorValue ==  Vector3.forward)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD06"));
                else if (PelvisVectorValue == Vector3.back)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD05"));
                else if (PelvisVectorValue == Vector3.right)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD08"));
                else if (PelvisVectorValue == Vector3.left)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD07"));
            }
            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    public void VisibleTopPelvis() //골반일치표시
    {
        if (PelvisValue > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisC"])
        {
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            
            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    public void VisiblePelvisDown() //골반일치표시 (다운스윙)
    {
        if (PelvisValue > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisC"])
        {
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            if (_showToast == false)
            {
                _showToast = true;
                if (PelvisVectorValue == Vector3.forward)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS01"));
                else if (PelvisVectorValue == Vector3.back)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS02"));
                else if (PelvisVectorValue == Vector3.right)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS03"));
                else if (PelvisVectorValue == Vector3.left)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS04"));
            }

            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    public void VisibleImpactPelvis() //골반일치표시
    {
        if (PelvisValue > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisC"])
        {
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            if (_showToast == false)
            {
                _showToast = true;
                if (PelvisVectorValue == Vector3.left)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP01"));
            }
            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    public void VisibleFollowPelvis() //골반일치표시
    {
        if (PelvisValue > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisC"])
        {
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            if (_showToast == false)
            {
                _showToast = true;
                if (PelvisVectorValue == Vector3.left)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL02"));
                else if (PelvisVectorValue == Vector3.right)
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL01"));
            }

            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    public void VisibleFinishPelvis() //골반일치표시
    {
        if (PelvisValue > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisC"])
        {
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            if (_showToast == false)
            {
                _showToast = true;
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FS01"));
            }

            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    //=============================================
    // 골반회전각도 (테이크백)
    //=============================================
    public float CheckPelvisRot(int bas, int allow)
    {
        //float GetAngle = Mathf.Clamp(bas - sensorProcess.iGetPelvisDir, min + adjVal, max - adjVal);

        PelvisRotValue = CalAngleDiff(bas, allow, sensorProcess.iGetPelvisDir);

        return PelvisRotValue;
    }

    public void VisiblePelvisRot()
    {
        if (Mathf.Abs(PelvisRotValue) > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisRotC"])
        {
            Utillity.Instance.HideGuideArrow();
            SetColor(MatPelvis);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB01"));
            Utillity.Instance.ShowGuideArrow("TB01");
            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    public void VisiblePelvisRotDown()
    {
        if (Mathf.Abs(PelvisRotValue) > 0.6f)//Utillity.Instance.dicCheckAddress["CheckPelvisRotC"])
        {
            Utillity.Instance.HideGuideArrow();
            SetColor(MatPelvis);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS05"));
            Utillity.Instance.ShowGuideArrow("DS05");

            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, PelvisValue));
        }
    }

    //=============================================
    // 팔꿈치
    //=============================================
    public float CheckRightElbow(int bas, int allow) //오른팔일치
    {
        //float forearmAng = Mathf.Clamp(sensorProcess.iGetForearmAngle , 0, bas + max);
        RightElbowValue = CalAngleDiff(bas, allow, sensorProcess.iGetForearmAngle);        
        return RightElbowValue;
    }

    public void VisibleRightElbow()//float CheckValue) //오른팔일치
    {
        if (RightElbowValue > 0.99f)
        {
            Utillity.Instance.HideGuideArrow();
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB04"));
            Utillity.Instance.ShowGuideArrow("TB04");

            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, RightElbowValue));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, RightElbowValue));
        }
    }

    public void VisibleRightElbowPosition(float CheckValue) //오른팔일치
    {
        //limbIK.solver.IKPositionWeight = Mathf.Lerp(limbIK.solver.IKPositionWeight, (1f - RightElbowValue), 0.5f);
        
        if (RightElbowValue > CheckValue)
        {
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
        }
        else
        {
            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, RightElbowValue));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, RightElbowValue));
        }
        
    }


    //=============================================
    // 왼쪽손목각도 - 팔꿈치인식률 (테이크백)
    //=============================================
    public float CheckLeftArmVisibility()
    {
        LeftArmVisibilityValue = sensorProcess.fLeftElbowSideVis;
        return LeftArmVisibilityValue;
    }

    public void VisibleLeftArmVisibility()
    {
        if(LeftArmVisibilityValue < 0.4f)//Utillity.Instance.dicCheckTakeback["CheckLeftArmVisibilityC"])
        {
            SetColor(MatForeArmL);
            SetColor(MatUpperArmL);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB06"));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, LeftArmVisibilityValue));
            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, LeftArmVisibilityValue));

        }
    }

	//=============================================
    // 팔과 몸 간격(어드레스)
    //=============================================
    public float CheckAddressHand(int bas, int allow) //오른팔일치
    {
        //float GetAngle = Mathf.Clamp(bas - sensorProcess.iGetHandSideDir, min + adjVal, max - adjVal); //80기준 +-도 70~90 = -10 ~ +10        

        AddressHandValue = CalAngleDiff(bas, allow, sensorProcess.iGetHandSideDir , true);
        return AddressHandValue;
    }

    public void VisibleAddressHand() //오른팔일치
    {
        if (Mathf.Abs(AddressHandValue) > 0.75f)//Utillity.Instance.dicCheckAddress["CheckAddressHandC"])
        {
            Utillity.Instance.HideGuideArrow();
            _showToast = false;


            SetColor(MatUpperArmL);
            SetColor(MatForeArmL);
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
        }
        else
        {
            if (AddressHandValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD04"));
                Utillity.Instance.ShowGuideArrow("AD04");
            }
            else //if (AddressHandValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD03"));
                Utillity.Instance.ShowGuideArrow("AD03");
            }

            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressHandValue)));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressHandValue)));
            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressHandValue)));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressHandValue)));
        }
    }

    //=============================================
    // 척추기울기(어드레스)
    //=============================================
    public float CheckAddressSpine(int bas, int allow) //척추기울기
    {
        //float GetAngle = Mathf.Clamp(bas - sensorProcess.iGetWaistSideDir, min + adjVal, max - adjVal); //148기준 +-도  = -15 ~ +15        

        AddressSpineValue = CalAngleDiff(bas, allow, sensorProcess.iGetWaistSideDir, true);

        return AddressSpineValue;
    }

    public void VisibleAddressSpine() //척추기울기
    {
        if (Mathf.Abs(AddressSpineValue) > 0.99f)//Utillity.Instance.dicCheckAddress["CheckAddressSpineC"])
        {
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
        else
        {
            if (AddressSpineValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD09"));
                Utillity.Instance.ShowGuideArrow("AD09");
            }
            else //if (AddressSpineValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD10"));
                Utillity.Instance.ShowGuideArrow("AD10");
            }
            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressSpineValue)));
        }
    }

    //=============================================
    // 무릎(어드레스)
    //=============================================
    public float CheckAddressKnee(int bas, int allow) //
    {
        //float GetAngle = Mathf.Clamp(bas - sensorProcess.iGetKneeSideDir, min + adjVal, max - adjVal); //169기준 +-도  = -10 ~ +10       

        AddressKneeValue = CalAngleDiff(bas, allow, sensorProcess.iGetKneeSideDir, true);

        return AddressKneeValue;
    }

    public void VisibleAddressKnee() //
    {
        if (Mathf.Abs(AddressKneeValue) > 0.99f)//Utillity.Instance.dicCheckAddress["CheckAddressKneeC"])
        {
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatThighL);
            SetColor(MatThighR);
        }
        else
        {
            if (AddressKneeValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD12"));
                Utillity.Instance.ShowGuideArrow("AD12");
            }
            else //if (AddressKneeValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD11"));
                Utillity.Instance.ShowGuideArrow("AD11");
            }

            MatThighL.color = Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressKneeValue));
            MatThighR.color = Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(AddressKneeValue));
        }
    }

    //=============================================
    // 어께회전(백스윙)
    //=============================================
    public float CheckShoulderRotate(int bas, int allow) //
    {
        //float temp = sensorProcess.iGetShoulderDir;
        ShoulderRotateValue = CalAngleDiff(bas, allow, sensorProcess.iGetShoulderDir, true);

        return ShoulderRotateValue;
    }

    public void VisibleShoulderRotate() //
    {
        if (Mathf.Abs(ShoulderRotateValue) > 0.99f)//Utillity.Instance.dicCheckBackswing["CheckShoulderRotateC"])
        {
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatChest);
        }
        else
        {
            if (ShoulderRotateValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS02"));
                Utillity.Instance.ShowGuideArrow("BS02");
            }
            else //if (ShoulderRotateValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS01"));
                Utillity.Instance.ShowGuideArrow("BS01");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderRotateValue)));
        }
    }

    //=============================================
    // 골반회전(백스윙)
    //=============================================
    public float CheckPelvisRotate(int bas, int allow) //
    {
        //float temp = sensorProcess.iGetPelvisDir;
        BackSwingPelvisValue = CalAngleDiff(bas, allow, sensorProcess.iGetPelvisDir, true);

        return BackSwingPelvisValue;
    }

    public void VisiblePelvisRotate() //
    {
        if (Mathf.Abs(BackSwingPelvisValue) > 0.99f)//Utillity.Instance.dicCheckBackswing["CheckPelvisRotateC"])
        {
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatPelvis);
        }
        else
        {
            if (BackSwingPelvisValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS04"));
                Utillity.Instance.ShowGuideArrow("BS04");
            }
            else //if (BackSwingPelvisValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS03"));
                Utillity.Instance.ShowGuideArrow("BS03");
            }

            SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(BackSwingPelvisValue)));
        }
    }

    //=============================================
    // 골반회전(백스윙)
    //=============================================
    public float CheckStance(int bas, int allow) //
    {
        //float temp = sensorProcess.iGetFootDisRate;
        StanceRateValue = CalAngleDiff(bas, allow, sensorProcess.iGetFootDisRate);

        return StanceRateValue;
    }

    public void VisibleStance() //
    {
        if (Mathf.Abs(StanceRateValue) > 0.4f)//Utillity.Instance.dicCheckAddress["CheckStanceC"])
        {
            Utillity.Instance.HideGuideArrow();
            _showToast = false;
            SetColor(MatLShin);
            SetColor(MatLFoot);
            SetColor(MatRShin);
            SetColor(MatRFoot);
        }
        else
        {
            if (StanceRateValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD13"));
                Utillity.Instance.ShowGuideArrow("AD13");
            }
            else //if (BackSwingPelvisValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD14"));
                Utillity.Instance.ShowGuideArrow("AD14");
            }

            SetColor(MatLShin, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(StanceRateValue)));
            SetColor(MatLFoot, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(StanceRateValue)));
            SetColor(MatRShin, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(StanceRateValue)));
            SetColor(MatRFoot, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(StanceRateValue)));
        }
    }

    //=============================================
    // 어께회전(다운스윙)
    //=============================================
    public float CheckShoulderRotateDown(float bas, float min, float max)
    {
        float temp = sensorProcess.iGetShoulderDir;
        if (temp < 0)
            temp += 360f;
        float GetAngle = Mathf.Clamp(bas - temp, min + adjVal, max - adjVal); //134

        if (GetAngle > 0) //Right
            ShoulderRotateValue = (max - GetAngle) / max;
        else //Left
            ShoulderRotateValue = (min - GetAngle) / max;

        return ShoulderRotateValue;
    }

    public void VisibleShoulderRotateDown()
    {
        if (Mathf.Abs(ShoulderRotateValue) > 0.6f)//Utillity.Instance.dicCheckAddress["CheckShoulderRotateDownC"])
        {
            Utillity.Instance.HideGuideArrow();

            SetColor(MatChest);
        }
        else if (ShoulderRotateValue > 0)
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS07"));
            Utillity.Instance.ShowGuideArrow("DS07");

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulderRotateValue)));
        }
    }

    //=============================================
    // 머리회전
    //=============================================
    public float CheckHead(float bas, float min, float max)
    {
        float temp = sensorProcess.iGetHeadDir;
        if (temp < 0)
            temp += 360f;
        float GetAngle = Mathf.Clamp(bas - temp, min + adjVal, max - adjVal); //155

        if (GetAngle > 0) //Right
            HeadValue = (max - GetAngle) / max;
        else //Left
            HeadValue = (min - GetAngle) / max;

        return 1f;//HeadValue;
    }

    public void VisibleHead()
    {
        if (Mathf.Abs(ShoulderRotateValue) > 0.6f)//Utillity.Instance.dicCheckAddress["CheckHeadC"])
        {
            Utillity.Instance.HideGuideArrow();

            SetColor(MatHead);
        }
        else
        {
            if (HeadValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP04"));
            }
            else //if (HeadValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP05"));
            }

            SetColor(MatHead, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(HeadValue)));
        }
    }

    //=============================================
    // 어깨각도
    //=============================================
    public float CheckShoulerHeightAngle(int bas, int allow)// float bas, float min, float max)
    {
        ShoulerHeightAngleValue = CalAngleDiff(bas, allow, sensorProcess.iGetShoulderAngle, true);
        return ShoulerHeightAngleValue;
    }



    public void VisibleShoulerHeightAngle()
    {
        if (Mathf.Abs(ShoulerHeightAngleValue) > 0.6f)//Utillity.Instance.dicCheckDownswing["CheckShoulerHeightAngleC"])
        {
            Utillity.Instance.HideGuideArrow();

            SetColor(MatChest);
        }
        else
        {
            if (ShoulerHeightAngleValue < 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS07"));
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS07"));
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulerHeightAngleValue)));
        }
    }

    public void VisibleShoulerHeightAngleAddress()
    {
        if (Mathf.Abs(ShoulerHeightAngleValue) > 0.99f)//Utillity.Instance.dicCheckDownswing["CheckShoulerHeightAngleC"])
        {
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            SetColor(MatChest);
        }
        else
        {
            if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD02"));
                Utillity.Instance.ShowGuideArrow("AD02");
            }
            else// if (ShoulerHeightAngleValue > 0)
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD01"));
                Utillity.Instance.ShowGuideArrow("AD01");
            }

            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, Mathf.Abs(ShoulerHeightAngleValue)));
        }
    }


    //=============================================
    // 몸과 손의 간격 (테이크백)
    //=============================================
    public float CheckChestHand(int bas, int allow)// float angle)
    {
        //float GetAngle = Mathf.Clamp(bas - sensorProcess.iGetHandSideDir, min + adjVal, max - adjVal);

        ChestHandValue = CalAngleDiff(bas, allow, sensorProcess.iGetHandSideDir);

        return ChestHandValue;
    }

    public void VisibleChestHand()
    {
        if(ChestHandValue > 0.6f)//Utillity.Instance.dicCheckTakeback["CheckChestHandC"])
        {
            SetColor(MatUpperArmR);
            SetColor(MatForeArmR);
            SetColor(MatUpperArmL);
            SetColor(MatForeArmL);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB09"));

            SetColor(MatUpperArmR, Color.Lerp(WrongPart, DefaultPart, ChestHandValue));
            SetColor(MatForeArmR, Color.Lerp(WrongPart, DefaultPart, ChestHandValue));
            SetColor(MatUpperArmL, Color.Lerp(WrongPart, DefaultPart, ChestHandValue));
            SetColor(MatForeArmL, Color.Lerp(WrongPart, DefaultPart, ChestHandValue));

        }
    }

    //=============================================
    // 허리 숙임 각도 (테이크백)
    //=============================================
    public float CheckWaistAngle(int bas, int allow)// float waistAngle)
    {
        //float GetAngle = Mathf.Clamp(bas - sensorProcess.iGetWaistSideDir, min + adjVal, max - adjVal);

        WaistValue = CalAngleDiff(bas, allow, sensorProcess.iGetWaistSideDir);

        return WaistValue;
    }

    public void VisibleWaist()
    {
        if (WaistValue > 0.99f)//Utillity.Instance.dicCheckTakeback["CheckWaistAngleC"])
        {
            SetColor(MatChest);
        }
        else
        {
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB05"));
            SetColor(MatChest, Color.Lerp(WrongPart, DefaultPart, WaistValue));
        }
    }

    public void CheckSwingAngle(float rate)
    {
        MatUpperArmR.color = Color.Lerp(WrongPart, DefaultPart, rate);
        MatForeArmR.color = Color.Lerp(WrongPart, DefaultPart, rate);
        MatUpperArmL.color = Color.Lerp(WrongPart, DefaultPart, rate);
        MatForeArmL.color = Color.Lerp(WrongPart, DefaultPart, rate);
    }

    //=============================================
    // 밸런스 (팔로우)
    //=============================================
    public float Checkbalance(int bas, int allow)// BalanceValue : -50~50  (-)Left (+)Right
    {
        
        //-50 ~ 50
        int GetBalance = Mathf.Clamp(_balanceValue - bas, -allow, allow);

        BalanceValue = CalAngleDiff(bas, allow, sensorProcess.iGetWeight, true);

        return BalanceValue;
    }

    public void VisibleBalance()
    {
        if (Mathf.Abs(BalanceValue) > 0.7f)//Utillity.Instance.dicCheckFollow["CheckBalanceC"])
        {
            //밸런스 정상
            SetColor(MatPelvis);
        }
        else
        {
            if (BalanceValue < 0) //너무 왼쪽
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL01"));
                SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, -BalanceValue));
            }
            else // 너무 오른쪽
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL02"));
                SetColor(MatPelvis, Color.Lerp(WrongPart, DefaultPart, BalanceValue));
            }
        }
    }

    //=============================================
    // 무게이동
    //=============================================
    public void VisibleWeight()
    {
        float weightRate = Mathf.Clamp(sensorProcess.iGetWeight, -25, 25);
        _balanceValue = (int)(weightRate * 2f);

        txtWeightLeft.text = $"{50 - _balanceValue}<size=30>%</size>";
        txtWeightRight.text = $"{50 + _balanceValue}<size=30>%</size>";
        WeightLeft.fillAmount = (0.5f - (_balanceValue / 100f));
        WeightRight.fillAmount = (0.5f + (_balanceValue / 100f));
    }

    public void VisibleWeightReset()
    {
        BalanceValue = 0;
        txtWeightLeft.text = $"50<size=30>%</size>";
        txtWeightRight.text = $"50<size=30>%</size>";
        WeightLeft.fillAmount = 0.5f;
        WeightRight.fillAmount = 0.5f;
    }
    */


    bool isShowProcess = false;
    //=============================================
    // 일반 함수
    //=============================================
    public void ShowAvatar(bool isShow)
    {
        if (_avatarOn == isShow)
            return;

        Debug.Log($"ShowAvatar({isShow})");
        Body_User.GetComponent<SkinnedMeshRenderer>().enabled = isShow;

        if (isShow == false)
        {
            for (int i = 0; i < Body_User.GetComponent<SkinnedMeshRenderer>().materials.Length; i++)
                Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[i].SetFloat("_Dissolve", 1);

            _avatarOn = false;

            txtWeightLeft.gameObject.SetActive(_avatarOn);
            txtWeightRight.gameObject.SetActive(_avatarOn);
            WeightRightRoot.SetActive(_avatarOn);
            WeightLeftRoot.SetActive(_avatarOn);
            cgBalance.alpha = 0;
            UserClub.SetActive(false);

        }
        else
        {
            if (isShowProcess == false)
            {
                isShowProcess = true;
                StartCoroutine(CoShowAvatar());
            }
        }
    }

    IEnumerator CoShowAvatar()
    {
        float timeSpeed = 0;
        float fadeSpeed = 0;
        float amountSpeed = 0;
        float delay = 0;

        if (GameManager.Instance.Mode == EStep.Realtime)
        {
            delay = 2f;
            timeSpeed = 0.2f;
            fadeSpeed = 1.5f;
            amountSpeed = 1f;
        }
        else if (GameManager.Instance.Mode == EStep.Preview)
        {
            delay = 0.2f;
            timeSpeed = 1f;
            fadeSpeed = 0.6f;
            amountSpeed = 0.4f;
        }

        float value = 0f;
        float uiVal = 0;
        WeightLeft.fillAmount = 0;
        WeightRight.fillAmount = 0;
        txtWeightLeft.text = $"0<size=30>%</size>";
        txtWeightRight.text = $"0<size=30>%</size>";
        WeightLeftRoot.gameObject.SetActive(true);
        WeightRightRoot.gameObject.SetActive(true);
        txtWeightLeft.gameObject.SetActive(true);
        txtWeightRight.gameObject.SetActive(true);

        Sequence sq = DOTween.Sequence();
        sq.Join(cgBalance.DOFade(1f, fadeSpeed).SetDelay(delay).From(0)).SetEase(Ease.InQuad);
        sq.Append(WeightLeft.DOFillAmount(1f, amountSpeed).From(0));
        sq.Join(WeightRight.DOFillAmount(1f, amountSpeed).From(0));
        sq.Append(WeightLeft.DOFillAmount(0.5f, amountSpeed));
        sq.Join(WeightRight.DOFillAmount(0.5f, amountSpeed));
        sq.Play();

        do
        {
            uiVal = WeightLeft.fillAmount > 1f ? 1f : WeightLeft.fillAmount;
            txtWeightLeft.text = $"{(int)(uiVal * 100f)}<size=30>%</size>";
            txtWeightRight.text = $"{(int)(uiVal * 100f)}<size=30>%</size>";

            for (int i = 0; i < Body_User.GetComponent<SkinnedMeshRenderer>().materials.Length; i++)
                Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[i].SetFloat("_Dissolve", 1f - value);

            value += timeSpeed * Time.deltaTime;

            yield return null;
        } while (value < 1f) ;

        for (int i = 0; i < Body_User.GetComponent<SkinnedMeshRenderer>().materials.Length; i++)
            Body_User.GetComponent<SkinnedMeshRenderer>()?.materials[i].SetFloat("_Dissolve", 0);

        txtWeightLeft.text = $"50<size=30>%</size>";
        txtWeightRight.text = $"50<size=30>%</size>";
        WeightLeft.fillAmount = 0.5f;
        WeightRight.fillAmount = 0.5f;
        cgBalance.alpha = 1f;

        if (GameManager.Instance.Mode == EStep.Realtime)
        {
            AudioManager.Instance.PlayTutorial("PracticeMode_01");
            Utillity.Instance.ShowToast("어드레스 자세를 잡아주세요.");

            yield return new WaitForSeconds(1f);
        }        

        _avatarOn = true;
        isShowProcess = false;
        UserClub.SetActive(true);
    }

    public bool GetAvatarOn()
    {
        return _avatarOn;
    }

    void SetColor(Material mat, Color col)
    {
        mat.SetColor("_BaseColor", col);
    }

    void SetColor(Material mat)
    {
        mat.SetColor("_BaseColor", DefaultPart);
    }
}
