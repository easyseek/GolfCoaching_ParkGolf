using UnityEngine;

public class IKFoot : MonoBehaviour
{
    public Transform leftFootPosition;
    public Transform rightFootPosition;
    public Transform leftOrgFootBone;
    public Transform rightOrgFootBone;

    public Transform PelvisPosition;

    private Animator animator;
    private int layerIndex;

    void Awake()
    {
        animator = GetComponent<Animator>();
        //layerIndex = animator.GetLayerIndex("Hand");
    }

    private void OnAnimatorIK(int _layerIndex)
    {
        /*
        if (_layerIndex != layerIndex_Club)
        {
            return;
        }=
        */
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);        

        Vector3 lPos = leftOrgFootBone.position;
        Vector3 rPos = rightOrgFootBone.position;
        //lPos.x = leftFootBone.position.x;
        //rPos.x = rightFootBone.position.x;

        animator.SetIKPosition(AvatarIKGoal.LeftFoot, lPos);
        animator.SetIKPosition(AvatarIKGoal.RightHand, rPos);

        //Debug.Log("OnAnimatorIK()");
    }
}
