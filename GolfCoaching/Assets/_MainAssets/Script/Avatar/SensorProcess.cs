using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static RootMotion.FinalIK.Grounding;
using Enums;

public class SensorProcess : MonoBehaviour
{
    //[SerializeField] webcamclient client;
    public WebcamTracker clientFront;
    public WebcamTracker clientSide;
    public float visibilityFront;
    public float visibilitySide;

    public EMirroViewType mirroViewType = EMirroViewType.OFF;

    public bool AutoStartDetect = false;

    // front
    //KalmanFilter _iGetHandDir = new KalmanFilter();
    AngleKalmanFilter _iGetHandDir = new AngleKalmanFilter();
    [HideInInspector] public int iGetHandDir; //각도 0~360
    [HideInInspector] public int iGetHandDirNF; //각도 0~360 (필터없음)

    KalmanFilter _iGetHandDistance = new KalmanFilter();
    [HideInInspector] public int iGetHandDistance;

    KalmanFilter _iGetShoulderDistance = new KalmanFilter();
    [HideInInspector] public int iGetShoulderDistance;

    KalmanFilter _iGetSpineDir = new KalmanFilter();
    [HideInInspector] public int iGetSpineDir;

    KalmanFilter _iGetShoulderAngle = new KalmanFilter();
    [HideInInspector] public int iGetShoulderAngle;

    KalmanFilter _iGetWeight = new KalmanFilter();
    [HideInInspector] public int iGetWeight;
    [HideInInspector] public float fGetPelvisDir;

    KalmanFilter _iGetFootDisRate = new KalmanFilter();
    [HideInInspector] public int iGetFootDisRate;

    KalmanFilter _iGetForearmAngle = new KalmanFilter();
    [HideInInspector] public int iGetForearmAngle; //각도

    KalmanFilter _iGetElbowFrontDir = new KalmanFilter();
    [HideInInspector] public int iGetElbowFrontDir;

    KalmanFilter _iGetElbowRightFrontDir = new KalmanFilter();
    [HideInInspector] public int iGetElbowRightFrontDir;

    KalmanFilter _iGetHandDirDistance = new KalmanFilter();
    [HideInInspector] public int iGetHandDirDistance;

    AngleKalmanFilter _iGetShoulderFrontDirWorld = new AngleKalmanFilter();
    [HideInInspector] public int iGetShoulderFrontDirWorld;
    [HideInInspector] public int iGetShoulderFrontDirWorldNF;

    AngleKalmanFilter _iGetPelvisFrontDirWorld = new AngleKalmanFilter();
    [HideInInspector] public int iGetPelvisFrontDirWorld;
    [HideInInspector] public int iGetPelvisFrontDirWorldNF;

    AngleKalmanFilter _iGetPelvisAngle = new AngleKalmanFilter();
    [HideInInspector] public int iGetPelvisAngle;

    KalmanFilter _iGetNoseDir = new KalmanFilter();
    [HideInInspector] public int iGetNoseDir;

    Vector2 handVectorFront;
    Vector2 handVectorSide;

    AngleKalmanFilter _iGetHandDirFromBottom = new AngleKalmanFilter();
    [HideInInspector] public int iGetHandDirFromBottom; //각도 0~360

    KalmanFilter _iGetKneeDisRate = new KalmanFilter();
    [HideInInspector] public int iGetKneeDisRate;

    //Side
    KalmanFilter _iGetWaistSideDir = new KalmanFilter();
    [HideInInspector] public int iGetWaistSideDir;

    KalmanFilter _iGetHandSideDir = new KalmanFilter();
    [HideInInspector] public int iGetHandSideDir;
    [HideInInspector] public int iGetHandSideDirNF;

    KalmanFilter _iGetKneeSideDir = new KalmanFilter();
    [HideInInspector] public int iGetKneeSideDir;

    KalmanFilter _iGetElbowSideDir = new KalmanFilter();
    [HideInInspector] public int iGetElbowSideDir;

    KalmanFilter _iGetArmpitDir = new KalmanFilter();
    [HideInInspector] public int iGetArmpitDir;

    KalmanFilter _iGetSideWeight = new KalmanFilter();
    [HideInInspector] public int iGetSideWeight;
    [HideInInspector] public float fGetSPCSideDir;

    KalmanFilter _iGetHandSideDistance = new KalmanFilter();
    [HideInInspector] public int iGetHandSideDistance;

    KalmanFilter _iGetGripDistance = new KalmanFilter();
    [HideInInspector] public int iGetGripDistance;
    
    AngleKalmanFilter _iGetShoulderSideDirWorld = new AngleKalmanFilter();
    [HideInInspector] public int iGetShoulderSideDirWorld;
    [HideInInspector] public int iGetShoulderSideDirWorldNF;

    AngleKalmanFilter _iGetPelvisSideDirWorld = new AngleKalmanFilter();
    [HideInInspector] public int iGetPelvisSideDirWorld;
    [HideInInspector] public int iGetPelvisSideDirWorldNF;

    AngleKalmanFilter _iGetNoseShoulderSideDir = new AngleKalmanFilter();
    [HideInInspector] public int iGetNoseShoulderSideDir;

    KalmanFilter _iGetNosePelvisSideDir = new KalmanFilter();
    [HideInInspector] public int iGetNosePelvisSideDir;


    //Combine
    AngleKalmanFilter _iGetShoulderDir = new AngleKalmanFilter();
    [HideInInspector] public int iGetShoulderDir;

    AngleKalmanFilter _iGetPelvisDir = new AngleKalmanFilter();
    [HideInInspector] public int iGetPelvisDir;
    

    //KalmanFilter[] _vRightElbowDir = new KalmanFilter[3];
    //[HideInInspector] public Vector3 vRightElbowDir;

    KalmanFilter _iGetHandCombineDir = new KalmanFilter();
    [HideInInspector] public int iGetHandCombineDir; //각도 0~360

    //options
    [HideInInspector] public float fLeftElbowSideVis;
    [HideInInspector] public float fRightElbowFrontVis;
    bool bNormal = false;
    bool _isGrip = false;

