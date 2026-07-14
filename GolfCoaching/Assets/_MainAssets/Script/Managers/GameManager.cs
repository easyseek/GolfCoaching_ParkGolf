using System;
using UnityEngine;
using Unity.Burst;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Enums;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine.Networking;
using System.IO;

//[ShowOdinSerializedPropertiesInInspector]
[BurstCompile]


public class GameManager : MonoBehaviourSingleton<GameManager>
{
    [HideInInspector] public EMODE MainMode = EMODE.Practice;
    public ESceneType SceneType = ESceneType.Boot;
    [HideInInspector] public EStep Mode = EStep.None;
    [HideInInspector] public List<SWINGSTEP> Stance = new List<SWINGSTEP>();
    [HideInInspector] public EStance Pose = EStance.None;
    [HideInInspector] public EClub Club = EClub.None;
    [HideInInspector] public ESwingType SwingType = ESwingType.None;
    [HideInInspector] public ERecordingType RecordingType = ERecordingType.None;
    [HideInInspector] public int StudioTabNum = 0;
    [HideInInspector] public int IronNumber = 0;

    private RectTransform m_OptionPanel = null;
    private RectTransform m_OptionPanelArrow = null;

    private RectTransform m_ChoicePanel = null;
    private CanvasGroup m_ChoiceCanvasGroup = null;

    [SerializeField] private GameObject m_SkipObj;

    private Vector2 flipY = new Vector2(0, 180);

    private Vector2 OP_ClosedPos;
    private Vector2 OP_OpenPos;
    private Vector2 CP_OpenPos;

    public float totalRemainTime = 36000;
    public DateTime lastTime;

    public int setWidth;
    public int setHeight;

    public string SelectedSceneName { get; set; } = "";

    private bool isTutorial = false;
    public bool IsTutorial {
        get { return isTutorial; }
        set { isTutorial = value; }
    }

    private bool _isOptionPanelOpen = false;
    private bool _isChoicePanelOpen = false;

    public void SceneManagement()
    {
        SceneManager.LoadScene(sceneName: SelectedSceneName);
    }

    /// <summary>
    /// 시간 계산 업데이트문
    /// </summary>
    //void FixedUpdate()
    //{
    //    {
    //        DateTime currentTime = DateTime.UtcNow.AddHours(9); // 한국 시간으로 현재 시간 업데이트
    //        TimeSpan timeDiff = currentTime - lastTime; // 마지막 업데이트 시간과의 차이 계산

    //        if (totalRemainTime > 0)
    //        {
    //            float useTime = (float)timeDiff.TotalSeconds;
    //            totalRemainTime -= useTime; // 경과된 실제 시간만큼 감소
    //            lastTime = currentTime; // 마지막 업데이트 시간을 현재 시간으로 설정
    //        }
    //    }
    //}

    public void SetOptionPanel()
    {
        m_OptionPanel = null;
        m_OptionPanelArrow = null;

        _isOptionPanelOpen = false;

        m_OptionPanel = GameObject.FindGameObjectWithTag("OptionPanel").GetComponent<RectTransform>();

        if (object.ReferenceEquals(m_OptionPanel, null))
            return;

        RectTransform[] children = m_OptionPanel.GetComponentsInChildren<RectTransform>();

        foreach (RectTransform child in children)
        {
            if (child.name == "OptionArrow")
            {
                m_OptionPanelArrow = child;
                break;
            }
        }

        if (object.ReferenceEquals(m_OptionPanelArrow, null))
            return;

        OP_ClosedPos = m_OptionPanel.anchoredPosition;
        OP_OpenPos = new Vector2(0, m_OptionPanel.anchoredPosition.y);
    }

    public void SetChoicePanel()
    {
        m_ChoicePanel = GameObject.FindGameObjectWithTag("ChoicePanel").GetComponent<RectTransform>();

        if(object.ReferenceEquals(m_ChoicePanel, null))
            return;

        CP_OpenPos = new Vector2(0, m_ChoicePanel.sizeDelta.y);
    }

    public void OnClick_ChoicePanel()
    {
        _isChoicePanelOpen = !_isChoicePanelOpen;

        m_ChoiceCanvasGroup.DOFade(_isChoicePanelOpen ? 1.0f : 0.0f, 0.1f);

        m_ChoicePanel.DOAnchorPos(_isChoicePanelOpen ? CP_OpenPos : Vector2.zero, 0.7f).SetEase(Ease.InOutQuad);
    }

    public void OnClick_OptionPanel()
    {
        _isOptionPanelOpen = !_isOptionPanelOpen;

        m_OptionPanelArrow.DORotate(_isOptionPanelOpen ? Vector2.zero : flipY, 0.7f);

        m_OptionPanel.DOAnchorPos(_isOptionPanelOpen ? OP_OpenPos : OP_ClosedPos, 0.7f).SetEase(Ease.InOutQuad);
    }

    public IEnumerator LoadImageCoroutine(string imageName, Action<Sprite> onLoaded)
    {
        string filePath /*= Path.Combine(Application.dataPath, "DataBase_park", "ProImage", imageName)*/;

#if UNITY_STANDALONE_LINUX
        string homeDir = System.Environment.GetEnvironmentVariable("HOME");
        filePath = Path.Combine(homeDir, $"{imageName}.png");
#else
        filePath = $"{Application.dataPath}/{imageName}.png";
        
        if (!File.Exists(filePath))
        {
            filePath = $"{Application.streamingAssetsPath}/{imageName}.png";
            //filePath = Path.Combine(Application.streamingAssetsPath, "DataBase_park", "ProImage", imagename);
        }

        if(!File.Exists(filePath))
        {
            filePath = @$"C:/{imageName}.png";
        }
#endif   
        string url = "file://" + filePath;

        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (uwr.result != UnityWebRequest.Result.Success)
#else
            if (uwr.isNetworkError || uwr.isHttpError)
#endif
            {
                Debug.LogError("프로필 이미지 로드 실패: " + uwr.error);
                onLoaded?.Invoke(null);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);

                Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

                onLoaded?.Invoke(newSprite);
            }
        }
    }

    public string LoadVideoURL(string videoName)
    {
        string filePath /*= Path.Combine(Application.dataPath, "DataBase_park", "ProVideo", videoName)*/;

        filePath = $"{Application.dataPath}/{videoName}";
#if UNITY_STANDALONE_LINUX
        string homeDir = System.Environment.GetEnvironmentVariable("HOME");
        filePath = Path.Combine(homeDir, videoName);
#else
        if (!File.Exists(filePath))
        {
            filePath = $"{Application.streamingAssetsPath}/{videoName}";
            //filePath = Path.Combine(Application.streamingAssetsPath, "DataBase_park", "ProVideo", videoName);
        }

        if (!File.Exists(filePath))
        {
            filePath = @$"C:/{videoName}";
        }
#endif
        //string url = "file://" + filePath;
        string url = filePath;

        return url;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded: " + scene.name);
        AudioManager.Instance.StopAudio();

        SceneType = Utillity.Instance.StringToEnum<ESceneType>(scene.name);

        switch (scene.name)
        {
            case "PracticeMode":
                m_SkipObj.SetActive(false);
                break;
        }

        Utillity.Instance.HideToast();
        Utillity.Instance.HideGuideArrow();
    }

    // 씬이 언로드되었을 때 호출
    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log("Scene Unloaded: " + scene.name);

        //switch (scene.name)
        //{
        //    case "PracticeMode":
        //        m_SkipObj.SetActive(true);
        //        break;
        //}
    }

    public int GetStanceIndex()
    {
        //return (int)Step.Max();
        return (int)Stance.Max();
    }
}
