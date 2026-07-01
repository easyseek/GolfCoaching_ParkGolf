using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using Enums;
using System.Collections;
using UnityEngine.UI.Extensions;
using System.Linq;

public class PracticeModeController : MonoBehaviour
{
    [SerializeField] private ModelController m_ModelController;

    [SerializeField] private RectTransform m_SwingStepContainer;

    [SerializeField] private Image m_ClubImg;
    [SerializeField] private Sprite[] m_ClubSprites;
    [SerializeField] private TextMeshProUGUI m_ClubNameText;

    [SerializeField] RawImage RawImage_Top;
    [SerializeField] RawImage RawImage_Bottom;
    [SerializeField] RenderTexture RenderTexture_Front;
    [SerializeField] RenderTexture RenderTexture_Side;

    [SerializeField] GameObject m_SkeletonCover = null;
    [SerializeField] GameObject m_FrontLines = null;
    [SerializeField] GameObject m_SideLines = null;

    [SerializeField] GameObject m_FrontPhotoImage = null;
    [SerializeField] GameObject m_SidePhotoImage = null;

    [Header("[ Score Panel ]")]
    [SerializeField] private GameObject m_PreviewQuarterObj;
    [SerializeField] private RectTransform m_ScorePanel;
    [SerializeField] private RectTransform m_ProQuarter;
    [SerializeField] private RectTransform m_PreviewQuarter;

    [SerializeField] private Image m_SwingAccImg;
    [SerializeField] private Image m_SwingAccPreviewImg;

    [SerializeField] private TextMeshProUGUI m_PercentageTxt;
    [SerializeField] private TextMeshProUGUI m_PercentagePreviewTxt;
    [SerializeField] private TextMeshProUGUI[] m_ScoreTxt;

    [Header("[ Feedback Panel ]")]
    [SerializeField] private RectTransform m_FeedbackPanel;
    [SerializeField] private RectTransform[] dotPoints;

    [SerializeField] private UILineRenderer dotLineRenderer;

    [SerializeField] private TextMeshProUGUI[] dotValueTxts;
    [SerializeField] private TextMeshProUGUI[] timesTxt;
    [SerializeField] private TextMeshProUGUI changeTxt;
    [SerializeField] private TextMeshProUGUI avgScoreTxt;
    [SerializeField] private TextMeshProUGUI tipTitleTxt;
    //[SerializeField] private TextMeshProUGUI tipDescTxt;

    [Header("[ AI Swing Panel ]")]
    [SerializeField] private RectTransform m_AISwingPanel;

    [Header("[ Pro Preview Panel ]")]
    [SerializeField] private RectTransform m_ProPreviewPanel;
    [SerializeField] private RectTransform m_ProPreviewPanelArrow;

    [SerializeField] private Animator m_AniPreview;

    private Tween m_rotationTween;

    private Queue<int> feedbackDataQueue = new Queue<int>();

    private List<int> avgScoreList = new List<int>();
    private Dictionary<SWINGSTEP, int> stepScoreDic = new Dictionary<SWINGSTEP, int>();

    private Vector3 proQuarterOriPos, proQuarterOriRot;
    private Vector3 previewQuarterOriPos, previewQuarterOriRot;

    private Vector2 flipY = new Vector3(0, 180);

    private Vector2 SP_ClosedPos;
    private Vector2 SP_OpenPos;
    private Vector2 FP_ClosedPos;
    private Vector2 AIP_ClosedPos;
    private Vector2 Propreview_ClosedPos;

    private float swingStepWidth;
    private float swingCorrectVal = 0;

    private int currentIndex = 0;
    private int totalSwinStep = 8;
    private int improveCnt = 0;
    private int avgScore = 0;
    
    public int AvgScore {
        get { return avgScore; }
        set { avgScore = value; }
    }

    bool _isFrontTop = true;
    bool _isScorePanelOpen = false;
    bool _isProPreviewPanelOpen = false;
    bool _isProPreviewOpenFirst = true;

    [SerializeField] GameObject debugFront;
    [SerializeField] GameObject debugSide;
    [SerializeField] GameObject ipGap;
    [SerializeField] Toggle tglDebug;

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;

        _isFrontTop = true;

