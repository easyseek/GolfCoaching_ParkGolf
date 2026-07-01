using UnityEngine;

public class IKDirector : MonoBehaviour
{
    #region Singleton
    public static IKDirector Instance;
    private static object syncRootObject = new object();
    private void SingletonManager()
    {
        if (Instance == null)
        {
            lock (syncRootObject)
            {
                if (Instance == null)
                {
                    Instance = this;
                }
            }
        }
        else
        {
            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
    }
    #endregion

    public Animator animator;

    void Start()
    {
        SingletonManager();
        animator = GetComponent<Animator>();
    }

    public void UpdateBodyPositions(Vector3[] positions)
    {
        if (positions.Length < 33 || animator == null)
        {
            Debug.LogError("포지션 배열이 너무 짧거나 애니메이터가 없습니다.");
            return;
        }

        UpdateBonePosition(HumanBodyBones.RightHand, positions[16]);
        UpdateBonePosition(HumanBodyBones.RightLowerArm, positions[14]);
        UpdateBonePosition(HumanBodyBones.RightUpperArm, positions[12]);
        UpdateBonePosition(HumanBodyBones.LeftHand, positions[15]);
        UpdateBonePosition(HumanBodyBones.LeftLowerArm, positions[13]);
        UpdateBonePosition(HumanBodyBones.LeftUpperArm, positions[11]);
        UpdateBonePosition(HumanBodyBones.RightUpperLeg, positions[24]);
        UpdateBonePosition(HumanBodyBones.RightLowerLeg, positions[26]);
        UpdateBonePosition(HumanBodyBones.RightFoot, positions[28]);
        UpdateBonePosition(HumanBodyBones.RightToes, positions[32]);
        UpdateBonePosition(HumanBodyBones.LeftUpperLeg, positions[23]);
        UpdateBonePosition(HumanBodyBones.LeftLowerLeg, positions[25]);
        UpdateBonePosition(HumanBodyBones.LeftFoot, positions[27]);
        UpdateBonePosition(HumanBodyBones.LeftToes, positions[31]);

        Vector3 hipsPosition = (positions[24] + positions[23]) / 2;
        UpdateBonePosition(HumanBodyBones.Hips, hipsPosition);
    }

    private void UpdateBonePosition(HumanBodyBones bone, Vector3 newPosition)
    {
        Transform boneTransform = animator.GetBoneTransform(bone);
        if (boneTransform != null)
        {
            boneTransform.position = newPosition;
        }
    }
}