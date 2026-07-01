using DG.Tweening;
using Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Debug = UnityEngine.Debug;
using UnityEngine.Rendering;

public class MirrorDirector : MonoBehaviour
{
    //[SerializeField] webcamclient webcamclient;
    [SerializeField] WebcamTrackerController webcamTrackerController;
    [SerializeField] WebcamTracker webcamTrackerFront;
    [SerializeField] WebcamTracker webcamTrackerSide;
    [SerializeField] SensorProcess sensorProcess;
    [SerializeField] DrawGuideLine drawGuideLineFront; 
    [SerializeField] DrawGuideLine drawGuideLineSide;
    [SerializeField] MirrorGuideController guideController;
    [SerializeField] TextMeshProUGUI txtAngle;
    [SerializeField] TextMeshProUGUI txtDebug;

    [Header("* Viewer Position")]
    [SerializeField] RectTransform PlayerViewFront;
    [SerializeField] RectTransform PlayerViewSide;
    [SerializeField] RectTransform ProVideoFront;
    [SerializeField] RectTransform ProVideoSide;
    [SerializeField] RectTransform ReplayFront;
    [SerializeField] RectTransform ReplaySide;
    [SerializeField] RectTransform RawImageFront;
    [SerializeField] RectTransform RawImageSide;

    [SerializeField] GameObject SplitBarY_UD;
    [SerializeField] GameObject SplitBarX_UD;
    [SerializeField] GameObject SplitBarY_SS;
    [SerializeField] GameObject SplitBarX_SS;

    [SerializeField] GameObject LaunchMonitorPanel;
    [SerializeField] GameObject LaunchMonitorEnable;

    [Header("* Video Player")]
    [SerializeField] VLCVideoPlayer videoProFront;
    //[SerializeField] VideoPlayer videoProSide;
    [SerializeField] VLCVideoPlayer videoProSide;
    [SerializeField] VideoPlayerControlMirror VideoPlayerControl;
    [SerializeField] VideoPlayerControlMirror proVideoPlayerControl;

    [Header("* Loading")]
    [SerializeField] RectTransform ReplayReadyUpdown;
    [SerializeField] RectTransform ReplayReadySide;
    [SerializeField] RectTransform ReplayProcessUpdown;
    [SerializeField] GameObject BlurBackUpdown;
    [SerializeField] RectTransform ReplayProcessSide;
    [SerializeField] GameObject BlurBackSide;
    bool isFirst = false;


    [Header("* Layout Option - ProVideo")]
    [SerializeField] Toggle tglProVideo;
    [SerializeField] GameObject ProVideoExt;
    
    [SerializeField] Toggle[] tglProSwingTypes;
    [SerializeField] Toggle tglProClubIron;
    [SerializeField] Toggle tglProClubDriver;
    [SerializeField] GameObject ProClubIronFixicon;
    
    [Header("* Layout Option - AI Coaching")]
    [SerializeField] Toggle tglCoaching;
    [SerializeField] GameObject CoachingMenuExt;    
    [SerializeField] Toggle tglCoachingPro;
    [SerializeField] Toggle[] tglSwingTypes;
    [SerializeField] Toggle tglClubIron;
    [SerializeField] Toggle tglClubDriver;
    [SerializeField] GameObject ClubIronFixicon;

    [Header("* Layout Option - Replay")]
    [SerializeField] Toggle tglReplay;
    [SerializeField] Button btnReplayList;

    [Header("* Layout Option - Mirror")]
    [SerializeField] Toggle tglMirror;

    [Header("* Layout Option - Option")]
    [SerializeField] Button btnSwap;
    [SerializeField] Toggle tglUpdown;
    //[SerializeField] Toggle tglProCoaching;


    [Header("* Replay Viewer")]
    [SerializeField] MirrorReplayListContoller mirrorReplayListContoller;

    [Space(5)]
    [SerializeField] GameObject LayoutChangedLockScreen;

    [Header("* Signal")]
    [SerializeField] Image imgSignal;
    [SerializeField] Toggle toggleDebug;
    [SerializeField] GameObject DebugViewer;


    bool _layoutProcess = false;
    bool _isSwap = true;
    bool _isProVideo = false;
    bool _isReplay = false;
    bool _isMirror = true;
    bool _isCoaching = false;
    public bool IsCoaching { get {return _isCoaching;} }
    bool _isUpdown = true;
    bool _isCoachingSwap = false;
    float AvgVisible = 0;
    bool _isProCoaching = true;

    bool proFrontEnd = false;
    bool proSideEnd = false;

    float _lastHandDir;
    bool _handCheck = false;

    //bool _isCoaching = false;

    [Space(5)]
    [SerializeField] GameObject ReplayUserInfo;
    [SerializeField] GameObject ButtonInfo;
    [SerializeField] RawImage rawImageFront;
    [SerializeField] RawImage rawImageSide;
    [SerializeField] GameObject SwingStepRoot;
    [SerializeField] GameObject CheckPoseInfoPanel;
    //int fps = 30;
    bool _isReplayUserInfo = false;
    //public int durationSeconds = 5;

    //private List<Texture2D> framesFront = new();
    //private List<Texture2D> framesSide = new();
    public List<byte[]> framesFront = new List<byte[]>();
    public List<byte[]> framesSide = new List<byte[]>();
    private bool isRecording = false;
    int widthFront;
    int heightFront;
    int widthSide;
    int heightSide;
    [SerializeField] TextMeshProUGUI txtButtonREC;
    bool checkTakeback = false;
    int checkTakebackFrame = 0;
    bool checkImpact = false;
    

    bool _replayReadyFront = false;
    bool _replayReadySide = false;

    string outputPathFront = string.Empty;
    string outputPathSide = string.Empty;

    string videoPath;

    bool userCheck = false;
    float userChechTimer = 0;

    enum RECODESTEP
    {
        READY = 0,
        RECORD,
        RECORDEND,
        MAKEFILE,
        MAKEFILEEND,
        REPLAY,
        REPLAYEND
    }
    RECODESTEP recStep = RECODESTEP.READY;

    public class ReplayInfo
    {
        public DateTime recordTime;
        public int replayIndex;
        public Texture2D thumbnail;
        public string frontPath;
        public string sidePath;

        /*public ReplayInfo(int index, string front, string side, Texture2D thumbnail)
        {
            recordTime = DateTime.Now;
            replayIndex = index;
            frontPath = front;
            sidePath = side;
            this.thumbnail = thumbnail; 
        }*/

        //public byte[] thumbnailBytes; // Texture2D 대신 byte[] 사용

        public ReplayInfo(int index, string front, string side, byte[] rawFrameData, int width, int height)
        {
            Debug.Log($"ReplayInfo({index}, {front}, {side}, {rawFrameData.Length}, {width}, {height})");
            // width나 height가 0이면 실행하지 않도록 방어 코드 추가
            if (width <= 0 || height <= 0) {
                Debug.LogError($"Invalid Texture Size: {width}x{height}");
                return; 
            }

            recordTime = DateTime.Now;
            replayIndex = index;
            frontPath = front;
            sidePath = side;

            // 1. Raw 데이터를 Texture2D로 일시적으로 로드하여 JPG로 압축
            Texture2D tempTex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tempTex.LoadRawTextureData(rawFrameData); // byte[]를 텍스처에 주입
            tempTex.Apply();

            // 2. 썸네일용 JPG 압축 저장 (메모리 최적화)
            //this.thumbnailBytes = tempTex.EncodeToJPG(75);
            thumbnail = tempTex;

            // 3. 임시 텍스처는 즉시 메모리 해제
            //Destroy(tempTex);
        }
    }
    int currentIndex = -1;
    Queue <ReplayInfo > qReplayInfos = new Queue<ReplayInfo>();

