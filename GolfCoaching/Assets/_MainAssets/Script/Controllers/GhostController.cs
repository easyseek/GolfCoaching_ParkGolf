using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Enums;
using System.Linq;

public class GhostController : MonoBehaviour
{
    [SerializeField] Animator ani3DModel;
    [SerializeField] Animator ani3DClub;

    SWINGSTEP swingStep = SWINGSTEP.READY;

    [Header("* CLUB Objs")]
    [SerializeField] GameObject objClub_expoertMidironFull;
    [SerializeField] GameObject objClub_expoertDruverFull;

    [Header("* Pose Texts")]
    [SerializeField] TextMeshProUGUI[] txtPoseNames;
    float _timer = 0;
    public bool isChanging = false;

    public Action EndEvent;

    [SerializeField] GameObject BodyRoot;
    [SerializeField] GameObject ClubObject;
    [SerializeField] GameObject ClubObject_Ghost;
    [SerializeField] Material matGhost;

    [Header("* PRO PHOTOS")]
    [SerializeField] Image imgAreaFront;
    [SerializeField] Image imgAreaSide;
    [SerializeField] Sprite[] ProPhotosFront;
    [SerializeField] Sprite[] ProPhotosSide;

    ProSwingStepData swingStepData = null;
    [SerializeField] Transform lShoulder;
    [SerializeField] Transform rShoulder;
    [SerializeField] Transform hand;
    float modelAngle;

    float impactAniValue = 0.633f;

