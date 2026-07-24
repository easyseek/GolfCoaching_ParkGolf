using DG.Tweening;
using Enums;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoPlayerControl : MonoBehaviour
{
    [Header("* UI Components")]
    [SerializeField] LessonDirector m_FocusCoachingdirector;
    [SerializeField] VideoFInishPanel m_VideoFinishPanel;
    [SerializeField] GameObject ResultPanel;
    [SerializeField] GameObject PlayUI;

    [SerializeField] RectTransform TopPanel;
    [SerializeField] VLCVideoPlayer videoPlayer;
    [SerializeField] TextMeshProUGUI txtLessonTitle;
    [SerializeField] TextMeshProUGUI txtProName;
    [SerializeField] TextMeshProUGUI txtPstTime;
    [SerializeField] TextMeshProUGUI txtRmnTime;
    [SerializeField] TextMeshProUGUI txtSpeedNor;

    [SerializeField] TextMeshProUGUI txtSpeed2X;
    [SerializeField] TextMeshProUGUI txtSpeed4X;
    [SerializeField] TextMeshProUGUI txtRepeat;
    [SerializeField] Image imgPause;
    [SerializeField] Image imgPlay;
    [SerializeField] Slider SldVideoContorol;

    [SerializeField] GameObject TimeSliderPanel;
    [SerializeField] GameObject RepeatSliderPanel;
    [SerializeField] GameObject ViewrWebCam;

    [SerializeField] Toggle tglRepeat;

    [SerializeField] Slider SldRepeatStart;
    [SerializeField] Slider SldRepeatEnd;
    [SerializeField] TextMeshProUGUI txtRepeatInfo;
    [SerializeField] TextMeshProUGUI txtRepeatStart;
    [SerializeField] TextMeshProUGUI txtRepeatEnd;
    [SerializeField] RectTransform imgRepeatRange;

    [SerializeField] Color activeColor = Color.white;

    private ProVideoData proVideoData;

    bool _isPlay = false;
    bool _isRepeat = false;
    float[] playSpeed = {1f, 0.5f, 0.25f };
    bool _isVideoPrepared = false;
    bool _loopVideo = false;

    float _repeatStartValue = 0;
    float _repeatEndValue = 1f;
    double _repeatStartTime = 0;
    double _repeatEndTime = 1f;

    bool _isPrepare = false;
    bool _endHandled = false;
    bool _hasPlaybackProgress = false;
    long _lastPlaybackTimeMs = -1;
    float _lastPlaybackProgressAt;
    Coroutine _updateCoroutine;
    Coroutine _resumeSeekCoroutine;
    bool _hasRequestedSeek;
    float _requestedSeekPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tglRepeat.onValueChanged.AddListener(OnValueChanged_Repeat);
        //PlayVideo();
    }

    public void PlayVideo(string url = null, ProVideoData data = null)
    {
        if (!object.ReferenceEquals(data, null))
        {
            proVideoData = data;
        }

        if (!string.IsNullOrEmpty(url))
        {
            videoPlayer.url = url;
        }

        if (_isPrepare == false)
        {
            videoPlayer.isLooping = _loopVideo;
            videoPlayer.started += OnVideoStarted;
            videoPlayer.ended += OnVlcVideoEnded;
            _isPrepare = true;

            SldVideoContorol.onValueChanged.AddListener(OnValueChanged_SeekVideo);
            SldRepeatStart.onValueChanged.AddListener(OnValueChanged_RepeatStart);
            SldRepeatEnd.onValueChanged.AddListener(OnValueChanged_RepeatEnd);
        }

        ResetForNewVideo();
        OnClick_SetSpeed(0);

        m_FocusCoachingdirector.LessonState = ELessonState.Play;

        _isVideoPrepared = false;
        _endHandled = false;
        _hasPlaybackProgress = false;
        _lastPlaybackTimeMs = -1;
        _lastPlaybackProgressAt = Time.unscaledTime;
        StartCoroutine(BeginPlayback());

        if (_updateCoroutine == null)
        {
            _updateCoroutine = StartCoroutine(CoUpdate());
        }
    }

    void ResetForNewVideo()
    {
        if (_resumeSeekCoroutine != null)
        {
            StopCoroutine(_resumeSeekCoroutine);
            _resumeSeekCoroutine = null;
        }

        _isPlay = false;
        _isRepeat = false;
        _loopVideo = false;
        _hasRequestedSeek = false;
        _requestedSeekPosition = 0f;
        _repeatStartValue = 0f;
        _repeatEndValue = 1f;
        _repeatStartTime = 0d;
        _repeatEndTime = 0d;

        videoPlayer.isLooping = false;
        videoPlayer.position = 0f;

        m_VideoFinishPanel.gameObject.SetActive(false);
        ResultPanel.SetActive(false);
        PlayUI.SetActive(true);
        tglRepeat.SetIsOnWithoutNotify(false);
        SldVideoContorol.SetValueWithoutNotify(0f);
        SldRepeatStart.SetValueWithoutNotify(0f);
        SldRepeatEnd.SetValueWithoutNotify(1f);
        TimeSliderPanel.SetActive(true);
        RepeatSliderPanel.SetActive(false);
        txtRepeat.color = Color.white;
        txtRepeatInfo.gameObject.SetActive(false);
        txtRepeatStart.gameObject.SetActive(false);
        txtRepeatEnd.gameObject.SetActive(false);
        txtPstTime.text = "00:00";
        txtRmnTime.text = "00:00";
        imgPlay.enabled = false;
        imgPause.enabled = false;

        SetRepeatRangeFill();
    }

    private IEnumerator BeginPlayback()
    {
        yield return null;
        videoPlayer.Play();
    }

    public void StopVideo()
    {
        videoPlayer.Stop();
    }

    public void SetTitle(string strTitle)
    {
        txtLessonTitle.text = strTitle;
    }

    public void SetProName(string strProName)
    {
        txtProName.text = strProName;
    }

    IEnumerator CoUpdate()
    {
        while (true)
        {
            if (_isVideoPrepared)
            {
                if (_isRepeat && _isPlay)
                {
                    double currentTime = videoPlayer.time;
                    if (currentTime < _repeatStartTime || currentTime > _repeatEndTime)
                    {
                        videoPlayer.Pause();
                        yield return null;

                        videoPlayer.position = videoPlayer.length > 0
                            ? (float)((_repeatStartTime + 1d) / videoPlayer.length)
                            : 0f;
                        yield return null;

                        videoPlayer.Play();
                    }
                }

                UpdateTimeUI();
                UpdateTimeSlider();

                long currentTimeMs = videoPlayer.time;
                long lengthMs = videoPlayer.length;

                if (_lastPlaybackTimeMs < 0 || currentTimeMs < _lastPlaybackTimeMs ||
                    currentTimeMs > _lastPlaybackTimeMs + 20)
                {
                    if (currentTimeMs > 0)
                    {
                        _hasPlaybackProgress = true;
                    }

                    _lastPlaybackTimeMs = currentTimeMs;
                    _lastPlaybackProgressAt = Time.unscaledTime;
                }

                if (!_endHandled && !_isRepeat && _isPlay && _hasPlaybackProgress && lengthMs > 0)
                {
                    long remainingMs = lengthMs > currentTimeMs ? lengthMs - currentTimeMs : 0;
                    long endToleranceMs = (long)Mathf.Clamp(lengthMs * 0.01f, 500f, 2000f);
                    float stalledSeconds = Time.unscaledTime - _lastPlaybackProgressAt;

                    bool reportedEnd = videoPlayer.position >= 0.995f || remainingMs <= 100;
                    bool stalledNearEnd = remainingMs <= endToleranceMs && stalledSeconds >= 0.75f;
                    bool stoppedNearEnd = !videoPlayer.isPlaying && videoPlayer.position >= 0.98f &&
                        stalledSeconds >= 0.5f;

                    if (reportedEnd || stalledNearEnd || stoppedNearEnd)
                    {
                        Debug.Log($"[VideoPlayerControl] End detected. time={currentTimeMs}, length={lengthMs}, position={videoPlayer.position:F4}, playing={videoPlayer.isPlaying}");
                        OnVideoEnd();
                    }
                }
            }

            yield return null;
        }
    }

    void OnVideoStarted(VLCVideoPlayer vp)
    {
        _isVideoPrepared = true;
        _isPlay = true;
        imgPlay.enabled = false;
        imgPause.enabled = false;

        _lastPlaybackTimeMs = videoPlayer.time;
        _lastPlaybackProgressAt = Time.unscaledTime;

        txtRmnTime.text = FormatTime(videoPlayer.length / 1000f);
        _repeatEndTime = videoPlayer.length;
    }

    void OnVlcVideoEnded(VLCVideoPlayer vp)
    {
        if (!_isVideoPrepared || _isRepeat || _loopVideo || _endHandled)
        {
            return;
        }

        OnVideoEnd();
    }

    void OnVideoEnd()
    {
        if (_endHandled)
        {
            return;
        }

        _endHandled = true;

        if (_loopVideo)
        {
            videoPlayer.position = 0f;
            videoPlayer.Play();
            _endHandled = false;
        }
        else
        {
            Debug.Log("Video has ended.");
            _isPlay = false;
            videoPlayer.Stop();
            m_FocusCoachingdirector.LessonState = ELessonState.End;
            PlayUI.SetActive(false);
            ResultPanel.SetActive(true);
        }
    }

    public void OnClick_ResultRepeat()
    {
        if (proVideoData == null)
        {
            return;
        }

        PlayVideo(null, proVideoData);
    }

    public void OnClick_ResultList()
    {
        videoPlayer.Stop();
        ResultPanel.SetActive(false);
        PlayUI.SetActive(true);

        m_FocusCoachingdirector.ReturnToVideoList();
    }

    public void OnClick_ResultExit()
    {
        videoPlayer.Stop();
        GameManager.Instance.SelectedSceneName = string.Empty;
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnClick_SetSpeed(int SpdType)
    {
        videoPlayer.playbackSpeed = playSpeed[SpdType];
        txtSpeedNor.color = SpdType == 0 ? activeColor : Color.white;
        txtSpeed2X.color = SpdType == 1 ? activeColor : Color.white;
        txtSpeed4X.color = SpdType == 2 ? activeColor : Color.white;
    }

    public void OnClick_PlayStop()
    {
        //_isPlay = !_isPlay;

        if (_isPlay == false)
        {
            if(_isRepeat && txtRepeatInfo.gameObject.activeInHierarchy == true)
            {
                SetRepeat(false);
            }

            if(TopPanel.transform.localPosition.y == 1380.0f)
            {
                _isPlay = true;
                imgPlay.enabled = false;
                ResumeFromRequestedPosition();
            }
            else
            {
                imgPause.enabled = true;
                imgPlay.enabled = false;
                TopPanel.DOLocalMoveY(1380f, 0.3f).From(960f).OnComplete(
                () => {
                    _isPlay = true;
                    imgPause.enabled = false;
                    /*if (_isRepeat)
                    {
                        if (videoPlayer.time < _repeatStartTime || videoPlayer.time > _repeatEndTime)
                        {
                            SldVideoContorol.value = _repeatStartValue;
                        }
                    }*/
                    ResumeFromRequestedPosition();
                });
            }
        }
        else
        {
            _isPlay = false;
            imgPlay.enabled = true;
            TopPanel.DOLocalMoveY(960, 0.3f).From(1380f);
            videoPlayer.Pause();
        }
    }

    public void OnClick_TopPanel()
    {
        TopPanel.DOLocalMoveY(1380f, 0.3f).From(960f);
    }

    public void OnClick_Repeat()
    {
        if(_isRepeat == false)
        {
            SetRepeat(true);
        }
        else
        {
            SetRepeat(false);
        }
    }

    void SetRepeat(bool isRepeat)
    {
        _isRepeat = isRepeat;

        if (_isRepeat)
        {
            txtRepeat.color = activeColor;
            TimeSliderPanel.SetActive(false);
            RepeatSliderPanel.SetActive(true);

            txtRepeatInfo.gameObject.SetActive(true);
        }
        else
        {
            txtRepeat.color = Color.white;
            TimeSliderPanel.SetActive(true);
            RepeatSliderPanel.SetActive(false);

            txtRepeatInfo.gameObject.SetActive(false);
            txtRepeatStart.gameObject.SetActive(false);
            txtRepeatEnd.gameObject.SetActive(false);

            _repeatStartValue = 0;
            _repeatEndValue = 1f;
            _repeatStartTime = 0;
            _repeatEndTime = videoPlayer.length;
            SldRepeatStart.SetValueWithoutNotify(_repeatStartValue);
            SldRepeatEnd.SetValueWithoutNotify(_repeatEndValue);
            SetRepeatRangeFill();
        }
    }

    public void OnValueChanged_SeekVideo(float sliderValue)
    {
        if (!_isVideoPrepared)
        {
            return;
        }

        _requestedSeekPosition = Mathf.Clamp01(sliderValue);
        _hasRequestedSeek = true;
        videoPlayer.position = _requestedSeekPosition;
        _lastPlaybackTimeMs = videoPlayer.length > 0
            ? (long)(videoPlayer.length * _requestedSeekPosition)
            : -1;
        _lastPlaybackProgressAt = Time.unscaledTime;
    }

    public void OnValueChanged_Repeat(bool isOn)
    {
        _loopVideo = isOn;
        //videoPlayer.isLooping = isOn;
    }

    // 현재 시간 UI 업데이트
    void UpdateTimeUI()
    {
        txtPstTime.text = FormatTime(videoPlayer.time / 1000f);        
    }

    // 타임라인 슬라이더 업데이트
    void UpdateTimeSlider()
    {
        if (!_isVideoPrepared || videoPlayer.length <= 0)
        {
            return;
        }

        float playerPosition = videoPlayer.position;
        if (_hasRequestedSeek)
        {
            SldVideoContorol.SetValueWithoutNotify(_requestedSeekPosition);
            if (Mathf.Abs(playerPosition - _requestedSeekPosition) <= 0.01f)
            {
                _hasRequestedSeek = false;
            }

            return;
        }

        SldVideoContorol.SetValueWithoutNotify(playerPosition);
    }

    void ResumeFromRequestedPosition()
    {
        if (!_hasRequestedSeek)
        {
            videoPlayer.Resume();
            return;
        }

        float resumePosition = _requestedSeekPosition;
        if (_resumeSeekCoroutine != null)
        {
            StopCoroutine(_resumeSeekCoroutine);
        }

        _resumeSeekCoroutine = StartCoroutine(CoResumeFromPosition(resumePosition));
    }

    IEnumerator CoResumeFromPosition(float resumePosition)
    {
        videoPlayer.position = resumePosition;
        videoPlayer.Resume();

        for (int i = 0; i < 3; i++)
        {
            yield return null;
            videoPlayer.position = resumePosition;
        }

        _requestedSeekPosition = resumePosition;
        _hasRequestedSeek = false;
        _lastPlaybackTimeMs = videoPlayer.length > 0
            ? (long)(videoPlayer.length * resumePosition)
            : -1;
        _lastPlaybackProgressAt = Time.unscaledTime;
        _resumeSeekCoroutine = null;
    }

    
    public void OnValueChanged_RepeatStart(float sliderValue)
    {
        if (_isVideoPrepared)
        {
            if(txtRepeatInfo.gameObject.activeInHierarchy == true)
            {
                txtRepeatInfo.gameObject.SetActive(false);
                txtRepeatStart.gameObject.SetActive(true);
                txtRepeatEnd.gameObject.SetActive(true);
            }

            if (sliderValue > _repeatEndValue)
            {
                sliderValue = _repeatEndValue - 0.05f;
                SldRepeatStart.SetValueWithoutNotify(sliderValue);
            }

            _repeatStartTime = sliderValue * videoPlayer.length;
            _repeatStartValue = sliderValue;
            txtRepeatStart.text = FormatTime((float)(_repeatStartTime / 1000d));
            SetRepeatRangeFill();
        }        
    }

    public void OnValueChanged_RepeatEnd(float sliderValue)
    {
        if (_isVideoPrepared)
        {
            if (txtRepeatInfo.gameObject.activeInHierarchy == true)
            {
                txtRepeatInfo.gameObject.SetActive(false);
                txtRepeatStart.gameObject.SetActive(true);
                txtRepeatEnd.gameObject.SetActive(true);
            }

            if (_repeatStartValue > sliderValue)
            {
                sliderValue = _repeatStartValue + 0.05f;
                SldRepeatEnd.SetValueWithoutNotify(sliderValue);
            }

            _repeatEndTime = sliderValue * videoPlayer.length;
            _repeatEndValue = sliderValue;
            txtRepeatEnd.text = FormatTime((float)(_repeatEndTime / 1000d));
            SetRepeatRangeFill();
        }
    }

    void SetRepeatRangeFill()
    {
        imgRepeatRange.sizeDelta = new Vector2((_repeatEndValue - _repeatStartValue) * 800f, imgRepeatRange.sizeDelta.y);
        imgRepeatRange.anchoredPosition = new Vector2((_repeatStartValue * 400f) - (1-_repeatEndValue) * 400f, imgRepeatRange.anchoredPosition.y);
    }

    public void OnClick_CameraOff()
    {
        ViewrWebCam.SetActive(!ViewrWebCam.activeInHierarchy);
    }

    // 시간 포맷팅 (mm:ss)
    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void OnDestroy()
    {
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }

        if (_resumeSeekCoroutine != null)
        {
            StopCoroutine(_resumeSeekCoroutine);
            _resumeSeekCoroutine = null;
        }

        videoPlayer.started -= OnVideoStarted;
        videoPlayer.ended -= OnVlcVideoEnded;
    }
}
