using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic; // Queue를 쓰기 위해 추가
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

public class VLCVideoPlayerSample : MonoBehaviour
{
    public RawImage screenDisplay;
    public string videoPath = "Assets/Videos/sample.mp4";

    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    private Texture2D _videoTexture;
    
    private IntPtr _texBuffer = IntPtr.Zero;
    private uint _width = 0;
    private uint _height = 0;
    private bool _frameUpdated = false;


/*
sudo apt update
sudo apt install vlc libvlc-dev vlc-plugin-base vlc-plugin-video-output ffmpeg -y
*/

    // 메인 스레드에서 실행할 액션들을 담는 큐 생성
    private readonly Queue<Action> _mainThreadQueue = new Queue<Action>();

    public UnityEngine.UI.Slider progressBar; // 유니티 UI 슬라이더 연결
    private bool _isDraggingSlider = false;   // 유저가 슬라이더를 드래그 중인지 체크
    
    void Start()
    {
        Core.Initialize();

        _libVLC = new LibVLC("--no-osd");
        _mediaPlayer = new MediaPlayer(_libVLC);

        _mediaPlayer.SetVideoFormatCallbacks(VideoFormatCallback, null);
        _mediaPlayer.SetVideoCallbacks(LockCallback, null, DisplayCallback);

        // [추가] 슬라이더의 최소/최대 값을 VLC Position 범위인 0.0 ~ 1.0에 맞춥니다.
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
        }
    
