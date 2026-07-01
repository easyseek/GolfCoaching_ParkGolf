using UnityEngine;
using UnityEngine.UI; // [추가] RawImage 제어를 위해 필수가 가동되어야 합니다.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

public class VLCVideoPlayer : MonoBehaviour
{
    [Header("Video Settings")]
    [Tooltip("재생할 동영상의 URL 또는 로컬 파일 경로")]
    public string url;
    public bool playOnAwake = true;
    public bool isLooping = false;

    [Header("Display Target")]
    [Tooltip("영상이 출력될 UI RawImage를 여기에 드래그앤드롭 하세요")]
    public RawImage targetDisplay; // <── [여기 할당 칸이 드디어 생겼습니다!]

    // --- 유니티 기본 VideoPlayer와 동일한 핵심 프로퍼티 ---
    public bool isPlaying => _mediaPlayer != null && _mediaPlayer.IsPlaying;
    public float playbackSpeed
    {
        get => _mediaPlayer != null ? _mediaPlayer.Rate : 1f;
        set { if (_mediaPlayer != null) _mediaPlayer.SetRate(value); }
    }
    public float position
    {
        get => _mediaPlayer != null ? _mediaPlayer.Position : 0f;
        set { if (_mediaPlayer != null) _mediaPlayer.Position = Mathf.Clamp01(value); }
    }
    public long time => _mediaPlayer != null ? _mediaPlayer.Time : 0;
    public long length => _mediaPlayer != null ? _mediaPlayer.Length : 0;
    public Texture2D texture => _videoTexture;

    // --- 이벤트 ---
    public event Action<VLCVideoPlayer> started;

    // --- 내부 VLC 로직 변수 ---
    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    private Texture2D _videoTexture;
    
    private IntPtr _texBuffer = IntPtr.Zero;
    private uint _width = 0;
    private uint _height = 0;
    private bool _frameUpdated = false;
    private bool _hasStartedInvoked = false;

    bool _isDraggingSlider;
    public UnityEngine.UI.Slider progressBar; // 유니티 UI 슬라이더 연결

    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

    void Start()
    {
        Core.Initialize();
        _libVLC = new LibVLC("--no-osd");
        _mediaPlayer = new MediaPlayer(_libVLC);

        _mediaPlayer.SetVideoFormatCallbacks(VideoFormatCallback, null);
        _mediaPlayer.SetVideoCallbacks(LockCallback, null, DisplayCallback);

        // 슬라이더의 최소/최대 값을 VLC Position 범위인 0.0 ~ 1.0에 맞춥니다.
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
        }

        if (playOnAwake && !string.IsNullOrEmpty(url))
        {
            Play();
        }
    }

