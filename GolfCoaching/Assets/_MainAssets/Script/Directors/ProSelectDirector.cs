using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using Enums;
using System.Linq;


public class ProSelectDirector : MonoBehaviour
{
    private CardPageControl<ProCardController, ProInfoData> cardPage;

    private FilterHandler<EStance> m_PoseFilterHandler;
    private FilterHandler<EClub> m_ClubFilterHandler;
    private FilterHandler<EFilter> m_SpecializationFilterHandler;

    [SerializeField] private ProPopupDetailPopup _proPopupDetailPopup;

    [SerializeField] private GameObject _proDataPrefab;
    
    [SerializeField] private RectTransform m_PrevGroup;
    [SerializeField] private RectTransform m_CurGroup;
    [SerializeField] private RectTransform m_NextGroup;
    [SerializeField] private RectTransform m_HideGroup;

    [SerializeField] private TMP_Dropdown m_GenderDropdown;

    [SerializeField] private Button NextPageButton;
    [SerializeField] private Button PrevPageButton;

    [SerializeField] private TextMeshProUGUI m_PageText;
    [SerializeField] private TextMeshProUGUI m_TotalProText;

    private int selectGender = 0; //    0:전체, 1:남, 2:여
    private int cardsPerPage = 4;
    private int totalProCount = 0;

    [Header("Search Panel")]
    [SerializeField] private GameObject _SearchPopup;
    [SerializeField] private GameObject _FilterPopup;
    [SerializeField] private GameObject[] m_FilterGroupObjs;

    [SerializeField] private Toggle m_FilterAllToggle;
    [SerializeField] private Toggle m_PoseAllFilter;
    [SerializeField] private Toggle[] m_PoseFilters;
    [SerializeField] private Toggle m_ClubAllFilter;
    [SerializeField] private Toggle[] m_ClubFilters;

    [SerializeField] private ToggleGroup m_PoseTg;
    [SerializeField] private ToggleGroup m_ClubTg;

    private Dictionary<int, ProInfoData> m_AllPro = new Dictionary<int, ProInfoData>();
    private List<ProVideoData> m_AllVideo = new List<ProVideoData>();
    private Dictionary<int, ProInfoData> m_FilteredPro = new Dictionary<int, ProInfoData>();

    private List<EStance> m_SelectPoseList = new List<EStance>();
    private List<EClub> m_SelectClubList = new List<EClub>();

    [Header("Specialization Panel")]
    [SerializeField] private GameObject m_SpecializationPopup;

    [SerializeField] private Toggle[] m_SpecializationFilters;

    [SerializeField] private FilterItemController m_SpecializationStateTxt;

    private List<EFilter> m_SelectFilterList = new List<EFilter>();

    [Header("Array Panel")]
    [SerializeField] private GameObject m_ArrayPopup;

    [SerializeField] private Toggle[] m_ArrayFilters;

    [SerializeField] private TextMeshProUGUI m_ArrayFilterText;

    [SerializeField] private EArraySortMode currentSortMode = EArraySortMode.View;

    private void Start()
    {
        StartCoroutine(WaitForProData());

        Init();
    }

    IEnumerator WaitForProData()
    {
        yield return new WaitUntil(() => GolfProDataManager.Instance != null && GolfProDataManager.Instance.GetProInfoList() != null);

        PerformLoadProUI();
    }

    private void PerformLoadProUI()
    {
        m_AllPro = GolfProDataManager.Instance.GetProInfoList();
        m_AllVideo = Utillity.Instance.LoadVideoUrlList(0, EVideoType.Lesson, EPoseDirection.All);

        totalProCount = GolfProDataManager.Instance.GetProDataList().Count;
        
        InitCardPage();

        UpdateFilteredPro();

        cardPage.SetData(m_FilteredPro.Values.ToList());
    }

    private void Init()
    {
        m_PoseFilterHandler = new FilterHandler<EStance>(m_PoseFilters, m_PoseAllFilter, m_PoseTg, m_SelectPoseList, UpdateFilteredPro);

        m_ClubFilterHandler = new FilterHandler<EClub>(m_ClubFilters, m_ClubAllFilter, m_ClubTg, m_SelectClubList, UpdateFilteredPro);

        m_SpecializationFilterHandler = new FilterHandler<EFilter>(m_SpecializationFilters, null, null, m_SelectFilterList, UpdateFilteredPro);

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

        for (int i = 0; i < m_SpecializationFilters.Length; i++)
        {
            m_SpecializationFilters[i].onValueChanged.AddListener(m_SpecializationFilterHandler.OnValueChangedFilter);
        }

        for (int i = 0; i < m_ArrayFilters.Length; i++)
        {
            m_ArrayFilters[i].onValueChanged.AddListener(OnValueChanged_ArrayFilter);
        }

        m_GenderDropdown.onValueChanged.AddListener(OnDropdownChanged);

        m_ArrayFilterText.text = Utillity.Instance.ConvertEnumToString(currentSortMode);
    }

