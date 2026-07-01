using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Enums;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using System.Linq;

public class SetupDirector : MonoBehaviour
{
    public TutorialController m_TutorialController;

    private ESetupMode m_ESetupMode = ESetupMode.None;
    public ESetupMode ESetupMode {
        get { return m_ESetupMode; }
        set {
            m_ESetupMode = value;

            switch (m_ESetupMode)
            {
                case ESetupMode.None:
                    //GameManager.Instance.Step.Clear();
                    stance.Clear();
                    club = EClub.None;
                    break;

                case ESetupMode.Pose:
                    m_ClubCanvas.SetActive(false);
                    m_PoseCanvas.SetActive(true);

                    ResetPoseUI();
                    club = EClub.None;
                    break;

                case ESetupMode.Club:
                    m_ClubCanvas.SetActive(true);
                    m_PoseCanvas.SetActive(false);

                    UpdateClubUI();
                    break;

                case ESetupMode.Confirm:
                    GameManager.Instance.Stance = stance;
                    //GameManager.Instance.Club = club;
                    GameManager.Instance.Club = EClub.MiddleIron;
                    GameManager.Instance.IronNumber = 7;
                    GameManager.Instance.Pose = pose;

                    SceneManager.LoadScene(GameManager.Instance.SelectedSceneName);
                    break;
            }
        }
    }

    #region PoseSetup
    [Header("** Pose Setup")]
    [SerializeField] private RectTransform m_PoseContainerRect;

    [SerializeField] private GameObject m_PoseCanvas;
    [SerializeField] private GameObject m_PoseToggleCover;
    [SerializeField] private GameObject m_StepToggleObj;
    [SerializeField] private GameObject m_SwingStepGroup;

    [SerializeField] private Button btnNext;

    [SerializeField] private ToggleGroup tgPose;

    [SerializeField] private ToggleGroup tgAllToggle;

    private List<SWINGSTEP> stance = new List<SWINGSTEP>();

    [SerializeField] Toggle[] m_PoseToggles;
    [SerializeField] Toggle[] m_StepToggles;
    [SerializeField] Toggle m_AllToggles;

    private EStance pose = EStance.None;

    private readonly float poseWidth = 600.0f;

    private int selectPoseIndex = -1;
    private int selectStepIndex = -1;
    #endregion

    [Space]

    #region ClubSetup
    [Header("** Club Setup")]
    [SerializeField] private RectTransform m_ContainerRect;

    [SerializeField] private Button btnConfirm;
    [SerializeField] private ToggleGroup tgClubGroup;
    [SerializeField] CanvasGroup[] m_SlideDetailCG;
    [SerializeField] Toggle[] m_SlideToggles;
    [SerializeField] GameObject m_toggleCover;
    [SerializeField] private GameObject m_ClubCanvas;

    private readonly float clubWidth = 1080f;

    EClub club = EClub.None;

    int selectClubIndex = 0;
    #endregion

    private EStep mode;

    void Start()
    {
        DOTween.Init();

        m_ESetupMode = ESetupMode.Pose;

        m_SwingStepGroup.SetActive(GameManager.Instance.Mode == EStep.Realtime ? false : true);

        InitPose();
        InitStep();
        InitClub();
    }

    void InitPose()
    {
        for (int i = 0; i < m_PoseToggles.Length; i++)
        {
            m_PoseToggles[i].onValueChanged.AddListener(OnValueChanged_Pose);
            //int index = i;
            //int value = m_PoseToggles[index].GetComponent<UIValueObject>().intValue;
            //m_PoseToggles[i].onValueChanged.AddListener((bool isOn) => {
            //    if (isOn)
            //    {
            //        OnValueChanged_Pose(value, m_PoseToggles[index]);
            //    }
            //});
        }

        ResetPoseUI();
    }

