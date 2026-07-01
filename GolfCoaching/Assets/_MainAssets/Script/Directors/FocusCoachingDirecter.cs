using DG.Tweening;
using Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FocusCoachingDirecter : MonoBehaviour
{
    [SerializeField] VideoPlayerControl videoPlayerControl;
    [SerializeField] VideoFInishPanel m_VideoFinishPanel;

    [SerializeField] private GameObject _SearchPopup;
    private CardPageControl<VideoCardController, ProVideoData> cardPage;

    private FilterHandler<EStance> m_PoseFilterHandler;
    private FilterHandler<EClub> m_ClubFilterHandler;

    private ELessonState m_LessonState = ELessonState.None;

    public ELessonState LessonState
    {
        get
        {
            return m_LessonState;
        }
        set
        {
            m_LessonState = value;
        }
    }

    public GameObject m_BottomBack;

    [Header("[ Video Card ]")]
    [SerializeField] private GameObject[] m_PopupObjs;
    [SerializeField] private GameObject m_videoCardPrefab;
    [SerializeField] private GameObject m_PickPanel;
    [SerializeField] private GameObject m_PlayPanel;
    [SerializeField] private GameObject m_Overlay_Canvas;
    [SerializeField] private GameObject m_CardListGroup;
    [SerializeField] private GameObject m_FilterGroup;
    [SerializeField] private GameObject m_ViewHistoryList;

    [SerializeField] private RectTransform m_PrevGroup;
    [SerializeField] private RectTransform m_CurGroup;
    [SerializeField] private RectTransform m_NextGroup;
    [SerializeField] private RectTransform m_HideGroup;

    [SerializeField] private Button NextPageButton;
    [SerializeField] private Button PrevPageButton;

    [SerializeField] private Toggle m_ViewHistoryToggle;

    [SerializeField] private TextMeshProUGUI m_TitleText;
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

    [Header("[ Filter ]")]
    [SerializeField] private GameObject m_ArrayPopup;

    [SerializeField] private Toggle m_PoseAllFilter;
    [SerializeField] private Toggle[] m_PoseFilters;
    [SerializeField] private Toggle m_ClubAllFilter;
    [SerializeField] private Toggle[] m_ClubFilters;
    [SerializeField] private Toggle[] m_ArrayFilters;

    [SerializeField] private ToggleGroup m_PoseTg;
    [SerializeField] private ToggleGroup m_ClubTg;
    [SerializeField] private ToggleGroup m_ArrayTg;

    [SerializeField] private FilterItemController m_PoseStateTxt;
    [SerializeField] private FilterItemController m_ClubStateTxt;

    [SerializeField] private TextMeshProUGUI m_ArrayFilterText;

    private EArraySortMode currentSortMode = EArraySortMode.View;

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        m_LessonState = ELessonState.List;

        m_PoseFilterHandler = new FilterHandler<EStance>(m_PoseFilters, m_PoseAllFilter, m_PoseTg, m_SelectPoseList, UpdateFilteredVideo);

        m_ClubFilterHandler = new FilterHandler<EClub>(m_ClubFilters, m_ClubAllFilter, m_ClubTg, m_SelectClubList, UpdateFilteredVideo);

        for (int i = 0; i < m_PoseFilters.Length; i++)
        {
            m_PoseFilters[i].onValueChanged.AddListener(m_PoseFilterHandler.OnValueChangedFilter);
        }

        m_PoseAllFilter.onValueChanged.AddListener(m_PoseFilterHandler.OnValueChangedAll);

        for (int i = 0; i < m_ClubFilters.Length; i++)
        {
            m_ClubFilters[i].onValueChanged.AddListener(m_ClubFilterHandler.OnValueChangedFilter);
        }

        for (int i = 0; i < m_ArrayFilters.Length; i++)
        {
            m_ArrayFilters[i].onValueChanged.AddListener(OnValueChanged_ArrayFilter);
        }

        m_ClubAllFilter.onValueChanged.AddListener(m_ClubFilterHandler.OnValueChangedAll);
        m_ViewHistoryToggle.onValueChanged.AddListener(OnValueChanged_ViewHistory);

        m_SelectProData = GolfProDataManager.Instance.SelectProData;

        currentSortMode = EArraySortMode.Recently;

        m_ArrayFilterText.text = Utillity.Instance.ConvertEnumToString(currentSortMode);

        StartCoroutine(WaitForVideoData());
    }

    #region VideoCard Method
    private IEnumerator WaitForVideoData()
    {
        yield return new WaitUntil(() => GolfProDataManager.Instance != null && GolfProDataManager.Instance.GetProVideoDic() != null);

        LoadVideoUI();
    }

    private void LoadVideoUI()
    {
        m_AllVideo = m_SelectProData.videoData.Where(v => v.videoType == EVideoType.Lesson).ToList();

        if (m_AllVideo == null || m_AllVideo.Count <= 0)
            isCurProVideo = false;
        else
            isCurProVideo = true;

        if (!isCurProVideo)
        {
            selectProUID = 0;

            //m_AllVideo = Utillity.Instance.LoadVideoUrlList(0, ESceneType.LessonMode, EPoseDirection.All);
        }
        else
        {
            selectProUID = m_SelectProData.uid;
        }

        InitCardPage();

        UpdateFilteredVideo();

        cardPage.SetData(m_FilteredVideo);
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

        SetListPanel(false);

        videoPlayerControl.SetTitle($"{Utillity.Instance.ConvertEnumToString(GameManager.Instance.SceneType)} / {Utillity.Instance.ConvertEnumToString(data.clubFilter)} • {Utillity.Instance.ConvertEnumToString(data.poseFilter)}");
        videoPlayerControl.SetProName($"{GolfProDataManager.Instance.GetProInfoData(data.uid).name} 프로");

        videoPlayerControl.PlayVideo(GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}"), data);

        GameManager.Instance.SetOptionPanel();
    }

    private void UpdateFilteredVideo()
    {
        if (m_LessonState == ELessonState.List || m_LessonState == ELessonState.Recently)
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
        }
        else if (m_LessonState == ELessonState.Best)
        {
            m_FilteredVideo = m_AllVideo
            .Where(v => v.poseFilter == m_VideoFinishPanel.CurVideoData.poseFilter)
                .OrderByDescending(v => GetSortValue(v))
                .ToList();
        }

        totalVideoCount = m_FilteredVideo.Count;
    }

    public void OnClick_NextPage() => cardPage.NextPage();

    public void OnClick_PrevPage() => cardPage.PrevPage();
    #endregion

    #region Filter Method
    public void ResetVideoList(EArraySortMode arraySort)
    {
        m_AllVideo = m_SelectProData.videoData.Where(v => v.videoType == EVideoType.Lesson).ToList();

        m_VideoFinishPanel.gameObject.SetActive(false);

        SetListPanel(true);

        m_PoseFilterHandler.Reset();
        m_ClubFilterHandler.Reset();

        currentSortMode = arraySort;

        m_PoseStateTxt.UpdateStringAndSize(m_PoseFilterHandler.SelectedCount != 0 ? $"자세({m_PoseFilterHandler.SelectedCount})" : $"자세");

        m_ClubStateTxt.UpdateStringAndSize(m_ClubFilterHandler.SelectedCount != 0 ? $"클럽({m_ClubFilterHandler.SelectedCount})" : $"클럽");

        m_ArrayFilterText.text = Utillity.Instance.ConvertEnumToString(currentSortMode);

        m_TitleText.text = $"레슨을 선택하세요";

        UpdateFilteredVideo();

        cardPage.SetData(m_FilteredVideo);
    }

    public void SetBestVideoPanel()
    {
        m_AllVideo = Utillity.Instance.LoadVideoUrlList(0, EVideoType.Lesson, EPoseDirection.All);

        m_VideoFinishPanel.gameObject.SetActive(false);


        m_FilterGroup.SetActive(false);
        SetListPanel(true);

        m_PoseFilterHandler.Reset();
        m_ClubFilterHandler.Reset();

        currentSortMode = EArraySortMode.View;

        m_ArrayFilterText.text = Utillity.Instance.ConvertEnumToString(currentSortMode);

        m_TitleText.text = $"{Utillity.Instance.ConvertEnumToString(m_VideoFinishPanel.CurVideoData.poseFilter)} BEST 영상";

        UpdateFilteredVideo();

        cardPage.SetData(m_FilteredVideo);
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

    public void OnValueChanged_ArrayFilter(bool isOn)
    {
        if (m_ArrayTg.GetFirstActiveToggle() == null) return;

        //GameObject obj = EventSystem.current.currentSelectedGameObject;

        int value = 0;

        value = m_ArrayTg.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (!isOn)
            return;

        if ((int)currentSortMode == value)
            return;

        switch (value)
        {
            case 0:
                currentSortMode = EArraySortMode.View;
                break;

            case 1:
                currentSortMode = EArraySortMode.Recently;
                break;

            case 2:
                currentSortMode = EArraySortMode.Favorite;
                break;
        }

        m_ArrayFilterText.text = Utillity.Instance.ConvertEnumToString(currentSortMode);

        UpdateFilteredVideo();
        cardPage.SetData(m_FilteredVideo);
    }

    private object GetSortValue(ProVideoData data)
    {
        switch (currentSortMode)
        {
            case EArraySortMode.View:
                return data.views;
            case EArraySortMode.Favorite:
                return data.favoriteCount;
            case EArraySortMode.Recently:
                return Utillity.Instance.StringToDateTime(data.recently);
            default:
                return data.views;
                //case EArraySortMode.Popularity:
                //default:
                //    return data.favoriteCount + data.views;
        }
    }

    public void OnClick_Popup(int pop)
    {
        m_PopupObjs[pop].SetActive(!m_PopupObjs[pop].activeInHierarchy);
    }

    public void OnClick_ArrayPanel()
    {
        m_ArrayPopup.SetActive(!m_ArrayPopup.activeInHierarchy);

        m_ArrayFilters[(int)currentSortMode].isOn = true;
    }
    #endregion

    public void OnValueChanged_ViewHistory(bool isOn)
    {
        m_CardListGroup.SetActive(!isOn);
        m_ViewHistoryList.SetActive(isOn);
    }

    public void Onclick_Button(string name)
    {
        switch (name)
        {
            case "Home":
                GameManager.Instance.SelectedSceneName = string.Empty;
                SceneManager.LoadScene("ModeSelect");
                break;

            case "Search":
                _SearchPopup.SetActive(!_SearchPopup.activeInHierarchy);
                break;

            case "Back":
                switch (m_LessonState)
                {
                    case ELessonState.List:
                        GameManager.Instance.SelectedSceneName = string.Empty;
                        SceneManager.LoadScene("ModeSelect");
                        break;

                    case ELessonState.Recently:
                    case ELessonState.Best:
                        LessonState = ELessonState.End;

                        m_PickPanel.SetActive(false);
                        m_Overlay_Canvas.SetActive(true);
                        m_VideoFinishPanel.gameObject.SetActive(true);
                        break;

                    case ELessonState.Play:
                    case ELessonState.End:
                        LessonState = ELessonState.List;
                        ResetVideoList(EArraySortMode.View);

                        m_VideoFinishPanel.gameObject.SetActive(false);
                        StartCoroutine(CoBackList());
                        break;
                }
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;
        }
    }

    public void SetListPanel(bool b)
    {
        m_PickPanel.SetActive(b);
        m_PlayPanel.SetActive(!b);
        m_Overlay_Canvas.SetActive(!b);
        m_BottomBack.SetActive(!b);
    }

    IEnumerator CoBackList()
    {
        videoPlayerControl.StopVideo();

        yield return new WaitForSeconds(0.5f);

        m_PickPanel.SetActive(true);
        m_PlayPanel.SetActive(false);
        m_Overlay_Canvas.SetActive(false);
    }
}
