using Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoFInishPanel : MonoBehaviour
{
    [SerializeField] VideoPlayerControl m_VideoPlayerControl = null;
    [SerializeField] FocusCoachingDirecter m_FocusCoachingDirector = null;

    [Header("[ RecommendVideo ]")]
    [SerializeField] private GameObject m_RecommendVideo = null;
    [SerializeField] Button[] m_RecoomendPlayButtons = null;

    [SerializeField] TextMeshProUGUI m_PlayNextDetailTxt;
    [SerializeField] TextMeshProUGUI[] m_PlayNextFiltersTxt;
    [SerializeField] TextMeshProUGUI m_PlayNextViewsTxt;

    [Header("[ RecentlyVideo ]")]
    [SerializeField] GameObject m_RecentlyPrefab;
    [SerializeField] GameObject m_RecentlyVideo;

    [SerializeField] Transform m_RecentlyParent;

    private ObjectPool<VideoCardController> m_RecentlyPool;

    private List<VideoCardController> m_ActiveRecentlyCards = new List<VideoCardController>();

    [Header("[ BestVideo ]")]
    [SerializeField] GameObject m_BestPrefab;
    [SerializeField] GameObject m_BestVideo;

    [SerializeField] Transform m_BestParent;

    [SerializeField] TextMeshProUGUI m_PoseTxt;

    private ObjectPool<VideoCardController> m_BestPool;

    private List<VideoCardController> m_ActiveBestCards = new List<VideoCardController>();

    private ProVideoData m_CurVideoData;
    public ProVideoData CurVideoData {
        get { return m_CurVideoData; }
    }

    private List<ProVideoData> m_AllVideo = new List<ProVideoData>();
    private List<ProVideoData> m_PlayedVideoList = new List<ProVideoData>();
    private List<ProVideoData> m_DuplicationList = new List<ProVideoData>();

    private void Awake()
    {
        m_RecentlyPool = new ObjectPool<VideoCardController>(m_RecentlyPrefab.GetComponent<VideoCardController>(), m_RecentlyParent, 6);

        m_BestPool = new ObjectPool<VideoCardController>(m_BestPrefab.GetComponent<VideoCardController>(), m_BestParent, 6);
    }

    public IEnumerator SetData(ProVideoData data)
    {
        yield return null;

        m_CurVideoData = data;

        AddToPlayedList(data);

        m_DuplicationList.Clear();
        m_DuplicationList.Add(data);

        AddRangeDistinct(m_DuplicationList, m_PlayedVideoList);

        // 추천영상
        m_AllVideo.Clear();
        m_AllVideo = GolfProDataManager.Instance.SelectProData.videoData.Where(v => v.videoType == EVideoType.Lesson).ToList();

        List<ProVideoData> recommendList = m_AllVideo
            .Where(v => 
                v.uid == data.uid &&
                !m_PlayedVideoList.Any(w => w.uid == v.uid && w.id == v.id) &&
                !m_DuplicationList.Any(w => w.uid == v.uid && w.id == v.id))
            .ToList();

        if(recommendList.Count > 0)
        {
            m_RecommendVideo.SetActive(true);

            ProVideoData randomData = recommendList[UnityEngine.Random.Range(0, recommendList.Count)];
            AddRangeDistinct(m_DuplicationList, new List<ProVideoData>() { randomData });
            SetRecommendVideo(randomData);
        }
        else
        {
            m_RecommendVideo.SetActive(false);
        }
        
        // Best영상
        m_AllVideo.Clear();
        m_AllVideo = Utillity.Instance.LoadVideoUrlList(0, EVideoType.Lesson, EPoseDirection.All);

        List<ProVideoData> bestList = m_AllVideo
            .Where(v => 
                v.poseFilter == data.poseFilter &&
                !m_PlayedVideoList.Any(w => w.uid == v.uid && w.id == v.id) && 
                !m_DuplicationList.Any(y => y.uid == v.uid && y.id == v.id))
            .OrderByDescending(v => v.views)
            .Take(6)
            .ToList();

        AddRangeDistinct(m_DuplicationList, bestList);

        if (bestList.Count > 0)
        {
            m_BestVideo.SetActive(true);

            SetBestVideo(bestList);
        }
        else
            m_BestVideo.SetActive(false);

        // 최신영상
        m_AllVideo.Clear();
        m_AllVideo = GolfProDataManager.Instance.SelectProData.videoData.Where(v => v.videoType == EVideoType.Lesson).ToList();
        
        List<ProVideoData> recentlyList = m_AllVideo
            .Where(v => 
                v.uid == data.uid &&
                !m_PlayedVideoList.Any(w => w.uid == v.uid && w.id == v.id) && 
                !m_DuplicationList.Any(y => y.uid == v.uid && y.id == v.id))
            .OrderByDescending(v => Utillity.Instance.StringToDateTime(v.recently))
            .Take(6)
            .ToList();

        AddRangeDistinct(m_DuplicationList, recentlyList);

        if (recentlyList.Count > 0)
        {
            m_RecentlyVideo.SetActive(true);

            SetRecentlyVideo(recentlyList);
        }
        else
            m_RecentlyVideo.SetActive(false);
    }

    private void SetRecommendVideo(ProVideoData data)
    {
        foreach (var button in m_RecoomendPlayButtons)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnClick_VideoItem(data));
        }

        m_PlayNextDetailTxt.text = data.name;
        m_PlayNextFiltersTxt[0].text = $"#{Utillity.Instance.ConvertEnumToString(data.poseFilter)}";
        m_PlayNextFiltersTxt[1].text = $"#{Utillity.Instance.ConvertEnumToString(data.clubFilter)}";
        m_PlayNextViewsTxt.text = $"{Utillity.Instance.FormatViewsCount(data.views)}회";
    }

    private void SetRecentlyVideo(List<ProVideoData> datas)
    {
        m_RecentlyPool.ReturnAll(m_ActiveRecentlyCards);

        foreach(ProVideoData data in datas)
        {
            VideoCardController card = m_RecentlyPool.Get();
            card.transform.SetParent(m_RecentlyParent, false);
            card.SetFinishVideoCard(data, m_ActiveRecentlyCards.Count, OnClick_VideoItem, EVideoSourceType.Recently);
            m_ActiveRecentlyCards.Add(card);
        }
    }

    private void SetBestVideo(List<ProVideoData> datas)
    {
        m_BestPool.ReturnAll(m_ActiveBestCards);

        m_PoseTxt.text = $"{Utillity.Instance.ConvertEnumToString(m_CurVideoData.poseFilter)} BEST 영상";

        for(int i = 0; i < datas.Count; i++)
        {
            VideoCardController card = m_BestPool.Get();
            card.transform.SetParent(m_BestParent, false);
            card.SetFinishVideoCard(datas[i], m_ActiveBestCards.Count, OnClick_VideoItem, EVideoSourceType.Best);
            m_ActiveBestCards.Add(card);
        }
    }

    private void AddRangeDistinct(List<ProVideoData> target, IEnumerable<ProVideoData> source)
    {
        foreach (var item in source)
        {
            if (!target.Any(v => v.uid == item.uid && v.id == item.id))
            {
                target.Add(item);
            }
        }
    }

    private void AddToPlayedList(ProVideoData data)
    {
        if (!m_PlayedVideoList.Any(v => v.uid == data.uid && v.id == data.id))
        {
            //Debug.Log($"[AddToPlayedList] {data.name} 추가됨");
            m_PlayedVideoList.Add(data);
        }
            
    }

    public void OnClick_VideoItem(ProVideoData data)
    {
        m_FocusCoachingDirector.SetListPanel(false);

        AddToPlayedList(data);

        m_VideoPlayerControl.SetTitle($"{Utillity.Instance.ConvertEnumToString(GameManager.Instance.SceneType)} / {Utillity.Instance.ConvertEnumToString(data.clubFilter)} • {Utillity.Instance.ConvertEnumToString(data.poseFilter)}");

        m_VideoPlayerControl.SetProName($"{GolfProDataManager.Instance.GetProInfoData(data.uid).name} 프로");

        m_VideoPlayerControl.PlayVideo(GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{data.uid}/{data.path}"), data);

        gameObject.SetActive(false);
    }

    public void OnClick_Repeat()
    {
        OnClick_VideoItem(m_CurVideoData);
    }

    public void OnClick_MoreRecent()
    {
        m_FocusCoachingDirector.LessonState = ELessonState.Recently;
        m_FocusCoachingDirector.ResetVideoList(EArraySortMode.Recently);
    }

    public void OnClick_MoreBest()
    {
        m_FocusCoachingDirector.LessonState = ELessonState.Best;
        m_FocusCoachingDirector.SetBestVideoPanel();
    }

    public void OnClick_Practice()
    {
        GameManager.Instance.Mode = EStep.Preview;

        GameManager.Instance.Stance = Utillity.Instance.PoseToSWINGSTEP(m_CurVideoData.poseFilter);
        GameManager.Instance.Club = m_CurVideoData.clubFilter;

        //GameManager.Instance.SelectedSceneName = "PracticeMode";
        GameManager.Instance.SelectedSceneName = "Mirror";
        //SceneManager.LoadScene("PracticeMode");
        SceneLoader.LoadScene("Mirror");
    }

    public void OnClick_VideoList()
    {
        m_FocusCoachingDirector.LessonState = ELessonState.List;
        m_FocusCoachingDirector.ResetVideoList(EArraySortMode.View);
    }
}