    [SerializeField] SensorViewerFront sensorViewerFront;
    [SerializeField] SensorViewerSide sensorViewerSide;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (GameManager.Instance.SwingType == ESwingType.Full)
        {
            if (GameManager.Instance.Club == EClub.MiddleIron || GameManager.Instance.Club == EClub.ShortIron
                || GameManager.Instance.Club == EClub.LongIron)
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.MiddleIron];


            }
            else if (GameManager.Instance.Club == EClub.Driver)
            {
                swingStepData = GolfProDataManager.Instance.SelectProData.swingData.dicFull[EClub.Driver];

            }
        }
        //sensorViewerFront.swingStepData = swingStepData;
        //sensorViewerSide.swingStepData = swingStepData;

        //if (GameManager.Instance.Mode == EStep.Beginner)
        //{
        SetGhostMatrial();
            ClubObject.SetActive(false);
            ClubObject_Ghost.SetActive(true);
        //}
        //else
        //{
        //    ClubObject.SetActive(true);
        //    ClubObject_Ghost.SetActive(false);
        //}


        //ani3DModel.Play("midiron_full");
        //ani3DModel.SetFloat("SwingValue", 0f);
        //ani3DClub.Play("midiron_full");
        //ani3DClub.SetFloat("SwingValue", 0f);

        StartCoroutine(PoseNameProcess());
    }

    public void SetProAnimation(ESwingType swingType, EClub club)
    {
        if(swingType == ESwingType.Full)
        {
            if(club == EClub.MiddleIron || club == EClub.ShortIron || club == EClub.LongIron)
            {
                ani3DModel.Play("midiron_full");

                //objClub_expoertMidironFull.SetActive(true);
                //ani3DClub = objClub_expoertMidironFull.GetComponent<Animator>();
                //ani3DClub.Play("expert_midiron_full");
                //impactAniValue = 0.633f;
            }
            else if (club == EClub.Driver)
            {
                ani3DModel.Play("driver_full");

                //objClub_expoertDruverFull.SetActive(true);
                //ani3DClub = objClub_expoertDruverFull.GetComponent<Animator>();
                //ani3DClub.Play("expert_driver_full");
                //impactAniValue = 0.637f;
            }
        }

        ani3DModel.SetFloat("SwingValue", 0f);
        //ani3DClub.SetFloat("SwingValue", 0f);
    }

    float  GetModelAngle()
    {
        Vector3 sCenter = (lShoulder.position + rShoulder.position) / 2f;
        Vector3 dir = hand.position - sCenter;

        //modelAngle = Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
        return Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
    }

    public void BodyShow(bool isShow)
    {
        if (BodyRoot.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
        {
            skinnedMeshRenderer.enabled = isShow;
            ClubObject.SetActive(isShow);
            ClubObject_Ghost.SetActive(isShow);
        }
    }

    public void SetGhostMatrial()
    {
        if (BodyRoot.TryGetComponent(out SkinnedMeshRenderer skinnedMeshRenderer))
        {
            Material[] mats = new Material[skinnedMeshRenderer.materials.Length];

            for (int i = 0; i < skinnedMeshRenderer.materials.Length; i++)
                mats[i] = matGhost;
            skinnedMeshRenderer.SetMaterials(mats.ToList());
        }
    }

    public void SetPose(SWINGSTEP swingStep, bool isRev = false)
    {
        if (this.swingStep != swingStep)
        {
            Debug.Log($"SetPose({swingStep})");
            isChanging = true;

            this.swingStep = swingStep;

            if(GameManager.Instance.Mode == EStep.Realtime)
            {
                if (swingStep == SWINGSTEP.READY)
                {
                    //StartCoroutine(MoveKeyPoint((SWINGSTEP)((int)swingStep), 0.0f));
                    StartCoroutine(MoveAnglePoint((SWINGSTEP)((int)swingStep), 0.0f));
                }
                else
                    //StartCoroutine(MoveKeyPoint((SWINGSTEP)((int)swingStep + 1)));
                    StartCoroutine(MoveAnglePoint((SWINGSTEP)((int)swingStep + 1)));
            }
            else
            {
                //StartCoroutine(MoveKeyPointExpert((SWINGSTEP)((int)swingStep), 0.0f, 2, true));
                StartCoroutine(MoveAnglePointExpert((SWINGSTEP)((int)swingStep), 0.0f, 2, true));
            }
        }
    }

    IEnumerator PoseNameProcess()
    {
        int lastPoseIndex = -1;
        while(true)
        {
            yield return null;

            if (lastPoseIndex == (int)swingStep && (int)swingStep > 6)
                continue;

            if (lastPoseIndex == (int)swingStep)
            {
                _timer += Time.deltaTime;
                if (_timer < 0.3f)
                    txtPoseNames[lastPoseIndex < -1 ? 0 : lastPoseIndex + 1].color = Color.yellow;
                else if (_timer < 0.6f)
                {
                    txtPoseNames[lastPoseIndex < -1 ? 0 : lastPoseIndex + 1].color = Color.gray;
                }
                else if (_timer > 0.6f)
                    _timer = 0;
            }
            else
            {
                for (int i = 0; i < txtPoseNames.Length; i++)
                {
                    if (i <= (int)swingStep)
                        txtPoseNames[i].color = Color.green;
                    else
                        txtPoseNames[i].color = Color.gray;
                }
                _timer = 0;
                Debug.Log($"lastPoseIndex = (int)swingStep; = {lastPoseIndex} = {(int)swingStep}");
                lastPoseIndex = (int)swingStep;                
            }
        }
    }

    IEnumerator MoveAnglePoint(SWINGSTEP swingStep, float coTime = 0.5f)
    {
        Debug.Log($"MoveAnglePoint({swingStep}, {coTime}) Start");
        yield return new WaitForSeconds(coTime);

        //float from = ani3DModel.GetFloat("SwingValue");
        float from = GetModelAngle();
        float to = 0;
        bool isRev = true;

        if (swingStep == SWINGSTEP.ADDRESS)
        {
            to = swingStepData.dicAddress["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.TAKEBACK)
        {
            to = swingStepData.dicTakeback["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.BACKSWING)
        {
            to = swingStepData.dicBackswing["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.TOP)
        {
            to = swingStepData.dicTop["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.DOWNSWING)
        {
            to = swingStepData.dicDownswing["GetHandDir"];
            isRev = false;
        }
        else if (swingStep == SWINGSTEP.IMPACT)
        {
            //to = 188f;//임팩트 고정
            to = swingStepData.dicImpact["GetHandDir"];
            isRev = false;
        }
        else if (swingStep == SWINGSTEP.FOLLOW)
        {
            to = swingStepData.dicFollow["GetHandDir"];
            isRev = false;
        }
        else if (swingStep == SWINGSTEP.FINISH)
        {
            to = swingStepData.dicFinish["GetHandDir"];
            isRev = false;
        }

        else if (swingStep == SWINGSTEP.READY)
        {
            to = swingStepData.dicAddress["GetHandDir"];
            isRev = true;
        }

        if (sensorViewerFront != null && sensorViewerFront.gameObject.activeInHierarchy) sensorViewerFront.SetGetProValue(swingStep);
        if (sensorViewerSide != null && sensorViewerSide.gameObject.activeInHierarchy) sensorViewerSide.SetGetProValue(swingStep);

        Debug.Log($"MoveAnglePoint({swingStep}) SET : {GetModelAngle()} -> {to}");

        if ((int)swingStep > -3 && (int)swingStep < 8)//Enum.IsDefined(typeof(SWINGSTEP), swingStep))
        {
            if (swingStep == SWINGSTEP.ADDRESS || swingStep == SWINGSTEP.READY)
            {
                SetAniValue(0);
            }
            else
            {
                //float elapsedTime = 0f;

                //float time = swingStep == SWINGSTEP.FINISH ? 1f : 2f;

                int idx = (int)swingStep;
                if (idx < 0) idx = 0;
                imgAreaFront.sprite = ProPhotosFront[idx];
                imgAreaSide.sprite = ProPhotosSide[idx];

                if (isRev)
                {
                    if (swingStep == SWINGSTEP.TOP)
                    {
                        while (ani3DModel.GetFloat("SwingValue") <= 0.4f)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.10f * Time.deltaTime);
                            SetAniValue(setVal);
                            //elapsedTime += Time.deltaTime;
                            yield return null;
                            //Debug.Log($"MoveAnglePoint({swingStep}) PLY : {GetModelAngle()} -> {to}");
                        }
                    }
                    else
                    {
                        while (GetModelAngle() >= to)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.15f * Time.deltaTime);
                            SetAniValue(setVal);
                            //elapsedTime += Time.deltaTime;
                            yield return null;
                            //Debug.Log($"MoveAnglePoint({swingStep}) PLY : {GetModelAngle()} -> {to}");
                        }
                    }
                }
                else
                {
                    if (swingStep == SWINGSTEP.FINISH)
                    {
                        while (ani3DModel.GetFloat("SwingValue") <= 0.99f)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.20f * Time.deltaTime);
                            SetAniValue(setVal);
                            //elapsedTime += Time.deltaTime;
                            yield return null;
                            //Debug.Log($"MoveAnglePoint({swingStep}) PLY : {GetModelAngle()} -> {to}");
                        }
                    }/*
                    else if (swingStep == SWINGSTEP.IMPACT)
                    {
                        while (ani3DModel.GetFloat("SwingValue") <= impactAniValue)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.10f * Time.deltaTime);
                            SetAniValue(setVal);
                            //elapsedTime += Time.deltaTime;
                            yield return null;
                            //Debug.Log($"MoveAnglePoint({swingStep}) PLY : {GetModelAngle()} -> {to}");
                        }
                    }*/
                    else if (swingStep == SWINGSTEP.DOWNSWING)
                    {
                        while (GetModelAngle() <= to)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.10f * Time.deltaTime);
                            SetAniValue(setVal);
                            //elapsedTime += Time.deltaTime;
                            yield return null;
                            //Debug.Log($"MoveAnglePoint({swingStep}) PLY : {GetModelAngle()} -> {to}");
                        }
                    }
                    else
                    {
                        while (GetModelAngle() <= to)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.04f * Time.deltaTime);
                            SetAniValue(setVal);
                            //elapsedTime += Time.deltaTime;
                            yield return null;
                            //Debug.Log($"MoveAnglePoint({swingStep}) PLY : {GetModelAngle()} -> {to}");
                        }
                    }
                }
                
            }

            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            Debug.Log("MoveAnglePoint() Finish");
        }

        yield return null;

        isChanging = false;
    }

    IEnumerator MoveKeyPoint(SWINGSTEP swingStep, float coTime = 0.5f)
    {
        Debug.Log($"MoveKeyPoint({swingStep}) Start");
        yield return new WaitForSeconds(coTime);

        //float from = ani3DModel.GetFloat("SwingValue");
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
            to = 0.23f;
        }
        else if (swingStep == SWINGSTEP.BACKSWING)
        {
            from = 0.23f;
            to = 0.35f;
        }
        else if (swingStep == SWINGSTEP.TOP)
        {
            from = 0.35f;
            to = 0.5f;
        }
        else if (swingStep == SWINGSTEP.DOWNSWING)
        {
            from = 0.5f;
            to = 0.61f;
        }
        else if (swingStep == SWINGSTEP.IMPACT)
        {
            from = 0.61f;
            to = 0.661f;
        }
        else if (swingStep == SWINGSTEP.FOLLOW)
        {
            from = 0.661f;
            to = 0.76f;
        }
        else if (swingStep == SWINGSTEP.FINISH)
        {
            from = 0.76f;
            to = 0.99f;
        }
        else if (swingStep == SWINGSTEP.READY)
        {
            from = 0;
            to = 0;
        }

        if(Enum.IsDefined(typeof(SWINGSTEP), swingStep))
        {
            float elapsedTime = 0f;
            
            float time = swingStep == SWINGSTEP.FINISH ? 1f : 2f;

            int idx = (int)swingStep;
            if (idx < 0) idx = 0;
            imgAreaFront.sprite = ProPhotosFront[idx];
            imgAreaSide.sprite = ProPhotosSide[idx];

            while (elapsedTime < time)
            {
                ani3DModel.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));                
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            ani3DModel.SetFloat("SwingValue", to);
            Utillity.Instance.HideToast();
            Utillity.Instance.HideGuideArrow();

            if (EndEvent != null)
                EndEvent.Invoke();
            Debug.Log("MoveKeyPoint() Finish");
        }

        yield return null;

        isChanging = false;
    }

    IEnumerator MoveKeyPointExpert(SWINGSTEP swingStep, float coTime = 0.5f, int repeat = 0, bool isToast = false)
    {
        yield return new WaitForSeconds(coTime);

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
            for(int i = 0; i < repeat; i++)
            {
                float elapsedTime = 0f;

                float time = swingStep == SWINGSTEP.FINISH ? 1f : 2f;

                int idx = (int)swingStep;
                if (idx < 0) idx = 0;
                imgAreaFront.sprite = ProPhotosFront[idx];
                imgAreaSide.sprite = ProPhotosSide[idx];

                while (elapsedTime < time)
                {
                    ani3DModel.SetFloat("SwingValue", Mathf.Lerp(from, to, elapsedTime / time));
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                ani3DModel.SetFloat("SwingValue", to);

                if(!isToast)
                    Utillity.Instance.HideToast();

                Utillity.Instance.HideGuideArrow();

                yield return new WaitForSeconds(1.5f);
            }

            if (EndEvent != null)
                EndEvent.Invoke();

            Debug.Log("MoveKeyPointExpert() Finish");
        }

        yield return null;

        isChanging = false;
    }

    void SetAniValue(float setVal)
    {
        ani3DModel.SetFloat("SwingValue", setVal);
        //ani3DClub.SetFloat("SwingValue", setVal);
    }

    IEnumerator MoveAnglePointExpert(SWINGSTEP swingStep, float coTime = 0.5f, int repeat = 0, bool isToast = false)
    {
        Debug.Log($"MoveAnglePoint({swingStep}, {coTime}) Start");
        yield return new WaitForSeconds(coTime);

        //float from = ani3DModel.GetFloat("SwingValue");
        float from = GetModelAngle();
        float to = 0;
        bool isRev = true;

        if (swingStep == SWINGSTEP.ADDRESS)
        {
            to = swingStepData.dicAddress["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.TAKEBACK)
        {
            to = swingStepData.dicTakeback["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.BACKSWING)
        {
            to = swingStepData.dicBackswing["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.TOP)
        {
            to = swingStepData.dicTop["GetHandDir"];
            isRev = true;
        }
        else if (swingStep == SWINGSTEP.DOWNSWING)
        {
            to = swingStepData.dicDownswing["GetHandDir"];
            isRev = false;
        }
        else if (swingStep == SWINGSTEP.IMPACT)
        {
            //to = 180f;//임팩트 고정
            to = swingStepData.dicImpact["GetHandDir"];
            isRev = false;
        }
        else if (swingStep == SWINGSTEP.FOLLOW)
        {
            to = swingStepData.dicFollow["GetHandDir"];
            isRev = false;
        }
        else if (swingStep == SWINGSTEP.FINISH)
        {
            to = swingStepData.dicFinish["GetHandDir"];
            isRev = false;
        }

        else if (swingStep == SWINGSTEP.READY)
        {
            to = swingStepData.dicAddress["GetHandDir"];
            isRev = true;
        }

        if (sensorViewerFront != null && sensorViewerFront.gameObject.activeInHierarchy) sensorViewerFront.SetGetProValue(swingStep);
        if (sensorViewerSide != null && sensorViewerSide.gameObject.activeInHierarchy) sensorViewerSide.SetGetProValue(swingStep);

        Debug.Log($"MoveAnglePoint({swingStep}) SET : {GetModelAngle()} -> {to}");

        if (Enum.IsDefined(typeof(SWINGSTEP), swingStep))
        {
            for (int i = 0; i < repeat; i++)
            {
                if (swingStep == SWINGSTEP.ADDRESS || swingStep == SWINGSTEP.READY)
                {
                    SetAniValue(0);
                }
                else
                {
                    int idx = (int)swingStep;
                    if (idx < 0) idx = 0;
                    imgAreaFront.sprite = ProPhotosFront[idx];
                    imgAreaSide.sprite = ProPhotosSide[idx];


                    if (swingStep == SWINGSTEP.TOP)
                    {
                        while (ani3DModel.GetFloat("SwingValue") <= 0.45f)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.15f * Time.deltaTime);
                            SetAniValue(setVal);
                            yield return null;
                        }
                    }
                    else if (swingStep == SWINGSTEP.FINISH)
                    {
                        while (ani3DModel.GetFloat("SwingValue") <= 0.99f)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.15f * Time.deltaTime);
                            SetAniValue(setVal);
                            yield return null;
                        }
                    }
                    else if ((int)swingStep > (int)SWINGSTEP.TOP)
                    {
                        while (GetModelAngle() <= to || ani3DModel.GetFloat("SwingValue") < 0.45f)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.15f * Time.deltaTime);
                            SetAniValue(setVal);
                            yield return null;
                        }
                    }
                    else
                    {
                        while (GetModelAngle() >= to)
                        {
                            float setVal = ani3DModel.GetFloat("SwingValue") + (0.15f * Time.deltaTime);
                            SetAniValue(setVal);
                            yield return null;
                        }
                    }

                }

                //ani3DModel.SetFloat("SwingValue", 0);

                if (!isToast)
                    Utillity.Instance.HideToast();

                Utillity.Instance.HideGuideArrow();

                yield return new WaitForSeconds(1.5f);

                SetAniValue(0);

                yield return null;
            }

            

            if (EndEvent != null)
                EndEvent.Invoke();

            Debug.Log("MoveAnglePoint() Finish");
        }

        yield return null;

        isChanging = false;

    }

    public void LoadDebugPoseData(SWINGSTEP swingStep)
    {
        Utillity.Instance.DelayFunction(
            () =>
            {
                if (sensorViewerFront != null && sensorViewerFront.gameObject.activeInHierarchy) sensorViewerFront.SetGetProValue(swingStep);
                if (sensorViewerSide != null && sensorViewerSide.gameObject.activeInHierarchy) sensorViewerSide.SetGetProValue(swingStep);
            }, 0.1f);
        
    }
}