        tglDebug.onValueChanged.AddListener(OnValueChanged_Debug);
        m_ModelController.txtDebug.gameObject.SetActive(false);
        Init();
    }

    
    void Update()
    {        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log($"Quit()");
            m_ModelController.CloseMocap();
            Application.Quit();
        }
        /*
        else if (Input.GetKeyDown(KeyCode.S))
        {
            int ran = Random.Range(0, 2);
            Utillity.Instance.ShowToast(Utillity.Instance.GetFeedbackDic()[$"AD0{ran}"]);
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            Utillity.Instance.HideToast();
        }
        else if(Input.GetKeyDown(KeyCode.P))
        {
            GenerateScorePanel(!_isScorePanelOpen);
        }
         */
    }


    void Init()
    {
        GameManager.Instance.SetOptionPanel();

        if (GameManager.Instance.Mode == EStep.Realtime)
        {
            m_ProPreviewPanel.gameObject.SetActive(false);
            m_PreviewQuarterObj.gameObject.SetActive(false);
        }
        else
        {
            m_ProPreviewPanel.gameObject.SetActive(true);
            m_PreviewQuarterObj.gameObject.SetActive(true);
        }

        m_ClubImg.sprite = m_ClubSprites[(int)GameManager.Instance.Club];
        m_ClubNameText.text = GameManager.Instance.IronNumber == -1 ? $"{Utillity.Instance.ConvertEnumToString(GameManager.Instance.Club)}" : GameManager.Instance.IronNumber == 10 ? $"{Utillity.Instance.ConvertEnumToString(GameManager.Instance.Club)}(P)" : $"{Utillity.Instance.ConvertEnumToString(GameManager.Instance.Club)}({GameManager.Instance.IronNumber})";

        //애니메이션 셋업


        proQuarterOriPos = m_ProQuarter.localPosition;
        proQuarterOriRot = m_ProQuarter.localEulerAngles;
        previewQuarterOriPos = m_PreviewQuarter.localPosition;
        previewQuarterOriRot = m_PreviewQuarter.localEulerAngles;

        // Score Panel
        SP_ClosedPos = m_ScorePanel.anchoredPosition;
        SP_OpenPos = new Vector2(-430.0f, m_ScorePanel.anchoredPosition.y);
        FP_ClosedPos = m_FeedbackPanel.anchoredPosition;
        AIP_ClosedPos = m_AISwingPanel.anchoredPosition;
        Propreview_ClosedPos = m_ProPreviewPanel.anchoredPosition;

        // SwingStep
        swingCorrectVal = m_SwingStepContainer.anchoredPosition.x;
        swingStepWidth = m_SwingStepContainer.GetChild(0).GetComponent<RectTransform>().rect.width;
        //UpdateSwingStepPosition();
        UpdateSwingStepAlphaAndScale();
    }

    public void GenerateFeedbackPanel(bool isOn)
    {
        if (GameManager.Instance.Mode == EStep.Preview)
            return;

        if (isOn)
        {
            avgScoreList.Add(CalculateAvgScore((int)m_ModelController.SwingStep + 1));
            AddFeedbackNewData(CalculateAvgScore((int)m_ModelController.SwingStep + 1));
        }

        m_FeedbackPanel.DOAnchorPos(isOn ? new Vector2(0, m_FeedbackPanel.anchoredPosition.y) : FP_ClosedPos, 0.7f).SetEase(Ease.InOutQuad).OnComplete(() => {
            if(isOn)
            {
                int count = avgScoreList.Count;

                if (count >= 3)
                {
                    List<int> last3 = avgScoreList.TakeLast(3).ToList();

                    if (last3[0] < last3[1] && last3[1] < last3[2])
                    {
                        GenerateAISwingPanel(true);
                    }
                    else
                    {
                        GenerateAISwingPanel(false);
                    }
                }
            }
        });
    }

    public void GenerateAISwingPanel(bool isOn)
    {
        if (GameManager.Instance.Mode == EStep.Preview)
            return;

        m_AISwingPanel.DOAnchorPos(isOn ? new Vector2(0, m_AISwingPanel.anchoredPosition.y) : AIP_ClosedPos, 0.7f).SetEase(Ease.InOutQuad)/*.OnComplete(() => {})*/;
    }

    public void GenerateScorePanel(bool isOn)
    {
        if (GameManager.Instance.Mode == EStep.Preview)
            return;

        if (isOn && m_ModelController.SwingStep == SWINGSTEP.READY)
            ActivateQuarterAnimation();
        else
            DeactivateQuarterAnimation();

        _isScorePanelOpen = isOn;
        m_ScorePanel.DOAnchorPos(_isScorePanelOpen ? SP_OpenPos : SP_ClosedPos, 0.7f).SetEase(Ease.InOutQuad);
    }

    public void GenerateProPreviewPanel()
    {
        if (GameManager.Instance.Mode == EStep.Realtime)
            return;

        _isProPreviewPanelOpen = !_isProPreviewPanelOpen;

        m_ProPreviewPanelArrow.DORotate(_isProPreviewPanelOpen ? flipY : Vector2.zero, 0.7f);
        m_ProPreviewPanel.DOAnchorPos(_isProPreviewPanelOpen ? new Vector2(0, m_ProPreviewPanel.anchoredPosition.y) : Propreview_ClosedPos, 0.7f).SetEase(Ease.InOutQuad)
            .OnComplete(() => {
                if(_isProPreviewPanelOpen)
                {
                    if (_isProPreviewOpenFirst)
                    {
                        StartCoroutine(MoveKeyPointExpert(m_AniPreview, GameManager.Instance.Stance.Max(), 1, 2.0f));
                    }
                    else
                    {
                        StartCoroutine(MoveKeyPointExpert(m_AniPreview, GameManager.Instance.Stance.Max(), 0, 2.0f));
                    }
                }
                else
                {
                    m_AniPreview.SetFloat("SwingValue", 0.0f);
                }
            });
    }

    public void AnimateProgress(float startValue, int endValue, float duration, bool reset = false)
    {
        // RealTime

        avgScore += endValue;
        int avg = CalculateAvgScore((int)m_ModelController.SwingStep + 1);
        avgScoreTxt.text = $"현재 매칭률 : {avg}%";

#if UNITY_EDITOR
        m_SwingAccImg.color = avg <= 29 ? Color.red : (avg <= 79 ? Color.yellow : Color.green);
#else
        m_SwingAccImg.color = avg <= 29 ? Utillity.Instance.HexToRGB(INI.Red) : (avg <= 79 ? Utillity.Instance.HexToRGB(INI.Yellow) : Utillity.Instance.HexToRGB(INI.Green500));
#endif

        m_SwingAccImg.DOFillAmount(avg / 100f, duration).SetEase(Ease.OutQuad).OnComplete(() => {
            UpdateScore((int)m_ModelController.SwingStep, endValue);
        });

        DOTween.To(() => startValue, x => 
        {
            if (!reset)
                m_PercentageTxt.text = $"{x:0}<size=40%>%</size>";
            else
            {
                m_SwingAccImg.fillAmount = 0.0f;
                m_PercentageTxt.text = string.Empty;
                stepScoreDic.Clear();
            }
                
        }, avg, duration).SetEase(Ease.OutQuad);
    }

    private Tween previewSwingAccTween;
    private Tween previewTextTween;
    private float previewCurScore = 0.0f;

    public void AnimateProgress(int score, bool reset = false)
    {
        // Preview

#if UNITY_EDITOR
        m_SwingAccPreviewImg.color = score <= 29 ? Color.red : (score <= 79 ? Color.yellow : Color.green);
#else
        m_SwingAccPreviewImg.color = score <= 29 ? Utillity.Instance.HexToRGB(INI.Red) : (score <= 79 ? Utillity.Instance.HexToRGB(INI.Yellow) : Utillity.Instance.HexToRGB(INI.Green500));
#endif

        previewSwingAccTween.Kill();
        previewTextTween.Kill();

        if (reset)
        {
            m_SwingAccPreviewImg.fillAmount = 0.0f;
            m_PercentagePreviewTxt.text = string.Empty;
            previewCurScore = 0.0f;
            return;
        }

        previewSwingAccTween = m_SwingAccPreviewImg.DOFillAmount(score / 100.0f, 1.0f).SetEase(Ease.OutQuad);

        float preValue = previewCurScore;

        previewCurScore = score;

        previewTextTween = DOTween.To(() => preValue, x => {
            m_PercentagePreviewTxt.text = $"{x:0}<size=40%>%</size>";
        }, score, 1.0f).SetEase(Ease.OutQuad);
    }

    public void ActivateQuarterAnimation()
    {
        if(GameManager.Instance.Mode == EStep.Realtime)
        {
            m_ProQuarter.gameObject.SetActive(true);

            m_ProQuarter.localPosition = proQuarterOriPos;
            m_ProQuarter.localEulerAngles = proQuarterOriRot;

            m_rotationTween = m_ProQuarter.DORotate(new Vector3(0, 0, -360), 2.0f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
        }
        else
        {
            m_PreviewQuarter.gameObject.SetActive(true);

            m_PreviewQuarter.localPosition = previewQuarterOriPos;
            m_PreviewQuarter.localEulerAngles = previewQuarterOriRot;

            m_rotationTween = m_PreviewQuarter.DORotate(new Vector3(0, 0, -360), 2.0f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
        }
    }

    public void DeactivateQuarterAnimation()
    {
        m_rotationTween.Kill();
        m_rotationTween = null;

        if (GameManager.Instance.Mode == EStep.Realtime)
        {
            m_ProQuarter.gameObject.SetActive(false);
        }
        else
        {
            m_PreviewQuarter.gameObject.SetActive(false);
        }
    }

    public void MoveToSwingStep(int index)
    {
        if (index < 0 || index >= totalSwinStep)
            return;

        if (currentIndex == index)
        {
            return;
        }
            

        float targetX = ((0 - index) * swingStepWidth) + swingCorrectVal;
        m_SwingStepContainer.DOAnchorPos(new Vector2(targetX, m_SwingStepContainer.anchoredPosition.y), 0.5f).SetEase(Ease.OutCubic);

        UpdateSwingStepAlphaAndScale(index);

        currentIndex = index;
    }

    private void UpdateSwingStepPosition()
    {
        float startX = ((0 - currentIndex) * swingStepWidth) + swingCorrectVal;
        m_SwingStepContainer.anchoredPosition = new Vector2(startX, m_SwingStepContainer.anchoredPosition.y);
    }

    private void UpdateSwingStepAlphaAndScale(int newIndex = -1)
    {
        if (newIndex == -1) newIndex = currentIndex;

        for (int i = 0; i < totalSwinStep; i++)
        {
            CanvasGroup canvasGroup = m_SwingStepContainer.GetChild(i).GetComponent<CanvasGroup>();
            RectTransform panelRect = m_SwingStepContainer.GetChild(i).GetComponent<RectTransform>();

            if (i == newIndex)
            {
                canvasGroup.DOFade(1f, 0.5f);
                panelRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutCubic);
            }
            else if (i == newIndex - 1 || i == newIndex + 1)
            {
                canvasGroup.DOFade(0.3f, 0.5f);
                panelRect.DOScale(new Vector3(0.8f, 0.8f, 1f), 0.5f).SetEase(Ease.OutCubic);
            }
            else
            {
                canvasGroup.DOFade(0f, 0.5f);
                panelRect.DOScale(new Vector3(0.6f, 0.6f, 1f), 0.5f).SetEase(Ease.OutCubic);
            }
        }
    }

    public void UpdateScore(int stepIndex, int score)
    {
        if (stepIndex < 0) return;
        if (m_ScoreTxt == null || stepIndex >= m_ScoreTxt.Length) return;

        stepScoreDic.Add(m_ModelController.SwingStep, score);
        TextMeshProUGUI txt = m_ScoreTxt[stepIndex];
        txt.text = score.ToString();

        Color color;

        if (score < 30)
            ColorUtility.TryParseHtmlString(INI.Red, out color);
        else if (score < 80)
            ColorUtility.TryParseHtmlString(INI.Yellow, out color);
        else
            ColorUtility.TryParseHtmlString(INI.Green500, out color);

        txt.DOColor(color, 0.5f).SetEase(Ease.OutQuad);
    }

    public void ResetScoreText()
    {
        foreach (var item in m_ScoreTxt)
        {
            item.color = Color.white;
            item.text = "-";
        }
    }

    public int CalculateAvgScore(int avgCnt)
    {
        if(avgCnt == 0)
        {
            avgScore = 0;
            m_SwingAccImg.fillAmount = 0.0f;

            return 0;
        }
        else
        {
            float avg = avgScore / avgCnt;

            return (int)Math.Round(avg, 0);
        }
    }

    public void ResetFeedbackGraph()
    {
        feedbackDataQueue.Clear();

        for(int i = 0; i < 3; i++)
        {
            dotPoints[i].gameObject.SetActive(false);
            dotValueTxts[i].text = string.Empty;
            timesTxt[i].text = $"{i + 1}회";
        }

        changeTxt.text = "분석이 시작됩니다.";
        dotLineRenderer.Points = new Vector2[0];
        dotLineRenderer.SetAllDirty();
        dotLineRenderer.enabled = false;
    }

    public void AddFeedbackNewData(int value)
    {
        if (feedbackDataQueue.Count >= 3)
            feedbackDataQueue.Dequeue();

        feedbackDataQueue.Enqueue(value);
        UpdateFeedbackGraph();
    }

    private void UpdateFeedbackGraph()
    {
        int[] scores = feedbackDataQueue.ToArray();
        int totalCount = avgScoreList.Count;
        int baseTurn = totalCount - scores.Length + 1;

        Vector2[] positions = new Vector2[scores.Length];

        Vector2 lineBasePos = dotLineRenderer.rectTransform.anchoredPosition;

        for(int i = 0; i < scores.Length; i++)
        {
            float posY = Mathf.Clamp01(scores[i] / 100.0f) * 85.0f;

            dotPoints[i].anchoredPosition = new Vector2(dotPoints[i].anchoredPosition.x, posY);
            dotPoints[i].gameObject.SetActive(true);

            dotValueTxts[i].text = scores[i].ToString();
            timesTxt[i].text = $"{baseTurn + i}회";

            positions[i] = dotPoints[i].anchoredPosition - lineBasePos;
        }

        dotLineRenderer.Points = positions;
        dotLineRenderer.SetAllDirty();
        dotLineRenderer.enabled = (scores.Length >= 2);

        if (scores.Length >= 2)
        {
            int[] array = feedbackDataQueue.ToArray();
            int diff = array[scores.Length - 1] - array[scores.Length - 2];

            changeTxt.color = diff >= 0 ? Utillity.Instance.HexToRGB(INI.Green600) : Utillity.Instance.HexToRGB(INI.Grey300);

            string result = diff >= 0 ? $"▲ {Mathf.Abs(diff)}% 향상됐어요!" : $"▼ {Mathf.Abs(diff)}% 하락했어요";

            changeTxt.text = result;
        }
        else
        {
            changeTxt.text = "분석이 시작됩니다.";
        }

        SWINGSTEP minKey = stepScoreDic.OrderBy(v => v.Value).First().Key;

        tipTitleTxt.text = $"{Utillity.Instance.ConvertEnumToString(minKey)}";

        stepScoreDic.Clear();
    }

    IEnumerator MoveKeyPointExpert(Animator animator, SWINGSTEP swingStep, int repeat = 0, float time = 1f)
    {
        float from = 0;
        float to = 0;

        if (swingStep == SWINGSTEP.ADDRESS)
        {
            to = 0;
        }
        else if (swingStep == SWINGSTEP.TAKEBACK)
        {
            to = 0.23f;
        }
        else if (swingStep == SWINGSTEP.BACKSWING)
        {
            to = 0.35f;
        }
        else if (swingStep == SWINGSTEP.TOP)
        {
            to = 0.5f;
        }
        else if (swingStep == SWINGSTEP.DOWNSWING)
        {
            to = 0.61f;
        }
        else if (swingStep == SWINGSTEP.IMPACT)
        {
            to = 0.661f;
        }
        else if (swingStep == SWINGSTEP.FOLLOW)
        {
            to = 0.76f;
        }
        else if (swingStep == SWINGSTEP.FINISH)
        {
            to = 0.99f;
        }
        else if (swingStep == SWINGSTEP.READY)
        {
            from = 0;
            to = 0;
        }

        if (Enum.IsDefined(typeof(SWINGSTEP), swingStep))
        {
            if(repeat == 0)
            {
                while(_isProPreviewPanelOpen)
                {
                    float elapsedTime = 0f;

                    int idx = (int)swingStep;
                    if (idx < 0) idx = 0;

                    while (elapsedTime < time)
                    {
                        if (!_isProPreviewPanelOpen)
                            break;

                        animator.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    if (!_isProPreviewPanelOpen)
                        break;

                    animator.SetFloat("SwingValue", to);

                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                for (int i = 0; i < repeat; i++)
                {
                    float elapsedTime = 0f;

                    int idx = (int)swingStep;
                    if (idx < 0) idx = 0;

                    while (elapsedTime < time)
                    {
                        animator.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                        elapsedTime += Time.deltaTime;
                        yield return null;
                    }

                    animator.SetFloat("SwingValue", to);

                    _isProPreviewOpenFirst = false;

                    yield return new WaitForSeconds(1.0f);

                    GenerateProPreviewPanel();
                }
            }
        }

        yield return null;
    }

    public void OnClick_SwapViwerTopBottom()
    {
        _isFrontTop = !_isFrontTop;

        RawImage_Top.texture = _isFrontTop ? RenderTexture_Front : RenderTexture_Side;
        RawImage_Bottom.texture = _isFrontTop ? RenderTexture_Side : RenderTexture_Front;

        RawImage_Top.transform.localScale = _isFrontTop ? new Vector3(-1, 1, 1) : Vector3.one;
        RawImage_Bottom.transform.localScale = _isFrontTop ? Vector3.one : new Vector3(-1, 1, 1);

        m_FrontLines.SetActive(_isFrontTop);
        m_SideLines.SetActive(!_isFrontTop);
    }

    public void Onclick_Button()
    {
        GameObject obj = EventSystem.current.currentSelectedGameObject;

        switch (obj.name)
        {
            // Top Left
            case "Home":
                /*m_ModelController.CloseMocap();
                GameManager.Instance.SelectedSceneName = string.Empty;
                SceneManager.LoadScene("ModeSelect");
                Utillity.Instance.HideToast();
                Utillity.Instance.HideGuideArrow();*/
                StartCoroutine(ExitPractice());
                break;

            case "Option":
                GameManager.Instance.OnClick_OptionPanel();
                break;

            case "SkeletonCam":
                if (!object.ReferenceEquals(m_SkeletonCover, null))
                    m_SkeletonCover.SetActive(!m_SkeletonCover.activeInHierarchy);

                break;

            case "AICoaching":
                GameManager.Instance.SelectedSceneName = "AICoaching";

                SceneManager.LoadScene("AICoaching");
                break;

            case "FocusPractice":
                GameManager.Instance.Mode = EStep.Preview;

                GameManager.Instance.Stance.Clear();
                GameManager.Instance.Stance.Add(stepScoreDic.OrderBy(v => v.Value).First().Key);
                //GameManager.Instance.Club = 

                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                break;
        }
    }

    IEnumerator ExitPractice()
    {
        Application.targetFrameRate = 30;
        QualitySettings.vSyncCount = 0;

        m_ModelController.CloseMocap();
        //Utillity.Instance.HideToast();
        //Utillity.Instance.HideGuideArrow();

        yield return new WaitUntil(() => m_ModelController.CheckMocapClose() == true);

        GameManager.Instance.Mode = EStep.Realtime;
        GameManager.Instance.SelectedSceneName = string.Empty;
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnClick_CamSwap()
    {
        bool CAMSWAP = PlayerPrefs.GetInt("CAMSWAP", 0) == 1 ?  true : false;
        CAMSWAP = !CAMSWAP;
        PlayerPrefs.SetInt("CAMSWAP", CAMSWAP ? 1 : 0);
    }

    public void OnValueChanged_Debug(bool isOn)
    {
        debugFront.SetActive(isOn);
        debugSide.SetActive(isOn);
        ipGap.SetActive(isOn);
        m_ModelController.txtDebug.gameObject.SetActive(isOn);

        m_ModelController.LoadDebugPoseData();
    }
}
