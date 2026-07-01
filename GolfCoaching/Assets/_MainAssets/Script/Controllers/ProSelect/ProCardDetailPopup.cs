using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Enums;
using System.Collections.Generic;
using System.Collections;

public class ProPopupDetailPopup : MonoBehaviour
{
    [SerializeField] private FilterItemController[] filters;

    [SerializeField] private GameObject _proSelectPopup;
    [SerializeField] private GameObject _proProfile;
    [SerializeField] private GameObject[] m_DetailObjs;

    [SerializeField] private Image m_BadgeImg;

    [SerializeField] private Sprite[] m_BadgeSprites;

    [SerializeField] private ToggleGroup m_toggleGroup;
    [SerializeField] private Toggle _IntroduceToggleButton;
    [SerializeField] private Toggle _HistoryToggleButton;
    [SerializeField] private Toggle _ContentToggleButton;
    [SerializeField] private Toggle[] m_DetailToggles;

    [SerializeField] RectTransform m_ProSelectPanel;
    [SerializeField] RectTransform m_FavoritePanel;

    [SerializeField] private CanvasGroup m_blurBackground;

    [SerializeField] private CustomButton m_FavoriteBtn;

    [SerializeField] private VLCVideoPlayer m_FrontVideo;
    [SerializeField] private VLCVideoPlayer m_SideVideo;

    [SerializeField] private TextMeshProUGUI m_IntroduceText;
    [SerializeField] private TextMeshProUGUI m_HistoryText;

    private Vector2 proClosedPos;
    private Vector2 favoriteClosedPos;

    private Sequence favoriteSeq;

    SelectProData m_SelectProData = new SelectProData();

    [Header("[ Contents Video ]")]
    private CardPageControl<VideoCardController, ProVideoData> cardPage;

    [SerializeField] private GameObject m_ProDetailVideoPrefab;
    [SerializeField] private GameObject m_PreviewPopup;

    [SerializeField] VLCVideoPlayer m_PreviewVP;

    [SerializeField] private RectTransform m_PrevGroup;
    [SerializeField] private RectTransform m_CurGroup;
    [SerializeField] private RectTransform m_NextGroup;
    [SerializeField] private RectTransform m_HideGroup;

    [SerializeField] private Button NextPageButton;
    [SerializeField] private Button PrevPageButton;

    [SerializeField] private TextMeshProUGUI m_VideoCountText;

    private List<ProVideoData> m_AllVideo = new List<ProVideoData>();

    [SerializeField] private int cardsPerPage = 6;
    private int totalVideoCount = 0;

    private void Start()
    {
        m_blurBackground.alpha = 0f;

        proClosedPos = m_ProSelectPanel.anchoredPosition;
        favoriteClosedPos = m_FavoritePanel.anchoredPosition;

        for (int i = 0; i < m_DetailToggles.Length; i++)
        {
            m_DetailToggles[i].onValueChanged.AddListener(OnValueChanged_Detail);
        }
    }

    public void ProButtonClick()
    {
        try
        {
            OpenPanel();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in ProButtonClick: {ex.Message}");
        }
    }

    public void OnClick_CloseDetail()
    {
        try
        {
            _IntroduceToggleButton.isOn = true;
            _HistoryToggleButton.isOn = false;
            _ContentToggleButton.isOn = false;
            ClosePanel();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in CloseButtonClick: {ex.Message}");
        }
    }

