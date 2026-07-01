using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Enums;
using System.Collections;
using DG.Tweening;

public class ClubChangeDirector : MonoBehaviour
{
    public TutorialController m_TutorialController;

    [SerializeField] private RectTransform m_ContainerRect;

    [SerializeField] private Button btnConfirm;
    [SerializeField] private ToggleGroup tgClubGroup;
    [SerializeField] CanvasGroup[] m_SlideDetailCG;
    [SerializeField] Toggle[] m_SlideToggles;
    [SerializeField] GameObject m_toggleCover;

    private readonly float clubWidth = 1080f;

    EClub club = EClub.None;

    int selectClubIndex = 0;
    private bool isSliding = false;

    void Start()
    {
        DOTween.Init();

        Init();
    }

    void Init()
    {
        for(int i = 0; i < m_SlideToggles.Length; i++)
        {
            int index = i;
            m_SlideToggles[i].onValueChanged.AddListener((bool isOn) => {
                if(isOn)
                {
                    OnValueChanged_ClubSelect(index, m_SlideToggles[index]);
                }    
            });
        }

        UpdateUI();

        club = EClub.Driver;

        if (m_toggleCover != null)
            m_toggleCover.SetActive(false);

        btnConfirm.interactable = tgClubGroup.AnyTogglesOn();

        if(GameManager.Instance.IsTutorial)
        {
            m_TutorialController.StartTutorial();
        }
    }

    public void SetClubSelect(int index)
    {
        //m_SlideToggles[index].isOn = true;
        OnValueChanged_ClubSelect(index, m_SlideToggles[index]);
    }

    public void OnClick_Back()
    {
        //SceneManager.LoadScene("ModeSelect");
        SceneManager.LoadScene("Pose");
    }

    public void OnClick_Confirm()
    {
        GameManager.Instance.Club = this.club;
        SceneManager.LoadScene(GameManager.Instance.SelectedSceneName);
    }

    public void OnValueChanged_ClubSelect(int index, Toggle clickedToggle)
    {
        if (isSliding)
        {
            clickedToggle.isOn = (index == selectClubIndex);
            return;
        }

        if(index == selectClubIndex)
        {
            clickedToggle.isOn = true;
            return;
        }

        Debug.Log($"[OnValueChanged_ClubSelect]");
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
        isSliding = true;

        if(m_toggleCover != null)
            m_toggleCover.SetActive(true);

        yield return m_SlideDetailCG[selectClubIndex].DOFade(0, 0.1f).WaitForCompletion();

        float targetPosX = -(index) * clubWidth;
        yield return m_ContainerRect.DOAnchorPosX(targetPosX, 0.5f).SetEase(Ease.OutCubic).WaitForCompletion();

        selectClubIndex = index;

        yield return m_SlideDetailCG[selectClubIndex].DOFade(1, 0.1f).WaitForCompletion();

        if (m_toggleCover != null)
            m_toggleCover.SetActive(false);

        isSliding = false;
    }

    private void UpdateUI()
    {
        float posX = -selectClubIndex * clubWidth;
        m_ContainerRect.anchoredPosition = new Vector2(posX, m_ContainerRect.anchoredPosition.y);

        if(m_SlideDetailCG != null)
        {
            for(int i = 0; i < m_SlideDetailCG.Length; i++)
            {
                m_SlideDetailCG[i].alpha = (i == selectClubIndex) ? 1.0f : 0.0f;
            }
        }
    }
}
