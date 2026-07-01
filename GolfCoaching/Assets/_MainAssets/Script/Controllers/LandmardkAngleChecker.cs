using UnityEngine;

public class LandmardkAngleChecker : MonoBehaviour
{
    [SerializeField] Transform LeftHand;
    [SerializeField] Transform RightHand;

    [SerializeField] Transform LeftPelvis;
    [SerializeField] Transform RightPelvis;

    [SerializeField] Transform LeftShoulder;
    [SerializeField] Transform RightShoulder;

    [SerializeField] Transform LeftFoot;
    [SerializeField] Transform RightFoot;

    [SerializeField] Transform Head;

    //어께 회전
    public float GetShoulderDir()
    {
        // 골반의 중앙을 기준으로 하는 벡터 계산
        Vector3 PelvisVector = RightPelvis.position - LeftPelvis.position;
        Vector3 shoulderVector = RightShoulder.position - LeftShoulder.position;

        // 어깨 벡터와 골반 벡터 간의 회전 각도 계산
        float angle = Vector3.SignedAngle(PelvisVector, shoulderVector, Vector3.forward);
        //calKinetic.ShoulderValue = angle / 180f;
        return angle / 180f;
    }

    //골반 회전
    public float GetPelvisDir()
    {
        // 양발의 중앙을 기준으로 하는 벡터 계산
        Vector3 FootVector = RightFoot.position - LeftFoot.position;
        Vector3 PelvisVector = RightPelvis.position - LeftPelvis.position;

        // 골반 벡터와 양발 벡터 간의 회전 각도 계산
        float angle = Vector3.SignedAngle(FootVector, PelvisVector, Vector3.forward);
        //calKinetic.PelvisValue = angle / 180f;
        return angle / 180f;
    }

    //스윙 회전각
    public float GetHandDir()
    {
        // 어꺠중심과 손중심을 기준
        Vector3 shoulderVector = (RightShoulder.position + LeftShoulder.position) / 2;
        Vector3 handVector = (RightHand.position + LeftHand.position) / 2;
        Vector3 dir = handVector - shoulderVector;
        dir.z = 0;

        // 어깨 벡터와 손 벡터 간의 각도 계산
        return Quaternion.FromToRotation(Vector3.up, dir).eulerAngles.z;
    }
}