    public bool Normal { get { return bNormal; } }

    Vector2 CheckAdressCenterShoulder = Vector2.zero;
    [HideInInspector] public float DistanceAdressCenterShoulder = 0;


    


    [SerializeField] float frontLenth = 1.25f;  //정면카메라와 거리 m
    [SerializeField] float sideLenth = 1.4f;    //측면카메라와 거리 m
    float cmbScale = 1f;

    [SerializeField] TextMeshProUGUI txtDebug;


    public bool handVectorIsLeft = true;

    bool startAuto = false;

    private void Awake()
    {
        cmbScale = sideLenth / frontLenth;
        //for (int i = 0; i < 3; i++) _vRightElbowDir[i] = new KalmanFilter();
    }

    void Start()
    {
        //시작할때 자동 측저시작
        if (AutoStartDetect)
            StartAutoDetect();
    }

    public void StartAutoDetect()
    {
        if (startAuto)
            return;

        if(clientFront == null || clientSide == null)
        {
            Debug.LogError($"Need WebcamClident clientFront:{clientFront} / clientSide:{clientSide}");
            return;
        }

        startAuto = true;
        StartCoroutine(UpdateSensor());
    }

    public void StopAudoDetect()
    {
        startAuto = false;
    }

    IEnumerator UpdateSensor()
    {
        //if (Input.GetKeyUp(KeyCode.Escape))
        //{
        //    Application.Quit();
        //}
        while (startAuto)
        {
            visibilityFront = clientFront.visibilityAvg;
            visibilitySide = clientSide.visibilityAvg;

            /*  Front  */
            if (clientFront.Track)
            {
                iGetHandDistance = (int)_iGetHandDistance.Update(GetHandDistance());  //양 손목 사이 거리 (카메라와 거리에 따라 상대적)
                iGetShoulderDistance = (int)_iGetShoulderDistance.Update(GetShoulderDistance());  //양 어깨 사이 거리 (카메라와 거리에 따라 상대적)

                GetHandPosition();  //스윙각도 계산을 위한 기준 손 좌표
                iGetHandDir = (int)_iGetHandDir.Update(GetHandDir());   //스윙각도 0~360도, 백스윙쪽이 각도가 줄어들고 팔로우쪽이 증가한다. 어드레서 약 180도
                iGetSpineDir = (int)_iGetSpineDir.Update(GetSpineDir());  //허리 정면 각도. 왼쪽0도~오른쪽180도 허리가 수직일때 90도
                iGetShoulderAngle = (int)_iGetShoulderAngle.Update(GetShoulderAngle()); //왼쪽기준 오른쪽 어깨 각도 아래쪽 최대 0도~ 위쪽 최대 180도. 어깨 일직선 90도
                iGetWeight = (int)_iGetWeight.Update(GetWeight());    //골반의 치우침 정도 약 -30 ~ 30도
                iGetFootDisRate = (int)_iGetFootDisRate.Update(GetFootDisRate());   //어깨 넓이 대비 다리 간격 백분율
                iGetForearmAngle = (int)_iGetForearmAngle.Update(GetForearmAngle());  //오른 어깨기준 오른팔꿈치까지 각도
                iGetElbowFrontDir = (int)_iGetElbowFrontDir.Update(GetElbowFrontDir());  //왼 팔의 팔꿈치 접힘 각도
                iGetElbowRightFrontDir = (int)_iGetElbowRightFrontDir.Update(GetElbowRightFrontDir());  //오른팔의 팔꿈치 접힘 각도(정면)
                iGetHandDirDistance = (int)_iGetHandDirDistance.Update(GetHandDirDistance());   //어깨중심에서 그립핸드까지 거리
                iGetShoulderFrontDirWorld = (int)_iGetShoulderFrontDirWorld.Update(GetShoulderDirWorld());  //월드 좌표로 변환한 어께 회전 값
                iGetPelvisFrontDirWorld = (int)_iGetPelvisFrontDirWorld.Update(GetPelvisFrontDirWorld());
                iGetPelvisAngle = (int)_iGetPelvisAngle.Update(GetPelvisAngle());
                iGetNoseDir = (int)_iGetNoseDir.Update(GetNoseDir());
                iGetKneeDisRate = (int)_iGetKneeDisRate.Update(GetKneeDisRate());   //어깨 넓이 대비 무릎 간격 백분율
            }

            /*  Side  */
            if (clientSide.Track)
            {
                iGetGripDistance = (int)_iGetGripDistance.Update( GetHandGripDistance());
                iGetWaistSideDir = (int)_iGetWaistSideDir.Update(GetWaistSideDir());  //허리 숙임 각도 바로 섰을때 90도
                iGetHandSideDir = (int)_iGetHandSideDir.Update(GetHandSideDir());   //오른 어깨기준 오른팔꿈치 각도
                iGetKneeSideDir = (int)_iGetKneeSideDir.Update(GetKneeSideDir());   //오른쪽 무릎 접힘 각도
                iGetElbowSideDir = (int)_iGetElbowSideDir.Update(GetElbowSideDir());  //오른쪽 팔의 팔꿈치 접힘 각도(측면)
                iGetArmpitDir = (int)_iGetArmpitDir.Update(GetArmpitDir()); //오른쪽 허리각도를 기준으로 팔꿈치 각도
                iGetSideWeight =(int)_iGetSideWeight.Update(GetSideWeight());    //골반/상체 중심점으로 앞/뒤치우침 정도 약 -2 ~ 8도
                iGetHandSideDistance = (int)_iGetHandSideDistance.Update(GetHandSideDistance()); //어깨에서 손까지 거리(Address 기준을 위해)
                iGetShoulderSideDirWorld = (int)_iGetShoulderSideDirWorld.Update(GetShoulderSideDirWorld()); //월드 좌표로 변환한 어께 회전 값
                iGetPelvisSideDirWorld = (int)_iGetPelvisSideDirWorld.Update(GetPelvisSideDirWorld());
                iGetNoseShoulderSideDir = (int)_iGetNoseShoulderSideDir.Update(GetNoseShoulderSideDir());
                iGetNosePelvisSideDir = (int)_iGetNosePelvisSideDir.Update(GetNosePelvisSideDir());
            }

            /*  Combine  */
            if (clientFront.Track && clientSide.Track)
            {
                iGetShoulderDir = (int)_iGetShoulderDir.Update(GetShoulderDir());   //어깨 회전각도 (정/측면 데이터 및 거리에 따른 보정 치)
                iGetPelvisDir = (int)_iGetPelvisDir.Update(GetPelvisDir()); //골반 회전각도 (정/측면 데이터 및 거리에 따른 보정 치)

                //GetRightElbowDir();
                iGetHandCombineDir = (int)_iGetHandCombineDir.Update(GetHandCombineDir());

                /*  Option */
                try
                {
                    fLeftElbowSideVis = clientSide.Landmark[13].visibility;// poseData2["landmark_13"].visibility;
                    fRightElbowFrontVis = clientFront.Landmark[14].visibility;// .poseData1["landmark_14"].visibility;

                    CheckNormal();

                }
                catch { fLeftElbowSideVis = 0; }
            }

            yield return null;
        }
    }