// 필수 설치
// sudo apt install vlc libvlc-dev vlc-plugin-base vlc-plugin-video-output ffmpeg -y
    void Update()
    {
        // 1. 메인 스레드 큐 처리 (무한 루프 방지용 고정 개수 루프)
        int commandsCount = 0;
        lock (_mainThreadQueue) { commandsCount = _mainThreadQueue.Count; }

        for (int i = 0; i < commandsCount; i++)
        {
            Action action = null;
            lock (_mainThreadQueue) { if (_mainThreadQueue.Count > 0) action = _mainThreadQueue.Dequeue(); }
            action?.Invoke();
        }

        // 2. 비디오 프레임 적용
        if (_frameUpdated && _videoTexture != null && _texBuffer != IntPtr.Zero)
        {
            _videoTexture.LoadRawTextureData(_texBuffer, (int)(_width * _height * 4));
            _videoTexture.Apply();
            _frameUpdated = false;

            if (!_hasStartedInvoked)
            {
                _hasStartedInvoked = true;
                started?.Invoke(this);
            }
        }

        // 매 프레임마다 슬라이더 위치를 영상 재생 위치와 동기화 (드래그 중이 아닐 때만)
        if (_mediaPlayer != null && progressBar != null && !_isDraggingSlider)
        {
            progressBar.value = _mediaPlayer.Position; // 0.0 ~ 1.0 값
        }
    }

    public void Play()
    {
        Debug.Log("VLCVideoPlayer - Play() Start");
        if (_mediaPlayer == null) return;

        if (targetDisplay != null) targetDisplay.color = Color.white;

        // 1. 미디어가 아예 없거나 URL이 바뀐 경우에만 새로 로드
        if (_mediaPlayer.Media == null || _mediaPlayer.Media.Mrl != url)
        {
            _hasStartedInvoked = false;

            if (_mediaPlayer.Media != null)
                _mediaPlayer.Media.Dispose();

            FromType determineType = FromType.FromPath;
            if (url.StartsWith("file://") || url.StartsWith("rtsp://") || url.StartsWith("http"))
                determineType = FromType.FromLocation;

            var media = new Media(_libVLC, url, determineType);
            
            if (isLooping)
            {
                media.AddOption("input-repeat=65535");
            }

            _mediaPlayer.Media = media;
            _mediaPlayer.Play();            
        }
        else
        {
            // 2. [보완] 미디어가 이미 로드되어 있는 상태 (Pause 상태 포함)
            // 현재 포지션이 유효하다면 그 위치를 유지한 채로 재생 명령 전달
            if (_mediaPlayer.State == VLCState.Paused)
            {
                _mediaPlayer.Play(); // VLC는 일시정지 중 Play()를 다시 호출하면 그 자리에서 이어집니다.
            }
            else if (_mediaPlayer.State == VLCState.Ended || _mediaPlayer.State == VLCState.Error)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Play();
            }
            else if (!_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Play();
            }
        }
        Debug.Log("VLCVideoPlayer - Play() End");
    }

    public void Pause() { _mediaPlayer?.Pause(); }

    public void TogglePause()
    {
        if (_mediaPlayer != null)
        {
            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Pause();
            else
                _mediaPlayer.Play();
        }
    }
    
    public void Stop() 
    { 
        _mediaPlayer?.Stop(); 
        _hasStartedInvoked = false; 

        // 정지 시 화면을 검은색으로 처리
        if (targetDisplay != null) targetDisplay.color = Color.black;

        // 3. UI 슬라이더 위치도 0으로 초기화
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
    }

    private uint VideoFormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        byte[] chromaBytes = System.Text.Encoding.UTF8.GetBytes("RGBA");
        Marshal.Copy(chromaBytes, 0, chroma, 4);
        _width = width; _height = height; pitches = width * 4; lines = height;
        long bufferSize = pitches * lines;

        if (_texBuffer != IntPtr.Zero) Marshal.FreeHGlobal(_texBuffer);
        _texBuffer = Marshal.AllocHGlobal((int)bufferSize);

        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(() =>
            {
                if (_videoTexture != null) Destroy(_videoTexture);
                
                // 가상 텍스처 생성
                _videoTexture = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false);
                
                // [수정된 핵심 로직] 지정된 RawImage에 새로 만든 텍스처를 강제로 꽂아줍니다.
                if (targetDisplay != null)
                {
                    targetDisplay.texture = _videoTexture;
                }
            });
        }
        return 1;
    }

    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        if (_texBuffer != IntPtr.Zero) Marshal.WriteIntPtr(planes, _texBuffer);
        return IntPtr.Zero;
    }

    private void DisplayCallback(IntPtr opaque, IntPtr picture) { _frameUpdated = true; }

    // 1. 사용자가 슬라이더를 누르는 순간 (Pointer Down)
    public void OnSliderPointerDown()
    {
        _isDraggingSlider = true;

        // 슬라이더를 누르면 영상을 즉시 일시정지합니다.
        if (_mediaPlayer != null && _mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Pause();
        }
    }

    // 2. 슬라이더를 드래그하는 동안 실시간 호출 (On Value Changed)
    public void OnSliderValueChanged(float value)
    {
        // 사용자가 마우스로 드래그 중일 때만 VLC의 재생 위치를 변경합니다.
        // VLC는 일시정지 상태에서 Position을 바꾸면 해당 위치의 프레임을 화면에 새로 그려줍니다.
        if (_isDraggingSlider && _mediaPlayer != null)
        {
            _mediaPlayer.Position = value;
        }
    }

    // 3. 사용자가 슬라이더를 놓는 순간 (Pointer Up)
    public void OnSliderPointerUp()
    {
        if (_mediaPlayer != null && progressBar != null)
        {
            // 최종 위치를 다시 확실하게 지정해준 뒤
            _mediaPlayer.Position = progressBar.value;
            
            // 영상을 다시 재생합니다.
            _mediaPlayer.Play();
        }
        
        // 영상 위치 이동 및 재생이 시작된 후 자동 업데이트를 다시 활성화합니다.
        _isDraggingSlider = false;
    }

    void OnDestroy()
    {
        if (_mediaPlayer != null) { _mediaPlayer.Stop(); _mediaPlayer.Dispose(); }
        _libVLC?.Dispose();
        if (_texBuffer != IntPtr.Zero) Marshal.FreeHGlobal(_texBuffer);
        if (_videoTexture != null) Destroy(_videoTexture);
    }
}