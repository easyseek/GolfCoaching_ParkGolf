using UnityEngine;

public class ModelPoseController : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] string _aniName;

    [SerializeField] Transform Bone_Chest;
    [SerializeField] Transform Bone_Pelvis;
    Quaternion Bone_ChestRotation;
    Quaternion Bone_PelvisRotation;

    [SerializeField] bool CoachingPose = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        _animator.Play(_aniName);

        foreach(var clip in _animator.GetCurrentAnimatorClipInfo(0))
        {
            Debug.Log($"{clip.clip.name}");
        }
    }


    void OnAnimatorIK(int layerIndex)
    {
        if (!CoachingPose)
        {
            SetChest();
            SetPelvis();
        }
    }

    void SetChest()
    {
        _animator.SetBoneLocalRotation(HumanBodyBones.UpperChest, Bone_ChestRotation);
    }
    public void SetChest(Quaternion value)
    {
        Bone_ChestRotation = value;
        
    }

    void SetPelvis()
    {
        Bone_Pelvis.localRotation = Bone_PelvisRotation;
    }

    public void SetPelvis(Quaternion value)
    {
        Bone_PelvisRotation = value;

    }
}
