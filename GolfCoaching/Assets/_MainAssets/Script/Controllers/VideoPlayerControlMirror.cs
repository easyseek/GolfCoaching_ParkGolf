using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class VideoPlayerControlMirror : MonoBehaviour
{
    [Header("VLC Video Players")]
    // [기존 VideoPlayer 대신 이전에 만든 VLCVideoPlayer 컴포넌트를 연결합니다]
    [SerializeField] VLCVideoPlayer vlcPlayerFront;
    [SerializeField] VLCVideoPlayer vlcPlayerSide;

    [Header("UI Controls")]
    [SerializeField] TextMeshProUGUI txtSpeedNor;
    [SerializeField] TextMeshProUGUI txtSpeed2X;
    [SerializeField] TextMeshProUGUI txtSpeed4X;
    [SerializeField] Image imgButtonPlay;
    [SerializeField] Sprite spPlay;
    [SerializeField] Sprite spPause;
    [SerializeField] Slider SldVideoContorol;
    [SerializeField] Color activeColor = Color.white;

    bool _isVideoPrepared;
    bool _isPlay;
    bool _isEnd;
    int _speed;

    bool _isDragging;
    bool _wasPlayingBeforeScrub;
    float _prevNormalized;

    // INI 클래스가 기존 프로젝트에 있다고 가정 (예: 1.0f, 0.5f, 0.25f 등)
    // 없을 경우를 대비해 하드코딩 예시값 처리 (기존 INI 상수가 있다면 그대로 사용하세요)
    float[] playSpeed = { 1.0f, 0.5f, 0.25f }; 

    void Start()
    {
        // VLC용 슬라이더 이벤트 및 포인터 등록
        SldVideoContorol.onValueChanged.AddListener(OnValueChanged_SeekVideo);
        RegisterSliderPointerEvents();

        // VLC가 첫 프레임을 성공적으로 렌더링하기 시작할 때를 준비 완료 시점으로 판정
        if (vlcPlayerFront != null)
        {
            vlcPlayerFront.started += OnVideoPreparedFront;
            //vlcPlayerFront.loopPointReached += OnVideoEnd;            
            vlcPlayerFront.progressBar = SldVideoContorol;
        }

        OnClick_SetSpeed(0); // 기본 배속 설정
    }

    void LateUpdate()
    {
        UpdateTimeSlider();
    }

    void RegisterSliderPointerEvents()
    {
        EventTrigger et = SldVideoContorol.GetComponent<EventTrigger>()
                       ?? SldVideoContorol.gameObject.AddComponent<EventTrigger>();
        AddTrigger(et, EventTriggerType.PointerDown, OnSliderPointerDown);
        AddTrigger(et, EventTriggerType.PointerUp,   OnSliderPointerUp);
        AddTrigger(et, EventTriggerType.Cancel,       OnSliderPointerUp);
    }

    void AddTrigger(EventTrigger et, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> cb)
    {
        var e = new EventTrigger.Entry { eventID = type };
        e.callback.AddListener(cb);
        et.triggers.Add(e);
    }

    void OnSliderPointerDown(BaseEventData data)
    {
        if (!_isVideoPrepared) return;
        _isDragging = true;
        _wasPlayingBeforeScrub = _isPlay;
        _prevNormalized = SldVideoContorol.normalizedValue;
        
        PauseBoth();
    }

    void OnSliderPointerUp(BaseEventData data)
    {
        _isDragging = false;
        bool wasPlaying = _wasPlayingBeforeScrub;
        _wasPlayingBeforeScrub = false;

        // 최종 드롭 위치 설정
        ApplySeekTarget(SldVideoContorol.normalizedValue);

        if (wasPlaying)
            PlayBoth();
    }

    public void OnValueChanged_SeekVideo(float value)
    {
        if (!_isDragging || !_isVideoPrepared) return;

        // VLC는 일시정지 상태에서 Position을 변경하면 해당 위치의 프레임이 즉시 화면에 갱신됩니다.
        ApplySeekTarget(value);
        _prevNormalized = value;
    }

    void ApplySeekTarget(float normalized)
    {
        float n = Mathf.Clamp01(normalized);
        
        if (vlcPlayerFront != null) vlcPlayerFront.position = n;
        if (vlcPlayerSide != null)  vlcPlayerSide.position = n;
    }

    void UpdateTimeSlider()
    {
        if (_isDragging) return;
        if (!_isVideoPrepared || vlcPlayerFront == null || vlcPlayerFront.length <= 0) return;
        
        // 정면 영상의 진행도를 기준으로 슬라이더 동기화
        SldVideoContorol.SetValueWithoutNotify(vlcPlayerFront.position);
    }

    public void PlayVideo(string urlFront = null, string urlSide = null)
    {
        if (vlcPlayerFront == null || vlcPlayerSide == null) return;

        if (!string.IsNullOrEmpty(urlFront)) vlcPlayerFront.url = urlFront;
        if (!string.IsNullOrEmpty(urlSide))  vlcPlayerSide.url = urlSide;

        // 두 플레이어 재생 시작
        vlcPlayerFront.Play();
        vlcPlayerSide.Play();

        // 배속 재적용
        OnClick_SetSpeed(_speed);

        _isPlay = true;
        _isEnd  = false;
        imgButtonPlay.sprite = spPause;
    }

    void OnVideoPreparedFront(VLCVideoPlayer player) 
    {
        _isVideoPrepared = true;
    }

    public double GetEndTime()  => vlcPlayerFront != null ? vlcPlayerFront.length / 1000.0d : 0d; // 초 단위 반환 유지 시
    public bool   GetPrepared() => _isVideoPrepared;
    public bool   isPlay()      => _isPlay;

    public void StopVideo()
    {
        _isPlay = false;
        imgButtonPlay.sprite = spPlay;
        PauseBoth();
    }

    public void ReleaseVIdeo()
    {
        if (vlcPlayerFront != null) vlcPlayerFront.Stop();
        if (vlcPlayerSide != null)  vlcPlayerSide.Stop();
    }

    void OnVideoEnd(VLCVideoPlayer player)
    {
        if (_isDragging) return;
        
        // 영상 끝 도달 시 처음으로 되돌려 교차 루프 시뮬레이션
        if (vlcPlayerFront != null) { vlcPlayerFront.Stop(); vlcPlayerFront.Play(); }
        if (vlcPlayerSide != null)  { vlcPlayerSide.Stop();  vlcPlayerSide.Play();  }
    }

    public void OnClick_SetSpeed(int SpdType)
    {
        if (vlcPlayerFront == null || vlcPlayerSide == null) return;

        vlcPlayerFront.playbackSpeed = playSpeed[SpdType];
        vlcPlayerSide.playbackSpeed  = playSpeed[SpdType];

        txtSpeedNor.color = SpdType == 0 ? activeColor : Color.white;
        txtSpeed2X.color  = SpdType == 1 ? activeColor : Color.white;
        txtSpeed4X.color  = SpdType == 2 ? activeColor : Color.white;

        _speed = SpdType;
    }

    public void OnClick_PlayStop()
    {
        if (vlcPlayerFront == null || vlcPlayerSide == null) return;

        // 현재 Front 재생기 기준으로 재생 상태를 판별하여 토글합니다.
        if (_isPlay)
        {
            PauseToggleBoth();
        }
    }

    void PlayBoth()  
    { 
        if (vlcPlayerFront != null) vlcPlayerFront.Play();  
        if (vlcPlayerSide != null)  vlcPlayerSide.Play();  
    }
    
    void PauseBoth() 
    { 
        if (vlcPlayerFront != null) vlcPlayerFront.Pause(); 
        if (vlcPlayerSide != null)  vlcPlayerSide.Pause(); 
    }

    void PauseToggleBoth() 
    { 
        if (vlcPlayerFront != null) vlcPlayerFront.TogglePause(); 
        if (vlcPlayerSide != null)  vlcPlayerSide.TogglePause(); 
    }
}