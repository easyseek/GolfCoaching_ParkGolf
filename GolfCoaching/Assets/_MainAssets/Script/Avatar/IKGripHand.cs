using UnityEngine;

public class IKGripHand : MonoBehaviour
{
    public Transform leftHand;
    public Transform rightHand;

    private Animator animator;
    private int layerIndex_Club;

    void Awake()
    {
        animator = GetComponent<Animator>();
        layerIndex_Club = animator.GetLayerIndex("Hand");
    }

    private void OnAnimatorIK(int _layerIndex)
    {
        if (_layerIndex != layerIndex_Club)
        {
            return;
        }

        /*        
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0.35f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0.35f);

                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHand.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHand.rotation);
        */
        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0.7f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);

        animator.SetIKPosition(AvatarIKGoal.RightHand, rightHand.position);
        animator.SetIKRotation(AvatarIKGoal.RightHand, rightHand.rotation);

        //Debug.Log("OnAnimatorIK()");
    }
}