    public void UpdateSensor(in Landmark2D[] FrontLandmark, in Landmark2D[] SideLandmark,
                            in Landmark3D[] FrontWorldLandmark, in Landmark3D[] SideWorldLandmark)
    {
        /*  Front  */
        if (clientFront.Track)
        {
            iGetHandDistance = (int)GetHandDistance(FrontLandmark);  //양 손목 사이 거리 (카메라와 거리에 따라 상대적)
            iGetShoulderDistance = (int)GetShoulderDistance(FrontLandmark);  //양 어깨 사이 거리 (카메라와 거리에 따라 상대적)

            GetHandPosition(FrontLandmark, SideLandmark);  //스윙각도 계산을 위한 기준 손 좌표
            iGetHandDir = (int)GetHandDir(FrontLandmark);   //스윙각도 0~360도, 백스윙쪽이 각도가 줄어들고 팔로우쪽이 증가한다. 어드레서 약 180도
            iGetSpineDir = (int)GetSpineDir(FrontLandmark);  //허리 정면 각도. 왼쪽0도~오른쪽180도 허리가 수직일때 90도
            iGetShoulderAngle = (int)GetShoulderAngle(FrontLandmark); //왼쪽기준 오른쪽 어깨 각도 아래쪽 최대 0도~ 위쪽 최대 180도. 어깨 일직선 90도
            iGetWeight = (int)GetWeight(FrontLandmark);    //골반의 치우침 정도 약 -30 ~ 30도
            iGetFootDisRate = (int)GetFootDisRate(FrontLandmark);   //어깨 넓이 대비 다리 간격 백분율
            iGetForearmAngle = (int)GetForearmAngle(FrontLandmark);  //오른 어깨기준 오른팔꿈치까지 각도
            iGetElbowFrontDir = (int)GetElbowFrontDir(FrontLandmark);  //왼 팔의 팔꿈치 접힘 각도
            iGetElbowRightFrontDir = (int)GetElbowRightFrontDir(FrontLandmark);  //오른팔의 팔꿈치 접힘 각도(정면)
            iGetHandDirDistance = (int)GetHandDirDistance(FrontLandmark);   //어깨중심에서 그립핸드까지 거리
            iGetShoulderFrontDirWorld = (int)GetShoulderDirWorld(FrontWorldLandmark);  //월드 좌표로 변환한 어께 회전 값
            iGetPelvisFrontDirWorld = (int)GetPelvisFrontDirWorld(FrontWorldLandmark);
            iGetPelvisAngle = (int)GetPelvisAngle(FrontLandmark);
            iGetNoseDir = (int)GetNoseDir(FrontLandmark);
            iGetKneeDisRate = (int)GetKneeDisRate();   //어깨 넓이 대비 무릎 간격 백분율
        }

        /*  Side  */
        if (clientSide.Track)
        {
            iGetGripDistance = (int)GetHandGripDistance(SideLandmark);
            iGetWaistSideDir = (int)GetWaistSideDir(SideLandmark);  //허리 숙임 각도 바로 섰을때 90도
            iGetHandSideDir = (int)GetHandSideDir(SideLandmark);   //오른 어깨기준 오른팔꿈치 각도
            iGetKneeSideDir = (int)GetKneeSideDir(SideLandmark);   //오른쪽 무릎 접힘 각도
            iGetElbowSideDir = (int)GetElbowSideDir(SideLandmark);  //오른쪽 팔의 팔꿈치 접힘 각도(측면)
            iGetArmpitDir = (int)GetArmpitDir(SideLandmark); //오른쪽 허리각도를 기준으로 팔꿈치 각도
            iGetSideWeight =(int)GetSideWeight(SideLandmark);    //골반/상체 중심점으로 앞/뒤치우침 정도 약 -2 ~ 8도
            iGetHandSideDistance = (int)GetHandSideDistance(SideLandmark); //어깨에서 손까지 거리(Address 기준을 위해)
            iGetShoulderSideDirWorld = (int)GetShoulderSideDirWorld(SideWorldLandmark); //월드 좌표로 변환한 어께 회전 값
            iGetPelvisSideDirWorld = (int)GetPelvisSideDirWorld(SideWorldLandmark);
            iGetNoseShoulderSideDir = (int)GetNoseShoulderSideDir(SideLandmark);
            iGetNosePelvisSideDir = (int)GetNosePelvisSideDir(SideLandmark);
        }

        /*  Combine  */
        if (clientFront.Track && clientSide.Track)
        {
            iGetShoulderDir = (int)GetShoulderDir();   //어깨 회전각도 (정/측면 데이터 및 거리에 따른 보정 치)
            iGetPelvisDir = (int)GetPelvisDir(); //골반 회전각도 (정/측면 데이터 및 거리에 따른 보정 치)

            iGetHandCombineDir = (int)GetHandCombineDir();

            /*  Option */
            try
            {
                fLeftElbowSideVis = SideLandmark[13].visibility;// poseData2["landmark_13"].visibility;
                fRightElbowFrontVis = FrontLandmark[14].visibility;// .poseData1["landmark_14"].visibility;
            }
            catch { fLeftElbowSideVis = 0; }
        }
    }



