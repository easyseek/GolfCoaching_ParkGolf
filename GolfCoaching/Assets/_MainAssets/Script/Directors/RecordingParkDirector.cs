using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Experimental;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class RecordingParkDirector : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] RawImage frontView;
    [SerializeField] RawImage sideView;
    [SerializeField] WebcamTracker frontCamera;
    [SerializeField] WebcamTracker sideCamera;
    [SerializeField] bool recordSide = true;

    [Header("Recording")]
    [SerializeField, Min(1)] int captureFps = 30;
    [SerializeField] string stretchingName = "stretching";
    [SerializeField] string frontVideoFilter = "transpose=1";
    [SerializeField] string sideVideoFilter = "transpose=0";
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Lesson UI")]
    [SerializeField] GameObject checkPanel;
    [SerializeField] GameObject readyPanel;
    [SerializeField] GameObject recordingPanel;
    [SerializeField] GameObject loadingPanel;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] Image redDot;

    public bool IsRecording => isRecording;
    public string SessionDirectory => sessionDirectory;

    readonly List<byte[]> frontFrames = new List<byte[]>();
    readonly List<byte[]> sideFrames = new List<byte[]>();
    readonly List<long> timestampsMs = new List<long>();

    bool isRecording;
    bool isCapturing;
    bool stopRequested;
    bool showFront = true;
    float recordingElapsed;
    float redDotBlinkElapsed;
    int pendingGpuReads;
    int frontWidth;
    int frontHeight;
    int sideWidth;
    int sideHeight;
    string sessionDirectory;
    string ffmpegPath;

    PoseLandmarker offlinePose;
    TextureFrame analyzerFrame;

    void Awake()
    {
        ffmpegPath = Path.Combine(Application.dataPath, "Plugins", "ffmpeg", "bin", "ffmpeg");

        // RecordingPark에서는 촬영 중 실시간 MediaPipe 추론을 사용하지 않는다.
        if (frontCamera != null) frontCamera.isImageTrack = false;
        if (sideCamera != null) sideCamera.isImageTrack = false;

        PrepareInitialCameraView();
        ShowReadyUI();
        SetStatus("Ready");
    }

    public void StartRecording()
    {
        if (isRecording)
            return;

        isRecording = true;
        ShowCountdownUI();
        Debug.Log("[RecordingPark] Start button clicked.");
        StartCoroutine(RecordThenAnalyze());
    }

    public void StopRecording()
    {
        if (!isCapturing)
            return;

        stopRequested = true;
        ShowProcessingUI();
        SetStatus("Finishing capture...");
    }

    public void SwitchView()
    {
        showFront = !showFront;
        ApplyCameraView();
    }

    public void BackToStudio()
    {
        if (!isRecording)
            SceneManager.LoadScene("Studio");
    }

    IEnumerator RecordThenAnalyze()
    {
        stopRequested = false;
        SetStatus("Ready");
        showFront = true;

        float countdown = 3f;
        while (countdown > 0f)
        {
            if (countText != null) countText.text = Mathf.CeilToInt(countdown).ToString();
            countdown -= Time.deltaTime;
            yield return null;
        }

        if (countText != null) countText.text = string.Empty;

        ApplyCameraView();
        ShowRecordingUI();
        isCapturing = true;
        recordingElapsed = 0f;
        redDotBlinkElapsed = 0f;
        if (timerText != null) timerText.text = "00:00:00";
        if (redDot != null) redDot.enabled = true;
        SetStatus("Waiting for cameras...");

        while (!AreCamerasReady() && !stopRequested)
        {
            UpdateRecordingUI(Time.deltaTime);
            yield return null;
        }

        if (stopRequested)
        {
            isCapturing = false;
            SetStatus("No frames captured");
            ShowReadyUI();
            yield break;
        }

        frontFrames.Clear();
        sideFrames.Clear();
        timestampsMs.Clear();
        pendingGpuReads = 0;

        frontWidth = frontView.texture.width;
        frontHeight = frontView.texture.height;
        if (recordSide)
        {
            sideWidth = sideView.texture.width;
            sideHeight = sideView.texture.height;
        }

        string sessionName = SafeName(stretchingName) + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        sessionDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "DataBase_park", "Stretching", sessionName);
        Directory.CreateDirectory(sessionDirectory);

        SetStatus("Recording");

        float startedAt = Time.realtimeSinceStartup;
        float interval = 1f / captureFps;
        float nextFrameAt = startedAt;

        while (!stopRequested)
        {
            yield return new WaitForEndOfFrame();

            float now = Time.realtimeSinceStartup;
            UpdateRecordingUI(Time.deltaTime);
            if (now < nextFrameAt)
                continue;

            nextFrameAt += interval;
            if (now - nextFrameAt > interval)
                nextFrameAt = now + interval;

            int frameIndex = frontFrames.Count;
            frontFrames.Add(null);
            if (recordSide)
                sideFrames.Add(null);

            timestampsMs.Add((long)((now - startedAt) * 1000f));
            CaptureFrameAsync(frontView.texture, frontFrames, frameIndex);
            if (recordSide)
                CaptureFrameAsync(sideView.texture, sideFrames, frameIndex);
        }

        isCapturing = false;
        SetStatus("Waiting for captured frames...");
        yield return new WaitUntil(() => pendingGpuReads <= 0);

        int frameCount = frontFrames.Count;
        if (recordSide)
            frameCount = Mathf.Min(frameCount, sideFrames.Count);
        frameCount = Mathf.Min(frameCount, timestampsMs.Count);

        RemoveInvalidTail(frontFrames, frameCount);
        RemoveInvalidTail(sideFrames, recordSide ? frameCount : 0);
        if (timestampsMs.Count > frameCount)
            timestampsMs.RemoveRange(frameCount, timestampsMs.Count - frameCount);

        if (frameCount == 0)
        {
            SetStatus("No frames captured");
            Debug.LogError("[RecordingPark] No frames captured.");
            ShowReadyUI();
            yield break;
        }

        SetStatus("Encoding front video...");
        yield return EncodeFrames(
            Path.Combine(sessionDirectory, "front.mp4"),
            frontWidth, frontHeight, frontVideoFilter, frontFrames);

        if (recordSide)
        {
            SetStatus("Encoding side video...");
            yield return EncodeFrames(
                Path.Combine(sessionDirectory, "side.mp4"),
                sideWidth, sideHeight, sideVideoFilter, sideFrames);
        }

        SetStatus("Analyzing skeletons...");
        yield return AnalyzeAllFrames(frameCount);

        DisposePoseAnalyzer();
        frontFrames.Clear();
        sideFrames.Clear();
        timestampsMs.Clear();

        SetStatus("Saved: " + sessionName);
        ShowReadyUI();
        Debug.Log("[RecordingPark] Saved: " + sessionDirectory + ", frames=" + frameCount);
    }

    void CaptureFrameAsync(Texture source, List<byte[]> destination, int index)
    {
        if (source == null)
            return;

        RenderTexture renderTexture = RenderTexture.GetTemporary(source.width, source.height, 0);
        Graphics.Blit(source, renderTexture);
        pendingGpuReads++;

        AsyncGPUReadback.Request(renderTexture, 0, TextureFormat.RGB24, request =>
        {
            try
            {
                if (!request.hasError && index >= 0 && index < destination.Count)
                    destination[index] = request.GetData<byte>().ToArray();
            }
            finally
            {
                RenderTexture.ReleaseTemporary(renderTexture);
                pendingGpuReads--;
            }
        });
    }

    IEnumerator EncodeFrames(
        string output, int width, int height, string filter, List<byte[]> frames)
    {
        Process process;
        Stream input;
        string error;

        if (!StartEncoder(output, width, height, filter, out process, out input, out error))
        {
            Debug.LogError("[RecordingPark] Encoder: " + error);
            yield break;
        }

        for (int i = 0; i < frames.Count; i++)
        {
            byte[] raw = frames[i];
            if (raw != null && raw.Length == width * height * 3)
                input.Write(raw, 0, raw.Length);

            if (i % 20 == 0)
                yield return null;
        }

        CloseInput(input);
        while (!process.HasExited)
            yield return null;
        process.Dispose();
    }

    IEnumerator AnalyzeAllFrames(int frameCount)
    {
        string csvPath = Path.Combine(sessionDirectory, "skeleton.csv");
        using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(true)))
        {
            writer.WriteLine(
                "frame,timestamp_ms,view,landmark,x_2d,y_2d,visibility_2d,x_3d,y_3d,z_3d,visibility_3d");

            for (int frame = 0; frame < frameCount; frame++)
            {
                Texture2D front = CreateTexture(frontFrames[frame], frontWidth, frontHeight);
                AnalyzeAndWrite(writer, frame, timestampsMs[frame], "F", front);
                if (front != null)
                    Destroy(front);

                if (recordSide)
                {
                    Texture2D side = CreateTexture(sideFrames[frame], sideWidth, sideHeight);
                    AnalyzeAndWrite(writer, frame, timestampsMs[frame], "S", side);
                    if (side != null)
                        Destroy(side);
                }

                if (frame % 5 == 0)
                {
                    SetStatus("Analyzing skeletons... " + (frame + 1) + "/" + frameCount);
                    writer.Flush();
                    yield return null;
                }
            }
        }
    }

    void AnalyzeAndWrite(StreamWriter writer, int frame, long timestamp, string view, Texture2D texture)
    {
        NormalizedLandmarks normalized = default;
        Landmarks world = default;
        bool detected = TryDetectPose(texture, out normalized, out world);

        for (int i = 0; i < 33; i++)
        {
            bool has2D = detected && normalized.landmarks != null && i < normalized.landmarks.Count;
            bool has3D = detected && world.landmarks != null && i < world.landmarks.Count;

            writer.Write(frame + "," + timestamp + "," + view + "," + i + ",");
            writer.Write(has2D ? F((float)normalized.landmarks[i].x) : "");
            writer.Write(",");
            writer.Write(has2D ? F((float)normalized.landmarks[i].y) : "");
            writer.Write(",");
            writer.Write(has2D ? F((float)normalized.landmarks[i].visibility) : "");
            writer.Write(",");
            writer.Write(has3D ? F(-(float)world.landmarks[i].y) : "");
            writer.Write(",");
            writer.Write(has3D ? F(-(float)world.landmarks[i].x) : "");
            writer.Write(",");
            writer.Write(has3D ? F((float)world.landmarks[i].z) : "");
            writer.Write(",");
            writer.WriteLine(has3D ? F((float)world.landmarks[i].visibility) : "");
        }
    }

    bool TryDetectPose(Texture2D texture, out NormalizedLandmarks normalized, out Landmarks world)
    {
        normalized = default;
        world = default;
        if (texture == null)
            return false;

        EnsurePoseAnalyzer(texture.width, texture.height);
        if (offlinePose == null || analyzerFrame == null)
            return false;

        analyzerFrame.ReadTextureOnCPU(texture, flipHorizontally: false, flipVertically: true);
        using (Mediapipe.Image image = analyzerFrame.BuildCPUImage())
        {
            PoseLandmarkerResult result = offlinePose.Detect(image);
            bool has2D = result.poseLandmarks != null && result.poseLandmarks.Count > 0;
            bool has3D = result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0;

            if (has2D)
                normalized = result.poseLandmarks[0];
            if (has3D)
                world = result.poseWorldLandmarks[0];
            return has2D || has3D;
        }
    }

    void EnsurePoseAnalyzer(int width, int height)
    {
        if (offlinePose == null)
        {
            TextAsset model = Resources.Load<TextAsset>("pose_landmarker_full");
            if (model == null)
            {
                Debug.LogError("[RecordingPark] pose_landmarker_full not found.");
                return;
            }

            var baseOptions = new Mediapipe.Tasks.Core.BaseOptions(
                Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                modelAssetBuffer: model.bytes);
            var options = new PoseLandmarkerOptions(
                baseOptions: baseOptions,
                runningMode: Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE);
            offlinePose = PoseLandmarker.CreateFromOptions(options);
        }

        if (analyzerFrame == null || analyzerFrame.width != width || analyzerFrame.height != height)
        {
            analyzerFrame?.Dispose();
            analyzerFrame = new TextureFrame(width, height, TextureFormat.RGBA32);
        }
    }

    void DisposePoseAnalyzer()
    {
        try { offlinePose?.Close(); } catch { }
        try { analyzerFrame?.Dispose(); } catch { }
        offlinePose = null;
        analyzerFrame = null;
    }

    Texture2D CreateTexture(byte[] rgb, int width, int height)
    {
        if (rgb == null || rgb.Length != width * height * 3)
            return null;

        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.LoadRawTextureData(rgb);
        texture.Apply(false, false);
        return texture;
    }

    bool StartEncoder(string output, int width, int height, string filter,
        out Process process, out Stream input, out string error)
    {
        process = null;
        input = null;
        error = "";

        if (!File.Exists(ffmpegPath))
        {
            error = "Missing ffmpeg: " + ffmpegPath;
            return false;
        }

        try
        {
            string vf = string.IsNullOrWhiteSpace(filter) ? "" : "-vf \"" + filter + "\" ";
            process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = ffmpegPath,
                    Arguments = "-y -f rawvideo -pixel_format rgb24 -video_size " +
                        width + "x" + height + " -framerate " + captureFps + " -i - " + vf +
                        "-c:v libx264 -preset ultrafast -crf 23 -pix_fmt yuv420p " +
                        "-movflags +faststart \"" + output + "\"",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            input = process.StandardInput.BaseStream;
            return true;
        }
        catch (Exception e)
        {
            error = e.Message;
            process?.Dispose();
            process = null;
            return false;
        }
    }

    static void CloseInput(Stream input)
    {
        if (input == null)
            return;
        try { input.Flush(); input.Close(); }
        catch (Exception e) { Debug.LogWarning("[RecordingPark] Encoder close: " + e.Message); }
    }

    static void RemoveInvalidTail(List<byte[]> frames, int count)
    {
        if (frames.Count > count)
            frames.RemoveRange(count, frames.Count - count);
    }

    static string F(float value)
    {
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    static string SafeName(string value)
    {
        string result = string.IsNullOrWhiteSpace(value) ? "stretching" : value.Trim();
        foreach (char c in Path.GetInvalidFileNameChars())
            result = result.Replace(c, '_');
        return result;
    }

    void ShowRecordingUI()
    {
        if (checkPanel != null) checkPanel.SetActive(false);
        if (readyPanel != null) readyPanel.SetActive(false);
        if (recordingPanel != null) recordingPanel.SetActive(true);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void ShowCountdownUI()
    {
        if (checkPanel != null) checkPanel.SetActive(false);
        if (readyPanel != null) readyPanel.SetActive(true);
        if (recordingPanel != null) recordingPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    void ShowProcessingUI()
    {
        if (readyPanel != null) readyPanel.SetActive(false);
        if (recordingPanel != null) recordingPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(true);
    }

    void ShowReadyUI()
    {
        isRecording = false;
        isCapturing = false;
        if (checkPanel != null) checkPanel.SetActive(true);
        if (readyPanel != null) readyPanel.SetActive(false);
        if (recordingPanel != null) recordingPanel.SetActive(false);
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    bool AreCamerasReady()
    {
        bool frontReady = frontView != null && frontView.texture != null &&
            frontCamera != null && frontCamera.isInit;
        bool sideReady = !recordSide || (sideView != null && sideView.texture != null &&
            sideCamera != null && sideCamera.isInit);
        return frontReady && sideReady;
    }

    void PrepareInitialCameraView()
    {
        if (frontView != null)
        {
            frontView.gameObject.SetActive(true);
            Color color = frontView.color;
            color.a = 1f;
            frontView.color = color;
        }
        if (sideView != null)
        {
            sideView.gameObject.SetActive(true);
            Color color = sideView.color;
            color.a = 0f;
            sideView.color = color;
        }
    }

    void ApplyCameraView()
    {
        if (frontView != null)
        {
            frontView.gameObject.SetActive(showFront);
            Color color = frontView.color;
            color.a = 1f;
            frontView.color = color;
        }
        if (sideView != null)
        {
            sideView.gameObject.SetActive(!showFront);
            Color color = sideView.color;
            color.a = 1f;
            sideView.color = color;
        }
    }

    void UpdateRecordingUI(float deltaTime)
    {
        recordingElapsed += deltaTime;
        int hour = Mathf.FloorToInt(recordingElapsed / 3600f);
        int minute = Mathf.FloorToInt((recordingElapsed % 3600f) / 60f);
        int second = Mathf.FloorToInt(recordingElapsed % 60f);
        if (timerText != null) timerText.text = string.Format("{0:00}:{1:00}:{2:00}", hour, minute, second);
        redDotBlinkElapsed += deltaTime;
        if (redDot != null && redDotBlinkElapsed >= 0.5f)
        {
            redDot.enabled = !redDot.enabled;
            redDotBlinkElapsed = 0f;
        }
    }

    void SetStatus(string text)
    {
        if (statusText != null)
            statusText.text = text;
    }

    void OnDestroy()
    {
        stopRequested = true;
        DisposePoseAnalyzer();
    }
}
