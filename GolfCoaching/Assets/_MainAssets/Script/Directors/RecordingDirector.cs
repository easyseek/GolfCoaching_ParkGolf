using Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.CoordinateSystem;
using Debug = UnityEngine.Debug;
using System.Text;
using UnityEngine.Rendering;
using System.Globalization;

public class RecordingDirector : MonoBehaviour
{
    enum EProfileStep
    {
        GRIP,
        READY,
        SWING,
        RESULT
    }

    enum ELessonStep
    {
        CHECK,
        READY,
        RECORDING,
        RESULT
    }

    enum EPracticeStep
    {
        GRIP,
        SWING,
        RESULT
    }

    [SerializeField] private ELessonView _lessonView = ELessonView.FRONT;

    [SerializeField] private GameObject m_ProfilePanel;
    [SerializeField] private GameObject m_LessonPanel;
    [SerializeField] private GameObject m_PracticePanel;
    [SerializeField] private GameObject m_TopObj;
    [SerializeField] private GameObject m_VirtualKeyboard;

    [SerializeField] private TextMeshProUGUI m_DedugText1;
    [SerializeField] private TextMeshProUGUI m_DedugText2;

    [Header("------------------------------ Profile ------------------------------")]
    [Header("* 1.GRIP")]
    [SerializeField] private GameObject m_ProfileGribPanel;

    [Header("* 2.READY")]
    [SerializeField] private GameObject m_ProfileReadyPanel;

    [SerializeField] private TextMeshProUGUI m_ProfileCountText;

    [Header("* 3.SWING")]
    [SerializeField] private GameObject m_ProfileSwingPanel;

    [Header("* 3.RESULT")]
    [SerializeField] private GameObject m_ProfileResultPanel;
    [SerializeField] private GameObject m_ProfilePlayBtn;

    [SerializeField] private RawImage m_ProfilePreviewRawImage;
    [SerializeField] private VLCVideoPlayer m_ProfileVideoPlayer;
    [SerializeField] private TextMeshProUGUI m_ProfileKeywordText;

    private string _profileFrontTempPath = null;
    private string _profileSideTempPath = null;

    [Header("------------------------------ Lesson ------------------------------")]
    [Header("* 1.CHECK")]
    [SerializeField] private GameObject m_CheckPanel;

    [SerializeField] private TextMeshProUGUI m_CountText;

    [Header("* 2.READY")]
    [SerializeField] private GameObject m_ReadyPanel;

    [Header("* 3.RECORDING")]
    [SerializeField] private GameObject m_RecordingPanel;
    [SerializeField] private GameObject m_LoadingPanel;

    [SerializeField] private Image m_RedDot;

    [SerializeField] private TextMeshProUGUI m_TimerText;

    private float blinkTime = 0.5f;
    private float blinkTimer;

    [Header("* 4.RESULT")]
    [SerializeField] private GameObject m_LessonResultPanel;
    [SerializeField] private GameObject[] m_PopupObjs;
    [SerializeField] private RawImage m_LessonVideoThumbnail;

    [SerializeField] private TextMeshProUGUI m_VideoTimeText;

    [SerializeField] private TMP_InputField m_SubjectInput;
    [SerializeField] private TMP_InputField m_KeywordInput;

    [SerializeField] private Toggle[] m_KeywordToggles;

    private EStance keywordPose = EStance.Full;
    private EClub keywordClub = EClub.MiddleIron;

    [Space]
    [Header("------------------------------ Practice ------------------------------")]

    [Header("* 1.GRIP")]
    [SerializeField] private GameObject m_GuidePanel;

    [Header("* 2.SWING")]
    [SerializeField] private GameObject m_SwingPanel;
    [SerializeField] private GameObject m_SwingProgress;
    [SerializeField] private GameObject m_Check;

    [SerializeField] private Image m_ProgressBarImg;
    [SerializeField] private Image m_ClubImg;

    [SerializeField] private Toggle[] m_StepToggles;

    [SerializeField] private Sprite[] m_ClubSprites;

    [SerializeField] private TextMeshProUGUI m_InfoText;

    [Header("* 3.RESULT")]
    [SerializeField] private SwingCardViewer m_SwingCardViewer;

    [SerializeField] private PopupControl m_PopupControl;

    [SerializeField] private GameObject m_PracticeResultPanel;
    [SerializeField] private GameObject m_Main;
    [SerializeField] private GameObject m_SelectRetake;
    [SerializeField] private GameObject m_ReviewCancle;
    [SerializeField] private GameObject[] m_CaptureChecks;
    [SerializeField] private GameObject[] m_CaptureObjs;

    [SerializeField] private Toggle[] m_CheckToggles;

    [SerializeField] private RawImage[] m_CaptureRawImages;
    [SerializeField] private Image m_InfoImage;

    [SerializeField] private Sprite[] m_InfoSprites;

    [SerializeField] private TextMeshProUGUI m_ResultInfoText;
    [SerializeField] private TextMeshProUGUI m_ResultInfoSubText;
    [SerializeField] private TextMeshProUGUI m_ClubTypeText;

    [Header("* MOCAP")]
    [SerializeField] GameObject[] m_MocapObjs;
    [SerializeField] WebcamTracker webcamTrackerFront;
    [SerializeField] WebcamTracker webcamTrackerSide;
    [SerializeField] WebcamTrackerController webcamTrackerController;
    [SerializeField] SensorProcess m_SensorProcess;
    [SerializeField] private TextMeshProUGUI m_AvgFrontText;
    [SerializeField] private TextMeshProUGUI m_AvgSideText;
    [SerializeField] private TextMeshProUGUI m_HandText;

    [SerializeField] private bool _debugProfileAnalyze = true;

    private struct FrameAnalyzeSnapshot
    {
        public bool isValid;

        public int handDir;
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

        public int pelvisAngle;
        public int noseDir;

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

        public int shoulderDir;
        public int pelvisDir;
        public int handCombineDir;

        public int handDirNF;
    }

    private List<FrameAnalyzeSnapshot> _profileAnalyzeSnapshots = new List<FrameAnalyzeSnapshot>(1024);

    [Header("* VIDEO REF.")]
    [SerializeField] private bool _debugSavePracticeAnalyzeJpg = true;
    [SerializeField] RawImage rawImageFront;
    [SerializeField] RawImage rawImageSide;

    private Texture2D captureFront = null;
    public List<byte[]> framesFront = new List<byte[]>();
    public List<byte[]> framesSide = new List<byte[]>();
    private List<Texture2D> captureRealPoseFrontPro = Enumerable.Repeat<Texture2D>(null, 8).ToList();
    private List<Texture2D> captureRealPoseSidePro = Enumerable.Repeat<Texture2D>(null, 8).ToList();
    private Dictionary<SWINGSTEP, Texture2D> captureFrontDic = new Dictionary<SWINGSTEP, Texture2D>();
    private Dictionary<SWINGSTEP, Texture2D> captureSideDic = new Dictionary<SWINGSTEP, Texture2D>();
    private List<SWINGSTEP> selectSwingStep = new List<SWINGSTEP>();

    [Header("* AUDIO REF.")]
    [SerializeField] private bool _enableMicRecording = true;
    [SerializeField] private string _micDeviceName = null;

    private AudioClip _micClip = null;
    private int _micSampleRate = 44100;
    private string _audioTempWavPath = null;
    private string _muxTempVideoPath = null;

    [Header("* MEDIAPIPE")]
    private PoseLandmarker _offlinePoseLM;
    private TextureFrame _tfAnalyzer;
    private bool _poseLMForVideo = false;
    private bool _rightHanded = true;

    [Header("* SWING RGB")]
    [SerializeField] private bool _profileUseExternalRgb = false;

    [SerializeField] private TextAsset _profileFrontRgb;
    [SerializeField] private TextAsset _profileSideRgb;

    [SerializeField] private int _profileRgbWidthFront = 1280;
    [SerializeField] private int _profileRgbHeightFront = 720;
    [SerializeField] private int _profileRgbWidthSide = 1280;
    [SerializeField] private int _profileRgbHeightSide = 720;

    [SerializeField] private int _profileRgbMaxFrames = 0;

    private ProSwingStepData _swingStepData;
    private Dictionary<SWINGSTEP, AngleRange> _angleRanges = new();
    private const int ANALYZER_FPS = 30;

    UnityEngine.Rect screenRect;

    private class AngleRange
    {
        public float CheckValue, LimitMin, LimitMax; public bool IsMore, UseLimit;
        public AngleRange(float checkValue, float limitMin, float limitMax, bool isMore, bool useLimit)
        {
            CheckValue = checkValue; LimitMin = limitMin; LimitMax = limitMax; IsMore = isMore; UseLimit = useLimit;
        }
        public bool InLimit(float v) => !UseLimit || (v >= LimitMin && v <= LimitMax);
        public bool Pass(float v) => IsMore ? (v > CheckValue) : (v < CheckValue);
    }

    Dictionary<string, int[]> ResultProData = new Dictionary<string, int[]>();

    private bool isRecording = false;
    bool isFinish = false;
    bool isSelectRetake = false;
    bool isReviewing = false;

    int widthFront;
    int heightFront;
    int widthSide;
    int heightSide;
    int curStepNum = 0;

    bool _replayReadyFront = false;
    bool _replayReadySide = false;
    int fps = 30;

    string ffmpegPath = string.Empty;

    float _lastHandDir;
    bool _handCheck = false;
    bool checkTakeback = false;
    bool checkImpact = false;
    float AvgVisible = 0;
    float _invisibleTimer = 0.2f;

    int checkTakebackFrame = 0;

    Vector3 _yFlip = new Vector3(1, -1, 1);

    readonly string reviewStr = "검토를 요청하면 수정이 어렵습니다.\r\n진행하시겠습니까?";
    readonly string retakeStr = "다시 촬영하면 이전 영상이 삭제됩니다.\r\n진행하시겠습니까?";
    readonly string backStr = "뒤로가면 영상이 삭제됩니다.\r\n진행하시겠습니까?";
    readonly string reviewCancleStr = "검토를 취소하면 영상이 삭제됩니다.\r\n진행하시겠습니까?";
    readonly string selectSwingStepStr = "다시 촬영할 자세를\r\n선택해주세요.";
    readonly string confirmReviewStr = "해당 영상이\r\n검토중으로 변경되었습니다.";
    readonly string recordingStopStr = "촬영 종료 시 영상이 삭제됩니다.\r\n계속하시겠습니까?";
    readonly string saveVideoStr = "촬영된 영상을 저장합니다.\r\n계속하시겠습니까?";
    readonly string invalidSubjectStr = "제목을 입력해주세요.";
    readonly string duplicateNameStr = "이미 동일한 제목의 영상이 있습니다.\r\n다른 제목을 입력해주세요.";

    EProfileStep profileStep = EProfileStep.GRIP;
    ELessonStep lessonStep = ELessonStep.CHECK;
    EPracticeStep practiceStep = EPracticeStep.GRIP;
    ESwingType swingType = ESwingType.Full;
    EClub club = EClub.MiddleIron;
    ERecordingType recordingType = ERecordingType.None;

    string videoTimeStr;
    string path;
    string imagePath;
    string videoPath;
    string videoCSVPath;
    string profileFrontPath;
    string profileSidePath;

    private string _pendingVideoTempPath = null;
    private string _pendingVideoFinalPath = null;
    private bool _pendingVideoReady = false;
    private bool _pendingConfirmed = false;

    Landmark2D[] front2D;
    Landmark2D[] side2D;
    Landmark3D[] front3D;
    Landmark3D[] side3D;

    private const int PRACTICE_LANDMARK_COUNT = 33;

    private string landmarkPath;

    IEnumerator Start()
    {
        yield return null;

        recordingType = GameManager.Instance.RecordingType;
        swingType = GameManager.Instance.SwingType;
        club = GameManager.Instance.Club;

        if (recordingType == ERecordingType.Profile)
        {
            webcamTrackerController.SetTracker(true, true);

            rawImageSide.gameObject.SetActive(true);

            Color c = rawImageSide.color;
            c.a = 0f;
            rawImageSide.color = c;

            rawImageSide.raycastTarget = false;

            m_ProfilePanel.SetActive(true);
        }
        else if (recordingType == ERecordingType.Lesson)
        {
            webcamTrackerController.SetTracker(false, false);
            ApplyLessonViewUI();
            m_LessonPanel.SetActive(true);
        }
        else if (recordingType == ERecordingType.Practice)
        {
            webcamTrackerController.SetTracker(true, true);
            rawImageSide.gameObject.SetActive(true);

            Color c = rawImageSide.color;
            c.a = 0f;
            rawImageSide.color = c;

            rawImageSide.raycastTarget = false;

            m_PracticePanel.SetActive(true);
        }

        Init();
        StartCoroutine(CheckStepCoroutine());
    }

    private void Init()
    {
        string homeDir = System.Environment.GetEnvironmentVariable("HOME");

        path = Path.Combine(homeDir, "DataBase", "ProSwing", GolfProDataManager.Instance.SelectProData.uid.ToString());
        landmarkPath = Path.Combine(path, "landmark");

        string videoDir = Path.Combine(homeDir, "DataBase", "ProVideo", GolfProDataManager.Instance.SelectProData.uid.ToString());

        if (!Directory.Exists(videoDir))
        {
            Directory.CreateDirectory(videoDir);
        }

        videoPath = Path.Combine(videoDir, "lesson_temp.mp4");

        _pendingVideoFinalPath = videoPath;

        string tempBaseName = Path.GetFileNameWithoutExtension(_pendingVideoFinalPath);
        _pendingVideoTempPath = Path.Combine(videoDir, tempBaseName + ".tmp.mp4");

        videoCSVPath = Path.Combine(homeDir, "DataBase", "ProVideo", GolfProDataManager.Instance.SelectProData.uid.ToString(), $"{GolfProDataManager.Instance.SelectProData.uid}.csv");

        imagePath = Path.Combine(homeDir, "DataBase", "ProImage", GolfProDataManager.Instance.SelectProData.uid.ToString());

        profileFrontPath = Path.Combine(homeDir, "DataBase", "ProVideo", GolfProDataManager.Instance.SelectProData.uid.ToString(), $"front_video_{(int)swingType}{(int)club}.mp4");
        profileSidePath = Path.Combine(homeDir, "DataBase", "ProVideo", GolfProDataManager.Instance.SelectProData.uid.ToString(), $"side_video_{(int)swingType}{(int)club}.mp4");

        _profileFrontTempPath = Path.Combine(homeDir, "DataBase", "ProVideo", GolfProDataManager.Instance.SelectProData.uid.ToString(), $"front_video_{(int)swingType}{(int)club}_temp.mp4");
        _profileSideTempPath = Path.Combine(homeDir, "DataBase", "ProVideo", GolfProDataManager.Instance.SelectProData.uid.ToString(), $"side_video_{(int)swingType}{(int)club}_temp.mp4");

        ffmpegPath = Path.Combine(Application.dataPath, "Plugins", "ffmpeg", "bin", "ffmpeg");

        for (int i = 0; i < m_CheckToggles.Length; i++)
        {
            int index = i;
            m_CheckToggles[i].onValueChanged.AddListener((isOn) => OnValueChanged_Check(index, isOn));
        }

        if (club != EClub.None)
        {
            if (recordingType == ERecordingType.Profile)
            {
                switch (swingType)
                {
                    case ESwingType.Full:
                        if (GolfProDataManager.Instance.SelectProData.swingData.dicFull.ContainsKey(club))
                            _swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[club];
                        break;

                    case ESwingType.ThreeQuarter:
                        if (GolfProDataManager.Instance.SelectProData.swingData.dicQuarter.ContainsKey(club))
                            _swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicQuarter[club];
                        break;

                    case ESwingType.Half:
                        if (GolfProDataManager.Instance.SelectProData.swingData.dicHalf.ContainsKey(club))
                            _swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicHalf[club];
                        break;
                }

                if (_swingStepData == null)
                {
                    _swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.MiddleIron];
                }
            }
        }

        // ProLandmarkStepPaths stepData = GolfProDataManager.Instance.GetLandmarkStepData(GolfProDataManager.Instance.SelectProData.uid, false, ESwingType.Full, EClub.MiddleIron);

        // if (stepData != null && stepData.stepLandmarks.TryGetValue(SWINGSTEP.ADDRESS, out var lm))
        // {
        //     Landmark2D leftWrist = lm.front2D[15];

