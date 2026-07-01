using DG.Tweening;
using Enums;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupOnboardingDirector : MonoBehaviour
{
    [SerializeField] Transform ProModel;
    [SerializeField] Transform ProClub;
    [SerializeField] Transform UserModel;
    
    [SerializeField] TextMeshProUGUI txtLablePro;
    [SerializeField] TextMeshProUGUI txtLableUser;
    [SerializeField] TextMeshProUGUI txtDescTop;
    [SerializeField] TextMeshProUGUI txtDescBtm;
    [SerializeField] GameObject StepGroup;
    [SerializeField] Transform StepGroupChild;
    [SerializeField] Animator aniPro;
    [SerializeField] Animator aniClub;
    [SerializeField] Animator aniuser;
    [SerializeField] Toggle tglRealtimeMode;
    [SerializeField] Toggle tglPreviewMode;
    [SerializeField] Highlighters.Highlighter highlighters;
    [SerializeField] Highlighters.Highlighter highlighters_calf;

    bool _startFinish = false;

    private void Awake()
    {
        tglRealtimeMode.interactable = false;
        tglPreviewMode.interactable = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        txtDescTop.text = string.Empty;
        txtDescBtm.text = string.Empty;
        highlighters.enabled = false;
        highlighters_calf.enabled = false;

        if (GameManager.Instance.Mode == EStep.None || GameManager.Instance.Mode == EStep.Realtime)
        {
            tglRealtimeMode.isOn = true;
            GameManager.Instance.Mode = EStep.Realtime;
        }
        else
        {
            tglPreviewMode.isOn = true;
            GameManager.Instance.Mode = EStep.Preview;
        }

        tglRealtimeMode.onValueChanged.AddListener(OnVlaueChanged_SelectMode);

        yield return new WaitForSeconds(0.5f);

        Sequence seq = DOTween.Sequence();
        seq.Join(ProModel.DOLocalMoveX(0, 1f))
            .Join(ProClub.DOLocalMoveX(0, 1f))
            .Join(UserModel.DOLocalMoveX(0, 1f))
            .Join(txtLablePro.transform.DOLocalMoveX(0, 1f)).Join(txtLablePro.DOFade(0,1f))
            .Join(txtLableUser.transform.DOLocalMoveX(0, 1f)).Join(txtLableUser.DOFade(0, 1f));
        seq.Play();
        yield return new WaitForSeconds(1.2f);

        _startFinish = true;
        tglRealtimeMode.interactable = true;
        tglPreviewMode.interactable = true;

        if (GameManager.Instance.Mode == EStep.Realtime)
        {
            StartCoroutine(CoRealtimeMode());
        }
        else
        {
            StartCoroutine(CoPreviewMode());
        }
    }

    public void OnClick_Next()
    {
        SceneManager.LoadScene("Setup");
    }

    public void OnClick_Back()
    {
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnVlaueChanged_SelectMode(bool val)
    {
        if (_startFinish == false)
            return;

        StopAllCoroutines();
        txtDescTop.text = string.Empty;
        txtDescBtm.text = string.Empty;
        highlighters.enabled = false;
        highlighters_calf.enabled = false;
        StepGroup.SetActive(false);

        if (tglRealtimeMode.isOn)
        {
            GameManager.Instance.Mode = EStep.Realtime;
            StartCoroutine(CoRealtimeMode());
        }
        else
        {
            GameManager.Instance.Mode = EStep.Preview;
            StartCoroutine(CoPreviewMode());
        }
    }

    IEnumerator CoPreviewMode()
    {
        StepGroup.SetActive(false);

        yield return null;

        while (true)
        {
            StartCoroutine(MoveKeyPoint(aniPro, SWINGSTEP.ADDRESS, 0f, aniClub));
            StartCoroutine(MoveKeyPoint(aniuser, SWINGSTEP.ADDRESS, 0f));
            StepGroupChild.DOLocalMoveX(-570, 0); //백스윙
            txtDescTop.text = "프로";            
            txtDescBtm.text = "프로가 먼저 시범을 보여드려요";

            ProModel.gameObject.SetActive(true);
            ProClub.gameObject.SetActive(true);
            UserModel.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.5f);

            aniPro.Play("midiron_full");
            yield return StartCoroutine(MoveKeyPointExpert(aniPro, SWINGSTEP.FINISH, 2, 2f, aniClub));
            yield return new WaitForSeconds(0.1f);

            ProModel.gameObject.SetActive(false);
            UserModel.gameObject.SetActive(true);
            ProClub.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.1f);

            aniuser.Play("midiron_full");
            txtDescTop.text = "나의 스윙차례 입니다.";
            txtDescBtm.text = "프로의 자세를 생각하며\r\n직접 스윙해보세요!";

            yield return StartCoroutine(MoveKeyPointExpert(aniuser, SWINGSTEP.FINISH, 1, 2.5f));
            
            yield return new WaitForSeconds(1.5f);
        }
    }

    IEnumerator CoRealtimeMode()
    {
        StepGroup.SetActive(true);
        ProModel.gameObject.SetActive(true);
        UserModel.gameObject.SetActive(true);

        aniPro.Play("midiron_full");
        aniuser.Play("midiron_full");

        yield return null;

        while (true)
        {
            StartCoroutine(MoveKeyPoint(aniPro, SWINGSTEP.ADDRESS, 0f, aniClub));
            StartCoroutine(MoveKeyPoint(aniuser, SWINGSTEP.ADDRESS, 0f));
            StepGroupChild.DOLocalMoveX(-70,0);
            txtDescTop.text = string.Empty;
            txtDescBtm.text = "프로의 스윙이 사라지지 않고\r\n그대로 있어요";

            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.TAKEBACK));
            txtDescBtm.text = "스윙 자세에 맞춰\r\n천천히 따라해보세요!";
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.BACKSWING));
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.TOP));
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.DOWNSWING));
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.IMPACT));
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.FOLLOW));
            yield return StartCoroutine(CoStepMode_SetPose(SWINGSTEP.FINISH));
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator CoStepMode_SetPose(SWINGSTEP step)
    {
        StepGroupChild.DOLocalMoveX(StepGroupChild.localPosition.x - 250, 0.3f);
        yield return StartCoroutine(MoveKeyPoint(aniPro, step, 0.65f, aniClub));
        yield return new WaitForSeconds(0.1f);
        yield return StartCoroutine(MoveKeyPoint(aniuser, step, 0.65f));

        if (step == SWINGSTEP.BACKSWING)
        {
            highlighters_calf.enabled = true;
            yield return new WaitForSeconds(0.4f);
            highlighters_calf.enabled = false;
        }
        else
        {
            highlighters.enabled = true;
            yield return new WaitForSeconds(0.2f);
            highlighters.enabled = false;
        }            
    }

    IEnumerator MoveKeyPoint(Animator animator, SWINGSTEP swingStep, float time, Animator club = null)
    {
        Debug.Log($"MoveKeyPoint({swingStep}) Start");

        float from = 0;
        float to = 0;
        if (swingStep == SWINGSTEP.ADDRESS)
        {
            from = 0;
            to = 0;
        }
        else if (swingStep == SWINGSTEP.TAKEBACK)
        {
            from = 0;
            to = 0.19f;
        }
        else if (swingStep == SWINGSTEP.BACKSWING)
        {
            from = 0.19f;
            to = 0.28f;
        }
        else if (swingStep == SWINGSTEP.TOP)
        {
            from = 0.28f;
            to = 0.45f;
        }
        else if (swingStep == SWINGSTEP.DOWNSWING)
        {
            from = 0.45f;
            to = 0.61f;
        }
        else if (swingStep == SWINGSTEP.IMPACT)
        {
            from = 0.61f;
            to = 0.638f;
        }
        else if (swingStep == SWINGSTEP.FOLLOW)
        {
            from = 0.638f;
            to = 0.68f;
        }
        else if (swingStep == SWINGSTEP.FINISH)
        {
            from = 0.68f;
            to = 0.99f;
        }
        else if (swingStep == SWINGSTEP.READY)
        {
            from = 0;
            to = 0;
        }

        if (Enum.IsDefined(typeof(SWINGSTEP), swingStep))
        {
            float elapsedTime = 0f;

            int idx = (int)swingStep;
            if (idx < 0) idx = 0;

            while (elapsedTime < time)
            {
                animator.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                if(club != null)
                    club.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            animator.SetFloat("SwingValue", to);
            if (club != null)
                club.SetFloat("SwingValue", to);

            Debug.Log("MoveKeyPoint() Finish");
        }

        yield return null;
    }

    IEnumerator MoveKeyPointExpert(Animator animator, SWINGSTEP swingStep, int repeat = 0, float time = 1f, Animator club = null)
    {
        float from = 0;
        float to = 0;

        if (swingStep == SWINGSTEP.ADDRESS)
        {
            to = 0;
        }
        else if (swingStep == SWINGSTEP.TAKEBACK)
        {
            to = 0.19f;
        }
        else if (swingStep == SWINGSTEP.BACKSWING)
        {
            to = 0.28f;
        }
        else if (swingStep == SWINGSTEP.TOP)
        {
            to = 0.45f;
        }
        else if (swingStep == SWINGSTEP.DOWNSWING)
        {
            to = 0.61f;
        }
        else if (swingStep == SWINGSTEP.IMPACT)
        {
            to = 0.638f;
        }
        else if (swingStep == SWINGSTEP.FOLLOW)
        {
            to = 0.68f;
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
            for (int i = 0; i < repeat; i++)
            {
                float elapsedTime = 0f;

                int idx = (int)swingStep;
                if (idx < 0) idx = 0;

                while (elapsedTime < time)
                {
                    animator.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                    if (club != null)
                        club.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                animator.SetFloat("SwingValue", to);
                if (club != null)
                    club.SetFloat("SwingValue", to);

                yield return new WaitForSeconds(0.5f);
            }
            
            Debug.Log("MoveKeyPointExpert() Finish");
        }

        yield return null;
    }
}
