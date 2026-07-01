using DG.Tweening;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class TutorialStep
{
    public int num = -1;
    public int canvasNum;
    public string messages = string.Empty;
    public int ballonNum = 0;
    public Vector2 ballonPosition = Vector2.zero;
    public int touchNum = 0;
    public Vector2 touchPosition = Vector2.zero;
    public float displayDuration = 2f;
    public bool ishand = false;
    public bool isCanvasGroup = false;
}

public class TutorialController : MonoBehaviour
{
    public LoginDirector m_LoginDirector;
    public ModeSelectDirector m_ModeSelectDirector;
    public ProPopupDetailPopup m_ProPopupDetailPopup;
    public PoseDirector m_PoseDirector;
    public ClubChangeDirector m_ClubChangeDirector;

    public TutorialStep[] m_TutorialSteps;

    public GameObject[] m_TutorialCG;
    public RectTransform[] wordBallons;
    public Text[] m_BallonText;
    public RectTransform[] m_hands;

    private bool isActive = false; 
    public bool IsActive {  
        get { return isActive; }
        set { isActive = value; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void StartTutorial()
    {
        if (isActive)
            return;

        StartCoroutine(RunTutorial());
    }

    private IEnumerator RunTutorial()
    {
        isActive = true;

        yield return new WaitForSeconds(1.0f);

        for (int i = 0; i < m_TutorialSteps.Length; i++)
        {
            int type = m_TutorialSteps[i].ballonNum;

            if (!string.IsNullOrEmpty(m_TutorialSteps[i].messages))
            {
                wordBallons[type].anchoredPosition = m_TutorialSteps[i].ballonPosition;
                m_BallonText[type].text = m_TutorialSteps[i].messages;
                wordBallons[type].gameObject.SetActive(true);

                if (m_TutorialSteps[i].isCanvasGroup && m_TutorialCG[m_TutorialSteps[i].canvasNum] != null)
                {
                    CanvasGroup[] cg = m_TutorialCG[m_TutorialSteps[i].canvasNum].GetComponentsInChildren<CanvasGroup>();

                    int index = 0;
                    foreach(var item in cg)
                    {
                        index++;
                        item.DOFade(1, 0.2f).SetEase(Ease.InCubic);

                        if(cg.Length != index)
                            yield return new WaitForSeconds(1.0f);
                    }

                    //m_TutorialCG[i].DOFade(1, 0.5f).SetEase(Ease.InCubic);
                }
                    
            }

            if (m_TutorialSteps[i].ishand)
            {
                m_hands[m_TutorialSteps[i].touchNum].anchoredPosition = m_TutorialSteps[i].touchPosition;
                m_hands[m_TutorialSteps[i].touchNum].gameObject.SetActive(true);
            }

            //if (m_TutorialSteps[i].highlightTarget != null && m_FingerImg != null)
            //{
            //    m_FingerImg.gameObject.SetActive(true);
            //    m_FingerImg.position = m_TutorialSteps[i].highlightTarget.position;
            //    Debug.Log($"[RunTutorial] m_FingerImg.position: {m_FingerImg.position}");
            //}
            
            if (!string.IsNullOrEmpty(m_TutorialSteps[i].messages))
            {
                yield return new WaitForSeconds(m_TutorialSteps[i].messages.Length * 0.1f + 1.5f);
            }
            else
                yield return new WaitForSeconds(m_TutorialSteps[i].displayDuration);

            if (m_TutorialSteps[i].ishand)
            {
                m_hands[m_TutorialSteps[i].touchNum].gameObject.SetActive(false);
            }

            if (m_TutorialSteps[i].isCanvasGroup && m_TutorialCG[m_TutorialSteps[i].canvasNum] != null)
            {
                CanvasGroup[] cg = m_TutorialCG[m_TutorialSteps[i].canvasNum].GetComponentsInChildren<CanvasGroup>();

                foreach (var item in cg)
                {
                    item.DOFade(0, 0.5f).SetEase(Ease.OutCubic);
                }

                //m_TutorialCG[i].DOFade(1, 0.5f).SetEase(Ease.InCubic);
            }


            switch (SceneManager.GetActiveScene().name)
            {
                case "Login":
                    if (m_TutorialSteps[i].num == 1)
                    {
                        if (!string.IsNullOrEmpty(m_TutorialSteps[i].messages))
                            wordBallons[type].gameObject.SetActive(false);

                        //if (m_TutorialCG[i] != null)
                        //    m_TutorialCG[i].DOFade(0, 0.5f).SetEase(Ease.OutCubic);

                        m_LoginDirector.LoginSuccess();
                    }
                    break;

                case "ProSelect":
                    if (!string.IsNullOrEmpty(m_TutorialSteps[i].messages))
                        wordBallons[type].gameObject.SetActive(false);

                    if (m_TutorialSteps[i].num == 1)
                    {
                        m_ProPopupDetailPopup.ProButtonClick();

                        yield return new WaitForSeconds(0.5f);
                    }
                    else if (m_TutorialSteps[i].num == 2)
                    {
                        yield return new WaitForSeconds(1.0f);
                        //m_ProPopupDetailPopup.OnValueChanged_Detail(2);
                        
                    }
                    else if (m_TutorialSteps[i].num == 3)
                    {
                        yield return new WaitForSeconds(0.5f);
                        m_ProPopupDetailPopup.OnClick_SelectPro();
                    }
                    break;

                case "ModeSelect":
                    if (!string.IsNullOrEmpty(m_TutorialSteps[type].messages))
                        wordBallons[type].gameObject.SetActive(false);

                    //if (m_TutorialCG[i] != null)
                    //    m_TutorialCG[i].DOFade(0, 0.5f).SetEase(Ease.InCubic);

                    yield return new WaitForSeconds(0.5f);

                    if (m_TutorialSteps[i].num == 1)
                    {
                        yield return new WaitForSeconds(0.5f);
                        m_ModeSelectDirector.OnClick_Practice();
                    }
                    break;

                case "Pose":
                    if (!string.IsNullOrEmpty(m_TutorialSteps[type].messages))
                        wordBallons[type].gameObject.SetActive(false);

                    if (m_TutorialSteps[i].num == 1)
                    {
                        yield return new WaitForSeconds(0.5f);
                        m_PoseDirector.OnValueChanged_Mode(2);
                    }
                    else if (m_TutorialSteps[i].num == 2)
                    {
                        yield return new WaitForSeconds(0.5f);
                        m_PoseDirector.OnClick_Next();
                    }
                    break;

                case "ClubChange":
                        wordBallons[type].gameObject.SetActive(false);

                    if (m_TutorialSteps[i].num == 1)
                    {
                        //m_ClubChangeDirector.SetClubSelect(3);
                        //yield return new WaitForSeconds(1.5f);
                        m_ClubChangeDirector.SetClubSelect(3);
                        yield return new WaitForSeconds(1.0f);
                    }
                    else if (m_TutorialSteps[i].num == 2)
                    {
                        yield return new WaitForSeconds(0.5f);
                        m_ClubChangeDirector.OnClick_Confirm();
                    }
                    break;
            }

            //if (i == m_TutorialSteps.Length - 1)
            //{
            //    if (!string.IsNullOrEmpty(m_TutorialSteps[i].sceneName))
            //        SceneManager.LoadScene(m_TutorialSteps[i].sceneName);
            //}
        }

        isActive = false;
    }
}