    //=========================================================
    // 정면 poseData1 만 사용
    //=========================================================

    //양 손목 사이 거리 (카메라와 거리에 따라 상대적) - positionOrg로 원본 값 비교
    float GetHandDistance()
    {
        return GetHandDistance(in clientFront.Landmark);
    }
    float GetHandDistance(in Landmark2D[] Landmark)
    {
        try
        {
            //iGetHandDistance = (int)_iGetHandDistance.Update(Vector2.Distance(Landmark[15].positionOrg, Landmark[16].positionOrg) * 100f);

            //return iGetHandDistance = (int)(iGetHandDistance * Utillity.Instance.frontPixelDistanceRate);
            return ((Vector2.Distance(Landmark[15].positionOrg, Landmark[16].positionOrg) * 100f) * Utillity.Instance.frontPixelDistanceRate);
        }
        catch { return -1; } //iGetHandDistance = -1; }
    }

    //양 어깨 사이 거리 (카메라와 거리에 따라 상대적)
    float GetShoulderDistance()
    {
        return GetShoulderDistance(in clientFront.Landmark); 
    }
    float GetShoulderDistance(in Landmark2D[] Landmark)
    {
        try
        {
            //iGetShoulderDistance = (int)(_iGetShoulderDistance.Update(Vector2.Distance(Landmark[11].positionOrg,
            //    Landmark[12].positionOrg) * 100f));

            //iGetShoulderDistance = (int)(iGetShoulderDistance * Utillity.Instance.frontPixelDistanceRate);
            return ((Vector2.Distance(Landmark[11].positionOrg, Landmark[12].positionOrg) * 100f) * Utillity.Instance.frontPixelDistanceRate);
        }
        catch { return -1; }//iGetShoulderDistance = -1; }
    }

    //float offset = 0.1f;
    //스윙각도 계산을 위한 기준 손 좌표
    void GetHandPosition()
    {
        GetHandPosition(in clientFront.Landmark, in clientSide.Landmark);
    }
    void GetHandPosition(in Landmark2D[] FrontLandmark, in Landmark2D[] SideLandmark)
    {
        try
        {
            //측면 카메라 기준으로 앞쪽에 있는 팔꿈치가 있는 방향의 손을 선택
            if (clientSide.Track)
            {
                if (SideLandmark[14].position.y > SideLandmark[13].position.y)
                {
                    handVectorIsLeft = false;
                    handVectorFront = FrontLandmark[20].position;
                    handVectorSide = SideLandmark[20].position;
                }
                else
                {
                    handVectorIsLeft = true;
                    handVectorFront = FrontLandmark[19].position;
                    handVectorSide = SideLandmark[19].position;
                }
            }
            else
            {
                handVectorFront = Vector2.Lerp(FrontLandmark[21].position, FrontLandmark[20].position
                    , FrontLandmark[20].visibility);
            }
        }
        catch { handVectorFront = Vector2.zero; }
    }

    //스윙각도 0~360도, 백스윙쪽이 각도가 줄어들고 팔로우쪽이 증가한다. 어드레서 약 180도
    // * 반드시 GetHandPosition()를 먼저 호출해야 한다.
    float GetHandDir()
    {
        return GetHandDir(in clientFront.Landmark);
    }
    float GetHandDir(in Landmark2D[] Landmark)
    {
        try
        {
            // 어꺠중심과 손중심을 기준
            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            Vector2 dir = handVectorFront - shoulderVector;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += 180f;
            iGetHandDirNF = (int)angle;
            
            //발 기준 (어드레스 체크용 추가)
            Vector2 footVector = (Landmark[27].position + Landmark[28].position) / 2;
            Vector2 dirBtm = handVectorFront - footVector;

            float angleBtm = Mathf.Atan2(dirBtm.y, -dirBtm.x) * Mathf.Rad2Deg;
            angleBtm += 180f;
            iGetHandDirFromBottom = (int)_iGetHandDirFromBottom.Update(angleBtm);

            return iGetHandDirNF;
        }
        catch { return -1; }//iGetHandDir = -1; }
    }
    

