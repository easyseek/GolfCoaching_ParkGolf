using DG.Tweening;
using UnityEngine;

public class LoadingController : MonoBehaviour
{
    public enum AnimationType
    {
        Bounce,
        Pulse,
        Wave
    }

    public AnimationType mode = AnimationType.Bounce;

    public RectTransform[] dots;

    public float T_up = 0.3f;
    public float T_hold = 0.5f;
    public float T_down = 0.3f;
    public float jumpHeight = 40f;

    private Vector2[] originalPositions;

    void Start()
    {
        DOTween.Init();

        foreach (var dot in dots)
            dot.DOKill();

        if (mode == AnimationType.Bounce)
            Invoke("PlayBounceMaster", 0.1f);
    }

    void PlayBounceMaster()
    {
        int count = dots.Length;
        float T_cycle = count * (T_up + T_hold) + T_down;

        originalPositions = new Vector2[count];

        for (int i = 0; i < count; i++)
            originalPositions[i] = dots[i].anchoredPosition;

        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < count; i++)
        {
            RectTransform dot = dots[i];
            Vector2 origin = originalPositions[i];
            float startTime = i * (T_up + T_hold);
            seq.Insert(startTime, dot.DOAnchorPosY(origin.y + jumpHeight, T_up).SetEase(Ease.OutSine));
            seq.Insert(startTime + T_up + T_hold, dot.DOAnchorPosY(origin.y, T_down).SetEase(Ease.InSine));
        }

        float dur = (float)seq.Duration();

        if (T_cycle > dur)
            seq.AppendInterval(T_cycle - dur);

        seq.SetLoops(-1, LoopType.Restart).SetUpdate(true).Play();
    }
}