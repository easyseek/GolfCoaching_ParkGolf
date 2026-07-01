using Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ModeController : MonoBehaviour
{
    [SerializeField] private GameObject m_ModePanel;
    [SerializeField] private GameObject m_StepToggleObj;

    [SerializeField] private ToggleGroup m_TGPose;
    [SerializeField] private ToggleGroup m_TGAllToggle;
    [SerializeField] private ToggleGroup m_ModeTG;

    [SerializeField] Toggle[] m_ModeToggles;
    [SerializeField] Toggle[] m_PoseToggles;
    [SerializeField] Toggle[] m_StepToggles;
    [SerializeField] Toggle m_AllToggles;

    private EStep mode = EStep.Realtime;
    private List<SWINGSTEP> stance = new List<SWINGSTEP>();
    private EStance pose = EStance.None;

    private bool bInit = false;

    private void OnEnable()
    {
        if(bInit)
        {
            StartCoroutine(OnEnableReset());
        }
    }

    private IEnumerator OnEnableReset()
    {
        yield return null;

        m_ModeToggles[0].isOn = true;

        ResetPoseUI();

        SetMode();
    }

    private void SetMode()
    {
        m_ModeToggles[(int)GameManager.Instance.Mode].isOn = true;
        m_PoseToggles[(int)GameManager.Instance.Pose].isOn = true;
    }

    private void Start()
    {
        InitPose();
        InitStep();

        for (int i = 0; i < m_ModeToggles.Length; i++)
        {
            m_ModeToggles[i].onValueChanged.AddListener(OnValueChanged_Mode);
        }

        SetMode();

        bInit = true;
    }

    void InitPose()
    {
        for (int i = 0; i < m_PoseToggles.Length; i++)
        {
            m_PoseToggles[i].onValueChanged.AddListener(OnValueChanged_Pose);
        }
    }

    private void InitStep()
    {
        for (int i = 0; i < m_StepToggles.Length; i++)
        {
            m_StepToggles[i].onValueChanged.AddListener(OnValueChanged_Step);
        }

        m_AllToggles.onValueChanged.AddListener(OnValueChanged_StepAll);
    }

    IEnumerator SetStep(EStance pose)
    {
        yield return null;
        stance.Clear();

        switch (pose)
        {
            case EStance.Address:
                SetStepValue(false, EStance.Address);
                break;

            case EStance.Half:
                SetStepValue(true, EStance.Takeback, EStance.Impact, EStance.Follow);
                break;

            case EStance.ThreeQuarter:
                SetStepValue(true, EStance.Takeback, EStance.Downswing, EStance.Impact, EStance.Follow);
                break;

            case EStance.Full:
                SetStepValue(true, EStance.Full);
                break;
        }

        SetAllStance(pose);
    }

    void SetAllStance(EStance pose)
    {
        stance.Clear();
        switch (pose)
        {
            case EStance.Address:
                stance.Add(SWINGSTEP.ADDRESS);
                break;

            case EStance.Half:
                stance.Add(SWINGSTEP.TAKEBACK);
                stance.Add(SWINGSTEP.IMPACT);
                stance.Add(SWINGSTEP.FOLLOW);
                break;

            case EStance.ThreeQuarter:
                stance.Add(SWINGSTEP.TAKEBACK);
                stance.Add(SWINGSTEP.DOWNSWING);
                stance.Add(SWINGSTEP.IMPACT);
                stance.Add(SWINGSTEP.FOLLOW);
                break;

            case EStance.Full:
                stance.Add(SWINGSTEP.TAKEBACK);
                stance.Add(SWINGSTEP.BACKSWING);
                stance.Add(SWINGSTEP.TOP);
                stance.Add(SWINGSTEP.DOWNSWING);
                stance.Add(SWINGSTEP.IMPACT);
                stance.Add(SWINGSTEP.FOLLOW);
                stance.Add(SWINGSTEP.FINISH);
                break;
        }
    }

    private void SetStepValue(bool active, params EStance[] steps)
    {
        if (steps == null || steps.Length == 0) return;

        foreach (EStance step in steps)
        {
            if (step == EStance.Address || step == EStance.Full)
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

                m_StepToggles[i - 5].GetComponent<ToggleUIAddon>().SetVisualState(active);
                m_StepToggles[i - 5].interactable = active;
            }
        }
    }

    private void ResetPoseUI()
    {
        m_PoseToggles[1].isOn = true;

        pose = EStance.Address;

        ResetStepUI();

        m_StepToggleObj.SetActive(false);
    }

    private void ResetStepUI()
    {
        if (stance.Count > 0)
        {
            foreach (var toggle in m_StepToggles)
            {
                toggle.isOn = false;
            }

            stance.Clear();
        }

        pose = EStance.None;

        m_TGAllToggle.allowSwitchOff = false;
        m_AllToggles.isOn = true;
    }

    public void OnValueChanged_Mode(bool isOn)
    {
        if (m_ModeTG.GetFirstActiveToggle() == null)
            return;

        int num = m_ModeTG.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;

        if (!isOn)
            return;

        if(num == 0)
        {
            mode = EStep.Realtime;
        }
        else if(num == 1)
        {
            mode = EStep.Preview;
        }

        ResetPoseUI();
    }

    public void OnValueChanged_Pose(bool isOn)
    {
        Toggle activeToggle = m_TGPose.ActiveToggles().FirstOrDefault();
        int value = activeToggle != null ? activeToggle.GetComponent<UIValueObject>().intValue : 1;

        EStance c = (EStance)value;

        //if (GameManager.Instance.IsTutorial)
        //    clickedToggle.isOn = true;

        ResetStepUI();

        if (pose == EStance.None || pose != c)
        {
            stance.Clear();
            if (c == EStance.Address)
            {
                m_StepToggleObj.SetActive(false);
            }
            else
            {
                m_StepToggleObj.SetActive(mode == EStep.Preview ? true : false);
            }

            pose = c;

            StartCoroutine(SetStep(pose));
        }
        else if (pose == c)
        {
            return;
        }
    }

    public void OnValueChanged_Step(bool isOn)
    {

        int value = -1;
        SWINGSTEP S = SWINGSTEP.CHECK;

        if (m_AllToggles.isOn)
        {
            stance.Clear();
        }


        for (int i = 0; i < m_StepToggles.Length; i++)
        {
            value = m_StepToggles[i].gameObject.GetComponent<UIValueObject>().intValue;
            S = (SWINGSTEP)value;
            if (m_StepToggles[i].isOn)
            {
                if (stance.Contains(S) == false)
                {
                    stance.Add(S);
                }

            }
            else
            {
                if (stance.Contains(S) == true)
                {
                    stance.Remove((SWINGSTEP)value);
                }
            }
        }

        if (stance.Count <= 0)
        {
            m_TGAllToggle.allowSwitchOff = false;
            m_AllToggles.isOn = true;
        }
        else
        {
            m_TGAllToggle.allowSwitchOff = true;
            m_AllToggles.isOn = false;
        }
    }

    public void OnValueChanged_StepAll(bool isOn)
    {
        if (isOn)
        {
            for (int i = 0; i < m_StepToggles.Length; i++)
            {
                m_StepToggles[i].isOn = false;
            }

            SetAllStance(pose);
        }
    }

    public void OnClick_ModePanel()
    {
        m_ModePanel.SetActive(!m_ModePanel.activeInHierarchy);
    }

    public void OnClick_Apply()
    {
        GameManager.Instance.Mode = mode;
        GameManager.Instance.Pose = pose;
        GameManager.Instance.Stance = stance;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
