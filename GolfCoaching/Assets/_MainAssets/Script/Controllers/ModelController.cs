using DG.Tweening;
using RootMotion.FinalIK;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Enums;
using Sequence = DG.Tweening.Sequence;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using static StudioManager;

public class ModelController : MonoBehaviour
{
    //[SerializeField] mocapFront mocapFront;
    //[SerializeField] mocapSide mocapSide;
    [SerializeField] SensorProcess sensorProcess;
    [SerializeField] PracticeModeController practiceModeController;
    [SerializeField] Animator ani3DModel_Front;
    [SerializeField] Animator ani3DModel_Foot;
    [SerializeField] GhostController ghostController_Front;
    [SerializeField] CalKinetic calKinetic_Front;
    [SerializeField] BipedIK bipedIK;
    //[SerializeField] LimbIK limbIK;
    [SerializeField] IKTargetController ikTargetController;
    //[SerializeField] CalKinetic calKinetic_Side;
    // HalfSwing
    //0 어드레스
    //0.42 백스윙엔드 0~0.42 180 / 0.42 = 404
    //0.57 임팩트 0.42~0.57(0.15)
    //0.99 피니쉬 0.42~0.99(0.57) 360 / 0.57 = 631


    // Full_Mirror
    // 0 ADRESS
    // 0.27 TAKEBACK 0 ~ 0.27
    // 0.4 BACK 0.27 ~ 0.4
    // 0.5 TOP 0.4 ~ 0.5
    // 0.63 DOWN 0.5 ~ 0.63
    // 0.67 IMPACT 0.63 ~ 0.67
    // 0.78 FOLLOW 0.67 ~ 0.78
    // 0.99 FINISH 0.78 ~ 0.99

    SWINGSTEP _swingStep = SWINGSTEP.CHECK;
    public SWINGSTEP SwingStep {
        get { return _swingStep; }
    }

    //SWINGSTEP _expertStep = SWINGSTEP.CHECK;// SWINGSTEP.CHECK;
    [SerializeField] List<SWINGSTEP> _expertStep = new List<SWINGSTEP>();// SWINGSTEP.CHECK;
    SWINGSTEP _curStep = SWINGSTEP.CHECK;

    private float _timer = 0;
    private float _timerTarget = 0;
    private float _adressTimer = 0;
    private float _invisibleTimer = 0;
    private float _checkTimer = 0;
    private float angle;
    private bool bReverse = true;

    [SerializeField] Slider sliderAngle;
    [SerializeField] TextMeshProUGUI txtSliderAngle;
    [SerializeField] TextMeshProUGUI txtSwingStep;
    [SerializeField] TextMeshProUGUI txtMocapAngle;
    [SerializeField] TextMeshProUGUI txtCorrectRate;
    [SerializeField] TextMeshProUGUI txtSumValue;
    [SerializeField] bool UseSlider = false;
    [SerializeField] CanvasGroup SwingProgressCG;
    [SerializeField] Image imgPass;
    Sequence sq;
    [SerializeField] Toggle tglAutoSTep;
    private CanvasGroup checkingsScreenCG;

    float _visibleLockTime = 0f;

    private bool _isCheck = true;
    public bool IsCheck 
        { get { return _isCheck; } }

    private bool _visibleLock = true;
    public bool VisibleLock 
        { get { return _visibleLock; } }

    [SerializeField] GameObject DifficultyObj;
    [SerializeField] GameObject VisibleLockScreen;
    [SerializeField] GameObject CheckingScreen;

    bool bFinishArrival = false; //피니시 자세에 도달여부
    bool bTopArrival = false; //피니시 자세에 도달여부
    bool addSecondAction = false; //어르레스 한정 2버째 코칭
    bool bMaxArrivalPreview = false; //마지막 자세에 도달여부(프리뷰용)
    bool bAdressResetPreview = false; //어드레스 여부(프리뷰용)

    [Header("* CHECK PARTS")]
    [SerializeField] AvataMaterialController AvataMaterialController;
    Action ActVisiblePart = null;
    float angleValue = 0.0f;
    float HandAngle = 0;

    [Header("* DEBUG")]
    [SerializeField] Image imgPelvisGauge;
    [SerializeField] Image imgShoulderGauge;
    //[SerializeField] mocapFront mcFront;
    //[SerializeField] mocapSide mcSide;
    [SerializeField] GameObject[] DebugUIs;
    public TextMeshProUGUI txtDebug;

    //Check Swing Data
    ProSwingStepData swingStepData = null;

    [SerializeField] float backwardValue = 2f;
    [SerializeField] float forwardValue = 0.7f;
    [SerializeField] Transform lShoulder;
    [SerializeField] Transform rShoulder;
    [SerializeField] Transform hand;
    float modelAngle = 0;
    Vector3 sCenter;

    float topAniValue = 0.45f;
    float followAniValue = 0.75f;
    float finishAniValue = 0.85f;

    [SerializeField] Transform debugAngleUser;
    [SerializeField] Transform debugAngleModel;

    const float ChkTime_Address = 3f;
    const float ChkTime_Takeback = 3f;//1f;
    const float ChkTime_BackSwing = 3f;//1f;
    const float ChkTime_Top = 0.5f;
    const float ChkTime_DownSwing = 3f;//1f;
    const float ChkTime_Impact = 3f;//1f;
    const float ChkTime_Follow = 3f;//1f;
    const float ChkTime_Finish = 0.5f;
    const float ChkTime_Reset = 2f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        _timer = -1f;
        _adressTimer = 0.5f;
        _invisibleTimer = 0.2f;
        _checkTimer = 1.5f;

        //스윙 데이터 로드
        try
        {
            
        }
        catch { };

        ghostController_Front.SetProAnimation(GameManager.Instance.SwingType, GameManager.Instance.Club);

        if (GameManager.Instance.SwingType == ESwingType.Full)
        {
            if (GameManager.Instance.Club == EClub.MiddleIron || GameManager.Instance.Club == EClub.ShortIron
                || GameManager.Instance.Club == EClub.LongIron)
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.MiddleIron];

                ani3DModel_Front.Play("midiron_full");
                ani3DModel_Foot.Play("midiron_full");
                topAniValue = 0.45f;
                followAniValue = 0.75f;
                finishAniValue = 0.85f;

            }
            else if (GameManager.Instance.Club == EClub.Driver)
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.Driver];

                ani3DModel_Front.Play("driver_full");
                ani3DModel_Foot.Play("driver_full");
                topAniValue = 0.47f;
                followAniValue = 0.075f;
                finishAniValue = 0.85f;
            }
        }

        
        _swingStep = SWINGSTEP.CHECK;
        /* 단일선택
        _expertStep = (SWINGSTEP)(GameManager.Instance.GetStanceIndex() - 7);
        */
        /* 다중선택 */
        for (int i = 0; i < GameManager.Instance.Stance.Count; i++)
            _expertStep.Add((GameManager.Instance.Stance[i]));
            //_expertStep.Add((SWINGSTEP)((int)GameManager.Instance.Stance[i] - 7));
        