    //허리 정면 각도. 왼쪽0도~오른쪽180도 허리가 수직일때 90도
    float GetSpineDir()
    {
        return GetSpineDir(in clientFront.Landmark);
    }
    float GetSpineDir(in Landmark2D[] Landmark)
    {
        try
        {
            // 어꺠중심과 골반중심
            Vector2 pelvisVector = (Landmark[23].position + Landmark[24].position) / 2;
            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            Vector2 dir = shoulderVector - pelvisVector;

            float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            //iGetSpineDir = (int)_iGetSpineDir.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetSpineDir = -1; }
    }

    //왼쪽기준 오른쪽 어깨 각도 아래쪽 최대 0도~ 위쪽 최대 180도. 어깨 일직선 90도
    float GetShoulderAngle()
    {
        return GetShoulderAngle(in clientFront.Landmark);
    }
    float GetShoulderAngle(in Landmark2D[] Landmark)
    {
        try
        {
            // 왼쪽 어꺠에서 오른쪽어께까지 각도
            Vector2 dir = Landmark[12].position - Landmark[11].position;

            float angle = -Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
            angle += 180f;
            //iGetShoulderAngle = (int)_iGetShoulderAngle.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetShoulderAngle = -1; }
    }

    //골만의 치우침 정도 약 -30 ~ 30도
    float GetWeight()
    {
        return GetWeight(in clientFront.Landmark);
    }
    float GetWeight(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 footCenter = (Landmark[27].position + Landmark[28].position) / 2;
            Vector2 pelvisCenter = (Landmark[23].position + Landmark[24].position) / 2;

            Vector2 dir = footCenter - pelvisCenter;

            dir.Normalize();

            //fGetPelvisDir = _iGetWeight.Update(dir.y);
            //iGetWeight = (int)(fGetPelvisDir * 100f);
            return (dir.y * 100f);

        }
        catch { return -1; }//iGetWeight = -1; }
    }


    //어깨 넓이 대비 다리 간격 백분율
    float GetFootDisRate()
    {
        return GetFootDisRate(in clientFront.Landmark);
    }
    float GetFootDisRate(in Landmark2D[] Landmark)
    {
        try
        {
            float footDis = Vector2.Distance(Landmark[27].positionOrg, Landmark[28].positionOrg) * 100f;
            float shoulderDis = Vector2.Distance(Landmark[11].positionOrg, Landmark[12].positionOrg) * 100f;
            footDis = footDis * Utillity.Instance.frontPixelDistanceRate;
            shoulderDis = shoulderDis * Utillity.Instance.frontPixelDistanceRate;
            float result = 1f;
            if (footDis != 0 && shoulderDis != 0)
                result = (footDis / shoulderDis) * 100f;
            //iGetFootDisRate = (int)_iGetFootDisRate.Update(result);
            return result;
        }
        catch { return -1; }//iGetFootDisRate = -1; }
    }

    //오른 어깨기준 오른팔꿈치까지 각도
    float GetForearmAngle()
    {
        return GetForearmAngle(in clientFront.Landmark);
    }
    float GetForearmAngle(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 dir = Landmark[14].position - Landmark[12].position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += 180f;
            //iGetForearmAngle = (int)_iGetForearmAngle.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetForearmAngle = -1; }
    }

    //왼쪽 팔의 팔꿈치 접힘 각도
    float GetElbowFrontDir()
    {
        return GetElbowFrontDir(in clientFront.Landmark);
    }
    float GetElbowFrontDir(in Landmark2D[] Landmark)
    {
        try
        {
            float angle = CalculateVectorAngle(Landmark[11].position, Landmark[13].position, Landmark[15].position);
            //iGetElbowFrontDir = (int)_iGetElbowFrontDir.Update(angle);
            return angle;
        }
        catch {return -1; }// iGetElbowFrontDir = -1; }
    }

    //오른팔의 팔꿈치 접힘 각도
    float GetElbowRightFrontDir()
    {
        return GetElbowRightFrontDir(in clientFront.Landmark);
    }
    float GetElbowRightFrontDir(in Landmark2D[] Landmark)
    {
        try
        {
            float angle = CalculateVectorAngle(Landmark[12].position, Landmark[14].position, Landmark[16].position);
            //iGetElbowRightFrontDir = (int)_iGetElbowRightFrontDir.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetElbowRightFrontDir = -1; }
    }

    //어깨 중심에서 그립까지 거리
    float GetHandDirDistance()
    {
        return GetHandDirDistance(in clientFront.Landmark);
    }
    float GetHandDirDistance(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            float dis = Vector2.Distance(shoulderVector, handVectorFront);
            //iGetHandDirDistance = (int)_iGetHandDirDistance.Update(dis);
            return dis;
        }
        catch { return -1; }//iGetHandDirDistance = -1; }
    }


    float GetShoulderDirWorld()
    {
        return GetShoulderDirWorld(in clientFront.WorldLandmark);
    }
    float GetShoulderDirWorld(in Landmark3D[] WorldLandmark)
    {
        try
        {
            //Vector3 shoulderCenter = (clientFront.WorldLandmark[11].position + clientFront.WorldLandmark[12].position) / 2f;
            Vector3 shoulderDir = (WorldLandmark[12].position - WorldLandmark[11].position).normalized;

            Vector3 ShoulderForward = Vector3.Cross(Vector3.up, shoulderDir).normalized;

            float yawAngle = Vector3.SignedAngle(Vector3.forward, ShoulderForward, Vector3.up) + 180f;

            iGetShoulderFrontDirWorldNF = (int)yawAngle;
            //iGetShoulderFrontDirWorld = (int)_iGetShoulderFrontDirWorld.Update(yawAngle);
            return iGetShoulderFrontDirWorldNF;
        }
        catch { return -1; }//iGetShoulderFrontDirWorld = -1; }
    }

    float GetPelvisFrontDirWorld()
    {
        return GetPelvisFrontDirWorld(in clientFront.WorldLandmark);
    }
    float GetPelvisFrontDirWorld(in Landmark3D[] WorldLandmark)
    {
        try
        {
            //Vector3 pelvisCenter = (clientFront.WorldLandmark[23].position + clientFront.WorldLandmark[24].position) / 2f;
            Vector3 pelvisDir = (WorldLandmark[24].position - WorldLandmark[23].position).normalized;

            Vector3 PelvisForward = Vector3.Cross(Vector3.up, pelvisDir).normalized;

            float yawAngle = Vector3.SignedAngle(Vector3.forward, PelvisForward, Vector3.up) + 180f;

            iGetPelvisFrontDirWorldNF = (int)yawAngle;
            //iGetPelvisFrontDirWorld = (int)_iGetPelvisFrontDirWorld.Update(yawAngle);
            return iGetPelvisFrontDirWorldNF;
        }
        catch { return -1; }//iGetPelvisFrontDirWorld = -1; }
    }

    //왼쪽기준 오른쪽 골반 각도 아래쪽 최대 0도~ 위쪽 최대 180도. 어깨 일직선 90도
    float GetPelvisAngle()
    {
        return GetPelvisAngle(in clientFront.Landmark);
    }
    float GetPelvisAngle(in Landmark2D[] Landmark)
    {
        try
        {
            // 왼쪽 어꺠에서 오른쪽어께까지 각도
            Vector2 dir = Landmark[24].position - Landmark[23].position;

            float angle = -Mathf.Atan2(dir.x, -dir.y) * Mathf.Rad2Deg;
            angle += 180f;
            //iGetPelvisAngle = (int)_iGetPelvisAngle.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetPelvisAngle = -1; }
    }

    float GetNoseDir()
    {
        return GetNoseDir(in clientFront.Landmark);
    }
    float GetNoseDir(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            Vector2 angle = Landmark[0].position - shoulderVector;
            return -Mathf.Atan2(angle.x, angle.y) * Mathf.Rad2Deg;        
        }
        catch { return -1; }
    }
    
    //어깨 넓이 대비 무릎 간격 백분율
    float GetKneeDisRate()
    {
        return GetKneeDisRate(in clientFront.Landmark);
    }
    float GetKneeDisRate(in Landmark2D[] Landmark)
    {
        try
        {
            float kneeDis = Vector2.Distance(Landmark[25].positionOrg, Landmark[26].positionOrg) * 100f;
            float shoulderDis = Vector2.Distance(Landmark[11].positionOrg, Landmark[12].positionOrg) * 100f;
            kneeDis = kneeDis * Utillity.Instance.frontPixelDistanceRate;
            shoulderDis = shoulderDis * Utillity.Instance.frontPixelDistanceRate;
            float result = 1f;
            if (kneeDis != 0 && shoulderDis != 0)
                result = (kneeDis / shoulderDis) * 100f;
            return result;
        }
        catch { return -1; }
    }


    






    //=========================================================
    // 측면 poseData2 만 사용
    //========================================================
    //양 손목 사이 거리 (카메라와 거리에 따라 상대적) - positionOrg로 원본 값 비교
    float GetHandGripDistance()
    {
        return GetHandGripDistance(in clientSide.Landmark);
    }
    float GetHandGripDistance(in Landmark2D[] Landmark)
    {
        try
        {
            //iGetGripDistance = (int)_iGetGripDistance.Update(Vector2.Distance(Landmark[15].positionOrg,
            //Landmark[16].positionOrg) * 100f);

            //iGetGripDistance = (int)(iGetHandDistance * Utillity.Instance.sidePixelDistanceRate);
            if (Landmark[15].visibility < 0.5f)
                return -1;
            else
                return ((Vector2.Distance(Landmark[15].positionOrg, Landmark[16].positionOrg) * 100f) * Utillity.Instance.sidePixelDistanceRate);
        }
        catch { return -1; }//iGetHandDistance = -1; }
    }

    //허리 숙임 각도 바로 섰을때 90도
    float GetWaistSideDir()
    {
        return GetWaistSideDir(in clientSide.Landmark);
    }
    float GetWaistSideDir(in Landmark2D[] Landmark)
    {
        try
        {
            //우측 골반에서 우측 어꺠로 기울기 감지
            Vector2 dir = Landmark[12].position - Landmark[24].position;

            float angle = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;

            //iGetWaistSideDir = (int)_iGetWaistSideDir.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetWaistSideDir = -1; }
    }

    //오른 어깨기준 오른팔꿈치 각도
    float GetHandSideDir()
    {
        return GetHandSideDir(in clientSide.Landmark);
    }
    float GetHandSideDir(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            Vector2 dir = handVectorSide - shoulderVector;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle += 180f;
            iGetHandSideDirNF = (int)angle;
            //iGetHandSideDir = (int)_iGetHandSideDir.Update(angle);
            return iGetHandSideDirNF;
        }
        catch { return -1; }//iGetHandSideDir = -1; }
    }

    float GetHandSideDistance()
    {
        return GetHandSideDistance(in clientSide.Landmark);
    }
    float GetHandSideDistance(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 shoulderVector = (Landmark[11].position + Landmark[12].position) / 2;
            float dis = Vector2.Distance(handVectorSide, shoulderVector);
            //iGetHandSideDistance = (int)_iGetHandSideDistance.Update(dis);
            return dis;
        }
        catch { return -1; }//iGetHandSideDistance = -1; }
    }