    public void OnClick_SelectPro()
    {
        GolfProDataManager.Instance.SelectProData = m_SelectProData;

        GameManager.Instance.SelectedSceneName = "ModeSelect";
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnValueChanged_Detail(bool isOn)
    {
        if (m_toggleGroup.GetFirstActiveToggle() == null) return;

        int kind = m_toggleGroup.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (!isOn)
            return;

        for (int i = 0; i < m_DetailObjs.Length; i++)
        {
            if (kind != i)
                m_DetailObjs[i].SetActive(false);
            else
                m_DetailObjs[i].SetActive(true);
        }

        if (kind == 0)
        {
            m_FrontVideo.playbackSpeed = INI.PlaySpeedNormal;
            m_SideVideo.playbackSpeed = INI.PlaySpeedNormal;

            m_FrontVideo.Play();
            m_SideVideo.Play();
        }
        else if (kind == 2)
        {
            cardPage.SetData(m_AllVideo);
        }
    }

    public void OpenPanel()
    {
        m_FrontVideo.playbackSpeed = INI.PlaySpeedNormal;
        m_SideVideo.playbackSpeed = INI.PlaySpeedNormal;

        m_FrontVideo.Play();
        m_SideVideo.Play();

        m_blurBackground.alpha = 0f;
        m_blurBackground.DOFade(1.0f, 0.1f);

        m_ProSelectPanel.anchoredPosition = proClosedPos;
        m_ProSelectPanel.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutCubic);
    }

    public void ClosePanel()
    {
        m_FrontVideo.Stop();
        m_SideVideo.Stop();

        m_blurBackground.DOFade(0.0f, 0.1f);

        m_ProSelectPanel.DOAnchorPosY(proClosedPos.y, 0.3f).SetEase(Ease.InCubic);
    }

    public void OnClickFavorite()
    {
        if (m_FavoriteBtn.IsToggled)
            return;

        if (favoriteSeq != null && favoriteSeq.IsActive())
        {
            favoriteSeq.Kill();
        }

        Debug.Log("[OnClickFavorite]");

        m_FavoritePanel.anchoredPosition = favoriteClosedPos;

        favoriteSeq = DOTween.Sequence();
        favoriteSeq.Append(m_FavoritePanel.DOAnchorPosY(0.0f, 0.5f).SetEase(Ease.OutCubic)).AppendInterval(1.0f);
        favoriteSeq.Append(m_FavoritePanel.DOAnchorPosY(favoriteClosedPos.y, 0.5f).SetEase(Ease.InCubic));

        favoriteSeq.Play();
    }

    public void SetProProfileData(int uid, ProInfoData proInfoData)
    {
        try
        {
            m_SelectProData.uid = uid;
            m_SelectProData.infoData = proInfoData;

            // 프로 전,측 영상
            m_FrontVideo.Stop();
            m_SideVideo.Stop();
            m_FrontVideo.url = string.Empty;
            m_SideVideo.url = string.Empty;

            if (GolfProDataManager.Instance.GetProVideoDic().TryGetValue(uid, out var videos))
            {
                m_SelectProData.videoData = videos;

                string path = videos.Where(v => v.direction == EPoseDirection.Front && v.videoType == EVideoType.Swing && v.clubFilter == EClub.MiddleIron && v.swingType == ESwingType.Full).Select(v => v.path).FirstOrDefault();

                if (!string.IsNullOrEmpty(path))
                    m_FrontVideo.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{path}");

                path = videos.Where(v => v.direction == EPoseDirection.Side && v.videoType == EVideoType.Swing && v.clubFilter == EClub.MiddleIron && v.swingType == ESwingType.Full).Select(v => v.path).FirstOrDefault();

                if (!string.IsNullOrEmpty(path))
                    m_SideVideo.url = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{path}");
            }
            else
            {
                m_SelectProData.videoData = new List<ProVideoData>();
            }

            // 프로 썸네일 이미지
            string thumbPath = GolfProDataManager.Instance.GetProImageData(uid, EImageType.Thumbnail).path;

            StartCoroutine(GameManager.Instance.LoadImageCoroutine($"{INI.proImagePath}{uid}/{thumbPath}", (Sprite sprite) =>
            {
                if (sprite != null)
                {
                    _proProfile.transform.GetChild(0).GetComponent<Image>().sprite = sprite;
                    m_SelectProData.imageData = GolfProDataManager.Instance.GetProImageDataList(uid);
                }
                else
                    Debug.Log("프로필 Sprite 로드 실패");
            }));

            // 프로 이름, 소속
            _proProfile.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = proInfoData.name;
            _proProfile.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = proInfoData.info;

            // 프로 랭크 뱃지
            int rank = Utillity.Instance.GetPopularityRank(uid);

            if (rank == 1)
            {
                m_BadgeImg.gameObject.SetActive(true);
                m_BadgeImg.sprite = m_BadgeSprites[0];
            }
            else if (rank == 2)
            {
                m_BadgeImg.gameObject.SetActive(true);
                m_BadgeImg.sprite = m_BadgeSprites[1];
            }
            else if (rank == 3)
            {
                m_BadgeImg.gameObject.SetActive(true);
                m_BadgeImg.sprite = m_BadgeSprites[2];
            }
            else if (rank > 0)
            {
                m_BadgeImg.gameObject.SetActive(false);
            }
            else
            {
                m_BadgeImg.gameObject.SetActive(false);
                Debug.Log($"해당 uid 없음");
            }

            m_BadgeImg.gameObject.SetActive(false);

            // 프로 키워드
            for (int i = 0; i < proInfoData.filters.Length; i++)
            {
                filters[i].UpdateStringAndSize(Utillity.Instance.ConvertEnumToString(proInfoData.filters[i]));
            }

            // 프로 소개
            m_IntroduceText.text = proInfoData.introduce;

            // 프로 경력, 활동
            m_HistoryText.text = proInfoData.history;

            // 프로 스윙 데이터
            m_SelectProData.swingData = GolfProDataManager.Instance.GetSwingData(uid);
            m_SelectProData.aiSwingData = GolfProDataManager.Instance.GetAISwingData(uid);

            LoadVideoUI();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in SetProProfileData: {ex.Message}");
        }
    }

