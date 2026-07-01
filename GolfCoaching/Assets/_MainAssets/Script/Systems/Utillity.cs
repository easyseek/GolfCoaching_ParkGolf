using System;
using System.Collections.Generic;
using UnityEngine;
using Enums;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using UnityEngine.Video;
using System.IO;

public class Utillity : MonoBehaviourSingleton<Utillity>
{
    //private Tween toastFadeTween;
    //private Tween toastDelayTween;

    [SerializeField] private CanvasGroup toastBackgroundCG;
    [SerializeField] private TextMeshProUGUI m_ToastTxt;
    [SerializeField] private ContentSizeFitter csfBackBox;
    Sequence seqShowToast;

    private string currentToastMsg = string.Empty;
    //string lastGuideArrowID = string.Empty;
    [SerializeField] Color colGoodText = Color.white;
    [SerializeField] Color colBadText = Color.white;

    GameObject lastGuideArrowObject = null;
    [SerializeField] GameObject[] GuideArrowPanels;

    [SerializeField] GameObject LoadingScreen;

    string[] lastGuideArrowHead = { "AD", "TB", "BS", "TP", "DS", "IP", "FL", "FS" };

    private Dictionary<string, string> m_FeedbackDic = null;

    //config.ini
    INIParser iniParser = new INIParser();
    public int frontCameraID = 0;
    public int sideCameraID = 2;
    public int resolution_width = 1280;
    public int resolution_height = 720;

    float frontPixelDistanceBase = 200f;
    float sidePixelDistanceBase = 200f;
    public float frontPixelDistance = 212f;
    public float sidePixelDistance = 207f;
    public float frontPixelDistanceRate = 1f;
    public float sidePixelDistanceRate = 1f;
    public float TwoCamDIsRate = 0;

    public int sideAngleOffset = 0;
    public int mirrorModeTimeout = 0;

    public int addresssHandDis = 18;

    public bool debugUse = true;

    public string mediapipeMode = "GPU";
    public int frameLimit = 30;

    public bool lessonUse = true;
    public bool PracticeUse = true;
    public bool aiCoachingUse = true;
    public bool mirrorUse = true;
    public bool RangeUse = true;
    public bool studioUse = true;

    private bool isToastActive = false;
    private bool isToastHiding = false;

    bool isShowLoading = false;
    int timeLoading = 30;

    public bool isInit = false;

    private void Start()
    {
        LoadFeedbackTable();
        //GetSwingValues();
        GetConfig();

        seqShowToast = DOTween.Sequence();
        seqShowToast.Join(toastBackgroundCG.DOFade(1.0f, 0.5f).From(0)).SetAutoKill(false);

        StartCoroutine(CoLoading());

        isInit = true;
    }

    public bool CheckInternet()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void SetResolution(int width, int height)
    {
        Screen.SetResolution(width, height, fullscreen: true);
    }

    public List<string> CSVSplitData(string t)
    {
        t = t.Replace("\r\n", "\n");
        string[] lineTemp = t.Split("\n"[0]);
        return new List<string>(lineTemp);
    }