        PlayVideo(videoPath);
    }

    public void PlayVideo(string path)
    {
        if (screenDisplay != null)
        {
            screenDisplay.color = Color.white; 
        }
        
        using (var media = new Media(_libVLC, path, FromType.FromPath))
        {
            _mediaPlayer.Play(media);
        }
    }

    void Update()
    {
        // 1. 백그라운드 스레드에서 요청한 메인 스레드 작업(텍스처 생성 등)이 있다면 실행
        lock (_mainThreadQueue)
        {
            while (_mainThreadQueue.Count > 0)
            {
                _mainThreadQueue.Dequeue()?.Invoke();
            }
        }

        // 2. 영상 프레임 버퍼가 업데이트되었다면 텍스처에 적용
        if (_frameUpdated && _texBuffer != IntPtr.Zero && _videoTexture != null)
        {
            _videoTexture.LoadRawTextureData(_texBuffer, (int)(_width * _height * 4));
            _videoTexture.Apply();
            _frameUpdated = false;
        }

        // 매 프레임마다 슬라이더 위치를 영상 재생 위치와 동기화 (드래그 중이 아닐 때만)
        if (_mediaPlayer != null && progressBar != null && !_isDraggingSlider)
        {
            progressBar.value = _mediaPlayer.Position; // 0.0 ~ 1.0 값
        }
    }

    // --- VLC 콜백 함수들 ---

    /*private uint VideoFormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        byte[] chromaBytes = System.Text.Encoding.UTF8.GetBytes("RGBA");
        Marshal.Copy(chromaBytes, 0, chroma, 4);

        _width = width;
        _height = height;
        pitches = width * 4;
        lines = height;

        long bufferSize = pitches * lines;
        _texBuffer = Marshal.AllocHGlobal((int)bufferSize);

        // [중요] Loom 대신 큐에 텍스처 생성 코드를 집어넣습니다.
        lock (_mainThreadQueue)
        {
            _mainThreadQueue.Enqueue(() => {
                _videoTexture = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false);
                screenDisplay.texture = _videoTexture;
            });
        }

        return 1;
    }*/
    private uint VideoFormatCallback(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        byte[] chromaBytes = System.Text.Encoding.UTF8.GetBytes("RGBA");
        Marshal.Copy(chromaBytes, 0, chroma, 4);
        _width = width; _height = height; pitches = width * 4; lines = height;
        long bufferSize = pitches * lines;
        _texBuffer = Marshal.AllocHGlobal((int)bufferSize);
        lock (_mainThreadQueue) { _mainThreadQueue.Enqueue(() => { _videoTexture = new Texture2D((int)_width, (int)_height, TextureFormat.RGBA32, false); screenDisplay.texture = _videoTexture; }); }
        return 1;
    }

    private IntPtr LockCallback(IntPtr opaque, IntPtr planes)
    {
        Marshal.WriteIntPtr(planes, _texBuffer);
        return IntPtr.Zero;
    }

    private void DisplayCallback(IntPtr opaque, IntPtr picture)
    {
        _frameUpdated = true;
    }

    // 1. 일시정지 / 토글 재생
    public void TogglePause()
    {
        if (_mediaPlayer != null)
        {
            if (screenDisplay != null)
            {
                screenDisplay.color = Color.white; 
            }

            if (_mediaPlayer.IsPlaying)
                _mediaPlayer.Pause();
            else
                _mediaPlayer.Play();
        }
    }

    // 2. 정지 (처음으로 되돌림)
    // 동영상을 처음 위치(첫 프레임)로 되돌리고 정지(일시정지) 상태로 만듭니다.
    /*
    public void StopVideo()
    {
        if (_mediaPlayer != null)
        {
            // 2. 재생 위치를 0.0(맨 처음)으로 되돌립니다.
            _mediaPlayer.Position = 0f;

            // 1. 영상을 일시정지 상태로 만듭니다.
            if (_mediaPlayer.IsPlaying)
            {
                _mediaPlayer.Pause();
            }

            // 3. 만약 UI 슬라이더가 연결되어 있다면 슬라이더 바 위치도 0으로 초기화합니다.
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }
        }
    }*/

    // 정지 시 영상을 완전히 멈추고 화면을 검은색으로 만듭니다.
    public void StopVideo()
    {
        if (_mediaPlayer != null)
        {
            // 1. VLC 재생기를 완전히 정지시킵니다. (디코더 스레드 종료)
            _mediaPlayer.Stop();

            // 2. 화면 출력을 검은색 화면으로 바꿉니다.
            if (screenDisplay != null)
            {
                // 방법 A: RawImage의 텍스처 연결을 잠시 해제하고 자체 색상을 검은색으로 변경
                screenDisplay.color = Color.black;
            }

            // 3. UI 슬라이더 위치도 0으로 초기화
            if (progressBar != null)
            {
                progressBar.value = 0f;
            }
        }
    }

    // 3. 탐색 (원하는 구간으로 이동 - Slider와 연동 가능)
    // normalizedTime: 0.0f (시작) ~ 1.0f (끝) 사이의 값
    public void Seek(float normalizedTime)
    {
        if (_mediaPlayer != null)
        {
            // Position은 0.0f ~ 1.0f 사이의 float 값을 받습니다.
            _mediaPlayer.Position = Mathf.Clamp01(normalizedTime);
        }
    }

    // 4. 볼륨 조절 (0 ~ 100)
    public void SetVolume(int volume)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = Mathf.Clamp(volume, 0, 100);
        }
    }

    // 5. 현재 재생 시간 및 총 길이 가져오기 (UI 텍스트 표시용)
    public string GetPlaybackTimeFormatted()
    {
        if (_mediaPlayer == null) return "00:00 / 00:00";

        // 밀리초(ms) 단위로 반환되므로 TimeSpan으로 변환
        TimeSpan currentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
        TimeSpan totalTime = TimeSpan.FromMilliseconds(_mediaPlayer.Length);

        return $"{currentTime.Minutes:D2}:{currentTime.Seconds:D2} / {totalTime.Minutes:D2}:{totalTime.Seconds:D2}";
    }

    
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
        _mediaPlayer?.Stop();
        _mediaPlayer?.Dispose();
        _libVLC?.Dispose();

        if (_texBuffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_texBuffer);
        }
    }
}