    #region Set ProData
    private void InitCardPage()
    {
        cardPage = new CardPageControl<ProCardController, ProInfoData>();
        cardPage.Initialize(_proDataPrefab, m_PrevGroup, m_CurGroup, m_NextGroup, m_HideGroup, cardsPerPage, (card, data, index, onClick) => {
            var imageData = GolfProDataManager.Instance.GetProImageData(data.uid, EImageType.Profile);
            card.SetProCard(data, imageData, (int uid) => onClick(data));
            return card;
        },
        SetPages);

        cardPage.SetCardClickAction(data => ApplyProData(data.uid));
    }

    private void SetPages()
    {
        m_PageText.text = $"{cardPage.CurrentPage + 1} / {cardPage.TotalPages}";

        m_TotalProText.text = $"총 {totalProCount}명";

        PrevPageButton.interactable = cardPage.CurrentPage > 0;
        NextPageButton.interactable = cardPage.CurrentPage + 1 < cardPage.TotalPages;
    }

    public void ApplyProData(int uid)
    {
        var proInfoData = GolfProDataManager.Instance.GetProInfoData(uid);

        if (proInfoData.Equals(default(ProInfoData)))
        {
            Debug.LogError("One of the data parameters is null or default");
            return;
        }

        _proPopupDetailPopup.SetProProfileData(uid, proInfoData);

        _proPopupDetailPopup.ProButtonClick();
    }

    private void UpdateFilteredPro()
    {
        if ((m_SelectFilterList == null || m_SelectFilterList.Count == 0) && (m_SelectPoseList == null || m_SelectPoseList.Count == 0) && (m_SelectClubList == null || m_SelectClubList.Count == 0))
        {
            m_FilteredPro = m_AllPro
                .Where(p => (selectGender == 0 || p.Value.gender == selectGender))
                .OrderByDescending(p => GetSortValue(p.Value))
                .ToDictionary(p => p.Key, p => p.Value);

            //m_FilteredPro = new Dictionary<int, ProInfoData>(m_AllPro);
        }
        else
        {
            List<ProVideoData> vData = m_AllVideo.Where(v => m_SelectPoseList.Contains(v.poseFilter) || m_SelectClubList.Contains(v.clubFilter)).ToList();

            List<int> proUids = vData.Select(v => v.uid).ToList();

            m_FilteredPro = m_AllPro
            .Where(p => p.Value.filters.Any(f => m_SelectFilterList.Contains(f)) || proUids.Contains(p.Key))
            .Where(p => (selectGender == 0 || p.Value.gender == selectGender))
            .OrderByDescending(p => GetSortValue(p.Value))
            .ThenBy(p => p.Key)
            .ToDictionary(p => p.Key, p => p.Value);
        }

        totalProCount = m_FilteredPro.Count;
    }

    public void OnClick_NextPage() => cardPage.NextPage();

    public void OnClick_PrevPage() => cardPage.PrevPage();
    #endregion

    #region Search Panel Method
    public void OnValueChanged_PoseFilter(bool isOn)
    {
        int value = 0;
        
        foreach (var toggle in m_PoseFilters)
        {
            value = toggle.gameObject.GetComponent<UIValueObject>().intValue;

            EStance pose = (EStance)value;

            if (toggle.isOn)
            {
                if (!m_SelectPoseList.Contains(pose))
                {
                    m_SelectPoseList.Add(pose);
                }
            }
            else
            {
                if (m_SelectPoseList.Contains(pose))
                {
                    m_SelectPoseList.Remove(pose);
                }
            }
        }

        if(m_SelectPoseList.Count <= 0)
        {
            m_PoseTg.allowSwitchOff = false;
            m_PoseAllFilter.isOn = true;
        }
        else
        {
            m_PoseTg.allowSwitchOff = true;
            m_PoseAllFilter.isOn = false;
        }

        UpdateFilteredPro();
    }

    public void OnValueChanged_PoseAll(bool isOn)
    {
        if (isOn)
        {
            m_PoseTg.allowSwitchOff = false;

            for (int i = 0; i < m_PoseFilters.Length; i++)
            {
                m_PoseFilters[i].isOn = false;
            }

            m_SelectPoseList.Clear();

            UpdateFilteredPro();
        }
    }

