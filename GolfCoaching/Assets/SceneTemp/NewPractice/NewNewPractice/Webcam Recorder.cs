using System.Collections; 
using System.Collections.Generic;
using UnityEngine; 
using UnityEngine.UI; 
using System.IO; 
using System.Diagnostics;
using System;
public class WebcamRecorder : MonoBehaviour // MonoBehaviour 클래스를 상속받아 Unity에서 동작하는 컴포넌트를 만듦
{
    public RawImage userCamRawImage; // 사용자 웹캠이 표시될 RawImage를 참조
    [SerializeField] private int frameRate = 60; // 초당 프레임 수 설정
    private List<byte[]> frameData = new List<byte[]>(); // 녹화된 프레임 데이터를 저장하는 리스트
    private string savePath; // 녹화된 데이터를 저장할 경로
    private string ffmpegPath; // FFmpeg 실행 파일 경로
    private bool isRecording = false; // 현재 녹화 중인지 여부를 나타내는 변수
    private float recordStartTime = 0; // 녹화 시작 시간을 저장하는 변수
    [SerializeField] private Button RecordStartButton; // 녹화 시작 버튼
    [SerializeField] private Button RecordStopButton; // 녹화 중지 버튼
    [SerializeField] private AudioSource audioSource; // 오디오를 녹음할 AudioSource 컴포넌트
    private Texture2D webcamTexture; // RawImage의 텍스처를 사용할 Texture2D 객체
    private int startFrameIndex = -1; // 녹화 시작 프레임의 인덱스
    private string videoFilePath; // 녹화된 비디오 파일 경로
    private string audioFilePath; // 녹화된 오디오 파일 경로
    IEnumerable Start()
    {
        yield return new WaitForSeconds(2);

        // FFmpeg 실행 파일 경로 설정
        ffmpegPath = Application.dataPath + "/Plugins/ffmpeg/bin/ffmpeg.exe";
        UnityEngine.Debug.Log("FFmpeg 경로: " + ffmpegPath);
        // RawImage가 할당되었고 텍스처가 있는지 확인
        
if (userCamRawImage != null && userCamRawImage.texture != null)
        {
            // RawImage의 텍스처에서 정보를 가져와 Texture2D 생성
            webcamTexture = new Texture2D(userCamRawImage.texture.width, userCamRawImage.texture.height, TextureFormat.RGB24, false);
        }
        else
        {
            // RawImage 또는 텍스처가 할당되지 않은 경우 오류 메시지 출력
            UnityEngine.Debug.LogError("RawImage가 할당되지 않았거나 텍스처가 없습니다. RawImage를 할당하고 텍스처를 설정해 주세요.");
        }
        // 녹화 시작 버튼 클릭 시 StartRecording 메서드 호출
        RecordStartButton.onClick.AddListener(StartRecording);
        // 녹화 중지 버튼 클릭 시 StopRecording 메서드 호출
        RecordStopButton.onClick.AddListener(StopRecording);
    }
    private void Update()
    {
        // 녹화 중인 경우, 프레임 캡처
        if (isRecording)
        {
            CaptureFrame();
        }
    }
    public void StartRecording()
    {
        // 녹화 파일 저장 경로 설정
        savePath = Path.Combine(Application.persistentDataPath, "WebcamRecording");
        
        // 저장 경로에 디렉토리가 없으면 생성
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        // 기존에 있는 파일 삭제
        DeleteAllFilesInDirectory(savePath);
        // 녹화 상태로 설정
        isRecording = true;
        // 녹화 시작 프레임 인덱스 설정
        startFrameIndex = frameData.Count;
        // 녹화 시작 시간 기록
        recordStartTime = Time.time;
        UnityEngine.Debug.Log("녹화 시작");
        // 오디오 녹화 시작
        StartAudioRecording();
    }
    private void StartAudioRecording()
    {
        // 마이크 장치가 있는지 확인
        if (Microphone.devices.Length > 0)
        {
            // 마이크를 통해 오디오 녹음 시작
            audioSource.clip = Microphone.Start(null, true, 600, 44100);
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            // 마이크가 없는 경우 오류 메시지 출력
            UnityEngine.Debug.LogError("오디오를 녹음할 마이크가 없습니다.");
        }
    }
    private void StopAudioRecording()
    {
        // 마이크 녹음 중이면 종료
        if (Microphone.IsRecording(null))
        {
            // 마이크 녹음 종료 및 오디오 저장
            Microphone.End(null);
            audioSource.Stop();
            TrimAndSaveAudioClip();
        }
    }
    public void StopRecording()
    {
        // 녹화 중이 아니면 반환
        if (!isRecording) return;
        // 오디오 녹음 중지
        StopAudioRecording();
        // 녹화 상태 해제
        isRecording = false;
        // 녹화 중지 코루틴 실행 후 완료되면 녹화 데이터 초기화
        StartCoroutine(StopRecordingCoroutine(() =>
        {
            UnityEngine.Debug.Log("녹화가 중지되었습니다. 비디오 및 오디오가 다음 경로에 저장되었습니다: " + savePath);
            // 녹화 데이터 초기화
            ClearRecordingData();
        }));
    }
    private void ClearRecordingData()
    {
        // 프레임 데이터 초기화
        frameData.Clear();
        UnityEngine.Debug.Log("프레임 데이터가 초기화되었습니다.");
        // 오디오 클립이 존재하면 초기화
        if (audioSource.clip != null)
        {
            audioSource.clip = null;
            UnityEngine.Debug.Log("오디오 데이터가 초기화되었습니다.");
        }
        // 녹화 관련 변수 초기화
        recordStartTime = 0f;
        startFrameIndex = -1;
    }
    private IEnumerator StopRecordingCoroutine(Action onComplete)
    {
        // 녹화가 끝난 마지막 프레임 인덱스 설정
        int endFrameIndex = frameData.Count - 1;
        // 프레임이 존재하는 경우 이미지 데이터를 비디오로 변환
        if (endFrameIndex >= 0)
        {
            yield return StartCoroutine(ConvertImagesToVideoCoroutine(0, endFrameIndex));
        }
        else
        {
            UnityEngine.Debug.LogWarning("녹화된 프레임이 없습니다.");
        }
        // 녹화 중지 후 콜백 호출
        onComplete?.Invoke();
    }
    private IEnumerator ConvertImagesToVideoCoroutine(int startFrame, int endFrame)
    {
        // 비디오 파일 경로 설정
        videoFilePath = Path.Combine(savePath, "outputvideo.mp4").Replace("\\", "/");
        // 녹화된 시간 계산
        float recordingDuration = Time.time - recordStartTime;
        // 캡처된 총 프레임 수 계산
        int totalCapturedFrames = endFrame - startFrame + 1;
        // 실제 프레임 속도 계산
        float actualFrameRate = totalCapturedFrames / recordingDuration;
        // 실제 프레임 속도가 설정된 프레임 속도를 초과하지 않도록 제한
        if (actualFrameRate > frameRate)
        {
            actualFrameRate = frameRate;
        }
        UnityEngine.Debug.Log($"캡처된 총 프레임: {totalCapturedFrames}, 녹화 시간: {recordingDuration}s, 계산된 프레임 속도: {actualFrameRate} FPS");
        // FFmpeg 프로세스 실행 정보 설정
        ProcessStartInfo startInfo = new ProcessStartInfo(ffmpegPath)
        {
            Arguments = $"-y -f rawvideo -pix_fmt rgb24 -s {webcamTexture.width}x{webcamTexture.height} -r {frameRate} -i pipe:0 -vf vflip -vcodec libx264 -pix_fmt yuv420p -preset ultrafast -crf 23 \"{videoFilePath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (Process process = new Process())
        {
            // FFmpeg 프로세스 시작
            process.StartInfo = startInfo;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            // 오류 메시지 처리
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.Log("FFmpeg 오류: " + args.Data);
                }
            };
            // 출력 메시지 처리
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.Log("FFmpeg 출력: " + args.Data);
                }
            };
            // 각 프레임 데이터를 FFmpeg로 전달
            using (var writer = process.StandardInput.BaseStream)
            {
                for (int i = startFrame; i <= endFrame; i++)
                {
                    byte[] frame = frameData[i];
                    writer.Write(frame, 0, frame.Length);
                }
            }
            // 프로세스가 종료될 때까지 대기
            if (!process.HasExited)
            {
                float startTime = Time.time;
                float timeout = 30.0f;
                while (!process.HasExited)
                {
                    if (Time.time - startTime > timeout)
                    {
                        UnityEngine.Debug.LogError("FFmpeg 프로세스 타임아웃.");
                        process.Kill();
                        break;
                    }
                    yield return null;
                }
            }
            // FFmpeg 종료 코드 확인
            if (process.ExitCode != 0)
            {
                string stderr = process.StandardError.ReadToEnd();
                UnityEngine.Debug.LogError("FFmpeg 오류: " + stderr);
            }
            else
            {
                UnityEngine.Debug.Log("비디오 인코딩이 완료되었습니다. 비디오가 다음 경로에 저장되었습니다: " + videoFilePath);
            }
        }
    }
    private void TrimAndSaveAudioClip()
    {
        // 오디오 클립이 존재하지 않으면 반환
        if (audioSource.clip == null)
            return;
        // 녹화된 시간에 맞게 오디오 클립 자르기
        float duration = Time.time - recordStartTime;
        int sampleCount = (int)(duration * audioSource.clip.frequency);
        int channelCount = audioSource.clip.channels;
        // 자른 오디오 클립 생성
        AudioClip trimmedClip = AudioClip.Create("TrimmedAudio", sampleCount, channelCount, audioSource.clip.frequency, false);
        float[] data = new float[sampleCount * channelCount];
        if (audioSource.clip.GetData(data, 0))
        {
            trimmedClip.SetData(data, 0);
            // 오디오 파일 경로 설정 및 저장
            audioFilePath = Path.Combine(savePath, "recordedaudio.wav");
            //SavWav.Save(audioFilePath, trimmedClip);
            UnityEngine.Debug.Log("녹음된 오디오가 저장되었습니다: " + audioFilePath);
        }
        else
        {
            UnityEngine.Debug.LogError("오디오 클립에서 데이터를 가져오지 못했습니다.");
        }
    }
    void DeleteAllFilesInDirectory(string directoryPath)
    {
        try
        {
            // 디렉토리 내 모든 파일 삭제
            var files = Directory.GetFiles(directoryPath);
            foreach (var file in files)
            {
                File.Delete(file);
                UnityEngine.Debug.Log("삭제된 파일: " + file);
            }
        }
        catch (System.Exception e)
        {
            // 파일 삭제 중 오류 발생 시 로그 출력
            UnityEngine.Debug.LogError("파일 삭제 중 오류 발생: " + e.Message);
        }
    }
    void CaptureFrame()
    {
        // 녹화 중일 때만 프레임 캡처
        if (isRecording)
        {
            // 현재 시간과 녹화 시작 시간 차이 계산
            float currentTime = Time.time - recordStartTime;
            int expectedFrameCount = Mathf.FloorToInt(currentTime * frameRate);
            // 캡처된 프레임 수가 기대 프레임 수보다 적은 경우에만 캡처 진행
            if (frameData.Count < expectedFrameCount)
            {
                // RawImage의 텍스처가 Texture2D인지 확인
                if (userCamRawImage.texture is Texture2D texture2D)
                {
                    // Texture2D 데이터를 webcamTexture로 복사
                    if (webcamTexture == null || webcamTexture.width != texture2D.width || webcamTexture.height != texture2D.height)
                    {
                        webcamTexture = new Texture2D(texture2D.width, texture2D.height, TextureFormat.RGB24, false);
                    }
                    webcamTexture.SetPixels(texture2D.GetPixels());
                    webcamTexture.Apply();
                    // 텍스처 데이터를 byte 배열로 변환하여 저장
                    byte[] bytes = webcamTexture.GetRawTextureData();
                    frameData.Add(bytes);
                    UnityEngine.Debug.Log("Captured frame: " + frameData.Count);
                }
                else
                {
                    UnityEngine.Debug.LogError("지원되지 않는 텍스처 타입입니다.");
                }
            }
        }
    }
}