    //오른쪽 무플 접힘 각도
    float GetKneeSideDir()
    {
        return GetKneeSideDir(in clientSide.Landmark);
    }
    float GetKneeSideDir(in Landmark2D[] Landmark)
    {
        try
        {
            float angle = CalculateVectorAngle(Landmark[24].position, Landmark[26].position, Landmark[28].position);
            //iGetKneeSideDir = (int)_iGetKneeSideDir.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetKneeSideDir = -1; }
    }

    //오른쪽 팔의 팔꿈치 접힘 각도
    float GetElbowSideDir()
    {
        return GetElbowSideDir(in clientSide.Landmark);
    }
    float GetElbowSideDir(in Landmark2D[] Landmark)
    {
        try
        {
            float angle = CalculateVectorAngle(Landmark[12].position, Landmark[14].position, Landmark[16].position);
            //iGetElbowSideDir = (int)_iGetElbowSideDir.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetElbowSideDir = -1; }
    }

    //오른쪽 허리각도를 기준으로 팔꿈치 각도
    float GetArmpitDir()
    {
        return GetArmpitDir(in clientSide.Landmark);
    }
    float GetArmpitDir(in Landmark2D[] Landmark)
    {
        try
        {
            float angle = CalculateVectorAngle(Landmark[24].position, Landmark[12].position, Landmark[14].position);
            //iGetArmpitDir = (int)_iGetArmpitDir.Update(angle);
            return angle;
        }
        catch { return -1; }//iGetArmpitDir = -1; }
    }

    //골반/상체 중심점으로 앞/뒤치우침 정도 약 -20 ~ 130
    float GetSideWeight()
    {
        return GetSideWeight(in clientSide.Landmark);
    }
    float GetSideWeight(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 footCenter = Vector2.zero;
            Vector2 shoulderCenter = Vector2.zero;
            Vector2 pelvisCenter = Vector2.zero;

            if (Landmark[27].visibility > 0.5f)
                footCenter = (Landmark[27].position + Landmark[28].position) / 2;
            else
                footCenter = Landmark[28].position;

            if (Landmark[11].visibility > 0.5f)
                shoulderCenter = (Landmark[12].position + Landmark[11].position) / 2;
            else
                shoulderCenter = Landmark[12].position;

            if (Landmark[23].visibility > 0.5f)
                pelvisCenter = (Landmark[23].position + Landmark[24].position) / 2;
            else
                pelvisCenter = Landmark[24].position;


            Vector2 dir = footCenter - ((pelvisCenter + shoulderCenter) / 2);

            dir.Normalize();

            //fGetSPCSideDir = _iGetSideWeight.Update(dir.y);
            //iGetSideWeight = (int)(fGetSPCSideDir * -1000f);

            return (dir.y * -1000f);
        }
        catch { return 0; }//iGetSideWeight = 0; }
    }

    float GetShoulderSideDirWorld()
    {
        return GetShoulderSideDirWorld(in clientSide.WorldLandmark);
    }
    float GetShoulderSideDirWorld(in Landmark3D[] WorldLandmark)
    {
        try
        {
            //Vector3 shoulderCenter = (clientSide.WorldLandmark[11].position + clientSide.WorldLandmark[12].position) / 2f;
            Vector3 shoulderDir = (WorldLandmark[12].position - WorldLandmark[11].position).normalized;

            Vector3 ShoulderForward = Vector3.Cross(Vector3.up, shoulderDir).normalized;

            float yawAngle = Vector3.SignedAngle(Vector3.forward, ShoulderForward, Vector3.up);// + 180f;
            if (90 < yawAngle && yawAngle < 180)
                yawAngle -= 90f;
            else
                yawAngle += 270f;
            iGetShoulderSideDirWorldNF = (int)yawAngle;
            //iGetShoulderSideDirWorld = (int)_iGetShoulderSideDirWorld.Update(yawAngle);
            return iGetShoulderSideDirWorldNF;
        }
        catch { return -1; }//iGetShoulderSideDirWorld = -1; }
    }

    float GetPelvisSideDirWorld()
    {
        return GetPelvisSideDirWorld(in clientSide.WorldLandmark);
    }
    float GetPelvisSideDirWorld(in Landmark3D[] WorldLandmark)
    {
        try
        {
            //Vector3 pelvisCenter = (clientSide.WorldLandmark[23].position + clientSide.WorldLandmark[24].position) / 2f;
            Vector3 pelvisDir = (WorldLandmark[24].position - WorldLandmark[23].position).normalized;

            Vector3 PelvisForward = Vector3.Cross(Vector3.up, pelvisDir).normalized;

            float yawAngle = Vector3.SignedAngle(Vector3.forward, PelvisForward, Vector3.up);// + 180f;
            if (90 < yawAngle && yawAngle < 180)
                yawAngle -= 90f;
            else
                yawAngle += 270f;
            iGetPelvisSideDirWorldNF = (int)yawAngle;
            //iGetPelvisSideDirWorld = (int)_iGetPelvisSideDirWorld.Update(yawAngle);
            return iGetPelvisSideDirWorldNF;
        }
        catch { return -1; }//iGetPelvisSideDirWorld = -1; }
    }

    float GetNoseShoulderSideDir()
    {
        return GetNoseShoulderSideDir(in clientSide.Landmark);
    }
    float GetNoseShoulderSideDir(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 angle = Landmark[0].position - Landmark[12].position;
            return -Mathf.Atan2(angle.x, angle.y) * Mathf.Rad2Deg;
        }
        catch { return -1; }
    }
    
