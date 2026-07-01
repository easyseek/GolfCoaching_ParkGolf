using RootMotion.FinalIK;
using System.Collections;
using TMPro;
using UnityEngine;

public class IKTargetController : MonoBehaviour
{
    [Header("* MOCAP")]
    [SerializeField] ModelController modelController;
    [SerializeField] SensorProcess sensorProcess;
    //[SerializeField] mocapFront mcFront;
    //[SerializeField] mocapSide mcSide;

    [Header("* IK TARGETS")]
    [SerializeField] Transform IKTarget_PelvisRoot;
    [SerializeField] Transform IKTarget_Pelvis;
    [SerializeField] Transform IKTarget_SpineRoot;
    [SerializeField] Transform IKTarget_Spine;
    [SerializeField] Transform IKTarget_LeftFoot;
    [SerializeField] Transform IKTarget_RightFoot;
    [SerializeField] Transform IKTarget_RightForearm;
    [SerializeField] Transform IKTarget_LeftKnee;
    [SerializeField] Transform IKTarget_RightKnee;


    [Header("* IK REF OBJECTS(PRO)")]
    [SerializeField] Transform ProBone_Pelvis;
    [SerializeField] Transform ProBone_SpineB;
    [SerializeField] Transform ProBone_SpineT;
    [SerializeField] Transform ProBone_LeftFoot;
    [SerializeField] Transform ProBone_RightFoot;
    [SerializeField] Transform ProBone_LeftKnee;
    [SerializeField] Transform ProBone_RightKnee;
    [SerializeField] Transform ProBone_LeftForearm;
    [SerializeField] Transform ProBone_RightForearm;
    [SerializeField] Transform ProBone_LeftUpperarm;
    [SerializeField] Transform ProBone_RightUpperarm;
    [SerializeField] Transform ProBone_LeftThigh;
    [SerializeField] Transform ProBone_RightThigh;

    [Header("* IK REF OBJECTS(USER)")]
    [SerializeField] Transform UserBone_Pelvis;
    [SerializeField] Transform UserBone_SpineB;
    [SerializeField] Transform UserBone_SpineT;
    [SerializeField] Transform UserBone_LeftFoot;
    [SerializeField] Transform UserBone_RightFoot;
    [SerializeField] Transform userBone_RightShoulder;    
    [SerializeField] Transform userBone_RightForearm;
    [SerializeField] Transform userBone_LeftUpperarm;
    [SerializeField] Transform userBone_RightUpperarm;
    [SerializeField] Transform UserBone_LeftThigh;
    [SerializeField] Transform UserBone_RightThigh;

    [Header("* IK REF OBJECTS(FOOT)")]
    [SerializeField] Transform FootBone_LeftKnee;
    [SerializeField] Transform FootBone_RightKnee;
    [SerializeField] Transform FootBone_LeftFoot;
    [SerializeField] Transform FootBone_RightFoot;

    [Space(10)]
    [SerializeField] Transform Pro_S;
    [SerializeField] Transform Pro_P;
    [SerializeField] Transform Pro_F;
    [SerializeField] Transform Ik_S;
    [SerializeField] Transform Ik_P;
    [SerializeField] Transform Ik_F;
    //[SerializeField] Transform IMocap_S;
    //[SerializeField] Transform IMocap_P;
    //[SerializeField] Transform IMocap_F;
    
    [SerializeField] TextMeshProUGUI txtDebug;    
    [SerializeField] LineRenderer lineSpine_Pro;
    [SerializeField] LineRenderer lineSpine_User;
    [SerializeField] LineRenderer lineShoulder_Pro;
    [SerializeField] LineRenderer lineShoulder_User;
    [SerializeField] LineRenderer linePelvis_Pro;
    [SerializeField] LineRenderer linePelvis_User;

    BipedIK bipedIK;

    public bool isPause = false;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        IKTarget_PelvisRoot.position = ProBone_Pelvis.position;

        //FootIK Value
        SetFootIK();

