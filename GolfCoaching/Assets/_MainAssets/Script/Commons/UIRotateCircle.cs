using UnityEngine;
using DG.Tweening;

public class UIRotateCircle : MonoBehaviour
{
    [SerializeField] Transform TargetObject;
    [SerializeField] float duration;
    [SerializeField] bool isClockwise;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TargetObject.DORotate(new Vector3(0, 0, isClockwise? -90 : 90), duration).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
    }
}