    float GetNosePelvisSideDir()
    {
        return GetNosePelvisSideDir(in clientSide.Landmark);
    }
    float GetNosePelvisSideDir(in Landmark2D[] Landmark)
    {
        try
        {
            Vector2 angle = Landmark[0].position - Landmark[24].position;
            return -Mathf.Atan2(angle.x, angle.y) * Mathf.Rad2Deg;
        }
        catch { return -1; }
    }







    //=========================================================
    // 정면, 측면 poseData1, poseData2 복합 사용
    //=========================================================



    //어깨 회전각도 (정/측면 데이터 및 거리에 따른 보정 치)
    float GetShoulderDir()
    {
        try
        {
            if (iGetShoulderSideDirWorldNF > 180)
                return iGetShoulderSideDirWorldNF; //GetShoulderDir = (int)_iGetShoulderDir.Update(iGetShoulderSideDirWorldNF);
            else
                return iGetShoulderFrontDirWorldNF; //iGetShoulderDir = (int)_iGetShoulderDir.Update(iGetShoulderFrontDirWorldNF);
        }
        catch { return -1; }//iGetShoulderDir = -1; }
    }    
    
    //골반 회전각도 (정/측면 데이터 및 거리에 따른 보정 치)
    float GetPelvisDir()
    {
        try
        {
            if (135 < iGetPelvisFrontDirWorldNF && iGetPelvisFrontDirWorldNF < 225)
                return iGetPelvisFrontDirWorldNF;// iGetPelvisDir = (int)_iGetPelvisDir.Update(iGetPelvisFrontDirWorldNF);
            else
                return iGetPelvisSideDirWorldNF;// iGetPelvisDir = (int)_iGetPelvisDir.Update(iGetPelvisSideDirWorldNF);
        }
        catch { return -1; }//iGetPelvisDir = -1; }
    }
    /*
    void GetRightElbowDir()
    {
        try
        {
            Vector2 dirF = clientFront.Landmark[14].position - clientFront.Landmark[12].position;
            Vector2 dirS = clientSide.Landmark[14].position - clientSide.Landmark[12].position;
            dirF.Normalize();
            dirS.Normalize();

            vRightElbowDir = new Vector3(
                _vRightElbowDir[0].Update(dirF.x+0.05f),
                _vRightElbowDir[1].Update(-dirS.y),
                _vRightElbowDir[2].Update(-dirS.x));
        }
        catch { vRightElbowDir = Vector3.zero; }
    }*/

