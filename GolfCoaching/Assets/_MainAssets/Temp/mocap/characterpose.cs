using UnityEngine;

public class characterpose : MonoBehaviour
{
    // Singleton 코드는 그대로 유지
    #region Singleton
    public static characterpose Instance;
    private static object syncRootObject = new object();
    private void SingletonManager()
    {
        if (Instance == null)
        {
            lock (syncRootObject)
            {
                if (Instance == null) // Double-check locking
                {
                    Instance = this;
                    //DontDestroyOnLoad(gameObject);
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
    public Vector3[] bodyPositions = new Vector3[17];
    public Vector3[] targetBodyPositions = new Vector3[17];
    public GameObject bodyCenter;

    [Range(0, 1)]
    public float ikWeight = 1f;
    [Range(0, 1)]
    public float smoothness = 0.1f;

    void Start()
    {
        SingletonManager();
        animator = GetComponent<Animator>();
    }

    public void UpdateBodyPositions(Vector3[] positions)
    {
        if (positions.Length < 17 || animator == null)
        {
            Debug.LogError("포지션 배열이 너무 짧거나 애니메이터가 없습니다.");
            return;
        }
        targetBodyPositions = positions;
    }

    private void Update()
    {
        for (int i = 0; i < bodyPositions.Length; i++)
        {
            bodyPositions[i] = Vector3.Lerp(bodyPositions[i], targetBodyPositions[i], smoothness);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        UpdateBodyParts();
    }

    private void UpdateBodyParts()
    {
        if (animator == null || bodyPositions == null || bodyPositions.Length < 17)
        {
            return;
        }

        // 전체 바디 포지션 업데이트
        Vector3 rootPosition = (bodyPositions[24] + bodyPositions[23]) / 2;
        animator.bodyPosition = Vector3.Lerp(animator.bodyPosition, rootPosition, ikWeight);


        // 상체 회전 설정
        SetUpperBodyRotation(bodyPositions[11], bodyPositions[12], bodyPositions[23], bodyPositions[24]);

        // 머리 IK 설정
        SetHeadIK(bodyPositions[0]);

        // 목 IK 설정
        SetNeckIK(bodyPositions[1]);

        // 척추 IK 설정
        SetSpineIK(bodyPositions[7], bodyPositions[8]);

        // 가슴 IK 설정
        SetChestIK(bodyPositions[6]);

        // 어깨 위치 및 회전 설정
        SetShoulderTransform(HumanBodyBones.RightShoulder, bodyPositions[11], bodyPositions[12]);
        SetShoulderTransform(HumanBodyBones.LeftShoulder, bodyPositions[12], bodyPositions[11]);

        // 팔 IK 설정
        SetArmIK(AvatarIKGoal.RightHand, bodyPositions[15], bodyPositions[13]);
        SetArmIK(AvatarIKGoal.LeftHand, bodyPositions[16], bodyPositions[14]);

        // 골반 IK 설정
        SetHipIK(bodyPositions[24], bodyPositions[23]);

        // 다리 IK 설정
        SetLegIK(AvatarIKGoal.RightFoot, bodyPositions[27], bodyPositions[25]);
        SetLegIK(AvatarIKGoal.LeftFoot, bodyPositions[28], bodyPositions[26]);

        // 발 IK 설정
        SetFootIK(AvatarIKGoal.RightFoot, bodyPositions[31]);
        SetFootIK(AvatarIKGoal.LeftFoot, bodyPositions[32]);
    }
    private void SetUpperBodyRotation(Vector3 rightShoulder, Vector3 leftShoulder, Vector3 rightHip, Vector3 leftHip)
    {
        Vector3 shoulderCenter = (rightShoulder + leftShoulder) / 2;
        Vector3 hipCenter = (rightHip + leftHip) / 2;
        Vector3 spineDirection = (shoulderCenter - hipCenter).normalized;
        Vector3 chestDirection = (leftShoulder - rightShoulder).normalized;

        Quaternion spineRotation = Quaternion.LookRotation(Vector3.Cross(spineDirection, chestDirection), spineDirection);

        animator.bodyRotation = Quaternion.Slerp(animator.bodyRotation, spineRotation, ikWeight);

        Transform chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chestTransform != null)
        {
            Quaternion chestRotation = Quaternion.LookRotation(Vector3.Cross(chestDirection, spineDirection), spineDirection);
            chestTransform.rotation = Quaternion.Slerp(chestTransform.rotation, chestRotation, ikWeight);
        }
    }

    private void SetHipIK(Vector3 rightHipPosition, Vector3 leftHipPosition)
    {
        Vector3 hipCenter = (rightHipPosition + leftHipPosition) / 2;
        Transform hipTransform = animator.GetBoneTransform(HumanBodyBones.Hips);
        if (hipTransform != null)
        {
            hipTransform.position = Vector3.Lerp(hipTransform.position, hipCenter, ikWeight);

            Vector3 hipDirection = (leftHipPosition - rightHipPosition).normalized;
            Quaternion hipRotation = Quaternion.LookRotation(Vector3.forward, hipDirection);
            hipTransform.rotation = Quaternion.Slerp(hipTransform.rotation, hipRotation, ikWeight);
        }
    }

    private void SetHeadIK(Vector3 headPosition)
    {
        Transform headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform neckTransform = animator.GetBoneTransform(HumanBodyBones.Neck);
        if (headTransform != null && neckTransform != null)
        {
            Vector3 headDirection = (headPosition - neckTransform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(headDirection, transform.up);

            headTransform.position = Vector3.Lerp(headTransform.position, headPosition, ikWeight);
            headTransform.rotation = Quaternion.Slerp(headTransform.rotation, targetRotation, ikWeight);
        }
    }

    private void SetNeckIK(Vector3 neckPosition)
    {
        Transform neckTransform = animator.GetBoneTransform(HumanBodyBones.Neck);
        if (neckTransform != null)
        {
            neckTransform.position = Vector3.Lerp(neckTransform.position, neckPosition, ikWeight);
        }
    }

    private void SetSpineIK(Vector3 upperSpinePosition, Vector3 lowerSpinePosition)
    {
        Transform spineTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
        if (spineTransform != null)
        {
            Vector3 spineCenter = (upperSpinePosition + lowerSpinePosition) / 2;
            spineTransform.position = Vector3.Lerp(spineTransform.position, spineCenter, ikWeight);

            Vector3 spineDirection = (upperSpinePosition - lowerSpinePosition).normalized;
            Vector3 forwardDirection = Vector3.Cross(spineDirection, transform.right);

            if (forwardDirection != Vector3.zero)
            {
                Quaternion spineRotation = Quaternion.LookRotation(forwardDirection, spineDirection);
                spineTransform.rotation = Quaternion.Slerp(spineTransform.rotation, spineRotation, ikWeight);
            }
        }
    }

    private void SetChestIK(Vector3 chestPosition)
    {
        Transform chestTransform = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chestTransform != null)
        {
            chestTransform.position = Vector3.Lerp(chestTransform.position, chestPosition, ikWeight);
        }
    }

    private void SetShoulderTransform(HumanBodyBones bone, Vector3 shoulderPosition, Vector3 otherShoulderPosition)
    {
        Transform shoulderTransform = animator.GetBoneTransform(bone);
        if (shoulderTransform != null)
        {
            shoulderTransform.position = Vector3.Lerp(shoulderTransform.position, shoulderPosition, ikWeight);
            Vector3 shoulderDirection = (shoulderPosition - otherShoulderPosition).normalized;
            shoulderTransform.rotation = Quaternion.LookRotation(Vector3.forward, shoulderDirection);
        }
    }
    private void SetFist(bool isRightHand)
    {
        // 손가락 본 정의
        HumanBodyBones[] fingerBones = {
        isRightHand ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal,
        isRightHand ? HumanBodyBones.RightThumbIntermediate : HumanBodyBones.LeftThumbIntermediate,
        isRightHand ? HumanBodyBones.RightThumbDistal : HumanBodyBones.LeftThumbDistal,
        isRightHand ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal,
        isRightHand ? HumanBodyBones.RightIndexIntermediate : HumanBodyBones.LeftIndexIntermediate,
        isRightHand ? HumanBodyBones.RightIndexDistal : HumanBodyBones.LeftIndexDistal,
        isRightHand ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal,
        isRightHand ? HumanBodyBones.RightMiddleIntermediate : HumanBodyBones.LeftMiddleIntermediate,
        isRightHand ? HumanBodyBones.RightMiddleDistal : HumanBodyBones.LeftMiddleDistal,
        isRightHand ? HumanBodyBones.RightRingProximal : HumanBodyBones.LeftRingProximal,
        isRightHand ? HumanBodyBones.RightRingIntermediate : HumanBodyBones.LeftRingIntermediate,
        isRightHand ? HumanBodyBones.RightRingDistal : HumanBodyBones.LeftRingDistal,
        isRightHand ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal,
        isRightHand ? HumanBodyBones.RightLittleIntermediate : HumanBodyBones.LeftLittleIntermediate,
        isRightHand ? HumanBodyBones.RightLittleDistal : HumanBodyBones.LeftLittleDistal
    };

        // 각 손가락 관절을 구부립니다
        foreach (HumanBodyBones bone in fingerBones)
        {
            Transform fingerBone = animator.GetBoneTransform(bone);
            if (fingerBone != null)
            {
                // 손가락을 구부리는 각도 (이 값을 조정하여 원하는 모양을 만들 수 있습니다)
                float bendAngle = 60f;
                fingerBone.localRotation = Quaternion.Euler(bendAngle, 0, 0);
            }
        }
    }
    private void SetArmIK(AvatarIKGoal goal, Vector3 handPosition, Vector3 elbowPosition)
    {
        animator.SetIKPositionWeight(goal, ikWeight);
        animator.SetIKRotationWeight(goal, ikWeight);

        // 손 위치를 팔의 위치로 설정
        animator.SetIKPosition(goal, handPosition);

        // 손목 방향을 기준으로 손의 회전 설정
        Transform handBone = (goal == AvatarIKGoal.RightHand) ?
            animator.GetBoneTransform(HumanBodyBones.RightHand) :
            animator.GetBoneTransform(HumanBodyBones.LeftHand);

        if (handBone != null)
        {
            Vector3 wristDirection = handPosition - elbowPosition;
            if (wristDirection != Vector3.zero)
            {
                Quaternion handRotation = Quaternion.LookRotation(wristDirection, Vector3.up);
                animator.SetIKRotation(goal, handRotation);
            }

            // 주먹 쥐기 설정
            SetFist(goal == AvatarIKGoal.RightHand);
        }

        AvatarIKHint hint = (goal == AvatarIKGoal.RightHand) ? AvatarIKHint.RightElbow : AvatarIKHint.LeftElbow;
        animator.SetIKHintPositionWeight(hint, ikWeight);
        animator.SetIKHintPosition(hint, elbowPosition);
    }

    private void SetLegIK(AvatarIKGoal goal, Vector3 footPosition, Vector3 kneePosition)
    {
        animator.SetIKPositionWeight(goal, ikWeight);
        animator.SetIKRotationWeight(goal, ikWeight);
        animator.SetIKPosition(goal, footPosition);

        AvatarIKHint hint = (goal == AvatarIKGoal.RightFoot) ? AvatarIKHint.RightKnee : AvatarIKHint.LeftKnee;
        animator.SetIKHintPositionWeight(hint, ikWeight);
        animator.SetIKHintPosition(hint, kneePosition);
    }

    private void SetFootIK(AvatarIKGoal goal, Vector3 toePosition)
    {
        Vector3 footPosition = animator.GetIKPosition(goal);
        Vector3 footDirection = toePosition - footPosition;
        if (footDirection != Vector3.zero)
        {
            Quaternion footRotation = Quaternion.LookRotation(footDirection, Vector3.up);
            animator.SetIKRotation(goal, footRotation);
        }
    }
}

