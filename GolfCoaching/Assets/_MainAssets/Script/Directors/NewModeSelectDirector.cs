using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.ParticleSystem;

public class ModeSelectDirector : MonoBehaviour
{
    public TutorialController m_TutorialController;
    [SerializeField] private ProPopupDetailPopup m_ProDetailPopup;
    [SerializeField] GameObject m_BlackPanel;

    [Header("* PANEL")]
    [SerializeField] GameObject PanelMyRecode;
    [SerializeField] GameObject PanelEvents;
    [SerializeField] GameObject PanelNotice;
    [SerializeField] GameObject PanelSetting;
    [SerializeField] GameObject Popup3DMode;
    
    [Header("* INFO")]
    [SerializeField] Image imgProPhoto;
    [SerializeField] TextMeshProUGUI txtProName;
    [SerializeField] TextMeshProUGUI txtProPosition;


    [Header("* USER")]    
    [SerializeField] TextMeshProUGUI txtUserName;
/*
    [Header("* TIMER")]
    public TextMeshProUGUI txtPlaybackTime; // UI에 표시될 Text 객체    
    public float totalPlaybackTime = 36000f; // 총 재생 시간을 저장할 변수 (10시간으로 초기 설정, 추후 서버에서 받아오기)
*/
    [Header("* RECODE")]
    public TextMeshProUGUI txtPlaybackTimeRecode; // UI에 표시될 Text 객체(Recode)
    public TextMeshProUGUI txtUsageTimeRecode; // UI에 표시될 Text 객체(Recode)

    [Header("* BETA OPTION")]
    [SerializeField] GameObject OffLesson;
    [SerializeField] GameObject OffPractice;
    [SerializeField] GameObject OffAICoaching;
    [SerializeField] GameObject OffMirror;
    [SerializeField] GameObject OffRange;
    [SerializeField] GameObject OffStudio;

    [SerializeField] DevSetting devSetting;

    private int selectProUID = 0;

    private void Awake()
    {
        /*
        OffLesson.SetActive(!Utillity.Instance.lessonUse);
        OffPractice.SetActive(!Utillity.Instance.PracticeUse);
        OffAICoaching.SetActive(!Utillity.Instance.aiCoachingUse);
        OffMirror.SetActive(!Utillity.Instance.mirrorUse);
        OffRange.SetActive(!Utillity.Instance.RangeUse);
        OffStudio.SetActive(Utillity.Instance.studioUse);
        */
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetUserInfo();

        SetProInfo();

        //StartCoroutine(CoLeftTimeProcess());

        if (GameManager.Instance.IsTutorial)
        {
            m_TutorialController.StartTutorial();
        }
        else
        {
            m_BlackPanel.SetActive(false);
        }

        GameManager.Instance.SelectedSceneName = "ModeSelect";

        //txtUserName.text = RestManager.Instance.LoginUserData.UserName + " 님";
        //StartCoroutine(CoClock());
    }

/*
    IEnumerator CoClock()
    {
        while(true)
        {
            txtPlaybackTime.text = DateTime.Now.ToString("hh:mm");
            yield return new WaitForSeconds(0.1f);
        }
    }
*/
    //유저 정보
    void SetUserInfo()
    {
    }

    //프로 정보
    void SetProInfo()
    {
        selectProUID = GolfProDataManager.Instance.SelectProData.uid;

        txtProName.text = $"{GolfProDataManager.Instance.SelectProData.infoData.name} 프로";
        txtProPosition.text = GolfProDataManager.Instance.SelectProData.infoData.info;

        StartCoroutine(GameManager.Instance.LoadImageCoroutine($"{INI.proImagePath}{selectProUID}/{GolfProDataManager.Instance.GetProImageData(selectProUID, Enums.EImageType.Thumbnail).path}", (Sprite sprite) => {
            if (sprite != null)
            {
                imgProPhoto.sprite = sprite;
            }
            else
                Debug.Log("썸네일 Sprite 로드 실패");
        }));
    }

