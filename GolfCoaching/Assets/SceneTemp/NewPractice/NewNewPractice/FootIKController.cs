using UnityEngine;

public class FootIKController : MonoBehaviour
{
    public Animator animator;
    public float distanceGround = 0.1f;
    public LayerMask layerMask;

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            // Left Foot
            animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);

            Ray leftRay = new Ray(animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);

            if (Physics.Raycast(leftRay, out RaycastHit leftHit, distanceGround + 1f, layerMask))
            {
                if (leftHit.transform.tag == "WalkableGround")
                {
                    Vector3 footPosition = leftHit.point;
                    footPosition.y += distanceGround;

                    animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
                    animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, leftHit.normal));
                }
            }

            // Right Foot
            animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

            Ray rightRay = new Ray(animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);

            if (Physics.Raycast(rightRay, out RaycastHit rightHit, distanceGround + 1f, layerMask))
            {
                if (rightHit.transform.tag == "WalkableGround")
                {
                    Vector3 footPosition = rightHit.point;
                    footPosition.y += distanceGround;

                    animator.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
                    animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, rightHit.normal));
                }
            }
        }
    }
}
