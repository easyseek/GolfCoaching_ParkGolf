using DG.Tweening;
using Enums;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.CoordinateSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Debug = UnityEngine.Debug;

using UnityEngine.Rendering;
using NUnit.Framework;

public class AICoachingDirector : MonoBehaviour
{
    [Header("* 1.GRIP")]
    [SerializeField] private GameObject PanelGrip;

    [Header("* 2.ADDRESS")]
    [SerializeField] private GameObject PanelAddress;
    [SerializeField] private GameObject imgBadIcon;
    [SerializeField] private TextMeshProUGUI txtAddressInfo;
    [SerializeField] private TextMeshProUGUI txtAddressCount;
    private int _addressCount = 3;

    [Header("* 3.SWING")]
    [SerializeField] private GameObject PanelSwing;
    [SerializeField] private CanvasGroup cgSwingInfo;

    [Header("* 4.ANALYZE")]
    [SerializeField] private GameObject PanelAnalyze;
    [SerializeField] private GameObject BlurBack;
    [SerializeField] private TextMeshProUGUI txtAnalyzeInfo;
    [SerializeField] private RectTransform imgLeftUserModel;
    [SerializeField] private RectTransform imgRightProModel;
    [SerializeField] private Image imgMergeGlow;

    [Header("* 5.RESULT")]
    [SerializeField] private GameObject PanelResult;
    [SerializeField] private GameObject m_AnalyzeGroup;
    [SerializeField] private GameObject m_Lesson;
    [SerializeField] private GameObject m_AnalyzeTotal;
    [SerializeField] private GameObject m_AnalyzePose;
    [SerializeField] private GameObject[] m_DotObjects;
    [SerializeField] private GameObject m_DirToggleCover;
    [SerializeField] private GameObject[] m_Models;

    [SerializeField] private RectTransform m_FrontProView, m_SideProView, m_FrontUserView, m_SideUserView;
    [SerializeField] private RectTransform m_FrontProReal, m_SideProReal, m_FrontUserReal, m_SideUserReal;
    [SerializeField] private RectTransform m_BeforeRateBar;
    [SerializeField] private RectTransform m_CurrentRateBar;
    [SerializeField] private RectTransform m_DetailAnalyzePanel;

    [SerializeField] private RawImage[] m_ProThumbnailImgs;
    [SerializeField] private RawImage[] m_UserThumbnailImgs;
    [SerializeField] private RawImage m_FrontProRealRaw, m_SideProRealRaw, m_FrontUserRealRaw, m_SideUserRealRaw;

    [SerializeField] private ToggleGroup m_ResultMainTG;
    [SerializeField] private ToggleGroup m_ResultPoseTG;
    [SerializeField] private ToggleGroup m_ModelDirectionTG;

    [SerializeField] private Toggle[] m_ResultMainToggles;
    [SerializeField] private Toggle[] m_ResultPoseToggles;
    [SerializeField] private Toggle[] m_ModelDirectionToggles;
    [SerializeField] private Toggle m_ModelChangeToggle;
    [SerializeField] private Toggle m_RealVideoSpeedToggle;

    [SerializeField] private VLCVideoPlayer m_RealProFrontVideo, m_RealProSideVideo, m_RealUserFrontVideo, m_RealUserSideVideo;

    [SerializeField] private TextMeshProUGUI m_ProNameText;
    [SerializeField] private TextMeshProUGUI m_UserNameText;
    [SerializeField] private TextMeshProUGUI m_MatchingRateText;
    [SerializeField] private TextMeshProUGUI m_ContrastRateText;
    [SerializeField] private TextMeshProUGUI m_BeforeBarText;
    [SerializeField] private TextMeshProUGUI m_CurrentBarText;
    [SerializeField] private TextMeshProUGUI m_GoodPoseText;
    [SerializeField] private TextMeshProUGUI m_BadPoseText;
    [SerializeField] private TextMeshProUGUI m_CurPoseScoreText;
    [SerializeField] private TextMeshProUGUI[] m_MyScoreTexts;
    [SerializeField] private TextMeshProUGUI[] m_MyScoreNameTexts;
    [SerializeField] private TextMeshProUGUI[] m_DotTexts;
    [SerializeField] private TextMeshProUGUI[] m_TotalTexts;

    [SerializeField] private UILineRenderer m_MyLineRenderer;
    [SerializeField] private UILineRenderer m_AvgLineRenderer;
    [SerializeField] private UILineRenderer m_PoseTimeLineRenderer;

    [SerializeField] private Graphic myFillGraphic;
    [SerializeField] private Graphic avgFillGraphic;

    [SerializeField] private Image m_PoseProgressImg;

    [SerializeField] private Animator m_ProModelAni;
    [SerializeField] private Animator m_UserModelAni;

    private Vector2 detailOpenPos = new Vector2(0, 1701.0f);

    private Vector2 proFrontPos, proFrontSize, proBackPos, proBackSize;
    private Vector2 userFrontPos, userFrontSize, userBackPos, userBackSize;

    private List<int> myScore = Enumerable.Repeat(-1, 8).ToList();
    private List<int> avgScore = new List<int> { 0, 0, 0, 0, 0, 0, 0, 0 };

    private List<int> addressTimeline = new List<int>();
    private List<int> takebackTimeline = new List<int>();
    private List<int> backswingTimeline = new List<int>();
    private List<int> topTimeline = new List<int>();
    private List<int> downswingTimeline = new List<int>();
    private List<int> impactTimeline = new List<int>();
    private List<int> followTimeline = new List<int>();
    private List<int> finishTimeline = new List<int>();

    private const int timelineHistoryMax = 10;

    private float[] currentScore = new float[8];
    private float[] currentAvgScore = new float[8];
    private float radius = 150.0f;
    private int pointCount = 6;

    private bool _isDetailPanelOpen = false;

    private bool _isFrontPro = true;
    private bool _isFrontUser = true;
    private bool _isViewAnimating = false;
    private bool _isTotalAnalyze = true;
    private bool _is3DModel = true;
    private bool[] _isGapStep = new bool[8];

    private List<FrameSensorSnapshot> analyzedProFrameSnapshots = new List<FrameSensorSnapshot>(1024);
    private List<int> matchedUserFrameIndicesForTotal = new List<int>(1024);
    private List<int> totalFrameScores = new List<int>(1024);

    private int totalAnalyzeScore = 0;

    private SWINGSTEP selectStep = SWINGSTEP.ADDRESS;
    private SWINGSTEP stepStage = SWINGSTEP.READY;

    [Header("* MOCAP")]
    [SerializeField] private TextMeshProUGUI m_DebugText;
    [SerializeField] private TextMeshProUGUI m_Debug2Text;
    [SerializeField] private TextMeshProUGUI m_Debug3Text;

    [SerializeField] private bool debugAnalyzeLog = false;
    [SerializeField] private bool debugScoreCsv = false;
    [SerializeField] private bool useRecordingProfileFrames = false;
    private bool _importedRecordingProfileFrames = false;

    private string debugFrameFolderName = "DebugAnalyzeFrames";

    private bool captureUseAsyncReadback = true;
    private bool _captureDone = false;

    private PoseLandmarker _offlinePoseLM = null;
    private TextureFrame _tfAnalyzer = null;
    private bool _poseLMForVideo = false;

    private UnityEngine.Rect screenRect;

    private string _debugFrameDir = string.Empty;

    private System.Text.StringBuilder _handDirLogSb = new System.Text.StringBuilder(8192);

    [SerializeField] private SensorProcess sensorProcess;
    [SerializeField] private WebcamTracker webcamFront;
    [SerializeField] private WebcamTrackerController webcamTrackerController;

    private ProSwingStepData swingStepData = null;
    private ProSwingStepData aiSwingStepData = null;

    private Dictionary<string, int[]> DicUserSwingData = new Dictionary<string, int[]>();

    private Dictionary<string, float> ErrorMargins = new Dictionary<string, float>()
    {
        { "GetHandDir", 95f }, { "GetHandDistance", 76f }, { "GetShoulderDistance", 30f },
        { "GetSpineDir", 5f }, { "GetShoulderAngle", 30f }, { "GetFootDisRate", 79f },
        { "GetWeight", 20f }, { "GetForearmAngle", 60f }, { "GetElbowFrontDir", 30f },
        { "GetHandSideDir", 70f }, { "GetWaistSideDir", 5f }, { "GetKneeSideDir", 18f },
        { "GetElbowSideDir", 45f }, { "GetArmpitDir", 15f }, { "GetShoulderDir", 75f },
        { "GetPelvisDir", 68f }
    };

    private struct FrameSensorSnapshot
    {
        public bool isValid;

        // Front
        public int handDir;
        public int handDirNF;
        public int handDistance;
        public int shoulderDistance;
        public int spineDir;
        public int shoulderAngle;
        public int footDisRate;
        public int weight;
        public int forearmAngle;
        public int elbowFrontDir;
        public int elbowRightFrontDir;
        public int handDirDistance;
        public int shoulderFrontDirWorld;
        public int pelvisFrontDirWorld;
        public int noseDir;
        public int pelvisAngle;

        // Side
        public int handSideDir;
        public int waistSideDir;
        public int kneeSideDir;
        public int elbowSideDir;
        public int armpitDir;
        public int handSideDistance;
        public int gripDistance;
        public int shoulderSideDirWorld;
        public int pelvisSideDirWorld;
        public int noseShoulderSideDir;
        public int nosePelvisSideDir;

        // Combine
        public int shoulderDir;
        public int pelvisDir;
        public int handCombineDir;
    }

    private List<FrameSensorSnapshot> analyzedFrameSnapshots = new List<FrameSensorSnapshot>(1024);

    private int takebackDetectedFrameIndex = -1;

    [Header("* VIDEO REF.")]
    [SerializeField] private RawImage rawImageFront;
    [SerializeField] private RawImage rawImageSide;

    public List<byte[]> framesFront = new List<byte[]>();
    public List<byte[]> framesSide = new List<byte[]>();

    private List<Texture2D> captureRealPoseFrontPro = Enumerable.Repeat<Texture2D>(null, 8).ToList();
    private List<Texture2D> captureRealPoseSidePro = Enumerable.Repeat<Texture2D>(null, 8).ToList();
    private List<Texture2D> captureRealPoseFrontUser = Enumerable.Repeat<Texture2D>(null, 8).ToList();
    private List<Texture2D> captureRealPoseSideUser = Enumerable.Repeat<Texture2D>(null, 8).ToList();

    private bool isRecording = false;
    private bool checkTakeback = false;
    private bool checkImpact = false;
    private bool isFinish = false;

    private int checkTakebackFrame = 0;
    private int widthFront;
    private int heightFront;
    private int widthSide;
    private int heightSide;
    private int curStepNum = 0;

    private float _lastHandDir;
    private bool _handCheck = false;
    private float AvgVisible = 0;

    private enum COACHINGSTEP
    {
        GRIP,
        ADDRESS,
        SWING,
        SWINGEND,
        ANALYZE,
        RESULT
    }

    private COACHINGSTEP coahingStep = COACHINGSTEP.GRIP;

    private static readonly SWINGSTEP[] RadarOrder = new[]
    {
        SWINGSTEP.ADDRESS,
        SWINGSTEP.TAKEBACK,
        SWINGSTEP.BACKSWING,
        SWINGSTEP.DOWNSWING,
        SWINGSTEP.FOLLOW,
        SWINGSTEP.FINISH
    };

    private PoseLandmarker offlinePoseLandmarker;
    private TextureFrame offlineTextureFrame;

    private string logPath = string.Empty;
    private List<string> logList = new List<string>();

    [Header("* EXTERNAL RGB ANALYZE")]
    [SerializeField] private bool useExternalRgbAnalyze = true;
    [SerializeField] private KeyCode externalAnalyzeKey = KeyCode.F9;

    [SerializeField] private TextAsset[] externalFrontRgbTextAssets;
    [SerializeField] private TextAsset[] externalSideRgbTextAssets;

    private int _externalRgbTextAssetIndex = 0;
    [SerializeField] private int externalRgbWidth = 640;
    [SerializeField] private int externalRgbHeight = 480;

    private bool _importedExternalRgbFrames = false;

    [SerializeField] private bool externalRgbBatchVerify = true;

    [SerializeField] private int externalRgbBatchRuns = 20;

    [SerializeField] private string externalRgbBatchOutputFolder = "ExternalRGB_BatchVerify";

    private bool _externalBatchRunning = false;

    private sealed class BatchRunResult
    {
        public int assetIndex;
        public int runIndex;
        public int[] stepIndex;
        public Dictionary<string, int[]> userData;
        public List<int> myScore;
    }

    private IEnumerator Start()
    {
        yield return null;

        Init();
        StartCoroutine(CheckAISwing());
    }

    private void Init()
    {
        if (webcamTrackerController != null)
        {
            webcamTrackerController.SetTracker(true, true);
        }

        string homeDir = System.Environment.GetEnvironmentVariable("HOME");

        if (GolfProDataManager.Instance != null)
        {
            GolfProDataManager.Instance.ReloadProSwingData();
        }

        swingStepData = null;
        aiSwingStepData = null;

        if (GolfProDataManager.Instance != null &&
            GolfProDataManager.Instance.SelectProData != null &&
            GolfProDataManager.Instance.SelectProData.swingData != null &&
            GolfProDataManager.Instance.SelectProData.swingData.dicFull != null &&
            GolfProDataManager.Instance.SelectProData.swingData.dicFull.ContainsKey(EClub.MiddleIron))
        {
            swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.MiddleIron];
        }

        if (GolfProDataManager.Instance != null &&
            GolfProDataManager.Instance.SelectProData != null &&
            GolfProDataManager.Instance.SelectProData.aiSwingData != null &&
            GolfProDataManager.Instance.SelectProData.aiSwingData.dicFull != null &&
            GolfProDataManager.Instance.SelectProData.aiSwingData.dicFull.ContainsKey(EClub.MiddleIron))
        {
            aiSwingStepData = GolfProDataManager.Instance.SelectProData.aiSwingData.dicFull[EClub.MiddleIron];
        }

        //aiSwingStepData = GolfProDataManager.Instance.SelectProData.aiSwingData.dicFull[EClub.MiddleIron];
        //swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.MiddleIron];

        string imagePath = Path.Combine(homeDir, "DataBase_park", "ProImage", $"{GolfProDataManager.Instance.SelectProData.uid}");

        for (int i = 0; i < 8; i++)
        {
            string frontPath = Path.Combine(imagePath, $"{((SWINGSTEP)i).ToString().ToLower()}_front_03_ai.png");
            string sidePath = Path.Combine(imagePath, $"{((SWINGSTEP)i).ToString().ToLower()}_side_03_ai.png");

            Debug.Log(frontPath);
            captureRealPoseFrontPro[i] = LoadTextureFromFile(frontPath);
            captureRealPoseSidePro[i] = LoadTextureFromFile(sidePath);
        }

        proFrontPos = m_FrontProView.anchoredPosition;
        proFrontSize = m_FrontProView.sizeDelta;
        proBackPos = m_SideProView.anchoredPosition;
        proBackSize = m_SideProView.sizeDelta;

        userFrontPos = m_FrontUserView.anchoredPosition;
        userFrontSize = m_FrontUserView.sizeDelta;
        userBackPos = m_SideUserView.anchoredPosition;
        userBackSize = m_SideUserView.sizeDelta;

        for (int i = 0; i < m_ResultMainToggles.Length; i++)
        {
            m_ResultMainToggles[i].onValueChanged.AddListener(OnValueChanged_ResultMainToggle);
        }

        for (int i = 0; i < m_ResultPoseToggles.Length; i++)
        {
            m_ResultPoseToggles[i].onValueChanged.AddListener(OnValueChanged_ResultPoseToggle);
        }

        for (int i = 0; i < m_ModelDirectionToggles.Length; i++)
        {
            m_ModelDirectionToggles[i].onValueChanged.AddListener(OnValueChanged_ToggleDirection);
        }

        if (m_ModelChangeToggle != null)
        {
            m_ModelChangeToggle.onValueChanged.AddListener(OnValueChanged_ModelChange);
        }

        if (m_RealVideoSpeedToggle != null)
        {
            m_RealVideoSpeedToggle.onValueChanged.AddListener(OnValueChanged_RealVideoSpeed);
        }

        //AnimateMatchingRate(0.0f, (myScore.Sum() / (myScore.Count - 2)) * 0.01f);
        AnimateMatchingRate(0.0f, Mathf.Clamp(totalAnalyzeScore, 0, 100) * 0.01f);
        AnimateTotalGraph(myScore, avgScore, 1.1f);
        StartCoroutine(ModelAnimation(true, m_ProModelAni, m_UserModelAni));