    //남은 시간
    /*
    IEnumerator CoLeftTimeProcess()
    {
        while (GameManager.Instance?.totalRemainTime > 0)
        {
            // 시간 포맷을 시:분:초 형태로 변환 및 업데이트
            int hours = (int)(GameManager.Instance.totalRemainTime / 3600);
            int minutes = (int)(GameManager.Instance.totalRemainTime / 60) % 60;
            int seconds = (int)GameManager.Instance.totalRemainTime % 60;
            txtPlaybackTime.text = string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
            txtPlaybackTimeRecode.text = txtPlaybackTime.text;
            yield return null;
        }
    }
*/
    void SetPanelActive(string PanelName)
    {
        PanelMyRecode.SetActive(PanelMyRecode.name.Equals(PanelName) ? true : false);
        PanelEvents.SetActive(PanelEvents.name.Equals(PanelName) ? true : false);
        PanelNotice.SetActive(PanelNotice.name.Equals(PanelName) ? true : false);
        PanelSetting.SetActive(PanelSetting.name.Equals(PanelName) ? true : false);
    }

    // //////////////////////////////////////////
    // UI Events
    // //////////////////////////////////////////
    public void OnClick_Lesson()
    {
        GameManager.Instance.SelectedSceneName = "LessonMode";
        SceneManager.LoadScene("LessonMode");
    }

    public void OnClick_Practice()
    {
        //GameManager.Instance.SelectedSceneName = "PracticeMode";
        //SceneManager.LoadScene("SetupMode");
        SceneManager.LoadScene("Mirror");
    }

    public void OnClick_AICoaching()
    {
        GameManager.Instance.SelectedSceneName = "AICoaching";

        SceneManager.LoadScene("AICoaching");
    }

    public void OnClick_FocusCoaching()
    {
        GameManager.Instance.SelectedSceneName = "FocusCoaching";

        SceneManager.LoadScene("FocusCoaching");
    }

    public void OnClick_ProChange()
    {
        SceneManager.LoadScene("ProSelect");
    }

    public void OnClick_Mirror()
    {
        SceneManager.LoadScene("Mirror");
    }

    public void OnClick_3DMode()
    {
        //SceneManager.LoadScene("Range");
        Popup3DMode.SetActive(!Popup3DMode.activeInHierarchy);
    }

    public void OnClick_3DVideo()
    {
        SceneManager.LoadScene("PracticeMode_Video");
    }

    public void OnClick_OpenRecode(string PanelName)
    {
#if SPOEX
        return;
#endif
        SetPanelActive(PanelName);
    }

    public void OnClick_OpenEvent(string PanelName)
    {
#if SPOEX
        return;
#endif
        SetPanelActive(PanelName);
    }

    public void OnClick_OpenNotice(string PanelName)
    {
#if SPOEX
        return;
#endif
        SetPanelActive(PanelName);
    }

    public void OnClick_Setting(string PanelName)
    {
#if SPOEX
        return;
#endif
        SetPanelActive(PanelName);
    }

    public void OnClick_DevSstting(bool isShow)
    {
        devSetting.gameObject.SetActive(isShow);
        if (isShow)
            devSetting.LoadValue();
    }

    public void OnClick_Studio()
    {
        GameManager.Instance.SelectedSceneName = "Studio";
        SceneManager.LoadScene("Studio");
        //SceneManager.LoadScene("PraticeMode_AngleCheck");
        //SceneManager.LoadScene("PraticeMode_AngleTest");
    }

    public void OnClick_PanelBack()
    {
        SetPanelActive(string.Empty);
        OnClick_DevSstting(false);
    }

    public void OnClick_DetailPopup()
    {
        m_ProDetailPopup.SetProProfileData(GolfProDataManager.Instance.SelectProData.uid, GolfProDataManager.Instance.SelectProData.infoData);

        m_ProDetailPopup.ProButtonClick();
    }

    public void OnClick_Exit()
    {
        SceneManager.LoadScene("Login");
    }
}
