using Enums;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoCardController : MonoBehaviour
{
    [SerializeField] private VLCVideoPlayer videoPlayer;

    [SerializeField] private RawImage rawImage;

    [SerializeField] private Image backgroundImage;

    [SerializeField] private Texture2D defaultImage;

    //[SerializeField] RenderTexture[] targetRTs;

    [SerializeField] private TextMeshProUGUI m_ClubAndPoseText = null;
    [SerializeField] private TextMeshProUGUI m_VideoNameText = null;
    [SerializeField] private TextMeshProUGUI m_ViewsText = null;
    [SerializeField] private TextMeshProUGUI m_VideoTimeText = null;

    [Header("[ ProData ]")]
    [SerializeField] private Image m_ProProfile = null;
    [SerializeField] private TextMeshProUGUI m_ProNameText = null;
    [SerializeField] private TextMeshProUGUI m_ProOrganizationText = null;

    private ProVideoData m_VideoData;
    private ProInfoData m_ProData;

    private EVideoSourceType m_VideoSourceType = EVideoSourceType.None;

    private string videoURL;

    private Action<ProVideoData> actVideoData;
    private Action<ProVideoData> actFinishVideoData;

    private bool _thumbLocked = false;

    public void SetProDetailVideoCard(ProVideoData data, int cardNum, Action<ProVideoData> act)
    {
        if (data == null)
            return;

        videoPlayer.targetDisplay = rawImage;

        m_VideoData = data;
        actVideoData = act;

        videoURL = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}");
        videoPlayer.url = videoURL;

        videoPlayer.started -= OnVideoStarted;
        videoPlayer.started += OnVideoStarted;

        if (videoPlayer.gameObject.activeInHierarchy)
            StartCoroutine(PlayAfterInitialization());
    }

    public void SetVideoCard(ProVideoData data, int cardNum, Action<ProVideoData> act)
    {
        if (data == null)
            return;

        videoPlayer.targetDisplay = rawImage;

        if (defaultImage != null)
            rawImage.texture = defaultImage;

        m_VideoData = data;
        m_ProData = GolfProDataManager.Instance.GetProInfoData(data.uid);

        actVideoData = act;

        string clubText = Utillity.Instance.ConvertEnumToString(data.clubFilter);
        string poseText = data.videoType == EVideoType.Swing
            ? ConvertSwingTypeToString(data.swingType)
            : Utillity.Instance.ConvertEnumToString(data.poseFilter);

        m_ClubAndPoseText.text = string.IsNullOrEmpty(poseText) ? clubText : $"{clubText} • {poseText}";

        bool isStudioSwingVideo = GameManager.Instance.SelectedSceneName == "Studio" && data.videoType == EVideoType.Swing;

        if (m_VideoNameText != null)
        {
            m_VideoNameText.gameObject.SetActive(!isStudioSwingVideo);

            if (!isStudioSwingVideo)
                m_VideoNameText.text = $"{data.name}";
        }

        if (m_ViewsText != null)
        {
            m_ViewsText.gameObject.SetActive(data.videoType != EVideoType.Swing);

            if (data.videoType != EVideoType.Swing)
                m_ViewsText.text = $"조회수 {Utillity.Instance.FormatViewsCount(data.views)}회";
        }

        videoURL = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}");
        videoPlayer.url = videoURL;

        videoPlayer.started -= OnVideoStarted;
        videoPlayer.started += OnVideoStarted;
        StartCoroutine(PlayAfterInitialization());
    }

    public void SetFinishVideoCard(ProVideoData data, int cardNum, Action<ProVideoData> act, EVideoSourceType sourceType = EVideoSourceType.None)
    {
        if (data == null)
            return;

        videoPlayer.targetDisplay = rawImage;

        if (defaultImage != null)
            rawImage.texture = defaultImage;

        m_VideoSourceType = sourceType;
        m_VideoData = data;
        m_ProData = GolfProDataManager.Instance.GetProInfoData(data.uid);

        actFinishVideoData = act;

        if (!object.ReferenceEquals(backgroundImage, null))
        {
            backgroundImage.color = (sourceType == EVideoSourceType.Best && cardNum == 0) ? Utillity.Instance.HexToRGB(INI.Green700) : Color.white;
        }

        if (!object.ReferenceEquals(m_VideoNameText, null))
        {
            m_VideoNameText.text = (sourceType == EVideoSourceType.Best && cardNum == 0) ? $"<color=white>{data.name}</color>" : $"<color=black>{data.name}</color>";
        }

        if (!object.ReferenceEquals(m_ViewsText, null))
        {
            m_ViewsText.text = (sourceType == EVideoSourceType.Best && cardNum == 0) ? $"<color=white>조회수 {Utillity.Instance.FormatViewsCount(data.views)}회</color>" : $"<color=black>조회수 {Utillity.Instance.FormatViewsCount(data.views)}회</color>";
        }

        if (!object.ReferenceEquals(m_ProProfile, null))
        {
            StartCoroutine(GameManager.Instance.LoadImageCoroutine($"{INI.proImagePath}{data.uid}/{GolfProDataManager.Instance.GetProImageData(data.uid, Enums.EImageType.Thumbnail).path}", (Sprite sprite) =>
            {
                if (sprite != null)
                {
                    m_ProProfile.sprite = sprite;
                }
                else
                    Debug.Log("썸네일 Sprite 로드 실패");
            }));
        }

        if (!object.ReferenceEquals(m_ProNameText, null))
        {
            m_ProNameText.text = (sourceType == EVideoSourceType.Best && cardNum == 0) ? $"<color=white>{m_ProData.name}</color>" : $"<color=black>{m_ProData.name}</color>";
        }

        if (!object.ReferenceEquals(m_ProOrganizationText, null))
        {
            m_ProOrganizationText.text = (sourceType == EVideoSourceType.Best && cardNum == 0) ? $"<color=white>{m_ProData.info}</color>" : $"<color=black>{m_ProData.info}</color>";
        }

        videoURL = GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}");
        videoPlayer.url = videoURL;

        videoPlayer.started -= OnVideoStarted;
        videoPlayer.started += OnVideoStarted;
        StartCoroutine(PlayAfterInitialization());
    }

    public void OnClick_VideoSelect()
    {
        if (actVideoData != null)
            actVideoData.Invoke(m_VideoData);
    }

    public void OnClick_FinishVideoSelect()
    {
        if (actFinishVideoData != null)
            actFinishVideoData.Invoke(m_VideoData);
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

    private IEnumerator PlayVideo()
    {
        if (videoPlayer == null)
        {
            Debug.Log($"VLCVideoPlayer is null");
            yield break;
        }

        while (!videoPlayer.gameObject.activeInHierarchy)
        {
            yield return null;
        }

        while (true)
        {
            videoPlayer.position = 0f;
            videoPlayer.Play();

            yield return new WaitForSeconds(3.0f);
            videoPlayer.Pause();
        }
    }

    private IEnumerator PlayAfterInitialization()
    {
        yield return null;
        _thumbLocked = false;
        videoPlayer.Play();
    }

    private void OnVideoStarted(VLCVideoPlayer vp)
    {
        if (_thumbLocked)
            return;

        _thumbLocked = true;
        vp.Pause();

        if (m_VideoTimeText != null)
            m_VideoTimeText.text = FormatTime(vp.length);
    }

    private static string FormatTime(long milliseconds)
    {
        long totalSeconds = Math.Max(0, milliseconds / 1000);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
            videoPlayer.started -= OnVideoStarted;
    }}