    ESwingType eSwingType = ESwingType.Full;
    EClub eClub = EClub.MiddleIron;

    [Header("* Launch Monitor")]
    [SerializeField] TextMeshProUGUI txtCarry;
    [SerializeField] TextMeshProUGUI txtTotal;
    [SerializeField] TextMeshProUGUI txtBall;
    [SerializeField] TextMeshProUGUI txtClub;


    IEnumerator Start()
    {
        LayoutChangedLockScreen.SetActive(true);

        toggleDebug.onValueChanged.AddListener(OnValueChanged_Debug);

        Init();

        yield return null;

        webcamTrackerController.SetTracker(FrontOn: true, SideOn: false);

        StartCoroutine(CheckPose());

        StartCoroutine(UserPoseCheck());

        LayoutChangedLockScreen.SetActive(false);

        sensorProcess.mirroViewType = EMirroViewType.SIDEBYSIDE;
    }

    private void Init()
    {
        videoPath = Path.Combine(Application.dataPath, "Videos");
        if (Directory.Exists(videoPath) == false)
            Directory.CreateDirectory(videoPath);

        //outputPathFront = Path.Combine(videoPath, "outputfront_{0}.webm");
        //outputPathSide = Path.Combine(videoPath, "outputside_{0}.webm");
        outputPathFront = Path.Combine(videoPath, "outputfront_{0}.mp4");
        outputPathSide = Path.Combine(videoPath, "outputside_{0}.mp4");

        int uid = GolfProDataManager.Instance.SelectProData.uid;
/*
        string proPath = GolfProDataManager.Instance.SelectProData.videoData.
            Where(v => v.direction == EPoseDirection.Front && v.sceneType == ESceneType.ProSelect).
            Select(v => v.path).FirstOrDefault();

        videoProFront.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{proPath}");

        proPath = GolfProDataManager.Instance.SelectProData.videoData.
            Where(v => v.direction == EPoseDirection.Side && v.sceneType == ESceneType.ProSelect).
        Select(v => v.path).FirstOrDefault();
        videoProSide.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{proPath}");
*/
        LoadProVideo();

        CheckPoseInfoPanel.SetActive(false);
        //videoProFront.loopPointReached += OnProVideoEndFront;
        //videoProSide.loopPointReached += OnProVideoEndSide;

        tglProVideo.onValueChanged.AddListener(OnValueChanged_Toggles);
        tglReplay.onValueChanged.AddListener(OnValueChanged_Toggles);
        //tglAlone.onValueChanged.AddListener(OnValueChanged_Toggles);
        tglCoaching.onValueChanged.AddListener(OnValueChanged_Toggles);
        tglMirror.onValueChanged.AddListener(OnValueChanged_Toggles);

        tglUpdown.onValueChanged.AddListener(OnValueChanged_Toggles);

        btnSwap.onClick.AddListener(OnClick_Swap);

        for(int i = 0; i < 3; i++)
        {
            tglProSwingTypes[i].onValueChanged.AddListener(OnValueChanged_ProVideoSwingType);
            tglSwingTypes[i].onValueChanged.AddListener(OnValueChanged_CoachingSwingType);
        }
        tglProClubIron.onValueChanged.AddListener(OnValueChanged_ProVideoSwingType);
        tglProClubDriver.onValueChanged.AddListener(OnValueChanged_ProVideoSwingType);
        tglClubIron.onValueChanged.AddListener(OnValueChanged_CoachingSwingType);
        tglClubDriver.onValueChanged.AddListener(OnValueChanged_CoachingSwingType);

        //tglProCoaching.onValueChanged.AddListener(OnValueChanged_ProCoaching);
        tglCoachingPro.onValueChanged.AddListener(OnValueChanged_ProCoaching);

        //기본
        for (int i = 0; i < 6; i++)
        {
            string frontPath = string.Format(outputPathFront, i);
            string sidePath = string.Format(outputPathSide, i);
            if (File.Exists(frontPath))
                File.Delete(frontPath);
            if (File.Exists(sidePath))
                File.Delete(sidePath);
        }

        imgSignal.enabled = false;

        StartCoroutine(CheckLaunchManager());
        /*
        if(LaunchManager.Instance.IsConnected())
        {
            LaunchManager.Instance.OnDataSend = OnDataSendProcess;
            LaunchManager.Instance.MonitorStart();
        }
        */
    }

    IEnumerator CheckLaunchManager()
    {
        LaunchMonitorPanel.SetActive(true);
        yield return null;
        
        LaunchMonitorEnable.SetActive(true);

        while(LaunchManager.Instance.IsConnected() == false)
        {
            yield return new WaitForSeconds(3f);
            LaunchManager.Instance.Connect();            
            //LaunchMonitorEnable.SetActive(false);
        }

        LaunchManager.Instance.OnDataSend = OnDataSendProcess;
        LaunchManager.Instance.MonitorStart();
        LaunchMonitorEnable.SetActive(false);
/*
        if(_isCoaching == false)
        {
            LaunchMonitorPanel.SetActive(true);
        }*/
    }

    void OnDataSendProcess(RspData rspData)
    {
        if (float.TryParse(rspData.BALL, out float ball))
            txtBall.text = $"{ball:F1}m/s";
        else
            txtBall.text = "--m/s";

        if (float.TryParse(rspData.CLUB, out float club))
            txtClub.text = $"{club:F1}m/s";
        else
            txtClub.text = "--m/s";

        if (int.TryParse(rspData.CARRY, out int carry))
            txtCarry.text = $"{carry}m";
        else
            txtCarry.text = "--m";

        if (int.TryParse(rspData.TOTAL, out int total))
            txtTotal.text = $"{total}m";
        else
            txtTotal.text = "--m";

    }