        SetReadyGrip();
    }

    private void SetReadyGrip()
    {
        ClearUserStepTextures();

        checkTakeback = false;
        checkImpact = false;
        isFinish = false;
        checkTakebackFrame = 0;
        takebackDetectedFrameIndex = -1;

        framesFront.Clear();
        framesSide.Clear();

        _importedRecordingProfileFrames = false;
        _importedExternalRgbFrames = false;

        _captureDone = false;

        Resources.UnloadUnusedAssets();

        _addressCount = 3;

        myScore = Enumerable.Repeat(-1, 8).ToList();
        stepStage = SWINGSTEP.READY;

        _handDirLogSb.Length = 0;

        captureRealPoseFrontUser = Enumerable.Repeat<Texture2D>(null, 8).ToList();
        captureRealPoseSideUser = Enumerable.Repeat<Texture2D>(null, 8).ToList();

        matchedUserFrameIndicesForTotal.Clear();
        totalFrameScores.Clear();
        totalAnalyzeScore = 0;
    }

    private IEnumerator CheckAISwing()
    {
        float timer = 0f;
        SetReadyGrip();

        Debug.Log("[CheckAISwing] START");

        while (true)
        {
            float handDir = -1f;

            if (sensorProcess != null)
            {
                handDir = sensorProcess.iGetHandDirNF;
                _handCheck = sensorProcess.IsAddressHand();
                _lastHandDir = handDir;
            }

            //m_DebugText.text = $"IsAdressHand:{_handCheck}";

            //m_Debug3Text.text = $"";

            switch (coahingStep)
            {
                case COACHINGSTEP.GRIP:
                    {
                        if (_externalBatchRunning)
                            break;

                        if (useExternalRgbAnalyze && Input.GetKeyDown(externalAnalyzeKey))
                        {
                            Debug.Log("[ExternalRGB] Key pressed");

                            SetReadyGrip();

                            if (externalRgbBatchVerify)
                            {
                                yield return StartCoroutine(RunExternalRgbBatchVerify());
                            }
                            else
                            {
                                SetCoachinggStep(COACHINGSTEP.ANALYZE);
                            }

                            break;
                        }

                        if (useExternalRgbAnalyze)
                            break;

                        if (_handCheck && handDir > swingStepData.dicTakeback["GetHandDir"]/* &&
                            handDir < (swingStepData.dicAddress["GetHandDir"] + 10f)*/)
                        {
                            timer += Time.deltaTime;
                            if (timer > 0.5f)
                            {
                                timer = 0f;
                                SetCoachinggStep(COACHINGSTEP.ADDRESS);
                                Debug.Log("[CheckAISwing] GRIP -> ADDRESS");
                            }
                        }
                        else
                        {
                            timer = 0f;
                        }
                    }
                    break;

                case COACHINGSTEP.ADDRESS:
                    {
                        m_Debug2Text.text = $"[handDir] {handDir} / {swingStepData.dicAddress["GetHandDir"]}";

                        if (sensorProcess.IsVisibility() && _handCheck && handDir >= swingStepData.dicTakeback["GetHandDir"])
                        {
                            timer += Time.deltaTime;
                            txtAddressInfo.text = "자세를 유지하고 준비해주세요\r\n곧 시작합니다";
                            txtAddressCount.text = _addressCount.ToString();
                            imgBadIcon.SetActive(false);

                            if (timer > 1f)
                            {
                                _addressCount--;
                                timer = 0f;

                                if (_addressCount < 0)
                                {
                                    SetResultData();

                                    ResetSwingForNewRecord();

                                    stepStage = SWINGSTEP.TAKEBACK;
                                    curStepNum = 1;

                                    cgSwingInfo.DOFade(0, 1f).SetDelay(1f);

                                    if (File.Exists(logPath))
                                    {
                                        File.Delete(logPath);
                                    }

                                    if (useRecordingProfileFrames && ProfileVerifyBuffer.HasData)
                                    {
                                        isRecording = false;
                                        SetCoachinggStep(COACHINGSTEP.ANALYZE);
                                    }
                                    else
                                    {
                                        SetCoachinggStep(COACHINGSTEP.SWING);
                                        isRecording = true;
                                        StartCoroutine(CaptureFrames());
                                    }
                                }
                            }
                        }
                        else
                        {
                            timer = 0f;
                            //txtAddressInfo.text = "자세를 인식하지 못했어요\r\n그립을 다시 잡아주세요";
                            txtAddressCount.text = string.Empty;
                            //imgBadIcon.SetActive(true);

                            SetCoachinggStep(COACHINGSTEP.GRIP);
                        }
                    }
                    break;

                case COACHINGSTEP.SWING:
                    {
                        if (!checkTakeback)
                        {
                            // if (_handCheck && handDir < swingStepData.dicTakeback["GetHandDir"])
                            // {
                            //     checkTakeback = true;
                            // }

                            if (_handCheck && handDir < swingStepData.dicTakeback["GetHandDir"])
                            {
                                TrimBeforeTakeback();
                                checkTakeback = true;
                            }

                            break;
                        }

                        if (!checkImpact)
                        {
                            if (handDir > swingStepData.dicImpact["GetHandDir"])
                            {
                                checkImpact = true;
                            }
                        }

                        if (checkImpact)
                        {
                            Debug.Log("[CheckAISwing] SWING -> SWINGEND");
                            SetCoachinggStep(COACHINGSTEP.SWINGEND);
                        }
                    }
                    break;

                case COACHINGSTEP.SWINGEND:
                    {
                        if (isRecording || !_captureDone)
                            break;

                        SetCoachinggStep(COACHINGSTEP.ANALYZE);
                        Debug.Log("[CheckAISwing] SWINGEND -> ANALYZE");
                    }
                    break;

                case COACHINGSTEP.ANALYZE:
                    {
                        Debug.Log("[CheckAISwing] Enter ANALYZE");

                        yield return StartCoroutine(AnalyzeSwingFromAllFrames());

                        txtAnalyzeInfo.text = "프로의 스윙과 매칭 중입니다";
                        imgLeftUserModel.gameObject.SetActive(true);
                        imgRightProModel.gameObject.SetActive(true);

                        yield return new WaitForSeconds(0.1f);
                        imgLeftUserModel.DOLocalMoveX(0, 1.3f);
                        imgRightProModel.DOLocalMoveX(0, 1.3f);

                        yield return new WaitForSeconds(1.5f);
                        imgMergeGlow.gameObject.SetActive(true);
                        imgMergeGlow.DOFade(1, 0.5f);

                        yield return new WaitForSeconds(1.3f);
                        txtAnalyzeInfo.text = "잠시 후 결과 화면으로 넘어갑니다";
                        imgLeftUserModel.gameObject.SetActive(false);
                        imgRightProModel.gameObject.SetActive(false);
                        imgMergeGlow.gameObject.SetActive(false);

                        yield return new WaitForSeconds(1.0f);

                        SetCoachinggStep(COACHINGSTEP.RESULT);
                        Debug.Log("[CheckAISwing] ANALYZE -> RESULT");
                    }
                    break;

                case COACHINGSTEP.RESULT:
                    {
                    }
                    break;
            }

            yield return null;
        }
    }

    private void ResetSwingForNewRecord()
    {
        curStepNum = 0;

        captureRealPoseFrontUser = Enumerable.Repeat<Texture2D>(null, 8).ToList();
        captureRealPoseSideUser = Enumerable.Repeat<Texture2D>(null, 8).ToList();

        isFinish = false;
        checkTakeback = false;
        checkImpact = false;
        stepStage = SWINGSTEP.TAKEBACK;
        takebackDetectedFrameIndex = -1;
    }

    private void SetCoachinggStep(COACHINGSTEP step)
    {
        if (step == COACHINGSTEP.GRIP)
        {
            SetReadyGrip();
            NewShowPanel(step);
        }
        else
        {
            NewShowPanel(step);
        }

        Debug.Log($"SetCoachinggStep() {coahingStep} -> {step}");
        coahingStep = step;
    }

    private void NewShowPanel(COACHINGSTEP step)
    {
        PanelGrip.SetActive(false);
        PanelAddress.SetActive(false);
        PanelSwing.SetActive(false);
        PanelAnalyze.SetActive(false);
        PanelResult.SetActive(false);
        BlurBack.SetActive(false);

        if (step == COACHINGSTEP.GRIP)
        {
            PanelGrip.SetActive(true);
        }
        else if (step == COACHINGSTEP.ADDRESS)
        {
            PanelAddress.SetActive(true);
            imgBadIcon.SetActive(false);
            _addressCount = 3;
            txtAddressInfo.text = "자세를 유지하고 준비해주세요\r\n곧 시작합니다";
            txtAddressCount.text = _addressCount.ToString();
            imgBadIcon.SetActive(false);
        }
        else if (step == COACHINGSTEP.SWING)
        {
            PanelSwing.SetActive(true);
            cgSwingInfo.alpha = 1;
        }
        else if (step == COACHINGSTEP.ANALYZE)
        {
            PanelAnalyze.SetActive(true);
            BlurBack.SetActive(true);
            txtAnalyzeInfo.text = "스윙을 분석하고 있습니다";
            imgLeftUserModel.gameObject.SetActive(false);
            imgLeftUserModel.anchoredPosition = new Vector2(-227, 0);
            imgRightProModel.gameObject.SetActive(false);
            imgRightProModel.anchoredPosition = new Vector2(232, 0);
            imgMergeGlow.gameObject.SetActive(false);
        }
        else if (step == COACHINGSTEP.RESULT)
        {
            OnClick_Result();

            SetTotalImageSetting();
            //AnimateMatchingRate(0.0f, (myScore.Sum() / (myScore.Count - 2)) * 0.01f);
            AnimateMatchingRate(0.0f, Mathf.Clamp(totalAnalyzeScore, 0, 100) * 0.01f);
            AnimateTotalGraph(myScore, avgScore, 1.1f);
        }
    }

    private Tween beforeRateTween;
    private Tween currentRateTween;

    public void AnimateMatchingRate(float beforeRatio, float currentRatio)
    {
        float beforeHeight = 195.0f * beforeRatio;
        int beforePercent = Mathf.RoundToInt(beforeRatio * 100);

        float currentHeight = 195.0f * currentRatio;
        int currentPercent = Mathf.RoundToInt(currentRatio * 100);

        m_BeforeRateBar.sizeDelta = new Vector2(m_BeforeRateBar.sizeDelta.x, 0);
        m_CurrentRateBar.sizeDelta = new Vector2(m_CurrentRateBar.sizeDelta.x, 0);

        if (beforeRateTween != null && beforeRateTween.IsActive())
        {
            beforeRateTween.Kill();
        }

        if (currentRateTween != null && currentRateTween.IsActive())
        {
            currentRateTween.Kill();
        }

        beforeRateTween = DOTween.To(() => m_BeforeRateBar.sizeDelta.y, y =>
        {
            m_BeforeRateBar.sizeDelta = new Vector2(m_BeforeRateBar.sizeDelta.x, y);
        }, beforeHeight, 1.0f).SetEase(Ease.OutCubic);

        currentRateTween = DOTween.To(() => m_CurrentRateBar.sizeDelta.y, y =>
        {
            m_CurrentRateBar.sizeDelta = new Vector2(m_CurrentRateBar.sizeDelta.x, y);
        }, currentHeight, 1.0f).SetEase(Ease.OutCubic);

        m_MatchingRateText.text = $"{currentPercent}%";

        DOTween.To(() => 0, x =>
        {
            m_BeforeBarText.text = $"{x}%";
        }, beforePercent, 1.0f).SetEase(Ease.OutCubic);

        DOTween.To(() => 0, x =>
        {
            m_CurrentBarText.text = $"{x}%";
        }, currentPercent, 1.0f).SetEase(Ease.OutCubic);
    }

    private void DrawTotalGraph(UILineRenderer renderer, float[] values)
    {
        if (values.Length != pointCount)
        {
            return;
        }

        List<Vector2> points = new List<Vector2>();
        float angleStep = 360.0f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angleDeg = 90f - (angleStep * i);
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float scaled = values[i] / 100f * radius;
            Vector2 point = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * scaled;
            points.Add(point);
        }

        points.Add(points[0]);

        renderer.Points = points.ToArray();
        renderer.SetAllDirty();
    }

    private void FillTotalGraph(Graphic graphic, float[] values)
    {
        if (!(graphic is MaskableGraphic))
        {
            return;
        }

        VertexHelper vh = new VertexHelper();
        Vector2 center = graphic.rectTransform.rect.center;
        vh.AddVert(center, graphic.color, Vector2.zero);

        float angleStep = 360f / pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            float angle = (90.0f - (angleStep * i)) * Mathf.Deg2Rad;
            float scaled = values[i] / 100.0f * radius;
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * scaled;
            vh.AddVert(pos, graphic.color, Vector2.zero);
        }

        for (int i = 1; i <= pointCount; i++)
        {
            int next = (i % pointCount) + 1;
            vh.AddTriangle(0, i, next);
        }

        Mesh mesh = new Mesh();
        vh.FillMesh(mesh);
        graphic.canvasRenderer.SetMesh(mesh);
        graphic.canvasRenderer.SetColor(graphic.color);
    }

    private void SaveStepFromFrame(int stepIdx, int frameIndex)
    {
        stepIdx = Mathf.Clamp(stepIdx, 0, 7);

        if (frameIndex < 0 || frameIndex >= framesFront.Count)
        {
            return;
        }

        if (captureRealPoseFrontUser[stepIdx] != null) Destroy(captureRealPoseFrontUser[stepIdx]);
        if (captureRealPoseSideUser[stepIdx] != null) Destroy(captureRealPoseSideUser[stepIdx]);

        Texture2D frontRawTex = null;
        if (framesFront != null && frameIndex >= 0 && frameIndex < framesFront.Count)
        {
            byte[] frontBytes = framesFront[frameIndex];
            if (frontBytes != null)
            {
                frontRawTex = CreateTextureFromRaw(frontBytes, widthFront, heightFront);
            }
        }

        Texture2D sideRawTex = null;
        if (framesSide != null && frameIndex >= 0 && frameIndex < framesSide.Count)
        {
            byte[] sideBytes = framesSide[frameIndex];
            if (sideBytes != null)
            {
                sideRawTex = CreateTextureFromRaw(sideBytes, widthSide, heightSide);
            }
        }

        try
        {
            if (frontRawTex != null)
            {
                captureRealPoseFrontUser[stepIdx] = ApplyThumbnailTransform(frontRawTex);
            }

            if (sideRawTex != null)
            {
                captureRealPoseSideUser[stepIdx] = ApplyThumbnailTransform(sideRawTex);
            }
        }
        finally
        {
            if (frontRawTex != null) Destroy(frontRawTex);
            if (sideRawTex != null) Destroy(sideRawTex);
        }
    }

    private Texture2D Rotate90(Texture2D src, bool cw)
    {
        int w = src.width;
        int h = src.height;

        Texture2D dst = new Texture2D(h, w, src.format, false);
        Color32[] srcPix = src.GetPixels32();
        Color32[] dstPix = new Color32[srcPix.Length];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int si = x + y * w;
                int dx;
                int dy;

                if (cw)
                {
                    dx = h - 1 - y;
                    dy = x;
                }
                else
                {
                    dx = y;
                    dy = w - 1 - x;
                }

                int di = dx + dy * h;
                dstPix[di] = srcPix[si];
            }
        }

        dst.SetPixels32(dstPix);
        dst.Apply(false);

        return dst;
    }

    private Texture2D Rotate180(Texture2D src)
    {
        if (src == null)
        {
            return null;
        }

        int w = src.width;
        int h = src.height;

        Texture2D dst = new Texture2D(w, h, src.format, false);

        Color32[] sp = src.GetPixels32();
        Color32[] dp = new Color32[sp.Length];

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            int ry = (h - 1 - y) * w;
            for (int x = 0; x < w; x++)
            {
                dp[(w - 1 - x) + ry] = sp[x + row];
            }
        }

        dst.SetPixels32(dp);
        dst.Apply(false);
        return dst;
    }

    private void SetTotalImageSetting()
    {
        int score = 0;

        for (int i = 0; i < m_ProThumbnailImgs.Length; i++)
        {
            int texIndex;

            switch (i)
            {
                case 0: texIndex = (int)SWINGSTEP.ADDRESS; break;
                case 1: texIndex = (int)SWINGSTEP.TAKEBACK; break;
                case 2: texIndex = (int)SWINGSTEP.BACKSWING; break;
                case 3: texIndex = (int)SWINGSTEP.BACKSWING; break;
                case 4: texIndex = (int)SWINGSTEP.DOWNSWING; break;
                case 5: texIndex = (int)SWINGSTEP.IMPACT; break;
                case 6: texIndex = (int)SWINGSTEP.FOLLOW; break;
                case 7: texIndex = (int)SWINGSTEP.FINISH; break;
                default: texIndex = -1; break;
            }

            if (texIndex >= 0 && texIndex < captureRealPoseFrontPro.Count)
            {
                m_ProThumbnailImgs[i].texture = captureRealPoseFrontPro[texIndex];
            }
            else
            {
                m_ProThumbnailImgs[i].texture = null;
            }

            if (texIndex >= 0 && texIndex < captureRealPoseFrontUser.Count)
            {
                m_UserThumbnailImgs[i].texture = captureRealPoseFrontUser[texIndex];
            }
            else
            {
                m_UserThumbnailImgs[i].texture = null;
            }

            switch (i)
            {
                case 0: score = GetStepScore(addressTimeline, (int)SWINGSTEP.ADDRESS); break;
                case 1: score = GetStepScore(takebackTimeline, (int)SWINGSTEP.TAKEBACK); break;
                case 2: score = GetStepScore(backswingTimeline, (int)SWINGSTEP.BACKSWING); break;
                case 3: score = GetStepScore(topTimeline, (int)SWINGSTEP.TOP); break;
                case 4: score = GetStepScore(downswingTimeline, (int)SWINGSTEP.DOWNSWING); break;
                case 5: score = GetStepScore(impactTimeline, (int)SWINGSTEP.IMPACT); break;
                case 6: score = GetStepScore(followTimeline, (int)SWINGSTEP.FOLLOW); break;
                case 7: score = GetStepScore(finishTimeline, (int)SWINGSTEP.FINISH); break;
                default: score = 0; break;
            }

#if UNITY_EDITOR
            m_TotalTexts[i].color =
                score <= 29 ? UnityEngine.Color.red :
                (score <= 79 ? UnityEngine.Color.yellow : UnityEngine.Color.green);
#else
            m_TotalTexts[i].color =
                score <= 29 ? Utillity.Instance.HexToRGB(INI.Red) :
                (score <= 79 ? Utillity.Instance.HexToRGB(INI.Yellow) : Utillity.Instance.HexToRGB(INI.Green500));
#endif
            m_TotalTexts[i].text = score.ToString();
        }
    }

    private int GetStepScore(List<int> timeline, int stepIndex)
    {
        if (timeline != null && timeline.Count > 0)
        {
            int last = timeline[timeline.Count - 1];
            if (last > 0)
            {
                return last;
            }
        }

        if (stepIndex >= 0 && stepIndex < myScore.Count && myScore[stepIndex] > 0)
        {
            return myScore[stepIndex];
        }

        return 0;
    }

    public SWINGSTEP GetCurStep()
    {
        return stepStage;
    }

    public void AnimateTotalGraph(List<int> targetScore, List<int> avgScore, float duration = 0.5f)
    {
        List<int> radarScores = new List<int>();
        for (int i = 0; i < RadarOrder.Length; i++)
        {
            int idx = (int)RadarOrder[i];
            radarScores.Add((idx >= 0 && idx < targetScore.Count) ? targetScore[idx] : 0);
        }

        for (int i = 0; i < pointCount; i++)
        {
            int index = i;
            float start = 0f;
            float end = radarScores[i];

            DOTween.To(() => start, x =>
            {
                currentScore[index] = x;

                if (index < m_MyScoreTexts.Length)
                {
                    m_MyScoreTexts[index].text = $"{Mathf.RoundToInt(x)}%";
                }

                DrawTotalGraph(m_MyLineRenderer, currentScore);
                FillTotalGraph(myFillGraphic, currentScore);

            }, end, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (index == pointCount - 1)
                {
                    HighlightMaxMin();
                }
            });

            int avg = (i < avgScore.Count) ? avgScore[i] : 0;

            DOTween.To(() => start, x =>
            {
                currentAvgScore[index] = x;

                DrawTotalGraph(m_AvgLineRenderer, currentAvgScore);
                FillTotalGraph(avgFillGraphic, currentAvgScore);

            }, avg, duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true);
        }
    }

    private void HighlightMaxMin()
    {
        int maxIndex = 0;
        int minIndex = 0;

        for (int i = 1; i < pointCount; i++)
        {
            if (currentScore[i] > currentScore[maxIndex])
                maxIndex = i;

            if (currentScore[i] < currentScore[minIndex])
                minIndex = i;
        }

        for (int i = 0; i < pointCount; i++)
        {
            if (i >= m_MyScoreNameTexts.Length)
            {
                continue;
            }

            SWINGSTEP step = RadarOrder[i];

            if (i == maxIndex)
            {
                m_GoodPoseText.text = Utillity.Instance.ConvertEnumToString(step);
                m_MyScoreNameTexts[i].color = Utillity.Instance.HexToRGB(INI.Green500);
            }
            else if (i == minIndex)
            {
                m_BadPoseText.text = Utillity.Instance.ConvertEnumToString(step);
                m_MyScoreNameTexts[i].color = Utillity.Instance.HexToRGB(INI.Red);
            }
            else
            {
                m_MyScoreNameTexts[i].color = Color.white;
            }

            m_MyScoreNameTexts[i].SetAllDirty();
        }
    }

    private void DrawTimeline(List<int> rates)
    {
        float yMax = 215.0f;
        List<Vector2> drawPoints = new List<Vector2>();

        for (int i = 0; i < m_DotObjects.Length; i++)
        {
            m_DotObjects[i].SetActive(false);
        }

        if (rates == null || rates.Count == 0)
        {
            m_PoseTimeLineRenderer.Points = System.Array.Empty<Vector2>();
            m_PoseTimeLineRenderer.SetAllDirty();
            return;
        }

        int maxDots = m_DotObjects != null ? m_DotObjects.Length : 0;
        int n = Mathf.Min(maxDots, rates.Count);
        int startIdx = rates.Count - n;

        for (int j = 0; j < n; j++)
        {
            int rate = rates[startIdx + j];
            float y = (rate / 100.0f) * yMax;

            RectTransform rt = m_DotObjects[j].GetComponent<RectTransform>();
            Vector2 anchoredPos = rt.anchoredPosition;
            anchoredPos.y = y;
            rt.anchoredPosition = anchoredPos;

            m_DotObjects[j].SetActive(true);

            Image img = m_DotObjects[j].GetComponent<Image>();

            if (img != null)
            {
#if UNITY_EDITOR
                img.color = rate <= 29 ? Color.red : (rate <= 79 ? Color.yellow : Color.green);
#else
                img.color = rate <= 29 ? Utillity.Instance.HexToRGB(INI.Red) : (rate <= 79 ? Utillity.Instance.HexToRGB(INI.Yellow) : Utillity.Instance.HexToRGB(INI.Green500));
#endif
            }

            if (m_DotTexts != null && j < m_DotTexts.Length)
                m_DotTexts[j].text = $"{rate}%";

            drawPoints.Add(new Vector2(rt.anchoredPosition.x, y));
        }

        m_PoseTimeLineRenderer.Points = drawPoints.ToArray();
        m_PoseTimeLineRenderer.SetAllDirty();

        for (int i = n; i < m_DotObjects.Length; i++)
        {
            m_DotObjects[i].SetActive(false);
        }
    }

    public void AnimateProgress(int value, float duration = 1.0f)
    {
        m_CurPoseScoreText.text = $"0<size=60%>%</size>";

#if UNITY_EDITOR
        m_PoseProgressImg.color = value <= 29 ? Color.red : (value <= 79 ? Color.yellow : Color.green);
#else
        m_PoseProgressImg.color = value <= 29 ? Utillity.Instance.HexToRGB(INI.Red) : (value <= 79 ? Utillity.Instance.HexToRGB(INI.Yellow) : Utillity.Instance.HexToRGB(INI.Green500));
#endif

        m_PoseProgressImg.fillAmount = 0f;
        m_PoseProgressImg.DOKill();
        m_PoseProgressImg.DOFillAmount(value / 100f, duration).SetEase(Ease.OutQuad);

        DOTween.To(() => 0, x =>
        {
            m_CurPoseScoreText.text = $"{x:0}<size=60%>%</size>";
        }, value, duration).SetEase(Ease.OutQuad);
    }

    public void ToggleModelView(bool front, bool is3DModel)
    {
        if (_isViewAnimating)
        {
            return;
        }

        m_DirToggleCover.SetActive(true);
        _isViewAnimating = true;


        RectTransform toProFront = _isFrontPro ? (is3DModel ? m_SideProView : m_SideProReal) : (is3DModel ? m_FrontProView : m_FrontProReal);
        RectTransform toProBack = _isFrontPro ? (is3DModel ? m_FrontProView : m_FrontProReal) : (is3DModel ? m_SideProView : m_SideProReal);

        proFrontPos = is3DModel ? m_FrontProView.anchoredPosition : m_FrontProReal.anchoredPosition;
        proFrontSize = is3DModel ? m_FrontProView.sizeDelta : m_FrontProReal.sizeDelta;
        proBackPos = is3DModel ? m_SideProView.anchoredPosition : m_SideProReal.anchoredPosition;
        proBackSize = is3DModel ? m_SideProView.sizeDelta : m_SideProReal.sizeDelta;

        Vector2 toProFrontPos = _isFrontPro ? proFrontPos : proBackPos;
        Vector2 toProFrontSize = _isFrontPro ? proFrontSize : proBackSize;
        Vector2 toProBackPos = _isFrontPro ? proBackPos : proFrontPos;
        Vector2 toProBackSize = _isFrontPro ? proBackSize : proFrontSize;

        toProFront.SetAsLastSibling();

        toProFront.DOAnchorPos(toProFrontPos, 0.35f).SetEase(Ease.InOutCubic);
        toProFront.DOSizeDelta(toProFrontSize, 0.35f).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            _isFrontPro = front;
            _isViewAnimating = false;
            m_DirToggleCover.SetActive(false);
        });

        toProBack.DOAnchorPos(toProBackPos, 0.35f).SetEase(Ease.InOutCubic);
        toProBack.DOSizeDelta(toProBackSize, 0.35f).SetEase(Ease.InOutCubic);


        RectTransform toUserFront = _isFrontUser ? (is3DModel ? m_SideUserView : m_SideUserReal) : (is3DModel ? m_FrontUserView : m_FrontUserReal);
        RectTransform toUserBack = _isFrontUser ? (is3DModel ? m_FrontUserView : m_FrontUserReal) : (is3DModel ? m_SideUserView : m_SideUserReal);

        userFrontPos = is3DModel ? m_FrontUserView.anchoredPosition : m_FrontUserReal.anchoredPosition;
        userFrontSize = is3DModel ? m_FrontUserView.sizeDelta : m_FrontUserReal.sizeDelta;
        userBackPos = is3DModel ? m_SideUserView.anchoredPosition : m_SideUserReal.anchoredPosition;
        userBackSize = is3DModel ? m_SideUserView.sizeDelta : m_SideUserReal.sizeDelta;

        Vector2 toUserFrontPos = _isFrontUser ? userFrontPos : userBackPos;
        Vector2 toUserFrontSize = _isFrontUser ? userFrontSize : userBackSize;
        Vector2 toUserBackPos = _isFrontUser ? userBackPos : userFrontPos;
        Vector2 toUserBackSize = _isFrontUser ? userBackSize : userFrontSize;

        toUserFront.SetAsLastSibling();

        toUserFront.DOAnchorPos(toUserFrontPos, 0.35f).SetEase(Ease.InOutCubic);
        toUserFront.DOSizeDelta(toUserFrontSize, 0.35f).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            _isFrontUser = front;
            _isViewAnimating = false;
            m_DirToggleCover.SetActive(false);
        });

        toUserBack.DOAnchorPos(toUserBackPos, 0.35f).SetEase(Ease.InOutCubic);
        toUserBack.DOSizeDelta(toUserBackSize, 0.35f).SetEase(Ease.InOutCubic);

    }

    private IEnumerator ModelAnimation(bool isAnim, params Animator[] anims)
    {
        if (anims == null || anims.Length == 0)
        {
            yield break;
        }

        if (isAnim)
        {
            while (true)
            {
                float elapsedTime = 0f;

                while (elapsedTime < 2.0f)
                {
                    if (!_isTotalAnalyze || !_is3DModel)
                    {
                        for (int i = 0; i < anims.Length; i++)
                        {
                            anims[i].SetFloat("SwingValue", 0.0f);
                        }
                        break;
                    }

                    for (int i = 0; i < anims.Length; i++)
                    {
                        anims[i].SetFloat("SwingValue", Mathf.Lerp(0, 0.99f, elapsedTime / 2.0f));
                    }

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                if (!_isTotalAnalyze || !_is3DModel)
                {
                    for (int i = 0; i < anims.Length; i++)
                    {
                        anims[i].SetFloat("SwingValue", 0.0f);
                    }
                    break;
                }

                for (int i = 0; i < anims.Length; i++)
                {
                    anims[i].SetFloat("SwingValue", 0.99f);
                }

                yield return new WaitForSeconds(1.0f);
            }
        }
        else
        {
            if (!_isTotalAnalyze)
            {
                float value = 0.0f;

                switch (selectStep)
                {
                    case SWINGSTEP.ADDRESS: value = 0.0f; break;
                    case SWINGSTEP.TAKEBACK: value = 0.23f; break;
                    case SWINGSTEP.BACKSWING: value = 0.35f; break;
                    case SWINGSTEP.TOP: value = 0.5f; break;
                    case SWINGSTEP.DOWNSWING: value = 0.61f; break;
                    case SWINGSTEP.IMPACT: value = 0.661f; break;
                    case SWINGSTEP.FOLLOW: value = 0.76f; break;
                    case SWINGSTEP.FINISH: value = 0.99f; break;
                }

                for (int i = 0; i < anims.Length; i++)
                {
                    anims[i].SetFloat("SwingValue", value);
                }
            }

            yield return null;
        }
    }

    private void VideoControl()
    {
        if (_isTotalAnalyze)
        {
            m_RealProFrontVideo.targetDisplay = m_FrontProRealRaw;
            m_RealProSideVideo.targetDisplay = m_SideProRealRaw;
            m_RealUserFrontVideo.targetDisplay = m_FrontUserRealRaw;
            m_RealUserSideVideo.targetDisplay = m_SideUserRealRaw;

            m_RealProFrontVideo.Play();
            m_RealProSideVideo.Play();

            m_RealUserFrontVideo.Stop();
            m_RealUserSideVideo.Stop();
        }
        else
        {
            m_RealProFrontVideo.Stop();
            m_RealProSideVideo.Stop();
            m_RealUserFrontVideo.Stop();
            m_RealUserSideVideo.Stop();

            m_RealProFrontVideo.targetDisplay = null;
            m_RealProSideVideo.targetDisplay = null;
            m_RealUserFrontVideo.targetDisplay = null;
            m_RealUserSideVideo.targetDisplay = null;

            int idx = (selectStep == SWINGSTEP.BACKSWING) ? 2 : (int)selectStep;

            m_FrontProRealRaw.color = Color.white;
            m_SideProRealRaw.color = Color.white;
            m_FrontUserRealRaw.color = Color.white;
            m_SideUserRealRaw.color = Color.white;

            m_FrontProRealRaw.texture = captureRealPoseFrontPro[idx];
            m_SideProRealRaw.texture = captureRealPoseSidePro[idx];

            m_FrontUserRealRaw.texture = captureRealPoseFrontUser[idx];
            m_SideUserRealRaw.texture = captureRealPoseSideUser[idx];
        }
    }

    public void Onclick_Button(string name)
    {
        switch (name)
        {
            case "Home":
                GameManager.Instance.SelectedSceneName = string.Empty;
                SceneManager.LoadScene("ModeSelect");
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;
        }
    }

    public void OnClick_Direction()
    {
        ToggleModelView(!_isFrontPro, false);
        ToggleModelView(!_isFrontUser, false);
    }

    public void OnClick_Result()
    {
        m_ProNameText.text = $"{GolfProDataManager.Instance.SelectProData.infoData.name} 프로";

        PanelResult.SetActive(true);

        m_ResultMainToggles[0].isOn = true;
        m_ResultMainToggles[0].onValueChanged.Invoke(true);

        m_ModelChangeToggle.onValueChanged.Invoke(false);
        m_RealVideoSpeedToggle.onValueChanged.Invoke(false);
    }

    public void OnClick_Video() { }

    public void OnClick_Retry()
    {
        SetCoachinggStep(COACHINGSTEP.GRIP);
    }

    public void OnClick_RetryCancel()
    {
        BlurBack.SetActive(false);
    }

    public void OnClick_RetryApply()
    {
        BlurBack.SetActive(false);
        SetCoachinggStep(COACHINGSTEP.GRIP);
    }

    public void OnClick_DetailAnalyzePanel()
    {
        _isDetailPanelOpen = !_isDetailPanelOpen;

        BlurBack.SetActive(_isDetailPanelOpen);
        m_DetailAnalyzePanel.DOAnchorPosY(_isDetailPanelOpen ? detailOpenPos.y : 0, 0.3f)
            .SetEase(_isDetailPanelOpen ? Ease.InCubic : Ease.OutCubic);
    }

    public void OnValueChanged_ResultMainToggle(bool isOn)
    {
        if (m_ResultMainTG.GetFirstActiveToggle() == null)
        {
            return;
        }

        int num = m_ResultMainTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (!isOn)
        {
            return;
        }

        if (num == 0)
        {
            _isTotalAnalyze = true;
            m_AnalyzeGroup.SetActive(true);
            m_Lesson.SetActive(false);
            m_AnalyzeTotal.SetActive(true);
            m_AnalyzePose.SetActive(false);

            OnValueChanged_ModelChange(_is3DModel);

            //AnimateMatchingRate(0.0f, (myScore.Sum() / (myScore.Count - 2)) * 0.01f);
            AnimateMatchingRate(0.0f, Mathf.Clamp(totalAnalyzeScore, 0, 100) * 0.01f);
            AnimateTotalGraph(myScore, avgScore, 1.1f);

            StartCoroutine(ModelAnimation(true, m_ProModelAni, m_UserModelAni));
        }
        else if (num == 1)
        {
            _isTotalAnalyze = false;
            m_AnalyzeGroup.SetActive(true);
            m_Lesson.SetActive(false);
            m_AnalyzeTotal.SetActive(false);
            m_AnalyzePose.SetActive(true);

            selectStep = SWINGSTEP.ADDRESS;

            if (!m_ResultPoseToggles[0].isOn)
            {
                m_ResultPoseToggles[0].isOn = true;
            }

            m_FrontUserReal.localScale = Vector3.one;
            m_SideUserReal.localScale = Vector3.one;

            AnimateProgress(addressTimeline.Count > 0 ? addressTimeline[addressTimeline.Count - 1] : 0);
            DrawTimeline(addressTimeline);

            StartCoroutine(ModelAnimation(false, m_ProModelAni, m_UserModelAni));
        }
        else if (num == 2)
        {
            _isTotalAnalyze = false;
            m_AnalyzeGroup.SetActive(false);
            m_Lesson.SetActive(true);
            m_AnalyzeTotal.SetActive(false);
            m_AnalyzePose.SetActive(false);

            StartCoroutine(ModelAnimation(false, m_ProModelAni, m_UserModelAni));
        }

        VideoControl();
    }

    public void OnValueChanged_ResultPoseToggle(bool isOn)
    {
        if (m_ResultPoseTG.GetFirstActiveToggle() == null)
        {
            return;
        }

        int num = m_ResultPoseTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (!isOn)
        {
            return;
        }

        selectStep = (SWINGSTEP)num;

        switch (num)
        {
            case 0: AnimateProgress(addressTimeline.Count > 0 ? addressTimeline[addressTimeline.Count - 1] : 0); DrawTimeline(addressTimeline); break;
            case 1: AnimateProgress(takebackTimeline.Count > 0 ? takebackTimeline[takebackTimeline.Count - 1] : 0); DrawTimeline(takebackTimeline); break;
            case 2: AnimateProgress(backswingTimeline.Count > 0 ? backswingTimeline[backswingTimeline.Count - 1] : 0); DrawTimeline(backswingTimeline); break;
            case 3: AnimateProgress(topTimeline.Count > 0 ? topTimeline[topTimeline.Count - 1] : 0); DrawTimeline(topTimeline); break;
            case 4: AnimateProgress(downswingTimeline.Count > 0 ? downswingTimeline[downswingTimeline.Count - 1] : 0); DrawTimeline(downswingTimeline); break;
            case 5: AnimateProgress(impactTimeline.Count > 0 ? impactTimeline[impactTimeline.Count - 1] : 0); DrawTimeline(impactTimeline); break;
            case 6: AnimateProgress(followTimeline.Count > 0 ? followTimeline[followTimeline.Count - 1] : 0); DrawTimeline(followTimeline); break;
            case 7: AnimateProgress(finishTimeline.Count > 0 ? finishTimeline[finishTimeline.Count - 1] : 0); DrawTimeline(finishTimeline); break;
        }

        StartCoroutine(ModelAnimation(false, m_ProModelAni, m_UserModelAni));
        VideoControl();
    }

    public void OnValueChanged_ToggleDirection(bool isOn)
    {
        if (m_ModelDirectionTG.GetFirstActiveToggle() == null)
        {
            return;
        }

        int num = m_ModelDirectionTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (!isOn)
        {
            return;
        }

        switch (num)
        {
            case 0:
                break;

            case 1:
                break;
        }
    }

    public void OnValueChanged_ModelChange(bool isOn)
    {
        _is3DModel = isOn;

        m_Models[0].SetActive(isOn);
        m_Models[1].SetActive(!isOn);

        if (isOn)
        {
            StartCoroutine(ModelAnimation(_isTotalAnalyze, m_ProModelAni, m_UserModelAni));
        }
        else
        {
            if (_isTotalAnalyze)
            {
                int uid = GolfProDataManager.Instance.SelectProData.uid;

                string proPath = GolfProDataManager.Instance.SelectProData.videoData
                    .Where(v => v.direction == EPoseDirection.Front && v.videoType == EVideoType.Swing && v.clubFilter == EClub.MiddleIron && v.swingType == ESwingType.Full)
                    .Select(v => v.path).FirstOrDefault();

                m_RealProFrontVideo.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{proPath}");

                proPath = GolfProDataManager.Instance.SelectProData.videoData
                    .Where(v => v.direction == EPoseDirection.Side && v.videoType == EVideoType.Swing && v.clubFilter == EClub.MiddleIron && v.swingType == ESwingType.Full)
                    .Select(v => v.path).FirstOrDefault();

                m_RealProSideVideo.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{proPath}");

                m_RealUserFrontVideo.url = string.Empty;
                m_RealUserSideVideo.url = string.Empty;
            }
            else
            {
                VideoControl();
            }
        }
    }

    public void OnValueChanged_RealVideoSpeed(bool isOn)
    {
        float speed = isOn ? 0.25f : 1.0f;

        m_RealProFrontVideo.playbackSpeed = speed;
        m_RealProSideVideo.playbackSpeed = speed;
        m_RealUserFrontVideo.playbackSpeed = speed;
        m_RealUserSideVideo.playbackSpeed = speed;
    }

    private void TrimBeforeTakeback()
    {
        if (framesFront == null || framesFront.Count <= 0)
        {
            return;
        }

        int trimCount = Mathf.Max(0, framesFront.Count - 6);

        if (trimCount <= 0)
        {
            return;
        }

        trimCount = Mathf.Min(trimCount, framesFront.Count);
        framesFront.RemoveRange(0, trimCount);

        if (framesSide != null && framesSide.Count > 0)
        {
            int sideTrimCount = Mathf.Min(trimCount, framesSide.Count);
            framesSide.RemoveRange(0, sideTrimCount);
        }
    }

    IEnumerator CaptureAsync(Texture source, List<byte[]> targetList, bool isFront)
    {
        if (source == null)
            yield break;

        if (source.width <= 16 || source.height <= 16)
            yield break;

        int w = source.width;
        int h = source.height;

        if (isFront)
        {
            widthFront = w;
            heightFront = h;
        }
        else
        {
            widthSide = w;
            heightSide = h;
        }

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0);
        Graphics.Blit(source, rt);

        AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24, (request) =>
        {
            if (!request.hasError)
            {
                byte[] data = request.GetData<byte>().ToArray();
                targetList.Add(data);

                if (targetList.Count > 90)
                {
                    targetList.RemoveAt(0);
                }
            }

            RenderTexture.ReleaseTemporary(rt);
        });

        yield break;
    }

    private IEnumerator CaptureFrames()
    {
        int finishFrame = 22;
        int postImpactLeft = -1;

        _captureDone = false;
        isRecording = true;

        while (isRecording)
        {
            yield return new WaitForEndOfFrame();

            Texture srcFront = rawImageFront != null ? rawImageFront.texture : null;
            Texture srcSide = rawImageSide != null ? rawImageSide.texture : null;

            if (srcFront == null)
            {
                yield return null;
                continue;
            }

            StartCoroutine(CaptureAsync(srcFront, framesFront, true));

            bool doSide = (webcamTrackerController != null && webcamTrackerController.IsSideOn && srcSide != null);

            if (doSide)
            {
                StartCoroutine(CaptureAsync(srcSide, framesSide, false));
            }
            else
            {
                framesSide.Add(null);
            }

            if (checkImpact)
            {
                if (postImpactLeft < 0)
                    postImpactLeft = finishFrame;

                if (postImpactLeft > 0)
                {
                    postImpactLeft--;
                }
                else
                {
                    isRecording = false;
                }
            }

            yield return null;
        }

        _captureDone = true;
    }

    // Debug
    private void EnsureDebugFrameDir()
    {
        if (!string.IsNullOrEmpty(_debugFrameDir))
        {
            return;
        }

        _debugFrameDir = Path.Combine(Application.persistentDataPath, debugFrameFolderName);
        try
        {
            if (!Directory.Exists(_debugFrameDir))
            {
                Directory.CreateDirectory(_debugFrameDir);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandDir] EnsureDebugFrameDir failed: {e}");
            _debugFrameDir = Application.persistentDataPath;
        }
    }

    private Texture2D ResizeIfNeeded(Texture2D src, int maxWidth)
    {
        if (src == null) return null;
        if (maxWidth <= 0 || src.width <= maxWidth) return src;

        float ratio = (float)maxWidth / (float)src.width;
        int newW = maxWidth;
        int newH = Mathf.Max(1, Mathf.RoundToInt(src.height * ratio));

        RenderTexture rt = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D dst = new Texture2D(newW, newH, TextureFormat.RGB24, false);
        dst.ReadPixels(new UnityEngine.Rect(0, 0, newW, newH), 0, 0);
        dst.Apply(false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return dst;
    }

    private string SaveFrameJpg(Texture2D tex, string fileName)
    {
        if (tex == null) return string.Empty;

        EnsureDebugFrameDir();
        string path = Path.Combine(_debugFrameDir, fileName);

        Texture2D small = ResizeIfNeeded(tex, 640);
        try
        {
            byte[] jpg = (small != null ? small : tex).EncodeToJPG(80);
            File.WriteAllBytes(path, jpg);

            return path;
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandDir] SaveFrameJpg failed: {e}");
            return string.Empty;
        }
        finally
        {
            if (small != null && small != tex)
            {
                Destroy(small);
            }
        }
    }

    private void ResetHandDirCompareLog()
    {
        _handDirLogSb.Length = 0;
        _handDirLogSb.AppendLine("frame,raw,kal,front_jpg,side_jpg");
    }

    private void SaveHandDirCompareCsv(string fileName = "HandDir_Compare.csv")
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, _handDirLogSb.ToString());
            Debug.Log($"[HandDir] saved: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[HandDir] save failed: {e}");
        }
    }

    private float[] BuildProHandDirTargets()
    {
        float[] t = new float[8];

        t[(int)SWINGSTEP.ADDRESS] = swingStepData.dicAddress["GetHandDir"];
        t[(int)SWINGSTEP.TAKEBACK] = swingStepData.dicTakeback["GetHandDir"];
        t[(int)SWINGSTEP.BACKSWING] = swingStepData.dicBackswing["GetHandDir"];
        t[(int)SWINGSTEP.TOP] = swingStepData.dicTop["GetHandDir"];
        t[(int)SWINGSTEP.DOWNSWING] = swingStepData.dicDownswing["GetHandDir"];
        t[(int)SWINGSTEP.IMPACT] = swingStepData.dicImpact["GetHandDir"];
        t[(int)SWINGSTEP.FOLLOW] = swingStepData.dicFollow["GetHandDir"];
        t[(int)SWINGSTEP.FINISH] = swingStepData.dicFinish["GetHandDir"];

        return t;
    }

    private Texture2D ApplyAnalyzeTransform(Texture2D src)
    {
        if (src == null)
        {
            return null;
        }

        Texture2D mirror = null;
        Texture2D rot = null;

        try
        {
            mirror = MirrorX(src);
            rot = Rotate180(mirror);
            return rot;
        }
        finally
        {
            if (mirror != null)
            {
                Destroy(mirror);
                mirror = null;
            }
        }
    }

    private Texture2D ApplyThumbnailTransform(Texture2D src)
    {
        if (src == null) return null;

        Texture2D rotated = Rotate90(src, false);

        Texture2D fixedTex = null;
        try
        {
            fixedTex = MirrorX(rotated);
        }
        finally
        {
            if (rotated != null) Destroy(rotated);
        }

        return fixedTex;
    }

    private Texture2D MirrorX(Texture2D src)
    {
        if (src == null) return null;

        int w = src.width;
        int h = src.height;

        Texture2D dst = new Texture2D(w, h, src.format, false);
        Color32[] sp = src.GetPixels32();
        Color32[] dp = new Color32[sp.Length];

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                dp[(w - 1 - x) + row] = sp[x + row];
            }
        }

        dst.SetPixels32(dp);
        dst.Apply(false);
        return dst;
    }

    private Texture2D LoadTextureFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        Texture2D tex = new Texture2D(2, 2);
        if (tex.LoadImage(fileData))
        {
            return tex;
        }

        return null;
    }

    private void SetResultData()
    {
        DicUserSwingData.Clear();

        // Front
        DicUserSwingData.Add("GetHandDir", new int[8]);
        DicUserSwingData.Add("GetHandDistance", new int[8]);
        DicUserSwingData.Add("GetShoulderDistance", new int[8]);
        DicUserSwingData.Add("GetSpineDir", new int[8]);
        DicUserSwingData.Add("GetShoulderAngle", new int[8]);
        DicUserSwingData.Add("GetFootDisRate", new int[8]);
        DicUserSwingData.Add("GetWeight", new int[8]);
        DicUserSwingData.Add("GetForearmAngle", new int[8]);
        DicUserSwingData.Add("GetElbowFrontDir", new int[8]);
        DicUserSwingData.Add("GetElbowRightFrontDir", new int[8]);
        DicUserSwingData.Add("GetHandDirDistance", new int[8]);
        DicUserSwingData.Add("GetShoulderFrontDirWorld", new int[8]);
        DicUserSwingData.Add("GetPelvisFrontDirWorld", new int[8]);
        DicUserSwingData.Add("GetNoseDir", new int[8]);
        DicUserSwingData.Add("GetPelvisAngle", new int[8]);

        // Side
        DicUserSwingData.Add("GetHandSideDir", new int[8]);
        DicUserSwingData.Add("GetWaistSideDir", new int[8]);
        DicUserSwingData.Add("GetKneeSideDir", new int[8]);
        DicUserSwingData.Add("GetElbowSideDir", new int[8]);
        DicUserSwingData.Add("GetArmpitDir", new int[8]);
        DicUserSwingData.Add("GetHandSideDistance", new int[8]);
        DicUserSwingData.Add("GetGripDistance", new int[8]);
        DicUserSwingData.Add("GetShoulderSideDirWorld", new int[8]);
        DicUserSwingData.Add("GetPelvisSideDirWorld", new int[8]);
        DicUserSwingData.Add("GetNoseShoulderSideDir", new int[8]);
        DicUserSwingData.Add("GetNosePelvisSideDir", new int[8]);

        // Combine
        DicUserSwingData.Add("GetShoulderDir", new int[8]);
        DicUserSwingData.Add("GetPelvisDir", new int[8]);
        DicUserSwingData.Add("GetHandCombineDir", new int[8]);
    }

    private void UserDataAdd(int step)
    {
        if (step < 0 || step > 7)
            return;

        if (sensorProcess == null)
            return;

        if (swingStepData == null)
            return;

        if (DicUserSwingData == null)
            return;

        if (myScore == null || myScore.Count < 8)
            return;

        if (!DicUserSwingData.ContainsKey("GetHandDir"))
            return;

        // Front
        DicUserSwingData["GetHandDir"][step] = sensorProcess.iGetHandDir;
        DicUserSwingData["GetHandDistance"][step] = sensorProcess.iGetHandDistance;
        DicUserSwingData["GetShoulderDistance"][step] = sensorProcess.iGetShoulderDistance;
        DicUserSwingData["GetSpineDir"][step] = sensorProcess.iGetSpineDir;
        DicUserSwingData["GetShoulderAngle"][step] = sensorProcess.iGetShoulderAngle;
        DicUserSwingData["GetFootDisRate"][step] = sensorProcess.iGetFootDisRate;
        DicUserSwingData["GetWeight"][step] = sensorProcess.iGetWeight;
        DicUserSwingData["GetForearmAngle"][step] = sensorProcess.iGetForearmAngle;
        DicUserSwingData["GetElbowFrontDir"][step] = sensorProcess.iGetElbowFrontDir;
        DicUserSwingData["GetElbowRightFrontDir"][step] = sensorProcess.iGetElbowRightFrontDir;
        DicUserSwingData["GetHandDirDistance"][step] = sensorProcess.iGetHandDirDistance;
        DicUserSwingData["GetShoulderFrontDirWorld"][step] = sensorProcess.iGetShoulderFrontDirWorld;
        DicUserSwingData["GetPelvisFrontDirWorld"][step] = sensorProcess.iGetPelvisFrontDirWorld;
        DicUserSwingData["GetNoseDir"][step] = sensorProcess.iGetNoseDir;
        DicUserSwingData["GetPelvisAngle"][step] = sensorProcess.iGetPelvisAngle;

        // Side
        DicUserSwingData["GetHandSideDir"][step] = sensorProcess.iGetHandSideDir;
        DicUserSwingData["GetWaistSideDir"][step] = sensorProcess.iGetWaistSideDir;
        DicUserSwingData["GetKneeSideDir"][step] = sensorProcess.iGetKneeSideDir;
        DicUserSwingData["GetElbowSideDir"][step] = sensorProcess.iGetElbowSideDir;
        DicUserSwingData["GetArmpitDir"][step] = sensorProcess.iGetArmpitDir;
        DicUserSwingData["GetHandSideDistance"][step] = sensorProcess.iGetHandSideDistance;
        DicUserSwingData["GetGripDistance"][step] = sensorProcess.iGetGripDistance;
        DicUserSwingData["GetShoulderSideDirWorld"][step] = sensorProcess.iGetShoulderSideDirWorld;
        DicUserSwingData["GetPelvisSideDirWorld"][step] = sensorProcess.iGetPelvisSideDirWorld;
        DicUserSwingData["GetNoseShoulderSideDir"][step] = sensorProcess.iGetNoseShoulderSideDir;
        DicUserSwingData["GetNosePelvisSideDir"][step] = sensorProcess.iGetNosePelvisSideDir;

        // Combine
        DicUserSwingData["GetShoulderDir"][step] = sensorProcess.iGetShoulderDir;
        DicUserSwingData["GetPelvisDir"][step] = sensorProcess.iGetPelvisDir;
        DicUserSwingData["GetHandCombineDir"][step] = sensorProcess.iGetHandCombineDir;

        string[] selectedKeys;

        switch (step)
        {
            case 0: selectedKeys = new[] { "GetShoulderAngle", "GetWaistSideDir", "GetKneeSideDir" }; break;                 // ADDRESS
            case 1: selectedKeys = new[] { "GetForearmAngle", "GetShoulderAngle" }; break;                                  // TAKEBACK
            case 2: selectedKeys = new[] { "GetShoulderDir", "GetPelvisDir", "GetForearmAngle", "GetWeight" }; break;       // BACKSWING
            case 3: selectedKeys = new[] { "GetShoulderDir", "GetPelvisDir", "GetForearmAngle", "GetWeight" }; break;       // TOP
            case 4: selectedKeys = new[] { "GetShoulderDir", "GetHandSideDir", "GetSpineDir", "GetWeight" }; break;         // DOWNSWING
            case 5: selectedKeys = new[] { "GetShoulderDir", "GetPelvisDir" }; break;                                       // IMPACT
            case 6: selectedKeys = new[] { "GetPelvisDir", "GetShoulderDir" }; break;                                       // FOLLOW
            case 7: selectedKeys = new[] { "GetPelvisDir", "GetShoulderDir" }; break;                                       // FINISH
            default: selectedKeys = new string[] { }; break;
        }

        const int IDX_DOWNSWING = 4;
        const int IDX_FOLLOW = 6;
        const int IDX_FINISH = 7;

        int stepScore = 0;

        if (step == IDX_FINISH)
        {
            int sum = 0;
            int count = 0;

            for (int s = IDX_DOWNSWING; s <= IDX_FOLLOW; s++)
            {
                int v = myScore[s];
                if (v >= 0)
                {
                    sum += v;
                    count++;
                }
            }

            if (count == 0)
            {
                for (int s = 0; s <= IDX_FOLLOW; s++)
                {
                    int v = myScore[s];
                    if (v >= 0)
                    {
                        sum += v;
                        count++;
                    }
                }
            }

            stepScore = (count > 0) ? Mathf.RoundToInt((float)sum / count) : 0;
        }
        else
        {
            stepScore = GetUserStepAverage(step, selectedKeys);
        }

        myScore[step] = stepScore;

        AppendStepTimelineScore(step, stepScore);
    }

    private void AppendStepTimelineScore(int step, int stepScore)
    {
        List<int> list = GetTimelineForStep(step);
        if (list == null)
            return;
        list.Add(stepScore);
        while (list.Count > timelineHistoryMax)
            list.RemoveAt(0);
    }

    private List<int> GetTimelineForStep(int step)
    {
        switch (step)
        {
            case 0: return addressTimeline;
            case 1: return takebackTimeline;
            case 2: return backswingTimeline;
            case 3: return topTimeline;
            case 4: return downswingTimeline;
            case 5: return impactTimeline;
            case 6: return followTimeline;
            case 7: return finishTimeline;
            default: return null;
        }
    }

    private int GetUserStepAverage(int step, params string[] selectedKeys)
    {
        Dictionary<string, int> proDic = GetProStepDic(step);

        if (proDic == null || selectedKeys == null || selectedKeys.Length == 0)
        {
            return 0;
        }

        float total = 0;
        int count = 0;

        foreach (string key in selectedKeys)
        {
            if (!DicUserSwingData.ContainsKey(key) || !proDic.ContainsKey(key))
            {
                continue;
            }

            float proValue = proDic[key];
            float userValue = DicUserSwingData[key][step];
            float tolerance = ErrorMargins.ContainsKey(key) ? ErrorMargins[key] : 1f;

            float diff = Mathf.Abs(userValue - proValue);
            float score;

            float angleThreshold = 5f;

            if (diff <= angleThreshold)
            {
                score = 100f;
            }
            else
            {
                float normalized = Mathf.Clamp01((diff - angleThreshold) / (tolerance - angleThreshold));
                score = 100f - Mathf.Pow(normalized, 0.5f) * 100.0f;
            }

            score = Mathf.Clamp(score, 40f, 100f);

            if (Mathf.Approximately(score, 40f))
            {
                score += UnityEngine.Random.Range(-5, 10);
                score = Mathf.Clamp(score, 35f, 50f);
            }

            total += score;
            count++;
        }

        int stepScore = (count > 0) ? Mathf.RoundToInt(total / count) : 0;

        if (stepScore == 0 && step > 0)
        {
            float prevTotal = 0;
            int prevCount = 0;
            for (int s = 0; s < step; s++)
            {
                prevTotal += myScore[s];
                prevCount++;
            }
            stepScore = (prevCount > 0) ? Mathf.RoundToInt(prevTotal / prevCount) : 0;
        }

        return stepScore;
    }

    private Dictionary<string, int> GetProStepDic(int step)
    {
        switch (step)
        {
            case 0: return aiSwingStepData.dicAddress;
            case 1: return aiSwingStepData.dicTakeback;
            case 2: return aiSwingStepData.dicBackswing;
            case 3: return aiSwingStepData.dicTop;
            case 4: return aiSwingStepData.dicDownswing;
            case 5: return aiSwingStepData.dicImpact;
            case 6: return aiSwingStepData.dicFollow;
            case 7: return aiSwingStepData.dicFinish;
            default: return null;
        }
    }

    private void EnsurePoseLM(int pixelWidth, int pixelHeight, bool forVideoMode)
    {
        if (_offlinePoseLM == null || _poseLMForVideo != forVideoMode)
        {
            try
            {
                if (_offlinePoseLM != null)
                {
                    _offlinePoseLM.Close();
                }
            }
            catch { }

            _offlinePoseLM = null;

            TextAsset modelBytes = Resources.Load<TextAsset>("pose_landmarker_full");

            if (modelBytes == null)
            {
                return;
            }

            Mediapipe.Tasks.Core.BaseOptions baseOptions =
                new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: modelBytes.bytes
                );

            Mediapipe.Tasks.Vision.Core.RunningMode runningMode =
                forVideoMode
                    ? Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO
                    : Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;

            PoseLandmarkerOptions poseOptions =
                new PoseLandmarkerOptions(
                    baseOptions: baseOptions,
                    runningMode: runningMode
                );

            _offlinePoseLM = PoseLandmarker.CreateFromOptions(poseOptions);
            _poseLMForVideo = forVideoMode;
        }

        if (_tfAnalyzer == null || _tfAnalyzer.width != pixelWidth || _tfAnalyzer.height != pixelHeight)
        {
            try
            {
                if (_tfAnalyzer != null)
                {
                    _tfAnalyzer.Dispose();
                }
            }
            catch { }

            _tfAnalyzer = new TextureFrame(pixelWidth, pixelHeight, TextureFormat.RGBA32);
        }
    }

    private bool TryDetectPose(Texture2D inputTexture, out NormalizedLandmarks normalizedLandmarks, out Landmarks worldLandmarks)
    {
        normalizedLandmarks = default;
        worldLandmarks = default;

        if (inputTexture == null || inputTexture.width <= 0 || inputTexture.height <= 0)
        {
            return false;
        }

        EnsurePoseLM(inputTexture.width, inputTexture.height, false);

        _tfAnalyzer.ReadTextureOnCPU(inputTexture, flipHorizontally: false, flipVertically: false);

        using (Mediapipe.Image cpuImage = _tfAnalyzer.BuildCPUImage())
        {
            PoseLandmarkerResult result = _offlinePoseLM.Detect(cpuImage);

            bool hasNormalizedLandmarks = result.poseLandmarks != null && result.poseLandmarks.Count > 0;
            bool hasWorldLandmarks = result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0;

            if (!hasNormalizedLandmarks && !hasWorldLandmarks)
            {
                return false;
            }

            if (hasNormalizedLandmarks)
            {
                normalizedLandmarks = result.poseLandmarks[0];
            }

            if (hasWorldLandmarks)
            {
                worldLandmarks = result.poseWorldLandmarks[0];
            }

            return true;
        }
    }

    private Landmark2D[] ConvertToLandmark2DArray(NormalizedLandmarks normalized, int pixelWidth, int pixelHeight)
    {
        if (normalized.landmarks == null || normalized.landmarks.Count == 0)
            return null;

        Landmark2D[] landmarks = new Landmark2D[33];

        screenRect = rawImageFront.rectTransform.rect;

        for (int i = 0; i < 33; i++)
        {
            landmarks[i] = new Landmark2D();

            float nx = (float)normalized.landmarks[i].x;
            float ny = (float)normalized.landmarks[i].y;
            float visibility = (float)normalized.landmarks[i].visibility;

            landmarks[i].positionOrg = new Vector2(nx, ny);
            landmarks[i].position = screenRect.GetPoint(normalized.landmarks[i]);
            landmarks[i].visibility = visibility;
        }

        return landmarks;
    }

    private Landmark3D[] ConvertToLandmark3DArray(Landmarks world)
    {
        if (world.landmarks == null || world.landmarks.Count == 0)
            return null;

        Landmark3D[] landmarks = new Landmark3D[33];

        for (int i = 0; i < 33; i++)
        {
            landmarks[i] = new Landmark3D();

            float wx = (float)world.landmarks[i].x;
            float wy = (float)world.landmarks[i].y;
            float wz = (float)world.landmarks[i].z;
            float visibility = (float)world.landmarks[i].visibility;

            Vector3 mapped = new Vector3(-wy, -wx, wz);

            landmarks[i].position = mapped;
            landmarks[i].visibility = visibility;
        }

        return landmarks;
    }

    private void ExtractLandmarks(Texture2D frontTexture, Texture2D sideTexture,
        out Landmark2D[] front2DLandmarks, out Landmark2D[] side2DLandmarks,
        out Landmark3D[] front3DLandmarks, out Landmark3D[] side3DLandmarks)
    {
        front2DLandmarks = null;
        side2DLandmarks = null;
        front3DLandmarks = null;
        side3DLandmarks = null;

        if (frontTexture != null)
        {
            NormalizedLandmarks frontNormalizedLandmarks;
            Landmarks frontWorldLandmarks;

            if (TryDetectPose(frontTexture, out frontNormalizedLandmarks, out frontWorldLandmarks))
            {
                if (frontNormalizedLandmarks.landmarks != null && frontNormalizedLandmarks.landmarks.Count > 0)
                {
                    front2DLandmarks = ConvertToLandmark2DArray(frontNormalizedLandmarks, frontTexture.width, frontTexture.height);
                }

                if (frontWorldLandmarks.landmarks != null && frontWorldLandmarks.landmarks.Count > 0)
                {
                    front3DLandmarks = ConvertToLandmark3DArray(frontWorldLandmarks);
                }
            }
        }

        if (sideTexture != null)
        {
            NormalizedLandmarks sideNormalizedLandmarks;
            Landmarks sideWorldLandmarks;

            if (TryDetectPose(sideTexture, out sideNormalizedLandmarks, out sideWorldLandmarks))
            {
                if (sideNormalizedLandmarks.landmarks != null && sideNormalizedLandmarks.landmarks.Count > 0)
                {
                    side2DLandmarks = ConvertToLandmark2DArray(sideNormalizedLandmarks, sideTexture.width, sideTexture.height);
                }

                if (sideWorldLandmarks.landmarks != null && sideWorldLandmarks.landmarks.Count > 0)
                {
                    side3DLandmarks = ConvertToLandmark3DArray(sideWorldLandmarks);
                }
            }
        }
    }

    private bool ImportRgbStreamBytes(byte[] data, int width, int height, List<byte[]> outFrames)
    {
        if (outFrames == null) return false;
        outFrames.Clear();

        if (data == null || data.Length == 0)
            return false;

        int bytesPerFrame = width * height * 3;
        if (bytesPerFrame <= 0 || data.Length < bytesPerFrame)
            return false;

        int frameCount = data.Length / bytesPerFrame;
        if (frameCount <= 0)
            return false;

        int offset = 0;
        for (int i = 0; i < frameCount; i++)
        {
            byte[] buf = new byte[bytesPerFrame];
            Buffer.BlockCopy(data, offset, buf, 0, bytesPerFrame);
            offset += bytesPerFrame;

            outFrames.Add(buf);
        }

        return outFrames.Count > 0;
    }

    private void TryImportFramesFromExternalRgb()
    {
        if (_importedExternalRgbFrames) return;
        if (!useExternalRgbAnalyze) return;

        if (externalFrontRgbTextAssets == null || externalFrontRgbTextAssets.Length == 0)
        {
            return;
        }

        if (framesFront == null)
            framesFront = new List<byte[]>(1024);

        if (framesSide == null)
            framesSide = new List<byte[]>(1024);

        int fi = Mathf.Clamp(_externalRgbTextAssetIndex, 0, externalFrontRgbTextAssets.Length - 1);
        TextAsset frontAsset = externalFrontRgbTextAssets[fi];

        if (frontAsset == null || frontAsset.bytes == null || frontAsset.bytes.Length == 0)
        {
            return;
        }

        bool okFront = ImportRgbStreamBytes(frontAsset.bytes, externalRgbWidth, externalRgbHeight, framesFront);
        if (!okFront)
        {
            return;
        }

        bool okSide = false;
        TextAsset sideAsset = null;

        if (externalSideRgbTextAssets != null && externalSideRgbTextAssets.Length > 0)
        {
            int si = Mathf.Clamp(_externalRgbTextAssetIndex, 0, externalSideRgbTextAssets.Length - 1);
            sideAsset = externalSideRgbTextAssets[si];

            if (sideAsset != null && sideAsset.bytes != null && sideAsset.bytes.Length > 0)
                okSide = ImportRgbStreamBytes(sideAsset.bytes, externalRgbWidth, externalRgbHeight, framesSide);
        }

        if (!okSide)
        {
            framesSide.Clear();
            for (int i = 0; i < framesFront.Count; i++)
                framesSide.Add(null);
        }
        else
        {
            int min = Mathf.Min(framesFront.Count, framesSide.Count);
            if (framesFront.Count != min) framesFront.RemoveRange(min, framesFront.Count - min);
            if (framesSide.Count != min) framesSide.RemoveRange(min, framesSide.Count - min);
        }

        widthFront = externalRgbWidth;
        heightFront = externalRgbHeight;
        widthSide = externalRgbWidth;
        heightSide = externalRgbHeight;

        _importedExternalRgbFrames = true;
    }

    private IEnumerator AnalyzeSwingFromAllFrames()
    {
        TryImportFramesFromExternalRgb();
        TryImportFramesFromRecordingProfile();

        if (useExternalRgbAnalyze && !_importedExternalRgbFrames)
        {
            yield break;
        }

        if (useRecordingProfileFrames && !_importedRecordingProfileFrames)
        {
            yield break;
        }

        int frameCount = (framesFront != null) ? framesFront.Count : 0;

        if (frameCount <= 0)
        {
            yield break;
        }

        analyzedFrameSnapshots.Clear();

        for (int i = 0; i < frameCount; i++)
        {
            analyzedFrameSnapshots.Add(default(FrameSensorSnapshot));
        }

        int validFront = 0;

        for (int i = 0; i < framesFront.Count; i++)
        {
            if (framesFront[i] != null && framesFront[i].Length > 0) validFront++;
        }
        Debug.Log($"[Analyze] frameCount={framesFront.Count}, validFront={validFront}, captureDone={_captureDone}");

        float[] proTargets = BuildProHandDirTargets();

        if (debugAnalyzeLog)
        {
            ResetHandDirCompareLog();
            EnsureDebugFrameDir();
        }

        const int BUDGET_MS = 8;
        System.Diagnostics.Stopwatch budgetSw = new System.Diagnostics.Stopwatch();
        budgetSw.Start();

        List<int> handNF = new List<int>(frameCount);
        List<int> shoulderNF = new List<int>(frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            long stepStartMs = budgetSw.ElapsedMilliseconds;

            byte[] frontBytes = (framesFront != null && i >= 0 && i < framesFront.Count) ? framesFront[i] : null;
            byte[] sideBytes = (framesSide != null && i >= 0 && i < framesSide.Count) ? framesSide[i] : null;

            Texture2D frontRaw = (frontBytes != null) ? CreateTextureFromRaw(frontBytes, widthFront, heightFront) : null;
            Texture2D sideRaw = (sideBytes != null) ? CreateTextureFromRaw(sideBytes, widthSide, heightSide) : null;

            int rawHand = -1;
            int kalHand = -1;
            int rawShoulder = -1;

            try
            {
                if (frontRaw != null)
                {
                    Texture2D frontTex = ApplyAnalyzeTransform(frontRaw);
                    Texture2D sideTex = (sideRaw != null) ? ApplyAnalyzeTransform(sideRaw) : null;

                    Landmark2D[] f2D;
                    Landmark2D[] s2D;
                    Landmark3D[] f3D;
                    Landmark3D[] s3D;

                    ExtractLandmarks(frontTex, sideTex, out f2D, out s2D, out f3D, out s3D);

                    if (f2D != null && f3D != null)
                    {
                        sensorProcess.UpdateSensor(f2D, s2D, f3D, s3D);

                        FrameSensorSnapshot snapshot = CaptureFrameSensorSnapshot();
                        analyzedFrameSnapshots[i] = snapshot;

                        rawHand = snapshot.handDirNF;
                        rawShoulder = snapshot.shoulderDir;

                        // rawHand = sensorProcess.iGetHandDirNF;
                        // rawShoulder = sensorProcess.iGetShoulderDir;
                    }

                    if (debugAnalyzeLog)
                    {
                        string fPath = SaveFrameJpg(frontTex, $"F_{i:0000}.jpg");
                        string sPath = sideTex != null ? SaveFrameJpg(sideTex, $"S_{i:0000}.jpg") : "";

                        _handDirLogSb
                            .Append(i).Append(',')
                            .Append(rawHand).Append(',')
                            .Append(kalHand).Append(',')
                            .AppendLine();
                    }

                    if (frontTex != null) Destroy(frontTex);
                    if (sideTex != null) Destroy(sideTex);
                }
            }
            finally
            {
                if (frontRaw != null) Destroy(frontRaw);
                if (sideRaw != null) Destroy(sideRaw);
            }

            handNF.Add(rawHand);
            shoulderNF.Add(rawShoulder);

            if (budgetSw.ElapsedMilliseconds - stepStartMs >= BUDGET_MS)
                yield return null;
        }

        int[] stepIndex = new int[8];

        for (int i = 0; i < 8; i++)
            stepIndex[i] = -1;

        bool IsCrossing(int a, int b, int target)
        {
            if (Mathf.Abs(a - b) > 180)
                return false;

            if (a < 0 || b < 0)
                return false;

            if (a == target)
                return true;

            if (a > b)
                return (target <= a && target >= b);

            if (a < b)
                return (target >= a && target <= b);

            return false;
        }

        int FindCrossingBestInRange(List<int> seq, int startIndex, int target, int rangeMin, int rangeMax)
        {
            int count = seq.Count;

            if (count <= 0)
                return -1;

            int start = Mathf.Clamp(startIndex, 0, count - 1);

            if (start >= count - 1)
                return start;

            for (int i = start; i < count - 1; i++)
            {
                int a = seq[i];
                int b = seq[i + 1];

                if (a < 0 || b < 0)
                    continue;

                if (Mathf.Abs(a - b) > 180)
                    continue;

                if (!IsCrossing(a, b, target))
                    continue;

                bool inA = (a >= rangeMin && a <= rangeMax);
                bool inB = (b >= rangeMin && b <= rangeMax);

                if (!inA && !inB)
                    continue;

                int diffA = Mathf.Abs(a - target);
                int diffB = Mathf.Abs(b - target);

                return (diffB < diffA) ? (i + 1) : i;
            }

            return -1;
        }

        int R0(float r) => Mathf.Clamp(Mathf.FloorToInt((frameCount - 1) * r), 0, frameCount - 1);
        int R1(float r) => Mathf.Clamp(Mathf.CeilToInt((frameCount - 1) * r), 0, frameCount - 1);

        (int, int) W_Address = (R0(0.00f), R1(0.15f));
        (int, int) W_Takeback = (R0(0.08f), R1(0.45f));
        (int, int) W_Backswing = (R0(0.18f), R1(0.55f));
        (int, int) W_Downswing = (R0(0.30f), R1(0.72f));
        (int, int) W_Follow = (R0(0.36f), R1(0.86f));
        (int, int) W_Finish = (R0(0.75f), R1(1.00f));

        int FindLocalMinInRange(List<int> seq, int start, int end, int maxAbsValue = 360)
        {
            if (seq == null || seq.Count == 0) return -1;

            start = Mathf.Clamp(start, 0, seq.Count - 1);
            end = Mathf.Clamp(end, 0, seq.Count - 1);

            if (start > end)
                return -1;

            int bestIdx = -1;
            int bestVal = int.MaxValue;

            for (int i = start; i <= end; i++)
            {
                int v = seq[i];

                if (v < 0)
                    continue;

                if (v > maxAbsValue)
                    continue;

                if (v < bestVal)
                {
                    bestVal = v;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        int FindPrevInRange(List<int> seq, int start, int end, int target)
        {
            if (seq == null || seq.Count <= 0)
                return -1;

            start = Mathf.Clamp(start, 0, seq.Count - 1);
            end = Mathf.Clamp(end, 0, seq.Count - 1);

            if (start > end)
                return -1;

            for (int i = start; i < seq.Count - 1 && i <= end - 1; i++)
            {
                int a = seq[i];
                int b = seq[i + 1];
                if (IsCrossing(a, b, target))
                    return i;
            }

            return -1;
        }

        int FindNextInRange(List<int> seq, int start, int end, int target)
        {
            if (seq == null || seq.Count <= 0)
                return -1;

            start = Mathf.Clamp(start, 0, seq.Count - 1);
            end = Mathf.Clamp(end, 0, seq.Count - 1);

            if (start > end)
                return -1;

            for (int i = start + 1; i < seq.Count && i <= end; i++)
            {
                int a = seq[i - 1];
                int b = seq[i];

                if (a < 0 || b < 0)
                    continue;

                if (Mathf.Abs(a - b) > 180)
                    continue;

                int min = Mathf.Min(a, b);
                int max = Mathf.Max(a, b);

                if (target >= min && target <= max)
                    return i;
            }
            return -1;
        }

        int FindBestInRange(List<int> seq, int start, int end, int target, int rangeMin, int rangeMax)
        {
            if (seq == null || seq.Count <= 0)
                return -1;

            start = Mathf.Clamp(start, 0, seq.Count - 1);
            end = Mathf.Clamp(end, 0, seq.Count - 1);

            if (start > end)
                return -1;

            for (int i = start; i < seq.Count - 1 && i <= end - 1; i++)
            {
                int a = seq[i];
                int b = seq[i + 1];

                if (a < 0 || b < 0)
                    continue;

                if (Mathf.Abs(a - b) > 180)
                    continue;

                if (!IsCrossing(a, b, target))
                    continue;

                bool inA = (a >= rangeMin && a <= rangeMax);
                bool inB = (b >= rangeMin && b <= rangeMax);

                if (!inA && !inB)
                    continue;

                int diffA = Mathf.Abs(a - target);
                int diffB = Mathf.Abs(b - target);

                return (diffB < diffA) ? (i + 1) : i;
            }
            return -1;
        }

        int FindPeakDeltaIndex(List<int> seq, int start, int end)
        {
            if (seq == null || seq.Count <= 0)
                return -1;

            start = Mathf.Clamp(start, 0, seq.Count - 2);
            end = Mathf.Clamp(end, 1, seq.Count - 1);

            if (start >= end)
                return -1;

            int bestIdx = -1;
            int bestAbsDelta = -1;

            for (int i = start; i <= end - 1; i++)
            {
                int a = seq[i];
                int b = seq[i + 1];

                if (a < 0 || b < 0)
                    continue;

                if (Mathf.Abs(a - b) > 180)
                    continue;

                int d = b - a;
                int ad = Mathf.Abs(d);

                if (ad > bestAbsDelta)
                {
                    bestAbsDelta = ad;
                    bestIdx = i + 1;
                }
            }

            return bestIdx;
        }

        int FindClosestInRange(List<int> seq, int start, int end, int target, int maxAbsDiff, bool preferEarly)
        {
            if (seq == null || seq.Count <= 0)
                return -1;

            start = Mathf.Clamp(start, 0, seq.Count - 1);
            end = Mathf.Clamp(end, 0, seq.Count - 1);

            if (start > end)
                return -1;

            int bestIdx = -1;
            int bestDiff = int.MaxValue;

            for (int i = start; i <= end && i < seq.Count; i++)
            {
                int v = seq[i];

                if (v < 0)
                    continue;

                int diff = Mathf.Abs(v - target);

                if (diff > maxAbsDiff)
                    continue;

                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestIdx = i;

                    if (preferEarly && bestDiff <= 3)
                        return bestIdx;
                }
            }

            return bestIdx;
        }

        int cur = 0;

        // ADDRESS
        {
            int takeT = Mathf.RoundToInt(proTargets[(int)SWINGSTEP.TAKEBACK]);
            int minAddr = takeT + 10;

            int idx = -1;

            for (int i = 0; i < frameCount; i++)
            {
                int v = handNF[i];
                if (v < 0) continue;

                if (v >= minAddr)
                {
                    idx = i;
                    break;
                }
            }

            if (idx < 0)
            {
                for (int i = 0; i < frameCount; i++)
                {
                    if (handNF[i] >= 0) { idx = i; break; }
                }
            }

            if (idx < 0)
                idx = 0;

            stepIndex[0] = idx;
            cur = idx + 1;
        }

        // TAKEBACK
        {
            int t = Mathf.RoundToInt(proTargets[1]);
            int start = Mathf.Max(cur, W_Takeback.Item1);
            int end = W_Takeback.Item2;

            int idx = -1;

            idx = FindPrevInRange(handNF, start, end, t);

            if (idx < 0)
                idx = FindClosestInRange(handNF, start, end, t, maxAbsDiff: 40, preferEarly: true);

            if (idx < 0)
                idx = FindLocalMinInRange(handNF, start, end);

            if (idx < 0)
            {
                for (int i = start; i <= end && i < handNF.Count; i++)
                {
                    if (handNF[i] >= 0) { idx = i; break; }
                }
            }

            if (idx >= 0)
            {
                stepIndex[1] = idx;
                cur = idx + 1;
            }
        }

        // BACKSWING
        {
            int t = Mathf.RoundToInt(proTargets[2]);
            int start = Mathf.Max(cur, W_Backswing.Item1);
            int end = W_Backswing.Item2;

            int idx = FindPrevInRange(handNF, start, end, t);

            if (idx >= 0)
            {
                stepIndex[2] = idx;
                cur = idx + 3;
            }
        }

        // TOP = BACKSWING
        if (stepIndex[2] >= 0) stepIndex[3] = stepIndex[2];

        // DOWNSWING
        {
            int t = Mathf.RoundToInt(proTargets[4]);

            int start = Mathf.Max(cur, W_Downswing.Item1);
            int end = W_Downswing.Item2;

            int downRangeMin = 100;
            int downRangeMax = 160;
            int effMin = Mathf.Min(downRangeMin, t);
            int effMax = Mathf.Max(downRangeMax, t);

            int idx = FindBestInRange(handNF, start, end, t, effMin, effMax);

            if (idx < 0)
            {
                int bestIdx = -1;
                int bestDiff = int.MaxValue;

                for (int i = start; i <= end && i < handNF.Count; i++)
                {
                    int v = handNF[i];

                    if (v < 0)
                        continue;

                    if (v < effMin || v > effMax)
                        continue;

                    int diff = Mathf.Abs(v - t);

                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestIdx = i;
                    }
                }

                if (bestIdx >= 0)
                    idx = bestIdx;
            }

            if (idx < 0)
            {
                idx = FindPeakDeltaIndex(handNF, start, end);
            }

            if (idx >= 0)
            {
                stepIndex[4] = idx;
                cur = idx + 1;
            }
        }

        // IMPACT = DOWNSWING
        if (stepIndex[4] >= 0) stepIndex[5] = stepIndex[4];

        // FOLLOW
        {
            int t = Mathf.RoundToInt(proTargets[6]);

            int baseStart = Mathf.Max(cur, W_Follow.Item1);
            int baseEnd = W_Follow.Item2;

            int ds = stepIndex[4];
            if (ds >= 0)
            {
                int dynStart = Mathf.Max(baseStart, ds + 1);

                int span = Mathf.Max(6, Mathf.RoundToInt(frameCount * 0.20f));
                int dynEnd = Mathf.Min(baseEnd, ds + span);

                int idx = FindNextInRange(handNF, dynStart, dynEnd, t);

                if (idx < 0)
                    idx = FindNextInRange(handNF, baseStart, baseEnd, t);

                if (idx < 0)
                {
                    int bestIdx = -1;
                    int bestDiff = int.MaxValue;

                    for (int i = baseStart; i <= baseEnd && i < handNF.Count; i++)
                    {
                        int v = handNF[i];

                        if (v < 0)
                            continue;

                        int diff = Mathf.Abs(v - t);

                        if (diff < bestDiff)
                        {
                            bestDiff = diff;
                            bestIdx = i;
                        }
                    }

                    idx = bestIdx;
                }

                if (idx >= 0)
                {
                    stepIndex[6] = idx;
                    cur = idx + 1;
                }
            }
            else
            {
                int idx = FindNextInRange(handNF, baseStart, baseEnd, t);

                if (idx >= 0)
                {
                    stepIndex[6] = idx;
                    cur = idx + 1;
                }
            }
        }

        // FINISH
        {
            // int t = Mathf.RoundToInt(proTargets[7]);

            // int start = Mathf.Max(cur, W_Finish.Item1);
            // int end = W_Finish.Item2;

            // int idx = FindPrevInRange(handNF, start, end, t);
            // stepIndex[7] = (idx >= 0) ? idx : end;

            stepIndex[7] = frameCount - 1;
        }

        FillStepGaps(stepIndex, frameCount);

        if (debugAnalyzeLog)
        {
            _handDirLogSb.AppendLine();
            _handDirLogSb.AppendLine("step,stepName,chosenFrame,target,chosen,diff");

            for (int s = 0; s < 8; s++)
            {
                string stepName = ((SWINGSTEP)s).ToString();
                int idx = stepIndex[s];
                int t = Mathf.RoundToInt(proTargets[s]);

                if (idx < 0 || idx >= handNF.Count)
                {
                    _handDirLogSb
                        .Append(s).Append(',')
                        .Append(stepName).Append(',')
                        .Append(-1).Append(',')
                        .Append(t).Append(',')
                        .Append(-1).Append(',')
                        .Append(-1)
                        .AppendLine();
                    continue;
                }

                int chosen = handNF[idx];
                int diff = (chosen >= 0) ? Mathf.Abs(chosen - t) : -1;

                _handDirLogSb
                    .Append(s).Append(',')
                    .Append(stepName).Append(',')
                    .Append(idx).Append(',')
                    .Append(t).Append(',')
                    .Append(chosen).Append(',')
                    .Append(diff)
                    .AppendLine();
            }

            SaveHandDirCompareCsv();
        }

        // 점수적용
        SetResultData();

        myScore = Enumerable.Repeat(-1, 8).ToList();

        for (int s = 0; s < 8; s++)
        {
            long stepStartMs = budgetSw.ElapsedMilliseconds;

            int idx = stepIndex[s];
            if (idx < 0 || idx >= frameCount)
                continue;

            if (idx >= analyzedFrameSnapshots.Count)
                continue;

            FrameSensorSnapshot snapshot = analyzedFrameSnapshots[idx];
            if (!snapshot.isValid)
                continue;

            UserDataAddFromSnapshot(s, snapshot);

            if (budgetSw.ElapsedMilliseconds - stepStartMs >= BUDGET_MS)
                yield return null;
        }

        if (debugScoreCsv)
            SaveScoreDebugCsv(stepIndex);

        // 썸네일저장
        for (int s = 0; s < 8; s++)
        {
            if (stepIndex[s] >= 0)
            {
                SaveStepFromFrame(s, stepIndex[s]);
            }
        }

        analyzedProFrameSnapshots.Clear();
        matchedUserFrameIndicesForTotal.Clear();
        totalFrameScores.Clear();
        totalAnalyzeScore = 0;

        if (LoadProAICsv(analyzedProFrameSnapshots))
        {
            matchedUserFrameIndicesForTotal = MatchUserFramesByProHand(analyzedProFrameSnapshots, analyzedFrameSnapshots, 40);

            totalAnalyzeScore = CalculateTotalAnalyzeScore(analyzedProFrameSnapshots, analyzedFrameSnapshots, matchedUserFrameIndicesForTotal);

            Debug.Log($"[AICoaching][Total] totalAnalyzeScore = {totalAnalyzeScore}, proFrames = {analyzedProFrameSnapshots.Count}, userFrames = {analyzedFrameSnapshots.Count}, matched = {totalFrameScores.Count}");
        }
        else
        {
            Debug.LogWarning("[AICoaching][Total] Pro AI full-frame csv load failed.");
        }

        Debug.Log("[AnalyzeSwingFromAllFrames] Done (Score Applied).");
    }


    private void TryImportFramesFromRecordingProfile()
    {
        if (_importedRecordingProfileFrames)
            return;

        if (!useRecordingProfileFrames)
            return;

        if (!ProfileVerifyBuffer.HasData)
        {
            Debug.Log("[AICoaching] ProfileVerifyBuffer has no data.");
            return;
        }

        ProfileVerifyBuffer.Export(
            out List<ProfileVerifyBuffer.RawFrame> front,
            out List<ProfileVerifyBuffer.RawFrame> side
        );

        if (front == null || front.Count <= 0)
        {
            Debug.Log("[AICoaching] Import failed. front is empty.");
            return;
        }

        int count = front.Count;

        if (framesFront == null)
            framesFront = new List<byte[]>(count);

        if (framesSide == null)
            framesSide = new List<byte[]>(count);

        framesFront.Clear();
        framesSide.Clear();

        for (int i = 0; i < count; i++)
        {
            ProfileVerifyBuffer.RawFrame f = front[i];

            if (f.rgb24 == null || f.rgb24.Length == 0 || f.width <= 0 || f.height <= 0)
            {
                framesFront.Add(null);
            }
            else
            {
                framesFront.Add(f.rgb24);
            }

            if (side != null && i < side.Count)
            {
                ProfileVerifyBuffer.RawFrame s = side[i];

                if (s.rgb24 == null || s.rgb24.Length == 0 || s.width <= 0 || s.height <= 0)
                {
                    framesSide.Add(null);
                }
                else
                {
                    framesSide.Add(s.rgb24);
                }
            }
            else
            {
                framesSide.Add(null);
            }
        }

        for (int i = 0; i < front.Count; i++)
        {
            if (front[i].rgb24 != null && front[i].rgb24.Length > 0)
            {
                widthFront = front[i].width;
                heightFront = front[i].height;
                break;
            }
        }

        if (side != null)
        {
            for (int i = 0; i < side.Count; i++)
            {
                if (side[i].rgb24 != null && side[i].rgb24.Length > 0)
                {
                    widthSide = side[i].width;
                    heightSide = side[i].height;
                    break;
                }
            }
        }

        _importedRecordingProfileFrames = true;

        ProfileVerifyBuffer.Clear();

        Debug.Log($"[AICoaching] Imported Recording(Profile) frames = {framesFront.Count}");
    }

    // private void EnsureCaptureRT(ref RenderTexture rt, Texture src)
    // {
    //     if (src == null)
    //         return;

    //     int w = src.width;
    //     int h = src.height;

    //     if (rt != null && (rt.width != w || rt.height != h))
    //     {
    //         rt.Release();
    //         Destroy(rt);
    //         rt = null;
    //     }

    //     if (rt == null)
    //     {
    //         rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
    //         rt.wrapMode = TextureWrapMode.Clamp;
    //         rt.filterMode = FilterMode.Bilinear;
    //         rt.Create();
    //     }
    // }

    // private bool TryGetRawFrame(List<RawFrame> list, int index, out RawFrame frame)
    // {
    //     frame = default(RawFrame);

    //     if (list == null)
    //         return false;

    //     if (index < 0 || index >= list.Count)
    //         return false;

    //     if (list[index].rgb == null || list[index].rgb.Length == 0)
    //         return false;

    //     frame = list[index];

    //     return true;
    // }

    private Texture2D CreateTextureFromRaw(byte[] rgb, int width, int height)
    {
        if (rgb == null || rgb.Length == 0 || width <= 0 || height <= 0)
        {
            return null;
        }

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.LoadRawTextureData(rgb);
        tex.Apply(false, false);

        return tex;
    }

    private void ClearUserStepTextures()
    {
        if (captureRealPoseFrontUser != null)
        {
            for (int i = 0; i < captureRealPoseFrontUser.Count; i++)
            {
                if (captureRealPoseFrontUser[i] != null) Destroy(captureRealPoseFrontUser[i]);
            }
        }

        if (captureRealPoseSideUser != null)
        {
            for (int i = 0; i < captureRealPoseSideUser.Count; i++)
            {
                if (captureRealPoseSideUser[i] != null) Destroy(captureRealPoseSideUser[i]);
            }
        }
    }

    private void SaveScoreDebugCsv(int[] stepIndex)
    {
        if (debugAnalyzeLog == false && debugScoreCsv == false)
        {
            return;
        }

        if (sensorProcess == null)
        {
            return;
        }

        if (swingStepData == null)
        {
            return;
        }

        if (aiSwingStepData == null)
        {
            return;
        }

        if (framesFront == null || framesFront.Count <= 0)
        {
            return;
        }

        if (stepIndex == null || stepIndex.Length < 8)
        {
            return;
        }

        int GetSwingDataValueLocal(string key, SWINGSTEP step)
        {
            try
            {
                if (step == SWINGSTEP.ADDRESS) return aiSwingStepData.dicAddress[key];
                if (step == SWINGSTEP.TAKEBACK) return aiSwingStepData.dicTakeback[key];
                if (step == SWINGSTEP.BACKSWING) return aiSwingStepData.dicBackswing[key];
                if (step == SWINGSTEP.TOP) return aiSwingStepData.dicTop[key];
                if (step == SWINGSTEP.DOWNSWING) return aiSwingStepData.dicDownswing[key];
                if (step == SWINGSTEP.IMPACT) return aiSwingStepData.dicImpact[key];
                if (step == SWINGSTEP.FOLLOW) return aiSwingStepData.dicFollow[key];
                if (step == SWINGSTEP.FINISH) return aiSwingStepData.dicFinish[key];
            }
            catch { }

            return -1;
        }

        string GetShoulderKeyByStepLocal(SWINGSTEP step)
        {
            if (step == SWINGSTEP.ADDRESS || step == SWINGSTEP.IMPACT)
                return "GetShoulderAngle";

            if (step == SWINGSTEP.FINISH)
                return "GetShoulderSideDirWorld";

            if (step == SWINGSTEP.TAKEBACK || step == SWINGSTEP.BACKSWING || step == SWINGSTEP.DOWNSWING)
                return "GetShoulderFrontDirWorld";

            if (step == SWINGSTEP.TOP || step == SWINGSTEP.FOLLOW)
                return "GetShoulderDir";

            return "GetShoulderAngle";
        }

        string GetPelvisKeyByStepLocal(SWINGSTEP step)
        {
            if (step == SWINGSTEP.ADDRESS)
                return "GetPelvisAngle";

            if (step == SWINGSTEP.TAKEBACK || step == SWINGSTEP.BACKSWING || step == SWINGSTEP.DOWNSWING || step == SWINGSTEP.IMPACT || step == SWINGSTEP.FINISH)
                return "GetPelvisFrontDirWorld";

            if (step == SWINGSTEP.TOP || step == SWINGSTEP.FOLLOW)
                return "GetPelvisDir";

            return "GetPelvisAngle";
        }

        int GetUserShoulderValueByStepLocal(SWINGSTEP step)
        {
            if (step == SWINGSTEP.ADDRESS || step == SWINGSTEP.IMPACT)
                return sensorProcess.iGetShoulderAngle;

            if (step == SWINGSTEP.FINISH)
                return sensorProcess.iGetShoulderSideDirWorld;

            if (step == SWINGSTEP.TAKEBACK || step == SWINGSTEP.BACKSWING || step == SWINGSTEP.DOWNSWING)
                return sensorProcess.iGetShoulderFrontDirWorld;

            if (step == SWINGSTEP.TOP || step == SWINGSTEP.FOLLOW)
                return sensorProcess.iGetShoulderDir;

            return sensorProcess.iGetShoulderAngle;
        }

        int GetUserPelvisValueByStepLocal(SWINGSTEP step)
        {
            if (step == SWINGSTEP.ADDRESS)
                return sensorProcess.iGetPelvisAngle;
            if (step == SWINGSTEP.TAKEBACK || step == SWINGSTEP.BACKSWING || step == SWINGSTEP.DOWNSWING || step == SWINGSTEP.IMPACT || step == SWINGSTEP.FINISH)
                return sensorProcess.iGetPelvisFrontDirWorld;
            if (step == SWINGSTEP.TOP || step == SWINGSTEP.FOLLOW)
                return sensorProcess.iGetPelvisDir;

            return sensorProcess.iGetPelvisAngle;
        }

        int CalcItemScore(int gap, int lacking, int tooMuch)
        {
            if (gap <= lacking)
                return 100;

            int hardMax = tooMuch * 3;

            if (gap >= hardMax)
                return 0;

            float t = Mathf.InverseLerp(lacking, hardMax, gap);

            return Mathf.RoundToInt(Mathf.Lerp(100f, 0f, t));
        }

        int EvaluateStepScore_Local(SWINGSTEP step)
        {
            // Pro
            int proWaist = GetSwingDataValueLocal("GetWaistSideDir", step);
            int proShoulder = GetSwingDataValueLocal(GetShoulderKeyByStepLocal(step), step);
            int proPelvis = GetSwingDataValueLocal(GetPelvisKeyByStepLocal(step), step);
            int proWeight = GetSwingDataValueLocal("GetWeight", step);
            int proHead = GetSwingDataValueLocal("GetNoseDir", step);

            // User
            int userWaist = sensorProcess.iGetWaistSideDir;
            int userShoulder = GetUserShoulderValueByStepLocal(step);
            int userPelvis = GetUserPelvisValueByStepLocal(step);
            int userWeight = sensorProcess.iGetWeight;
            int userHead = sensorProcess.iGetNoseDir;

            // Gap
            int gWaist = Mathf.Abs(proWaist - userWaist);
            int gShoulder = Mathf.Abs(proShoulder - userShoulder);
            int gPelvis = Mathf.Abs(proPelvis - userPelvis);
            int gWeight = Mathf.Abs(proWeight - userWeight);
            int gHead = Mathf.Abs(proHead - userHead);

            int sWaist = CalcItemScore(gWaist, 5, 10);
            int sShoulder = CalcItemScore(gShoulder, 5, 10);
            int sPelvis = CalcItemScore(gPelvis, 5, 10);
            int sWeight = CalcItemScore(gWeight, 3, 6);
            int sHead = CalcItemScore(gHead, 4, 8);

            int avg = Mathf.RoundToInt((sWaist + sShoulder + sPelvis + sWeight + sHead) / 5f);

            return Mathf.Clamp(avg, 0, 100);
        }

        // CSV 준비
        System.Text.StringBuilder sb = new System.Text.StringBuilder(4096);
        sb.AppendLine("step,frameIdx,proWaist,userWaist,dWaist,proShoulderKey,proShoulder,userShoulder,dShoulder,proPelvisKey,proPelvis,userPelvis,dPelvis,proWeight,userWeight,dWeight,proHead,userHead,dHead,stepScore");

        for (int s = 0; s < 8; s++)
        {
            int idx = stepIndex[s];

            if (idx < 0 || idx >= framesFront.Count)
                continue;

            SWINGSTEP stepEnum = (SWINGSTEP)(s + (int)SWINGSTEP.ADDRESS);

            byte[] frontBytes = (framesFront != null && idx >= 0 && idx < framesFront.Count) ? framesFront[idx] : null;
            byte[] sideBytes = (framesSide != null && idx >= 0 && idx < framesSide.Count) ? framesSide[idx] : null;

            if (frontBytes == null)
                continue;

            Texture2D frontRaw = CreateTextureFromRaw(frontBytes, widthFront, heightFront);
            Texture2D sideRaw = (sideBytes != null) ? CreateTextureFromRaw(sideBytes, widthSide, heightSide) : null;

            try
            {
                if (frontRaw == null)
                    continue;

                Texture2D frontTex = ApplyAnalyzeTransform(frontRaw);
                Texture2D sideTex = (sideRaw != null) ? ApplyAnalyzeTransform(sideRaw) : null;

                Landmark2D[] f2D;
                Landmark2D[] s2D;
                Landmark3D[] f3D;
                Landmark3D[] s3D;

                ExtractLandmarks(frontTex, sideTex, out f2D, out s2D, out f3D, out s3D);

                if (f2D != null && f3D != null)
                {
                    sensorProcess.UpdateSensor(f2D, s2D, f3D, s3D);

                    int stepScore = EvaluateStepScore_Local(stepEnum);

                    // Pro
                    int proWaist = GetSwingDataValueLocal("GetWaistSideDir", stepEnum);

                    string shKey = GetShoulderKeyByStepLocal(stepEnum);
                    int proShoulder = GetSwingDataValueLocal(shKey, stepEnum);

                    string pvKey = GetPelvisKeyByStepLocal(stepEnum);
                    int proPelvis = GetSwingDataValueLocal(pvKey, stepEnum);

                    int proWeight = GetSwingDataValueLocal("GetWeight", stepEnum);
                    int proHead = GetSwingDataValueLocal("GetNoseDir", stepEnum);

                    // User
                    int userWaist = sensorProcess.iGetWaistSideDir;
                    int userShoulder = GetUserShoulderValueByStepLocal(stepEnum);
                    int userPelvis = GetUserPelvisValueByStepLocal(stepEnum);
                    int userWeight = sensorProcess.iGetWeight;
                    int userHead = sensorProcess.iGetNoseDir;

                    // Gap
                    int dWaist = Mathf.Abs(proWaist - userWaist);
                    int dShoulder = Mathf.Abs(proShoulder - userShoulder);
                    int dPelvis = Mathf.Abs(proPelvis - userPelvis);
                    int dWeight = Mathf.Abs(proWeight - userWeight);
                    int dHead = Mathf.Abs(proHead - userHead);

                    sb.Append(stepEnum).Append(',')
                      .Append(idx).Append(',')
                      .Append(proWaist).Append(',').Append(userWaist).Append(',').Append(dWaist).Append(',')
                      .Append(shKey).Append(',').Append(proShoulder).Append(',').Append(userShoulder).Append(',').Append(dShoulder).Append(',')
                      .Append(pvKey).Append(',').Append(proPelvis).Append(',').Append(userPelvis).Append(',').Append(dPelvis).Append(',')
                      .Append(proWeight).Append(',').Append(userWeight).Append(',').Append(dWeight).Append(',')
                      .Append(proHead).Append(',').Append(userHead).Append(',').Append(dHead).Append(',')
                      .Append(stepScore)
                      .AppendLine();
                }

                if (frontTex != null)
                    Destroy(frontTex);

                if (sideTex != null)
                    Destroy(sideTex);
            }
            finally
            {
                if (frontRaw != null)
                    Destroy(frontRaw);

                if (sideRaw != null)
                    Destroy(sideRaw);
            }
        }

        // 저장
        try
        {
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "Debug");
            System.IO.Directory.CreateDirectory(dir);

            string path = System.IO.Path.Combine(dir, "ScoreDebug.csv");
            System.IO.File.WriteAllText(path, sb.ToString());
            Debug.Log($"[ScoreDebug] Saved: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ScoreDebug] Save failed: {e}");
        }
    }

    private HashSet<string> GetScoreKeys()
    {
        HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);

        if (ErrorMargins != null)
        {
            foreach (KeyValuePair<string, float> kv in ErrorMargins)
            {
                if (!string.IsNullOrEmpty(kv.Key))
                    set.Add(kv.Key);
            }
        }

        return set;
    }

    private string GetExternalBatchOutDir()
    {
        string dir = Path.Combine(Application.persistentDataPath, externalRgbBatchOutputFolder);

        return dir;
    }

    private string MakeBatchSessionDir()
    {
        string root = GetExternalBatchOutDir();
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string dir = Path.Combine(root, stamp);
        Directory.CreateDirectory(dir);

        return dir;
    }

    private static int[] CloneArray8(int[] src)
    {
        int[] dst = new int[8];

        if (src == null)
            return dst;

        for (int i = 0; i < 8 && i < src.Length; i++)
            dst[i] = src[i];

        return dst;
    }

    private static List<int> CloneList8(List<int> src)
    {
        List<int> dst = new List<int>(8);

        for (int i = 0; i < 8; i++)
        {
            int v = (src != null && i < src.Count) ? src[i] : -1;
            dst.Add(v);
        }

        return dst;
    }

    private static Dictionary<string, int[]> CloneUserData(Dictionary<string, int[]> src)
    {
        Dictionary<string, int[]> dst = new Dictionary<string, int[]>(64);

        if (src == null)
            return dst;

        foreach (KeyValuePair<string, int[]> kv in src)
        {
            dst[kv.Key] = CloneArray8(kv.Value);
        }

        return dst;
    }

    private IEnumerator RunExternalRgbBatchVerify()
    {
        SetCoachinggStep(COACHINGSTEP.ANALYZE);

        if (externalFrontRgbTextAssets == null || externalFrontRgbTextAssets.Length == 0)
        {
            SetCoachinggStep(COACHINGSTEP.RESULT);
            yield break;
        }

        int runs = Mathf.Max(1, externalRgbBatchRuns);
        int assetCount = externalFrontRgbTextAssets.Length;

        string sessionDir = MakeBatchSessionDir();

        List<BatchRunResult> results = new List<BatchRunResult>(assetCount * Mathf.Max(1, runs));

        for (int a = 0; a < assetCount; a++)
        {
            _externalRgbTextAssetIndex = a;

            _importedExternalRgbFrames = false;
            TryImportFramesFromExternalRgb();

            if (!_importedExternalRgbFrames || framesFront == null || framesFront.Count <= 0)
            {
                Debug.Log($"[ExternalRGB][Batch] Import failed. assetIndex={a}");
                continue;
            }

            for (int r = 0; r < runs; r++)
            {
                SetResultData();

                myScore = Enumerable.Repeat(-1, 8).ToList();
                addressTimeline.Clear();
                takebackTimeline.Clear();
                backswingTimeline.Clear();
                topTimeline.Clear();
                downswingTimeline.Clear();
                impactTimeline.Clear();
                followTimeline.Clear();
                finishTimeline.Clear();

                yield return StartCoroutine(AnalyzeSwingFromAllFrames());

                if (r == 0)
                {
                    BatchRunResult rr = new BatchRunResult();
                    rr.assetIndex = a;
                    rr.runIndex = r;
                    //rr.stepIndex = CloneArray8(stepIndex);
                    rr.stepIndex = new int[8];
                    rr.userData = CloneUserData(DicUserSwingData);
                    rr.myScore = CloneList8(myScore);

                    results.Add(rr);

                    string path = Path.Combine(sessionDir, $"{a:00}_{r:00}_UserData_ai.csv");
                    WriteUserDataCsv_ProfilePracticeFormat(path, rr.userData);
                    //WriteUserDataCsv_ScoreKeys(path, rr.userData);
                }

                yield return null;
            }

            yield return null;
        }

        if (assetCount >= 2)
        {
            string interPath = Path.Combine(sessionDir, "InterAsset_MeanError.csv");

            WriteInterAssetMeanErrorCsv(interPath, results, assetCount);
        }

        SetCoachinggStep(COACHINGSTEP.RESULT);
    }

    private void WriteInterAssetMeanErrorCsv(string path, List<BatchRunResult> results, int assetCount)
    {
        if (results == null || results.Count == 0)
            return;

        Dictionary<int, BatchRunResult> byAsset = new Dictionary<int, BatchRunResult>(assetCount);
        for (int i = 0; i < results.Count; i++)
        {
            BatchRunResult rr = results[i];

            if (rr == null)
                continue;

            if (!byAsset.ContainsKey(rr.assetIndex))
                byAsset.Add(rr.assetIndex, rr);
        }

        HashSet<string> scoreKeys = GetScoreKeys();

        HashSet<string> keySet = new HashSet<string>(StringComparer.Ordinal);

        foreach (KeyValuePair<int, BatchRunResult> kv in byAsset)
        {
            if (kv.Value.userData == null)
                continue;

            foreach (string k in kv.Value.userData.Keys)
            {
                if (!scoreKeys.Contains(k))
                    continue;

                keySet.Add(k);
            }

        }

        List<string> keys = new List<string>(keySet);
        keys.Sort(StringComparer.Ordinal);

        StringBuilder sb = new StringBuilder(16384);

        sb.Append("step,key,meanAbsDiff");
        for (int a = 0; a < assetCount; a++)
        {
            sb.Append(",v").Append(a);
        }

        sb.AppendLine();

        for (int s = 0; s < 8; s++)
        {
            SWINGSTEP step = (SWINGSTEP)s;

            for (int k = 0; k < keys.Count; k++)
            {
                string key = keys[k];

                int[] v = new int[assetCount];

                for (int a = 0; a < assetCount; a++)
                    v[a] = -1;

                for (int a = 0; a < assetCount; a++)
                {
                    BatchRunResult rr;

                    if (!byAsset.TryGetValue(a, out rr) || rr.userData == null)
                        continue;

                    int[] arr;
                    if (!rr.userData.TryGetValue(key, out arr) || arr == null)
                        continue;

                    if (s < arr.Length)
                        v[a] = arr[s];
                }

                float sumAbs = 0f;
                int pairCount = 0;

                for (int a0 = 0; a0 < assetCount; a0++)
                {
                    if (v[a0] < 0)
                        continue;

                    for (int a1 = a0 + 1; a1 < assetCount; a1++)
                    {
                        if (v[a1] < 0)
                            continue;

                        float d = Mathf.Abs(v[a0] - v[a1]);
                        sumAbs += d;
                        pairCount++;
                    }
                }

                float meanAbs = (pairCount > 0) ? (sumAbs / pairCount) : 0f;

                sb.Append(step).Append(',')
                  .Append(key).Append(',')
                  .Append(meanAbs.ToString("0.###"));

                for (int a = 0; a < assetCount; a++)
                {
                    sb.Append(',');

                    if (v[a] >= 0)
                        sb.Append(v[a]);
                }

                sb.AppendLine();
            }
        }

        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[ExternalRGB][InterAsset] Saved MeanAbs+Values CSV: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExternalRGB][InterAsset] CSV save failed: {e}");
        }
    }

    private void WriteUserDataCsv_ProfilePracticeFormat(string path, Dictionary<string, int[]> userData)
    {
        if (userData == null)
            return;

        StringBuilder sb = new StringBuilder(16384);

        sb.Append("key");
        for (int s = 0; s < 8; s++)
        {
            sb.Append(',').Append(((SWINGSTEP)s).ToString());
        }
        sb.AppendLine();

        List<string> keys = new List<string>(userData.Keys);
        keys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];

            int[] arr = null;
            if (!userData.TryGetValue(key, out arr) || arr == null)
                continue;

            sb.Append(key);

            for (int s = 0; s < 8; s++)
            {
                int v = (s < arr.Length) ? arr[s] : -1;
                sb.Append(',').Append(v);
            }

            sb.AppendLine();
        }

        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[CSV] Saved (Profile/Practice format): " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[CSV] Save failed: " + e);
        }
    }

    private FrameSensorSnapshot CaptureFrameSensorSnapshot()
    {
        FrameSensorSnapshot snapshot = new FrameSensorSnapshot();

        if (sensorProcess == null)
        {
            snapshot.isValid = false;
            return snapshot;
        }

        snapshot.isValid = true;

        // Front
        snapshot.handDir = sensorProcess.iGetHandDir;
        snapshot.handDirNF = sensorProcess.iGetHandDirNF;
        snapshot.handDistance = sensorProcess.iGetHandDistance;
        snapshot.shoulderDistance = sensorProcess.iGetShoulderDistance;
        snapshot.spineDir = sensorProcess.iGetSpineDir;
        snapshot.shoulderAngle = sensorProcess.iGetShoulderAngle;
        snapshot.footDisRate = sensorProcess.iGetFootDisRate;
        snapshot.weight = sensorProcess.iGetWeight;
        snapshot.forearmAngle = sensorProcess.iGetForearmAngle;
        snapshot.elbowFrontDir = sensorProcess.iGetElbowFrontDir;
        snapshot.elbowRightFrontDir = sensorProcess.iGetElbowRightFrontDir;
        snapshot.handDirDistance = sensorProcess.iGetHandDirDistance;
        snapshot.shoulderFrontDirWorld = sensorProcess.iGetShoulderFrontDirWorld;
        snapshot.pelvisFrontDirWorld = sensorProcess.iGetPelvisFrontDirWorld;
        snapshot.noseDir = sensorProcess.iGetNoseDir;
        snapshot.pelvisAngle = sensorProcess.iGetPelvisAngle;

        // Side
        snapshot.handSideDir = sensorProcess.iGetHandSideDir;
        snapshot.waistSideDir = sensorProcess.iGetWaistSideDir;
        snapshot.kneeSideDir = sensorProcess.iGetKneeSideDir;
        snapshot.elbowSideDir = sensorProcess.iGetElbowSideDir;
        snapshot.armpitDir = sensorProcess.iGetArmpitDir;
        snapshot.handSideDistance = sensorProcess.iGetHandSideDistance;
        snapshot.gripDistance = sensorProcess.iGetGripDistance;
        snapshot.shoulderSideDirWorld = sensorProcess.iGetShoulderSideDirWorld;
        snapshot.pelvisSideDirWorld = sensorProcess.iGetPelvisSideDirWorld;
        snapshot.noseShoulderSideDir = sensorProcess.iGetNoseShoulderSideDir;
        snapshot.nosePelvisSideDir = sensorProcess.iGetNosePelvisSideDir;

        // Combine
        snapshot.shoulderDir = sensorProcess.iGetShoulderDir;
        snapshot.pelvisDir = sensorProcess.iGetPelvisDir;
        snapshot.handCombineDir = sensorProcess.iGetHandCombineDir;

        return snapshot;
    }

    private void UserDataAddFromSnapshot(int step, FrameSensorSnapshot snapshot)
    {
        if (step < 0 || step > 7)
            return;

        if (!snapshot.isValid)
            return;

        if (swingStepData == null)
            return;

        if (DicUserSwingData == null)
            return;

        if (myScore == null || myScore.Count < 8)
            return;

        if (!DicUserSwingData.ContainsKey("GetHandDir"))
            return;

        // Front
        DicUserSwingData["GetHandDir"][step] = snapshot.handDir;
        DicUserSwingData["GetHandDistance"][step] = snapshot.handDistance;
        DicUserSwingData["GetShoulderDistance"][step] = snapshot.shoulderDistance;
        DicUserSwingData["GetSpineDir"][step] = snapshot.spineDir;
        DicUserSwingData["GetShoulderAngle"][step] = snapshot.shoulderAngle;
        DicUserSwingData["GetFootDisRate"][step] = snapshot.footDisRate;
        DicUserSwingData["GetWeight"][step] = snapshot.weight;
        DicUserSwingData["GetForearmAngle"][step] = snapshot.forearmAngle;
        DicUserSwingData["GetElbowFrontDir"][step] = snapshot.elbowFrontDir;
        DicUserSwingData["GetElbowRightFrontDir"][step] = snapshot.elbowRightFrontDir;
        DicUserSwingData["GetHandDirDistance"][step] = snapshot.handDirDistance;
        DicUserSwingData["GetShoulderFrontDirWorld"][step] = snapshot.shoulderFrontDirWorld;
        DicUserSwingData["GetPelvisFrontDirWorld"][step] = snapshot.pelvisFrontDirWorld;
        DicUserSwingData["GetNoseDir"][step] = snapshot.noseDir;
        DicUserSwingData["GetPelvisAngle"][step] = snapshot.pelvisAngle;

        // Side
        DicUserSwingData["GetHandSideDir"][step] = snapshot.handSideDir;
        DicUserSwingData["GetWaistSideDir"][step] = snapshot.waistSideDir;
        DicUserSwingData["GetKneeSideDir"][step] = snapshot.kneeSideDir;
        DicUserSwingData["GetElbowSideDir"][step] = snapshot.elbowSideDir;
        DicUserSwingData["GetArmpitDir"][step] = snapshot.armpitDir;
        DicUserSwingData["GetHandSideDistance"][step] = snapshot.handSideDistance;
        DicUserSwingData["GetGripDistance"][step] = snapshot.gripDistance;
        DicUserSwingData["GetShoulderSideDirWorld"][step] = snapshot.shoulderSideDirWorld;
        DicUserSwingData["GetPelvisSideDirWorld"][step] = snapshot.pelvisSideDirWorld;
        DicUserSwingData["GetNoseShoulderSideDir"][step] = snapshot.noseShoulderSideDir;
        DicUserSwingData["GetNosePelvisSideDir"][step] = snapshot.nosePelvisSideDir;

        // Combine
        DicUserSwingData["GetShoulderDir"][step] = snapshot.shoulderDir;
        DicUserSwingData["GetPelvisDir"][step] = snapshot.pelvisDir;
        DicUserSwingData["GetHandCombineDir"][step] = snapshot.handCombineDir;

        string[] selectedKeys;

        switch (step)
        {
            case 0: selectedKeys = new[] { "GetShoulderAngle", "GetWaistSideDir", "GetKneeSideDir" }; break;
            case 1: selectedKeys = new[] { "GetForearmAngle", "GetShoulderAngle" }; break;
            case 2: selectedKeys = new[] { "GetShoulderDir", "GetPelvisDir", "GetForearmAngle", "GetWeight" }; break;
            case 3: selectedKeys = new[] { "GetShoulderDir", "GetPelvisDir", "GetForearmAngle", "GetWeight" }; break;
            case 4: selectedKeys = new[] { "GetShoulderDir", "GetHandSideDir", "GetSpineDir", "GetWeight" }; break;
            case 5: selectedKeys = new[] { "GetShoulderDir", "GetPelvisDir" }; break;
            case 6: selectedKeys = new[] { "GetPelvisDir", "GetShoulderDir" }; break;
            case 7: selectedKeys = new[] { "GetPelvisDir", "GetShoulderDir" }; break;
            default: selectedKeys = new string[] { }; break;
        }

        const int downswingIndex = 4;
        const int followIndex = 6;
        const int finishIndex = 7;

        int stepScore = 0;

        if (step == finishIndex)
        {
            int sum = 0;
            int count = 0;

            for (int s = downswingIndex; s <= followIndex; s++)
            {
                int value = myScore[s];
                if (value >= 0)
                {
                    sum += value;
                    count++;
                }
            }

            if (count == 0)
            {
                for (int s = 0; s <= followIndex; s++)
                {
                    int value = myScore[s];
                    if (value >= 0)
                    {
                        sum += value;
                        count++;
                    }
                }
            }

            stepScore = (count > 0) ? Mathf.RoundToInt((float)sum / count) : 0;
        }
        else
        {
            stepScore = GetUserStepAverage(step, selectedKeys);
        }

        if (_isGapStep != null && step >= 0 && step < _isGapStep.Length && _isGapStep[step])
        {
            stepScore = 20;
        }

        myScore[step] = stepScore;

        AppendStepTimelineScore(step, stepScore);
    }

    private string GetProAICsvPath()
    {
        string homeDir = System.Environment.GetEnvironmentVariable("HOME");
        int uid = GolfProDataManager.Instance.SelectProData.uid;

        string proSwingDir = Path.Combine(homeDir, "DataBase_park", "ProSwing", uid.ToString());
        string fileName = $"{(int)ESwingType.Full}_{(int)EClub.MiddleIron}_ai_frames.csv";

        return Path.Combine(proSwingDir, fileName);
    }

    private int ParseCsvInt(string[] cols, Dictionary<string, int> headerMap, string key, int defaultValue = -1)
    {
        if (cols == null || headerMap == null)
            return defaultValue;

        if (!headerMap.TryGetValue(key, out int index))
            return defaultValue;

        if (index < 0 || index >= cols.Length)
            return defaultValue;

        if (int.TryParse(cols[index], out int value))
            return value;

        return defaultValue;
    }

    private bool LoadProAICsv(List<FrameSensorSnapshot> outSnapshots)
    {
        if (outSnapshots == null)
            return false;

        outSnapshots.Clear();

        string csvPath = GetProAICsvPath();

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            Debug.LogError("[AICoaching][Total] Pro AI full-frame csv not found: " + csvPath);
            return false;
        }

        try
        {
            string[] lines = File.ReadAllLines(csvPath, new UTF8Encoding(true));

            if (lines == null || lines.Length <= 1)
            {
                Debug.LogError("[AICoaching][Total] Pro AI full-frame csv is empty.");
                return false;
            }

            string[] headers = lines[0].Split(',');
            Dictionary<string, int> headerMap = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i].Trim();
                if (!headerMap.ContainsKey(header))
                {
                    headerMap.Add(header, i);
                }
            }

            for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] cols = line.Split(',');

                FrameSensorSnapshot snapshot = new FrameSensorSnapshot();

                snapshot.isValid = ParseCsvInt(cols, headerMap, "isValid", 0) == 1;

                snapshot.handDir = ParseCsvInt(cols, headerMap, "GetHandDir");
                snapshot.handDirNF = ParseCsvInt(cols, headerMap, "GetHandDirNF");
                snapshot.handDistance = ParseCsvInt(cols, headerMap, "GetHandDistance");
                snapshot.shoulderDistance = ParseCsvInt(cols, headerMap, "GetShoulderDistance");
                snapshot.spineDir = ParseCsvInt(cols, headerMap, "GetSpineDir");
                snapshot.shoulderAngle = ParseCsvInt(cols, headerMap, "GetShoulderAngle");
                snapshot.footDisRate = ParseCsvInt(cols, headerMap, "GetFootDisRate");
                snapshot.weight = ParseCsvInt(cols, headerMap, "GetWeight");
                snapshot.forearmAngle = ParseCsvInt(cols, headerMap, "GetForearmAngle");
                snapshot.elbowFrontDir = ParseCsvInt(cols, headerMap, "GetElbowFrontDir");
                snapshot.elbowRightFrontDir = ParseCsvInt(cols, headerMap, "GetElbowRightFrontDir");
                snapshot.handDirDistance = ParseCsvInt(cols, headerMap, "GetHandDirDistance");
                snapshot.shoulderFrontDirWorld = ParseCsvInt(cols, headerMap, "GetShoulderFrontDirWorld");
                snapshot.pelvisFrontDirWorld = ParseCsvInt(cols, headerMap, "GetPelvisFrontDirWorld");
                snapshot.noseDir = ParseCsvInt(cols, headerMap, "GetNoseDir");
                snapshot.pelvisAngle = ParseCsvInt(cols, headerMap, "GetPelvisAngle");

                snapshot.handSideDir = ParseCsvInt(cols, headerMap, "GetHandSideDir");
                snapshot.waistSideDir = ParseCsvInt(cols, headerMap, "GetWaistSideDir");
                snapshot.kneeSideDir = ParseCsvInt(cols, headerMap, "GetKneeSideDir");
                snapshot.elbowSideDir = ParseCsvInt(cols, headerMap, "GetElbowSideDir");
                snapshot.armpitDir = ParseCsvInt(cols, headerMap, "GetArmpitDir");
                snapshot.handSideDistance = ParseCsvInt(cols, headerMap, "GetHandSideDistance");
                snapshot.gripDistance = ParseCsvInt(cols, headerMap, "GetGripDistance");
                snapshot.shoulderSideDirWorld = ParseCsvInt(cols, headerMap, "GetShoulderSideDirWorld");
                snapshot.pelvisSideDirWorld = ParseCsvInt(cols, headerMap, "GetPelvisSideDirWorld");
                snapshot.noseShoulderSideDir = ParseCsvInt(cols, headerMap, "GetNoseShoulderSideDir");
                snapshot.nosePelvisSideDir = ParseCsvInt(cols, headerMap, "GetNosePelvisSideDir");

                snapshot.shoulderDir = ParseCsvInt(cols, headerMap, "GetShoulderDir");
                snapshot.pelvisDir = ParseCsvInt(cols, headerMap, "GetPelvisDir");
                snapshot.handCombineDir = ParseCsvInt(cols, headerMap, "GetHandCombineDir");

                outSnapshots.Add(snapshot);
            }

            Debug.Log($"[AICoaching][Total] Pro AI full-frame loaded: {outSnapshots.Count}");
            return outSnapshots.Count > 0;
        }
        catch (Exception e)
        {
            Debug.LogError("[AICoaching][Total] LoadProAIFullFrameCsv failed: " + e.Message);
            return false;
        }
    }

    private List<int> MatchUserFramesByProHand(List<FrameSensorSnapshot> proSnapshots, List<FrameSensorSnapshot> userSnapshots, int maxHandDiff = 40)
    {
        List<int> matchedIndices = new List<int>();

        if (proSnapshots == null || userSnapshots == null)
            return matchedIndices;

        if (proSnapshots.Count <= 0 || userSnapshots.Count <= 0)
            return matchedIndices;

        int userStartIndex = 0;

        for (int proIndex = 0; proIndex < proSnapshots.Count; proIndex++)
        {
            FrameSensorSnapshot proSnapshot = proSnapshots[proIndex];

            if (!proSnapshot.isValid || proSnapshot.handDirNF < 0)
            {
                matchedIndices.Add(-1);
                continue;
            }

            int bestUserIndex = -1;
            int bestDiff = int.MaxValue;

            for (int userIndex = userStartIndex; userIndex < userSnapshots.Count; userIndex++)
            {
                FrameSensorSnapshot userSnapshot = userSnapshots[userIndex];

                if (!userSnapshot.isValid || userSnapshot.handDirNF < 0)
                    continue;

                int diff = Mathf.Abs(userSnapshot.handDirNF - proSnapshot.handDirNF);

                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    bestUserIndex = userIndex;
                }

                if (bestDiff <= 2)
                    break;
            }

            if (bestUserIndex >= 0 && bestDiff <= maxHandDiff)
            {
                matchedIndices.Add(bestUserIndex);
                userStartIndex = bestUserIndex + 1;
            }
            else
            {
                matchedIndices.Add(-1);
            }

            if (userStartIndex >= userSnapshots.Count)
            {
                for (int remain = proIndex + 1; remain < proSnapshots.Count; remain++)
                {
                    matchedIndices.Add(-1);
                }

                break;
            }
        }

        return matchedIndices;
    }

    private int GetSnapshotValueByKey(FrameSensorSnapshot snapshot, string key)
    {
        switch (key)
        {
            case "GetShoulderDir": return snapshot.shoulderDir;
            case "GetPelvisDir": return snapshot.pelvisDir;
            case "GetSpineDir": return snapshot.spineDir;
            case "GetWeight": return snapshot.weight;
            case "GetForearmAngle": return snapshot.forearmAngle;
            case "GetHandSideDir": return snapshot.handSideDir;
            case "GetWaistSideDir": return snapshot.waistSideDir;
            case "GetKneeSideDir": return snapshot.kneeSideDir;
            default: return -1;
        }
    }

    private int CalculateFrameCompareScore(FrameSensorSnapshot proSnapshot, FrameSensorSnapshot userSnapshot)
    {
        string[] compareKeys = new[]
        {
        "GetShoulderDir",
        "GetPelvisDir",
        "GetSpineDir",
        "GetWeight",
        "GetForearmAngle",
        "GetHandSideDir"
    };

        float totalScore = 0f;
        int validCount = 0;

        for (int i = 0; i < compareKeys.Length; i++)
        {
            string key = compareKeys[i];

            int proValue = GetSnapshotValueByKey(proSnapshot, key);
            int userValue = GetSnapshotValueByKey(userSnapshot, key);

            if (proValue < 0 || userValue < 0)
                continue;

            float tolerance = ErrorMargins.ContainsKey(key) ? ErrorMargins[key] : 30f;
            float diff = Mathf.Abs(userValue - proValue);

            float score;
            float goodThreshold = 5f;

            if (diff <= goodThreshold)
            {
                score = 100f;
            }
            else
            {
                float normalized = Mathf.Clamp01((diff - goodThreshold) / Mathf.Max(1f, tolerance - goodThreshold));
                score = 100f - Mathf.Pow(normalized, 0.5f) * 100f;
            }

            score = Mathf.Clamp(score, 0f, 100f);

            totalScore += score;
            validCount++;
        }

        if (validCount <= 0)
            return 0;

        return Mathf.RoundToInt(totalScore / validCount);
    }

    private int CalculateTotalAnalyzeScore(List<FrameSensorSnapshot> proSnapshots, List<FrameSensorSnapshot> userSnapshots, List<int> matchedUserIndices)
    {
        totalFrameScores.Clear();

        if (proSnapshots == null || userSnapshots == null || matchedUserIndices == null)
            return 0;

        float totalScore = 0f;
        int validFrameCount = 0;

        int loopCount = Mathf.Min(proSnapshots.Count, matchedUserIndices.Count);

        for (int i = 0; i < loopCount; i++)
        {
            int userIndex = matchedUserIndices[i];

            if (userIndex < 0 || userIndex >= userSnapshots.Count)
                continue;

            FrameSensorSnapshot proSnapshot = proSnapshots[i];
            FrameSensorSnapshot userSnapshot = userSnapshots[userIndex];

            if (!proSnapshot.isValid || !userSnapshot.isValid)
                continue;

            int frameScore = CalculateFrameCompareScore(proSnapshot, userSnapshot);

            totalFrameScores.Add(frameScore);
            totalScore += frameScore;
            validFrameCount++;
        }

        if (validFrameCount <= 0)
            return 0;

        return Mathf.RoundToInt(totalScore / validFrameCount);
    }

    private void FillStepGaps(int[] stepIndex, int frameCount)
    {
        if (stepIndex == null || stepIndex.Length == 0 || frameCount <= 0)
            return;

        if (_isGapStep == null || _isGapStep.Length != stepIndex.Length)
            _isGapStep = new bool[stepIndex.Length];

        Array.Clear(_isGapStep, 0, _isGapStep.Length);

        int lastValid = -1;

        for (int i = 0; i < stepIndex.Length; i++)
        {
            int idx = stepIndex[i];

            if (idx >= 0 && idx < frameCount)
            {
                lastValid = idx;
                continue;
            }

            if (lastValid >= 0)
            {
                stepIndex[i] = lastValid;
                _isGapStep[i] = true;
            }
        }
    }

    private void WriteUserDataCsv_ScoreKeys(string path, Dictionary<string, int[]> userData)
    {
        if (userData == null)
            return;

        HashSet<string> scoreKeys = GetScoreKeys();

        StringBuilder sb = new StringBuilder(16384);

        sb.Append("key");

        for (int s = 0; s < 8; s++)
        {
            sb.Append(',').Append(((SWINGSTEP)s).ToString());
        }
        sb.AppendLine();

        List<string> keys = new List<string>(userData.Keys);
        keys.Sort(StringComparer.Ordinal);

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];

            if (scoreKeys != null && scoreKeys.Count > 0 && !scoreKeys.Contains(key))
                continue;

            int[] arr = null;
            if (!userData.TryGetValue(key, out arr) || arr == null)
                continue;

            sb.Append(key);

            for (int s = 0; s < 8; s++)
            {
                int v = (s < arr.Length) ? arr[s] : -1;
                sb.Append(',').Append(v);
            }

            sb.AppendLine();
        }

        try
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("[CSV] Saved (ScoreKeys): " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[CSV] Save failed: " + e);
        }
    }

    private void OnDestroy()
    {
        if (_tfAnalyzer != null)
        {
            _tfAnalyzer.Dispose();
            _tfAnalyzer = null;
        }

        if (_offlinePoseLM != null)
        {
            _offlinePoseLM.Close();
            _offlinePoseLM = null;
        }

        if (offlineTextureFrame != null)
        {
            offlineTextureFrame.Dispose();
            offlineTextureFrame = null;
        }

        if (offlinePoseLandmarker != null)
        {
            offlinePoseLandmarker.Close();
            offlinePoseLandmarker = null;
        }
    }
}