        //     Debug.Log($"LWrist org={leftWrist.positionOrg} pos={leftWrist.position} vis={leftWrist.visibility}");
        // }
    }

    void SetReady()
    {
        if (recordingType == ERecordingType.Profile)
        {
            _replayReadyFront = false;
            _replayReadySide = false;
        }
        else if (recordingType == ERecordingType.Lesson)
        {
            foreach (var toggle in m_KeywordToggles)
            {
                toggle.onValueChanged.AddListener(isOn => OnValueChanged_Keyword(toggle, isOn));
            }

            m_TopObj.SetActive(true);

            _lessonView = ELessonView.FRONT;
            ApplyLessonViewUI();
        }
        else if (recordingType == ERecordingType.Practice)
        {
            foreach (var toggle in m_StepToggles)
            {
                toggle.gameObject.SetActive(true);
            }

            _invisibleTimer = 0.2f;

            if (!isSelectRetake)
            {
                foreach (var obj in m_CaptureObjs)
                {
                    obj.SetActive(false);
                }

                captureFrontDic.Clear();
                captureSideDic.Clear();
            }

            m_ProgressBarImg.fillAmount = 0.0f;
            m_Check.SetActive(false);
            m_SwingProgress.SetActive(false);
            m_InfoText.text = string.Empty;
            m_TopObj.SetActive(true);
        }
    }

    private IEnumerator CheckStepCoroutine()
    {
        while (true)
        {
            yield return null;

            if (recordingType == ERecordingType.Profile)
            {
                EProfileStep currentStep = profileStep;

                switch (currentStep)
                {
                    case EProfileStep.GRIP:
                        yield return StartCoroutine(HandleProfileGribStep());
                        break;

                    case EProfileStep.READY:
                        yield return StartCoroutine(HandleProfileReadyStep());
                        break;

                    case EProfileStep.SWING:
                        yield return StartCoroutine(HandleProfileSWINGStep());
                        break;

                    case EProfileStep.RESULT:
                        yield return StartCoroutine(HandleProfileResultStep());
                        break;
                }
            }
            else if (recordingType == ERecordingType.Lesson)
            {
                ELessonStep currentStep = lessonStep;

                switch (currentStep)
                {
                    case ELessonStep.CHECK:
                        yield return StartCoroutine(HandleCheckStep());
                        break;

                    case ELessonStep.READY:
                        yield return StartCoroutine(HandleReadyStep());
                        break;

                    case ELessonStep.RECORDING:
                        yield return StartCoroutine(HandleRecordingStep());
                        break;

                    case ELessonStep.RESULT:
                        yield return StartCoroutine(HandleLessonResultStep());
                        break;
                }
            }
            else if (recordingType == ERecordingType.Practice)
            {
                EPracticeStep currentStep = practiceStep;

                switch (currentStep)
                {
                    case EPracticeStep.GRIP:
                        yield return StartCoroutine(HandleGripStep());
                        break;

                    case EPracticeStep.SWING:
                        yield return StartCoroutine(HandleSwingStep());
                        break;

                    case EPracticeStep.RESULT:
                        yield return StartCoroutine(HandleResultStep());
                        break;
                }
            }
        }
    }

    #region Profile
    private IEnumerator HandleProfileGribStep()
    {
        if (recordingType == ERecordingType.Profile && _profileUseExternalRgb)
        {
            while (profileStep == EProfileStep.GRIP)
            {
                if (Input.GetKeyDown(KeyCode.F9))
                {
                    SetRecordingStep(EProfileStep.RESULT);
                    yield break;
                }

                yield return null;
            }

            yield break;
        }

        // -----------------------------

        float timer = 0f;
        float handDir = -1f;

        while (profileStep == EProfileStep.GRIP)
        {
            handDir = m_SensorProcess.iGetHandDirNF;

            _handCheck = m_SensorProcess.IsAddressHand();

            //m_DedugText1.text = $"hand : {handDir}, {_handCheck}";
            //m_DedugText2.text = $"vis : {m_SensorProcess.IsVisibility()}\r\nvFront : {m_SensorProcess.visibilityFront}\r\nvSide : {m_SensorProcess.visibilitySide}\r\nelbowF : {m_SensorProcess.fRightElbowFrontVis}\r\nelbowS : {m_SensorProcess.fLeftElbowSideVis}";

            //if (_handCheck && m_SensorProcess.iGetHandDir > 160 && m_SensorProcess.iGetHandDir < 200)
            if (m_SensorProcess.IsVisibility() && _handCheck && handDir > _swingStepData.dicTakeback["GetHandDir"])
            {
                timer += Time.deltaTime;

                if (timer > 0.8f)
                {
                    timer = 0.0f;
                    SetRecordingStep(EProfileStep.READY);
                    yield break;
                }
            }
            else
            {
                timer = 0f;
            }

            yield return null;
        }
    }

    private IEnumerator HandleProfileReadyStep()
    {
        float timer = 3f;

        float takeT = (_swingStepData != null && _swingStepData.dicTakeback != null && _swingStepData.dicTakeback.ContainsKey("GetHandDir"))
            ? _swingStepData.dicTakeback["GetHandDir"]
            : 0f;

        float startMin = takeT + 10f;

        while (profileStep == EProfileStep.READY)
        {
            float handDir = m_SensorProcess != null ? m_SensorProcess.iGetHandDirNF : -1f;
            bool okHand = (m_SensorProcess != null) ? m_SensorProcess.IsAddressHand() : false;

            if (!m_SensorProcess.IsVisibility() || !okHand || handDir < startMin)
            {
                m_ProfileCountText.text = string.Empty;
                SetRecordingStep(EProfileStep.GRIP);
                yield break;
            }
            else
            {
                timer -= Time.deltaTime;
                m_ProfileCountText.text = $"{timer:0}";
            }

            if (timer < 0f)
            {
                SetRecordingStep(EProfileStep.SWING);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator HandleProfileSWINGStep()
    {
        isRecording = true;
        checkTakeback = false;
        checkImpact = false;

        float handDir = -1f;

        StartCoroutine(CaptureFramesProfile());

        while (profileStep == EProfileStep.SWING)
        {
            if (isRecording)
            {
                handDir = m_SensorProcess.iGetHandDirNF;
                _handCheck = m_SensorProcess.IsAddressHand();

                if (!checkTakeback && _handCheck && handDir < _swingStepData.dicTakeback["GetHandDir"])
                {
                    checkTakeback = true;
                }

                if (checkTakeback && !checkImpact && handDir > _swingStepData.dicImpact["GetHandDir"])
                {
                    checkImpact = true;
                }
            }

            yield return null;
        }
    }

    private IEnumerator HandleProfileResultStep()
    {
        while (_profileCaptureCount > 0)
        {
            yield return null;
        }

        m_LoadingPanel.SetActive(true);

        if (recordingType == ERecordingType.Profile && _profileUseExternalRgb)
        {
            if (!TryImportProfileFramesFromRgb(out List<byte[]> rgbFront, out List<byte[]> rgbSide))
            {
                Debug.LogError("[ProfileRGB] Import failed.");
                m_LoadingPanel.SetActive(false);

                Back();
                yield break;
            }

            framesFront.Clear();
            framesSide.Clear();

            framesFront = rgbFront;
            framesSide = rgbSide;

            widthFront = _profileRgbWidthFront;
            heightFront = _profileRgbHeightFront;
            widthSide = _profileRgbWidthSide;
            heightSide = _profileRgbHeightSide;
        }

        _replayReadyFront = false;
        _replayReadySide = false;

        TryDeleteFile(_profileFrontTempPath);
        TryDeleteFile(_profileSideTempPath);

        List<byte[]> saveFrontFrames = new List<byte[]>(framesFront);
        List<byte[]> saveSideFrames = new List<byte[]>(framesSide);

        StartCoroutine(SendFramesToFFmpeg(_profileFrontTempPath, widthFront, heightFront, true, saveFrontFrames, () => _replayReadyFront = true));
        StartCoroutine(SendFramesToFFmpeg(_profileSideTempPath, widthSide, heightSide, false, saveSideFrames, () => _replayReadySide = true));

        yield return new WaitUntil(() => _replayReadyFront && _replayReadySide);

        m_LoadingPanel.SetActive(false);

        SetProfileResult();

        while (profileStep == EProfileStep.RESULT)
        {
            yield return null;
        }
    }

    private void StoreProfileFrames()
    {
        if (recordingType != ERecordingType.Profile)
            return;

        if (!_debugProfileAnalyze)
            return;

        if (framesFront == null || framesSide == null || framesFront.Count <= 0 || framesSide.Count != framesFront.Count)
        {
            return;
        }

        List<Texture2D> frontTextures = new List<Texture2D>(framesFront.Count);
        List<Texture2D> sideTextures = new List<Texture2D>(framesSide.Count);

        try
        {
            for (int i = 0; i < framesFront.Count; i++)
            {
                Texture2D frontTex = (framesFront[i] != null) ? CreateTextureFromRaw(framesFront[i], widthFront, heightFront) : null;
                Texture2D sideTex = (framesSide[i] != null) ? CreateTextureFromRaw(framesSide[i], widthSide, heightSide) : null;

                frontTextures.Add(frontTex);
                sideTextures.Add(sideTex);
            }

            ProfileVerifyBuffer.StoreFromTextures(frontTextures, sideTextures);
        }
        finally
        {
            for (int i = 0; i < frontTextures.Count; i++)
            {
                if (frontTextures[i] != null) Destroy(frontTextures[i]);
            }

            for (int i = 0; i < sideTextures.Count; i++)
            {
                if (sideTextures[i] != null) Destroy(sideTextures[i]);
            }
        }
    }

    private string GetProfileDebugRoot()
    {
        int uid = 0;
        if (GolfProDataManager.Instance != null && GolfProDataManager.Instance.SelectProData != null)
            uid = GolfProDataManager.Instance.SelectProData.uid;

        string root = Path.Combine(Application.persistentDataPath, "DebugProfileAnalyze", uid.ToString());

        if (!Directory.Exists(root))
            Directory.CreateDirectory(root);

        return root;
    }

    private string GetProfileDebugFramesDir()
    {
        string dir = Path.Combine(GetProfileDebugRoot(), "Frames");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return dir;
    }

    private string GetProfileDebugSelectedDir()
    {
        string dir = Path.Combine(GetProfileDebugRoot(), "Selected");

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        return dir;
    }

    private int GetPostImpactFrames()
    {
        switch (swingType)
        {
            case ESwingType.Half:
                return 20;

            case ESwingType.ThreeQuarter:
                return 20;

            case ESwingType.Full:
            default:
                return 20;
        }
    }

    private void SetProfileResult()
    {
        m_ProfileResultPanel.SetActive(true);
        m_ProfilePlayBtn.SetActive(true);

        m_ProfileKeywordText.text = $"{swingType} • {club}";

        if (m_ProfileVideoPlayer != null)
        {
            m_ProfileVideoPlayer.started -= OnProfileVideoStarted;
            m_ProfileVideoPlayer.started += OnProfileVideoStarted;
            m_ProfileVideoPlayer.Stop();
            m_ProfileVideoPlayer.targetDisplay = m_ProfilePreviewRawImage;
            m_ProfileVideoPlayer.playOnAwake = false;
            m_ProfileVideoPlayer.isLooping = false;
            m_ProfileVideoPlayer.url = _profileFrontTempPath;
            m_ProfileVideoPlayer.playbackSpeed = INI.PlaySpeedNormal;
            m_ProfileVideoPlayer.Play();
        }
    }

    private void OnProfileVideoStarted(VLCVideoPlayer player)
    {
        player.position = 0f;
        player.Pause();
        m_ProfilePlayBtn.SetActive(true);
    }

    public void OnClick_ProfilePreview()
    {
        if (m_ProfileVideoPlayer == null)
            return;

        if (m_ProfileVideoPlayer.isPlaying)
        {
            m_ProfileVideoPlayer.Pause();
            m_ProfilePlayBtn.SetActive(true);
        }
        else
        {
            if (m_ProfileVideoPlayer.position >= 0.999f)
                m_ProfileVideoPlayer.position = 0f;

            m_ProfileVideoPlayer.Play();
            m_ProfilePlayBtn.SetActive(false);
        }
    }

    private IEnumerator SaveProfileData()
    {
        m_LoadingPanel.SetActive(true);

        if (swingType == ESwingType.Full && club == EClub.MiddleIron)
        {
            StoreProfileFrames();

            SetResultData();

            yield return StartCoroutine(AnalyzeSwingFromAllFrames());

            SaveAllImages(true);

            if (SaveCsvFull(true))
            {
                SaveProfileAICsv();
                GolfProDataManager.Instance.ReloadProSwingData();
            }
            else
            {
                SaveProfileAICsv();
            }
        }

        if (m_ProfileVideoPlayer != null)
        {
            m_ProfileVideoPlayer.Stop();
        }

        TryDeleteFile(profileFrontPath);
        TryDeleteFile(profileSidePath);

        if (File.Exists(_profileFrontTempPath))
        {
            File.Move(_profileFrontTempPath, profileFrontPath);
        }

        if (File.Exists(_profileSideTempPath))
        {
            File.Move(_profileSideTempPath, profileSidePath);
        }

        ProfileVideos();

        m_LoadingPanel.SetActive(false);

        Back();
    }

    #endregion

    #region Lesson
    private IEnumerator HandleCheckStep()
    {
        SetReady();

        while (lessonStep == ELessonStep.CHECK)
        {
            yield return null;
        }
    }

    private IEnumerator HandleReadyStep()
    {
        _lessonView = ELessonView.FRONT;
        ApplyLessonViewUI();

        float timer = 3f;

        while (lessonStep == ELessonStep.READY)
        {
            timer -= Time.deltaTime;

            m_CountText.text = $"{timer:0}";

            if (timer < 0)
            {
                SetRecordingStep(ELessonStep.RECORDING);
            }

            yield return null;
        }
    }

    private IEnumerator HandleRecordingStep()
    {
        string str = string.Empty;
        float timer = 0;
        isRecording = true;

        _pendingConfirmed = false;
        _pendingVideoReady = false;
        TryDeleteFile(_pendingVideoTempPath);
        TryDeleteFile(_audioTempWavPath);
        TryDeleteFile(_muxTempVideoPath);

        ApplyLessonViewUI();

        StartCoroutine(CaptureFrames());

        //AutoSelectMicDevice();
        //StartMicRecording();
        yield return StartCoroutine(AutoSelectAndStartMicRecording());

        while (lessonStep == ELessonStep.RECORDING)
        {
            if (isRecording)
            {
                timer += Time.deltaTime;
                int hour = Mathf.FloorToInt(timer / 3600);
                int min = Mathf.FloorToInt((timer % 3600) / 60);
                int sec = Mathf.FloorToInt(timer % 60);

                str = $"{hour:00}:{min:00}:{sec:00}";
                m_TimerText.text = str;

                blinkTimer += Time.deltaTime;

                if (blinkTimer >= blinkTime)
                {
                    m_RedDot.enabled = !m_RedDot.enabled;
                    blinkTimer = 0.0f;
                }
            }
            else
            {
                yield return new WaitUntil(() => _replayReadyFront);

                StopMicAndSaveWav();

                if (!string.IsNullOrEmpty(_audioTempWavPath) && File.Exists(_audioTempWavPath))
                {
                    yield return StartCoroutine(MuxVideoAndAudio(_pendingVideoTempPath, _audioTempWavPath));
                }

                yield return new WaitForSeconds(0.3f);
                videoTimeStr = str.Substring(str.IndexOf(':') + 1);
                m_TopObj.SetActive(false);
                m_LoadingPanel.SetActive(false);
                SetRecordingStep(ELessonStep.RESULT);
            }

            yield return null;
        }
    }

    private IEnumerator HandleLessonResultStep()
    {
        m_LessonVideoThumbnail.texture = captureFront;
        m_VideoTimeText.text = videoTimeStr;
        m_SubjectInput.text = string.Empty;

        while (lessonStep == ELessonStep.RESULT)
        {
            yield return null;
        }
    }

    public void SetKeyword()
    {
        m_KeywordInput.text = $"{Utillity.Instance.ConvertEnumToString(keywordPose)} • {Utillity.Instance.ConvertEnumToString(keywordClub)}";
    }

    public void OnValueChanged_Keyword(Toggle toggle, bool isOn)
    {
        UIValueObject valueObj = toggle.GetComponent<UIValueObject>();

        if (!isOn)
            return;

        if (valueObj.boolValue)
            keywordPose = (EStance)valueObj.intValue;
        else
            keywordClub = (EClub)valueObj.intValue;

        //Debug.Log($"{(int)keywordPose} • {(int)keywordClub}");
        //Debug.Log($"{Utillity.Instance.ConvertEnumToString(keywordPose)} • {Utillity.Instance.ConvertEnumToString(keywordClub)}");

        SetKeyword();
    }
    #endregion

    #region Practice
    private IEnumerator HandleGripStep()
    {
        SetReady();

        List<SWINGSTEP> swingStep = isSelectRetake ? selectSwingStep : GameManager.Instance.Stance;

        float timer = 0f;

        while (practiceStep == EPracticeStep.GRIP)
        {
            if ((m_SensorProcess.iGetHandDir > 160 && m_SensorProcess.iGetHandDir < 200)
                       && (m_SensorProcess.IsAddressHand())
                       && (m_SensorProcess.visibilityFront > 0.8f && m_SensorProcess.visibilitySide > 0.7f))
            {
                timer += Time.deltaTime;

                if (timer > 0.8f)
                {
                    m_ClubImg.sprite = m_ClubSprites[(int)club];

                    for (int i = 0; i < m_StepToggles.Length; i++)
                    {
                        if (!swingStep.Contains((SWINGSTEP)i))
                        {
                            m_StepToggles[i].gameObject.SetActive(false);
                        }
                    }

                    SetResultData();
                    SetRecordingStep(EPracticeStep.SWING);

                    yield break;
                }
            }
            else
            {
                timer = 0f;
            }

            yield return null;
        }
    }

    private IEnumerator HandleSwingStep()
    {
        List<SWINGSTEP> swingStep = isSelectRetake ? selectSwingStep : GameManager.Instance.Stance;

        float timer = 0f;

        foreach (SWINGSTEP step in swingStep)
        {
            m_StepToggles[(int)step].isOn = true;

            m_InfoText.text = $"{Utillity.Instance.ConvertEnumToString(step)} 자세를 잡아주세요";

            yield return new WaitForSeconds(2.0f);

            m_InfoText.text = $"{Utillity.Instance.ConvertEnumToString(step)} 자세를 인식중입니다...";

            m_SwingProgress.SetActive(true);
            timer = 0.0f;

            while (true)
            {
                timer += Time.deltaTime;

                if (step == SWINGSTEP.FINISH)
                    m_ProgressBarImg.fillAmount = Mathf.Clamp01(timer);
                else
                    m_ProgressBarImg.fillAmount = Mathf.Clamp01(timer / 1.0f);

                if (timer >= ((step == SWINGSTEP.FINISH) ? 1f : 1.0f))
                {
                    timer = 0.0f;
                    m_InfoText.text = $"{Utillity.Instance.ConvertEnumToString(step)} 완료!";
                    m_Check.SetActive(true);

                    CaptureFrameFront(step);
                    CaptureFrameSide(step);

                    AudioManager.Instance.PlayNext();
                    break;
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.5f);

            m_ProgressBarImg.fillAmount = 0.0f;
            m_Check.SetActive(false);
            m_SwingProgress.SetActive(false);
            m_InfoText.text = string.Empty;

            bool wrote = false;

            if (captureFrontDic.TryGetValue(step, out Texture2D stepFront) &&
                captureSideDic.TryGetValue(step, out Texture2D stepSide) &&
                stepFront != null && stepSide != null)
            {
                ExtractLandmarks(stepFront, stepSide, out front2D, out side2D, out front3D, out side3D);

                if (front2D != null && side2D != null && front3D != null && side3D != null)
                {
                    bool landmarkSaved = SavePracticeLandmarkCsv(step, front2D, front3D, side2D, side3D);

                    if (!landmarkSaved)
                    {
                        Debug.Log($"landmark csv save failed : {step}");
                    }

                    m_SensorProcess.UpdateSensor(in front2D, in side2D, in front3D, in side3D);
                    ApplySensorValuesToResult((int)step);
                    wrote = true;

                    SavePracticeAnalyzeDebugJpg(step, stepFront, stepSide);
                }
            }

            if (step == swingStep.Last())
            {
                if (SaveCsvFull())
                {
                    GolfProDataManager.Instance.ReloadProSwingData();
                    SetRecordingStep(EPracticeStep.RESULT);
                }
                else
                {
                    SetRecordingStep(EPracticeStep.GRIP);
                }

                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator HandleResultStep()
    {
        SetResult();

        while (practiceStep == EPracticeStep.RESULT)
        {
            yield return null;
        }
    }

    private void SavePracticeAnalyzeDebugJpg(SWINGSTEP step, Texture2D frontTex, Texture2D sideTex)
    {
        if (!_debugSavePracticeAnalyzeJpg)
            return;

        try
        {
            int uid = 0;
            if (GolfProDataManager.Instance != null && GolfProDataManager.Instance.SelectProData != null)
            {
                uid = GolfProDataManager.Instance.SelectProData.uid;
            }

            string root = Path.Combine(Application.persistentDataPath, "DebugPracticeAnalyzeFrames", uid.ToString());
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            string stepName = step.ToString();

            if (frontTex != null)
            {
                Texture2D vflip = FlipTextureVertical(frontTex);
                string fp = Path.Combine(root, $"P_{stepName}_F.jpg");
                WriteJpg(vflip, fp, 85);
                Destroy(vflip);
            }

            if (sideTex != null)
            {
                Texture2D vflip = FlipTextureVertical(sideTex);
                string sp = Path.Combine(root, $"P_{stepName}_S.jpg");
                WriteJpg(vflip, sp, 85);
                Destroy(vflip);
            }
        }
        catch (Exception e)
        {
            Debug.Log("[PracticeDebugJPG] Save failed: " + e.Message);
        }
    }
    #endregion

    private IEnumerator AnalyzeSwingFromAllFrames()
    {
        if (framesFront == null || framesFront.Count <= 0)
        {
            Debug.Log("[AnalyzeSwingFromAllFrames] No frames.");
            yield break;
        }

        int frameCount = framesFront.Count;

        _profileAnalyzeSnapshots.Clear();
        for (int i = 0; i < frameCount; i++)
        {
            _profileAnalyzeSnapshots.Add(default(FrameAnalyzeSnapshot));
        }

        if (framesSide == null || framesSide.Count != frameCount)
        {
            yield break;
        }

        float[] proTargets = BuildProHandDirTargets();

        List<int> handNF = new List<int>(frameCount);
        List<string> frontJpgNames = new List<string>(frameCount);
        List<string> sideJpgNames = new List<string>(frameCount);

        string framesDir = null;

        if (_debugProfileAnalyze)
        {
            framesDir = GetProfileDebugFramesDir();
        }

        for (int i = 0; i < frameCount; i++)
        {
            byte[] fBytes = framesFront[i];
            byte[] sBytes = framesSide[i];

            Texture2D f = (fBytes != null) ? CreateTextureFromRaw(fBytes, widthFront, heightFront) : null;
            Texture2D s = (sBytes != null) ? CreateTextureFromRaw(sBytes, widthSide, heightSide) : null;

            if (f == null || s == null)
            {
                if (f != null)
                    Destroy(f);

                if (s != null)
                    Destroy(s);

                handNF.Add(-1);
                frontJpgNames.Add(string.Empty);
                sideJpgNames.Add(string.Empty);
                continue;
            }

            try
            {
                string fName = $"F_{i:0000}.jpg";
                string sName = $"S_{i:0000}.jpg";

                if (_debugProfileAnalyze && framesDir != null)
                {
                    Texture2D fSave = FlipTextureVertical(f);
                    Texture2D sSave = FlipTextureVertical(s);

                    WriteJpg(fSave, Path.Combine(framesDir, fName), 85);
                    WriteJpg(sSave, Path.Combine(framesDir, sName), 85);

                    Destroy(fSave);
                    Destroy(sSave);
                }

                frontJpgNames.Add(fName);
                sideJpgNames.Add(sName);

                Landmark2D[] f2;
                Landmark2D[] s2;
                Landmark3D[] f3;
                Landmark3D[] s3;

                ExtractLandmarks(f, s, out f2, out s2, out f3, out s3);

                int curHand = -1;

                if (f2 != null && s2 != null && f3 != null && s3 != null)
                {
                    m_SensorProcess.UpdateSensor(in f2, in s2, in f3, in s3);
                    //curHand = m_SensorProcess.iGetHandDirNF;

                    FrameAnalyzeSnapshot snapshot = CaptureAnalyzeSnapshot();
                    _profileAnalyzeSnapshots[i] = snapshot;

                    curHand = snapshot.handDirNF;
                }

                handNF.Add(curHand);
            }
            finally
            {
                Destroy(f);
                Destroy(s);
            }

            if (i % 10 == 0)
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
            if (start > end) return -1;

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

        if (stepIndex[2] >= 0) stepIndex[3] = stepIndex[2];

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

        if (stepIndex[4] >= 0) stepIndex[5] = stepIndex[4];

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

        stepIndex[7] = frameCount - 1;

        if (_debugProfileAnalyze)
        {
            SaveProfileHandDirCompareCsv_Full(handNF, proTargets, stepIndex, frontJpgNames, sideJpgNames);
            SaveProfileSelectedStepJpg_Full(stepIndex);
        }

        ProfileSelectedFramesToDic(stepIndex);

        for (int s = 0; s < 8; s++)
        {
            int idx = stepIndex[s];

            if (idx < 0)
                continue;

            if (idx >= _profileAnalyzeSnapshots.Count)
                continue;

            FrameAnalyzeSnapshot snapshot = _profileAnalyzeSnapshots[idx];

            if (!snapshot.isValid)
                continue;

            ApplySnapshotToResult(s, snapshot);

            if (s % 2 == 0)
                yield return null;
        }
    }

    private float[] BuildProHandDirTargets()
    {
        float[] targets = new float[8];

        for (int i = 0; i < 8; i++)
        {
            targets[i] = -1f;
        }

        if (_swingStepData == null)
        {
            return targets;
        }

        targets[(int)SWINGSTEP.ADDRESS] = GetProHandDirFromDic(_swingStepData.dicAddress);
        targets[(int)SWINGSTEP.TAKEBACK] = GetProHandDirFromDic(_swingStepData.dicTakeback);
        targets[(int)SWINGSTEP.BACKSWING] = GetProHandDirFromDic(_swingStepData.dicBackswing);
        targets[(int)SWINGSTEP.TOP] = GetProHandDirFromDic(_swingStepData.dicTop);
        targets[(int)SWINGSTEP.DOWNSWING] = GetProHandDirFromDic(_swingStepData.dicDownswing);
        targets[(int)SWINGSTEP.IMPACT] = GetProHandDirFromDic(_swingStepData.dicImpact);
        targets[(int)SWINGSTEP.FOLLOW] = GetProHandDirFromDic(_swingStepData.dicFollow);
        targets[(int)SWINGSTEP.FINISH] = GetProHandDirFromDic(_swingStepData.dicFinish);

        //targets[(int)SWINGSTEP.TOP] = targets[(int)SWINGSTEP.BACKSWING];
        //targets[(int)SWINGSTEP.IMPACT] = targets[(int)SWINGSTEP.DOWNSWING];

        return targets;
    }

    private float GetProHandDirFromDic(Dictionary<string, int> dic)
    {
        if (dic == null)
            return -1f;

        if (dic.TryGetValue("GetHandDir", out int v))
        {
            return v;
        }

        return -1f;
    }

    // Profile csv
    private void ProfileVideos()
    {
        string frontFile = Path.GetFileName(profileFrontPath);
        string sideFile = Path.GetFileName(profileSidePath);

        bool hasFront = !string.IsNullOrEmpty(profileFrontPath) && File.Exists(profileFrontPath);
        bool hasSide = !string.IsNullOrEmpty(profileSidePath) && File.Exists(profileSidePath);

        if (!hasFront && !hasSide)
            return;

        int uid = 0;

        if (GolfProDataManager.Instance != null && GolfProDataManager.Instance.SelectProData != null)
            uid = GolfProDataManager.Instance.SelectProData.uid;

        string baseName = $"profile_{uid}_{(int)swingType}_{(int)club}";
        string recently = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

        //const int sceneType = 12;
        const int videoType = 0;
        const int clubFilter = 3;
        const int favoriteCount = 0;
        const int views = 0;

        if (hasFront)
            UpsertVideoRowByPath(frontFile, baseName + "_front", 0, videoType, clubFilter, (int)swingType, favoriteCount, views, recently);

        if (hasSide)
            UpsertVideoRowByPath(sideFile, baseName + "_side", 1, videoType, clubFilter, (int)swingType, favoriteCount, views, recently);

        GolfProDataManager.Instance.ReloadProVideoData();
    }

    private void UpsertVideoRowByPath(string fileName, string name, int direction, int videoType, int clubFilter,
                                      int poseFilter, int favoriteCount, int views, string recently)
    {
        if (string.IsNullOrEmpty(videoCSVPath) || string.IsNullOrEmpty(fileName))
            return;

        if (!File.Exists(videoCSVPath) || new FileInfo(videoCSVPath).Length == 0)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(videoCSVPath));
            File.WriteAllText(videoCSVPath, "ID,NAME,PATH,DIRECTION,VIDEOTYPE,SWINGTYPE,CLUBFILTER,POSEFILTER,FAVORITECOUNT,VIEWS,RECENTLY\n", new UTF8Encoding(true));
        }

        List<string> lines = File.ReadAllLines(videoCSVPath, new UTF8Encoding(true)).ToList();

        if (lines.Count == 0)
        {
            lines.Add("ID,NAME,PATH,DIRECTION,VIDEOTYPE,SWINGTYPE,CLUBFILTER,POSEFILTER,FAVORITECOUNT,VIEWS,RECENTLY");
        }

        int idx = -1;
        int id = -1;

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] c = lines[i].Split(',');

            if (c.Length < 10)
                continue;

            if (string.Equals(c[2], fileName, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                int.TryParse(c[0], out id);

                break;
            }
        }

        if (id <= 0)
            id = GetNextId();

        string newLine = $"{id},{name},{fileName},{direction},{videoType},{(int)swingType},{clubFilter},{poseFilter},{favoriteCount},{views},{recently}";

        if (idx >= 0)
            lines[idx] = newLine;
        else
            lines.Add(newLine);

        File.WriteAllLines(videoCSVPath, lines, new UTF8Encoding(true));
    }

    private void SaveProfileHandDirCompareCsv_Full(List<int> handNF, float[] proTargets, int[] stepIndex, List<string> frontJpgNames, List<string> sideJpgNames)
    {
        try
        {
            string root = GetProfileDebugRoot();
            string fp = Path.Combine(root, "HandDir_Compare_Profile.csv");

            Dictionary<int, string> selectedMap = new Dictionary<int, string>();
            for (int s = 0; s < 8; s++)
            {
                int idx = stepIndex[s];

                if (idx >= 0 && !selectedMap.ContainsKey(idx))
                {
                    selectedMap.Add(idx, ((SWINGSTEP)s).ToString());
                }
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("FrameIndex,HandDir,FrontJpg,SideJpg,SelectedStep");

            for (int i = 0; i < handNF.Count; i++)
            {
                string step = selectedMap.ContainsKey(i) ? selectedMap[i] : "";
                string fj = (frontJpgNames != null && i < frontJpgNames.Count) ? frontJpgNames[i] : "";
                string sj = (sideJpgNames != null && i < sideJpgNames.Count) ? sideJpgNames[i] : "";
                sb.AppendLine($"{i},{handNF[i]},{fj},{sj},{step}");
            }

            sb.AppendLine();
            sb.AppendLine("Step,Target,SelectedIndex,SelectedHandDir");

            for (int s = 0; s < 8; s++)
            {
                int idx = stepIndex[s];
                int hv = (idx >= 0 && idx < handNF.Count) ? handNF[idx] : -1;
                sb.AppendLine($"{((SWINGSTEP)s).ToString()},{proTargets[s]},{idx},{hv}");
            }

            File.WriteAllText(fp, sb.ToString(), new UTF8Encoding(true));
            Debug.Log("[ProfileAnalyze] CSV saved: " + fp);
        }
        catch (Exception e)
        {
            Debug.Log("[ProfileAnalyze] CSV save failed: " + e.Message);
        }
    }

    private void SaveProfileSelectedStepJpg_Full(int[] stepIndex)
    {
        try
        {
            string dir = GetProfileDebugSelectedDir();

            for (int s = 0; s < 8; s++)
            {
                int idx = stepIndex[s];

                if (idx < 0 || idx >= framesFront.Count || idx >= framesSide.Count)
                    continue;

                byte[] frontBytes = framesFront[idx];
                byte[] sideBytes = framesSide[idx];

                if (frontBytes == null || sideBytes == null)
                    continue;

                Texture2D f = CreateTextureFromRaw(frontBytes, widthFront, heightFront);
                Texture2D si = CreateTextureFromRaw(sideBytes, widthSide, heightSide);

                if (f == null || si == null)
                {
                    if (f != null) Destroy(f);
                    if (si != null) Destroy(si);
                    continue;
                }

                try
                {
                    string stepName = ((SWINGSTEP)s).ToString();

                    Texture2D fSave = FlipTextureVertical(f);
                    Texture2D sSave = FlipTextureVertical(si);

                    WriteJpg(fSave, Path.Combine(dir, $"SEL_{stepName}_F.jpg"), 90);
                    WriteJpg(sSave, Path.Combine(dir, $"SEL_{stepName}_S.jpg"), 90);

                    Destroy(fSave);
                    Destroy(sSave);
                }
                finally
                {
                    Destroy(f);
                    Destroy(si);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("[ProfileAnalyze] Selected step JPG failed: " + e.Message);
        }
    }

    private static void WriteJpg(Texture2D tex, string fullPath, int quality)
    {
        if (tex == null || string.IsNullOrEmpty(fullPath))
            return;

        byte[] bytes = tex.EncodeToJPG(Mathf.Clamp(quality, 10, 100));
        File.WriteAllBytes(fullPath, bytes);
    }

    private static Texture2D FlipTextureVertical(Texture2D src)
    {
        int w = src.width;
        int h = src.height;

        Texture2D dst = new Texture2D(w, h, TextureFormat.RGB24, false);

        Color32[] srcPixels = src.GetPixels32();
        Color32[] dstPixels = new Color32[srcPixels.Length];

        for (int y = 0; y < h; y++)
        {
            int srcRow = y * w;
            int dstRow = (h - 1 - y) * w;

            for (int x = 0; x < w; x++)
            {
                dstPixels[dstRow + x] = srcPixels[srcRow + x];
            }
        }

        dst.SetPixels32(dstPixels);
        dst.Apply(false, false);

        return dst;
    }

    private static Texture2D MirrorX(Texture2D src)
    {
        if (src == null)
        {
            return null;
        }

        int w = src.width;
        int h = src.height;

        Texture2D dst = new Texture2D(w, h, TextureFormat.RGB24, false);

        Color32[] sp = src.GetPixels32();
        Color32[] dp = new Color32[sp.Length];

        for (int y = 0; y < h; y++)
        {
            int row = y * w;

            for (int x = 0; x < w; x++)
            {
                dp[row + x] = sp[row + (w - 1 - x)];
            }
        }

        dst.SetPixels32(dp);
        dst.Apply(false, false);

        return dst;
    }

    private void SetResult()
    {
        m_TopObj.SetActive(false);
        m_Main.SetActive(!isReviewing);
        m_SelectRetake.SetActive(false);
        m_ReviewCancle.SetActive(isReviewing);

        m_InfoImage.sprite = isReviewing ? m_InfoSprites[1] : m_InfoSprites[0];
        m_ResultInfoText.text = isReviewing ? "영상을 검토중입니다." : "영상촬영이 완료되었습니다.";
        m_ResultInfoSubText.text = isReviewing ? "영업일 기준 최대 2일 이내 검토가 완료됩니다." : "데이터 분석을 위해 검토를 요청해주세요.";

        if (isReviewing)
        {
            m_InfoImage.sprite = m_InfoSprites[1];
            m_InfoText.text = "영상을 검토중입니다.";
        }
        else
        {
            m_InfoImage.sprite = m_InfoSprites[0];
            m_InfoText.text = "영상을 검토중입니다.";
        }

        List<SwingCardViewer.CardData> newList = new List<SwingCardViewer.CardData>();

        if (!isSelectRetake)
        {
            int cnt = 0;

            foreach (SWINGSTEP step in GameManager.Instance.Stance)
            {
                int iStep = (int)step;

                m_CaptureObjs[iStep].SetActive(true);

                m_CaptureRawImages[(int)step].texture = captureFrontDic[step];

                newList.Add(new SwingCardViewer.CardData(Utillity.Instance.ConvertEnumToString(step), captureFrontDic[step]));
            }

            m_SwingCardViewer.SetCardList(newList);

            foreach (SWINGSTEP step in GameManager.Instance.Stance)
            {
                int iStep = (int)step;
                int localIndex = cnt;

                m_CaptureObjs[iStep].GetComponent<Button>().onClick.RemoveAllListeners();
                m_CaptureObjs[iStep].GetComponent<Button>().onClick.AddListener(() => m_SwingCardViewer.ShowAtIndex(localIndex));

                cnt++;
            }
        }
        else
        {
            foreach (SWINGSTEP step in selectSwingStep)
            {
                m_CaptureRawImages[(int)step].texture = captureFrontDic[step];
            }

            foreach (SWINGSTEP step in GameManager.Instance.Stance)
            {
                newList.Add(new SwingCardViewer.CardData(step.ToString(), captureFrontDic[step]));
            }

            m_SwingCardViewer.SetCardList(newList);
        }

        for (int i = 0; i < m_CaptureChecks.Length; i++)
        {
            m_CaptureChecks[i].SetActive(false);
            m_CheckToggles[i].isOn = false;
        }
    }

    private void SetRecordingStep(Enum filter)
    {
        if (filter is ELessonStep LessonStep)
        {
            switch (filter)
            {
                case ELessonStep.CHECK:
                case ELessonStep.READY:
                case ELessonStep.RECORDING:
                case ELessonStep.RESULT:
                    NewShowPanel(LessonStep);

                    lessonStep = LessonStep;
                    break;
            }
        }
        else if (filter is EPracticeStep PracticeStep)
        {
            switch (filter)
            {
                case EPracticeStep.GRIP:
                case EPracticeStep.SWING:
                case EPracticeStep.RESULT:
                    NewShowPanel(filter);

                    practiceStep = PracticeStep;
                    break;
            }
        }
        else if (filter is EProfileStep ProfileStep)
        {
            switch (filter)
            {
                case EProfileStep.GRIP:
                case EProfileStep.READY:
                case EProfileStep.SWING:
                case EProfileStep.RESULT:
                    NewShowPanel(filter);

                    profileStep = ProfileStep;
                    break;
            }
        }
    }

    private void NewShowPanel(Enum filter)
    {
        if (filter is ELessonStep)
        {
            m_CheckPanel.SetActive(false);
            m_ReadyPanel.SetActive(false);
            m_RecordingPanel.SetActive(false);
            m_LessonResultPanel.SetActive(false);

            switch (filter)
            {
                case ELessonStep.CHECK:
                    m_CheckPanel.SetActive(true);
                    break;

                case ELessonStep.READY:
                    m_ReadyPanel.SetActive(true);
                    break;

                case ELessonStep.RECORDING:
                    m_RecordingPanel.SetActive(true);
                    break;

                case ELessonStep.RESULT:
                    m_LessonResultPanel.SetActive(true);
                    break;
            }
        }
        else if (filter is EPracticeStep)
        {
            m_GuidePanel.SetActive(false);
            m_SwingPanel.SetActive(false);
            m_PracticeResultPanel.SetActive(false);

            switch (filter)
            {
                case EPracticeStep.GRIP:
                    m_GuidePanel.SetActive(true);
                    break;

                case EPracticeStep.SWING:
                    m_InfoText.text = string.Empty;
                    m_SwingPanel.SetActive(true);
                    break;

                case EPracticeStep.RESULT:
                    m_PracticeResultPanel.SetActive(true);
                    break;
            }
        }
        else if (filter is EProfileStep)
        {
            m_ProfileGribPanel.SetActive(false);
            m_ProfileReadyPanel.SetActive(false);
            m_ProfileSwingPanel.SetActive(false);
            m_ProfileResultPanel.SetActive(false);

            switch (filter)
            {
                case EProfileStep.GRIP:
                    m_ProfileGribPanel.SetActive(true);
                    break;

                case EProfileStep.READY:
                    m_ProfileReadyPanel.SetActive(true);
                    break;

                case EProfileStep.SWING:
                    m_ProfileSwingPanel.SetActive(true);
                    break;

                case EProfileStep.RESULT:
                    //m_ProfileResultPanel.SetActive(true);
                    break;
            }
        }
    }

    public void ProceedReview()
    {
        Debug.Log($"검토중");

        if (recordingType == ERecordingType.Lesson)
        {
            _pendingConfirmed = true;
            StartCoroutine(SaveRecordingVideo());

            return;
        }
        else if (recordingType == ERecordingType.Practice)
        {
            SaveAllImages();
            GolfProDataManager.Instance.ReloadProSwingData();
            Back();
        }
        else if (recordingType == ERecordingType.Profile)
        {
            StartCoroutine(SaveProfileData());
        }
    }

    public void ProceedRetake()
    {
        isSelectRetake = false;
        selectSwingStep.Clear();
        SetRecordingStep(EPracticeStep.GRIP);
    }

    public void ProceedSelectRetake()
    {
        isSelectRetake = true;
        SetRecordingStep(EPracticeStep.GRIP);
    }

    public void CancleReview()
    {
        isReviewing = false;

        isSelectRetake = false;
        SetRecordingStep(EPracticeStep.GRIP);
    }

    public void Back()
    {
        if (recordingType == ERecordingType.Lesson && !_pendingConfirmed)
        {
            DiscardPendingRecording();
        }

        if (recordingType == ERecordingType.Profile)
        {
            if (m_ProfileVideoPlayer != null)
            {
                m_ProfileVideoPlayer.Stop();
            }

            TryDeleteFile(_profileFrontTempPath);
            TryDeleteFile(_profileSideTempPath);
        }

        SceneManager.LoadScene(GameManager.Instance.SelectedSceneName);
    }

    public void Onclick_Button(string name)
    {
        switch (name)
        {
            case "Home":
                GameManager.Instance.SelectedSceneName = string.Empty;
                SceneManager.LoadScene("ModeSelect");
                break;

            case "Back":
                if (recordingType == ERecordingType.Lesson)
                {
                    m_PopupControl.ShowPopup(recordingStopStr, Utillity.Instance.HexToRGB(INI.Red), Back);
                }
                else if (recordingType == ERecordingType.Practice)
                {
                    if (practiceStep == EPracticeStep.GRIP || practiceStep == EPracticeStep.SWING)
                    {
                        Back();
                    }
                    else
                    {
                        if (isReviewing)
                            Back();
                        else
                            m_PopupControl.ShowPopup(backStr, Utillity.Instance.HexToRGB(INI.Red), Back);
                    }
                }
                else if (recordingType == ERecordingType.Profile)
                {
                    m_PopupControl.ShowPopup(recordingStopStr, Utillity.Instance.HexToRGB(INI.Red), Back);
                }
                break;

            case "Review":
                if (recordingType == ERecordingType.Lesson)
                {
                    m_PopupControl.ShowPopup(saveVideoStr, Utillity.Instance.HexToRGB(INI.Red), ProceedReview);
                }
                else if (recordingType == ERecordingType.Practice)
                {
                    m_PopupControl.ShowPopup(reviewStr, Utillity.Instance.HexToRGB(INI.Red), ProceedReview);
                }
                else if (recordingType == ERecordingType.Profile)
                {
                    m_PopupControl.ShowPopup(saveVideoStr, Utillity.Instance.HexToRGB(INI.Red), ProceedReview);
                }
                break;

            case "Review_Cancle":
                m_PopupControl.ShowPopup(reviewCancleStr, Utillity.Instance.HexToRGB(INI.Red), CancleReview);
                break;

            case "Retake":
                m_PopupControl.ShowPopup(retakeStr, Utillity.Instance.HexToRGB(INI.Red), ProceedRetake);
                break;

            case "Select_Retake":
                for (int i = 0; i < m_CaptureChecks.Length; i++)
                {
                    m_CaptureChecks[i].SetActive(true);
                    m_CheckToggles[i].isOn = false;
                }

                selectSwingStep.Clear();
                m_Main.SetActive(false);
                m_SelectRetake.SetActive(true);
                break;

            case "Record":
                if (recordingType == ERecordingType.Lesson)
                {
                    SetRecordingStep(ELessonStep.READY);
                }
                else if (recordingType == ERecordingType.Practice)
                {
                    if (selectSwingStep.Count == 0)
                        m_PopupControl.ShowTopPanel(selectSwingStepStr);
                    else
                        m_PopupControl.ShowPopup(retakeStr, Utillity.Instance.HexToRGB(INI.Red), ProceedSelectRetake);
                }
                break;

            case "RecordFinish":
                isFinish = true;
                m_LoadingPanel.SetActive(true);
                break;

            case "Cancle":
                foreach (GameObject item in m_CaptureChecks)
                {
                    item.SetActive(false);
                }

                m_Main.SetActive(true);
                m_SelectRetake.SetActive(false);
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;

            case "SwitchView":
                OnClick_LessonSwitchView();
                break;
        }
    }

    public void OnClick_Popup(int pop)
    {
        if (string.IsNullOrEmpty(m_KeywordInput.text))
        {
            SetKeyword();
        }

        m_PopupObjs[pop].SetActive(!m_PopupObjs[pop].activeInHierarchy);
    }

    public void OnValueChanged_Check(int idx, bool isOn)
    {
        int value = m_CheckToggles[idx].GetComponent<UIValueObject>().intValue;

        if (isOn)
        {
            if (!selectSwingStep.Contains((SWINGSTEP)value))
                selectSwingStep.Add((SWINGSTEP)value);

            selectSwingStep.Sort();
        }
        else
        {
            if (selectSwingStep.Contains((SWINGSTEP)value))
                selectSwingStep.Remove((SWINGSTEP)value);
        }
    }

    private void ApplyLessonViewUI()
    {
        bool isFront = (_lessonView == ELessonView.FRONT);

        if (rawImageFront != null)
        {
            rawImageFront.gameObject.SetActive(isFront);
        }

        if (rawImageSide != null)
        {
            rawImageSide.gameObject.SetActive(!isFront);
        }
    }

    public void OnClick_LessonSwitchView()
    {
        if (recordingType != ERecordingType.Lesson)
            return;

        if (_lessonView == ELessonView.FRONT)
        {
            _lessonView = ELessonView.SIDE;
        }
        else
        {
            _lessonView = ELessonView.FRONT;
        }

        ApplyLessonViewUI();
    }

    IEnumerator SaveRecordingVideo()
    {
        m_VirtualKeyboard.SetActive(false);
        m_LoadingPanel.SetActive(true);

        string baseFileName = BuildSafeFileNameFromSubject();

        if (string.IsNullOrEmpty(baseFileName))
        {
            m_LoadingPanel.SetActive(false);
            m_PopupControl.ShowTopPanel(invalidSubjectStr);
            yield break;
        }

        string directory = Path.GetDirectoryName(videoCSVPath);
        if (string.IsNullOrEmpty(directory))
        {
            directory = Path.GetDirectoryName(_pendingVideoTempPath);
        }

        if (string.IsNullOrEmpty(directory))
        {
            m_LoadingPanel.SetActive(false);
            yield break;
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string finalPath = Path.Combine(directory, baseFileName + ".mp4");

        if (File.Exists(finalPath))
        {
            m_LoadingPanel.SetActive(false);
            m_PopupControl.ShowTopPanel(duplicateNameStr);
            yield break;
        }

        if (_pendingConfirmed && File.Exists(_pendingVideoTempPath))
        {
            try
            {
                File.Move(_pendingVideoTempPath, finalPath);
                _pendingVideoFinalPath = finalPath;
            }
            catch (Exception e)
            {
                Debug.Log("[VIDEO] Move temp->final failed: " + e.Message);
                m_LoadingPanel.SetActive(false);
                yield break;
            }
        }
        else
        {
            m_LoadingPanel.SetActive(false);
            Debug.Log("[VIDEO] SaveRecordingVideo: temp file not ready.");
            yield break;
        }

        yield return new WaitForSeconds(2.0f);

        AppendCSVRow(
            Path.GetFileNameWithoutExtension(finalPath),
            Path.GetFileName(finalPath),
            2, 1, (int)keywordClub, (int)keywordPose, 0, 0,
            DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
        );

        GolfProDataManager.Instance.ReloadProVideoData();

        isFinish = false;
        m_LoadingPanel.SetActive(false);

        SetRecordingStep(ELessonStep.CHECK);
    }

    private string BuildSafeFileNameFromSubject()
    {
        if (m_SubjectInput == null)
        {
            return null;
        }

        string raw = m_SubjectInput.text;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        raw = raw.Trim();

        char[] invalidChars = Path.GetInvalidFileNameChars();

        for (int i = 0; i < invalidChars.Length; i++)
        {
            char invalid = invalidChars[i];
            raw = raw.Replace(invalid, '_');
        }

        raw = raw.Replace(' ', '_');
        raw = raw.Trim('_');

        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return raw;
    }

    public void AppendCSVRow(string name, string path, int direction, int videoType, int clubFilter,
                          int poseFilter, int favoriteCount, int views, string recently)
    {
        int newId = GetNextId();

        string newRow = $"{newId},{name},{path},{direction},{videoType},{(int)swingType},{clubFilter},{poseFilter},{favoriteCount},{views},{recently}";

        using (StreamWriter sw = new StreamWriter(videoCSVPath, true, new UTF8Encoding(true)))
        {
            sw.WriteLine(newRow);
        }
    }

    private int GetNextId()
    {
        if (!File.Exists(videoCSVPath))
            return 10001;

        IEnumerable<string> lines = File.ReadAllLines(videoCSVPath, new UTF8Encoding(true)).Skip(1);

        if (!lines.Any())
            return 10001;

        int maxId = lines
            .Select(line => int.TryParse(line.Split(',')[0], out int id) ? id : 0)
            .Max();

        return maxId + 1;
    }

    void SetResultData()
    {
        if (!isSelectRetake)
        {
            ResultProData.Clear();
            ResultProData.Add("GetHandDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetHandDistance", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetShoulderDistance", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetSpineDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetShoulderAngle", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetFootDisRate", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetWeight", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetForearmAngle", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetElbowFrontDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetElbowRightFrontDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetHandDirDistance", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetShoulderFrontDirWorld", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetPelvisFrontDirWorld", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetPelvisAngle", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetNoseDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetHandSideDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetWaistSideDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetKneeSideDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetElbowSideDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetArmpitDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetHandSideDistance", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetGripDistance", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetShoulderSideDirWorld", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetPelvisSideDirWorld", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetNoseShoulderSideDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetNosePelvisSideDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetShoulderDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
            ResultProData.Add("GetPelvisDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });

            ResultProData.Add("GetHandCombineDir", new int[8] { 0, 0, 0, 0, 0, 0, 0, 0 });
        }
    }

    bool SaveCsvFull(bool isAI = false)
    {
        try
        {
            Directory.CreateDirectory(path);

            string output = string.Empty;
            output += "NAME,ADDRESS,TAKEBACK,BACKSWING,TOP,DOWNSWING,IMPACT,FOLLOW,FINISH\r\n";
            output += "GetHandDir," + string.Join(",", ResultProData["GetHandDir"]) + "\r\n";
            output += "GetHandDistance," + string.Join(",", ResultProData["GetHandDistance"]) + "\r\n";
            output += "GetShoulderDistance," + string.Join(",", ResultProData["GetShoulderDistance"]) + "\r\n";

            output += "GetSpineDir," + string.Join(",", ResultProData["GetSpineDir"]) + "\r\n";
            output += "GetShoulderAngle," + string.Join(",", ResultProData["GetShoulderAngle"]) + "\r\n";
            output += "GetFootDisRate," + string.Join(",", ResultProData["GetFootDisRate"]) + "\r\n";
            output += "GetWeight," + string.Join(",", ResultProData["GetWeight"]) + "\r\n";
            output += "GetForearmAngle," + string.Join(",", ResultProData["GetForearmAngle"]) + "\r\n";
            output += "GetElbowFrontDir," + string.Join(",", ResultProData["GetElbowFrontDir"]) + "\r\n";
            output += "GetElbowRightFrontDir," + string.Join(",", ResultProData["GetElbowRightFrontDir"]) + "\r\n";
            output += "GetHandDirDistance," + string.Join(",", ResultProData["GetHandDirDistance"]) + "\r\n";

            output += "GetShoulderFrontDirWorld," + string.Join(",", ResultProData["GetShoulderFrontDirWorld"]) + "\r\n";
            output += "GetPelvisFrontDirWorld," + string.Join(",", ResultProData["GetPelvisFrontDirWorld"]) + "\r\n";

            output += "GetPelvisAngle," + string.Join(",", ResultProData["GetPelvisAngle"]) + "\r\n";
            output += "GetNoseDir," + string.Join(",", ResultProData["GetNoseDir"]) + "\r\n";

            output += "GetHandSideDir," + string.Join(",", ResultProData["GetHandSideDir"]) + "\r\n";
            output += "GetWaistSideDir," + string.Join(",", ResultProData["GetWaistSideDir"]) + "\r\n";
            output += "GetKneeSideDir," + string.Join(",", ResultProData["GetKneeSideDir"]) + "\r\n";
            output += "GetElbowSideDir," + string.Join(",", ResultProData["GetElbowSideDir"]) + "\r\n";
            output += "GetArmpitDir," + string.Join(",", ResultProData["GetArmpitDir"]) + "\r\n";
            output += "GetHandSideDistance," + string.Join(",", ResultProData["GetHandSideDistance"]) + "\r\n";

            output += "GetGripDistance," + string.Join(",", ResultProData["GetGripDistance"]) + "\r\n";
            output += "GetShoulderSideDirWorld," + string.Join(",", ResultProData["GetShoulderSideDirWorld"]) + "\r\n";
            output += "GetPelvisSideDirWorld," + string.Join(",", ResultProData["GetPelvisSideDirWorld"]) + "\r\n";

            output += "GetNoseShoulderSideDir," + string.Join(",", ResultProData["GetNoseShoulderSideDir"]) + "\r\n";
            output += "GetNosePelvisSideDir," + string.Join(",", ResultProData["GetNosePelvisSideDir"]) + "\r\n";

            output += "GetShoulderDir," + string.Join(",", ResultProData["GetShoulderDir"]) + "\r\n";
            output += "GetPelvisDir," + string.Join(",", ResultProData["GetPelvisDir"]) + "\r\n";

            output += "GetHandCombineDir," + string.Join(",", ResultProData["GetHandCombineDir"]);

            string fileName = $"{(int)swingType}_{(int)club}";

            if (isAI)
            {
                fileName += "_ai";
            }

            string filePath = Path.Combine(path, fileName + ".csv");
            File.WriteAllText(filePath, output, new UTF8Encoding(true));

            UpsertSwingListCsv();

            return true;
        }
        catch (Exception e)
        {
            Debug.Log("[SaveCsvFull] Failed:" + e.Message);
            return false;
        }
    }

    private void UpsertSwingListCsv()
    {
        int currentSwing = (int)swingType;
        int currentClub = (int)club;
        string currentPath = $"{currentSwing}_{currentClub}.csv";

        string listPath = Path.Combine(path, $"{GolfProDataManager.Instance.SelectProData.uid}.csv");

        Directory.CreateDirectory(path);

        List<string> resultLines = new List<string>();
        resultLines.Add("SWING,CLUB,PATH");

        HashSet<string> addedKeys = new HashSet<string>();

        if (File.Exists(listPath))
        {
            string[] lines = File.ReadAllLines(listPath, new UTF8Encoding(true));

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] cols = line.Split(',');

                if (cols.Length < 2)
                    continue;

                string swingText = cols[0].Trim();
                string clubText = cols[1].Trim();

                if (!int.TryParse(swingText, out int rowSwing))
                    continue;

                if (!int.TryParse(clubText, out int rowClub))
                    continue;

                string key = $"{rowSwing}_{rowClub}";

                if (rowSwing == currentSwing && rowClub == currentClub)
                    continue;

                if (addedKeys.Contains(key))
                    continue;

                string rowPath = cols.Length >= 3 && !string.IsNullOrWhiteSpace(cols[2])
                    ? cols[2].Trim()
                    : $"{rowSwing}_{rowClub}.csv";

                resultLines.Add($"{rowSwing},{rowClub},{rowPath}");
                addedKeys.Add(key);
            }
        }

        string currentKey = $"{currentSwing}_{currentClub}";

        if (!addedKeys.Contains(currentKey))
        {
            resultLines.Add($"{currentSwing},{currentClub},{currentPath}");
            addedKeys.Add(currentKey);
        }

        File.WriteAllLines(listPath, resultLines, new UTF8Encoding(true));
    }

    private string GetProfileAICsvPath()
    {
        string fileName = $"{(int)swingType}_{(int)club}_ai_frames";
        return Path.Combine(path, fileName + ".csv");
    }

    private bool SaveProfileAICsv()
    {
        try
        {
            if (_profileAnalyzeSnapshots == null || _profileAnalyzeSnapshots.Count <= 0)
            {
                Debug.Log("[SaveProfileAICsv] No profile snapshots.");
                return false;
            }

            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[SaveProfileAICsv] null or empty.");
                return false;
            }

            Directory.CreateDirectory(path);

            StringBuilder output = new StringBuilder(1024 * 64);

            output.Append("frameIndex,isValid");
            output.Append(",GetHandDir");
            output.Append(",GetHandDirNF");
            output.Append(",GetHandDistance");
            output.Append(",GetShoulderDistance");
            output.Append(",GetSpineDir");
            output.Append(",GetShoulderAngle");
            output.Append(",GetFootDisRate");
            output.Append(",GetWeight");
            output.Append(",GetForearmAngle");
            output.Append(",GetElbowFrontDir");
            output.Append(",GetElbowRightFrontDir");
            output.Append(",GetHandDirDistance");
            output.Append(",GetShoulderFrontDirWorld");
            output.Append(",GetPelvisFrontDirWorld");
            output.Append(",GetPelvisAngle");
            output.Append(",GetNoseDir");
            output.Append(",GetHandSideDir");
            output.Append(",GetWaistSideDir");
            output.Append(",GetKneeSideDir");
            output.Append(",GetElbowSideDir");
            output.Append(",GetArmpitDir");
            output.Append(",GetHandSideDistance");
            output.Append(",GetGripDistance");
            output.Append(",GetShoulderSideDirWorld");
            output.Append(",GetPelvisSideDirWorld");
            output.Append(",GetNoseShoulderSideDir");
            output.Append(",GetNosePelvisSideDir");
            output.Append(",GetShoulderDir");
            output.Append(",GetPelvisDir");
            output.Append(",GetHandCombineDir");
            output.AppendLine();

            for (int i = 0; i < _profileAnalyzeSnapshots.Count; i++)
            {
                FrameAnalyzeSnapshot snapshot = _profileAnalyzeSnapshots[i];

                output.Append(i).Append(',');
                output.Append(snapshot.isValid ? 1 : 0).Append(',');

                output.Append(snapshot.handDir).Append(',');
                output.Append(snapshot.handDirNF).Append(',');
                output.Append(snapshot.handDistance).Append(',');
                output.Append(snapshot.shoulderDistance).Append(',');
                output.Append(snapshot.spineDir).Append(',');
                output.Append(snapshot.shoulderAngle).Append(',');
                output.Append(snapshot.footDisRate).Append(',');
                output.Append(snapshot.weight).Append(',');
                output.Append(snapshot.forearmAngle).Append(',');
                output.Append(snapshot.elbowFrontDir).Append(',');
                output.Append(snapshot.elbowRightFrontDir).Append(',');
                output.Append(snapshot.handDirDistance).Append(',');
                output.Append(snapshot.shoulderFrontDirWorld).Append(',');
                output.Append(snapshot.pelvisFrontDirWorld).Append(',');
                output.Append(snapshot.pelvisAngle).Append(',');
                output.Append(snapshot.noseDir).Append(',');
                output.Append(snapshot.handSideDir).Append(',');
                output.Append(snapshot.waistSideDir).Append(',');
                output.Append(snapshot.kneeSideDir).Append(',');
                output.Append(snapshot.elbowSideDir).Append(',');
                output.Append(snapshot.armpitDir).Append(',');
                output.Append(snapshot.handSideDistance).Append(',');
                output.Append(snapshot.gripDistance).Append(',');
                output.Append(snapshot.shoulderSideDirWorld).Append(',');
                output.Append(snapshot.pelvisSideDirWorld).Append(',');
                output.Append(snapshot.noseShoulderSideDir).Append(',');
                output.Append(snapshot.nosePelvisSideDir).Append(',');
                output.Append(snapshot.shoulderDir).Append(',');
                output.Append(snapshot.pelvisDir).Append(',');
                output.Append(snapshot.handCombineDir);
                output.AppendLine();
            }

            string filePath = GetProfileAICsvPath();
            File.WriteAllText(filePath, output.ToString(), new UTF8Encoding(true));

            Debug.Log("[SaveProfileAICsv] Save File : " + filePath);
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("[SaveProfileAICsv] Failed: " + e.Message);
            return false;
        }
    }

    private void ProfileSelectedFramesToDic(int[] stepIndex)
    {
        if (stepIndex == null || stepIndex.Length < 8)
            return;

        captureFrontDic.Clear();
        captureSideDic.Clear();

        for (int s = 0; s < 8; s++)
        {
            int idx = stepIndex[s];

            if (idx < 0)
                continue;

            if (idx >= framesFront.Count || idx >= framesSide.Count)
                continue;

            byte[] frontBytes = framesFront[idx];
            byte[] sideBytes = framesSide[idx];

            if (frontBytes == null || sideBytes == null)
                continue;

            Texture2D f = CreateTextureFromRaw(frontBytes, widthFront, heightFront);
            Texture2D si = CreateTextureFromRaw(sideBytes, widthSide, heightSide);

            if (f == null || si == null)
            {
                if (f != null) Destroy(f);
                if (si != null) Destroy(si);
                continue;
            }

            SWINGSTEP step = (SWINGSTEP)s;

            if (!captureFrontDic.ContainsKey(step))
                captureFrontDic.Add(step, f);
            else
                captureFrontDic[step] = f;

            if (!captureSideDic.ContainsKey(step))
                captureSideDic.Add(step, si);
            else
                captureSideDic[step] = si;
        }
    }

    private void SaveAllImages(bool isAI = false)
    {
        string filepath = imagePath;
        string aiStr = isAI ? "_ai" : string.Empty;

        foreach (KeyValuePair<SWINGSTEP, Texture2D> pic in captureFrontDic)
        {
            SWINGSTEP step = pic.Key;
            Texture2D tex = pic.Value;

            Texture2D rotated = Rotate90CCW(tex);
            Texture2D fixedTex = MirrorX(rotated);

            byte[] bytes = fixedTex.EncodeToPNG();

            string fileName = $"{step.ToString().ToLower()}_front_{(int)swingType}{(int)club}{aiStr}.png";
            string filePath = Path.Combine(filepath, fileName);

            File.WriteAllBytes(filePath, bytes);

            Destroy(rotated);
            Destroy(fixedTex);
        }

        foreach (KeyValuePair<SWINGSTEP, Texture2D> pic in captureSideDic)
        {
            SWINGSTEP step = pic.Key;
            Texture2D tex = pic.Value;

            Texture2D rotated = Rotate90CCW(tex);
            Texture2D fixedTex = MirrorX(rotated);

            byte[] bytes = fixedTex.EncodeToPNG();
            string fileName = $"{step.ToString().ToLower()}_side_{(int)swingType}{(int)club}{aiStr}.png";
            string filePath = Path.Combine(filepath, fileName);

            File.WriteAllBytes(filePath, bytes);

            Destroy(rotated);
            Destroy(fixedTex);
        }
    }

    bool CaptureFrameFront(SWINGSTEP step = SWINGSTEP.READY)
    {
        Texture sourceTexFront = rawImageFront.texture;

        if (sourceTexFront == null)
            return false;

        widthFront = sourceTexFront.width;
        heightFront = sourceTexFront.height;

        RenderTexture renderTexFront = RenderTexture.GetTemporary(widthFront, heightFront, 0);
        Graphics.Blit(sourceTexFront, renderTexFront);

        RenderTexture.active = renderTexFront;

        Texture2D captureFront = new Texture2D(widthFront, heightFront, TextureFormat.RGB24, false);
        captureFront.ReadPixels(new UnityEngine.Rect(0, 0, widthFront, heightFront), 0, 0);
        captureFront.Apply();

        if (recordingType == ERecordingType.Lesson)
        {
        }
        else if (recordingType == ERecordingType.Practice)
        {
            if (!isSelectRetake)
                captureFrontDic.Add(step, captureFront);
            else
                captureFrontDic[step] = captureFront;
        }
        else
        {
            Destroy(captureFront);
        }

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexFront);

        return true;
    }

    void CaptureFrameSide(SWINGSTEP step = SWINGSTEP.READY)
    {
        Texture sourceTexSide = rawImageSide.texture;

        if (sourceTexSide == null)
            return;

        widthSide = sourceTexSide.width;
        heightSide = sourceTexSide.height;

        RenderTexture renderTexSide = RenderTexture.GetTemporary(widthSide, heightSide, 0);
        Graphics.Blit(sourceTexSide, renderTexSide);

        RenderTexture.active = renderTexSide;
        Texture2D captureSide = new Texture2D(widthSide, heightSide, TextureFormat.RGB24, false);
        captureSide.ReadPixels(new UnityEngine.Rect(0, 0, widthSide, heightSide), 0, 0);
        captureSide.Apply();

        if (recordingType == ERecordingType.Practice)
        {
            if (!isSelectRetake)
                captureSideDic.Add(step, captureSide);
            else
                captureSideDic[step] = captureSide;
        }
        else
        {
            Destroy(captureSide);
        }

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexSide);
    }

    IEnumerator CaptureFramesProfile()
    {
        int finishFrame = GetPostImpactFrames();
        int postImpactLeft = -1;

        int retry = 0;
        checkTakebackFrame = 0;
        _profileCaptureCount = 0;

        while (isRecording)
        {
            yield return new WaitForEndOfFrame();

            Texture sourceTexFront = rawImageFront != null ? rawImageFront.texture : null;
            Texture sourceTexSide = rawImageSide != null ? rawImageSide.texture : null;

            if (sourceTexFront == null || sourceTexSide == null)
            {
                continue;
            }

            widthFront = sourceTexFront.width;
            heightFront = sourceTexFront.height;
            widthSide = sourceTexSide.width;
            heightSide = sourceTexSide.height;

            bool canCapture = true;

            if (checkTakeback == false)
            {
                checkTakebackFrame++;

                if (framesFront.Count > 90)
                {
                    checkTakebackFrame = 0;
                    framesFront.Clear();
                    framesSide.Clear();
                }

                if (_handCheck == false)
                {
                    canCapture = false;
                }
            }

            if (canCapture)
            {
                int frameIndex = framesFront.Count;

                framesFront.Add(null);
                framesSide.Add(null);

                StartCoroutine(CaptureAsync(sourceTexFront, framesFront, true, frameIndex));
                StartCoroutine(CaptureAsync(sourceTexSide, framesSide, false, frameIndex));
            }

            if (checkImpact)
            {
                if (postImpactLeft < 0)
                {
                    postImpactLeft = finishFrame;
                }

                if (postImpactLeft > 0)
                {
                    postImpactLeft--;
                }
                else
                {
                    isRecording = false;
                }
            }

            if (webcamTrackerFront.visibilityAvg < 0.1f)
            {
                if (retry > 5)
                {
                    isRecording = false;
                    SetRecordingStep(EProfileStep.GRIP);
                    yield break;
                }
                else
                {
                    retry++;
                }
            }
        }

        yield return new WaitUntil(() => _profileCaptureCount <= 0);

        int trimCount = Math.Max(0, checkTakebackFrame - 30);

        if (trimCount > 0 && framesFront.Count >= trimCount)
        {
            framesFront.RemoveRange(0, trimCount);
        }

        if (trimCount > 0 && framesSide.Count >= trimCount)
        {
            framesSide.RemoveRange(0, trimCount);
        }

        int syncedCount = Mathf.Min(framesFront.Count, framesSide.Count);

        if (framesFront.Count > syncedCount)
        {
            framesFront.RemoveRange(syncedCount, framesFront.Count - syncedCount);
        }

        if (framesSide.Count > syncedCount)
        {
            framesSide.RemoveRange(syncedCount, framesSide.Count - syncedCount);
        }

        SetRecordingStep(EProfileStep.RESULT);
    }

    IEnumerator CaptureFrames()
    {
        string output = _pendingVideoTempPath;
        int width = rawImageFront.texture.width;
        int height = rawImageFront.texture.height;

        int frameCount = 0;

        widthFront = width;
        heightFront = height;

        if (!TryStartFFmpeg(output, width, height, out Process process, out Stream stdin, out string errorMsg, true))
        {
            Debug.LogError($"FFmpeg 실행 실패: {errorMsg}");
            yield break;
        }

        while (isRecording)
        {
            yield return new WaitForEndOfFrame();
            RawImage src = (_lessonView == ELessonView.FRONT) ? rawImageFront : rawImageSide;

            if (src == null || src.texture == null)
            {
                continue;
            }

            if (src.texture.width != width || src.texture.height != height)
            {
                continue;
            }

            RenderTexture renderTex = RenderTexture.GetTemporary(width, height, 0);
            Graphics.Blit(src.texture, renderTex);
            RenderTexture.active = renderTex;

            Texture2D frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            frame.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0);
            frame.Apply();

            if (frameCount == 0)
            {
                captureFront = Rotate90CCW(frame);
            }

            byte[] raw = frame.GetRawTextureData();

            if (_lessonView == ELessonView.SIDE)
            {
                FlipVerticalRGB24InPlace(raw, width, height);
            }

            stdin.Write(raw, 0, raw.Length);

            UnityEngine.Object.Destroy(frame);
            RenderTexture.ReleaseTemporary(renderTex);
            RenderTexture.active = null;

            frameCount++;

            if (isFinish)
            {
                isRecording = false;
            }
        }

        stdin.Flush();
        stdin.Close();

        yield return new WaitUntil(() => process.HasExited);

        process.Close();

        GC.Collect();

        _replayReadyFront = true;
        _pendingVideoReady = true;
    }

    private void FlipVerticalRGB24InPlace(byte[] raw, int width, int height)
    {
        if (raw == null)
            return;

        int stride = width * 3;
        byte[] temp = new byte[stride];

        int half = height / 2;

        for (int y = 0; y < half; y++)
        {
            int top = y * stride;
            int bottom = (height - 1 - y) * stride;

            Buffer.BlockCopy(raw, top, temp, 0, stride);
            Buffer.BlockCopy(raw, bottom, raw, top, stride);
            Buffer.BlockCopy(temp, 0, raw, bottom, stride);
        }
    }

    private IEnumerator SendFramesToFFmpeg(string output, int width, int height, bool isFront, List<byte[]> frames, Action ComplateEvent)
    {
        yield return null;
        yield return null;

        Process process = null;
        Stream stdin = null;

        if (!TryStartFFmpeg(output, width, height, out process, out stdin, out string errorMsg, isFront))
        {
            yield break;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            byte[] raw = frames[i];

            if (raw != null && raw.Length > 0)
            {
                stdin.Write(raw, 0, raw.Length);
            }

            if (i % 20 == 0)
                yield return null;
        }

        frames.Clear();

        stdin.Flush();
        stdin.Close();

        bool isExited = false;
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            _ = process.WaitForExit(5000);
            process.Close();
            isExited = true;
        });

        while (!isExited)
        {
            yield return null;
        }

        ComplateEvent?.Invoke();

        yield return new WaitForSeconds(0.1f);
    }

    private string BuildFFmpegVideoArguments(string output, int width, int height, string vfOption)
    {
        return $"-y -f rawvideo -pixel_format rgb24 -video_size {width}x{height} -framerate 30 -i - -vf {vfOption} -c:v libx264 -preset ultrafast -crf 23 -profile:v baseline -level 3.1 -pix_fmt yuv420p -movflags +faststart \"{output}\"";
    }

    private bool TryStartFFmpeg(string output, int width, int height, out Process process, out Stream stdin, out string errorMsg, bool isFront)
    {
        stdin = null;
        process = new Process();
        errorMsg = "";
        string vfOption = isFront ? "\"transpose=1\"" : "\"transpose=0\"";

        if (File.Exists(ffmpegPath) == false)
        {
            return false;
        }

        try
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = BuildFFmpegVideoArguments(output, width, height, vfOption),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            stdin = process.StandardInput.BaseStream;
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            errorMsg = "Win32 오류: " + ex.Message;
            return false;
        }
        catch (System.Exception ex)
        {
            errorMsg = "일반 오류: " + ex.Message;
            return false;
        }
    }

    private void DiscardPendingRecording()
    {
        TryDeleteFile(_pendingVideoTempPath);
        TryDeleteFile(_muxTempVideoPath);
        TryDeleteFile(_audioTempWavPath);

        _pendingVideoReady = false;
        _pendingConfirmed = false;
    }

    private void TryDeleteFile(string file)
    {
        if (string.IsNullOrEmpty(file))
            return;

        try
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        catch { }
    }

    private string AddAiSuffixToCsvPath(string csvPath)
    {
        if (string.IsNullOrEmpty(csvPath))
            return csvPath;

        string dir = Path.GetDirectoryName(csvPath);
        string name = Path.GetFileNameWithoutExtension(csvPath);
        string ext = Path.GetExtension(csvPath);

        if (name.EndsWith("_ai", StringComparison.OrdinalIgnoreCase))
            return csvPath;

        return Path.Combine(dir, $"{name}_ai{ext}");
    }

    private void ExtractLandmarks(Texture2D frontTexture, Texture2D sideTexture, out Landmark2D[] front2DLandmarks, out Landmark2D[] side2DLandmarks, out Landmark3D[] front3DLandmarks, out Landmark3D[] side3DLandmarks)
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

    private FrameAnalyzeSnapshot CaptureAnalyzeSnapshot()
    {
        FrameAnalyzeSnapshot snapshot = new FrameAnalyzeSnapshot();

        if (m_SensorProcess == null)
        {
            snapshot.isValid = false;
            return snapshot;
        }

        snapshot.isValid = true;

        snapshot.handDir = m_SensorProcess.iGetHandDir;
        snapshot.handDistance = m_SensorProcess.iGetHandDistance;
        snapshot.shoulderDistance = m_SensorProcess.iGetShoulderDistance;
        snapshot.spineDir = m_SensorProcess.iGetSpineDir;
        snapshot.shoulderAngle = m_SensorProcess.iGetShoulderAngle;
        snapshot.footDisRate = m_SensorProcess.iGetFootDisRate;
        snapshot.weight = m_SensorProcess.iGetWeight;
        snapshot.forearmAngle = m_SensorProcess.iGetForearmAngle;
        snapshot.elbowFrontDir = m_SensorProcess.iGetElbowFrontDir;
        snapshot.elbowRightFrontDir = m_SensorProcess.iGetElbowRightFrontDir;
        snapshot.handDirDistance = m_SensorProcess.iGetHandDirDistance;

        snapshot.shoulderFrontDirWorld = m_SensorProcess.iGetShoulderFrontDirWorld;
        snapshot.pelvisFrontDirWorld = m_SensorProcess.iGetPelvisFrontDirWorld;

        snapshot.pelvisAngle = m_SensorProcess.iGetPelvisAngle;
        snapshot.noseDir = m_SensorProcess.iGetNoseDir;

        snapshot.handSideDir = m_SensorProcess.iGetHandSideDir;
        snapshot.waistSideDir = m_SensorProcess.iGetWaistSideDir;
        snapshot.kneeSideDir = m_SensorProcess.iGetKneeSideDir;
        snapshot.elbowSideDir = m_SensorProcess.iGetElbowSideDir;
        snapshot.armpitDir = m_SensorProcess.iGetArmpitDir;
        snapshot.handSideDistance = m_SensorProcess.iGetHandSideDistance;

        snapshot.gripDistance = m_SensorProcess.iGetGripDistance;
        snapshot.shoulderSideDirWorld = m_SensorProcess.iGetShoulderSideDirWorld;
        snapshot.pelvisSideDirWorld = m_SensorProcess.iGetPelvisSideDirWorld;

        snapshot.noseShoulderSideDir = m_SensorProcess.iGetNoseShoulderSideDir;
        snapshot.nosePelvisSideDir = m_SensorProcess.iGetNosePelvisSideDir;

        snapshot.shoulderDir = m_SensorProcess.iGetShoulderDir;
        snapshot.pelvisDir = m_SensorProcess.iGetPelvisDir;
        snapshot.handCombineDir = m_SensorProcess.iGetHandCombineDir;

        snapshot.handDirNF = m_SensorProcess.iGetHandDirNF;

        return snapshot;
    }

    private void ApplySnapshotToResult(int swingStepIndex, FrameAnalyzeSnapshot snapshot)
    {
        if (!snapshot.isValid)
            return;

        ResultProData["GetHandDir"][swingStepIndex] = snapshot.handDir;
        ResultProData["GetHandDistance"][swingStepIndex] = snapshot.handDistance;
        ResultProData["GetShoulderDistance"][swingStepIndex] = snapshot.shoulderDistance;
        ResultProData["GetSpineDir"][swingStepIndex] = snapshot.spineDir;
        ResultProData["GetShoulderAngle"][swingStepIndex] = snapshot.shoulderAngle;
        ResultProData["GetFootDisRate"][swingStepIndex] = snapshot.footDisRate;
        ResultProData["GetWeight"][swingStepIndex] = snapshot.weight;
        ResultProData["GetForearmAngle"][swingStepIndex] = snapshot.forearmAngle;
        ResultProData["GetElbowFrontDir"][swingStepIndex] = snapshot.elbowFrontDir;
        ResultProData["GetElbowRightFrontDir"][swingStepIndex] = snapshot.elbowRightFrontDir;
        ResultProData["GetHandDirDistance"][swingStepIndex] = snapshot.handDirDistance;

        ResultProData["GetShoulderFrontDirWorld"][swingStepIndex] = snapshot.shoulderFrontDirWorld;
        ResultProData["GetPelvisFrontDirWorld"][swingStepIndex] = snapshot.pelvisFrontDirWorld;

        ResultProData["GetPelvisAngle"][swingStepIndex] = snapshot.pelvisAngle;
        ResultProData["GetNoseDir"][swingStepIndex] = snapshot.noseDir;

        ResultProData["GetHandSideDir"][swingStepIndex] = snapshot.handSideDir;
        ResultProData["GetWaistSideDir"][swingStepIndex] = snapshot.waistSideDir;
        ResultProData["GetKneeSideDir"][swingStepIndex] = snapshot.kneeSideDir;
        ResultProData["GetElbowSideDir"][swingStepIndex] = snapshot.elbowSideDir;
        ResultProData["GetArmpitDir"][swingStepIndex] = snapshot.armpitDir;
        ResultProData["GetHandSideDistance"][swingStepIndex] = snapshot.handSideDistance;

        ResultProData["GetGripDistance"][swingStepIndex] = snapshot.gripDistance;
        ResultProData["GetShoulderSideDirWorld"][swingStepIndex] = snapshot.shoulderSideDirWorld;
        ResultProData["GetPelvisSideDirWorld"][swingStepIndex] = snapshot.pelvisSideDirWorld;

        ResultProData["GetNoseShoulderSideDir"][swingStepIndex] = snapshot.noseShoulderSideDir;
        ResultProData["GetNosePelvisSideDir"][swingStepIndex] = snapshot.nosePelvisSideDir;

        ResultProData["GetShoulderDir"][swingStepIndex] = snapshot.shoulderDir;
        ResultProData["GetPelvisDir"][swingStepIndex] = snapshot.pelvisDir;
        ResultProData["GetHandCombineDir"][swingStepIndex] = snapshot.handCombineDir;
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

    // private NormalizedLandmarks DetectOne(Texture2D inputTexture)
    // {
    //     if (inputTexture == null || inputTexture.width <= 0 || inputTexture.height <= 0)
    //     {
    //         return default;
    //     }

    //     EnsurePoseLM(inputTexture.width, inputTexture.height, false);

    //     _tfAnalyzer.ReadTextureOnCPU(inputTexture, flipHorizontally: false, flipVertically: true);

    //     using (Mediapipe.Image cpuImage = _tfAnalyzer.BuildCPUImage())
    //     {
    //         PoseLandmarkerResult result = _offlinePoseLM.Detect(cpuImage);

    //         bool hasAny = result.poseLandmarks != null && result.poseLandmarks.Count > 0;

    //         if (hasAny)
    //         {
    //             return result.poseLandmarks[0];
    //         }
    //     }

    //     return default;
    // }

    // private Landmarks DetectOneWorld(Texture2D inputTexture)
    // {
    //     if (inputTexture == null)
    //     {
    //         return default;
    //     }

    //     EnsurePoseLM(inputTexture.width, inputTexture.height, false);

    //     _tfAnalyzer.ReadTextureOnCPU(inputTexture, flipHorizontally: false, flipVertically: true);

    //     using (Mediapipe.Image cpuImage = _tfAnalyzer.BuildCPUImage())
    //     {
    //         PoseLandmarkerResult result = _offlinePoseLM.Detect(cpuImage);

    //         bool hasAny = result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0;

    //         if (hasAny)
    //         {
    //             return result.poseWorldLandmarks[0];
    //         }
    //     }

    //     return default;
    // }

    private bool TryDetectPose(Texture2D inputTexture, out NormalizedLandmarks normalizedLandmarks, out Landmarks worldLandmarks)
    {
        normalizedLandmarks = default;
        worldLandmarks = default;

        if (inputTexture == null || inputTexture.width <= 0 || inputTexture.height <= 0)
        {
            return false;
        }

        EnsurePoseLM(inputTexture.width, inputTexture.height, false);

        _tfAnalyzer.ReadTextureOnCPU(inputTexture, flipHorizontally: false, flipVertically: true);

        using (Mediapipe.Image cpuImage = _tfAnalyzer.BuildCPUImage())
        {
            PoseLandmarkerResult result = _offlinePoseLM.Detect(cpuImage);

            bool has2D = result.poseLandmarks != null && result.poseLandmarks.Count > 0;
            bool has3D = result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0;

            if (!has2D && !has3D)
            {
                return false;
            }

            if (has2D)
            {
                normalizedLandmarks = result.poseLandmarks[0];
            }

            if (has3D)
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

    private int _profileCaptureCount = 0;

    private IEnumerator CaptureAsync(Texture source, List<byte[]> targetList, bool isFront, int frameIndex)
    {
        if (source == null)
            yield break;

        if (source.width <= 16 || source.height <= 16)
            yield break;

        int width = source.width;
        int height = source.height;

        if (isFront)
        {
            widthFront = width;
            heightFront = height;
        }
        else
        {
            widthSide = width;
            heightSide = height;
        }

        while (targetList.Count <= frameIndex)
        {
            targetList.Add(null);
        }

        RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0);
        Graphics.Blit(source, renderTexture);

        _profileCaptureCount++;

        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, (request) =>
        {
            try
            {
                if (!request.hasError)
                {
                    byte[] data = request.GetData<byte>().ToArray();

                    if (frameIndex >= 0 && frameIndex < targetList.Count)
                    {
                        targetList[frameIndex] = data;
                    }
                }
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                _profileCaptureCount--;
            }
        });

        yield break;
    }

    private bool SavePracticeLandmarkCsv(SWINGSTEP step, Landmark2D[] frontLandmarks2D, Landmark3D[] frontLandmarks3D, Landmark2D[] sideLandmarks2D, Landmark3D[] sideLandmarks3D)
    {
        try
        {
            string fullPath = GetPracticeLandmarkCsvPath(step);
            string dir = Path.GetDirectoryName(fullPath);

            if (string.IsNullOrEmpty(dir))
            {
                Debug.Log("[SavePracticeLandmarkCsv] dir is null or empty.");
                return false;
            }

            Directory.CreateDirectory(dir);

            StringBuilder sb = new StringBuilder(8192);
            sb.AppendLine("type,view,idx,orgX,orgY,posX,posY,x,y,z,visibility");

            AppendPracticeRuntime2D(sb, "F", frontLandmarks2D);
            AppendPracticeRuntime3D(sb, "F", frontLandmarks3D);
            AppendPracticeRuntime2D(sb, "S", sideLandmarks2D);
            AppendPracticeRuntime3D(sb, "S", sideLandmarks3D);

            File.WriteAllText(fullPath, sb.ToString(), new UTF8Encoding(true));

            Debug.Log("[SavePracticeLandmarkCsv] Save File : " + fullPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.Log("[SavePracticeLandmarkCsv] Failed: " + e.Message);
            return false;
        }
    }

    private void AppendPracticeRuntime2D(StringBuilder sb, string view, Landmark2D[] landmarks)
    {
        if (landmarks == null || landmarks.Length < PRACTICE_LANDMARK_COUNT)
            return;

        for (int i = 0; i < PRACTICE_LANDMARK_COUNT; i++)
        {
            Landmark2D p = landmarks[i];

            sb.Append("2D").Append(',')
              .Append(view).Append(',')
              .Append(i).Append(',')
              .Append(p.positionOrg.x.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.positionOrg.y.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.x.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.y.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(",,").Append(',')
              .Append(p.visibility.ToString("0.######", CultureInfo.InvariantCulture))
              .AppendLine();
        }
    }

    private void AppendPracticeRuntime3D(StringBuilder sb, string view, Landmark3D[] landmarks)
    {
        if (landmarks == null || landmarks.Length < PRACTICE_LANDMARK_COUNT)
            return;

        for (int i = 0; i < PRACTICE_LANDMARK_COUNT; i++)
        {
            Landmark3D p = landmarks[i];

            sb.Append("3D").Append(',')
              .Append(view).Append(',')
              .Append(i).Append(',')
              .Append(",,,,")
              .Append(p.position.x.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.y.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.z.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.visibility.ToString("0.######", CultureInfo.InvariantCulture))
              .AppendLine();
        }
    }

    private string GetPracticeLandmarkCsvPath(SWINGSTEP step)
    {
        string fileName = $"{(int)swingType}_{(int)club}_landmark_{(int)step:00}_{step}.csv";
        return Path.Combine(landmarkPath, fileName);
    }

    // private void AutoSelectMicDevice()
    // {
    //     string[] devices = Microphone.devices;

    //     if (devices == null || devices.Length == 0)
    //     {
    //         Debug.Log("[AUDIO] 마이크 장치 없음. 녹음 비활성화");
    //         _micDeviceName = null;
    //         _enableMicRecording = false;
    //         return;
    //     }

    //     LogMicDevices(devices);

    //     if (!string.IsNullOrEmpty(_micDeviceName))
    //     {
    //         for (int i = 0; i < devices.Length; i++)
    //         {
    //             if (string.Equals(devices[i], _micDeviceName, StringComparison.OrdinalIgnoreCase))
    //             {
    //                 _enableMicRecording = true;
    //                 Debug.Log("[AUDIO] 기존 선택 마이크 사용: " + _micDeviceName);
    //                 return;
    //             }
    //         }
    //     }

    //     // 블루투스 마이크
    //     string bluetoothDevice = FindBluetoothMicDevice(devices);
    //     if (!string.IsNullOrEmpty(bluetoothDevice))
    //     {
    //         _micDeviceName = bluetoothDevice;
    //         _enableMicRecording = true;

    //         Debug.Log("[AUDIO] 선택된 블루투스 마이크: " + _micDeviceName);
    //         return;
    //     }

    //     // USB/외부 마이크
    //     string externalDevice = FindExternalMicDevice(devices);
    //     if (!string.IsNullOrEmpty(externalDevice))
    //     {
    //         _micDeviceName = externalDevice;
    //         _enableMicRecording = true;

    //         Debug.Log("[AUDIO] 선택된 외부 마이크: " + _micDeviceName);
    //         return;
    //     }

    //     // Default Input Device
    //     string defaultDevice = FindDefaultInputDevice(devices);
    //     if (!string.IsNullOrEmpty(defaultDevice))
    //     {
    //         _micDeviceName = defaultDevice;
    //         _enableMicRecording = true;

    //         Debug.Log("[AUDIO] 명시적 블루투스 이름 없음. 기본 입력 장치 사용: " + _micDeviceName);
    //         return;
    //     }

    //     Debug.Log("[AUDIO] 사용 가능한 마이크 없음. 녹음 비활성화");
    //     _micDeviceName = null;
    //     _enableMicRecording = false;
    // }

    private void LogMicDevices(string[] devices)
    {
        if (devices == null)
            return;

        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log($"[AUDIO] Device[{i}] = {devices[i]}");
        }
    }

    private string FindBluetoothMicDevice(string[] devices)
    {
        if (devices == null)
            return null;

        for (int i = 0; i < devices.Length; i++)
        {
            string deviceName = devices[i];

            if (string.IsNullOrEmpty(deviceName))
                continue;

            if (IsMonitorDevice(deviceName))
                continue;

            if (IsBluetoothMicName(deviceName))
                return deviceName;
        }

        return null;
    }

    private string FindExternalMicDevice(string[] devices)
    {
        if (devices == null)
            return null;

        for (int i = 0; i < devices.Length; i++)
        {
            string deviceName = devices[i];

            if (string.IsNullOrEmpty(deviceName))
                continue;

            if (IsMonitorDevice(deviceName))
                continue;

            if (IsDefaultInputDevice(deviceName))
                continue;

            if (IsBuiltInMicName(deviceName))
                continue;

            return deviceName;
        }

        return null;
    }

    private string FindDefaultInputDevice(string[] devices)
    {
        if (devices == null)
            return null;

        for (int i = 0; i < devices.Length; i++)
        {
            string deviceName = devices[i];

            if (string.IsNullOrEmpty(deviceName))
                continue;

            if (IsMonitorDevice(deviceName))
                continue;

            if (IsDefaultInputDevice(deviceName))
                return deviceName;
        }

        return null;
    }

    private bool IsBluetoothMicName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        string lower = deviceName.ToLowerInvariant();

        return lower.Contains("bluetooth") ||
               lower.Contains("bluez") ||
               lower.Contains("bt-") ||
               lower.Contains("bt_") ||
               lower.Contains("handsfree") ||
               lower.Contains("hands-free") ||
               lower.Contains("headset") ||
               lower.Contains("headphones") ||
               lower.Contains("hfp") ||
               lower.Contains("hsp") ||
               lower.Contains("airpods") ||
               lower.Contains("galaxy buds") ||
               lower.Contains("buds") ||
               lower.Contains("qcy") ||
               lower.Contains("jbl") ||
               lower.Contains("bose") ||
               lower.Contains("sony") ||
               lower.Contains("wh-") ||
               lower.Contains("wf-");
    }

    private bool IsBuiltInMicName(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        string lower = deviceName.ToLowerInvariant();

        return lower.Contains("built-in") ||
               lower.Contains("builtin") ||
               lower.Contains("internal") ||
               lower.Contains("내장");
    }

    private bool IsDefaultInputDevice(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        string lower = deviceName.ToLowerInvariant();

        return lower.Contains("default input device") ||
               lower == "default" ||
               lower.Contains("기본 입력");
    }

    private bool IsMonitorDevice(string deviceName)
    {
        if (string.IsNullOrEmpty(deviceName))
            return false;

        string lower = deviceName.ToLowerInvariant();

        return lower.Contains("monitor of") ||
               lower.Contains("monitor") ||
               lower.Contains("output") ||
               lower.Contains("speaker");
    }

    // private void StartMicRecording()
    // {
    //     if (!_enableMicRecording)
    //         return;

    //     try
    //     {
    //         int min;
    //         int max;

    //         Microphone.GetDeviceCaps(_micDeviceName, out min, out max);

    //         if (max == 0)
    //             max = 44100;

    //         _micSampleRate = Mathf.Clamp(44100, (min == 0 ? 8000 : min), max);

    //         _micClip = Microphone.Start(_micDeviceName, true, 600, _micSampleRate);
    //     }
    //     catch (Exception e)
    //     {
    //         Debug.Log("[AUDIO] StartMicRecording error: " + e.Message);
    //         _micClip = null;
    //     }
    // }

    private IEnumerator AutoSelectAndStartMicRecording()
    {
        if (!_enableMicRecording)
        {
            Debug.Log("[AUDIO] Mic recording disabled.");
            yield break;
        }

        string[] devices = Microphone.devices;

        if (devices == null || devices.Length == 0)
        {
            Debug.Log("[AUDIO] Microphone.devices is empty.");
            _micDeviceName = null;
            _micClip = null;
            yield break;
        }

        LogMicDevices(devices);

        List<string> candidates = BuildMicCandidates(devices);

        for (int i = 0; i < candidates.Count; i++)
        {
            string deviceName = candidates[i];

            if (string.IsNullOrEmpty(deviceName))
                continue;

            Debug.Log("[AUDIO] Try mic: " + deviceName);

            bool started = false;
            yield return StartCoroutine(TryStartMicDevice(deviceName, result => started = result));

            if (started)
            {
                _micDeviceName = deviceName;
                Debug.Log("[AUDIO] Mic selected and started: " + _micDeviceName);
                yield break;
            }
        }

        Debug.Log("[AUDIO] No usable mic device found.");
        _micDeviceName = null;
        _micClip = null;
    }

    private List<string> BuildMicCandidates(string[] devices)
    {
        List<string> result = new List<string>();

        void AddIfExists(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return;

            if (!devices.Contains(deviceName))
                return;

            if (result.Contains(deviceName))
                return;

            if (IsMonitorDevice(deviceName))
                return;

            result.Add(deviceName);
        }

        // 기존 선택 마이크가 있고 현재 장치 목록에 있으면 우선 사용
        AddIfExists(_micDeviceName);

        // USB / 동글 / 외부 마이크
        for (int i = 0; i < devices.Length; i++)
        {
            string deviceName = devices[i];

            if (string.IsNullOrEmpty(deviceName))
                continue;

            if (IsMonitorDevice(deviceName))
                continue;

            if (IsDefaultInputDevice(deviceName))
                continue;

            if (IsBuiltInMicName(deviceName))
                continue;

            if (IsBluetoothMicName(deviceName))
                continue;

            AddIfExists(deviceName);
        }

        // 블루투스 마이크 이름이 직접 보이는 경우
        for (int i = 0; i < devices.Length; i++)
        {
            if (IsBluetoothMicName(devices[i]))
                AddIfExists(devices[i]);
        }

        // Default Input Device
        for (int i = 0; i < devices.Length; i++)
        {
            if (IsDefaultInputDevice(devices[i]))
                AddIfExists(devices[i]);
        }

        // 마지막 fallback. 내장 마이크 포함 전체 후보
        for (int i = 0; i < devices.Length; i++)
        {
            AddIfExists(devices[i]);
        }

        return result;
    }

    private IEnumerator TryStartMicDevice(string deviceName, Action<bool> onResult)
    {
        onResult?.Invoke(false);

        if (string.IsNullOrEmpty(deviceName))
            yield break;

        try
        {
            if (Microphone.IsRecording(deviceName))
            {
                Microphone.End(deviceName);
            }
        }
        catch { }

        if (_micClip != null)
        {
            Destroy(_micClip);
            _micClip = null;
        }

        int min = 0;
        int max = 0;

        try
        {
            Microphone.GetDeviceCaps(deviceName, out min, out max);
        }
        catch (Exception e)
        {
            Debug.Log("[AUDIO] GetDeviceCaps failed: " + deviceName + " / " + e.Message);
            yield break;
        }

        if (max <= 0)
            max = 44100;

        int minRate = min <= 0 ? 8000 : min;
        int targetRate = Mathf.Clamp(44100, minRate, max);

        _micSampleRate = targetRate;

        Debug.Log($"[AUDIO] Start mic device={deviceName}, min={min}, max={max}, rate={_micSampleRate}");

        try
        {
            _micClip = Microphone.Start(deviceName, true, 600, _micSampleRate);
        }
        catch (Exception e)
        {
            Debug.Log("[AUDIO] Microphone.Start failed: " + deviceName + " / " + e.Message);
            _micClip = null;
            yield break;
        }

        float timer = 0f;
        int position = 0;

        while (timer < 1.0f)
        {
            yield return new WaitForSeconds(0.1f);
            timer += 0.1f;

            try
            {
                position = Microphone.GetPosition(deviceName);
            }
            catch
            {
                position = 0;
            }

            if (position > 0)
                break;
        }

        if (_micClip != null && position > 0)
        {
            Debug.Log("[AUDIO] Mic start OK: " + deviceName + " / pos=" + position);
            onResult?.Invoke(true);
            yield break;
        }

        Debug.Log("[AUDIO] Mic start failed or no position movement: " + deviceName);

        try
        {
            Microphone.End(deviceName);
        }
        catch { }

        if (_micClip != null)
        {
            Destroy(_micClip);
            _micClip = null;
        }
    }

    private void StopMicAndSaveWav()
    {
        if (!_enableMicRecording)
            return;

        if (_micClip == null)
        {
            Debug.Log("[AUDIO] StopMicAndSaveWav skipped. _micClip is null.");
            return;
        }

        int recordedSamples = 0;

        try
        {
            if (!string.IsNullOrEmpty(_micDeviceName))
            {
                recordedSamples = Microphone.GetPosition(_micDeviceName);
            }
        }
        catch
        {
            recordedSamples = 0;
        }

        try
        {
            if (!string.IsNullOrEmpty(_micDeviceName))
            {
                Microphone.End(_micDeviceName);
            }
        }
        catch { }

        if (recordedSamples <= 0)
        {
            Debug.Log("[AUDIO] recordedSamples <= 0. WAV not saved.");
            Destroy(_micClip);
            _micClip = null;
            _audioTempWavPath = null;
            return;
        }

        try
        {
            string dir = Path.GetDirectoryName(videoPath);
            string name = Path.GetFileNameWithoutExtension(videoPath);

            _audioTempWavPath = Path.Combine(dir, name + "_audio.wav");

            SaveWav(_micClip, _audioTempWavPath, recordedSamples);

            Debug.Log("[AUDIO] WAV saved: " + _audioTempWavPath + " / samples=" + recordedSamples);
        }
        catch (Exception e)
        {
            Debug.Log("[AUDIO] Save WAV failed: " + e.Message);
            _audioTempWavPath = null;
        }
        finally
        {
            Destroy(_micClip);
            _micClip = null;
        }
    }

    private void SaveWav(AudioClip clip, string fullPath, int recordedSamples)
    {
        if (clip == null)
            throw new ArgumentNullException(nameof(clip));

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

        int channels = clip.channels;
        int sampleCount = Mathf.Clamp(recordedSamples, 1, clip.samples);

        float[] data = new float[sampleCount * channels];
        clip.GetData(data, 0);

        byte[] pcm16 = FloatToPCM16(data);

        using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int sampleRate = _micSampleRate;
            int byteRate = sampleRate * channels * 2;
            short blockAlign = (short)(channels * 2);
            short bitsPerSample = 16;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + pcm16.Length);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write(blockAlign);
            bw.Write(bitsPerSample);

            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(pcm16.Length);
            bw.Write(pcm16);
        }
    }

    private static byte[] FloatToPCM16(float[] samples)
    {
        byte[] bytes = new byte[samples.Length * 2];
        int i = 0;
        foreach (float f in samples)
        {
            short v = (short)Mathf.Clamp(Mathf.RoundToInt(f * short.MaxValue), short.MinValue, short.MaxValue);
            bytes[i++] = (byte)(v & 0xff);
            bytes[i++] = (byte)((v >> 8) & 0xff);
        }
        return bytes;
    }

    private IEnumerator MuxVideoAndAudio(string videoIn, string wavIn)
    {
        if (string.IsNullOrEmpty(videoIn) || string.IsNullOrEmpty(wavIn) || !File.Exists(videoIn) || !File.Exists(wavIn))
        {
            yield break;
        }

        string dir = Path.GetDirectoryName(videoIn);
        string name = Path.GetFileNameWithoutExtension(videoIn);
        _muxTempVideoPath = Path.Combine(dir, name + "_mux.mp4");

        string args = $"-y -i \"{videoIn}\" -i \"{wavIn}\" -map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a 128k -ar 48000 -shortest -movflags +faststart \"{_muxTempVideoPath}\"";

        Process proc = new Process();

        proc.StartInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        bool exited = false;
        try
        {
            proc.EnableRaisingEvents = true;
            proc.Exited += (s, e) => exited = true;
            proc.Start();
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
        }
        catch (Exception e)
        {
            Debug.Log("[AUDIO] Mux ffmpeg start failed: " + e.Message);
            yield break;
        }

        float time = 0f;
        const float timeout = 20f;

        while (!exited && time < timeout)
        {
            time += Time.deltaTime;
            yield return null;
        }

        if (!exited)
        {
            try
            {
                proc.Kill();
            }
            catch { }
            Debug.Log("[AUDIO] Mux ffmpeg timeout, killed.");
        }

        try
        {
            proc.Close();
        }
        catch { }

        try
        {
            if (File.Exists(_muxTempVideoPath))
            {
                try
                {
                    File.Delete(videoIn);
                }
                catch { }
                File.Move(_muxTempVideoPath, videoIn);
            }
        }
        catch (Exception e)
        {
            Debug.Log("[AUDIO] Replace with muxed file failed: " + e.Message);
        }

        try
        {
            if (File.Exists(wavIn))
                File.Delete(wavIn);
        }
        catch { }
    }



    // ===================== [ADD] RGB Import Helpers =====================
    // private bool TryImportProfileFramesFromRgb(out List<Texture2D> outFront, out List<Texture2D> outSide)
    // {
    //     outFront = null;
    //     outSide = null;

    //     if (_profileFrontRgb == null || _profileSideRgb == null)
    //     {
    //         Debug.LogError("[ProfileRGB] TextAsset is null.");
    //         return false;
    //     }

    //     if (_profileRgbWidthFront <= 0 || _profileRgbHeightFront <= 0 ||
    //         _profileRgbWidthSide <= 0 || _profileRgbHeightSide <= 0)
    //     {
    //         Debug.LogError("[ProfileRGB] Invalid RGB sizes.");
    //         return false;
    //     }

    //     if (!TryDecodeRgb24Frames(_profileFrontRgb.bytes, _profileRgbWidthFront, _profileRgbHeightFront, out outFront))
    //         return false;

    //     if (!TryDecodeRgb24Frames(_profileSideRgb.bytes, _profileRgbWidthSide, _profileRgbHeightSide, out outSide))
    //         return false;

    //     // 프론트/사이드 프레임 수를 맞춘다(분석 코드가 Count 동일을 요구함)
    //     int n = Mathf.Min(outFront.Count, outSide.Count);
    //     if (n <= 0)
    //     {
    //         Debug.LogError("[ProfileRGB] No frames decoded.");
    //         SafeDestroyFrames(outFront);
    //         SafeDestroyFrames(outSide);
    //         outFront = null;
    //         outSide = null;
    //         return false;
    //     }

    //     if (_profileRgbMaxFrames > 0)
    //         n = Mathf.Min(n, _profileRgbMaxFrames);

    //     if (outFront.Count != n) outFront.RemoveRange(n, outFront.Count - n);
    //     if (outSide.Count != n) outSide.RemoveRange(n, outSide.Count - n);

    //     Debug.Log($"[ProfileRGB] Imported frames. front={outFront.Count} side={outSide.Count}");
    //     return true;
    // }

    // private bool TryDecodeRgb24Frames(byte[] bytes, int w, int h, out List<Texture2D> frames)
    // {
    //     frames = null;

    //     if (bytes == null || bytes.Length <= 0)
    //     {
    //         Debug.LogError("[ProfileRGB] bytes empty.");
    //         return false;
    //     }

    //     int frameSize = w * h * 3;
    //     if (frameSize <= 0)
    //     {
    //         Debug.LogError("[ProfileRGB] frameSize invalid.");
    //         return false;
    //     }

    //     int frameCount = bytes.Length / frameSize;
    //     if (frameCount <= 0)
    //     {
    //         Debug.LogError($"[ProfileRGB] Not enough bytes. len={bytes.Length} frameSize={frameSize}");
    //         return false;
    //     }

    //     int usable = frameCount * frameSize;

    //     frames = new List<Texture2D>(frameCount);

    //     // 한 프레임씩 잘라 Texture2D로
    //     for (int i = 0; i < frameCount; i++)
    //     {
    //         int offset = i * frameSize;

    //         // ✅ Unity는 (byte[], offset, length) 오버로드가 없음
    //         //    -> 프레임 크기만큼 새 배열로 복사해서 넣어야 함
    //         byte[] frame = new byte[frameSize];
    //         Buffer.BlockCopy(bytes, offset, frame, 0, frameSize);

    //         Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
    //         tex.LoadRawTextureData(frame);   // ✅ 이건 지원됨
    //         tex.Apply(false, false);

    //         frames.Add(tex);
    //     }

    //     if (bytes.Length != usable)
    //         Debug.Log($"[ProfileRGB] trailing bytes ignored: {bytes.Length - usable}");

    //     return true;
    // }

    private bool TryImportProfileFramesFromRgb(out List<byte[]> outFront, out List<byte[]> outSide)
    {
        outFront = null;
        outSide = null;

        if (_profileFrontRgb == null || _profileSideRgb == null)
        {
            Debug.LogError("[ProfileRGB] TextAsset is null.");
            return false;
        }

        if (_profileRgbWidthFront <= 0 || _profileRgbHeightFront <= 0 ||
            _profileRgbWidthSide <= 0 || _profileRgbHeightSide <= 0)
        {
            Debug.LogError("[ProfileRGB] Invalid RGB sizes.");
            return false;
        }

        if (!TryDecodeRgb24Frames(_profileFrontRgb.bytes, _profileRgbWidthFront, _profileRgbHeightFront, out outFront))
            return false;

        if (!TryDecodeRgb24Frames(_profileSideRgb.bytes, _profileRgbWidthSide, _profileRgbHeightSide, out outSide))
            return false;

        int n = Mathf.Min(outFront.Count, outSide.Count);

        if (n <= 0)
        {
            Debug.LogError("[ProfileRGB] No frames decoded.");
            outFront = null;
            outSide = null;
            return false;
        }

        if (_profileRgbMaxFrames > 0)
            n = Mathf.Min(n, _profileRgbMaxFrames);

        if (outFront.Count != n) outFront.RemoveRange(n, outFront.Count - n);
        if (outSide.Count != n) outSide.RemoveRange(n, outSide.Count - n);

        Debug.Log($"[ProfileRGB] Imported frames. front={outFront.Count} side={outSide.Count}");
        return true;
    }

    private bool TryDecodeRgb24Frames(byte[] bytes, int w, int h, out List<byte[]> frames)
    {
        frames = null;

        if (bytes == null || bytes.Length <= 0)
        {
            Debug.LogError("[ProfileRGB] bytes empty.");
            return false;
        }

        int frameSize = w * h * 3;
        if (frameSize <= 0)
        {
            Debug.LogError("[ProfileRGB] frameSize invalid.");
            return false;
        }

        int frameCount = bytes.Length / frameSize;
        if (frameCount <= 0)
        {
            Debug.LogError($"[ProfileRGB] Not enough bytes. len={bytes.Length} frameSize={frameSize}");
            return false;
        }

        int usable = frameCount * frameSize;

        frames = new List<byte[]>(frameCount);

        for (int i = 0; i < frameCount; i++)
        {
            int offset = i * frameSize;

            byte[] frame = new byte[frameSize];
            Buffer.BlockCopy(bytes, offset, frame, 0, frameSize);

            frames.Add(frame);
        }

        if (bytes.Length != usable)
            Debug.Log($"[ProfileRGB] trailing bytes ignored: {bytes.Length - usable}");

        return true;
    }

    // private void SafeDestroyFrames(List<Texture2D> frames)
    // {
    //     if (frames == null) return;
    //     for (int i = 0; i < frames.Count; i++)
    //     {
    //         if (frames[i] != null) Destroy(frames[i]);
    //     }
    //     frames.Clear();
    // }
    // =====================================================================

    private void ApplySensorValuesToResult(int swingStepIndex)
    {
        // front
        ResultProData["GetHandDir"][swingStepIndex] = m_SensorProcess.iGetHandDir;
        ResultProData["GetHandDistance"][swingStepIndex] = m_SensorProcess.iGetHandDistance;
        ResultProData["GetShoulderDistance"][swingStepIndex] = m_SensorProcess.iGetShoulderDistance;
        ResultProData["GetSpineDir"][swingStepIndex] = m_SensorProcess.iGetSpineDir;
        ResultProData["GetShoulderAngle"][swingStepIndex] = m_SensorProcess.iGetShoulderAngle;
        ResultProData["GetFootDisRate"][swingStepIndex] = m_SensorProcess.iGetFootDisRate;
        ResultProData["GetWeight"][swingStepIndex] = m_SensorProcess.iGetWeight;
        ResultProData["GetForearmAngle"][swingStepIndex] = m_SensorProcess.iGetForearmAngle;
        ResultProData["GetElbowFrontDir"][swingStepIndex] = m_SensorProcess.iGetElbowFrontDir;
        ResultProData["GetElbowRightFrontDir"][swingStepIndex] = m_SensorProcess.iGetElbowRightFrontDir;
        ResultProData["GetHandDirDistance"][swingStepIndex] = m_SensorProcess.iGetHandDirDistance;

        ResultProData["GetShoulderFrontDirWorld"][swingStepIndex] = m_SensorProcess.iGetShoulderFrontDirWorld;
        ResultProData["GetPelvisFrontDirWorld"][swingStepIndex] = m_SensorProcess.iGetPelvisFrontDirWorld;

        ResultProData["GetPelvisAngle"][swingStepIndex] = m_SensorProcess.iGetPelvisAngle;
        ResultProData["GetNoseDir"][swingStepIndex] = m_SensorProcess.iGetNoseDir;

        // side
        ResultProData["GetHandSideDir"][swingStepIndex] = m_SensorProcess.iGetHandSideDir;
        ResultProData["GetWaistSideDir"][swingStepIndex] = m_SensorProcess.iGetWaistSideDir;
        ResultProData["GetKneeSideDir"][swingStepIndex] = m_SensorProcess.iGetKneeSideDir;
        ResultProData["GetElbowSideDir"][swingStepIndex] = m_SensorProcess.iGetElbowSideDir;
        ResultProData["GetArmpitDir"][swingStepIndex] = m_SensorProcess.iGetArmpitDir;
        ResultProData["GetHandSideDistance"][swingStepIndex] = m_SensorProcess.iGetHandSideDistance;

        ResultProData["GetGripDistance"][swingStepIndex] = m_SensorProcess.iGetGripDistance;
        ResultProData["GetShoulderSideDirWorld"][swingStepIndex] = m_SensorProcess.iGetShoulderSideDirWorld;
        ResultProData["GetPelvisSideDirWorld"][swingStepIndex] = m_SensorProcess.iGetPelvisSideDirWorld;

        ResultProData["GetNoseShoulderSideDir"][swingStepIndex] = m_SensorProcess.iGetNoseShoulderSideDir;
        ResultProData["GetNosePelvisSideDir"][swingStepIndex] = m_SensorProcess.iGetNosePelvisSideDir;

        ResultProData["GetShoulderDir"][swingStepIndex] = m_SensorProcess.iGetShoulderDir;
        ResultProData["GetPelvisDir"][swingStepIndex] = m_SensorProcess.iGetPelvisDir;

        ResultProData["GetHandCombineDir"][swingStepIndex] = m_SensorProcess.iGetHandCombineDir;
    }

    private static Texture2D Rotate90CCW(Texture2D src)
    {
        int width = src.width;
        int height = src.height;

        Color32[] srcPixels = src.GetPixels32();
        Texture2D dst = new Texture2D(height, width, src.format, false);
        Color32[] dstPixels = new Color32[srcPixels.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int srcIdx = y * width + x;
                int dstX = y;
                int dstY = width - 1 - x;
                int dstIdx = dstY * height + dstX;

                dstPixels[dstIdx] = srcPixels[srcIdx];
            }
        }

        dst.SetPixels32(dstPixels);
        dst.Apply(false, false);

        return dst;
    }
}