    void LoadProVideo()
    {
        int uid = GolfProDataManager.Instance.SelectProData.uid;   

        //string fileName = $"{INI.proVideoPath}{uid}/front_video_{(int)eSwingType}{(int)eClub}.webm";
        string fileName = $"{INI.proVideoPath}{uid}/front_video_{(int)eSwingType}{(int)eClub}.mp4";
        Debug.Log($"LoadProVideo() - {fileName}");;
        if(File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOME"), fileName)))
        {
            videoProFront.url = GameManager.Instance.LoadVideoURL(fileName);

            //fileName = $"{INI.proVideoPath}{uid}/side_video_{(int)eSwingType}{(int)eClub}.webm";
            fileName = $"{INI.proVideoPath}{uid}/side_video_{(int)eSwingType}{(int)eClub}.mp4";
            if(File.Exists(Path.Combine(Environment.GetEnvironmentVariable("HOME"), fileName)))
                videoProSide.url = GameManager.Instance.LoadVideoURL(fileName);
        }
        else
        {
            if(_isProVideo)
                Utillity.Instance.ShowToast("선택한 항목에 대한 프로영상이 없습니다.");

            videoProFront.url = null;
            videoProSide.url = null;         
        }
    }

    IEnumerator UserPoseCheck()
    {
        float sec = 0;

        while(true)
        {
            //yield return new WaitUntil(() => _isCoaching == true);

            if(_isCoaching)
            {
                if(userCheck == true)
                {
                    if(sensorProcess.Normal == false)
                    {
                        userChechTimer += Time.deltaTime;

                        if(userChechTimer > 0.5f)
                        {
                            userCheck = false;
                            userChechTimer = 0;
                            
                            drawGuideLineFront.SetDraw(userCheck, _isCoaching);
                            drawGuideLineSide.SetDraw(userCheck, _isCoaching);
                            guideController.SetCoaching(userCheck);
                            guideController.SetDrill(false);
                        }
                    }
                    else
                        userChechTimer = 0;
                }
                else //if(userCheck == false && sensorProcess.Normal == true)
                {
                    if(sensorProcess.Normal == true)
                    {
                        userChechTimer += Time.deltaTime;

                        if(userChechTimer > 0.5f)
                        {
                            userCheck = true;
                            userChechTimer = 0;
                            
                            drawGuideLineFront.SetDraw(userCheck, _isCoaching);
                            drawGuideLineSide.SetDraw(userCheck, _isCoaching);
                            guideController.SetCoaching(userCheck);
                        }
                    }
                    else
                        userChechTimer = 0;
                }

                //yield return new WaitForSeconds(1);
                //txtDebug.text = $"{webcamTrackerSide.visibilityAvg} => {sec}";
                if (webcamTrackerSide.visibilityAvg > 0.25f)
                    sec = 0;
                else
                {
                    sec += Time.deltaTime;

                    if (sec >= Utillity.Instance.mirrorModeTimeout) //지정시간 뒤 시작화면으로
                    {
                        GameManager.Instance.SelectedSceneName = string.Empty;
                        SceneManager.LoadScene("Login");
                        yield break;
                    }
                }
            }

            

            yield return null;
        }
    }

    public void Onclick_Button()
    {
        GameObject obj = EventSystem.current.currentSelectedGameObject;

        switch (obj.name)
        {
            case "Home":
                GameManager.Instance.SelectedSceneName = string.Empty;
                if(LaunchManager.Instance.IsConnected())
                {
                    LaunchManager.Instance.OnDataSend = null;
                    LaunchManager.Instance.MonitorStop();
                }
                SceneManager.LoadScene("ModeSelect");
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;
        }
    }

    public void OnClick_ReplayViewer()
    {
        List<ReplayInfo> replayInfo = qReplayInfos.ToList();
        replayInfo.Reverse();

        VideoPlayerControl.StopVideo();
        mirrorReplayListContoller.SetReplays(replayInfo, () => VideoPlayerControl.PlayVideo());

        SetReady();
    }

    public void OnValueChanged_ProVideo(bool isOn)
    {
        //ProVideoGroup.SetActive(isOn);
    }

    public void OnValueChanged_SideView(bool isOn)
    {
        _isReplay = isOn;
    }

    public void OnValueChanged_Toggles(bool isOn)
    {
        if (isOn == false)
            return;

        if (_layoutProcess)
            return;

        LayoutChangedLockScreen.SetActive(true);

        _layoutProcess = true;

        _isUpdown = tglUpdown.isOn;
        _isProVideo = tglProVideo.isOn;
        _isMirror = tglMirror.isOn;
        _isCoaching = tglCoaching.isOn;

        _isReplay = tglReplay.isOn;

        Debug.Log($"_isReplay:{_isReplay}/_isProVideo:{_isProVideo}/_isMirror:{_isMirror}/_isCoaching:{_isCoaching}");

        if(_isProVideo)
        {
            videoProFront.playbackSpeed = INI.PlaySpeedNormal;
            videoProSide.playbackSpeed = INI.PlaySpeedNormal;
            videoProFront.Play();
            videoProSide.Play();
            ReplayReadyUpdown.gameObject.SetActive(false);
            ReplayReadySide.gameObject.SetActive(false);
            ReplayUserInfo.SetActive(false);
            proVideoPlayerControl.gameObject.SetActive(true);
            proVideoPlayerControl.PlayVideo();

            sensorProcess.mirroViewType = EMirroViewType.SIDEBYSIDE;
        }
        else if(_isReplay)
        {
            videoProFront.Stop();
            videoProSide.Stop();
            isFirst = false;
            ReplayReadyUpdown.gameObject.SetActive(_isUpdown ? true : false);
            ReplayReadySide.gameObject.SetActive(!_isUpdown ? true : false);
            _isReplayUserInfo = false;
            ReplayUserInfo.SetActive(false);
            proVideoPlayerControl.gameObject.SetActive(false);
            proVideoPlayerControl.StopVideo();

            sensorProcess.mirroViewType = EMirroViewType.SIDEBYSIDE;
        }
        else if(_isCoaching)
        {
            videoProFront.Stop();
            videoProSide.Stop();
            ReplayReadyUpdown.gameObject.SetActive(false);
            ReplayReadySide.gameObject.SetActive(false);
            ReplayUserInfo.SetActive(false);
            proVideoPlayerControl.gameObject.SetActive(false);
            //proVideoPlayerControl.StopVideo();        
        }
        else //mirror
        {
            videoProFront.Stop();
            videoProSide.Stop();
            ReplayReadyUpdown.gameObject.SetActive(false);
            ReplayReadySide.gameObject.SetActive(false);
            ReplayUserInfo.SetActive(false);
            proVideoPlayerControl.gameObject.SetActive(false);
            proVideoPlayerControl.StopVideo();

            sensorProcess.mirroViewType = EMirroViewType.SIDEBYSIDE;
        }
            
        ButtonInfo.SetActive((_isMirror) ? true : false);
        VideoPlayerControl.gameObject.SetActive(false);// this.VideoPlayerControl.GetPrepared());
        //btnSwap.gameObject.SetActive(!tglAlone.isOn);


        StartCoroutine(SetViewerLayout(_isUpdown, _isSwap));
    }
    /*
    public void OnValueChanged_Coaching(bool isOn)
    {
        if (_isCoaching != isOn)
        {
            //webcamTrackerSide.SetTrack(isOn);
            webcamTrackerController.SetTracker(FrontOn: true, SideOn: isOn);
            drawGuideLineFront.SetDraw(isOn);
            drawGuideLineSide.SetDraw(isOn);
            guideController.SetCoaching(isOn);
            _isCoaching = isOn;
        }
    }*/

    public void OnClick_Swap()
    {
        _isSwap = !_isSwap;
        //OnValueChanged_Toggles(true);
        bool ProVideo = tglProVideo.isOn;
        StartCoroutine(SetViewerLayout(_isUpdown, _isSwap));
    }

    IEnumerator SetViewerLayout(bool UpDown, bool swap)
    {
        if (UpDown)
        {            
            if (_isProVideo || _isReplay)
            {
                if (swap == false)
                {
                    //상하, 프로ON
                    PlayerViewFront.anchoredPosition = new Vector2(0, 0);
                    PlayerViewSide.anchoredPosition = new Vector2(540, 0);
                    PlayerViewFront.sizeDelta = new Vector2(540, 920);
                    PlayerViewSide.sizeDelta = new Vector2(540, 920);
                    RawImageFront.sizeDelta = new Vector2(960, 540);
                    RawImageSide.sizeDelta = new Vector2(960, 540);

                    ProVideoFront.anchoredPosition = new Vector2(0, -920);
                    ProVideoSide.anchoredPosition = new Vector2(540, -920);
                    ReplayFront.anchoredPosition = new Vector2(0, -920);
                    ReplaySide.anchoredPosition = new Vector2(540, -920);

                    ReplayReadyUpdown.anchoredPosition = new Vector2(0, -920);
                    ReplayProcessUpdown.anchoredPosition = new Vector2(0, -920);
                }
                else
                {
                    //하상, 프로ON
                    PlayerViewFront.anchoredPosition = new Vector2(0, -920);
                    PlayerViewSide.anchoredPosition = new Vector2(540, -920);
                    PlayerViewFront.sizeDelta = new Vector2(540, 920);
                    PlayerViewSide.sizeDelta = new Vector2(540, 920);
                    RawImageFront.sizeDelta = new Vector2(960, 540);
                    RawImageSide.sizeDelta = new Vector2(960, 540);

                    ProVideoFront.anchoredPosition = new Vector2(0, 0);
                    ProVideoSide.anchoredPosition = new Vector2(540, 0);
                    ReplayFront.anchoredPosition = new Vector2(0, 0);
                    ReplaySide.anchoredPosition = new Vector2(540, 0);

                    ReplayReadyUpdown.anchoredPosition = new Vector2(0, 0);
                    ReplayProcessUpdown.anchoredPosition = new Vector2(0, 0);
                }

                SplitBarY_UD.SetActive(true);
                SplitBarY_UD.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 0);
                SplitBarY_UD.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                SplitBarX_UD.SetActive(true);
                SplitBarY_SS.SetActive(false);
                SplitBarX_SS.SetActive(false);

                tglCoaching.isOn = false;
            }
            else if(_isCoaching)
            {
                //상하, 프로OFF
                PlayerViewFront.anchoredPosition = new Vector2(0, 0 );
                PlayerViewSide.anchoredPosition = new Vector2(0, 0);// -1380);
                PlayerViewFront.sizeDelta = new Vector2(1080, 1920);
                PlayerViewSide.sizeDelta = new Vector2(270, 460);
                RawImageFront.sizeDelta = new Vector2(1920, 1080);
                RawImageSide.sizeDelta = new Vector2(480, 270);
                PlayerViewSide.transform.SetAsLastSibling();

                ProVideoFront.anchoredPosition = new Vector2(0, -920);
                ProVideoSide.anchoredPosition = new Vector2(540, -920);
                ReplayFront.anchoredPosition = new Vector2(0, -920);
                ReplaySide.anchoredPosition = new Vector2(540, -920);

                ReplayReadyUpdown.anchoredPosition = new Vector2(0, -920);
                ReplayProcessUpdown.anchoredPosition = new Vector2(0, -920);

                SplitBarY_UD.SetActive(false);
                SplitBarX_UD.SetActive(false);
                SplitBarY_SS.SetActive(false);
                SplitBarX_SS.SetActive(false);

                sensorProcess.mirroViewType = EMirroViewType.FRONTMAIN;
            }
            else
            {
                //상하, 프로OFF
                PlayerViewFront.anchoredPosition = new Vector2(0, -228);
                PlayerViewSide.anchoredPosition = new Vector2(540, -228);
                //PlayerViewFront.anchoredPosition = new Vector2(0, 0);//임시
                //PlayerViewSide.anchoredPosition = new Vector2(540, 0);
                PlayerViewFront.sizeDelta = new Vector2(540, 920);
                PlayerViewSide.sizeDelta = new Vector2(540, 920);
                RawImageFront.sizeDelta = new Vector2(960, 540);
                RawImageSide.sizeDelta = new Vector2(960, 540);

                ProVideoFront.anchoredPosition = new Vector2(0, -920);
                ProVideoSide.anchoredPosition = new Vector2(540, -920);
                ReplayFront.anchoredPosition = new Vector2(0, -920);
                ReplaySide.anchoredPosition = new Vector2(540, -920);

                ReplayReadyUpdown.anchoredPosition = new Vector2(0, -920);
                ReplayProcessUpdown.anchoredPosition = new Vector2(0, -920);

                SplitBarY_UD.SetActive(true);
                SplitBarY_UD.GetComponent<RectTransform>().sizeDelta = new Vector2(2, -920);                
                SplitBarY_UD.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 232);
                //SplitBarY_UD.GetComponent<RectTransform>().sizeDelta = new Vector2(2, -920);//임시
                //SplitBarY_UD.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 460); //임시
                SplitBarX_UD.SetActive(false);
                SplitBarY_SS.SetActive(false);
                SplitBarX_SS.SetActive(false);
            }
        }
        else
        {
            if (_isProVideo || _isReplay)
            {
                if (swap == false)
                {
                    //좌우, 프로ON
                    PlayerViewFront.anchoredPosition = new Vector2(0, 0);
                    PlayerViewSide.anchoredPosition = new Vector2(0, -920);
                    PlayerViewFront.sizeDelta = new Vector2(540, 920);
                    PlayerViewSide.sizeDelta = new Vector2(540, 920);
                    RawImageFront.sizeDelta = new Vector2(960, 540);
                    RawImageSide.sizeDelta = new Vector2(960, 540);

                    ProVideoFront.anchoredPosition = new Vector2(540, 0);
                    ProVideoSide.anchoredPosition = new Vector2(540, -920);
                    ReplayFront.anchoredPosition = new Vector2(540, 0);
                    ReplaySide.anchoredPosition = new Vector2(540, -920);

                    ReplayReadySide.anchoredPosition = new Vector2(540, 0);
                    ReplayProcessSide.anchoredPosition = new Vector2(540, 0);
                }
                else
                {
                    //우좌, 프로ON
                    PlayerViewFront.anchoredPosition = new Vector2(540, 0);
                    PlayerViewSide.anchoredPosition = new Vector2(540, -920);
                    PlayerViewFront.sizeDelta = new Vector2(540, 920);
                    PlayerViewSide.sizeDelta = new Vector2(540, 920);
                    RawImageFront.sizeDelta = new Vector2(960, 540);
                    RawImageSide.sizeDelta = new Vector2(960, 540);

                    ProVideoFront.anchoredPosition = new Vector2(0, 0);
                    ProVideoSide.anchoredPosition = new Vector2(0, -920);
                    ReplayFront.anchoredPosition = new Vector2(0, 0);
                    ReplaySide.anchoredPosition = new Vector2(0, -920);

                    ReplayReadySide.anchoredPosition = new Vector2(0, 0);
                    ReplayProcessSide.anchoredPosition = new Vector2(0, 0);
                }

                SplitBarX_SS.SetActive(true);
                SplitBarX_SS.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
                SplitBarX_SS.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                SplitBarY_SS.SetActive(true);
                SplitBarY_UD.SetActive(false);
                SplitBarX_UD.SetActive(false);

                tglCoaching.isOn = false;
            }
            else if(_isCoaching)
            {
                //좌우, 프로OFF
                PlayerViewFront.anchoredPosition = new Vector2(0, 0);
                PlayerViewSide.anchoredPosition = new Vector2(0, 0);// -1380);
                PlayerViewFront.sizeDelta = new Vector2(540, 920);
                PlayerViewSide.sizeDelta = new Vector2(540, 920);
                RawImageFront.sizeDelta = new Vector2(960, 540);
                RawImageSide.sizeDelta = new Vector2(960, 540);
                PlayerViewSide.transform.SetAsLastSibling();

                ProVideoFront.anchoredPosition = new Vector2(0, -920);
                ProVideoSide.anchoredPosition = new Vector2(540, -920);
                ReplayFront.anchoredPosition = new Vector2(0, -920);
                ReplaySide.anchoredPosition = new Vector2(540, -920);

                ReplayReadySide.anchoredPosition = new Vector2(540, 0);
                ReplayProcessSide.anchoredPosition = new Vector2(540, 0);

                SplitBarX_SS.SetActive(true);
                SplitBarX_SS.GetComponent<RectTransform>().sizeDelta = new Vector2(-540, 2);
                SplitBarX_SS.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                SplitBarY_SS.SetActive(false);
                SplitBarY_UD.SetActive(false);
                SplitBarX_UD.SetActive(false);
            }
            else
            {
                //좌우, 프로OFF
                PlayerViewFront.anchoredPosition = new Vector2(270, 0);
                PlayerViewSide.anchoredPosition = new Vector2(270, -920);
                PlayerViewFront.sizeDelta = new Vector2(540, 920);
                PlayerViewSide.sizeDelta = new Vector2(540, 920);
                RawImageFront.sizeDelta = new Vector2(960, 540);
                RawImageSide.sizeDelta = new Vector2(960, 540);

                ProVideoFront.anchoredPosition = new Vector2(0, -920);
                ProVideoSide.anchoredPosition = new Vector2(540, -920);
                ReplayFront.anchoredPosition = new Vector2(0, -920);
                ReplaySide.anchoredPosition = new Vector2(540, -920);

                ReplayReadySide.anchoredPosition = new Vector2(540, 0);
                ReplayProcessSide.anchoredPosition = new Vector2(540, 0);

                SplitBarX_SS.SetActive(true);
                SplitBarX_SS.GetComponent<RectTransform>().sizeDelta = new Vector2(-540, 2);
                SplitBarX_SS.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
                SplitBarY_SS.SetActive(false);
                SplitBarY_UD.SetActive(false);
                SplitBarX_UD.SetActive(false);
            }
        }

