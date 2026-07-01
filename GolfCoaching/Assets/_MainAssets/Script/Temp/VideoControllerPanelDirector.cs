using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Unity.Burst;
using System.Collections;
[BurstCompile]


public class VideoControllerPanelDirector : MonoBehaviour//SerializedMonoBehaviour, ISerializationCallbackReceiver
{
    [Header("비디오 컨트롤러 패널")]
    /// <summary>
    ///비디오 컨트롤러 패널
    /// </summary>
    [SerializeField] private GameObject videoControllerPanel;

    [Header("닫기 버튼")]
    /// <summary>
    ///닫기 버튼
    /// </summary>
    [SerializeField] private Button CloseButton;

    private void Awake()
    {
    }

    private void Start()
    {
        CloseButton.onClick.AddListener(CloseButtonFunction);
        void CloseButtonFunction()
        {
            //Timing.RunCoroutine(coroutine: VideoControllerPanelHide());
            StartCoroutine(VideoControllerPanelHide());
        }
    }

    /// <summary>
    ///비디오 나타나는 애니메이션
    /// </summary>
    public IEnumerator VideoControllerPanelShow()
    {
        yield return null;
        videoControllerPanel.transform.DOLocalMoveY(750, 0.5f);
    }

    /// <summary>
    ///비디오 사라지는 애니메이션
    /// </summary>
    public IEnumerator VideoControllerPanelHide()
    {
        yield return null;
        videoControllerPanel.transform.DOLocalMoveY(1200, 0.5f);
    }

}
