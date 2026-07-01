using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Enums;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System.Linq;

public class PoseDirector : MonoBehaviour
{
    public TutorialController m_TutorialController;

    [SerializeField] private RectTransform m_PoseContainerRect;

    [SerializeField] private GameObject[] m_modeToggleObj;

    [SerializeField] private TextMeshProUGUI m_BegginerLabel = null;
    [SerializeField] private TextMeshProUGUI m_ExpertLabel = null;

    [SerializeField] private Button btnNext;
    [SerializeField] private Transform objPoseRoot;
    [SerializeField] private Image[] imgPoses;
    [SerializeField] private ToggleGroup tgMode;
    [SerializeField] private ToggleGroup tgBeginner;
    [SerializeField] private ToggleGroup tgSwing;
    private Color colDisable = Color.gray;

    private EStep mode = EStep.None;
    private List<EStance> stance = new List<EStance>();
    //private EStance stance = EStance.None;

    private List<Toggle> tgSwingList = new List<Toggle>();
    private List<Toggle> tgBeginnerList = new List<Toggle>();

    private readonly float poseWidth = 800.0f;

    private int curPoseIndex = 0;
    private bool isSliding = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitPose();
    }

    void InitPose()
    {
        colDisable.a = 0.5f;

        for (int i = 0; i < tgSwing.transform.GetComponentsInChildren<Toggle>().Length; i++)
        {
            tgSwing.transform.GetComponentsInChildren<Toggle>()[i].onValueChanged.AddListener(OnValueChanged_ExpertSwing);            
            tgSwingList.Add(tgSwing.transform.GetComponentsInChildren<Toggle>()[i]);
        }
        //tgSwing.enabled = false;

        for (int i = 0; i < tgBeginner.transform.GetComponentsInChildren<Toggle>().Length; i++)
        {
            tgBeginner.transform.GetComponentsInChildren<Toggle>()[i].onValueChanged.AddListener(OnValueChanged_BeginnerSwing);
            tgBeginnerList.Add(tgBeginner.transform.GetComponentsInChildren<Toggle>()[i]);
        }

        mode = EStep.Realtime;

        Color color;

        ColorUtility.TryParseHtmlString(INI.Green500, out color);
        m_BegginerLabel.color = color;
        ColorUtility.TryParseHtmlString(INI.Grey500, out color);
        m_ExpertLabel.color = color;

        m_modeToggleObj[0].SetActive(true);
        m_modeToggleObj[1].SetActive(false);

        CheckNext();

        if (GameManager.Instance.IsTutorial)
        {
            m_TutorialController.StartTutorial();
        }

    }

    void SetPose()
    {
        //GameManager.Instance.Stance = stance;
        GameManager.Instance.Mode = mode;
    }
    
    void CheckNext()
    {
        if (tgBeginner.AnyTogglesOn() || tgSwing.AnyTogglesOn())
            btnNext.interactable = true;
        else
            btnNext.interactable = false;
    }

    // //////////////////////////////////////////
    // UI Events
    // //////////////////////////////////////////
    public void OnClick_Back()
    {
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnClick_Next()
    {
        SetPose();
        SceneManager.LoadScene("ClubChange");
        //GameManager.Instance.SceneManagement();
    }

    public void OnValueChanged_BeginnerSwing(bool isOn)
    {
        if (tgBeginner.GetFirstActiveToggle() == null) return;
        
        int value = tgBeginner.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;
        EStance s = (EStance)value;
        
        if (stance.Count == 0 || stance[0] != s)
        {
            stance.Clear();
            StartCoroutine(SwitchPose(value));
            stance.Add(s);
        }
        else if (stance[0] == s)
        {
            return;
        }

        /*
        if (stance == EStance.None || stance != s)
        {
            StartCoroutine(SwitchPose(value));
            stance = s;
        }
        else if (stance == s)
        {
            return;
        }
        */

        CheckNext();
    }

    public void OnValueChanged_ExpertSwing(bool isOn)
    {
        int value = -1;
        EStance S = EStance.None;
        for (int i = 0; i < tgSwingList.Count; i++)
        {
            value = tgSwingList[i].gameObject.GetComponent<UIValueObject>().intValue;
            S = (EStance)value;
            if (tgSwingList[i].isOn)
            {
                if(stance.Contains(S) == false)
                {
                    Debug.Log($"stance({stance.Count}) ADD : {S}");
                    stance.Add(S);
                    StartCoroutine(SwitchPose(value));
                }
            }
            else 
            {
                if (stance.Contains(S) == true)
                {
                    stance.Remove((EStance)value);
                    if(stance.Count > 0)
                    {
                        StartCoroutine(SwitchPose((int)stance.Max()));
                    }
                    Debug.Log($"stance({stance.Count}) DEL : {S}");
                }
            }
        }


        CheckNext();
    }

    public void OnValueChanged_Mode(int value)
    {
        if (tgMode.GetFirstActiveToggle() == null) return;

        EStep m = (EStep)tgMode.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (GameManager.Instance.IsTutorial)
        {
            m = (EStep)value;
        }

        if(mode == EStep.None)
        {
            mode = m;
        }
        else if (mode != m)
        {
            mode = m;

            ResetToggle();
        }
        else if (mode == m)
        {
            return;
        }

        Color color;

        if (mode == EStep.Realtime)
        {
            ColorUtility.TryParseHtmlString(INI.Green500, out color);
            m_BegginerLabel.color = color;
            ColorUtility.TryParseHtmlString(INI.Grey500, out color);
            m_ExpertLabel.color = color;

            m_modeToggleObj[0].SetActive(true);
            m_modeToggleObj[1].SetActive(false);
        }
        else if(mode == EStep.Preview)
        {
            ColorUtility.TryParseHtmlString(INI.Grey500, out color);
            m_BegginerLabel.color = color;
            ColorUtility.TryParseHtmlString(INI.Green500, out color);
            m_ExpertLabel.color = color;

            m_modeToggleObj[0].SetActive(false);
            m_modeToggleObj[1].SetActive(true);
        }
    }

    /*
    public void OnValueChanged_Beginner(bool isOn)
    {
        if (tgBeginner.GetFirstActiveToggle() == null) return;

        EStance s = (EStance)tgBeginner.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (stance == EStance.None || stance != s)
        {
            stance = s;
        }
        else if (stance == s)
        {
            return;
        }

        CheckNext();
    }
    */

    private IEnumerator SwitchPose(int index)
    {
        isSliding = true;

        //if (m_toggleCover != null)
        //    m_toggleCover.SetActive(true);

        float targetPosX;
        
        if(index == 0)
            targetPosX = 0;
        else
            targetPosX = -(index - 5) * poseWidth;

        Debug.Log($"[SwitchPose] targetPosX : {targetPosX}");
        yield return m_PoseContainerRect.DOAnchorPosX(targetPosX, 0.5f).SetEase(Ease.OutCubic).WaitForCompletion();

        curPoseIndex = index - 5;

        //if (m_toggleCover != null)
        //    m_toggleCover.SetActive(false);

        isSliding = false;
    }

    public void ResetToggle()
    {
        //tgBeginner.allowSwitchOff = true;
        //tgSwing.allowSwitchOff = true;
        //tgLevel.allowSwitchOff = true;

        if(mode == EStep.Realtime)
        {
            foreach (var toggle in tgSwingList)
            {
                toggle.isOn = false;
            }

            for (int i = 0; i < tgBeginnerList.Count; i++)
            {
                if (i == 0)
                {
                    //stance = (EStance)tgBeginnerList[i].GetComponent<UIValueObject>().intValue;
                    tgBeginnerList[i].isOn = true;
                }
                else
                    tgBeginnerList[i].isOn = false;
            }
        }
        else
        {
            foreach (var toggle in tgBeginnerList)
            {
                //Debug.Log($"beginner toggle name : {toggle.name}");
                toggle.isOn = false;
            }

            for (int i = 0; i < tgSwingList.Count; i++)
            {
                if (i == 0)
                {
                    //stance = (EStance)tgSwingList[i].GetComponent<UIValueObject>().intValue;
                    m_PoseContainerRect.anchoredPosition = new Vector2(0, m_PoseContainerRect.anchoredPosition.y);
                    tgSwingList[i].isOn = true;
                }
                else
                    tgSwingList[i].isOn = false;
            }
        }

        //btnNext.interactable = false;
        CheckNext();
    }
}