        ProVideoFront.gameObject.SetActive(_isProVideo);
        ProVideoSide.gameObject.SetActive(_isProVideo);
        ReplayFront.gameObject.SetActive(_isReplay);
        ReplaySide.gameObject.SetActive(_isReplay);

        //btnSwap.gameObject.SetActive((ProVideo || Replay) ? true : false);
        //btnSwap.interactable = ((_isProVideo || _isReplay) ? true : false);
        btnReplayList.gameObject.SetActive(_isReplay);
        //tglCoaching.interactable = ((_isProVideo || _isReplay) ? false : true);

        ReplayProcessUpdown.gameObject.SetActive(false);
        ReplayProcessSide.gameObject.SetActive(false);



        yield return null;// new WaitForEndOfFrame();
                
        //webcamTrackerController.SetTracker(FrontOn: true, SideOn: _isCoaching);
        webcamTrackerController.SetTracker(FrontOn: (_isCoaching || _isReplay), SideOn: _isCoaching);
        drawGuideLineFront.SetDraw(_isCoaching, _isCoaching);
        drawGuideLineSide.SetDraw(_isCoaching, _isCoaching);
        guideController.SetCoaching(_isCoaching);
        CheckPoseInfoPanel.SetActive(_isCoaching);
        CoachingMenuExt.SetActive(_isCoaching);
        tglCoaching.gameObject.GetComponent<LayoutElement>().ignoreLayout = _isCoaching ? true : false;                
        if(_isCoaching)
        {
            if(tglSwingTypes[0].isOn)
            {
                tglClubDriver.gameObject.SetActive(true);
                ClubIronFixicon.SetActive(false);
            }
            else
            {
                tglClubIron.isOn = true;
                tglClubDriver.gameObject.SetActive(false);
                ClubIronFixicon.SetActive(true);
            }
        }

