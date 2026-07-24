using Enums;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StudioDirector : MonoBehaviour
{
    private CardPageControl<VideoCardController, ProVideoData> cardPage;

    private FilterHandler<EStance> m_PoseFilterHandler;
    private FilterHandler<EClub> m_ClubFilterHandler;

    [SerializeField] private PopupControl m_PopupControl;

    [SerializeField] private GameObject m_Profile;
    [SerializeField] private GameObject m_Lesson;
    [SerializeField] private GameObject m_FilmingPanel;
    [SerializeField] private GameObject m_VideoGroup;
    [SerializeField] private GameObject m_NoVideoObj;

    [SerializeField] private ToggleGroup m_TabTypeTG;

    [SerializeField] private Toggle[] m_TabTypeToggles;

    [Header(" Profile ")]
    [SerializeField] VLCVideoPlayer m_VideoProFront;
    [SerializeField] VLCVideoPlayer m_VideoProSide;
    [SerializeField] VideoPlayerControlMirror m_ProVideoPlayerControl;

    [Header("[ Lesson ]")]
    [SerializeField] private GameObject[] m_PopupObjs;
    [SerializeField] private GameObject m_videoCardPrefab;
    [SerializeField] private GameObject m_StudioPanel;
    [SerializeField] private GameObject m_EditorPanel;
    [SerializeField] private GameObject m_CardListGroup;

    [SerializeField] private RectTransform m_PrevGroup;
    [SerializeField] private RectTransform m_CurGroup;
    [SerializeField] private RectTransform m_NextGroup;
    [SerializeField] private RectTransform m_HideGroup;

    [SerializeField] private Button NextPageButton;
    [SerializeField] private Button PrevPageButton;

    [SerializeField] private TextMeshProUGUI m_PageText;
    [SerializeField] private TextMeshProUGUI m_TotalVideoText;

    private List<ProVideoData> m_AllVideo = new List<ProVideoData>();
    private List<ProVideoData> m_FilteredVideo = new List<ProVideoData>();

    private List<EStance> m_SelectPoseList = new List<EStance>();
    private List<EClub> m_SelectClubList = new List<EClub>();

    SelectProData m_SelectProData = new SelectProData();

    [SerializeField] private int cardsPerPage = 6;
    private int totalVideoCount = 0;
    private int selectProUID = 0;

    private bool isCurProVideo = true;

    [Header("[ Mode Select ]")]
    [SerializeField] private GameObject m_SwingToggleObj;
    [SerializeField] private GameObject m_StepToggleObj;
    [SerializeField] private GameObject m_FilmingObj;
    [SerializeField] private GameObject m_ClubObj;
    [SerializeField] private GameObject[] m_ContentsObj;

    [SerializeField] private ToggleGroup m_FilmingModeTG;
    [SerializeField] private ToggleGroup m_PracticeSwingTG;
    [SerializeField] private ToggleGroup m_AllStepTG;
    [SerializeField] private ToggleGroup m_ClubTG;

    [SerializeField] private Toggle[] m_FilmingModeToggles;
    [SerializeField] private Toggle[] m_PracticeSwingToggles;
    [SerializeField] private Toggle[] m_StepToggles;
    [SerializeField] private Toggle[] m_ClubToggles;
    [SerializeField] private Toggle m_AllStepToggle;

    [SerializeField] private TextMeshProUGUI m_NextBtnText;
    [SerializeField] private TextMeshProUGUI m_TitleText;

    [Header("[ Filter ]")]
    [SerializeField] private Toggle m_PoseAllFilter;
    [SerializeField] private Toggle[] m_PoseFilters;
    [SerializeField] private Toggle m_ClubAllFilter;
    [SerializeField] private Toggle[] m_ClubFilters;

    [SerializeField] private ToggleGroup m_PoseTg;
    [SerializeField] private ToggleGroup m_ClubTg;

    [SerializeField] private FilterItemController m_PoseStateTxt;
    [SerializeField] private FilterItemController m_ClubStateTxt;

    [Header("[ Editor ]")]
    [SerializeField] GameObject m_SubPanelInfo;
    [SerializeField] GameObject m_SubPanelStatic;
    [SerializeField] GameObject m_SubPanelKeyword;

    [SerializeField] RawImage rawImgSelectEditThumb;
    [SerializeField] TextMeshProUGUI txtSelectTitle;
    [SerializeField] ToggleGroup m_SubPanelToggleGroup;
    [SerializeField] Toggle[] m_SubPanelToggles;
    [SerializeField] private Toggle[] m_ViewToggles;
    [SerializeField] VLCVideoPlayer previewPlayer;
    [SerializeField] private GameObject previewPlayBtn;
    [SerializeField] private float thumbDelay = 0.08f;

    private bool previewPreparing = false;
    private bool previewPlaying = false;
    private bool wasPreviewPlaying = false;
    private Coroutine previewCo = null;
    private Coroutine pauseCo = null;

    [SerializeField] private Sprite[] m_ViewSprite;

    [SerializeField] private EArraySortMode currentSortMode = EArraySortMode.View;

    private ESwingType swingType = ESwingType.None;
    private List<SWINGSTEP> step = new List<SWINGSTEP>();
    private EClub club = EClub.None;
    private ERecordingType recordingType = ERecordingType.None;

    int tabNum = 0;

    readonly string profileStr = "스윙 촬영을 진행하시겠습니까?";
    readonly string lessonStr = "레슨 촬영을 진행하시겠습니까?";
    readonly string duplicateProfileStr = "이미 같은 클럽/스윙 영상이 있습니다.\r\n다시 촬영하시겠습니까?";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Init();
    }

    private void Update()
    {
        CheckPreviewEnd();
    }

    private void Init()
    {
        recordingType = ERecordingType.Profile;

        m_PoseFilterHandler = new FilterHandler<EStance>(m_PoseFilters, m_PoseAllFilter, m_PoseTg, m_SelectPoseList, UpdateFilteredVideo);

        m_ClubFilterHandler = new FilterHandler<EClub>(m_ClubFilters, m_ClubAllFilter, m_ClubTg, m_SelectClubList, UpdateFilteredVideo);

        m_SelectProData = GolfProDataManager.Instance.SelectProData;

        SetToggles();


        StartCoroutine(WaitForVideoData());

        //SetProfile();
    }

    private void SetToggles()
    {
        for (int i = 0; i < m_PoseFilters.Length; i++)
        {
            m_PoseFilters[i].onValueChanged.AddListener(m_PoseFilterHandler.OnValueChangedFilter);
        }

        m_PoseAllFilter.onValueChanged.AddListener(m_PoseFilterHandler.OnValueChangedAll);

        for (int i = 0; i < m_ClubFilters.Length; i++)
        {
            m_ClubFilters[i].onValueChanged.AddListener(m_ClubFilterHandler.OnValueChangedFilter);
        }

        m_ClubAllFilter.onValueChanged.AddListener(m_ClubFilterHandler.OnValueChangedAll);

        foreach (var toggle in m_TabTypeToggles)
        {
            toggle.onValueChanged.AddListener(OnValueChanged_TabType);
        }

        foreach (var toggle in m_SubPanelToggles)
        {
            toggle.onValueChanged.AddListener(OnValueChanged_SubPanel);
        }

        foreach (var toggle in m_FilmingModeToggles)
        {
            toggle.onValueChanged.AddListener(OnValueChanged_FilmingMode);
        }

        foreach (var toggle in m_PracticeSwingToggles)
        {
            toggle.onValueChanged.AddListener(OnValueChanged_PracticeSwing);
        }

        foreach (var toggle in m_StepToggles)
        {
            toggle.onValueChanged.AddListener(OnValueChanged_Step);
        }

        m_AllStepToggle.onValueChanged.AddListener(OnValueChanged_StepAll);

        foreach (var toggle in m_ClubToggles)
        {
            toggle.onValueChanged.AddListener(OnValueChanged_Club);
        }
    }

    private IEnumerator WaitForVideoData()
    {
        yield return new WaitUntil(() => GolfProDataManager.Instance != null && GolfProDataManager.Instance.GetProVideoDic() != null);

        LoadVideoUI();
    }

    public void Onclick_Button(string name)
    {
        //GameObject obj = EventSystem.current.currentSelectedGameObject;

        switch (name)
        {
            case "Home":
                GameManager.Instance.Stance.Clear();
                GameManager.Instance.SwingType = ESwingType.None;
                GameManager.Instance.Club = EClub.None;

                GameManager.Instance.SelectedSceneName = string.Empty;
                SceneManager.LoadScene("ModeSelect");
                break;

            case "Back":
                StopEditPreview();
                m_StudioPanel.SetActive(true);
                m_EditorPanel.SetActive(false);
                break;

            case "Filming_ModeSelect":
                m_FilmingPanel.SetActive(true);

                // if (recordingType == ERecordingType.Profile)
                // {
                //     m_FilmingPanel.SetActive(true);
                //     //m_PopupControl.ShowPopup(profileStr, Utillity.Instance.HexToRGB(INI.Red), LoadRecordinig);
                //     //SceneManager.LoadScene("Recording");
                // }
                // else if (recordingType == ERecordingType.Lesson)
                // {
                //     m_PopupControl.ShowPopup(lessonStr, Utillity.Instance.HexToRGB(INI.Red), LoadRecordinig);

                // }
                // else if (recordingType == ERecordingType.Practice)
                // {
                //     m_FilmingPanel.SetActive(true);
                // }
                break;

            case "Cancle_ModeSelect":
                if (m_ClubObj.activeInHierarchy)
                {
                    m_PracticeSwingToggles[0].isOn = true;

                    m_FilmingObj.SetActive(true);
                    m_ClubObj.SetActive(false);

                    m_FilmingModeToggles[0].isOn = true;

                    //m_FilmingModeToggles[0].onValueChanged.Invoke(true);

                    m_TitleText.text = "어떤 영상을 촬영하시겠어요?";
                    m_NextBtnText.text = "다음";

                    club = EClub.None;
                }
                else
                {
                    m_FilmingPanel.SetActive(false);
                }
                break;

            case "Next":
                if (m_FilmingObj.activeInHierarchy)
                {
                    if (recordingType == ERecordingType.Profile || recordingType == ERecordingType.Practice)
                    {
                        m_FilmingObj.SetActive(false);
                        m_ClubObj.SetActive(true);

                        m_PracticeSwingToggles[0].onValueChanged.Invoke(true);
                        m_ClubToggles[1].onValueChanged.Invoke(true);
                    }
                    else if (recordingType == ERecordingType.Lesson)
                    {
                        m_PopupControl.ShowPopup(lessonStr, Utillity.Instance.HexToRGB(INI.Red), LoadRecordinig);
                    }
                    else if (recordingType == ERecordingType.Stretching)
                    {
                        LoadStretchingRecording();
                    }

                    // if (step.Count > 0 && swingType != ESwingType.None)
                    // {
                    //     m_FilmingObj.SetActive(false);
                    //     m_ClubObj.SetActive(true);
                    //     m_TitleText.text = "자세와 클럽을 선택하세요";
                    //     m_NextBtnText.text = "다음";

                    //     if (swingType == ESwingType.Half || swingType == ESwingType.ThreeQuarter)
                    //     {
                    //         m_HalfAndThree_Clubs.SetActive(true);
                    //         m_Full_Clubs.SetActive(false);

                    //         m_HalfAndThreeToggles[0].isOn = true;
                    //         m_HalfAndThreeToggles[0].onValueChanged.Invoke(true);
                    //     }
                    //     else if (swingType == ESwingType.Full)
                    //     {
                    //         m_HalfAndThree_Clubs.SetActive(false);
                    //         m_Full_Clubs.SetActive(true);

                    //         m_FullToggles[0].isOn = true;
                    //         m_FullToggles[0].onValueChanged.Invoke(true);
                    //     }
                    // }
                    // else
                    // {
                    //     if (recordingType == ERecordingType.Lesson)// || recordingType == ERecordingType.Profile)
                    //     {
                    //         GameManager.Instance.RecordingType = recordingType;

                    //         SceneManager.LoadScene("Recording");
                    //     }
                    // }
                }
                else
                {
                    if (club != EClub.None && step.Count > 0 && swingType != ESwingType.None)
                    {
                        if (recordingType == ERecordingType.Profile && HasExistingProfileVideo())
                        {
                            m_PopupControl.ShowPopup(duplicateProfileStr, Utillity.Instance.HexToRGB(INI.Red), LoadRecordingWithSelectedOptions);
                        }
                        else
                        {
                            LoadRecordingWithSelectedOptions();
                        }
                    }
                }
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;
        }
    }

    public void LoadStretchingRecording()
    {
        GameManager.Instance.RecordingType = ERecordingType.Stretching;
        GameManager.Instance.SelectedSceneName = "Recording_park";
        SceneManager.LoadScene("Recording_park");
    }

    public void LoadRecordinig()
    {
        GameManager.Instance.RecordingType = recordingType;

        SceneManager.LoadScene("Recording");
    }


    private void LoadRecordingWithSelectedOptions()
    {
        GameManager.Instance.Club = club;
        GameManager.Instance.Stance = step;
        GameManager.Instance.SwingType = swingType;
        GameManager.Instance.RecordingType = recordingType;

        SceneManager.LoadScene("Recording");
    }

    private bool HasExistingProfileVideo()
    {
        if (GolfProDataManager.Instance == null || GolfProDataManager.Instance.SelectProData == null)
            return false;

        int uid = GolfProDataManager.Instance.SelectProData.uid;
        string homeDir = System.Environment.GetEnvironmentVariable("HOME");
        string proVideoDir = Path.Combine(homeDir, "DataBase_park", "ProVideo", uid.ToString());
        string frontPath = Path.Combine(proVideoDir, $"front_video_{(int)swingType}{(int)club}.mp4");
        string sidePath = Path.Combine(proVideoDir, $"side_video_{(int)swingType}{(int)club}.mp4");

        return File.Exists(frontPath) || File.Exists(sidePath);
    }

    private void LoadVideoUI()
    {
        if (tabNum == 0)
            m_AllVideo = m_SelectProData.videoData.Where(v => v.videoType == EVideoType.Swing).ToList();
        else
            m_AllVideo = m_SelectProData.videoData.Where(v => v.videoType == EVideoType.Lesson).ToList();

        GameManager.Instance.StudioTabNum = tabNum;

        if (m_AllVideo == null || m_AllVideo.Count <= 0)
        {
            isCurProVideo = false;

            m_VideoGroup.SetActive(false);
            m_NoVideoObj.SetActive(true);
        }
        else
        {
            isCurProVideo = true;

            m_VideoGroup.SetActive(true);
            m_NoVideoObj.SetActive(false);
        }

        if (!isCurProVideo)
        {
            selectProUID = 0;

            // if (tabNum == 0)
            //     m_AllVideo = Utillity.Instance.LoadVideoUrlList(0, EVideoType.Swing, EPoseDirection.All);
            // else
            //     m_AllVideo = Utillity.Instance.LoadVideoUrlList(0, EVideoType.Lesson, EPoseDirection.All);
        }
        else
        {
            selectProUID = m_SelectProData.uid;

            InitCardPage();

            UpdateFilteredVideo();

            cardPage.SetData(m_FilteredVideo);
        }

        // InitCardPage();

        // UpdateFilteredVideo();

        // cardPage.SetData(m_FilteredVideo);
    }

    private void InitCardPage()
    {
        cardPage = new CardPageControl<VideoCardController, ProVideoData>();
        cardPage.Initialize(m_videoCardPrefab, m_PrevGroup, m_CurGroup, m_NextGroup, m_HideGroup, cardsPerPage, (card, data, index, onClick) =>
        {
            card.SetVideoCard(data, index, onClick);
            return card;
        },
        SetPages);

        cardPage.SetCardClickAction(ApplyVideoData);
    }
    private void SetPages()
    {
        m_PageText.text = $"{cardPage.CurrentPage + 1} / {cardPage.TotalPages}";

        m_TotalVideoText.text = $"영상 {totalVideoCount}개";

        PrevPageButton.interactable = cardPage.CurrentPage > 0;
        NextPageButton.interactable = cardPage.CurrentPage + 1 < cardPage.TotalPages;
    }

    public void ApplyVideoData(ProVideoData data)
    {
        //Debug.Log($"[ApplyVideoData] 영상 눌림 {data.uid}, {data.name}");

        m_StudioPanel.SetActive(false);
        m_EditorPanel.SetActive(true);

        string clubText = Utillity.Instance.ConvertEnumToString(data.clubFilter);
        string poseText = data.videoType == EVideoType.Swing
            ? ConvertSwingTypeToString(data.swingType)
            : Utillity.Instance.ConvertEnumToString(data.poseFilter);

        txtSelectTitle.text = string.IsNullOrEmpty(poseText) ? clubText : $"{clubText} • {poseText}";
        StopPreviewCo();
        previewPlayer.Stop();
        previewPlayer.targetDisplay = rawImgSelectEditThumb;
        previewPlayer.playOnAwake = false;
        previewPlayer.isLooping = false;
        previewPlayer.started -= OnVideoStarted;
        previewPlayer.started += OnVideoStarted;
        previewPlayer.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}");
        previewPreparing = true;
        previewPlaying = false;
        wasPreviewPlaying = false;
        SetPlayBtn(true);
        previewCo = StartCoroutine(CoPreparePreview());
        //imgSelectEditThumb.texture = ;
    }


    private void UpdateFilteredVideo()
    {
        if ((m_SelectPoseList == null || m_SelectPoseList.Count == 0) && ((m_SelectClubList == null || m_SelectClubList.Count == 0)))
        {
            m_FilteredVideo = m_AllVideo
                .OrderByDescending(v => GetSortValue(v))
                .ThenBy(v => v.uid)
                .ToList();

            //m_FilteredVideo = new List<ProVideoData>(m_AllVideo);
        }
        else
        {
            m_FilteredVideo = m_AllVideo
                .Where(v => m_SelectPoseList.Contains(v.poseFilter) || m_SelectClubList.Contains(v.clubFilter))
                .OrderByDescending(v => GetSortValue(v))
                .ThenBy(v => v.uid)
                .ToList();
        }

        totalVideoCount = m_FilteredVideo.Count;
    }

    public void OnClick_NextPage() => cardPage.NextPage();

    public void OnClick_PrevPage() => cardPage.PrevPage();

    private int GetSortValue(ProVideoData data)
    {
        switch (currentSortMode)
        {
            case EArraySortMode.View:
                return data.views;
            case EArraySortMode.Favorite:
                return data.favoriteCount;
            default:
                return data.views;
        }
    }

    private static string ConvertSwingTypeToString(ESwingType swingType)
    {
        switch (swingType)
        {
            case ESwingType.Full: return "풀 스윙";
            case ESwingType.ThreeQuarter: return "쓰리쿼터 스윙";
            case ESwingType.Half: return "하프 스윙";
            default: return string.Empty;
        }
    }

    private void SetStepValue(bool active, params EStance[] steps)
    {
        if (steps == null || steps.Length == 0) return;

        foreach (EStance step in steps)
        {
            if (step == EStance.Full)
            {
                foreach (Toggle toggle in m_StepToggles)
                {
                    toggle.GetComponent<ToggleUIAddon>().SetVisualState(active);
                    toggle.interactable = active;
                }
            }
            else
            {
                int i = (int)step;

                if (i == 1)
                {
                    m_StepToggles[0].GetComponent<ToggleUIAddon>().SetVisualState(active);
                    m_StepToggles[0].interactable = active;
                }
                else
                {
                    m_StepToggles[i - 4].GetComponent<ToggleUIAddon>().SetVisualState(active);
                    m_StepToggles[i - 4].interactable = active;
                }
            }
        }
    }

    IEnumerator SetStep(ESwingType pose)
    {
        yield return null;
        step.Clear();

        switch (pose)
        {
            case ESwingType.Half:
                SetStepValue(true, EStance.Address, EStance.Takeback, EStance.Impact, EStance.Follow);
                break;

            case ESwingType.ThreeQuarter:
                SetStepValue(true, EStance.Address, EStance.Takeback, EStance.Downswing, EStance.Impact, EStance.Follow);
                break;

            case ESwingType.Full:
                SetStepValue(true, EStance.Full);
                break;
        }

        SetAllStep(pose);
    }

    void SetAllStep(ESwingType pose)
    {
        step.Clear();
        switch (pose)
        {
            case ESwingType.Half:
                step.Add(SWINGSTEP.ADDRESS);
                step.Add(SWINGSTEP.TAKEBACK);
                step.Add(SWINGSTEP.IMPACT);
                step.Add(SWINGSTEP.FOLLOW);
                break;

            case ESwingType.ThreeQuarter:
                step.Add(SWINGSTEP.ADDRESS);
                step.Add(SWINGSTEP.TAKEBACK);
                step.Add(SWINGSTEP.BACKSWING);
                step.Add(SWINGSTEP.DOWNSWING);
                step.Add(SWINGSTEP.IMPACT);
                step.Add(SWINGSTEP.FOLLOW);
                break;

            case ESwingType.Full:
                step.Add(SWINGSTEP.ADDRESS);
                step.Add(SWINGSTEP.TAKEBACK);
                step.Add(SWINGSTEP.BACKSWING);
                step.Add(SWINGSTEP.TOP);
                step.Add(SWINGSTEP.DOWNSWING);
                step.Add(SWINGSTEP.IMPACT);
                step.Add(SWINGSTEP.FOLLOW);
                step.Add(SWINGSTEP.FINISH);
                break;
        }
    }

    private void ResetStepUI()
    {
        if (step.Count > 0)
        {
            foreach (var toggle in m_StepToggles)
            {
                toggle.isOn = false;
            }

            step.Clear();
        }

        swingType = ESwingType.None;

        m_AllStepTG.allowSwitchOff = false;
        m_AllStepToggle.isOn = true;
    }

    private void SetProfile()
    {
        int uid = GolfProDataManager.Instance.SelectProData.uid;
        string proPath = string.Empty;
        /*string proPath = GolfProDataManager.Instance.SelectProData.videoData.
            Where(v => v.direction == EPoseDirection.Front && v.sceneType == ESceneType.ProSelect).
            Select(v => v.path).FirstOrDefault();
        
        if (string.IsNullOrEmpty(m_VideoProFront.url))
            m_VideoProFront.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{proPath}");
        */

        proPath = $"{INI.proVideoPath}{uid}/front_video_03.mp4";
        if (File.Exists(Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), proPath)))
        {
            m_VideoProFront.url = GameManager.Instance.LoadVideoURL(proPath);

            proPath = $"{INI.proVideoPath}{uid}/side_video_03.mp4";
            if (File.Exists(Path.Combine(System.Environment.GetEnvironmentVariable("HOME"), proPath)))
                m_VideoProSide.url = GameManager.Instance.LoadVideoURL(proPath);
        }
        else
        {
            m_VideoProFront.url = null;
            m_VideoProSide.url = null;
        }
        /*proPath = GolfProDataManager.Instance.SelectProData.videoData.
            Where(v => v.direction == EPoseDirection.Side && v.sceneType == ESceneType.ProSelect).
        Select(v => v.path).FirstOrDefault();
        if (string.IsNullOrEmpty(m_VideoProSide.url))
            m_VideoProSide.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{proPath}");
        */
        m_ProVideoPlayerControl.PlayVideo();

        recordingType = ERecordingType.Profile;
    }

    public void OnClick_Popup(int pop)
    {
        m_PopupObjs[pop].SetActive(!m_PopupObjs[pop].activeInHierarchy);
    }

    public void OnClick_ResetPose()
    {
        m_PoseFilterHandler.Reset();
        UpdateFilteredVideo();
    }

    public void OnClick_ApplyFilter()
    {
        cardPage.SetData(m_FilteredVideo);

        m_PoseStateTxt.UpdateStringAndSize(m_PoseFilterHandler.SelectedCount != 0 ? $"자세({m_PoseFilterHandler.SelectedCount})" : $"자세");

        m_ClubStateTxt.UpdateStringAndSize(m_ClubFilterHandler.SelectedCount != 0 ? $"클럽({m_ClubFilterHandler.SelectedCount})" : $"클럽");

        foreach (var p in m_PopupObjs)
        {
            p.SetActive(false);
        }
    }

    public void OnClick_ResetClub()
    {
        m_ClubFilterHandler.Reset();
        UpdateFilteredVideo();
    }

    public void OnValueChanged_TabType(bool isOn)
    {
        if (isOn)
        {
            tabNum = m_TabTypeTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

            // m_Profile.SetActive(num == 0 ? true : false);
            // m_Lesson.SetActive(num == 1 ? true : false);

            LoadVideoUI();

            // switch (num)
            // {
            //     case 0:
            //         SetProfile();
            //         break;

            //     case 1:
            //         recordingType = ERecordingType.Lesson;
            //         break;
            //     case 2:
            //         recordingType = ERecordingType.Practice;
            //         m_ProVideoPlayerControl.StopVideo();
            //         break;
            // }
        }
    }

    public void OnValueChanged_FilmingMode(bool isOn)
    {
        if (m_FilmingModeTG.GetFirstActiveToggle() == null) return;

        if (isOn)
        {
            int num = m_FilmingModeTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

            //Debug.Log($"num = {num}");
            if (num == 0)
            {
                recordingType = ERecordingType.Profile;

                //m_PracticeSwingToggles[0].isOn = true;
                //m_PracticeSwingToggles[0].onValueChanged.Invoke(true);
                //ResetStepUI();
            }
            else if (num == 1)
            {
                recordingType = ERecordingType.Lesson;

                //m_PracticeSwingToggles[0].isOn = true;
                //ResetStepUI();
            }
            else if (num == 2)
            {
                recordingType = ERecordingType.Practice;

                //m_PracticeSwingToggles[0].onValueChanged.Invoke(true);
            }
            else if (num == 3)
            {
                recordingType = ERecordingType.Stretching;
            }

            for (int i = 0; i < m_ContentsObj.Length; i++)
            {
                if (num == i)
                    m_ContentsObj[i].SetActive(true);
                else
                    m_ContentsObj[i].SetActive(false);
            }
            //m_SwingToggleObj.SetActive((num == 0 || num == 2) ? true : false);
            //m_StepToggleObj.SetActive(num == 2 ? true : false);
        }
    }

    public void OnValueChanged_PracticeSwing(bool isOn)
    {
        if (m_PracticeSwingTG.GetFirstActiveToggle() == null) return;

        if (isOn)
        {
            int num = m_PracticeSwingTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

            ESwingType t = (ESwingType)num;

            ResetStepUI();

            if (swingType == ESwingType.None || swingType != t)
            {
                m_ClubToggles[1].isOn = true;
                //m_ClubToggles[1].onValueChanged.Invoke(true);

                step.Clear();

                swingType = t;

                StartCoroutine(SetStep(swingType));
            }
            else if (swingType == t)
            {
                return;
            }
        }
    }

    public void OnValueChanged_Step(bool isOn)
    {
        int value = -1;
        SWINGSTEP s = SWINGSTEP.CHECK;

        if (m_AllStepToggle.isOn)
        {
            step.Clear();
        }

        for (int i = 0; i < m_StepToggles.Length; i++)
        {
            value = m_StepToggles[i].gameObject.GetComponent<UIValueObject>().intValue;
            s = (SWINGSTEP)value;

            if (m_StepToggles[i].isOn)
            {
                if (step.Contains(s) == false)
                {
                    step.Add(s);
                    step.Sort();
                }
            }
            else
            {
                if (step.Contains(s) == true)
                {
                    step.Remove(s);
                }
            }
        }

        if (step.Count <= 0)
        {
            m_AllStepTG.allowSwitchOff = false;
            m_AllStepToggle.isOn = true;
        }
        else
        {
            m_AllStepTG.allowSwitchOff = true;
            m_AllStepToggle.isOn = false;
        }
    }

    public void OnValueChanged_StepAll(bool isOn)
    {
        if (isOn)
        {
            foreach (var toggle in m_StepToggles)
            {
                toggle.isOn = false;
            }

            SetAllStep(swingType);
        }
    }

    public void OnValueChanged_SubPanel(bool isOn)
    {
        if (isOn)
        {
            int tabIdx = m_SubPanelToggleGroup.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;
            m_SubPanelInfo.SetActive(tabIdx == 0 ? true : false);
            m_SubPanelStatic.SetActive(tabIdx == 1 ? true : false);
            m_SubPanelKeyword.SetActive(tabIdx == 2 ? true : false);
        }
    }

    public void OnValueChanged_Club(bool isOn)
    {
        if (isOn)
        {
            int num = -1;

            num = m_ClubTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

            // if (m_HalfAndThree_Clubs.activeInHierarchy)
            // {
            //     num = m_HalfAndThreeTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;
            // }
            // else if (m_Full_Clubs.activeInHierarchy)
            // {
            //     num = m_FullTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;
            // }

            EClub c = (EClub)num;

            if (c != EClub.None)
            {
                club = c;
            }
        }
    }


    private void CheckPreviewEnd()
    {
        if (!previewPlaying || previewPreparing)
            return;

        if (previewPlayer == null)
            return;

        bool isPlayingNow = previewPlayer.isPlaying;
        bool playbackStoppedAfterPlaying = wasPreviewPlaying && !isPlayingNow;
        bool positionReachedEnd = previewPlayer.position >= 0.985f;
        bool timeReachedEnd = previewPlayer.length > 0 && previewPlayer.time >= previewPlayer.length - 250;

        wasPreviewPlaying = isPlayingNow;

        if (playbackStoppedAfterPlaying || positionReachedEnd || timeReachedEnd)
            ResetPreview();
    }

    private void ResetPreview()
    {
        previewPreparing = true;
        previewPlaying = false;
        wasPreviewPlaying = false;
        SetPlayBtn(true);

        previewPlayer.Stop();
        previewCo = StartCoroutine(CoPreparePreview());
    }

    private void OnVideoStarted(VLCVideoPlayer vp)
    {
        if (!previewPreparing)
            return;

        if (pauseCo != null)
            StopCoroutine(pauseCo);

        pauseCo = StartCoroutine(CoPausePreview(vp));
    }

    public void OnClick_EditPreview()
    {
        if (previewPlayer == null)
            return;

        if (previewPlayer.isPlaying)
        {
            previewPlaying = false;
            wasPreviewPlaying = false;
            previewPlayer.Pause();
            SetPlayBtn(true);
        }
        else
        {
            StopPreviewCo();
            previewPreparing = false;

            if (previewPlayer.position >= 0.985f)
                previewPlayer.position = 0f;

            previewPlaying = true;
            wasPreviewPlaying = false;
            previewPlayer.Play();
            SetPlayBtn(false);
        }
    }

    private void StopEditPreview()
    {
        if (previewPlayer == null)
            return;

        StopPreviewCo();
        previewPreparing = false;
        previewPlaying = false;
        wasPreviewPlaying = false;
        previewPlayer.Stop();
        SetPlayBtn(true);
    }

    private IEnumerator CoPreparePreview()
    {
        yield return null;
        previewCo = null;

        if (!previewPreparing || previewPlayer == null || !m_EditorPanel.activeInHierarchy)
            yield break;

        previewPlayer.Play();
    }

    private IEnumerator CoPausePreview(VLCVideoPlayer player)
    {
        if (thumbDelay > 0f)
            yield return new WaitForSecondsRealtime(thumbDelay);
        else
            yield return null;

        pauseCo = null;

        if (!previewPreparing || player == null || !m_EditorPanel.activeInHierarchy)
            yield break;

        previewPreparing = false;
        previewPlaying = false;
        wasPreviewPlaying = false;
        player.Pause();
        SetPlayBtn(true);
    }

    private void SetPlayBtn(bool active)
    {
        if (previewPlayBtn != null)
            previewPlayBtn.SetActive(active);
    }

    private void StopPreviewCo()
    {
        if (previewCo != null)
        {
            StopCoroutine(previewCo);
            previewCo = null;
        }

        if (pauseCo != null)
        {
            StopCoroutine(pauseCo);
            pauseCo = null;
        }
    }

    private void OnDestroy()
    {
        if (previewPlayer != null)
            previewPlayer.started -= OnVideoStarted;
    }
}
