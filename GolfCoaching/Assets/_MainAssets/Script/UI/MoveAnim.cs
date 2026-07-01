using UnityEngine;
using DG.Tweening;

public class MoveAnim : MonoBehaviour
{
    // 0도 위쪽
    public float angle = 0f;
    
    public bool useAngle = true;
    
    public Vector3 customDirection = Vector3.up;

    public float moveDistance = 100f;
    public float duration = 1f;

    void Start()
    {
        Vector3 direction;
        if (useAngle)
        {
            float rad = (90f - angle) * Mathf.Deg2Rad;
            direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0);
        }
        else
        {
            direction = customDirection.normalized;
        }

        Vector3 targetPos = transform.localPosition + direction * moveDistance;

        transform.DOLocalMove(targetPos, duration).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}