        //if(LaunchManager.Instance.IsConnected())
        //    LaunchMonitorPanel.SetActive(!_isCoaching);
        LaunchMonitorPanel.SetActive(!_isCoaching);

        ProVideoExt.SetActive(_isProVideo);
        tglProVideo.gameObject.GetComponent<LayoutElement>().ignoreLayout = _isProVideo ? true : false;
        if(_isProVideo)
        {
            if(tglProSwingTypes[0].isOn)
            {             
                tglProClubDriver.gameObject.SetActive(true);
                ProClubIronFixicon.SetActive(false);   
            }
            else
            {
                tglProClubIron.isOn = true;
                tglProClubDriver.gameObject.SetActive(false);
                ProClubIronFixicon.SetActive(true);
            }
        }

        yield return null;
        LayoutChangedLockScreen.SetActive(false);
        _layoutProcess = false;
    }

    public void OnClick_CoachingSwap()
    {
        if (_isCoaching == false)
            return;

        _isCoachingSwap = !_isCoachingSwap;

        if(_isCoachingSwap == false)
        {
            PlayerViewFront.anchoredPosition = new Vector2(0, 0);
            PlayerViewSide.anchoredPosition = new Vector2(0, 0);// -1380);
            PlayerViewFront.sizeDelta = new Vector2(1080, 1920);
            PlayerViewSide.sizeDelta = new Vector2(270, 460);
            RawImageFront.sizeDelta = new Vector2(1920, 1080);
            RawImageSide.sizeDelta = new Vector2(480, 270);

            PlayerViewSide.transform.SetAsLastSibling(); 

            sensorProcess.mirroViewType = EMirroViewType.FRONTMAIN;
        }
        else
        {
            PlayerViewFront.anchoredPosition = new Vector2(0, 0);// -1380);
            PlayerViewSide.anchoredPosition = new Vector2(0, 0);
            PlayerViewFront.sizeDelta = new Vector2(270, 460); 
            PlayerViewSide.sizeDelta = new Vector2(1080, 1920);
            RawImageFront.sizeDelta = new Vector2(480, 270); 
            RawImageSide.sizeDelta = new Vector2(1920, 1080);

            PlayerViewFront.transform.SetAsLastSibling();

            sensorProcess.mirroViewType = EMirroViewType.SIDEMAIN;
        }

        webcamTrackerController.SetTracker(FrontOn: true, SideOn: _isCoaching);        
        drawGuideLineFront.SetDraw(_isCoaching, _isCoaching);
        drawGuideLineSide.SetDraw(_isCoaching, _isCoaching);
        guideController.SetCoaching(_isCoaching);

    }

    public void OnValueChanged_ProCoaching(bool isOn)
    {
        if (isOn)
        {
            if (_isProCoaching)
                return;
            else
            {
                CheckPoseInfoPanel.transform.DOMoveY(80, 0.5f).From(-86);
                guideController.CoachingVisible = true;
            }
        }
        else
        {
            if (_isProCoaching == false)
                return;
            else
            {
                CheckPoseInfoPanel.transform.DOMoveY(-86, 0.5f).From(80);
                guideController.CoachingVisible = false;
            }
        }

        _isProCoaching = isOn;        
    }

    public void OnValueChanged_ProVideoSwingType(bool isOn)
    {
        if(isOn == false)
            return;

        Utillity.Instance.HideToast();

        if(_isProVideo)
        {
            if(tglProSwingTypes[0].isOn)
            {
                eSwingType = ESwingType.Full;
                tglProClubDriver.gameObject.SetActive(true);
                ProClubIronFixicon.SetActive(false);
            }
            else
            {
                if(tglProSwingTypes[1].isOn)
                    eSwingType = ESwingType.ThreeQuarter;
                else 
                    eSwingType = ESwingType.Half;

                tglProClubIron.isOn = true;
                tglProClubDriver.gameObject.SetActive(false);
                ProClubIronFixicon.SetActive(true);
            }

            if(tglProClubIron.isOn)
                eClub = EClub.MiddleIron;
            else
                eClub = EClub.Driver;
        }

        //데이터 로드
        LoadProVideo();
        proVideoPlayerControl.PlayVideo();
    }

    public void OnValueChanged_CoachingSwingType(bool isOn)
    {
        if(isOn == false)
            return;

        Utillity.Instance.HideToast();

        if(tglSwingTypes[0].isOn)
        {
            guideController.eSwingType = ESwingType.Full;
            tglClubDriver.gameObject.SetActive(true);
            ClubIronFixicon.SetActive(false);
        }
        else
        {
            if(tglSwingTypes[1].isOn)
                guideController.eSwingType = ESwingType.ThreeQuarter;
            else 
                guideController.eSwingType = ESwingType.Half;
                
            tglClubIron.isOn = true;
            tglClubDriver.gameObject.SetActive(false);
            ClubIronFixicon.SetActive(true);
        }

        if(tglClubIron.isOn)
                guideController.eClub = EClub.MiddleIron;
            else
                guideController.eClub = EClub.Driver;

        //데이터 로드
        if(guideController.LoadSwingStepData() == false)
        {
            if(_isCoaching)
                Utillity.Instance.ShowToast("선택한 항목에 대한 프로 데이터가 없습니다.");

            //guideController.eSwingType = ESwingType.Full;
            tglClubIron.SetIsOnWithoutNotify(true);
            guideController.eClub = EClub.MiddleIron;
            tglSwingTypes[0].isOn = true;
        }        
    }


    /*
    public void OnClick_TestREC()
    {
        if (isRecording)
        {
            txtButtonREC.text = "REC";
            txtButtonREC.color = Color.black;
            StopRecording();
        }
        else
        {
            txtButtonREC.text = "STOP";
            txtButtonREC.color = Color.red;
            StartRecording();
        }
    }

    public void StartRecording()
    {

        isRecording = true;
        framesFront.Clear();
        framesSide.Clear();
        StartCoroutine(CaptureFrames());
    }

    public void StopRecording()
    {

        isRecording = false;
    }
    */



    //-----------------------------------------------------------------------
    // 영상 처리부
    //-----------------------------------------------------------------------
    //TODO:AICoaching에 사용 된 Director에 중복으로 사용되는 문제 개선필요
    IEnumerator CheckPose()
    {
        float timer = 0;
        SetReady();

        while (true)
        {/*
            if(_isCoaching)
            {
                if(userCheck == true)
                {
                    if(sensorProcess.Normal == false)
                    {
                        userChechTimer += Time.deltaTime;

                        if(userChechTimer > 0.5f)
                        {
                            userCheck = false;
                            userChechTimer = 0;
                            
                            drawGuideLineFront.SetDraw(userCheck);
                            drawGuideLineSide.SetDraw(userCheck);
                            guideController.SetCoaching(userCheck);
                        }
                    }
                    else
                        userChechTimer = 0;
                }
                else //if(userCheck == false && sensorProcess.Normal == true)
                {
                    if(sensorProcess.Normal == true)
                    {
                        userChechTimer += Time.deltaTime;

                        if(userChechTimer > 0.5f)
                        {
                            userCheck = true;
                            userChechTimer = 0;
                            
                            drawGuideLineFront.SetDraw(userCheck);
                            drawGuideLineSide.SetDraw(userCheck);
                            guideController.SetCoaching(userCheck);
                        }
                    }
                    else
                        userChechTimer = 0;
                }
            }
*/

            _lastHandDir = sensorProcess.iGetHandDir;
            _handCheck = sensorProcess.IsAddressHand(false);//sensorProcess.iGetHandDistance < 80f ? true : false;
            //AvgVisible = sensorProcess.visibilityFront;

            yield return new WaitUntil(() => _isReplay == true);
            //yield return new WaitUntil(() => _isCoaching == true);

            //어드레스 감지
            if (recStep.Equals(RECODESTEP.READY))
            {
                if (_handCheck && (_lastHandDir < 190f && _lastHandDir > 170))
                {
                    txtDebug.text = "어드레스 감지";
                    imgSignal.enabled = true;
                    imgSignal.color = Color.green;
                    timer += Time.deltaTime;
                    if (timer > 0.25f)
                    {
                        isRecording = true;
                        //framesFront.Clear();
                        recStep = RECODESTEP.RECORD;
                        StartCoroutine(CaptureFrames());
                        txtDebug.text = "녹화 중";
                        imgSignal.color = Color.red;
                        if (_isReplayUserInfo == true)
                            ReplayUserInfo.SetActive(false);
                    }
                }
                else
                {
                    timer = 0;
                    checkTakeback = false;
                    checkImpact = false;
                    checkTakebackFrame = 0;
                    txtDebug.text = "준비";
                    imgSignal.enabled = false;
                }
            }

            else if (recStep.Equals(RECODESTEP.RECORD))
            {
                //테이크백 감지
                if (checkTakeback == false && checkImpact == false)
                {
                    if(_lastHandDir < 150f)
                        checkTakeback = true;

                    if(_handCheck == false)
                        SetReady();
                }
                //임팩트 감지
                else if (checkTakeback == true && checkImpact == false)
                {
                    if(_lastHandDir > 170f)
                        checkImpact = true;
                }

                //txtAngle.text = $"T:{checkTakeback},I:{checkImpact},H:{_lastHandDir}"; //framesFront.Count.ToString();
                if (framesFront.Count > 200)
                    SetReady();
            } 
            else if (recStep.Equals(RECODESTEP.RECORDEND))
            {
                if (_isUpdown)
                {
                    if(isFirst == false)
                    {
                        ReplayProcessUpdown.gameObject.SetActive(true);
                        isFirst = true;
                    }
                    BlurBackUpdown.SetActive(ReplayReadyUpdown.gameObject.activeInHierarchy);
                }
                else
                {
                    if(isFirst == false)
                    {
                        ReplayProcessSide.gameObject.SetActive(true);
                        isFirst = true;
                    }
                    BlurBackSide.SetActive(ReplayReadySide.gameObject.activeInHierarchy);
                }

                this.VideoPlayerControl.gameObject.SetActive(false);
                txtDebug.text = " 리플레이 준비 중";
                recStep = RECODESTEP.MAKEFILE;
                //VideoPlayerControl.ReleaseVIdeo();
                yield return null;
                currentIndex++;
                if (currentIndex > 5)
                {
                    qReplayInfos.Dequeue();
                    currentIndex = 0;
                }
                string frontPath = string.Format(outputPathFront, currentIndex);
                string sidePath = string.Format(outputPathSide, currentIndex);
                //savePng(framesFront.ToArray());
                Debug.Log($"framesFront Count:{framesFront.Count}");
                StartCoroutine(SendFramesToFFmpeg(frontPath, widthFront, heightFront, true, framesFront, () => _replayReadyFront = true));
                StartCoroutine(SendFramesToFFmpeg(sidePath, widthSide, heightSide, false, framesSide, () => _replayReadySide = true));

                //if (qReplayInfos.Count > 5)
                // ReplayInfo를 만드는 시점 (예: 캡처 종료 후)
                if (framesFront.Count > 0 && widthFront > 0 && heightFront > 0)
                {
                    qReplayInfos.Enqueue(new ReplayInfo(currentIndex, frontPath, sidePath, framesFront[framesFront.Count/3], widthFront, heightFront));
                }
                else
                {
                    Debug.LogWarning("아직 캡처된 프레임이 없거나 해상도가 잡히지 않았습니다.");
                }
                //qReplayInfos.Enqueue(new ReplayInfo(currentIndex, frontPath, sidePath, framesFront[0], widthFront, heightFront));
                yield return new WaitUntil(() => _replayReadyFront == true && _replayReadySide == true);
                recStep = RECODESTEP.REPLAY;

                yield return StartCoroutine(Replay());
            }

            //if(_isReplay == false)
            //    yield break;

            yield return null;
        }
    }
