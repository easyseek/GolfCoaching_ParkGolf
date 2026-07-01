using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HandPoseCopy : MonoBehaviour
{
    [SerializeField] Transform SourceHandLeft;
    [SerializeField] Transform SourceHandRight;
    [SerializeField] Transform TargetHandLeft;    
    [SerializeField] Transform TargetHandRight;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1);
        MatchTransforms();
    }

    public void MatchTransforms()
    {
        try
        {
            Transform[] SourceHandsL = GetAllChildTransforms(SourceHandLeft);
            Transform[] TargetHandsL = GetAllChildTransforms(TargetHandLeft);
            Transform[] SourceHandsR = GetAllChildTransforms(SourceHandRight);
            Transform[] TargetHandsR = GetAllChildTransforms(TargetHandRight);

            for (int i = 0; i < SourceHandsL.Length; i++)
            {
                TargetHandsL[i].localPosition = SourceHandsL[i].localPosition;
                TargetHandsL[i].localRotation = SourceHandsL[i].localRotation;
                TargetHandsR[i].localPosition = SourceHandsR[i].localPosition;
                TargetHandsR[i].localRotation = SourceHandsR[i].localRotation;
            }
        }
        catch { }
    }

    public Transform[] GetAllChildTransforms(Transform parent)
    {
        List<Transform> childTransforms = new List<Transform>();
        CollectChildTransforms(parent, childTransforms);
        return childTransforms.ToArray();
    }

    private void CollectChildTransforms(Transform parent, List<Transform> childTransforms)
    {
        foreach (Transform child in parent)
        {
            childTransforms.Add(child);
            CollectChildTransforms(child, childTransforms);
        }
    }

}