    private void InitStep()
    {
        for (int i = 0; i < m_StepToggles.Length; i++)
        {
            m_StepToggles[i].onValueChanged.AddListener(OnValueChanged_Step);
            //int index = i;
            //int value = m_StepToggles[index].GetComponent<UIValueObject>().intValue;
            //m_StepToggles[i].onValueChanged.AddListener((bool isOn) => {
            //    if (isOn)
            //    {
            //        OnValueChanged_Step(value, m_StepToggles[index]);
            //    }
            //});
        }

        //m_AllToggles.onValueChanged.AddListener(OnValueChanged_Step);
        m_AllToggles.onValueChanged.AddListener(OnValueChanged_StepAll);

        //step = EStance.None;
    }

    private void InitClub()
    {
        for (int i = 0; i < m_SlideToggles.Length; i++)
        {
            int index = i;
            m_SlideToggles[i].onValueChanged.AddListener((bool isOn) => {
                if (isOn)
                {
                    OnValueChanged_ClubSelect(index, m_SlideToggles[index]);
                }
            });
        }

        UpdateClubUI();

        club = EClub.Driver;

        if (m_toggleCover != null)
            m_toggleCover.SetActive(false);

        btnConfirm.interactable = tgClubGroup.AnyTogglesOn();

        if (GameManager.Instance.IsTutorial)
        {
            m_TutorialController.StartTutorial();
        }
    }

    #region PoseMethod
    //public void OnValueChanged_Pose(int index, Toggle clickedToggle)
    public void OnValueChanged_Pose(bool isOn)
    {
        Toggle activeToggle = tgPose.ActiveToggles().FirstOrDefault();
        int value = activeToggle != null ? activeToggle.GetComponent<UIValueObject>().intValue : 1;

        EStance c = (EStance)value;

        //if (GameManager.Instance.IsTutorial)
        //    clickedToggle.isOn = true;

        if (value == 1)
            GameManager.Instance.SwingType = ESwingType.None;
        else if (value == 2)
            GameManager.Instance.SwingType = ESwingType.Half;
        else if (value == 3)
            GameManager.Instance.SwingType = ESwingType.ThreeQuarter;
        else if (value == 4)
            GameManager.Instance.SwingType = ESwingType.Full;


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
                m_StepToggleObj.SetActive(GameManager.Instance.Mode == EStep.Preview? true : false);
            }