    int tempVal = 180;
    bool isFront = true;
    float GetHandCombineDir()
    {
        try
        {
            if (isFront)
            {
                tempVal = iGetHandDirNF;
                if (tempVal < 80)
                    isFront = false;
            }
            else
            {
                tempVal = iGetHandSideDirNF;
                if (tempVal > 80)
                    isFront = true;
            }
            //iGetHandCombineDir = (int)_iGetHandCombineDir.Update(tempVal);
            return tempVal;

        }
        catch { return -1; } //iGetHandCombineDir = -1; }
    }

    public bool IsAddressHand(bool visCheck = true)
    {
        if (_isGrip == false)
        {
            if (iGetHandDistance <= Utillity.Instance.addresssHandDis ||
            (iGetGripDistance > 0 && iGetGripDistance <= Utillity.Instance.addresssHandDis))
                _isGrip = true;
        }
        else
        {
            if (iGetHandDistance > (Utillity.Instance.addresssHandDis * 2) ||
            (iGetGripDistance > 0 && iGetGripDistance > (Utillity.Instance.addresssHandDis * 2)))
                _isGrip = false;
        }

        if(visCheck)
        {
            if (!IsVisibility())
                _isGrip = false;
        }

        return _isGrip;
    }

    public bool IsDrillPose(bool isDrilling = false)
    {
        //Debug.Log($"IsDrillPose() {clientFront.Landmark[24].x} < {clientFront.Landmark[16].x} && {clientFront.Landmark[23].x} < {clientFront.Landmark[15].x}");

        float limitX = (clientFront.KalmanPositions.CenterShoulder.x + clientFront.KalmanPositions.CenterPelvis.x) / 2f;
        if(limitX > clientFront.Landmark[16].x && limitX > clientFront.Landmark[15].x)
        {
            return true;
        }
        else
        {
            if(isDrilling)
            {
                if(limitX > clientFront.Landmark[16].x || limitX > clientFront.Landmark[15].x)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
    }

    float visFailTimer = 0;
    public bool IsVisibility()
    {
        //bool elbowOk = (fLeftElbowSideVis >= 0.5f) && (fRightElbowFrontVis >= 0.5f);
        bool avgOk = (visibilityFront >= 0.5f) && (visibilitySide >= 0.5f);

        if (/*!elbowOk ||*/ !avgOk)
        {
            visFailTimer += Time.deltaTime;

            if (visFailTimer >= 0.25f)
            {
                return false;
            }
        }

        return /*elbowOk &&*/ avgOk;
    }


    //=========================================================
    // Util 함수
    //=========================================================
    float CalculateVectorAngle(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        try
        {
            Vector2 vec1 = v1 - v2;
            Vector3 vec2 = v3 - v2;

            return Vector2.Angle(vec1.normalized, vec2.normalized);
        }
        catch { return -1f; }
    }

    void CheckNormal()
    {
        bNormal = true;

        if(clientFront.visibilityAvg < 0.25f || clientSide.visibilityAvg < 0.25f)
            bNormal = false;
        else if (mirroViewType == EMirroViewType.FRONTMAIN)
        {
            if(clientFront.KalmanPositions.CenterShoulder.y > 200 || clientFront.KalmanPositions.CenterShoulder.y < -200 
                || clientFront.KalmanPositions.CenterShoulder.x > 10 || clientFront.KalmanPositions.CenterShoulder.x < -550)
                bNormal = false;
            if(clientFront.KalmanPositions.CenterPelvis.y > 200 || clientFront.KalmanPositions.CenterPelvis.y < -200 
                || clientFront.KalmanPositions.CenterPelvis.x < -100 || clientFront.KalmanPositions.CenterPelvis.x > 180)
                bNormal = false;
            
            if(clientSide.KalmanPositions.CenterShoulder.y > 50 || clientSide.KalmanPositions.CenterShoulder.y < -55
                || clientSide.KalmanPositions.CenterShoulder.x > 2.5f || clientSide.KalmanPositions.CenterShoulder.x < -137.5f)
                bNormal = false;
            if(clientSide.KalmanPositions.CenterPelvis.y < -82.5f || clientSide.KalmanPositions.CenterPelvis.y > 50
                || clientSide.KalmanPositions.CenterPelvis.x > 37.5f || clientSide.KalmanPositions.CenterPelvis.x < -62.5f)
                bNormal = false;
        }
        else if (mirroViewType == EMirroViewType.SIDEMAIN)
        {
            
            if(clientFront.KalmanPositions.CenterShoulder.y > 50 || clientFront.KalmanPositions.CenterShoulder.y < -50
                || clientFront.KalmanPositions.CenterShoulder.x > 2.5f || clientFront.KalmanPositions.CenterShoulder.x < -137.5f)
                bNormal = false;
            if(clientFront.KalmanPositions.CenterPelvis.y > 50 || clientFront.KalmanPositions.CenterPelvis.y < -50
                || clientFront.KalmanPositions.CenterPelvis.x < -25 || clientFront.KalmanPositions.CenterPelvis.x > 45)
                bNormal = false;

            if(clientSide.KalmanPositions.CenterShoulder.y > 200 || clientSide.KalmanPositions.CenterShoulder.y < -220 
                || clientSide.KalmanPositions.CenterShoulder.x > 10 || clientSide.KalmanPositions.CenterShoulder.x < -550)
                bNormal = false;
            if(clientSide.KalmanPositions.CenterPelvis.y < -330 || clientSide.KalmanPositions.CenterPelvis.y > 200 
                || clientSide.KalmanPositions.CenterPelvis.x > 150 || clientSide.KalmanPositions.CenterPelvis.x < -250)
                bNormal = false;
                
        }
    }

    public void SetAdressCenterShoulder(bool reset = false)
    {
        if(reset)
        {
            CheckAdressCenterShoulder = Vector2.zero;
            return;
        }

        CheckAdressCenterShoulder = (clientFront.Landmark[11].position + clientFront.Landmark[12].position) / 2;
    }

    public Vector2 GetLandmarkPosition(bool isFront, int markNum)
    {
        try
        {
            if (isFront)
                return clientFront.Landmark[markNum].position;
            else
                return clientSide.Landmark[markNum].position;
        }
        catch
        {
            return Vector2.zero;
        }
    }

    
}