    public void ShowToast(string message, bool isGood = false)//, float displayDuration = 0.0f, float fadeDuration = 0.5f)
    {
        //if (isToastActive || currentToastMsg.Equals(message))
        if (currentToastMsg.Equals(message) || string.IsNullOrEmpty(message))
        {
            return;
        }

        m_ToastTxt.color = isGood ? colGoodText : colBadText;

        if (isToastActive)
        {
            StopAllCoroutines();

            currentToastMsg = message;
            m_ToastTxt.text = message;
            if (seqShowToast.IsPlaying())
                seqShowToast.Pause();
            toastBackgroundCG.alpha = 1;
        }
        else
        {
            currentToastMsg = message;
            m_ToastTxt.text = message;
            seqShowToast.Restart();
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)csfBackBox.transform);
        isToastActive = true;
    }

    public void HideToast(bool quickHide = false)//float fadeDuration = 0.5f, bool quickHide = false, string message = null)
    {
        if (isToastActive == false)
            return;

        StartCoroutine(CoHideToast(quickHide));
        /*
        currentToastMsg = string.Empty;
        m_ToastTxt.text = string.Empty;
        if (seqShowToast.IsPlaying())
            seqShowToast.Pause();
        toastBackgroundCG.alpha = 0;
        isToastActive = false;
        */
    }

    
    IEnumerator CoHideToast(bool quickHide)
    {
        while(quickHide == false && toastBackgroundCG.alpha > 0)
        {
            toastBackgroundCG.alpha -= 1.5f * Time.deltaTime;

            yield return null;
        }

        currentToastMsg = string.Empty;
        m_ToastTxt.text = string.Empty;
        if (seqShowToast.IsPlaying())
            seqShowToast.Pause();
        toastBackgroundCG.alpha = 0;
        isToastActive = false;
    }

    public float CalculateVectorAngle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        //float radians = 0.0f;
        //float angle = 0.0f;

        //radians = MathF.Atan2(c[1] - b[1], c[0] - b[0]) - MathF.Atan2(a[1] - b[1], a[0] - b[0]);
        //angle = MathF.Abs(radians * 180.0f / Mathf.PI);

        //if (angle > 180.0f)
        //    angle = 360 - angle;

        Vector3 vec1 = v2 - v1;
        Vector3 vec2 = v3 - v2;

        float dot = Vector3.Dot(vec1.normalized, vec2.normalized);

        float angleRad = Mathf.Acos(Mathf.Clamp(dot, -1.0f, 1.0f));

        return angleRad * Mathf.Rad2Deg;
    }

    public float CalculateVectorAngle(Vector2 v1, Vector2 v2, Vector2 v3)
    {
        try
        {
            Vector2 vec1 = v1 - v2;
            Vector3 vec2 = v3 - v2;

            return Vector2.Angle(vec1.normalized, vec2.normalized);
        }
        catch { return -1f; }
    }

    public Vector3 GetCenter(Vector3 pointA, Vector3 pointB)
    {
        return (pointA + pointB) / 2;
    }

    public float GetAngle(Vector3 from, Vector3 to, EDirection dir)
    {
        Vector3 direction = to - from;
        direction.Normalize();

        Vector3 dirVector = Vector3.zero;

        if (dir == EDirection.Up)
            dirVector = Vector3.up;
        else if (dir == EDirection.Down)
            dirVector = Vector3.down;
        else if (dir == EDirection.Left)
            dirVector = Vector3.left;
        else if (dir == EDirection.Right)
            dirVector = Vector3.right;

        float dotProduct = Vector3.Dot(direction, dirVector);
        float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

        return angle;
    }

    public float GetPercentage(float value, float min, float max, float mid, bool bPerfect = false)
    {
        if (bPerfect)
            return 100.0f;

        //Debug.Log($"[GetPercentage] value : {value}");

        if (value <= min) return 0f;
        if (value >= max) return 0f;

        if (value < mid)
        {
            // 아래쪽
            return (value - min) / (mid - min) * 100f;
        }
        else
        {
            // 위쪽
            return (max - value) / (max - mid) * 100f;
        }
    }

    public void ShowGuideArrow(string guideID)
    {
        if (lastGuideArrowObject != null)
            return;

        try
        {
            for (int i = 0; i < lastGuideArrowHead.Length; i++)
            {
                if(lastGuideArrowHead[i].Contains(guideID.Substring(0,2)))
                {
                    lastGuideArrowObject = GuideArrowPanels[i].transform.Find(guideID).gameObject;
                    lastGuideArrowObject.SetActive(true);
                    //lastGuideArrowID = guideID;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            HideGuideArrow();
        }
    }

    public void HideGuideArrow()
    {
        if (lastGuideArrowObject != null)
        {
            lastGuideArrowObject.SetActive(false);
            lastGuideArrowObject = null;
            //lastGuideArrowID = string.Empty;
        }        
    }

    public Vector3 CalculateDirection(Vector3 from, Vector3 to)
    {
        return (to - from).normalized;
    }

    public bool LoadFeedbackTable()
    {
        Debug.Log($"LoadFeedbackTable");
        if (m_FeedbackDic == null)
            m_FeedbackDic = new Dictionary<string, string>();
        else
            m_FeedbackDic.Clear();

        var _list = CSVReader.ReadCSV("TableFeedback");

        if (_list == null || _list.Count == 0)
        {
            return false;
        }

        foreach (var item in _list)
        {

            string sID = item["sID"].ToString();
            string sFeedback = item["sFeedback"].ToString();

            //Debug.Log($"sID : {sID}, sFeedback : {sFeedback}");

            m_FeedbackDic.Add(sID, sFeedback);
        }

        return true;
    }

    public string GetFeedbackData(string id)
    {
        string temp;
        this.m_FeedbackDic.TryGetValue(id, out temp);
        return temp;
    }

    public Dictionary<string, string> GetFeedbackDic()
    {
        return m_FeedbackDic;
    }

    public void DelayFunction(Action act, float sec)
    {
        StartCoroutine(CoDelayFunction(act, sec));
    }

    IEnumerator CoDelayFunction(Action act, float sec)
    {
        yield return new WaitForSeconds(sec);

        act.Invoke();
    }

    public void GetConfig()
    {
        string iniPath = "";
        
        Debug.Log("Graphics API: " + SystemInfo.graphicsDeviceType);
        Debug.Log("Graphics Version: " + SystemInfo.graphicsDeviceVersion);

//#if UNITY_EDITOR
//        iniParser.Open(Application.streamingAssetsPath + "\\config.ini");
#if UNITY_EDITOR_LINUX
        iniPath = Path.Combine(Application.streamingAssetsPath,"config.ini");
#elif UNITY_STANDALONE_LINUX
        iniPath = Path.Combine(Application.dataPath,"../config.ini");
#else
        iniPath = Application.dataPath + @"\..\config.ini";
#endif
        bool existsIniFile = File.Exists(iniPath);
        iniParser.Open(iniPath);

        frontCameraID = (int)iniParser.ReadValue("CAMERA", "front_camera_id", 0);
        sideCameraID = (int)iniParser.ReadValue("CAMERA", "side_camera_id", 2);

        resolution_width = (int)iniParser.ReadValue("CAMERA", "resolution_width", 1280);
        resolution_height = (int)iniParser.ReadValue("CAMERA", "resolution_height", 720);

        frontPixelDistanceBase = (float)iniParser.ReadValue("READONLY", "front_pixel_distance_base", 200f);
        sidePixelDistanceBase = (float)iniParser.ReadValue("READONLY", "side_pixel_distance_base", 200f);
        frontPixelDistance = (float)iniParser.ReadValue("CAMERA", "front_pixel_distance", 212f);
        sidePixelDistance = (float)iniParser.ReadValue("CAMERA", "side_pixel_distance", 207f);
        CalDIstanceRate();

        sideAngleOffset = (int)iniParser.ReadValue("CAMERA", "side_angle_offset", 0);

        mirrorModeTimeout = (int)iniParser.ReadValue("KIOSK", "mirrormode_timeout", 30);
        debugUse = (bool)iniParser.ReadValue("KIOSK", "debugmode_enable", false);
        mediapipeMode = (string)iniParser.ReadValue("KIOSK", "mediapipe_Delegate_type", "GPU");
        frameLimit = (int)iniParser.ReadValue("KIOSK", "frame_limit", 30);

        lessonUse = (bool)iniParser.ReadValue("KIOSK", "home_lesson_enable", false);
        PracticeUse = (bool)iniParser.ReadValue("KIOSK", "home_practice_enable", false);
        aiCoachingUse = (bool)iniParser.ReadValue("KIOSK", "home_aicoaching_enable", false);
        mirrorUse = (bool)iniParser.ReadValue("KIOSK", "home_mirror_enable", false);
        RangeUse = (bool)iniParser.ReadValue("KIOSK", "home_range_enable", false);
        studioUse = (bool)iniParser.ReadValue("KIOSK", "home_studio_enable", false);

        addresssHandDis = (int)iniParser.ReadValue("SWINGCHECK", "address_hand_distance", 18);

        iniParser.Close();

        if(existsIniFile == false)
            SetConfig(true);
    }

    public void SetConfig(bool isMake = false)
    {
//#if UNITY_EDITOR
//        iniParser.Open(Application.streamingAssetsPath + "\\config.ini");
#if UNITY_EDITOR_LINUX
        iniParser.Open(Path.Combine(Application.streamingAssetsPath,"config.ini"));
#elif UNITY_STANDALONE_LINUX
        iniParser.Open(Path.Combine(Application.dataPath,"../config.ini"));
#else
        iniParser.Open(Application.dataPath + @"\..\config.ini");
#endif

        if(isMake)
        {
            iniParser.WriteValue("READONLY", "front_pixel_distance_base", 200);
            iniParser.WriteValue("READONLY", "side_pixel_distance_base", 200);    
        }

        iniParser.WriteValue("CAMERA", "front_camera_id", frontCameraID);
        iniParser.WriteValue("CAMERA", "side_camera_id", sideCameraID);

        if(isMake)
        {
            iniParser.WriteValue("CAMERA", "resolution_width", 1280);
            iniParser.WriteValue("CAMERA", "resolution_height", 720);    
        }
        
        iniParser.WriteValue("CAMERA", "front_pixel_distance", frontPixelDistance);
        iniParser.WriteValue("CAMERA", "side_pixel_distance", sidePixelDistance);
        CalDIstanceRate();

        iniParser.WriteValue("CAMERA", "side_angle_offset", sideAngleOffset);

        iniParser.WriteValue("KIOSK", "mirrormode_timeout", mirrorModeTimeout);
        iniParser.WriteValue("KIOSK", "debugmode_enable", debugUse);
        
        if(isMake)
        {
            iniParser.WriteValue("KIOSK", "mediapipe_Delegate_type", "GPU");
            iniParser.WriteValue("KIOSK", "frame_limit", 30);    
        }

        iniParser.WriteValue("KIOSK", "home_lesson_enable", lessonUse);
        iniParser.WriteValue("KIOSK", "home_practice_enable", PracticeUse);
        iniParser.WriteValue("KIOSK", "home_aicoaching_enable", aiCoachingUse);
        iniParser.WriteValue("KIOSK", "home_mirror_enable", mirrorUse);
        iniParser.WriteValue("KIOSK", "home_range_enable", RangeUse);
        iniParser.WriteValue("KIOSK", "home_studio_enable", studioUse);

        iniParser.WriteValue("SWINGCHECK", "address_hand_distance", addresssHandDis);

        iniParser.Close();
    }

    public void CalDIstanceRate()
    {
        

        frontPixelDistanceRate = frontPixelDistanceBase / frontPixelDistance;
        sidePixelDistanceRate = sidePixelDistanceBase / sidePixelDistance;
        TwoCamDIsRate = frontPixelDistance / sidePixelDistance;
    }

    public float CalculatePoseAverage(params float[] scores)
    {
        if (scores == null || scores.Length == 0)
            return 0f;

        float sum = 0f;
        foreach (float score in scores)
        {
            sum += Mathf.Abs(score);
        }
        return sum / scores.Length;
    }

    public T StringToEnum<T>(string e)
    {
        return (T)Enum.Parse(typeof(T), e);
    }

    public List<ProVideoData> LoadVideoUrlList(int uid, EVideoType videoType, EPoseDirection direction)
    {
        List<ProVideoData> filteredList = new List<ProVideoData>();

        if(uid == 0)
        {
            filteredList = GolfProDataManager.Instance.GetProVideoDic()
                .SelectMany(kvp => kvp.Value)
                .Where(v => (direction == EPoseDirection.All || v.direction == direction) && v.videoType == videoType)
                .ToList();
        }
        else
        {
            if (GolfProDataManager.Instance.GetProVideoDic().TryGetValue(uid, out var videos))
            {
                filteredList = videos.Where(v => (direction == EPoseDirection.All || v.direction == direction) && v.videoType == videoType).ToList();

                //GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{path}");
            }
        }

        return filteredList;
    }

    public Dictionary<int, List<ProVideoData>> LoadVideoUrlDic(int uid, EVideoType videoType, EPoseDirection direction)
    {
        Dictionary<int, List<ProVideoData>> filteredDic = new Dictionary<int, List<ProVideoData>>();

        if (uid == 0)
        {
            filteredDic.Add(uid, GolfProDataManager.Instance.GetProVideoDic()
                .SelectMany(kvp => kvp.Value)
                .Where(v => (direction == EPoseDirection.All || v.direction == direction) && v.videoType == videoType)
                .ToList());
        }
        else
        {
            if (GolfProDataManager.Instance.GetProVideoDic().TryGetValue(uid, out var videos))
            {
                filteredDic.Add(uid, videos.Where(v => (direction == EPoseDirection.All || v.direction == direction) && v.videoType == videoType).ToList());

                //GameManager.Instance.LoadVideoURL($"{INI.proVideoPath}{uid}/{path}");
            }
        }

        return filteredDic;
    }

    public string ConvertEnumToString(Enum filter)
    {
        if (filter is EFilter)
        {
            switch (filter)
            {
                case EFilter.Basic: return "기본기 중심";
                case EFilter.Swing: return "스윙분석";
                case EFilter.Range: return "비거리 향상";
                case EFilter.Short: return "숏게임";
                case EFilter.Female: return "여성 맞춤 골퍼";
                case EFilter.Body: return "체형맞춤";
                case EFilter.Real: return "실전 연습";
                case EFilter.Front: return "정면 자세 교정";
                case EFilter.Repeat: return "반복 훈련 중심";
                case EFilter.Club: return "클럽별 맞춤 지도";
                default: return string.Empty;
            }
        }
        else if (filter is EClub)
        {
            switch (filter)
            {
                case EClub.Driver: return "드라이버";
                case EClub.Wood: return "우드";
                case EClub.LongIron: return "롱 아이언";
                case EClub.MiddleIron: return "미들 아이언";
                case EClub.ShortIron: return "숏 아이언";
                case EClub.Approach: return "어프로치";
                case EClub.Putter: return "퍼터";
                default: return string.Empty;
            }
        }
        else if (filter is EStance)
        {
            switch (filter)
            {
                case EStance.Half: return "하프 스윙";
                case EStance.ThreeQuarter: return "쓰리쿼터 스윙";
                case EStance.Full: return "풀 스윙";
                case EStance.Grib: return "그립";
                case EStance.Address: return "어드레스";
                case EStance.Takeback: return "테이크백";
                case EStance.Backswing: return "백스윙";
                case EStance.Top: return "탑";
                case EStance.Downswing: return "다운스윙";
                case EStance.Impact: return "임팩트";
                case EStance.Follow: return "팔로우";
                case EStance.Finish: return "피니쉬";
                default: return string.Empty;
            }
        }
        else if (filter is EArraySortMode)
        {
            switch (filter)
            {
                case EArraySortMode.View: return "조회수순";
                case EArraySortMode.Recently: return "최신순";
                case EArraySortMode.Favorite: return "즐겨찾기 많은 순";
                case EArraySortMode.ManyVideo: return "영상 많은 순";
                default: return string.Empty;
            }
        }
        else if (filter is ESceneType)
        {
            switch (filter)
            {
                case ESceneType.LessonMode: return "레슨";
                case ESceneType.PracticeMode: return "연습";
                case ESceneType.AICoaching: return "AI스윙진단";
                case ESceneType.Mirror: return "거울";
                case ESceneType.Range: return "거리측정";
                default: return string.Empty;
            }
        }
        else if (filter is SWINGSTEP)
        {
            switch (filter)
            {
                case SWINGSTEP.ADDRESS: return "어드레스";
                case SWINGSTEP.TAKEBACK: return "테이크백";
                case SWINGSTEP.BACKSWING: return "백스윙";
                case SWINGSTEP.TOP: return "탑";
                case SWINGSTEP.DOWNSWING: return "다운스윙";
                case SWINGSTEP.IMPACT: return "임팩트";
                case SWINGSTEP.FOLLOW: return "팔로우";
                case SWINGSTEP.FINISH: return "피니쉬";
                default: return string.Empty;
            }
        }
        else
            return string.Empty;
    }

    public string GetVideoTime(VideoPlayer vp)
    {
        double totalSeconds = vp.length;

        int min = (int)(totalSeconds / 60.0f);
        int sec = (int)(totalSeconds % 60.0f);

        return $"{min:D2}:{sec:D2}";
    }

    public int GetPopularityRank(int uid)
    {
        var proList = GolfProDataManager.Instance.GetProInfoList();

        if (!proList.ContainsKey(uid))
            return -1;

        var rankedList = proList.OrderByDescending(p => p.Value.views)
            .Select((p, index) => new { p.Key, Rank = index + 1 })
            .ToList();

        var entry = rankedList.FirstOrDefault(x => x.Key == uid);

        return entry != null ? entry.Rank : -1;
    }

    public string FormatViewsCount(long count)
    {
        if (count < 1_000)
        {
            return $"{count}";
        }
        else if (count < 10_000)
        {
            return $"{(count / 1_000f):F1}천";
        }
        else if (count < 100_000)
        {
            return $"{(count / 10_000f):F1}만";
        }
        else if (count < 1_000_000)
        {
            return $"{(count / 10_000f):F1}만";
        }
        else if (count < 10_000_000)
        {
            return $"{(count / 10_000f):F1}만";
        }
        else if (count < 100_000_000)
        {
            return $"{(count / 10_000_000f):F1}천만";
        }
        else
        {
            return $"{(count / 100_000_000f):F1}억";
        }
    }

    public List<SWINGSTEP> PoseToSWINGSTEP(EStance pose)
    {
        List<SWINGSTEP> list = new List<SWINGSTEP>(); ;

        switch (pose)
        {
            case EStance.Grib:
            case EStance.Address:
                list.Add(SWINGSTEP.ADDRESS);
                break;
            case EStance.Half:
                list.AddRange(new List<SWINGSTEP> { SWINGSTEP.TAKEBACK, SWINGSTEP.IMPACT, SWINGSTEP.FOLLOW });
                break;

            case EStance.ThreeQuarter:
                list.AddRange(new List<SWINGSTEP> { SWINGSTEP.TAKEBACK, SWINGSTEP.DOWNSWING, SWINGSTEP.IMPACT, SWINGSTEP.FOLLOW });
                break;

            case EStance.Full:
                list.AddRange(new List<SWINGSTEP> { SWINGSTEP.TAKEBACK, SWINGSTEP.BACKSWING, SWINGSTEP.TOP, SWINGSTEP.DOWNSWING, SWINGSTEP.IMPACT, SWINGSTEP.FOLLOW, SWINGSTEP.FINISH });
                break;

            case EStance.Takeback:
            case EStance.Backswing:
            case EStance.Top:
            case EStance.Downswing:
            case EStance.Impact:
            case EStance.Follow:
            case EStance.Finish:
                list.Add((SWINGSTEP)(pose - 4));
                break;
        }

        return list;
    }

    public DateTime StringToDateTime(string date)
    {
        if (DateTime.TryParseExact(date, "o", null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime result))
            return result;

        return DateTime.MinValue;
    }

    public Color HexToRGB(string hex)
    {
        Color color = Color.white;

        ColorUtility.TryParseHtmlString(hex, out color);

        return color;
    }

    public void OnClick_Jump()
    {
        GameManager.Instance.Mode = EStep.Preview;
        //GameManager.Instance.Stance.Add(EStance.EFollow);

        SceneManager.LoadScene("PracticeMode");
    }


    public void ShowLoading()
    {
        isShowLoading = true;
        timeLoading = 30;
        LoadingScreen.SetActive(true);
    }

    public void CloseLoading()
    {
        isShowLoading = false;
        timeLoading = 0;
        LoadingScreen.SetActive(false);
    }

    IEnumerator CoLoading()
    {
        while(true)
        {
            yield return new WaitForSeconds(1);
            if(timeLoading > 0)
                timeLoading--;

            if(isShowLoading && timeLoading <= 0)
            {
                CloseLoading();
            }
        }
    }
}