#if !UNITY_EDITOR
        UseSlider = false;
#endif

        if (UseSlider) _visibleLock = false;

        checkingsScreenCG = CheckingScreen.GetComponent<CanvasGroup>();
        SwingProgressCG.alpha = 0;

        StartCoroutine(CoModelAnimation());
        imgPass.fillAmount = 0;

        sliderAngle.gameObject.SetActive(UseSlider);
        txtSliderAngle.gameObject.SetActive(UseSlider);

        calKinetic_Front.useUserAngle = !UseSlider;


        if (GameManager.Instance.Mode == EStep.Preview)
        {
            CheckingScreen.SetActive(false);
            DifficultyObj.SetActive(false);
        }

        practiceModeController.ResetFeedbackGraph();

        CheckState(true);

    }

    public void CloseMocap()
    {
        //mocapFront.StopPipeClient();
        //mocapSide.StopPipeClient();
    }

    public bool CheckMocapClose()
    {
        return true;// (mocapFront.IsAlive() == false && mocapSide.IsAlive() == false);
    }

    void AniSetSwingValue(float value)
    {
        //if(bFinishArrival == false && _swingStep == SWINGSTEP.FOLLOW && value > 0.98f)
        //{
        //    bFinishArrival = true;
        //}

        ani3DModel_Front.SetFloat("SwingValue", value);
        ani3DModel_Foot.SetFloat("SwingValue", value);
    }


    void ProSetStep(SWINGSTEP step, Action endEvent= null)
    {
        ghostController_Front.EndEvent = () =>
        {
            AvataMaterialController.Reset();
            if (endEvent != null)
                endEvent.Invoke();
        };

        ghostController_Front.SetPose(step, bReverse);
    }

    public void LoadDebugPoseData()
    {
        SWINGSTEP val = (int)_swingStep < 0 ? SWINGSTEP.ADDRESS : _swingStep;
        ghostController_Front.LoadDebugPoseData(val);
    }

    void ResetAddress(bool isLock = false)
    {
        _visibleLock = isLock;
        _visibleLockTime = isLock ? 0.5f : 0;
        practiceModeController.AnimateProgress(0, 0, 1.0f, true);
        txtCorrectRate.text = string.Empty;
        _swingStep = SWINGSTEP.CHECK;
        CheckState(true);
        SwingProgressCG.alpha = 0;
        practiceModeController.MoveToSwingStep(0);        
        angle = 0.0f;        
        bReverse = true;
        AvataMaterialController.ShowAvatar(!_visibleLock);
        AvataMaterialController.VisibleWeightReset();
        AvataMaterialController.Reset();
        ActVisiblePart = null;
        //SetlimbIK(false);
        //AvataMaterialController.SetLinkIKAble(false);
        practiceModeController.ResetScoreText();
        practiceModeController.CalculateAvgScore(0);
        Utillity.Instance.HideToast(quickHide:true);
        Utillity.Instance.HideGuideArrow();
        bFinishArrival = false;
        bTopArrival = false;
        AniSetSwingValue(0);
        ProSetStep(_swingStep);
        practiceModeController.GenerateScorePanel(false);

        bMaxArrivalPreview = false;
        addSecondAction = false;
        _timer = _timerTarget = ChkTime_Address;// 7.5f;

        sensorProcess.SetAdressCenterShoulder(true);
        //calKinetic_Front.SpineValue = 0;
    }

    void CheckState(bool isCheck = false)
    {
        if (UseSlider) isCheck = false;

        _isCheck = isCheck;

        if (GameManager.Instance.Mode == EStep.Realtime)
            CheckingScreen.SetActive(isCheck);

        bipedIK.enabled = !isCheck;
        ikTargetController.isPause = isCheck;

        if (!isCheck)
        {
            _swingStep = SWINGSTEP.READY;
            practiceModeController.GenerateScorePanel(!isCheck);
            Utillity.Instance.HideToast(quickHide: true);
            //SetlimbIK(true);
            //if (GameManager.Instance.Mode == EStep.Beginner)
            _timer = _timerTarget = ChkTime_Address;// 7.5f;
            //else
            //    _timer = _timerTarget = 0f;
        }
        else
        {
            imgPass.fillAmount = 0;
        }
    }

    /*
    void SetlimbIK(bool isOn)
    {
        if (GameManager.Instance.Mode == EStep.Preview)
        {
            limbIK.solver.IKPositionWeight = isOn ? 1 : 0;
            limbIK.enabled = isOn;
        }
        else
            StartCoroutine(CoSetlimbIK(isOn));
    }

    IEnumerator CoSetlimbIK(bool isOn)
    {
        if (isOn)
        {
            limbIK.solver.IKPositionWeight = 1;// 0;
            limbIK.enabled = true;
        }
        else
        {
            while (limbIK.solver.IKPositionWeight > 0.05f)
            {
                limbIK.solver.IKPositionWeight -= 2f * Time.deltaTime;
                yield return null;
            }
            limbIK.solver.IKPositionWeight = 0;
            limbIK.enabled = false;
        }
    }
    */
    IEnumerator CoModelAnimation()
    {
        if (GameManager.Instance.Mode == EStep.Preview)
        {
            yield return new WaitForSeconds(1f);
            practiceModeController.MoveToSwingStep((int)SWINGSTEP.ADDRESS);
            //practiceModeController.MoveToSwingStep((int)_expertStep.Max());
            bool isStart = false;
            Utillity.Instance.ShowToast("프로의 자세를 자세히 보세요.");
            ProSetStep(_expertStep.Max(), () => isStart = true);
            //ProSetStep(_expertStep, () => isStart = true);

            yield return new WaitUntil(() => isStart == true);
            Utillity.Instance.HideToast(quickHide: true);
            //yield return new WaitForSeconds(1f);

            ghostController_Front.BodyShow(false);

            yield return new WaitForSeconds(0.5f);

            Utillity.Instance.ShowToast("먼저 어드레스 자세를 잡고 동작을 따라해보세요!");
            practiceModeController.GenerateProPreviewPanel();
            practiceModeController.ActivateQuarterAnimation();
        }

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetAddress(false);
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                ActVisiblePart = null;
            }

            Vector3 sCenter = (lShoulder.position + rShoulder.position) / 2f;
            Vector3 dir = hand.position - sCenter;
            modelAngle = Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;

            HandAngle = sensorProcess.iGetHandDir;// mocapFront.GetHandDir();

            if (UseSlider == false && !_visibleLock && (sensorProcess.visibilityFront < 0.4f || sensorProcess.visibilitySide < 0.2f))
            {
                _invisibleTimer -= Time.deltaTime;

                if (_invisibleTimer < 0.0f)
                {
                    _invisibleTimer = 0.2f;
                    practiceModeController.ActivateQuarterAnimation();

                    // preview
                    practiceModeController.AnimateProgress(0, true);

                    ResetAddress(true);
                    yield return null;
                    continue;
                }
            }
            else if (_visibleLock)
            {
                if (sensorProcess.visibilityFront > 0.85f && sensorProcess.visibilitySide > 0.75f)
                {
                    _visibleLockTime -= Time.deltaTime;
                    if (_visibleLockTime < 0.0f)
                    {
                        AvataMaterialController.ShowAvatar(true);
                        _visibleLock = false;
                    }
                }               
                else
                {
                    _visibleLockTime = 0.5f;
                    yield return null;
                    continue;
                }
            }
            else if (!_visibleLock && (sensorProcess.visibilityFront >= 0.7 || sensorProcess.visibilitySide >= 0.65))
                _invisibleTimer = 0.2f;

            if (_visibleLock || AvataMaterialController.GetAvatarOn() == false)
            {
                yield return null;
                continue;
            }

            if (bReverse)
            {
                if (UseSlider)
                {
                    txtSliderAngle.text = sliderAngle.value.ToString("0");
                    angleValue = Mathf.Lerp(angleValue, sliderAngle.value, 0.5f);
                }
                else
                {
                    txtMocapAngle.text = HandAngle.ToString("0");

                    angleValue = Mathf.Lerp(angleValue, HandAngle, 0.5f);
                }
                angle = Mathf.Lerp(angle, (Mathf.Clamp(angleValue, 0.0f, 359.0f) - 192) * -1, 0.5f);
            }
            else
            {
                if (UseSlider)
                {
                    txtSliderAngle.text = sliderAngle.value.ToString("0");
                    angleValue = Mathf.Lerp(angleValue, sliderAngle.value, 0.5f);
                }
                else
                {
                    txtMocapAngle.text = HandAngle.ToString("0");
                    angleValue = Mathf.Lerp(angleValue, HandAngle, 0.5f);
                }
                angle = Mathf.Lerp(angle, Mathf.Clamp(angleValue, 89.0f, 359.0f), 0.5f);
            }

            if (_isCheck && (angleValue > 160 && angleValue < 200) && sensorProcess.IsAddressHand())//(mocapFront.GetHandDistance() < 0.25f))
            {
                _checkTimer -= Time.deltaTime;

                if (_checkTimer <= 0)
                {
                    _checkTimer = 1.5f;
                    _isCheck = false;

                    practiceModeController.DeactivateQuarterAnimation();

                    if (GameManager.Instance.Mode == EStep.Realtime)
                        SwingProgressCG.DOFade(1.0f, 1f).From(0);

                    checkingsScreenCG.DOFade(0.0f, 1.0f).SetEase(Ease.InOutQuad).OnComplete(() =>
                    {
                        CheckState();
                        checkingsScreenCG.alpha = 1.0f;
                    });

                    sensorProcess.SetAdressCenterShoulder();
                }
            }
            else
            {
                _checkTimer = 1.5f;
            }