            StartCoroutine(SwitchPose(value));
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
                    //Debug.Log($"stance({stance.Count}) ADD : {S}");
                    stance.Add(S);
                    stance.Sort();
                    StartCoroutine(SwitchStep(value));
                }

            }
            else
            {
                if (stance.Contains(S) == true)
                {
                    stance.Remove(S);
                    if (stance.Count > 0)
                    {
                        StartCoroutine(SwitchStep((int)stance.Last()));
                    }
                    //Debug.Log($"stance({stance.Count}) DEL : {S}");
                }
            }
        }

        if(stance.Count <= 0)
        {
            tgAllToggle.allowSwitchOff = false;
            m_AllToggles.isOn = true;
        }
        else
        {
            tgAllToggle.allowSwitchOff = true;
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

            StartCoroutine(SwitchPose((int)pose));
        }
    }

    private IEnumerator SwitchPose(int index)
    {
        if (m_PoseToggleCover != null)
            m_PoseToggleCover.SetActive(true);

        float targetPosX;

        if (index == 0)
            targetPosX = 0;
        else
            targetPosX = -(index) * poseWidth;

        yield return m_PoseContainerRect.DOAnchorPosX(targetPosX, 0.2f).SetEase(Ease.OutCubic).WaitForCompletion();

        selectPoseIndex = index;

        if (m_PoseToggleCover != null)
            m_PoseToggleCover.SetActive(false);
    }

    private IEnumerator SwitchStep(int i)
    {
        if (m_PoseToggleCover != null)
            m_PoseToggleCover.SetActive(true);

        float targetPosX;
        int index = i + 4;

        if (index == -1)
        {
            targetPosX = -(selectPoseIndex) * poseWidth;
            index = selectPoseIndex;
        }
        else
            targetPosX = -(index) * poseWidth;

        yield return m_PoseContainerRect.DOAnchorPosX(targetPosX, 0.2f).SetEase(Ease.OutCubic).WaitForCompletion();

        selectStepIndex = index;

        if (m_PoseToggleCover != null)
            m_PoseToggleCover.SetActive(false);
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

    IEnumerator SetStep(EStance pose)
    {
        yield return null;
        stance.Clear();

        switch (pose)
        {
            //case EStance.Grib:

            //    break;

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

    private void SetStepValue(bool active, params EStance[] steps)
    {
        if(steps == null || steps.Length == 0) return;

        foreach(EStance step in steps)
        {
            if(step == EStance.Address || step == EStance.Full)
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

        SwitchPose(1);

        pose = EStance.Address;

        ResetStepUI();

        m_StepToggleObj.SetActive(false);

        btnNext.interactable = tgPose.AnyTogglesOn();
    }

    private void ResetStepUI()
    {
        //if (step.Count > 0)
        if (stance.Count > 0)
        {
            selectStepIndex = -1;

            foreach (var toggle in m_StepToggles)
            {
                toggle.isOn = false;
            }

            //step.Clear();
            stance.Clear();
        }

        pose = EStance.None;

        tgAllToggle.allowSwitchOff = false;
        m_AllToggles.isOn = true;
    }
    #endregion

    #region ClubMethod
    public void SetClubSelect(int index)
    {
        //m_SlideToggles[index].isOn = true;
        OnValueChanged_ClubSelect(index, m_SlideToggles[index]);
    }

    public void OnValueChanged_ClubSelect(int index, Toggle clickedToggle)
    {
        if (index == selectClubIndex)
        {
            clickedToggle.isOn = true;
            return;
        }

        if (GameManager.Instance.IsTutorial)
            clickedToggle.isOn = true;

        btnConfirm.interactable = tgClubGroup.AnyTogglesOn();

        if (tgClubGroup.AnyTogglesOn())
        {
            int value = tgClubGroup.GetFirstActiveToggle().GetComponent<UIValueObject>().intValue;
            EClub c = (EClub)value;

            if (club == EClub.None || club != c)
            {
                StartCoroutine(SwitchClub(value));
                club = c;
            }
            else if (club == c)
            {
                return;
            }
        }
    }

    private IEnumerator SwitchClub(int index)
    {
        if (m_toggleCover != null)
            m_toggleCover.SetActive(true);

        yield return m_SlideDetailCG[selectClubIndex].DOFade(0, 0.1f).WaitForCompletion();

        float targetPosX = -(index) * clubWidth;
        yield return m_ContainerRect.DOAnchorPosX(targetPosX, 0.5f).SetEase(Ease.OutCubic).WaitForCompletion();

        selectClubIndex = index;

        yield return m_SlideDetailCG[selectClubIndex].DOFade(1, 0.1f).WaitForCompletion();

        if (m_toggleCover != null)
            m_toggleCover.SetActive(false);
    }

    private void UpdateClubUI()
    {
        float posX = -selectClubIndex * clubWidth;
        m_ContainerRect.anchoredPosition = new Vector2(posX, m_ContainerRect.anchoredPosition.y);

        if (m_SlideDetailCG != null)
        {
            for (int i = 0; i < m_SlideDetailCG.Length; i++)
            {
                m_SlideDetailCG[i].alpha = (i == selectClubIndex) ? 1.0f : 0.0f;
            }
        }

        btnConfirm.interactable = tgClubGroup.AnyTogglesOn();
    }
    #endregion

    public void OnClick_Back()
    {
        if (m_ESetupMode == ESetupMode.Pose)
        {
            //SceneManager.LoadScene("ModeSelect");
            SceneManager.LoadScene("SetupMode");
        }
        else if(m_ESetupMode == ESetupMode.Club)
        {
            ESetupMode = ESetupMode.Pose;
        }
    }

    public void OnClick_Next()
    {
        if(m_ESetupMode == ESetupMode.Pose)
        {
            //ESetupMode = ESetupMode.Club;
            ESetupMode = ESetupMode.Confirm;
        }
        else if(m_ESetupMode == ESetupMode.Club)
        {
            ESetupMode = ESetupMode.Confirm;
        }

        //SceneManager.LoadScene("ClubChange");
        //GameManager.Instance.SceneManagement();
    }
}