        StartCoroutine(CoUpdate());
    }

    void SetFootIK()
    {
        //새로운 모델 추가 해서 FootBone_Name x4 설정필요
        if (TryGetComponent<BipedIK>(out bipedIK))
        {
            bipedIK.solvers.leftFoot.SetIKPositionWeight(1f);
            bipedIK.solvers.leftFoot.SetIKRotationWeight(1f);
            bipedIK.solvers.leftFoot.maintainRotationWeight = 0.5f;
            bipedIK.solvers.leftFoot.bendModifier = IKSolverLimb.BendModifier.Goal;
            bipedIK.solvers.leftFoot.bendModifierWeight = 0.25f;

            bipedIK.solvers.rightFoot.SetIKPositionWeight(1f);
            bipedIK.solvers.rightFoot.SetIKRotationWeight(1f);
            bipedIK.solvers.rightFoot.maintainRotationWeight = 0.5f;
            bipedIK.solvers.rightFoot.bendModifier = IKSolverLimb.BendModifier.Goal;
            bipedIK.solvers.rightFoot.bendModifierWeight = 0.25f;
        }

    }
    KalmanFilter[] kalmanFilter = new KalmanFilter[3] { new KalmanFilter() , new KalmanFilter() , new KalmanFilter()};
    IEnumerator CoUpdate()
    {
        while (true)
        {
            yield return null;

            yield return new WaitUntil(() => isPause == false);


            Pro_F.position = Utillity.Instance.GetCenter(ProBone_LeftFoot.position, ProBone_RightFoot.position);
            Pro_P.position = ProBone_Pelvis.position;
            Pro_S.position = ProBone_SpineT.position;

            if (!modelController.VisibleLock && modelController.IsCheck)
            {
                continue;
            }

            //발 고정
            //IKTarget_LeftFoot.position = ProBone_LeftFoot.position;
            //IKTarget_RightFoot.position = ProBone_RightFoot.position;
            IKTarget_LeftFoot.position = FootBone_LeftFoot.position;
            IKTarget_RightFoot.position = FootBone_RightFoot.position;
            IKTarget_LeftFoot.rotation = FootBone_LeftFoot.rotation;
            IKTarget_RightFoot.rotation = FootBone_RightFoot.rotation;
            Ik_F.position = Utillity.Instance.GetCenter(IKTarget_LeftFoot.position, IKTarget_RightFoot.position);

            //인식률 체크//
            if (modelController.VisibleLock)
            {
                IKTarget_PelvisRoot.position = ProBone_Pelvis.position;
                continue;
            }

            IKTarget_LeftKnee.position = FootBone_LeftKnee.position;
            IKTarget_RightKnee.position = FootBone_RightKnee.position;


            //골반
            //IKTarget_Pelvis.position = ProBone_Pelvis.position;

            Vector3 footCenter = Utillity.Instance.GetCenter(ProBone_LeftFoot.position, ProBone_RightFoot.position);
            Vector3 pelvisDir = ProBone_Pelvis.position - footCenter;
            pelvisDir.x = -(sensorProcess.fGetPelvisDir * 0.7f) * pelvisDir.magnitude;
            IKTarget_Pelvis.position = footCenter + pelvisDir;

            //오른팔 -> 잘못된 자세표현을 위한 강제 어깨높이
            //float len = Vector3.Distance(userBone_RightUpperarm.position , userBone_RightForearm.position) + 0.05f;
            //IKTarget_RightForearm.position = userBone_RightUpperarm.position + sensorProcess.vRightElbowDir * len;
            Vector3 errPos = userBone_RightForearm.position;
            errPos.z += 0.05f;
            errPos.x -= 0.2f;
            errPos.y += 0.2f;
            IKTarget_RightForearm.position = errPos;

            //상체
            /*Vector3 proSpine = ProBone_SpineT.position;
            proSpine.y += 0.05f;
            IKTarget_Spine.position = Vector3.Lerp(IKTarget_Spine.position, proSpine, 0.5f);
            */
        }

    }


    private Vector3 LimitPositionChange(Vector3 oldPosition, Vector3 newPosition, float maxChange)
    {
        Vector3 change = newPosition - oldPosition;
        float magnitude = change.magnitude;
        if (magnitude > maxChange)
        {
            change = change.normalized * maxChange;
        }
        return oldPosition + change;
    }
}