/*
    void savePng(in Texture2D[] texs)
    {
        for(int i = 0; i < texs.Length; i++)
        {
            byte[] texPngByte = texs[i].EncodeToJPG();
            File.WriteAllBytes($"{videoPath}/{i}.png", texPngByte);
        }
    }
*/
    IEnumerator Replay()
    {
        videoProFront.Stop();
        videoProSide.Stop();
        VideoPlayerControl.gameObject.SetActive(true);
        yield return null;
        //VideoPlayerControl.StopVideo();


        if (_isUpdown)
        {
            ReplayReadyUpdown.gameObject.SetActive(false);
            ReplayProcessUpdown.gameObject.SetActive(false);
        }
        else
        {
            ReplayReadySide.gameObject.SetActive(false);
            ReplayProcessSide.gameObject.SetActive(false);
        }

        

        if (_isReplay == false)
        {
            SetReady();
            yield break;
        }
        VideoPlayerControl.ReleaseVIdeo();
        //yield return null;
        //VideoPlayerControl.StopVideo();
        yield return new WaitForSeconds(0.1f);
        txtDebug.text = "리플레이 재생 " + currentIndex;

        //VideoPlayerControl.StopVideo();
        string frontPath = string.Format(outputPathFront, currentIndex);
        string sidePath = string.Format(outputPathSide, currentIndex);
        Debug.Log($"VideoPlayerControl.PlayVideo({frontPath}, {sidePath});");
        VideoPlayerControl.PlayVideo(frontPath, sidePath);
        //VideoPlayerControl.PlayVideo(outputPathFront, outputPathSide);

        yield return new WaitForSeconds((float)VideoPlayerControl.GetEndTime());
        if(_isReplayUserInfo == false)
        {
            _isReplayUserInfo = true;
            ReplayUserInfo.SetActive(true);
        }
        recStep = RECODESTEP.REPLAYEND;
        //txtDebug.text = "리플레이 종료";
        yield return new WaitForSeconds(0.5f);
        
        SetReady();
    }

    void SetReady()
    {
        recStep = RECODESTEP.READY;
        //txtDebug.text = "준비";
        checkTakeback = false;
        checkImpact = false;
        checkTakebackFrame = 0;
        isRecording = false;

        framesFront.Clear();
        framesSide.Clear();

        Resources.UnloadUnusedAssets();

        _replayReadyFront = false;
        _replayReadySide = false;
    }

    // 두 프레임을 하나로 묶는 구조체
    public struct FrameSet
    {
        public byte[] frontData;
        public byte[] sideData;
    }

    // 리스트 하나로 관리 (선택 사항이지만 추천함)
    public List<FrameSet> dualFrames = new List<FrameSet>();
    
    private void CaptureDualCameras()
    {
        Texture texFront = rawImageFront.texture;
        Texture texSide = rawImageSide.texture;

        // 1. 임시 RenderTexture 생성
        RenderTexture rtFront = RenderTexture.GetTemporary(texFront.width, texFront.height, 0);
        RenderTexture rtSide = RenderTexture.GetTemporary(texSide.width, texSide.height, 0);

        // 2. GPU 복사
        Graphics.Blit(texFront, rtFront);
        Graphics.Blit(texSide, rtSide);

        // 3. 비동기 요청 (두 개를 동시에 던짐)
        var requestFront = AsyncGPUReadback.Request(rtFront, 0, TextureFormat.RGB24);
        var requestSide = AsyncGPUReadback.Request(rtSide, 0, TextureFormat.RGB24);

        // 4. 별도의 코루틴이나 체크 로직을 통해 두 데이터가 모두 완료될 때까지 대기
        StartCoroutine(WaitAndSaveFrames(requestFront, requestSide, rtFront, rtSide));
    }

    private IEnumerator WaitAndSaveFrames(AsyncGPUReadbackRequest reqFront, AsyncGPUReadbackRequest reqSide, RenderTexture rtF, RenderTexture rtS)
    {
        // 두 요청이 모두 끝날 때까지 대기 (CPU는 쉬지 않고 다른 일 함)
        while (!reqFront.done || !reqSide.done)
        {
            yield return null;
        }

        if (!reqFront.hasError && !reqSide.hasError)
        {
            // 두 프레임을 동시에 리스트에 추가 (완벽한 동기화)
            framesFront.Add(reqFront.GetData<byte>().ToArray());
            framesSide.Add(reqSide.GetData<byte>().ToArray());

            // 90프레임 초과 시 관리
            if (framesFront.Count > 90)
            {
                framesFront.RemoveAt(0);
                framesSide.RemoveAt(0);
            }
        }

        // 메모리 해제
        RenderTexture.ReleaseTemporary(rtF);
        RenderTexture.ReleaseTemporary(rtS);
    }

    IEnumerator CaptureAsync(Texture source, List<byte[]> targetList, bool isFront)
    {
        // 1. 해상도가 아직 0이라면 캡처 진행 안 함
        if (source.width <= 16 || source.height <= 16) yield break;

        int w = source.width;
        int h = source.height;

        // 2. FFmpeg와 ReplayInfo에서 쓸 전역 변수 업데이트
        if (isFront) {
            widthFront = w;
            heightFront = h;
        } else {
            widthSide = w;
            heightSide = h;
        }
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0);
        Graphics.Blit(source, rt);

        // 비동기로 읽기 요청 (여기서 CPU는 멈추지 않고 바로 다음 줄로 넘어감)
        AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24, (request) => {
            if (!request.hasError)
            {
                // 데이터가 도착하면 리스트에 추가
                byte[] data = request.GetData<byte>().ToArray();
                targetList.Add(data);

                // 90프레임 관리
                if (targetList.Count > 90)
                {
                    targetList.RemoveAt(0);
                }
            }
            RenderTexture.ReleaseTemporary(rt);
        });
        
        yield break;
    }


