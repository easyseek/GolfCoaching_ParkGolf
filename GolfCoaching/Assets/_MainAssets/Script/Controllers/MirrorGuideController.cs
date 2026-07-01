using Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MirrorGuideController : MonoBehaviour
{
    [Header("* Ref Objects")]
    [SerializeField] SensorProcess sensorProcess;
    [SerializeField] WebcamTracker webcamTrackerFront;
    [SerializeField] WebcamTracker webcamTrackerSide;
    [SerializeField] DrawGuideLine guideLineFront;
    [SerializeField] DrawGuideLine guideLineSide;
    bool isSensorOn = false;
    public bool Coaching = false;
    public bool CoachingVisible = true;
    float limitWidth;

    [Header("* Wall")]
    [SerializeField] Transform WallLeft;
    [SerializeField] Transform WallRight;
    [SerializeField] Transform WallFront;
    [SerializeField] Transform WallBack;    

    [Header("* Balance")]
    [SerializeField] GameObject BalanaceLeft;
    [SerializeField] GameObject BalanaceRight;
    [SerializeField] GameObject BalanaceFront;
    [SerializeField] GameObject BalanaceBack;    
    [SerializeField] Image imgBalanceLeft;
    [SerializeField] Image imgBalanceRight;
    [SerializeField] Image imgBalanceFront;
    [SerializeField] Image imgBalanceBack;
    [SerializeField] TextMeshProUGUI textBalanceLeft;
    [SerializeField] TextMeshProUGUI textBalanceRight;
    [SerializeField] TextMeshProUGUI textBalanceFront;
    [SerializeField] TextMeshProUGUI textBalanceBack;

    [Header("* Visible Obj. Pro Root")]
    //[SerializeField] Transform FrontShoulderRoot;
    //[SerializeField] Transform FrontPelvisRoot;
    //[SerializeField] Transform FrontSpineRoot;
    [SerializeField] Transform PoseFrontHeadRoot;
    //[SerializeField] Transform SideSpineRoot;
    [SerializeField] Transform FrontHandRoot;
    //[SerializeField] Transform SideHandFailRoot;
    [SerializeField] Transform SideHandRoot;
    [SerializeField] Transform PoseSideHeadRoot;

    //[SerializeField] Transform SideWallRoot;

    //[SerializeField] Transform DebugHand;
    [SerializeField] Transform FootDisRateRoot;
    [SerializeField] Transform KneeDisRateRoot;

    [Header("* Visible Obj. Pro Childs")]
    [SerializeField] Transform ProShoulderAngleRoot_Front;
    [SerializeField] Transform ProPelvisAngleRoot_Front;
    [SerializeField] Transform ProShoulderAngle_Front;
    [SerializeField] Transform ProPelvisAngle_Front;
    [SerializeField] Transform FootRate_LeftPosition;
    [SerializeField] Transform FootRate_RightPosition;
    [SerializeField] Transform KneeRate_LeftPosition;
    [SerializeField] Transform KneeRate_RightPosition;



    [Header("* Swing Step Info")]
    [SerializeField] RectTransform SwingMovingRoot;
    [SerializeField] TextMeshProUGUI[] txtInfoSwingSteps;
    float moveStepX = -75f;

    [Header("* User Info")]
    [SerializeField] Transform AngleRootShoulder_Front;
    [SerializeField] Transform AngleRootPelvis_Front;
    [SerializeField] RectTransform AngleRotatonShoulder_Front;       
    [SerializeField] RectTransform AngleRotatonPelvis_Front;

    //[SerializeField] Transform AngleRootShoulder_Side;
    //[SerializeField] Transform AngleRootPelvis_Side;
    //[SerializeField] Transform AngleRotatonShoulder_Side;
    //[SerializeField] Transform AngleRotatonPelvis_Side;

    [SerializeField] GameObject HandHistoryPrafab;
    [SerializeField] GameObject[] HandHistoryNodes;

    [SerializeField] Transform ShoulderArrowLeftRoot_Front; 
    [SerializeField] Transform ShoulderArrowLeft_Front;

    [SerializeField] RectTransform AngleDrillShoulder_Front;
    [SerializeField] RectTransform AngleDrillPelvis_Front;

    //Check Swing Data
    ProSwingStepData swingStepData = null;
    int SwingLimitUp = 90;
    int SwingLimitDown = 0;
    Vector2 ballPosition;

    //Check Swing Skeleton Data
    ProLandmarkStepPaths swingStepSkData;
    Landmark2D[] SkLandmark2Ds;
    Vector2 ProRightPelviCenter; //보정된 프로의 골반 중앙
    Vector2 ProLeftPelvis; //보정된 프로의 골반 위치
    Vector2 ProRightPelvis; //보정된 프로의 골반 위치
    float ProLeftPelvisRate; //보정된 프로의 골반 값
    float ProRightPelvisRate; //보정된 프로의 골반 위치 값
    

    float frontShoulderAngle = 0;
    float frontPelvisAngle = 0;


    [HideInInspector] public float HandFrontDistance;
    [HideInInspector] public float HandSideDistance;
    
    float FrontHandSideDistanceScale = -1;
    float SideHandSideDistanceScale = -1;
    float CheckAddressTime = 0;

    int turnPauseAngle = 175;
    int turnTakeback = 170;
    int turnBackswing = 140;
    int turnTop = 100;
    int turnDownswind = 130;
    int turnImpact = 170;
    int turnFollow = 200;
    int turnFinish = 260;

    SWINGSTEP swingStep = SWINGSTEP.READY;
    bool isReverse = true;

    [SerializeField] TextMeshProUGUI txtDebug;

    int MoveStep = 0;

    [SerializeField] RectTransform debugHandAngle;

    [Header("* Pro Info")]
    [SerializeField] TextMeshProUGUI txtCheckString_1;
    [SerializeField] TextMeshProUGUI txtCheckString_2;
    [SerializeField] TextMeshProUGUI txtCheckString_3;
    [SerializeField] TextMeshProUGUI txtCheckString_4;
    [SerializeField] TextMeshProUGUI txtCheckString_5;
    [SerializeField] Color colBalanced; //적정
    [SerializeField] Color colLacking;  //부족
    [SerializeField] Color colTooMuch;  //과함
    [SerializeField] Color colUnstable; //불안정

    int ProValue_1 = 0;
    int ProValue_2 = 0;
    int ProValue_3 = 0;
    int ProValue_4 = 0;
    int ProValue_5 = 0;

    [Header("* Debug Value")]
    [SerializeField] TextMeshProUGUI txtPro_1;
    [SerializeField] TextMeshProUGUI txtPro_2;
    [SerializeField] TextMeshProUGUI txtPro_3;
    [SerializeField] TextMeshProUGUI txtPro_4;
    [SerializeField] TextMeshProUGUI txtPro_5;

    [SerializeField] TextMeshProUGUI txtUser_1;
    [SerializeField] TextMeshProUGUI txtUser_2;
    [SerializeField] TextMeshProUGUI txtUser_3;
    [SerializeField] TextMeshProUGUI txtUser_4;
    [SerializeField] TextMeshProUGUI txtUser_5;

    public ESwingType eSwingType = ESwingType.Full;
    public EClub eClub = EClub.MiddleIron;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LoadSwingStepData();
        swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.MiddleIron];
        SetTurnAngle();

        ResetProLine();
        BalanaceLeft.SetActive(false);
        BalanaceRight.SetActive(false);
        BalanaceFront.SetActive(false);
        BalanaceBack.SetActive(false);

        WallLeft.gameObject.SetActive(false);
        WallRight.gameObject.SetActive(false);
        WallFront.gameObject.SetActive(false);
        WallBack.gameObject.SetActive(false);

        AngleRootShoulder_Front.gameObject.SetActive(false);
        AngleRootPelvis_Front.gameObject.SetActive(false);
        //AngleRootShoulder_Side.gameObject.SetActive(false);
        //AngleRootPelvis_Side.gameObject.SetActive(false);
        ProShoulderAngleRoot_Front.gameObject.SetActive(false);
        ProPelvisAngleRoot_Front.gameObject.SetActive(false);
        
        //프로 원본 스켈레톤
        swingStepSkData = GolfProDataManager.Instance.GetLandmarkStepData(GolfProDataManager.Instance.SelectProData.uid, false, 0, club: EClub.MiddleIron);
        LoadSkeletonSwint(SWINGSTEP.ADDRESS);

        SetReady();

        Coaching = false;
    }

    public bool LoadSwingStepData()
    {
        if(eSwingType == ESwingType.Full)
        {
            if(GolfProDataManager.Instance.SelectProData.swingData.dicFull.ContainsKey(eClub))
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[eClub];
                return true;
                //eSwingType = ESwingType.Full;
                //eClub = EClub.MiddleIron;
            }
            else
                return false;
            
        }
        else if(eSwingType == ESwingType.ThreeQuarter)
        {
            if(GolfProDataManager.Instance.SelectProData.swingData.dicQuarter.ContainsKey(eClub))
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicQuarter[eClub];
                return true;
            }
            else
                return false;
        }
        else if(eSwingType == ESwingType.Half)
        {
            if(GolfProDataManager.Instance.SelectProData.swingData.dicHalf.ContainsKey(eClub))
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicHalf[eClub];
                return true;
            }
            else
                return false;
        }
        else
        {
            return false;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (Coaching == false)
            return;

        VisibleWeight();
        DrawUserInfoFront();
        DrawUserInfoSide();
        SwingPoseDetect();

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveStep = 1;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveStep = -1;
        }
        if (swingStep == SWINGSTEP.READY)
        {
            SwingMovingRoot.anchoredPosition = Vector2.MoveTowards(SwingMovingRoot.anchoredPosition, new Vector2(moveStepX, 0),
                Time.deltaTime * (5000f));
        }
        else
        {
            SwingMovingRoot.anchoredPosition = Vector2.MoveTowards(SwingMovingRoot.anchoredPosition, new Vector2(moveStepX, 0),
                Time.deltaTime * (2000f));
        }
    }

    void SetTurnAngle()
    {
        //turnTackback = (GetSwingDataValue("GetHandDir", SWINGSTEP.ADDRESS) + GetSwingDataValue("GetHandDir", SWINGSTEP.TAKEBACK)) / 2;
        //turnPauseAngle = (GetSwingDataValue("GetHandDir", SWINGSTEP.ADDRESS);
        turnTakeback = (GetSwingDataValue("GetHandDir", SWINGSTEP.ADDRESS) + GetSwingDataValue("GetHandDir", SWINGSTEP.TAKEBACK)) / 2;//- 8;
        turnBackswing = (GetSwingDataValue("GetHandDir", SWINGSTEP.TAKEBACK) + GetSwingDataValue("GetHandDir", SWINGSTEP.BACKSWING)) / 2;
        turnTop = (int)((GetSwingDataValue("GetHandDir", SWINGSTEP.BACKSWING) + GetSwingDataValue("GetHandDir", SWINGSTEP.TOP)) / 1.8);//2;
        turnDownswind = (GetSwingDataValue("GetHandDir", SWINGSTEP.TOP) + GetSwingDataValue("GetHandDir", SWINGSTEP.DOWNSWING)) / 2;
        turnImpact = (GetSwingDataValue("GetHandDir", SWINGSTEP.DOWNSWING) + GetSwingDataValue("GetHandDir", SWINGSTEP.IMPACT)) / 2;
        turnFollow = (GetSwingDataValue("GetHandDir", SWINGSTEP.IMPACT) + GetSwingDataValue("GetHandDir", SWINGSTEP.FOLLOW)) / 2;
        turnFinish = GetSwingDataValue("GetHandDir", SWINGSTEP.FOLLOW) + 15;
        turnPauseAngle = turnTakeback;
    }

    void DrawUserInfoFront()
    {
        //1. 허리
        txtUser_1.text = $"{sensorProcess.iGetWaistSideDir}\r\n<size=20>({ProValue_1 - sensorProcess.iGetWaistSideDir})</size>";        

        //2. 어께
        if (guideLineFront.LinePause == false)
            AngleRootShoulder_Front.localPosition = webcamTrackerFront.KalmanPositions.CenterShoulder;

        if (_inDrill)
        {
            frontShoulderAngle = sensorProcess.iGetShoulderSideDirWorld;
        }
        else if (SWINGSTEP.READY == swingStep || SWINGSTEP.ADDRESS == swingStep || SWINGSTEP.IMPACT == swingStep)
        {
            frontShoulderAngle = sensorProcess.iGetShoulderAngle;
        }
        else if (SWINGSTEP.FINISH == swingStep)
        {
            frontShoulderAngle = sensorProcess.iGetShoulderSideDirWorld;
        }
        /*else if (SWINGSTEP.FINISH == swingStep)
        {
            frontShoulderAngle = GetSwingDataValue("GetShoulderSideDirWorld", SWINGSTEP.FINISH);
        }*/
        else if (SWINGSTEP.TAKEBACK == swingStep || SWINGSTEP.BACKSWING == swingStep || SWINGSTEP.DOWNSWING == swingStep)
        {
            frontShoulderAngle = sensorProcess.iGetShoulderFrontDirWorld;
        }
        else if (SWINGSTEP.TOP == swingStep || SWINGSTEP.FOLLOW == swingStep)
        {
            frontShoulderAngle = sensorProcess.iGetShoulderDir;
        }
        else
            frontShoulderAngle = 0;

        float resize = frontShoulderAngle > 180 ? frontShoulderAngle - 180f : frontShoulderAngle;
        if (resize < 90)
        {
            //310-103
            AngleRotatonShoulder_Front.sizeDelta = new Vector2(6, Mathf.Lerp(310f, 103f, Mathf.InverseLerp(0, 90, resize)));
        }
        else
        {
            AngleRotatonShoulder_Front.sizeDelta = new Vector2(6, Mathf.Lerp(310f, 103f, Mathf.InverseLerp(180, 90, resize)));
        }
        AngleRotatonShoulder_Front.gameObject.SetActive(true);
        AngleRotatonShoulder_Front.localRotation = Quaternion.Euler(0, 0, frontShoulderAngle);

        txtUser_2.text = $"{frontShoulderAngle}\r\n<size=20>({(ProValue_2-(int)frontShoulderAngle)})</size>";//frontShoulderAngle.ToString();                

        //3. 골반
        if (guideLineFront.LinePause == false)
            AngleRootPelvis_Front.localPosition = webcamTrackerFront.KalmanPositions.CenterPelvis;

        if (_inDrill)
        {
            frontPelvisAngle = sensorProcess.iGetPelvisFrontDirWorld;
        }
        else if (SWINGSTEP.READY == swingStep || SWINGSTEP.ADDRESS == swingStep)
        {
            frontPelvisAngle = sensorProcess.iGetPelvisAngle;
        }
        /*
        else if (SWINGSTEP.FINISH == swingStep)
        {
            frontPelvisAngle = GetSwingDataValue("GetPelvisSideDirWorld", SWINGSTEP.FINISH);
            //frontPelvisAngle = sensorProcess.iGetPelvisSideDirWorld;
        }
        else if (SWINGSTEP.FOLLOW == swingStep)
        {
            frontPelvisAngle = sensorProcess.iGetPelvisSideDirWorld;
        }*/
        else if (SWINGSTEP.DOWNSWING == swingStep || SWINGSTEP.TAKEBACK == swingStep || SWINGSTEP.IMPACT == swingStep || SWINGSTEP.BACKSWING == swingStep || SWINGSTEP.FINISH== swingStep)
        {
            frontPelvisAngle = sensorProcess.iGetPelvisFrontDirWorld;
        }
        else if (SWINGSTEP.TOP == swingStep || SWINGSTEP.FOLLOW == swingStep)
        {
            frontPelvisAngle = sensorProcess.iGetPelvisDir;
        }
        else
            frontPelvisAngle = 0;

        resize = frontPelvisAngle > 180 ? frontPelvisAngle - 180f : frontPelvisAngle;
        if (resize < 90)
        {
            //260-85
            AngleRotatonPelvis_Front.sizeDelta = new Vector2(6, Mathf.Lerp(260f, 85f, Mathf.InverseLerp(0, 90, resize)));
        }
        else
        {
            AngleRotatonPelvis_Front.sizeDelta = new Vector2(6, Mathf.Lerp(260f, 85f, Mathf.InverseLerp(180, 90, resize)));
        }
        AngleRotatonPelvis_Front.gameObject.SetActive(true);
        AngleRotatonPelvis_Front.localRotation = Quaternion.Euler(0, 0, frontPelvisAngle);

        txtUser_3.text = $"{frontPelvisAngle}\r\n<size=20>({(ProValue_3-(int)frontPelvisAngle)})</size>";//frontPelvisAngle.ToString();        
        

        //4. 체중
        txtUser_4.text = $"{sensorProcess.iGetWeight}\r\n<size=20>({(ProValue_4-(int)sensorProcess.iGetWeight)})</size>";//sensorProcess.iGetWeight.ToString();
        

        //5. 머리
        txtUser_5.text = $"{sensorProcess.iGetNoseDir}\r\n<size=20>({(ProValue_5 - (int)sensorProcess.iGetNoseDir)})</size>";//sensorProcess.iGetNoseShoulderSideDir.ToString();

        //드릴
        /*if(_inDrill && guideLineFront.LinePause == false)
        {
            AngleDrillShoulder_Front.localPosition = webcamTrackerFront.KalmanPositions.CenterShoulder;
            AngleDrillPelvis_Front.localPosition = webcamTrackerFront.KalmanPositions.CenterPelvis;
        }*/
    }

    
    void DrawUserInfoSide()
    {
        if (swingStep != SWINGSTEP.READY && guideLineFront.LinePause == false)
        {
            WallBack.gameObject.SetActive(true);
            Vector2 rightHip = webcamTrackerSide.KalmanPositions.RightPelvis;
            WallBack.localPosition = rightHip;

            WallFront.gameObject.SetActive(true);
            Vector2 neck = (webcamTrackerSide.KalmanPositions.RightShoulder + webcamTrackerSide.KalmanPositions.Nose) / 2;
            neck.x = rightHip.x;
            WallFront.localPosition = neck;
        }
    }

    public void SetCoaching(bool isOn)
    {
        Coaching = isOn;
        if(Coaching == false)
        {
            ResetProLine();
            BalanaceLeft.SetActive(false);
            BalanaceRight.SetActive(false);
            BalanaceFront.SetActive(false);
            BalanaceBack.SetActive(false);
            WallLeft.gameObject.SetActive(false);
            WallRight.gameObject.SetActive(false);
            WallFront.gameObject.SetActive(false);
            WallBack.gameObject.SetActive(false);

            AngleRootShoulder_Front.gameObject.SetActive(false);
            AngleRootPelvis_Front.gameObject.SetActive(false);
            //AngleRootShoulder_Side.gameObject.SetActive(false);
            //AngleRootPelvis_Side.gameObject.SetActive(false);
            ProShoulderAngleRoot_Front.gameObject.SetActive(false);
            ProPelvisAngleRoot_Front.gameObject.SetActive(false);

            ShowSpinMeter(false);

            Utillity.Instance.HideToast(true);
        }
        else
        {
            BalanaceFront.transform.localScale = new Vector3(-1, 1, 1) * (guideLineSide.ScreenScale * 2f);
            BalanaceBack.transform.localScale = new Vector3(-1, 1, 1) * (guideLineSide.ScreenScale * 2f);
            BalanaceLeft.transform.localScale = Vector3.one * (guideLineFront.ScreenScale * 2f);
            BalanaceRight.transform.localScale = Vector3.one * (guideLineFront.ScreenScale * 2f);

            WallLeft.transform.localScale = new Vector3(-1, 1, 1) * (guideLineFront.ScreenScale * 2f);
            WallRight.transform.localScale = new Vector3(-1, 1, 1) * (guideLineFront.ScreenScale * 2f);
            WallFront.transform.localScale = Vector3.one * (guideLineSide.ScreenScale * 2f);
            WallBack.transform.localScale = Vector3.one * (guideLineSide.ScreenScale * 2f);

            //FrontShoulderRoot.localScale = new Vector3(-1, 1, 1) * (guideLineFront.ScreenScale * 2f);
            //FrontPelvisRoot.localScale = new Vector3(-1, 1, 1) * (guideLineFront.ScreenScale * 2f);
            //FrontSpineRoot.localScale = new Vector3(-1, 1, 1) * (guideLineFront.ScreenScale * 2f);
            PoseFrontHeadRoot.localScale = new Vector3(-1, 1, 1) * (guideLineFront.ScreenScale * 1f);
            
            //SideSpineRoot.localScale = Vector3.one * (guideLineSide.ScreenScale * 2f);
            FrontHandRoot.localScale = Vector3.one * (guideLineFront.ScreenScale * 1f);
            //SideHandFailRoot.localScale = Vector3.one * (guideLineSide.ScreenScale * 2f);
            SideHandRoot.localScale = Vector3.one * (guideLineSide.ScreenScale * 1f);
            PoseSideHeadRoot.localScale = Vector3.one * (guideLineSide.ScreenScale * 1f);

            AngleRootShoulder_Front.localScale = Vector3.one * (guideLineFront.ScreenScale);
            AngleRootPelvis_Front.localScale = Vector3.one * (guideLineFront.ScreenScale);

            AngleRootShoulder_Front.gameObject.SetActive(true);
            AngleRootPelvis_Front.gameObject.SetActive(true);

            AngleDrillShoulder_Front.localScale = Vector3.one * (guideLineFront.ScreenScale);
            AngleDrillPelvis_Front.localScale = Vector3.one * (guideLineFront.ScreenScale);

            FootDisRateRoot.localScale = Vector3.one * (guideLineFront.ScreenScale);
            KneeDisRateRoot.localScale = Vector3.one * (guideLineFront.ScreenScale);
        }
    }

    public void ShowSpinMeter(bool isOn)
    {
        ProShoulderAngleRoot_Front.gameObject.SetActive(isOn);
        ProPelvisAngleRoot_Front.gameObject.SetActive(isOn);

        WallBack.gameObject.SetActive(isOn);
        WallFront.gameObject.SetActive(isOn);
        WallLeft.gameObject.SetActive(isOn);
        WallRight.gameObject.SetActive(isOn);
    }

    void SetSwingStepInfo(SWINGSTEP step)
    {

        moveStepX = -75f - (250f * ((int)step + 1));
    }


    bool _inDrill = false;
    float _drillTimer = 0;

    public void SetDrill(bool isDrill)
    {
        if(isDrill)
        {
            Debug.Log("Start DRILL");
            _inDrill = true;
            _drillTimer = 0;
            AngleDrillShoulder_Front.gameObject.SetActive(true);
            AngleDrillPelvis_Front.gameObject.SetActive(true);

            guideLineSide.LinePause = true;
            guideLineFront.LinePause = true;
            AngleDrillShoulder_Front.localPosition = webcamTrackerFront.KalmanPositions.CenterShoulder;
            AngleDrillPelvis_Front.localPosition = webcamTrackerFront.KalmanPositions.CenterPelvis;
        }
        else
        {
            Debug.Log("Stop DRILL");
            _inDrill = false;
            _drillTimer = 0;
            AngleDrillShoulder_Front.gameObject.SetActive(false);
            AngleDrillPelvis_Front.gameObject.SetActive(false);

            guideLineSide.LinePause = false;
            guideLineFront.LinePause = false;
        }
    }

    void SwingPoseDetect()
    {
        int swingAngle = sensorProcess.iGetHandDir;
        int swingAngleBtm = sensorProcess.iGetHandDirFromBottom;
        //txtDebug.text = $"swingAngle : {swingAngle}\r\nturnTop:{turnTop}";
        //골반 테스트
                
        ProLeftPelvis = (SkLandmark2Ds[23].position * adjustRate) + adjustDir; //보정된 프로의 골반 위치
        ProRightPelvis = (SkLandmark2Ds[24].position * adjustRate) + adjustDir; //보정된 프로의 골반 위치

        if (isReverse == true)
        {
            if (swingStep == SWINGSTEP.READY)
            {   
                if (sensorProcess.IsAddressHand(false) && swingAngle < 190 && swingAngle > 170)
                {
                    CheckAddressTime += Time.deltaTime;
                    if (CheckAddressTime > 1f)
                    {
                        swingStep = SWINGSTEP.ADDRESS;
                        SetSwingStepInfo(swingStep);
                        ResetProLine();
                        Utillity.Instance.HideToast();
                        ShowSpinMeter(true);
                        CheckAddressTime = 0;
                        ResetCheckString();
                        turnPauseAngle = swingAngle - 3;
                        if (turnPauseAngle < turnTakeback)
                            turnPauseAngle = turnTakeback;

                        _inDrill = false;
                        _drillTimer = 0;
                        LoadSkeletonSwint(swingStep);
                        //ProLeftPelvis = webcamTrackerFront.KalmanPositions.LeftPelvis - SkLandmark2Ds[23].position;
                        //ProRightPelvis = webcamTrackerFront.KalmanPositions.RightPelvis - SkLandmark2Ds[24].position;

                    }
                }
                else
                {
                    CheckAddressTime = 0;

                    if(_inDrill == false && sensorProcess.IsDrillPose())
                    {
                        _drillTimer += Time.deltaTime;
                        //Debug.Log($"Check DRILL - {_drillTimer:F2}");
                        if(_drillTimer > 1f)
                        {
                            SetDrill(true);
                        }
                    }
                    else if(_inDrill && sensorProcess.IsDrillPose(_inDrill) == false)
                    {
                        SetDrill(false);
                    }
                }
            }
            else if (swingStep == SWINGSTEP.ADDRESS)
            {
                //txtDebug.text = $"GetShoulderAngle:{GetSwingDataValue("GetShoulderAngle", SWINGSTEP.ADDRESS)} == {sensorProcess.iGetShoulderAngle}\r\n";
                //txtDebug.text += $"GetNoseDir:{GetSwingDataValue("GetNoseDir", SWINGSTEP.ADDRESS)} == {sensorProcess.iGetNoseDir}";
                //txtDebug.text = $"HandTop : {sensorProcess.iGetHandDir}\r\nHandBtm : {sensorProcess.iGetHandDirFromBottom}\r\nBalance : {sensorProcess.iGetWeight}";
                if (CoachingVisible)
                {
                    CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5
                
                
                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부
                }
                
                //골반 테스트
                adjustRate = Vector2.Distance(webcamTrackerFront.KalmanPositions.LeftPelvis, webcamTrackerFront.KalmanPositions.RightPelvis) / Vector2.Distance(SkLandmark2Ds[23].position , SkLandmark2Ds[24].position);
                Vector2 pelvisCenter = ((SkLandmark2Ds[23].position + SkLandmark2Ds[24].position) * adjustRate) / 2;
                adjustDir = webcamTrackerFront.KalmanPositions.CenterPelvis - pelvisCenter;



                SwingLimitUp = guideLineSide.SwingLimitUp;
                SwingLimitDown = guideLineSide.SwingLimitDown;
                ballPosition = guideLineSide.ballPosition.anchoredPosition;

                HandFrontDistance = sensorProcess.iGetHandDirDistance;
                HandSideDistance = sensorProcess.iGetHandSideDistance;

                //가이드 라인 정지
                if (swingAngle <= turnPauseAngle && swingAngleBtm <= turnPauseAngle && Mathf.Abs(ProValue_4-sensorProcess.iGetWeight) < 5f)
                {
                    guideLineSide.LinePause = true;
                    guideLineFront.LinePause = true;

                    if (SideHandSideDistanceScale < 0)
                        SideHandSideDistanceScale = HandSideDistance / GetSwingDataValue("GetHandSideDistance", SWINGSTEP.ADDRESS);
                    if (FrontHandSideDistanceScale < 0)
                        FrontHandSideDistanceScale = HandFrontDistance / GetSwingDataValue("GetHandDirDistance", SWINGSTEP.ADDRESS);                    
                }

                if (swingAngle <= turnTakeback || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.TAKEBACK;
                    SetSwingStepInfo(swingStep);                    
                    ResetProLine();                    
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
            else if (swingStep == SWINGSTEP.TAKEBACK)
            {
                if (CoachingVisible)
                {
                    CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }

                if (swingAngle <= turnBackswing || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.BACKSWING;
                    SetSwingStepInfo(swingStep);
                    ResetProLine();
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
            else if (swingStep == SWINGSTEP.BACKSWING)
            {
                if (CoachingVisible)
                {
                    CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }

                if (swingAngle <= turnTop || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.TOP;
                    SetSwingStepInfo(swingStep);
                    ResetProLine();
                    isReverse = false;
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
        }
        else
        {
            if (swingStep == SWINGSTEP.TOP)
            {
                if (CoachingVisible)
                {
                    CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }

                if (swingAngle >= turnDownswind || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.DOWNSWING;
                    SetSwingStepInfo(swingStep);
                    ResetProLine();
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
            else if (swingStep == SWINGSTEP.DOWNSWING)
            {
                if (CoachingVisible)
                {
                    CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }

                if (swingAngle >= turnImpact || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.IMPACT;
                    SetSwingStepInfo(swingStep);
                    ResetProLine();
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
            else if (swingStep == SWINGSTEP.IMPACT)
            {
                if (CoachingVisible)
                {
                    CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }

                if (swingAngle >= turnFollow || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.FOLLOW;
                    SetSwingStepInfo(swingStep);
                    ResetProLine();
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
            else if (swingStep == SWINGSTEP.FOLLOW)
            {
                if (CoachingVisible)
                {
                    //CheckHandSidePosition(); //측면 손 위치
                    //CheckHandDir(); //정면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    CheckShoulderAngle(); //2
                    CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }

                if (swingAngle >= turnFinish || MoveStep > 0)
                {
                    MoveStep = 0;
                    swingStep = SWINGSTEP.FINISH;
                    SetSwingStepInfo(swingStep);
                    ResetProLine();
                    ResetCheckString();
                    LoadSkeletonSwint(swingStep);
                }
            }
            else if (swingStep == SWINGSTEP.FINISH)
            {
                if (CoachingVisible)
                {
                    //CheckHandSidePosition(); //측면 손 위치
                    CheckGetFootDisRate();//다리 간격
                    CheckGetKneeDisRate();//무릎 간격

                    CheckWaistSideDir(); //1
                    //CheckShoulderAngle(); //2
                    //CheckPelvisAngle(); //3

                    CheckBalance(); //4
                    //CheckHeadDir(); //5

                    //CheckHandSideAngle(); //측면 손이 벗어나는지 여부  
                }
                
                if (swingAngle < 190 && swingAngle > 170 || MoveStep > 0)
                {
                    SetReady();
                }
            }
        }

        if(_inDrill == false)
        {

            //자세 풀기
            if ((swingStep != SWINGSTEP.FINISH && sensorProcess.IsAddressHand(false) == false && swingAngle < 200 && swingAngle > 160) || MoveStep < 0)
            {
                SetReady();
            }
            if (swingStep != SWINGSTEP.READY && swingStep != SWINGSTEP.ADDRESS)
            {
                //다른 동작중 어드레스 감지
                if (sensorProcess.IsAddressHand(false) && swingAngle < 190 && swingAngle > 170
                    && Mathf.Abs(sensorProcess.iGetWeight) < 10 && sensorProcess.iGetShoulderAngle > 80)
                {
                    CheckAddressTime += Time.deltaTime;
                    if (CheckAddressTime > 1f)
                    {
                        SetReady();
                    }
                }
                else
                    CheckAddressTime = 0;
            }
        }
    }
    
    void LoadSkeletonSwint(SWINGSTEP step)
    {
        if (swingStepSkData != null && swingStepSkData.stepLandmarks.TryGetValue(step, out var lm))
        {
            SkLandmark2Ds = lm.front2D;
        }
    }

    void SetReady()
    {
        //Debug.Log("SetReady()");

        MoveStep = 0;
        swingStep = SWINGSTEP.READY;
        SetSwingStepInfo(swingStep);
        ResetProLine();
        guideLineSide.LinePause = false;
        guideLineFront.LinePause = false;
        isReverse = true;
        SideHandSideDistanceScale = -1f;
        FrontHandSideDistanceScale = -1f;
        HandFrontDistance = -1f;
        HandSideDistance = -1f;
        ShowSpinMeter(false);
        turnPauseAngle = turnTakeback;

        CheckAddressTime = 0;
        /*
        for (int i = 0; i < 8; i++)
        {
            HandHistoryNodes[i].gameObject.SetActive(false);
        }*/

        txtPro_1.text = "-";
        txtPro_2.text = "-";
        txtPro_3.text = "-";
        txtPro_4.text = "-";
        txtPro_5.text = "-";

        ResetCheckString();

        if (Coaching)
            Utillity.Instance.ShowToast("어드레스 자세를 잡아주세요", true);
    }
    
    void ResetCheckString()
    {
        txtCheckString_1.text = "-";
        txtCheckString_2.text = "-";
        txtCheckString_3.text = "-";
        txtCheckString_4.text = "-";
        txtCheckString_5.text = "-";
        txtCheckString_1.color = colBalanced;
        txtCheckString_2.color = colBalanced;
        txtCheckString_3.color = colBalanced;
        txtCheckString_4.color = colBalanced;
        txtCheckString_5.color = colBalanced;
    }

    void ResetProLine()
    {
        //FrontShoulderRoot.gameObject.SetActive(false);
        //FrontPelvisRoot.gameObject.SetActive(false);
        //FrontSpineRoot.gameObject.SetActive(false);
        //SideSpineRoot.gameObject.SetActive(false);

        FrontHandRoot.gameObject.SetActive(false);
        //SideHandFailRoot.gameObject.SetActive(false);
        SideHandRoot.gameObject.SetActive(false);
        //SideWallRoot.gameObject.SetActive(false);

        ProShoulderAngle_Front.gameObject.SetActive(false);
        ProPelvisAngle_Front.gameObject.SetActive(false);

        //ShoulderArrowLeftRoot_Front.gameObject.SetActive(false);
        //ShoulderArrowLeft_Front.gameObject.SetActive(false);

        PoseSideHeadRoot.gameObject.SetActive(false);
        PoseFrontHeadRoot.gameObject.SetActive(false);

        FootDisRateRoot.gameObject.SetActive(false);
        KneeDisRateRoot.gameObject.SetActive(false);

    }

    void CheckBalance()
    {
        ProValue_4 = GetSwingDataValue("GetWeight", swingStep);
        txtPro_4.text = GetSwingDataValue("GetWeight", swingStep).ToString();
        GetCheckValueString(in txtCheckString_4, ProValue_4, sensorProcess.iGetWeight, 8, 18);
    }

    void CheckHeadDir()
    {
        ProValue_5 = GetSwingDataValue("GetNoseDir", swingStep);
        txtPro_5.text = GetSwingDataValue("GetNoseDir", swingStep).ToString();
        GetCheckValueString(in txtCheckString_5, ProValue_5, sensorProcess.iGetNoseDir, 10, 20);
        //txtDebug.text = $"{ProValue_5} : {txtPro_5.text}";
        if (swingStep == SWINGSTEP.ADDRESS && guideLineFront.LinePause == false)
        {
            PoseSideHeadRoot.gameObject.SetActive(true);
            float headDir = GetSwingDataValue("GetNosePelvisSideDir", SWINGSTEP.ADDRESS) * Mathf.Deg2Rad;
            float len = Vector2.Distance(webcamTrackerSide.KalmanPositions.RightPelvis, webcamTrackerSide.KalmanPositions.Nose);

            PoseSideHeadRoot.localPosition = webcamTrackerSide.KalmanPositions.RightPelvis +
                                (new Vector2(-Mathf.Sin(headDir), Mathf.Cos(headDir)) * len);
            //PoseSideHeadRoot.localPosition = webcamTrackerSide.KalmanPositions.RightPelvis + (headPos * len);            
            PoseSideHeadRoot.localRotation = Quaternion.Euler(new Vector3(0, 0, GetSwingDataValue("GetNoseShoulderSideDir", SWINGSTEP.ADDRESS)));

            PoseFrontHeadRoot.gameObject.SetActive(true);
            headDir = ProValue_5 * Mathf.Deg2Rad;
            len = Vector2.Distance(webcamTrackerFront.KalmanPositions.CenterShoulder, webcamTrackerFront.KalmanPositions.Nose);
            PoseFrontHeadRoot.localPosition = webcamTrackerFront.KalmanPositions.CenterShoulder +
                                (new Vector2(-Mathf.Sin(headDir), Mathf.Cos(headDir)) * len);
            PoseFrontHeadRoot.localRotation = Quaternion.Euler(new Vector3(0, 0, ProValue_5));
            
            //txtDebug.text = $"{ProValue_5} : {sensorProcess.iGetNoseDir}";
            //txtDebug.text += $"\r\n{GetSwingDataValue("GetNosePelvisSideDir", SWINGSTEP.ADDRESS)} : {sensorProcess.iGetNosePelvisSideDir}";
        }
        else
        {
            PoseSideHeadRoot.gameObject.SetActive(false);
            PoseFrontHeadRoot.gameObject.SetActive(false);
        }
    }

    void VisibleWeight()
    {
        if (webcamTrackerFront.Landmark == null || webcamTrackerFront.Landmark[27].visibility < 0.5f || webcamTrackerFront.Landmark[28].visibility < 0.5f)
        {
            BalanaceLeft.SetActive(false);
            BalanaceRight.SetActive(false);
        }
        else
        {
            BalanaceLeft.SetActive(true);
            BalanaceRight.SetActive(true);

            //WallLeft.gameObject.SetActive(true);
            //WallRight.gameObject.SetActive(true);

            float weightRate = Mathf.Clamp(sensorProcess.iGetWeight, -25, 25);
            int _balanceValue = (int)(weightRate * 2f);

            Vector2 vecl = guideLineFront.filteringFootLeft;
            Vector2 vecr = guideLineFront.filteringFootRight;

            if (vecl.x < vecr.x)
                vecl.x = vecr.x;
            else
                vecr.x = vecl.x;

            WallLeft.transform.localPosition = vecl;
            WallRight.transform.localPosition = vecr;


            textBalanceLeft.text = $"{50 - _balanceValue}<size=30>%</size>";
            textBalanceRight.text = $"{50 + _balanceValue}<size=30>%</size>";
            imgBalanceLeft.fillAmount = (0.5f - (_balanceValue / 100f));
            imgBalanceRight.fillAmount = (0.5f + (_balanceValue / 100f));
        }

        if(webcamTrackerSide.Track)
        {
            if (webcamTrackerSide.Landmark == null || webcamTrackerSide.Landmark[28].visibility < 0.5f)
            {
                BalanaceFront.SetActive(false);
                BalanaceBack.SetActive(false);

            }
            else
            {
                BalanaceFront.SetActive(true);
                BalanaceBack.SetActive(true);
                
                float _value = Mathf.InverseLerp(0, 150, sensorProcess.iGetSideWeight); //0 ~ 150

                imgBalanceFront.fillAmount = _value;
                imgBalanceBack.fillAmount = 1f - _value;
                textBalanceFront.text = $"{(int)(_value * 100)}<size=30>%</size>";
                textBalanceBack.text = $"{(int)((1f - _value) * 100)}<size=30>%</size>";
            }
        }
    }


    void CheckShoulderAngle()
    {

        ProShoulderAngle_Front.gameObject.SetActive(true);
        float retAngle = 0;
        if (SWINGSTEP.ADDRESS == swingStep || SWINGSTEP.IMPACT == swingStep)
        {
            retAngle = GetSwingDataValue("GetShoulderAngle", swingStep);
        }
        else if (SWINGSTEP.FINISH == swingStep)
        {
            retAngle = GetSwingDataValue("GetShoulderSideDirWorld", swingStep);
        }
        else if (SWINGSTEP.TAKEBACK == swingStep || SWINGSTEP.BACKSWING == swingStep || SWINGSTEP.DOWNSWING == swingStep)
        {
            retAngle = GetSwingDataValue("GetShoulderFrontDirWorld", swingStep);
        }
        else if (SWINGSTEP.TOP == swingStep || SWINGSTEP.FOLLOW == swingStep)
        {
            retAngle = GetSwingDataValue("GetShoulderDir", swingStep);
        }
        else
            retAngle = 0;

        ProShoulderAngle_Front.localRotation = Quaternion.Euler(0, 0, retAngle);
        ProValue_2 = (int)retAngle;
        txtPro_2.text = retAngle.ToString();
        GetCheckValueString(in txtCheckString_2, ProValue_2, (int)frontShoulderAngle, 10, 20);
    }

    void CheckPelvisAngle()
    {
        ProPelvisAngle_Front.gameObject.SetActive(true);
        float retAngle = 0;
        if (SWINGSTEP.ADDRESS == swingStep)
        {
            retAngle = GetSwingDataValue("GetPelvisAngle", swingStep);
        }
        /*
        else if (SWINGSTEP.FINISH == swingStep || SWINGSTEP.FOLLOW == swingStep)
        {
            retAngle = GetSwingDataValue("GetPelvisSideDirWorld", swingStep);
        }*/
        else if (SWINGSTEP.TAKEBACK == swingStep || SWINGSTEP.BACKSWING == swingStep || SWINGSTEP.DOWNSWING == swingStep || SWINGSTEP.IMPACT== swingStep || SWINGSTEP.FINISH == swingStep)
        {
            retAngle = GetSwingDataValue("GetPelvisFrontDirWorld", swingStep);
        }
        else if (SWINGSTEP.TOP == swingStep ||SWINGSTEP.FOLLOW == swingStep)
        {
            retAngle = GetSwingDataValue("GetPelvisDir", swingStep);
        }

        ProPelvisAngle_Front.localRotation = Quaternion.Euler(0, 0, retAngle);
        ProValue_3 = (int)retAngle;
        txtPro_3.text = retAngle.ToString();
        GetCheckValueString(in txtCheckString_3, ProValue_3, (int)frontPelvisAngle, 10, 20);
    }

    void CheckWaistSideDir()
    {
        /*
        Vector2 cShoulder = Vector2.zero;
        Vector2 cPelvis = Vector2.zero;
        if (webcamTrackerSide.Landmark?[12].visibility > 0.5f)
        {
            if (webcamTrackerSide.Landmark[11].visibility > 0.5f)
                cShoulder = (webcamTrackerSide.Landmark[11].position + webcamTrackerSide.Landmark[12].position) / 2;
            else
                cShoulder = webcamTrackerSide.Landmark[12].position;
            SideSpineRoot.gameObject.SetActive(true);
        }
        else
            SideSpineRoot.gameObject.SetActive(false);

        //골반 중심
        if (webcamTrackerSide.Landmark?[24].visibility > 0.5f)
        {
            if (webcamTrackerSide.Landmark[23].visibility > 0.5f)
                cPelvis = (webcamTrackerSide.Landmark[23].position + webcamTrackerSide.Landmark[24].position) / 2;
            else
                cPelvis = webcamTrackerSide.Landmark[24].position;
            SideSpineRoot.gameObject.SetActive(true);
        }
        else
            SideSpineRoot.gameObject.SetActive(false);

        SideSpineRoot.localPosition = (cShoulder + cPelvis) / 2;
        */
        ProValue_1 = GetSwingDataValue("GetWaistSideDir");
        txtPro_1.text = GetSwingDataValue("GetWaistSideDir").ToString();
        GetCheckValueString(in txtCheckString_1, ProValue_1, sensorProcess.iGetWaistSideDir, 5, 10);
    }

    void CheckHandSideAngle()
    {
        try
        {
            if (webcamTrackerSide.Landmark != null && webcamTrackerSide.Landmark[16].visibility > 0.3f)
            {
                Vector2 sideHandPos = webcamTrackerSide.Landmark[16].position;
                Vector2 dir = (sideHandPos - ballPosition).normalized;
                int sideHandAngle = (int)(Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg) + 180;


/*
                if (sideHandAngle < SwingLimitDown || sideHandAngle > SwingLimitUp)
                {
                    SideHandFailRoot.gameObject.SetActive(true);
                    SideHandFailRoot.localPosition = sideHandPos;
                }
                else
                    SideHandFailRoot.gameObject.SetActive(false);
                    */
            }
            //else
            //    SideHandFailRoot.gameObject.SetActive(false);
        }
        catch
        {
            //SideHandFailRoot.gameObject.SetActive(false);
        }
    }

    void CheckHandSidePosition()
    {
        SideHandRoot.gameObject.SetActive(true);
        Vector2 cShoulder = webcamTrackerSide.KalmanPositions.CenterShoulder;
        float handDis = 0;
        if (swingStep == SWINGSTEP.ADDRESS)
            handDis = (sensorProcess.iGetHandSideDistance);// Vector2.Distance(cShoulder, webcamTrackerSide.landmark[16].position);
        else
            handDis = (GetSwingDataValue("GetHandSideDistance") * SideHandSideDistanceScale);

        Vector2 rotated = Quaternion.Euler(0, 0, GetSwingDataValue("GetHandSideDir")) * new Vector2(-1, 0);//cShoulder;
        SideHandRoot.localPosition = cShoulder + (rotated.normalized * handDis);
    }

    void CheckHandDir()
    {
        FrontHandRoot.gameObject.SetActive(true);
        Vector2 cShoulder = webcamTrackerFront.KalmanPositions.CenterShoulder;
        float handDis = 0;
        if (swingStep == SWINGSTEP.ADDRESS)
            handDis = (sensorProcess.iGetHandDirDistance);// HandFrontDistance;
        else
            handDis = (GetSwingDataValue("GetHandDirDistance") * FrontHandSideDistanceScale);

        Vector2 rotated = Quaternion.Euler(0, 0, GetSwingDataValue("GetHandDir")) * new Vector2(-1, 0);//cShoulder;
        FrontHandRoot.localPosition = cShoulder + (rotated.normalized * handDis);
    }

    int GetSwingDataValue(string dataName, SWINGSTEP step = SWINGSTEP.CHECK)
    {
        if (step == SWINGSTEP.CHECK)
            step = swingStep;

        if (step == SWINGSTEP.ADDRESS)
            return swingStepData.dicAddress[dataName];
        else if (step == SWINGSTEP.TAKEBACK)
            return swingStepData.dicTakeback[dataName];
        else if (step == SWINGSTEP.BACKSWING)
            return swingStepData.dicBackswing[dataName];
        else if (step == SWINGSTEP.TOP)
            return swingStepData.dicTop[dataName];
        else if (step == SWINGSTEP.DOWNSWING)
            return swingStepData.dicDownswing[dataName];
        else if (step == SWINGSTEP.IMPACT)
            return swingStepData.dicImpact[dataName];
        else if (step == SWINGSTEP.FOLLOW)
            return swingStepData.dicFollow[dataName];
        else if (step == SWINGSTEP.FINISH)
            return swingStepData.dicFinish[dataName];
        else
            return -1;
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
            if (Mathf.Approximately(ret, 0)) ret = 0.01f;
        }

        return ret;
    }

    void GetCheckValueString(in TextMeshProUGUI targetText, int proVal, int userVal, int lackingVal, int toomuchVal)
    {
        int gap = Mathf.Abs(proVal - userVal);
        string retStr = string.Empty;
        if (gap > toomuchVal)
        {
            retStr = $"과함";
            targetText.color = colTooMuch;
        }
        else if (gap > lackingVal)
        {
            retStr = $"부족";
            targetText.color = colLacking;                
        }
        else
        {
            retStr = $"적정";
            targetText.color = colBalanced;
        }
        
        targetText.text = retStr;
    }

    Vector2 adjustDir  =Vector2.zero;
    float adjustRate = 1f;

    public void CheckGetFootDisRate()
    {
        
        FootDisRateRoot.gameObject.SetActive(true);
        
        Vector2 FootDir = (SkLandmark2Ds[27].position - SkLandmark2Ds[23].position).normalized;
        float dis = Vector2.Distance(webcamTrackerFront.KalmanPositions.LeftFoot, ProLeftPelvis);
        Vector2 FootPos = ProLeftPelvis + (FootDir * dis);
        FootPos.x = webcamTrackerFront.KalmanPositions.LeftFoot.x;
        FootRate_LeftPosition.localPosition = FootPos;//ProLeftPelvis + (FootDir * dis);

        FootDir = (SkLandmark2Ds[28].position - SkLandmark2Ds[24].position).normalized;
        dis = Vector2.Distance(webcamTrackerFront.KalmanPositions.RightFoot, ProRightPelvis);
        FootPos = ProRightPelvis + (FootDir * dis);
        FootPos.x = webcamTrackerFront.KalmanPositions.RightFoot.x;
        FootRate_RightPosition.localPosition = FootPos;//ProRightPelvis + (FootDir * dis);
        
    }

    public void CheckGetKneeDisRate()
    {
        KneeDisRateRoot.gameObject.SetActive(true);

        Vector2 KneeDir = (SkLandmark2Ds[25].position - SkLandmark2Ds[23].position).normalized;
        float dis = Vector2.Distance(webcamTrackerFront.KalmanPositions.LeftKnee, ProLeftPelvis);
        Vector2 KneePos = ProLeftPelvis + (KneeDir * dis);
        KneePos.x = webcamTrackerFront.KalmanPositions.LeftKnee.x;
        KneeRate_LeftPosition.localPosition = KneePos;//ProLeftPelvis + (KneeDir * dis);

        KneeDir = (SkLandmark2Ds[26].position - SkLandmark2Ds[24].position).normalized;
        dis = Vector2.Distance(webcamTrackerFront.KalmanPositions.RightKnee, ProRightPelvis);
        KneePos = ProRightPelvis + (KneeDir * dis);
        KneePos.x = webcamTrackerFront.KalmanPositions.RightKnee.x;
        KneeRate_RightPosition.localPosition = KneePos;//ProRightPelvis + (KneeDir * dis);
        //KneeRate_RightPosition.localPosition = SkLandmark2Ds[26].position  * adjustRate + adjustDir;
    }


    //=============================================
    // CHECK
    //=============================================
    /*
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


    public void CheckGetShoulderDir(int bas, int allow)// 16
    {
        ShoulderDir = CalAngleDiff(bas, allow, sensorProcess.iGetShoulderDir, true);
    }

    public void CheckGetPelvisDir(int bas, int allow)// 17
    {
        PelvisDir = CalAngleDiff(bas, allow, sensorProcess.iGetPelvisDir, true);
    }

    public void CheckLeftElbowSideVis()
    {
        LeftElbowSideVis = sensorProcess.fLeftElbowSideVis;
    }
    */
}
