using DG.Tweening;
using UnityEngine;

public class UILoadingDot : MonoBehaviour
{
    Sequence sqLoadingDot;
    [SerializeField] Transform[] tDots;

    [SerializeField] float StartY;
    [SerializeField] float EndY;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Awake()
    {
        if (tDots.Length != 4)
            Debug.LogError("Error Init!!");

        tDots[0].localPosition = new Vector2(tDots[0].localPosition.x, StartY);
        tDots[1].localPosition = new Vector2(tDots[1].localPosition.x, StartY);
        tDots[2].localPosition = new Vector2(tDots[2].localPosition.x, StartY);
        tDots[3].localPosition = new Vector2(tDots[3].localPosition.x, EndY);
    }

    void Start()
    {
        sqLoadingDot = DOTween.Sequence()
            .SetAutoKill(false).SetLoops(-1, LoopType.Restart)
            .Append(tDots[0].DOLocalMoveY(EndY, 0.5f)).Join(tDots[3].DOLocalMoveY(StartY, 0.5f))
            .Append(tDots[1].DOLocalMoveY(EndY, 0.5f)).Join(tDots[0].DOLocalMoveY(StartY, 0.5f))
            .Append(tDots[2].DOLocalMoveY(EndY, 0.5f)).Join(tDots[1].DOLocalMoveY(StartY, 0.5f))
            .Append(tDots[3].DOLocalMoveY(EndY, 0.5f)).Join(tDots[2].DOLocalMoveY(StartY, 0.5f));
        sqLoadingDot.Play();
    }
}