/*
    bool CaptureFrameFront()//RenderTexture renderTex)
    {
        Texture sourceTexFront = rawImageFront.texture;
        widthFront = sourceTexFront.width;
        heightFront = sourceTexFront.height;
        RenderTexture renderTexFront = RenderTexture.GetTemporary(widthFront, heightFront, 0);
        Graphics.Blit(sourceTexFront, renderTexFront);

        RenderTexture.active = renderTexFront;
        Texture2D tex = new Texture2D(widthFront, heightFront, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, widthFront, heightFront), 0, 0);
        tex.Apply();
        
        if (checkTakeback == false)
        {
            checkTakebackFrame++;

            if (framesFront.Count > 90)
            {
                checkTakebackFrame = 0;
                framesFront.Clear();
                framesSide.Clear();//사이드도 같이 삭제
            }

            if (_handCheck == false)
            {
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexFront);

                return false;
            }
        }
        
        framesFront.Add(tex);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexFront);

        return true;
    }

    void CaptureFrameSide()//RenderTexture renderTex)
    {
        Texture sourceTexSide = rawImageSide.texture;
        widthSide = sourceTexSide.width;
        heightSide = sourceTexSide.height;
        RenderTexture renderTexSide = RenderTexture.GetTemporary(widthSide, heightSide, 0);
        Graphics.Blit(sourceTexSide, renderTexSide);

        RenderTexture.active = renderTexSide;
        Texture2D tex = new Texture2D(widthSide, heightSide, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, widthSide, heightSide), 0, 0);
        tex.Apply();
                
        framesSide.Add(tex);

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(renderTexSide);
    }
    */
    
    IEnumerator CaptureFrames()
    {
        //float interval = 1f / fps;
        int retry = 0;
        //txtDebug.text += "프레임 캡쳐 시작" + "\r\n";
        checkTakebackFrame = 0;
        int frameCount = 0;


        while (isRecording)
        {
            yield return new WaitForEndOfFrame();
            //yield return null;

            
            /*if (CaptureFrameFront())
            {
                CaptureFrameSide();
            }*/
            StartCoroutine(CaptureAsync(rawImageFront.texture, framesFront, true));
            StartCoroutine(CaptureAsync(rawImageSide.texture, framesSide, false));

            //yield return new WaitForSeconds(interval);

            if (checkImpact == true)
            {
                txtDebug.text = "녹화 중+";
                imgSignal.color = Color.yellow;
                if (frameCount < 45)
                    frameCount++;
                else
                    isRecording = false;
            }
            else
            {
                //if (AvgVisible < 0.1f)
                if (webcamTrackerFront.visibilityAvg < 0.1f)
                {
                    if (retry > 5)
                    {
                        SetReady();
                        yield break;
                    }
                    else
                    {
                        retry++;
                        txtDebug.text = $"녹화 중 (감지불가-{retry})";
                    }
                }
                else
                {
                    txtDebug.text = $"녹화 중";
                    imgSignal.color = Color.red;
                }
            }

            if (_isReplay == false)
            {
                txtDebug.text = $"녹화 중지";
                SetReady();
                yield break;
            }
            
        }


        if (checkImpact == true && _isReplay)
        {
            imgSignal.color = Color.black;
            //앞 프레임 삭제
            framesFront.RemoveRange(0, Math.Max(0, checkTakebackFrame - 30));
            framesSide.RemoveRange(0, Math.Max(0, checkTakebackFrame - 30));

            txtDebug.text = $"녹화 종료({Math.Max(0, checkTakebackFrame - 30)})";
            recStep = RECODESTEP.RECORDEND;
            Debug.Log("프레임 캡쳐 종료");
            yield return null;
        }
        else
        {
            txtDebug.text = $"녹화 중지";
            SetReady();
        }
    }


    // 1. 매개변수 타입을 List<Texture2D>에서 List<byte[]>로 변경
    private IEnumerator SendFramesToFFmpeg(string output, int width, int height, bool isFront, List<byte[]> frames, Action ComplateEvent)
    {
        yield return null;
        yield return null;

        Process process = null;
        Stream stdin = null;

        if (!TryStartFFmpeg(output, width, height, out process, out stdin, out string errorMsg, isFront))
        {
            Debug.LogError("FFmpeg 실행 실패: " + errorMsg);
            yield break;
        }

        Debug.Log("FFmpeg 실행 성공");

        for (int i = 0; i < frames.Count; i++)
        {
            // 2. 이미 바이트 배열이므로 GetRawTextureData() 없이 바로 사용
            byte[] raw = frames[i]; 
            
            // 데이터가 null인지 체크 (안전장치)
            if (raw != null && raw.Length > 0)
            {
                stdin.Write(raw, 0, raw.Length);
            }

            // 20프레임마다 한 번씩 쉬어주어 메인 스레드 부하 분산
            //if (i % 20 == 0)
                yield return null;
        }

        stdin.Flush();
        stdin.Close();

        bool isExited = false;
        System.Threading.Tasks.Task.Run(() =>
        {
            if (process != null)
            {
                process.WaitForExit(5000);
                Debug.Log($"process.Close()");
                process.Close();
            }
            isExited = true; 
        });

        while (!isExited)
        {
            yield return null;
        }

        ComplateEvent?.Invoke();
        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator SendFramesToFFmpeg(string output, int width, int height, bool isFront, List<Texture2D> frames, Action ComplateEvent)
    {
        yield return null;
        yield return null;

        Process process = null;
        Stream stdin = null;

        if (!TryStartFFmpeg(output, width, height, out process, out stdin, out string errorMsg, isFront))
        {
            Debug.Log("FFmpeg 실행 실패: " + errorMsg);
            yield break;
        }

        Debug.Log("FFmpeg 실행 성공");

        for(int i = 0; i < frames.Count; i++)
        {
            byte[] raw = frames[i].GetRawTextureData();
            // FFmpeg가 기대하는 정확한 프레임 크기 계산 (RGB24 기준)
            int expectedSize = width * height * 3;

            if (raw.Length >= expectedSize)
            {
                // 실제 배열이 더 크더라도 expectedSize만큼만 정확히 씁니다.
                stdin.Write(raw, 0, expectedSize);
            }
            else
            {
                Debug.LogError($"프레임 데이터가 너무 작습니다! 예상: {expectedSize}, 실제: {raw.Length}");
                break;
            }

            //stdin.Write(raw, 0, raw.Length);

            if (i % 20 == 0)
                yield return null;
        }

        stdin.Flush();
        stdin.Close();

            bool isExited = false;
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            _ = process.WaitForExit(5000);  // 메인 스레드 블로킹 안 함
            //process.WaitForExit();  // 메인 스레드 블로킹 안 함
            Debug.Log($"process.Close()");
            process.Close();
            isExited = true;  // 종료되었음을 표시
        });

        while (!isExited)
        {
            yield return null;
        }

        // 종료 후 후처리
        ComplateEvent?.Invoke();

        yield return new WaitForSeconds(0.1f);
    }

    private bool TryStartFFmpeg(string output, int width, int height, out Process process, out Stream stdin, out string errorMsg, bool isFront = true)
    {
        stdin = null;
        process = new Process();
        errorMsg = "";
        string vfOption = isFront ? "\"transpose=1\"" : "\"transpose=0\"";

#if UNITY_STANDALONE_LINUX
        string ffmpegPath = Path.Combine(Application.dataPath, "Plugins", "ffmpeg", "bin", "ffmpeg");
#else
        string ffmpegPath = Path.Combine(Application.dataPath, "Plugins", "ffmpeg", "bin", "ffmpeg.exe");
#endif
        if(File.Exists(ffmpegPath) == false)
        {
            Debug.Log($"ffmpeg 찾을 수 없음 : {ffmpegPath}");
            return false;
        }

        try
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                
                //Arguments = $"-y -f rawvideo -pixel_format rgb24 -video_size {width}x{height} -framerate 30 -i - -vf {vfOption} -c:v libvpx -pix_fmt yuv420p -b:v 2M -qmin 10 -qmax 42 -speed 4 \"{output}\"",
                Arguments = $"-y -f rawvideo -pixel_format rgb24 -video_size {width}x{height} -framerate 30 -i - -vf {vfOption} -c:v libx264 -pix_fmt yuv420p -b:v 2M -crf 23 -preset medium \"{output}\"",

                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            //process.StartInfo.RedirectStandardError = true;
            //process.StartInfo.RedirectStandardOutput = true;

            process.OutputDataReceived += (s, e) => Debug.Log($"FFmpeg [Out]: {e.Data}");
            //process.ErrorDataReceived += (s, e) => Debug.LogError($"FFmpeg [Err]: {e.Data}");

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


    void OnProVideoEndFront(VLCVideoPlayer vp)
    {
        proFrontEnd = true;
        videoProFront.Pause();
        PlayProVideo();
    }

    void OnProVideoEndSide(VLCVideoPlayer vp)
    {
        proSideEnd = true;
        videoProSide.Pause();
        PlayProVideo();
    }
    

    void PlayProVideo()
    {
        //Debug.Log($"PlayProVideo() {proFrontEnd} / {proSideEnd}");
        if (proFrontEnd && proSideEnd)
        {
            proFrontEnd = false;
            proSideEnd = false;

            //videoProFront.time = 0;
            videoProFront.playbackSpeed = INI.PlaySpeedNormal;
            videoProFront.Play();

            //videoProSide.time = 0;
            videoProSide.playbackSpeed = INI.PlaySpeedNormal;
            videoProSide.Play();
        }
    }

    public void OnValueChanged_Debug(bool isOn)
    {
        DebugViewer.SetActive(isOn);
    }    
}