    public void OnValueChanged_ClubFilter(bool isOn)
    {
        int value = 0;

        foreach (var toggle in m_ClubFilters)
        {
            value = toggle.gameObject.GetComponent<UIValueObject>().intValue;

            EClub club = (EClub)value;

            if (toggle.isOn)
            {
                if (!m_SelectClubList.Contains(club))
                {
                    m_SelectClubList.Add(club);
                }
            }
            else
            {
                if (m_SelectClubList.Contains(club))
                {
                    m_SelectClubList.Remove(club);
                }
            }
        }

        if (m_SelectClubList.Count <= 0)
        {
            m_ClubTg.allowSwitchOff = false;
            m_ClubAllFilter.isOn = true;
        }
        else
        {
            m_ClubTg.allowSwitchOff = true;
            m_ClubAllFilter.isOn = false;
        }

        UpdateFilteredPro();
    }

    public void OnValueChanged_ClubAll(bool isOn)
    {
        if (isOn)
        {
            m_ClubTg.allowSwitchOff = false;

            for (int i = 0; i < m_ClubFilters.Length; i++)
            {
                m_ClubFilters[i].isOn = false;
            }

            m_SelectClubList.Clear();

            UpdateFilteredPro();
        }
    }

    public void OnClick_Search()
    {
        _SearchPopup.SetActive(!_SearchPopup.activeInHierarchy);
    }

    public void OnClick_Filter()
    {
        m_FilterAllToggle.isOn = true;

        _FilterPopup.SetActive(!_FilterPopup.activeInHierarchy);
    }

    public void OnClick_ApplyFilter()
    {
        cardPage.SetData(m_FilteredPro.Values.ToList());

        OnClick_Filter();
    }

    public void OnClick_ResetFilter()
    {
        m_PoseFilterHandler.Reset();
        m_ClubFilterHandler.Reset();
        UpdateFilteredPro();
    }

    public void OnValueChanged_FilterGroup(int value)
    {
        if(value == -1)
        {
            for (int i = 0; i < m_FilterGroupObjs.Length; i++)
            {
                m_FilterGroupObjs[i].SetActive(true);
                m_FilterGroupObjs[i].transform.SetSiblingIndex(i);
            }

            return;
        }

        for (int i = 0; i < m_FilterGroupObjs.Length; i++)
        {
            if (value != i)
            {
                m_FilterGroupObjs[i].SetActive(false);
            }
            else
            {
                m_FilterGroupObjs[i].SetActive(true);
                m_FilterGroupObjs[i].transform.SetSiblingIndex(0);
            }
        }
    }
    #endregion

    #region Specialization Panel Method
    public void OnValueChanged_SpecializationFilter(bool isOn)
    {
        int value = 0;

        foreach (var toggle in m_SpecializationFilters)
        {
            value = toggle.gameObject.GetComponent<UIValueObject>().intValue;

            EFilter specialization = (EFilter)value;

            if (toggle.isOn)
            {
                if (!m_SelectFilterList.Contains(specialization))
                {
                    m_SelectFilterList.Add(specialization);
                }
            }
            else
            {
                if (m_SelectFilterList.Contains(specialization))
                {
                    m_SelectFilterList.Remove(specialization);
                }
            }
        }

        UpdateFilteredPro();
    }

    public void OnClick_ApplySpecialization()
    {
        cardPage.SetData(m_FilteredPro.Values.ToList());

        m_SpecializationStateTxt.UpdateStringAndSize(m_SpecializationFilterHandler.SelectedCount != 0 ? $"전문분야({m_SpecializationFilterHandler.SelectedCount})" : $"전문분야");

        OnClick_SpecializationPanel();
    }

    public void OnClick_ResetSpecialization()
    {
        m_SpecializationFilterHandler.Reset();
        UpdateFilteredPro();
    }

    public void OnClick_SpecializationPanel()
    {
        m_SpecializationPopup.SetActive(!m_SpecializationPopup.activeInHierarchy);
    }
    #endregion

    #region Array Panel Method
    private object GetSortValue(ProInfoData data)
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

    public void OnValueChanged_ArrayFilter(bool isOn)
    {
        GameObject obj = EventSystem.current.currentSelectedGameObject;
        int value = obj.GetComponent<UIValueObject>().intValue;

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

            case 3:
                Debug.Log($"영상 많은 순");
                break;
        }

        m_ArrayFilterText.text = Utillity.Instance.ConvertEnumToString(currentSortMode);

        UpdateFilteredPro();
        cardPage.SetData(m_FilteredPro.Values.ToList());
    }

    public void OnClick_ArrayPanel()
    {
        m_ArrayPopup.SetActive(!m_ArrayPopup.activeInHierarchy);
    }
    #endregion

    public void OnDropdownChanged(int index)
    {
        selectGender = index;

        UpdateFilteredPro();
        cardPage.SetData(m_FilteredPro.Values.ToList());
    }

    public void Onclick_Back()
    {
        SceneManager.LoadScene(GameManager.Instance.SelectedSceneName);
    }
}
