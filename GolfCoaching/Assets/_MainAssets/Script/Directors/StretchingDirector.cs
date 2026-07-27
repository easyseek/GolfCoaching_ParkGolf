using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StretchingDirector : MonoBehaviour
{
    private const string VideoName = "Stretching_video.mp4";

    [SerializeField] private GameObject ready;
    [SerializeField] private GameObject result;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private GameObject viewerWebcam;
    [SerializeField] private Toggle repeatToggle;
    [SerializeField] private Toggle cameraToggle;
    [SerializeField] private Toggle swapToggle;
    [SerializeField] private WebcamTrackerController webcamCtrl;
    [SerializeField] private RawImage rawFront;
    [SerializeField] private RawImage rawSide;
    [SerializeField] private VLCVideoPlayer videoPlayer;
    [SerializeField] private RectTransform frontCameraView;
    [SerializeField] private RectTransform videoView;
    [SerializeField] private float countTime = 3f;
    [SerializeField] private float videoWaitTime = 5f;

    private bool videoReady;
    private bool videoLoaded;
    private bool isRepeat;
    private bool isCamOn = true;
    private bool isPlay;
    private bool holdVideo;
    private Coroutine countCo;
    private Coroutine endCo;
    private Coroutine readyCo;
    private ViewLayout frontCameraLayout;
    private ViewLayout videoLayout;
    private RectTransform viewCanvas;

    private struct ViewLayout
    {
        public Vector2 center;
        public Vector2 screenSize;
        public Vector2 sourceSize;
        public Vector3 sourceScale;
    }

    private void Start()
    {
        SaveViewLayouts();
        InitToggles();
        InitView();

        StartCoroutine(LoadVideo());
        countCo = StartCoroutine(CountReady());
    }

    private void InitToggles()
    {
        if (repeatToggle != null)
        {
            isRepeat = repeatToggle.isOn;
            repeatToggle.onValueChanged.RemoveListener(OnValueChanged_Repeat);
            repeatToggle.onValueChanged.AddListener(OnValueChanged_Repeat);
        }

        if (cameraToggle != null)
        {
            isCamOn = cameraToggle.isOn;
            cameraToggle.onValueChanged.RemoveListener(OnValueChanged_Camera);
            cameraToggle.onValueChanged.AddListener(OnValueChanged_Camera);
        }

        if (swapToggle != null)
        {
            swapToggle.onValueChanged.RemoveListener(OnValueChanged_Swap);
            swapToggle.onValueChanged.AddListener(OnValueChanged_Swap);
            ApplyViewSwap(swapToggle.isOn);
        }
    }

    private void InitView()
    {
        if (ready != null)
        {
            ready.SetActive(true);
        }

        if (result != null)
        {
            result.SetActive(false);
        }

        if (viewerWebcam != null)
        {
            viewerWebcam.SetActive(false);
        }

        isPlay = false;
        holdVideo = true;
        SetTracker(false);
        SetCameraView(isCamOn);

        if (countText != null)
        {
            countText.text = countTime.ToString("0");
        }
    }

    private IEnumerator LoadVideo()
    {
        videoReady = false;
        videoLoaded = false;

        if (videoPlayer == null)
        {
            videoReady = true;
            yield break;
        }

        string videoPath = GetVideoPath();
        if (!File.Exists(videoPath))
        {
            Debug.LogWarning($"[StretchingDirector] Video not found: {videoPath}");
            videoReady = true;
            yield break;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.url = videoPath;
        videoPlayer.started -= OnVideoStarted;
        videoPlayer.started += OnVideoStarted;

        yield return null;

        videoPlayer.Play();

        float timer = 0f;
        while (!videoReady && timer < videoWaitTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!videoReady)
        {
            Debug.LogWarning($"[StretchingDirector] Video prepare timeout: {videoPath}");
            videoReady = true;
        }
        else
        {
            videoLoaded = true;
            videoPlayer.position = 0f;
            PauseVideo();
        }
    }

    private IEnumerator CountReady()
    {
        float timer = countTime;

        while (timer > 0f)
        {
            HoldVideo();

            if (countText != null)
            {
                countText.text = Mathf.CeilToInt(timer).ToString();
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        if (countText != null)
        {
            countText.text = "0";
        }

        while (!videoReady)
        {
            HoldVideo();
            yield return null;
        }

        HoldVideo();
        StartStretching();
    }

    private void StartStretching()
    {
        if (ready != null)
        {
            ready.SetActive(false);
        }

        if (viewerWebcam != null)
        {
            viewerWebcam.SetActive(true);
        }

        isPlay = true;
        holdVideo = false;
        SetTracker(true);
        SetCameraView(isCamOn);

        if (videoPlayer != null && videoLoaded)
        {
            videoPlayer.position = 0f;
            videoPlayer.Play();

            if (endCo != null)
            {
                StopCoroutine(endCo);
            }

            endCo = StartCoroutine(CheckVideoEnd());
        }
    }

    public void OnClick_ModeSelect()
    {
        isPlay = false;
        holdVideo = false;
        SetTracker(false);
        SetCameraView(false);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        GameManager.Instance.SelectedSceneName = string.Empty;
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnClick_Replay()
    {
        if (endCo != null)
        {
            StopCoroutine(endCo);
            endCo = null;
        }

        if (countCo != null)
        {
            StopCoroutine(countCo);
            countCo = null;
        }

        if (readyCo != null)
        {
            StopCoroutine(readyCo);
            readyCo = null;
        }

        if (result != null)
        {
            result.SetActive(false);
        }

        isPlay = false;
        holdVideo = true;
        InitView();

        readyCo = StartCoroutine(ReadyVideo());
        countCo = StartCoroutine(CountReady());
    }

    private IEnumerator CheckVideoEnd()
    {
        bool hasProgress = false;

        while (videoPlayer != null && videoLoaded)
        {
            long length = videoPlayer.length;
            long time = videoPlayer.time;

            if (time > 500 || videoPlayer.position > 0.01f)
            {
                hasProgress = true;
            }

            bool nearEnd = length > 0 && hasProgress && (time >= length - 200 || videoPlayer.position >= 0.995f);
            bool stoppedEnd = hasProgress && !videoPlayer.isPlaying && videoPlayer.position >= 0.98f;

            if (nearEnd || stoppedEnd)
            {
                if (isRepeat)
                {
                    videoPlayer.position = 0f;
                    videoPlayer.Play();
                    hasProgress = false;
                }
                else
                {
                    ShowResult();
                    yield break;
                }
            }

            yield return null;
        }
    }

    public void OnValueChanged_Repeat(bool isOn)
    {
        isRepeat = isOn;

        if (videoPlayer != null)
        {
            videoPlayer.isLooping = false;
        }
    }

    public void OnValueChanged_Camera(bool isOn)
    {
        isCamOn = isOn;

        SetCameraView(isCamOn);
    }

    public void OnValueChanged_Swap(bool isOn)
    {
        ApplyViewSwap(isOn);
    }

    private void SaveViewLayouts()
    {
        Canvas.ForceUpdateCanvases();
        viewCanvas = videoView.parent as RectTransform;
        frontCameraLayout = CaptureViewLayout(frontCameraView);
        videoLayout = CaptureViewLayout(videoView);

        frontCameraView.SetParent(viewCanvas, true);
        videoView.SetParent(viewCanvas, true);
    }

    private ViewLayout CaptureViewLayout(RectTransform view)
    {
        Vector3[] corners = new Vector3[4];
        view.GetWorldCorners(corners);

        Vector2 bottomLeft = viewCanvas.InverseTransformPoint(corners[0]);
        Vector2 topRight = viewCanvas.InverseTransformPoint(corners[2]);
        ViewLayout layout = new ViewLayout();
        layout.center = (bottomLeft + topRight) * 0.5f;
        layout.screenSize = new Vector2(
            Mathf.Abs(topRight.x - bottomLeft.x),
            Mathf.Abs(topRight.y - bottomLeft.y));
        layout.sourceSize = view.rect.size;
        layout.sourceScale = view.localScale;

        return layout;
    }

    private void ApplyViewSwap(bool isSwapped)
    {
        ViewLayout targetFrontLayout = isSwapped ? videoLayout : frontCameraLayout;
        ViewLayout targetVideoLayout = isSwapped ? frontCameraLayout : videoLayout;

        ApplyViewLayout(frontCameraView, frontCameraLayout, targetFrontLayout);
        ApplyViewLayout(videoView, videoLayout, targetVideoLayout);

        if (isSwapped)
        {
            frontCameraView.SetAsFirstSibling();
            videoView.SetSiblingIndex(1);
        }
        else
        {
            videoView.SetAsFirstSibling();
            frontCameraView.SetSiblingIndex(1);
        }
    }

    private void ApplyViewLayout(RectTransform view, ViewLayout sourceLayout, ViewLayout targetLayout)
    {
        view.SetParent(viewCanvas, false);
        view.anchorMin = new Vector2(0.5f, 0.5f);
        view.anchorMax = new Vector2(0.5f, 0.5f);
        view.pivot = new Vector2(0.5f, 0.5f);
        view.sizeDelta = sourceLayout.sourceSize;
        view.anchoredPosition = targetLayout.center;

        float scaleX = targetLayout.screenSize.x / sourceLayout.screenSize.x;
        float scaleY = targetLayout.screenSize.y / sourceLayout.screenSize.y;
        view.localScale = new Vector3(
            sourceLayout.sourceScale.x * scaleX,
            sourceLayout.sourceScale.y * scaleY,
            sourceLayout.sourceScale.z);
    }

    private void ShowResult()
    {
        isPlay = false;
        holdVideo = false;
        SetTracker(false);
        SetCameraView(false);

        PauseVideo();

        if (result != null)
        {
            result.SetActive(true);
        }
    }


    private IEnumerator ReadyVideo()
    {
        if (videoPlayer == null || !videoLoaded)
        {
            yield break;
        }

        videoPlayer.position = 0f;
        yield return null;
        HoldVideo();

        videoPlayer.position = 0f;
        yield return null;
        HoldVideo();
    }

    private void HoldVideo()
    {
        if (!holdVideo || videoPlayer == null || !videoLoaded)
        {
            return;
        }

        videoPlayer.position = 0f;
        PauseVideo();
    }

    private void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
        }
    }

    private void OnVideoStarted(VLCVideoPlayer vp)
    {
        videoReady = true;

        if (videoPlayer != null)
        {
            videoPlayer.started -= OnVideoStarted;
        }
    }

    private void SetCameraView(bool active)
    {
        SetCamImage(rawFront, active);
        SetCamImage(rawSide, active);
    }

    private void SetTracker(bool active)
    {
        if (webcamCtrl != null)
        {
            webcamCtrl.SetTracker(active, active);
        }
    }

    private void SetCamImage(RawImage img, bool active)
    {
        if (img == null)
        {
            return;
        }

        img.gameObject.SetActive(active);

        Color color = img.color;
        color.a = active ? 1f : 0f;
        img.color = color;
        img.raycastTarget = false;
    }

    private string GetVideoPath()
    {
        string streamingPath = Path.Combine(Application.streamingAssetsPath, VideoName);
        if (File.Exists(streamingPath))
        {
            return streamingPath;
        }

        return Path.Combine(Application.dataPath, "Resources", VideoName);
    }
}