#if !UNITY_EDITOR
            if (sensorProcess.DistanceAdressCenterShoulder > 130f)
            {
                yield return null;
                if(GameManager.Instance.Mode == EStep.Preview)
                {
                    _curStep = SWINGSTEP.READY;
                    bMaxArrivalPreview = false;
                    bFinishArrival = false;
                    bReverse = true;
                    AniSetSwingValue(0);
                    bAdressResetPreview = false;
                    practiceModeController.AnimateProgress(0, true);
                    Utillity.Instance.HideToast(quickHide:true);
                    Utillity.Instance.HideGuideArrow();
                    AvataMaterialController.Reset();
                    sensorProcess.SetAdressCenterShoulder(true);
                    _isCheck = true;
                }
                else
                    ResetAddress(false);
                continue;
            }
#endif
            if (_isCheck || CheckingScreen.activeInHierarchy)
            {
                yield return null;
                continue;
            }

            if (GameManager.Instance.Mode == EStep.Preview)
            {
                if (bAdressResetPreview == false)
                {
                    if ((angleValue > 160 && angleValue < 200) && sensorProcess.IsAddressHand())
                        bAdressResetPreview = true;
                    else
                    {
                        yield return null;
                        continue;
                    }
                }
            }



            //무게이동
            AvataMaterialController.VisibleWeight();

            //AniSetSwingValue(AngleToStepValue(angleValue, bReverse));
            AngleToStepValue(bReverse);

            float rElbVis = sensorProcess.fRightElbowFrontVis;// mcFront.GetMocapVisibilty(14);            
            //limbIK.solver.IKPositionWeight = Mathf.Lerp(limbIK.solver.IKPositionWeight,  rElbVis > 0.6f ? rElbVis : 0, 0.5f);

            // preview 현재 step
            if (GameManager.Instance.Mode == EStep.Preview)
            {
                practiceModeController.MoveToSwingStep((int)_curStep);
            }

            /*if (bReverse)
            {
                if (angleValue > 180f)
                    calKinetic_Front.SpineValue = -Mathf.Clamp((angleValue - 180f), 0, 25);
                else
                    calKinetic_Front.SpineValue = 0;
            }*/

            if (ghostController_Front.isChanging)
            {
                yield return null;
                continue;
            }




            if(GameManager.Instance.Mode == EStep.Realtime)
            {
                switch (_swingStep)
                {
                    case SWINGSTEP.READY:
                        if (CheckPass_Ready())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.ADDRESS;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);
                            //SetlimbIK(true);
                            //AvataMaterialController.SetLinkIKAble(true);
                            _adressTimer = 0.5f;

                            _timer = _timerTarget = ChkTime_Takeback;// 2.0f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.StanceRateValue, AvataMaterialController.ShoulerHeightAngleValue,
                                //AvataMaterialController.AddressKneeValue, AvataMaterialController.PelvisValue, AvataMaterialController.AddressSpineValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.ShoulderAngle,
                                AvataMaterialController.HandDir,
                                AvataMaterialController.WaistSideDir) * 100, 0);
                            //AvataMaterialController.KneeSideDir) * 100, 0);

                            practiceModeController.DeactivateQuarterAnimation();
                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }
                        break;
                    case SWINGSTEP.ADDRESS:
                        if (CheckPass_Address())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.TAKEBACK;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);


                            _timer = _timerTarget = ChkTime_BackSwing;// 2.0f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.RightElbowValue, AvataMaterialController.WaistValue,
                            //    AvataMaterialController.LeftArmVisibilityValue, AvataMaterialController.ChestHandValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.ShoulderAngle,
                                AvataMaterialController.ForearmAngle,
                                AvataMaterialController.LeftElbowSideVis,
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }

                        break;

                    case SWINGSTEP.TAKEBACK:
                        if (CheckPass_TakeBack())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.BACKSWING;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);
                           //AvataMaterialController.SetLinkIKAble(false);
                            //SetlimbIK(false);

                            _timer = _timerTarget = ChkTime_Top;// 0.5f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.RightElbowValue, AvataMaterialController.ShoulderRotateValue,
                            //    AvataMaterialController.BackSwingPelvisValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.ShoulderAngle,
                                AvataMaterialController.ForearmAngle,
                                AvataMaterialController.PelvisDir,
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }

                        break;
                    case SWINGSTEP.BACKSWING:
                        if (CheckPass_BackSwing())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.TOP;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);
                            bReverse = false;
                            //SetlimbIK(false);
                            //limbIK.enabled = false;
                            _timer = _timerTarget = ChkTime_DownSwing;// 2.0f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue, AvataMaterialController.ShoulderRotateValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.ShoulderDir,
                                AvataMaterialController.HandDir,
                                AvataMaterialController.ForearmAngle,
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }

                        break;
                    case SWINGSTEP.TOP:
                        if (CheckPass_Top())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.DOWNSWING;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);

                            _timer = _timerTarget = ChkTime_Impact;// 2.0f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.ShoulerHeightAngleValue, AvataMaterialController.PelvisValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.ShoulderDir,
                                AvataMaterialController.HandDir,
                                AvataMaterialController.ForearmAngle,
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }

                        break;
                    case SWINGSTEP.DOWNSWING:
                        if (CheckPass_DownSwing())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.IMPACT;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);

                            _timer = _timerTarget = ChkTime_Follow;// 2.0f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.SpineDir,
                                AvataMaterialController.ElbowRightFrontDir,
                                AvataMaterialController.ElbowFrontDir,
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }

                        break;
                    case SWINGSTEP.IMPACT:
                        if (CheckPass_Impact())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.FOLLOW;
                            practiceModeController.MoveToSwingStep((int)_swingStep + 1);
                            ProSetStep(_swingStep);

                            _timer = _timerTarget = ChkTime_Finish;// 0.5f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.ElbowFrontDir,
                                AvataMaterialController.ElbowRightFrontDir,
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            AudioManager.Instance.PlayNext();
                        }
                        break;
                    case SWINGSTEP.FOLLOW:
                        if (CheckPass_Follow())
                        {
                            ActVisiblePart = null;
                            _swingStep = SWINGSTEP.FINISH;
                            ShowPass();
                            //ProSetStep(_swingStep);
                            ikTargetController.isPause = true;

                            _timer = _timerTarget = ChkTime_Reset;// 2.0f;

                            int avg = 0;
                            //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue) * 100, 0);
                            avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                AvataMaterialController.Weight) * 100, 0);

                            practiceModeController.AnimateProgress(0, avg, 1.0f);

                            practiceModeController.GenerateFeedbackPanel(true);

                            AudioManager.Instance.PlayNext();
                        }
                        break;
                    case SWINGSTEP.FINISH:
                        if (CheckPass_Finish())
                        {
                            ActVisiblePart = null;
                            ResetAddress(false);
                            practiceModeController.GenerateFeedbackPanel(false);
                            practiceModeController.GenerateAISwingPanel(false);

                            AudioManager.Instance.PlayNext();

                        }
                        break;
                }
            }
            else if(GameManager.Instance.Mode == EStep.Preview)
            {
                //ProSetStep(_expertStep);
                int avg = 0;

                _timer = _timerTarget = 0.0f;

                if(bReverse)
                {
                    if (angleValue > 200)
                    {
                        yield return null;
                        continue;
                    }
                }
                /*
                else
                {
                    if (bFinishArrivalPreview == false && angleValue > 270)
                        bFinishArrivalPreview = true;
                }*/

                if (UseSlider)
                    HandAngle = angleValue;

                if (_expertStep.Max() > SWINGSTEP.TOP)
                {
                    //if (bReverse && (HandAngle <= Utillity.Instance.dicSwingCheckAngle["topM"] || angleValue <= Utillity.Instance.dicSwingCheckAngle["topM"]))
                    if (bReverse && (angleValue <= (swingStepData.dicTop["GetHandDir"] + 10f)))// || sensorProcess.iGetShoulderDir >= 215))
                    {
                        bReverse = false;
                    }
                }
                
                if(bReverse)
                {
                    if (_expertStep.Contains(_curStep))
                    {
                        switch (_curStep)
                        {
                            case SWINGSTEP.ADDRESS:
                                CheckPass_Ready();
                                //SetlimbIK(false);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.StanceRateValue, AvataMaterialController.ShoulerHeightAngleValue,
                                //    AvataMaterialController.AddressKneeValue, AvataMaterialController.PelvisValue, AvataMaterialController.AddressSpineValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.ShoulderAngle,
                                    AvataMaterialController.HandDir,
                                    AvataMaterialController.WaistSideDir) * 100, 0);
                                //AvataMaterialController.KneeSideDir) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;

                            case SWINGSTEP.TAKEBACK:
                                CheckPass_Address();

                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.RightElbowValue, AvataMaterialController.WaistValue,
                                //    AvataMaterialController.LeftArmVisibilityValue, AvataMaterialController.ChestHandValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.ShoulderAngle,
                                    AvataMaterialController.ForearmAngle,
                                    AvataMaterialController.LeftElbowSideVis,
                                    AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;

                            case SWINGSTEP.BACKSWING:
                                CheckPass_TakeBack();
                                //SetlimbIK(true);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.RightElbowValue, AvataMaterialController.ShoulderRotateValue,
                                //    AvataMaterialController.BackSwingPelvisValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.ShoulderAngle,
                                    AvataMaterialController.ForearmAngle,
                                    AvataMaterialController.PelvisDir,
                                    AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;

                            case SWINGSTEP.TOP:
                                CheckPass_BackSwing();
                                //SetlimbIK(false);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue, AvataMaterialController.ShoulderRotateValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.ShoulderDir,
                                    AvataMaterialController.HandDir,
                                    AvataMaterialController.ForearmAngle,
                                    AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;
                        }
                    }
                }
                else
                {
                    if (_expertStep.Contains(_curStep))
                    {
                        switch (_curStep)
                        {
                            case SWINGSTEP.DOWNSWING:
                                CheckPass_Top();
                                //SetlimbIK(false);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.ShoulerHeightAngleValue, AvataMaterialController.PelvisValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.ShoulderDir,
                                    AvataMaterialController.HandDir,
                                    AvataMaterialController.ForearmAngle,
                                    AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;

                            case SWINGSTEP.IMPACT:
                                CheckPass_DownSwing();
                                //SetlimbIK(false);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.SpineDir,
                                    AvataMaterialController.ElbowRightFrontDir,
                                    AvataMaterialController.ElbowFrontDir,
                                    AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;

                            case SWINGSTEP.FOLLOW:
                                CheckPass_Impact();
                                //SetlimbIK(false);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(
                                    AvataMaterialController.ElbowFrontDir,
                                    AvataMaterialController.ElbowRightFrontDir,
                                    AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;

                            case SWINGSTEP.FINISH:
                                CheckPass_Follow();
                                //SetlimbIK(false);
                                //avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.PelvisValue) * 100, 0);
                                avg = (int)Math.Round(Utillity.Instance.CalculatePoseAverage(AvataMaterialController.Weight) * 100, 0);

                                practiceModeController.AnimateProgress(avg);
                                break;
                        }
                    }

                    /*
                    if (_swingStep == SWINGSTEP.READY)
                    {
                        //if ((HandAngle >= Utillity.Instance.dicSwingCheckAngle["followL"] && HandAngle <= Utillity.Instance.dicSwingCheckAngle["followM"])
                        //|| (angleValue >= Utillity.Instance.dicSwingCheckAngle["followL"] && angleValue <= Utillity.Instance.dicSwingCheckAngle["followM"]))
                        if(HandAngleCheck((int)swingStepData.dicFollow["GetHandDir"]))
                        {
                            _swingStep = SWINGSTEP.FOLLOW;
                        }
                    }*/


                    //if(_curStep > SWINGSTEP.IMPACT &&
                    //HandAngleCheck((int)swingStepData.dicImpact["GetHandDir"]))


                    if (bFinishArrival &&
                        HandAngleCheck((int)swingStepData.dicImpact["GetHandDir"]))
                    {
                        _curStep = SWINGSTEP.READY;
                        bMaxArrivalPreview = false;
                        bFinishArrival = false;
                        bReverse = true;
                        AniSetSwingValue(0);
                        //SetlimbIK(false);
                        AvataMaterialController.VisibleWeightReset();
                        AvataMaterialController.Reset();
                        practiceModeController.AnimateProgress(0, true);
                    }
                }
            }
            
            /*if ((angleValue > 170 && angleValue < 195) && _curStep < SWINGSTEP.TOP && _curStep > SWINGSTEP.ADDRESS)
            {
                _adressTimer -= Time.deltaTime;

                if (_adressTimer < 0)
                {
                    ResetAddress(false);
                }
            }
            else
            {
                _adressTimer = 0.5f;
            }*/
            
            yield return null;
        }
    }

    bool CheckResetPreview(SWINGSTEP arrStep)
    {
        if(_curStep == arrStep)
            return true;

        if (bMaxArrivalPreview == false)
        {
            if (_expertStep.Max() == arrStep)
                bMaxArrivalPreview = true;

            _curStep = arrStep;
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            AvataMaterialController.Reset();

            return true;
        }
        else
        {
            _curStep = SWINGSTEP.READY;
            bMaxArrivalPreview = false;
            bFinishArrival = false;
            bReverse = true;
            AniSetSwingValue(0);
            //SetlimbIK(false);
            bAdressResetPreview = false;
            practiceModeController.AnimateProgress(0, true);
            Utillity.Instance.HideToast(quickHide: true);
            Utillity.Instance.HideGuideArrow();
            AvataMaterialController.Reset();

            return false;
        }
    }

    //=============================================
    // 사용자 스윙 동기화
    //=============================================
    void AngleToStepValue(bool isReverse)
    {
        debugAngleUser.position = sCenter;
        debugAngleModel.position = sCenter;

        debugAngleModel.eulerAngles = new Vector3(0, 0, modelAngle);
        debugAngleUser.eulerAngles = new Vector3(0, 0, angleValue);

        //a :0 / top : 4.5, fend =  0.74
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(modelAngle, angleValue));
        float t = Mathf.InverseLerp(0f, 10, angleDiff);
        float curVal = ani3DModel_Front.GetFloat("SwingValue");
        float setVal = 0;

        if (isReverse == true)
        {
            float back = backwardValue * t;
            if (angleDiff > 1)
            {
                // 180 -> 60
                if (modelAngle >= angleValue)
                {
                    setVal = Mathf.Clamp(curVal + Time.deltaTime * back, 0, topAniValue);
                    AniSetSwingValue( Mathf.Lerp(curVal, setVal, t));
                }
                else if (modelAngle < angleValue)
                {
                    setVal = Mathf.Clamp(curVal - Time.deltaTime * back, 0, topAniValue);
                    AniSetSwingValue( Mathf.Lerp(curVal, setVal, t));
                }

            }

            if (GameManager.Instance.Mode == EStep.Preview)
            {
                //_curStep 처리
                if (HandAngleCheck((int)swingStepData.dicAddress["GetHandDir"]) && sensorProcess.IsAddressHand())//mocapFront.GetHandDistance() < 0.25f)
                {
                    //SetlimbIK(false);
                    //_curStep = SWINGSTEP.ADDRESS;
                    //AvataMaterialController.SetLinkIKAble(false);
                    CheckResetPreview(SWINGSTEP.ADDRESS);
                }
                else if (HandAngleCheck((int)swingStepData.dicTakeback["GetHandDir"]))
                {
                    //SetlimbIK(true);
                    //AvataMaterialController.SetLinkIKAble(true);
                    //_curStep = SWINGSTEP.TAKEBACK;
                    CheckResetPreview(SWINGSTEP.TAKEBACK);
                }
                else if (HandAngleCheck((int)swingStepData.dicBackswing["GetHandDir"]))
                {
                    //SetlimbIK(true);
                    //AvataMaterialController.SetLinkIKAble(true);
                    //_curStep = SWINGSTEP.BACKSWING;
                    CheckResetPreview(SWINGSTEP.BACKSWING);
                }
                else if (HandAngleCheck((int)swingStepData.dicTop["GetHandDir"],
                    MinAngle: 0))//, MaxAngle: (int)swingStepData.dicTop["GetHandDir"] + 10))
                {
                    //SetlimbIK(false);
                    //AvataMaterialController.SetLinkIKAble(false);
                    //_curStep = SWINGSTEP.TOP;
                    CheckResetPreview(SWINGSTEP.TOP);
                }
            }
        }
        else// if (reverse == false)
        {
            if (bFinishArrival == true)
            {
                setVal = curVal + Time.deltaTime * forwardValue;
                AniSetSwingValue(Mathf.Lerp(curVal, setVal, finishAniValue));
            }
            else
            {
                

                //if (angleValue > 280 && _swingStep == SWINGSTEP.FINISH)
                if ((angleValue > swingStepData.dicFinish["GetHandDir"] || (angleValue < 90 && sensorProcess.iGetShoulderDir < 90)) && _swingStep == SWINGSTEP.FOLLOW)
                {
                    bFinishArrival = true;
                }
                else if ((angleValue > swingStepData.dicFinish["GetHandDir"] || (angleValue < 90 && sensorProcess.iGetShoulderDir < 90)) && GameManager.Instance.Mode == EStep.Preview)
                {
                    bFinishArrival = true;
                }
                else
                {
                    if (sensorProcess.iGetShoulderDir < 95)
                    {
                        Mathf.Clamp(angleValue, 270, 359);
                    }

                    float forward = forwardValue * t;
                    if (angleDiff > 1)
                    {
                        //Debug.Log($"{angleDiff} / {t}");
                        // 60 -> 270
                        
                        

                        if (modelAngle >= angleValue)
                        {
                            setVal = Mathf.Clamp(curVal - Time.deltaTime * forward, topAniValue, followAniValue);
                            AniSetSwingValue(Mathf.Lerp(curVal, setVal, t));
                        }
                        else if (modelAngle < angleValue)
                        {
                            setVal = Mathf.Clamp(curVal + Time.deltaTime * forward, topAniValue, 0.99f);
                            AniSetSwingValue(Mathf.Lerp(curVal, setVal, t));
                        }
                    }
                }
            }

            if (GameManager.Instance.Mode == EStep.Preview)
            {
                //_curStep 처리
                if (HandAngleCheck((int)swingStepData.dicDownswing["GetHandDir"]))
                {
                    //SetlimbIK(false);
                    //_curStep = SWINGSTEP.DOWNSWING;
                    //AvataMaterialController.SetLinkIKAble(false);
                    CheckResetPreview(SWINGSTEP.DOWNSWING);
                }
                else if (HandAngleCheck(180))
                {
                    //SetlimbIK(false);
                    //_curStep = SWINGSTEP.IMPACT;
                    //AvataMaterialController.SetLinkIKAble(false);
                    CheckResetPreview(SWINGSTEP.IMPACT);
                }
                else if (HandAngleCheck((int)swingStepData.dicFollow["GetHandDir"]))
                {
                    //SetlimbIK(false);
                    //_curStep = SWINGSTEP.FOLLOW;
                    //AvataMaterialController.SetLinkIKAble(false);
                    CheckResetPreview(SWINGSTEP.FOLLOW);
                }
                else if (HandAngleCheck((int)swingStepData.dicFinish["GetHandDir"], MaxAngle: 360)
                    || (sensorProcess.iGetShoulderDir < 90 && HandAngleCheck((int)swingStepData.dicFinish["GetHandDir"], MaxAngle: 70, MinAngle: 0)))
                {
                    //SetlimbIK(false);
                    //AvataMaterialController.SetLinkIKAble(false);
                    _curStep = SWINGSTEP.FINISH;
                    //CheckResetPreview(SWINGSTEP.TOP);
                }
            }
        }

    }

    bool HandAngleCheck(int BaseAngle, int MinAngle = -1, int MaxAngle = -1)
    {
        if(MinAngle < 0) MinAngle = BaseAngle - 10;
        if(MaxAngle < 0) MaxAngle = BaseAngle + 10;
        //Debug.Log($"HandAngleCheck() :{MinAngle} <= {angleValue} <= {MaxAngle} ");
        if (angleValue >= MinAngle && angleValue <= MaxAngle)
            return true;
        else
            return false;
    }

    bool CheckPass_Ready() //레디중 어드레스 검증
    {

        bool result = false;
        bool condition = ((HandAngleCheck((int)swingStepData.dicAddress["GetHandDir"]) && sensorProcess.IsAddressHand()));//mocapFront.GetHandDistance() < 0.25f));
        
        AvataMaterialController.CheckGetShoulderAngle(swingStepData.dicAddress["GetShoulderAngle"], 7);//
        AvataMaterialController.CheckGetHandDir(swingStepData.dicAddress["GetHandDir"], 7); //
        AvataMaterialController.CheckGetWaistSideDir(swingStepData.dicAddress["GetWaistSideDir"], 7); //
        //AvataMaterialController.CheckGetKneeSideDir(swingStepData.dicAddress["GetKneeSideDir"],7); //        

        txtDebug.text = "";
        txtDebug.text += $"1.GetShoulderAngle:{AvataMaterialController.ShoulderAngle}\r\n";
        txtDebug.text += $"2.GetHandDir:{AvataMaterialController.HandDir}\r\n";
        txtDebug.text += $"3.GetWaistSideDir:{AvataMaterialController.WaistSideDir}\r\n";
        //txtDebug.text += $"4.GetKneeSideDir:{AvataMaterialController.KneeSideDir}\r\n";

        if (condition)
        {
            if (Mathf.Abs(AvataMaterialController.ShoulderAngle) < 1f) //
            {
                if (ActVisiblePart == null)
                    ActVisiblePart = () => AvataMaterialController.VisibleGetShoulderAngle_Address();
            }
            else if (Mathf.Abs(AvataMaterialController.HandDir) < 1f) //
            {
                if (ActVisiblePart == null)
                    ActVisiblePart = () => AvataMaterialController.VisibleGetHandDir_Address();
            }
            else if (Mathf.Abs(AvataMaterialController.WaistSideDir) < 1f) //
            {
                if (ActVisiblePart == null)
                    ActVisiblePart = () => AvataMaterialController.VisibleGetWaistSideDir_Address();
            }
            /*else if (Mathf.Abs(AvataMaterialController.KneeSideDir) < 1f) //
            {
                if (ActVisiblePart == null)
                    ActVisiblePart = () => AvataMaterialController.VisibleGetKneeSideDir_Address();
            }*/
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("AD00"), true);
                Utillity.Instance.HideGuideArrow();
                if (addSecondAction == false)
                {
                    ActVisiblePart = null;
                    addSecondAction = true;
                }
                _timer -= Time.deltaTime;
            }
        }

        result = CheckPass(condition);
        return result;
    }

    bool CheckPass_Address() //어드레스 중 테이크백 검증
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicTakeback["GetHandDir"]);

        AvataMaterialController.CheckGetForearmAngle(swingStepData.dicTakeback["GetForearmAngle"], 13);//오른팔
        AvataMaterialController.CheckGetShoulderAngle(swingStepData.dicTakeback["GetShoulderAngle"], 7);//
        AvataMaterialController.CheckLeftElbowSideVis();//
        AvataMaterialController.CheckGetWeight(swingStepData.dicTakeback["GetWeight"], 5);//
        
        txtDebug.text = "";        
        txtDebug.text += $"1.GetShoulderAngle:{AvataMaterialController.ShoulderAngle}\r\n";
        txtDebug.text += $"2.LeftElbowSideVis:{AvataMaterialController.LeftElbowSideVis}\r\n";
        txtDebug.text += $"3.GetWeight:{AvataMaterialController.Weight}\r\n";
        txtDebug.text += $"4.GetForearmAngle:{AvataMaterialController.ForearmAngle}\r\n";

        if (condition)
        {
            AvataMaterialController.VisibleGetForearmAngle_All(1f); //항상 표시

            if (ActVisiblePart == null && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
            {
                
                if (Mathf.Abs(AvataMaterialController.ShoulderAngle) < 1f) //
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleGetShoulderAngle_Takeback();
                }
                else if (Mathf.Abs(AvataMaterialController.LeftElbowSideVis) > 0.4f) //
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleLeftElbowSideVis_Takeback();
                }
                else if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f) //우선순위 3
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
                }
                else if (Mathf.Abs(AvataMaterialController.ForearmAngle) < 1f) //
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleGetForearmAngle_Takeback();
                }
                else
                {
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TB00"), true);
                    Utillity.Instance.HideGuideArrow();
                }
            }
        }

        result = CheckPass(condition);//, 2f);
        return result;
    }

    bool CheckPass_TakeBack() //테이크백 => 백스윙 검증
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicBackswing["GetHandDir"]);

        AvataMaterialController.CheckGetForearmAngle(swingStepData.dicBackswing["GetForearmAngle"], 13);//오른팔
        AvataMaterialController.CheckGetShoulderAngle(swingStepData.dicBackswing["GetShoulderAngle"], 7);//
        //AvataMaterialController.CheckGetPelvisDir(swingStepData.dicBackswing["GetPelvisDir"], 7);//
        AvataMaterialController.CheckGetWeight(swingStepData.dicBackswing["GetWeight"], 5);//

        txtDebug.text = "";        
        txtDebug.text += $"1.GetShoulderAngle:{AvataMaterialController.ShoulderAngle}\r\n";
        txtDebug.text += $"2.GetPelvisDir:{AvataMaterialController.PelvisDir}\r\n";
        txtDebug.text += $"3.GetWeight:{AvataMaterialController.Weight}\r\n";
        txtDebug.text += $"4.GetForearmAngle:{AvataMaterialController.ForearmAngle}\r\n";

        if (condition)
        {
            AvataMaterialController.VisibleGetForearmAngle_All(1f); //항상 표시

            if (ActVisiblePart == null && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
            {
                if (Mathf.Abs(AvataMaterialController.ShoulderAngle) < 1f) //
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleGetShoulderAngle_Backswing();
                }
                else if (Mathf.Abs(AvataMaterialController.PelvisDir) < 1f)
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleGetPelvisDir_Backswing();
                }
                else if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f)
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
                }
                else if (Mathf.Abs(AvataMaterialController.ForearmAngle) < 1f) //
                {
                    ActVisiblePart = () => AvataMaterialController.VisibleGetForearmAngle_Backswing();
                }
                else
                {
                    Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("BS00"), true);
                    Utillity.Instance.HideGuideArrow();
                }
            }
        }

        //스텝별 체크로직
        result = CheckPass(condition);//, 2f);

        return result;
    }


    bool CheckPass_BackSwing() //백스윙 => 탑 검증
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicTop["GetHandDir"], 
            MinAngle:0, MaxAngle: (int)swingStepData.dicBackswing["GetHandDir"] - 10);

        //AvataMaterialController.CheckGetShoulderDir(swingStepData.dicTop["GetShoulderDir"], 10); //어깨 회전
        AvataMaterialController.CheckGetHandDir(swingStepData.dicTop["GetHandDir"], 7); //
        AvataMaterialController.CheckGetForearmAngle(swingStepData.dicTop["GetForearmAngle"], 13);//오른팔        
        AvataMaterialController.CheckGetWeight(swingStepData.dicTop["GetWeight"], 5);//

        txtDebug.text = "";
        txtDebug.text += $"1.GetShoulderDir:{AvataMaterialController.ShoulderDir}\r\n";
        txtDebug.text += $"2.GetHandDir:{AvataMaterialController.HandDir}\r\n";
        txtDebug.text += $"3.GetWeight:{AvataMaterialController.Weight}\r\n";
        txtDebug.text += $"4.GetForearmAngle:{AvataMaterialController.ForearmAngle}\r\n";
        

        if (ActVisiblePart == null && condition && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
        {
            if (Math.Abs(AvataMaterialController.ShoulderDir) < 1f) //우선순위 1
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetShoulderDir_Top();
            }
            else if (Mathf.Abs(AvataMaterialController.HandDir) < 1f) //
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetHandDir_Top();
            }
            else if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
            }
            else if (Mathf.Abs(AvataMaterialController.ForearmAngle) < 1f) //
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetForearmAngle_Top();
            }
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("TP00"), true);
                Utillity.Instance.HideGuideArrow();
            }
        }

        result = CheckPass(condition);

        return result;
    }

    bool CheckPass_Top() //탑 => 다운스윙 검증
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicDownswing["GetHandDir"]);