    private void InitCardPage()
    {
        cardPage = new CardPageControl<VideoCardController, ProVideoData>();
        cardPage.Initialize(m_ProDetailVideoPrefab, m_PrevGroup, m_CurGroup, m_NextGroup, m_HideGroup, cardsPerPage, (card, data, index, onClick) =>
        {
            card.SetProDetailVideoCard(data, index, onClick);
            return card;
        },
        SetPages);

        cardPage.SetCardClickAction(ApplyVideoData);
    }

    private void LoadVideoUI()
    {
        InitCardPage();

        m_AllVideo = m_SelectProData.videoData.Where(v => v.videoType == EVideoType.Lesson).ToList();

        totalVideoCount = m_AllVideo.Count;
    }

    private void SetPages()
    {
        m_VideoCountText.text = $"영상 : {totalVideoCount}개";

        PrevPageButton.interactable = cardPage.CurrentPage > 0;
        NextPageButton.interactable = cardPage.CurrentPage + 1 < cardPage.TotalPages;
    }

    public void OnClick_NextPage() => cardPage.NextPage();
    public void OnClick_PrevPage() => cardPage.PrevPage();

    public void ApplyVideoData(ProVideoData data)
    {
        //Debug.Log($"[ApplyVideoData] {data.name}");
        OnClick_PreviewPopup();

        if (m_PreviewPopup.activeInHierarchy)
        {
            if (data == null)
                return;

            string videoURL = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}");
            m_PreviewVP.url = videoURL;

            StartCoroutine(PlayPreView());
        }
    }

    private IEnumerator PlayPreView()
    {
        if (m_PreviewVP == null)
        {
            Debug.Log($"VLCVideoPlayer is null");
            yield break;
        }

        yield return null;

        while (m_PreviewPopup.activeInHierarchy)
        {
            m_PreviewVP.position = 0f;
            m_PreviewVP.Play();

            yield return new WaitForSeconds(5.0f);
            m_PreviewVP.Pause();

        }
    }

    public void OnClick_PreviewPopup()
    {
        m_PreviewPopup.SetActive(!m_PreviewPopup.activeInHierarchy);
    }
}