//        AvataMaterialController.CheckGetShoulderDir(swingStepData.dicDownswing["GetShoulderDir"], 10); //어깨 회전
        AvataMaterialController.CheckGetHandSideDir(swingStepData.dicDownswing["GetHandSideDir"], 7);
        AvataMaterialController.CheckGetSpineDir(swingStepData.dicDownswing["GetSpineDir"], 7);
        AvataMaterialController.CheckGetWeight(swingStepData.dicDownswing["GetWeight"], 5);

        txtDebug.text = "";
        txtDebug.text += $"1.GetShoulderDir:{AvataMaterialController.ShoulderDir}\r\n";
        txtDebug.text += $"2.GetHandSideDir:{AvataMaterialController.HandSideDir}\r\n";
        txtDebug.text += $"3.GetSpineDir:{AvataMaterialController.SpineDir}\r\n";
        txtDebug.text += $"4.GetWeight:{AvataMaterialController.Weight}\r\n";

        if (ActVisiblePart == null && condition && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
        {
            //if (Math.Abs(AvataMaterialController.ShoulderDir) < 1f)
            if (AvataMaterialController.ShoulderDir > -1f && AvataMaterialController.ShoulderDir < 0)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetShoulderDir_Downswing();
            }
            else if (Mathf.Abs(AvataMaterialController.HandSideDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetHandSideDir_Downswing();
            }
            else if (Mathf.Abs(AvataMaterialController.SpineDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetSpineDir_Downswing();
            }
            else if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
            }
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("DS00"), true);
                Utillity.Instance.HideGuideArrow();
            }
        }


        result = CheckPass(condition);

        return result;
    }

    bool CheckPass_DownSwing() //다운스윙 => 임팩트 검증
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicImpact["GetHandDir"]);
        //bool condition = HandAngleCheck(180);

        AvataMaterialController.CheckGetSpineDir(swingStepData.dicImpact["GetSpineDir"], 7);
        AvataMaterialController.CheckGetElbowRightFrontDir(swingStepData.dicImpact["GetElbowRightFrontDir"], 7);
        AvataMaterialController.CheckGetElbowFrontDir(swingStepData.dicImpact["GetElbowFrontDir"], 7);
        AvataMaterialController.CheckGetWeight(swingStepData.dicImpact["GetWeight"], 5);

        txtDebug.text = "";
        txtDebug.text += $"1.GetSpineDir:{AvataMaterialController.SpineDir}\r\n";
        txtDebug.text += $"2.GetElbowRightFrontDir:{AvataMaterialController.ElbowRightFrontDir}\r\n";
        txtDebug.text += $"3.GetElbowFrontDir:{AvataMaterialController.ElbowFrontDir}\r\n";
        txtDebug.text += $"4.GetWeight:{AvataMaterialController.Weight}\r\n";


        if (ActVisiblePart == null && condition && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
        {
            if (Mathf.Abs(AvataMaterialController.SpineDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetSpineDir_Impact();
            }
            else if (Mathf.Abs(AvataMaterialController.ElbowRightFrontDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetElbowRightFrontDir_Impact();
            }
            else if (Mathf.Abs(AvataMaterialController.ElbowFrontDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetElbowFrontDir_Impact();
            }
            else if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
            }
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("IP00"), true);
                Utillity.Instance.HideGuideArrow();
            }
        }

        result = CheckPass(condition);//, 1.5f);

        return result;
    }

    bool CheckPass_Impact() //임팩트 => 팔로우 검증
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicFollow["GetHandDir"]);

        AvataMaterialController.CheckGetWeight(swingStepData.dicFollow["GetWeight"], 5);
        AvataMaterialController.CheckGetElbowRightFrontDir(swingStepData.dicFollow["GetElbowRightFrontDir"], 7);
        AvataMaterialController.CheckGetElbowFrontDir(swingStepData.dicFollow["GetElbowFrontDir"], 7);
        

        txtDebug.text = "";
        txtDebug.text += $"1.GetWeight:{AvataMaterialController.Weight}\r\n";
        txtDebug.text += $"2.GetElbowRightFrontDir:{AvataMaterialController.ElbowRightFrontDir}\r\n";
        txtDebug.text += $"3.GetElbowFrontDir:{AvataMaterialController.ElbowFrontDir}\r\n";

        if (ActVisiblePart == null && condition && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
        {
            if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
            }
            else if (Mathf.Abs(AvataMaterialController.ElbowRightFrontDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetElbowRightFrontDir_Follow();
            }
            else if (Mathf.Abs(AvataMaterialController.ElbowFrontDir) < 1f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleGetElbowFrontDir_Follow();
            }
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FL00"), true);
                Utillity.Instance.HideGuideArrow();
            }
        }

        //txtDebug.text = $"{AvataMaterialController.BalanceValue.ToString("0.00")} <" + Utillity.Instance.dicCheckFollow["CheckBalanceC"].ToString("0.00")
        //    + "\r\n" + (Math.Abs(AvataMaterialController.BalanceValue) < Utillity.Instance.dicCheckFollow["CheckBalanceC"]);

        result = CheckPass(condition);//, 2f);

        return result;
    }

    bool CheckPass_Follow() //팔로우 => 피니쉬 검증
    {
        bool result = false;
        bool condition = (HandAngleCheck((int)swingStepData.dicFinish["GetHandDir"], MaxAngle:360)
            || HandAngleCheck((int)swingStepData.dicFinish["GetHandDir"], MaxAngle: 70, MinAngle:0)
            || sensorProcess.iGetShoulderDir < 90);

        AvataMaterialController.CheckGetWeight(swingStepData.dicFollow["GetWeight"], 5);

        txtDebug.text = "";
        txtDebug.text += $"1.GetWeight:{AvataMaterialController.Weight}\r\n";

        if (ActVisiblePart == null && condition && (GameManager.Instance.Mode == EStep.Preview || imgPass.fillAmount > 0.1f))
        {
            if (Mathf.Abs(AvataMaterialController.Weight) < 0.7f)
            {
                ActVisiblePart = () => AvataMaterialController.VisibleWeight_All();
            }
            else
            {
                Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackData("FS00"), true);
                Utillity.Instance.HideGuideArrow();
            }
        }

        result = CheckPass(condition);//, 0.2f);

        return result;
    }

    bool CheckPass_Finish() //피니시 후 종료 조건(리셋)
    {
        bool result = false;
        bool condition = HandAngleCheck((int)swingStepData.dicFinish["GetHandDir"], MinAngle:90, MaxAngle:270);
        
        //스텝별 체크로직
        result = CheckPass(condition);

        return result;
    }

    bool CheckPass(bool condition)
    {
        bool ret = false;
        if (tglAutoSTep.isOn == false) return ret;

            
        if (condition)
        {
            if(ActVisiblePart != null)
                ActVisiblePart.Invoke();

            _timer -= Time.deltaTime;
            imgPass.fillAmount = 1 - (_timer / _timerTarget);


            if (_timer < 0)
            {
                imgPass.fillAmount = 0;

                ret = true;
            }
            else
                ret = false;
        }
        else
        {
            AvataMaterialController.Reset();
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();
            ActVisiblePart = null;

            _timer = _timerTarget;
            imgPass.fillAmount = 0;
            ret = false;
        }

        return ret;
    }

    void ShowPass()
    {
        imgPass.enabled = true;
        sq.Restart();
    }

}
