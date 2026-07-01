using Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Experimental;
using Mediapipe.Unity.CoordinateSystem;
using Debug = UnityEngine.Debug;
using System.Globalization;

public class AnalyzeTest : MonoBehaviour
{
    [SerializeField] private KeyCode runKey = KeyCode.F9;
    [SerializeField] private bool requireSide = true;

    [SerializeField] private KeyCode runCsvKey = KeyCode.F10;
    [SerializeField] private bool useRuntimeLandmarkCsv = true;

    [SerializeField] private Texture2D[] frontStepPngs = new Texture2D[8];
    [SerializeField] private Texture2D[] sideStepPngs = new Texture2D[8];

    [SerializeField] private RawImage rawImageFrontRef;
    [SerializeField] private RawImage rawImageSideRef;

    [SerializeField] private bool exportProcessedCsv = true;
    [SerializeField] private bool exportLandmarkCsv = true;

    [SerializeField] private int missingStepValue = -1;

    [SerializeField] private bool writeEmptyLandmarkCsvWhenMissing = false;

    [SerializeField] private SensorProcess sensorProcess;
    [SerializeField] private WebcamTrackerController webcamTrackerController;

    private PoseLandmarker _offlinePoseLM;
    private TextureFrame _tfAnalyzer;
    private bool _poseLMForVideo = false;

    private Dictionary<string, int[]> ResultProData = new Dictionary<string, int[]>();

    private bool _running = false;

    private void Update()
    {
        if (_running)
        {
            return;
        }

        if (Input.GetKeyDown(runKey))
        {
            StartCoroutine(Run_ExtractAndSaveCsv());
        }

        if (Input.GetKeyDown(runCsvKey))
        {
            StartCoroutine(Run_ApplyFromRuntimeLandmarkCsv());
        }
    }

    private IEnumerator Run_ExtractAndSaveCsv()
    {
        if (webcamTrackerController != null)
        {
            webcamTrackerController.SetTracker(true, true);
        }

        _running = true;
        SetResultData();

        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            Texture2D frontStepPng = frontStepPngs[stepIndex];
            Texture2D sideStepPng = (sideStepPngs != null && sideStepPngs.Length > stepIndex) ? sideStepPngs[stepIndex] : null;

            if (frontStepPng == null)
            {
                Debug.Log($"[PNGAnalyze] Skip step={stepIndex} ({(SWINGSTEP)stepIndex}) reason=front null");

                if (exportLandmarkCsv && writeEmptyLandmarkCsvWhenMissing)
                {
                    SaveStepRuntimeLandmarksCsv(stepIndex, null, null, null, null);
                }

                continue;
            }

            if (requireSide && sideStepPng == null)
            {
                Debug.Log($"[PNGAnalyze] Skip step={stepIndex} ({(SWINGSTEP)stepIndex}) reason=side null (requireSide=true)");

                if (exportLandmarkCsv && writeEmptyLandmarkCsvWhenMissing)
                {
                    SaveStepRuntimeLandmarksCsv(stepIndex, null, null, null, null);
                }

                continue;
            }

            Texture2D frontWebcamFrame = AsWebcamFrameRGB24(frontStepPng);
            Texture2D sideWebcamFrame = (requireSide && sideStepPng != null) ? AsWebcamFrameRGB24(sideStepPng) : null;

            NormalizedLandmarks frontNormalized2D = DetectOne(frontWebcamFrame);
            Landmarks frontWorld3D = DetectOneWorld(frontWebcamFrame);

            NormalizedLandmarks sideNormalized2D = default;
            Landmarks sideWorld3D = default;

            if (requireSide && sideWebcamFrame != null)
            {
                sideNormalized2D = DetectOne(sideWebcamFrame);
                sideWorld3D = DetectOneWorld(sideWebcamFrame);
            }

            if (exportLandmarkCsv)
            {
                Landmark2D[] frontLandmarks2D = ConvertToLandmark2DArray(frontNormalized2D, isFront: true);
                Landmark3D[] frontLandmarks3D = ConvertToLandmark3DArray(frontWorld3D);

                Landmark2D[] sideLandmarks2D = null;
                Landmark3D[] sideLandmarks3D = null;

                if (requireSide)
                {
                    sideLandmarks2D = ConvertToLandmark2DArray(sideNormalized2D, isFront: false);
                    sideLandmarks3D = ConvertToLandmark3DArray(sideWorld3D);
                }

                bool hasFront = (frontLandmarks2D != null && frontLandmarks3D != null);
                bool hasSide = (!requireSide) || (sideLandmarks2D != null && sideLandmarks3D != null);

                if (!hasFront || !hasSide)
                {
                    Debug.LogWarning($"[PNGAnalyze] Skip step={stepIndex} ({(SWINGSTEP)stepIndex}) reason=landmark null fOK={hasFront} sOK={hasSide}");

                    if (writeEmptyLandmarkCsvWhenMissing)
                    {
                        SaveStepRuntimeLandmarksCsv(stepIndex, null, null, null, null);
                    }

                    SafeDestroy(frontWebcamFrame);
                    SafeDestroy(sideWebcamFrame);
                    continue;
                }

                SaveStepRuntimeLandmarksCsv(stepIndex,
                    frontLandmarks2D, frontLandmarks3D,
                    sideLandmarks2D, sideLandmarks3D);
            }

            if (exportProcessedCsv)
            {
                Landmark2D[] frontLandmarks2D = ConvertToLandmark2DArray(frontNormalized2D, isFront: true);
                Landmark3D[] frontLandmarks3D = ConvertToLandmark3DArray(frontWorld3D);

                Landmark2D[] sideLandmarks2D = null;
                Landmark3D[] sideLandmarks3D = null;

                if (requireSide)
                {
                    sideLandmarks2D = ConvertToLandmark2DArray(sideNormalized2D, isFront: false);
                    sideLandmarks3D = ConvertToLandmark3DArray(sideWorld3D);
                }

                bool hasFront = (frontLandmarks2D != null && frontLandmarks3D != null);
                bool hasSide = (!requireSide) || (sideLandmarks2D != null && sideLandmarks3D != null);

                if (!hasFront || !hasSide)
                {
                    Debug.LogWarning($"[PNGAnalyze] Skip processed step={stepIndex} ({(SWINGSTEP)stepIndex}) reason=landmark null fOK={hasFront} sOK={hasSide}");

                    SafeDestroy(frontWebcamFrame);
                    SafeDestroy(sideWebcamFrame);
                    continue;
                }

                sensorProcess.UpdateSensor(in frontLandmarks2D, in sideLandmarks2D, in frontLandmarks3D, in sideLandmarks3D);
                ApplySensorValuesToResult(stepIndex);
            }

            SafeDestroy(frontWebcamFrame);
            SafeDestroy(sideWebcamFrame);

            if (stepIndex % 2 == 0)
            {
                yield return null;
            }
        }

        if (exportProcessedCsv)
        {
            SaveCsvFull();
        }

        _running = false;
    }

    private Texture2D AsWebcamFrameRGB24(Texture2D src)
    {
        if (src == null)
        {
            return null;
        }

        RenderTexture rt = RenderTexture.GetTemporary(
            src.width,
            src.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(src, rt);

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D dst = new Texture2D(src.width, src.height, TextureFormat.RGB24, false, true);
        dst.ReadPixels(new UnityEngine.Rect(0, 0, src.width, src.height), 0, 0);
        dst.Apply(false, false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return dst;
    }

    private IEnumerator Run_ApplyFromRuntimeLandmarkCsv()
    {
        _running = true;
        SetResultData();

        try
        {
            if (sensorProcess != null && sensorProcess.clientFront != null)
            {
                sensorProcess.clientFront.Track = true;
            }

            if (sensorProcess != null && sensorProcess.clientSide != null)
            {
                sensorProcess.clientSide.Track = true;
            }
        }
        catch { }

        for (int stepIndex = 0; stepIndex < 8; stepIndex++)
        {
            string stepName = ((SWINGSTEP)stepIndex).ToString();
            string file = $"runtime_landmark_{stepIndex:00}_{stepName}.csv";
            string path = Path.Combine(Application.persistentDataPath, file);

            if (!LoadRuntimeLandmarksCsv(path, requireSide, out var front2D, out var side2D, out var front3D, out var side3D))
            {
                Debug.LogWarning("[PNGAnalyze] Runtime landmark CSV missing or load failed (skip): " + path);
                continue;
            }

            sensorProcess.UpdateSensor(in front2D, in side2D, in front3D, in side3D);
            ApplySensorValuesToResult(stepIndex);

            if (stepIndex % 2 == 0)
            {
                yield return null;
            }
        }

        SaveCsvFull();

        _running = false;
    }

    private bool LoadRuntimeLandmarksCsv(
        string csvPath,
        bool requireSide,
        out Landmark2D[] front2D,
        out Landmark2D[] side2D,
        out Landmark3D[] front3D,
        out Landmark3D[] side3D)
    {
        front2D = CreateLandmark2DArray();
        side2D = CreateLandmark2DArray();
        front3D = CreateLandmark3DArray();
        side3D = CreateLandmark3DArray();

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            return false;
        }

        string[] lines = File.ReadAllLines(csvPath);
        if (lines == null || lines.Length <= 1)
        {
            return false;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] c = line.Split(',');
            if (c.Length < 11)
            {
                continue;
            }

            string type = c[0];
            string view = c[1];

            if (!TryParseInt(c[2], out int idx))
            {
                continue;
            }

            if (idx < 0 || idx >= 33)
            {
                continue;
            }

            bool isFront = (view == "F");
            if (!isFront && !requireSide)
            {
                continue;
            }

            if (type == "2D")
            {
                Landmark2D lm = new Landmark2D();
                lm.positionOrg = new Vector2(ParseFloat(c[3]), ParseFloat(c[4]));
                lm.position = new Vector2(ParseFloat(c[5]), ParseFloat(c[6]));
                lm.visibility = ParseFloat(c[10]);

                if (isFront)
                {
                    front2D[idx] = lm;
                }
                else
                {
                    side2D[idx] = lm;
                }
            }
            else if (type == "3D")
            {
                Landmark3D lm = new Landmark3D();
                lm.position = new Vector3(ParseFloat(c[7]), ParseFloat(c[8]), ParseFloat(c[9]));
                lm.visibility = ParseFloat(c[10]);

                if (isFront)
                {
                    front3D[idx] = lm;
                }
                else
                {
                    side3D[idx] = lm;
                }
            }
        }

        return true;
    }

    private Landmark2D[] CreateLandmark2DArray()
    {
        Landmark2D[] a = new Landmark2D[33];
        for (int i = 0; i < 33; i++)
        {
            a[i] = new Landmark2D();
        }
        return a;
    }

    private Landmark3D[] CreateLandmark3DArray()
    {
        Landmark3D[] a = new Landmark3D[33];
        for (int i = 0; i < 33; i++)
        {
            a[i] = new Landmark3D();
        }
        return a;
    }

    private float ParseFloat(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return 0f;
        }

        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
        {
            return v;
        }

        if (float.TryParse(s, out v))
        {
            return v;
        }

        return 0f;
    }

    private bool TryParseInt(string s, out int v)
    {
        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
        {
            return true;
        }

        if (int.TryParse(s, out v))
        {
            return true;
        }

        v = 0;
        return false;
    }

    private void SafeDestroy(Texture2D tex)
    {
        if (tex != null)
        {
            Destroy(tex);
        }
    }

    private int[] CreateStepArrayDefaultValue()
    {
        int[] arr = new int[8];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = missingStepValue;
        }
        return arr;
    }

    private void SetResultData()
    {
        ResultProData.Clear();

        ResultProData.Add("GetHandDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetHandDistance", CreateStepArrayDefaultValue());
        ResultProData.Add("GetShoulderDistance", CreateStepArrayDefaultValue());

        ResultProData.Add("GetSpineDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetShoulderAngle", CreateStepArrayDefaultValue());
        ResultProData.Add("GetFootDisRate", CreateStepArrayDefaultValue());
        ResultProData.Add("GetWeight", CreateStepArrayDefaultValue());
        ResultProData.Add("GetForearmAngle", CreateStepArrayDefaultValue());
        ResultProData.Add("GetElbowFrontDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetElbowRightFrontDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetHandDirDistance", CreateStepArrayDefaultValue());

        ResultProData.Add("GetShoulderFrontDirWorld", CreateStepArrayDefaultValue());
        ResultProData.Add("GetPelvisFrontDirWorld", CreateStepArrayDefaultValue());

        ResultProData.Add("GetPelvisAngle", CreateStepArrayDefaultValue());
        ResultProData.Add("GetNoseDir", CreateStepArrayDefaultValue());

        ResultProData.Add("GetHandSideDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetWaistSideDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetKneeSideDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetElbowSideDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetArmpitDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetHandSideDistance", CreateStepArrayDefaultValue());

        ResultProData.Add("GetGripDistance", CreateStepArrayDefaultValue());
        ResultProData.Add("GetShoulderSideDirWorld", CreateStepArrayDefaultValue());
        ResultProData.Add("GetPelvisSideDirWorld", CreateStepArrayDefaultValue());

        ResultProData.Add("GetNoseShoulderSideDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetNosePelvisSideDir", CreateStepArrayDefaultValue());

        ResultProData.Add("GetShoulderDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetPelvisDir", CreateStepArrayDefaultValue());
        ResultProData.Add("GetHandCombineDir", CreateStepArrayDefaultValue());
    }

    private void ApplySensorValuesToResult(int swingStepIndex)
    {
        ResultProData["GetHandDir"][swingStepIndex] = sensorProcess.iGetHandDir;
        ResultProData["GetHandDistance"][swingStepIndex] = sensorProcess.iGetHandDistance;
        ResultProData["GetShoulderDistance"][swingStepIndex] = sensorProcess.iGetShoulderDistance;

        ResultProData["GetSpineDir"][swingStepIndex] = sensorProcess.iGetSpineDir;
        ResultProData["GetShoulderAngle"][swingStepIndex] = sensorProcess.iGetShoulderAngle;
        ResultProData["GetFootDisRate"][swingStepIndex] = sensorProcess.iGetFootDisRate;
        ResultProData["GetWeight"][swingStepIndex] = sensorProcess.iGetWeight;
        ResultProData["GetForearmAngle"][swingStepIndex] = sensorProcess.iGetForearmAngle;
        ResultProData["GetElbowFrontDir"][swingStepIndex] = sensorProcess.iGetElbowFrontDir;
        ResultProData["GetElbowRightFrontDir"][swingStepIndex] = sensorProcess.iGetElbowRightFrontDir;
        ResultProData["GetHandDirDistance"][swingStepIndex] = sensorProcess.iGetHandDirDistance;

        ResultProData["GetShoulderFrontDirWorld"][swingStepIndex] = sensorProcess.iGetShoulderFrontDirWorld;
        ResultProData["GetPelvisFrontDirWorld"][swingStepIndex] = sensorProcess.iGetPelvisFrontDirWorld;

        ResultProData["GetPelvisAngle"][swingStepIndex] = sensorProcess.iGetPelvisAngle;
        ResultProData["GetNoseDir"][swingStepIndex] = sensorProcess.iGetNoseDir;

        ResultProData["GetHandSideDir"][swingStepIndex] = sensorProcess.iGetHandSideDir;
        ResultProData["GetWaistSideDir"][swingStepIndex] = sensorProcess.iGetWaistSideDir;
        ResultProData["GetKneeSideDir"][swingStepIndex] = sensorProcess.iGetKneeSideDir;
        ResultProData["GetElbowSideDir"][swingStepIndex] = sensorProcess.iGetElbowSideDir;
        ResultProData["GetArmpitDir"][swingStepIndex] = sensorProcess.iGetArmpitDir;
        ResultProData["GetHandSideDistance"][swingStepIndex] = sensorProcess.iGetHandSideDistance;

        ResultProData["GetGripDistance"][swingStepIndex] = sensorProcess.iGetGripDistance;
        ResultProData["GetShoulderSideDirWorld"][swingStepIndex] = sensorProcess.iGetShoulderSideDirWorld;
        ResultProData["GetPelvisSideDirWorld"][swingStepIndex] = sensorProcess.iGetPelvisSideDirWorld;

        ResultProData["GetNoseShoulderSideDir"][swingStepIndex] = sensorProcess.iGetNoseShoulderSideDir;
        ResultProData["GetNosePelvisSideDir"][swingStepIndex] = sensorProcess.iGetNosePelvisSideDir;

        ResultProData["GetShoulderDir"][swingStepIndex] = sensorProcess.iGetShoulderDir;
        ResultProData["GetPelvisDir"][swingStepIndex] = sensorProcess.iGetPelvisDir;

        ResultProData["GetHandCombineDir"][swingStepIndex] = sensorProcess.iGetHandCombineDir;
    }

    private bool SaveCsvFull()
    {
        try
        {
            string output = string.Empty;
            output += "NAME,ADDRESS,TAKEBACK,BACKSWING,TOP,DOWNSWING,IMPACT,FOLLOW,FINISH\r\n";

            output += "GetHandDir," + string.Join(",", ResultProData["GetHandDir"]) + "\r\n";
            output += "GetHandDistance," + string.Join(",", ResultProData["GetHandDistance"]) + "\r\n";
            output += "GetShoulderDistance," + string.Join(",", ResultProData["GetShoulderDistance"]) + "\r\n";

            output += "GetSpineDir," + string.Join(",", ResultProData["GetSpineDir"]) + "\r\n";
            output += "GetShoulderAngle," + string.Join(",", ResultProData["GetShoulderAngle"]) + "\r\n";
            output += "GetFootDisRate," + string.Join(",", ResultProData["GetFootDisRate"]) + "\r\n";
            output += "GetWeight," + string.Join(",", ResultProData["GetWeight"]) + "\r\n";
            output += "GetForearmAngle," + string.Join(",", ResultProData["GetForearmAngle"]) + "\r\n";
            output += "GetElbowFrontDir," + string.Join(",", ResultProData["GetElbowFrontDir"]) + "\r\n";
            output += "GetElbowRightFrontDir," + string.Join(",", ResultProData["GetElbowRightFrontDir"]) + "\r\n";
            output += "GetHandDirDistance," + string.Join(",", ResultProData["GetHandDirDistance"]) + "\r\n";

            output += "GetShoulderFrontDirWorld," + string.Join(",", ResultProData["GetShoulderFrontDirWorld"]) + "\r\n";
            output += "GetPelvisFrontDirWorld," + string.Join(",", ResultProData["GetPelvisFrontDirWorld"]) + "\r\n";

            output += "GetPelvisAngle," + string.Join(",", ResultProData["GetPelvisAngle"]) + "\r\n";
            output += "GetNoseDir," + string.Join(",", ResultProData["GetNoseDir"]) + "\r\n";

            output += "GetHandSideDir," + string.Join(",", ResultProData["GetHandSideDir"]) + "\r\n";
            output += "GetWaistSideDir," + string.Join(",", ResultProData["GetWaistSideDir"]) + "\r\n";
            output += "GetKneeSideDir," + string.Join(",", ResultProData["GetKneeSideDir"]) + "\r\n";
            output += "GetElbowSideDir," + string.Join(",", ResultProData["GetElbowSideDir"]) + "\r\n";
            output += "GetArmpitDir," + string.Join(",", ResultProData["GetArmpitDir"]) + "\r\n";
            output += "GetHandSideDistance," + string.Join(",", ResultProData["GetHandSideDistance"]) + "\r\n";

            output += "GetGripDistance," + string.Join(",", ResultProData["GetGripDistance"]) + "\r\n";
            output += "GetShoulderSideDirWorld," + string.Join(",", ResultProData["GetShoulderSideDirWorld"]) + "\r\n";
            output += "GetPelvisSideDirWorld," + string.Join(",", ResultProData["GetPelvisSideDirWorld"]) + "\r\n";

            output += "GetNoseShoulderSideDir," + string.Join(",", ResultProData["GetNoseShoulderSideDir"]) + "\r\n";
            output += "GetNosePelvisSideDir," + string.Join(",", ResultProData["GetNosePelvisSideDir"]) + "\r\n";

            output += "GetShoulderDir," + string.Join(",", ResultProData["GetShoulderDir"]) + "\r\n";
            output += "GetPelvisDir," + string.Join(",", ResultProData["GetPelvisDir"]) + "\r\n";

            output += "GetHandCombineDir," + string.Join(",", ResultProData["GetHandCombineDir"]);

            string dir = Application.persistentDataPath;
            string fullPath = Path.Combine(dir, "data" + ".csv");
            File.WriteAllText(fullPath, output);

            Debug.Log("[PNGAnalyze] CSV saved: " + fullPath);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[PNGAnalyze] CSV save failed: " + e);
            return false;
        }
    }

    private void SaveStepRuntimeLandmarksCsv(
        int step,
        Landmark2D[] front2D, Landmark3D[] front3D,
        Landmark2D[] side2D, Landmark3D[] side3D)
    {
        try
        {
            string dir = Application.persistentDataPath;
            string stepName = ((SWINGSTEP)step).ToString();
            string file = $"0_3_landmark_{step:00}_{stepName}.csv";
            string path = Path.Combine(dir, file);

            var sb = new System.Text.StringBuilder(8192);
            sb.AppendLine("type,view,idx,orgX,orgY,posX,posY,x,y,z,visibility");

            AppendRuntime2D(sb, "F", front2D);
            AppendRuntime3D(sb, "F", front3D);

            if (requireSide)
            {
                AppendRuntime2D(sb, "S", side2D);
                AppendRuntime3D(sb, "S", side3D);
            }

            File.WriteAllText(path, sb.ToString());
            Debug.Log("[PNGAnalyze] Runtime landmark CSV saved: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[PNGAnalyze] Runtime landmark CSV save failed: " + e);
        }
    }

    private void AppendRuntime2D(System.Text.StringBuilder sb, string view, Landmark2D[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 33)
        {
            return;
        }

        for (int i = 0; i < 33; i++)
        {
            Landmark2D p = landmarks[i];

            sb.Append("2D").Append(',')
              .Append(view).Append(',')
              .Append(i).Append(',')
              .Append(p.positionOrg.x.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.positionOrg.y.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.x.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.y.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(",,").Append(',')
              .Append(p.visibility.ToString("0.######", CultureInfo.InvariantCulture))
              .AppendLine();
        }
    }

    private void AppendRuntime3D(System.Text.StringBuilder sb, string view, Landmark3D[] landmarks)
    {
        if (landmarks == null || landmarks.Length < 33)
        {
            return;
        }

        for (int i = 0; i < 33; i++)
        {
            Landmark3D p = landmarks[i];

            sb.Append("3D").Append(',')
              .Append(view).Append(',')
              .Append(i).Append(',')
              .Append(",,,,")
              .Append(p.position.x.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.y.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.position.z.ToString("0.######", CultureInfo.InvariantCulture)).Append(',')
              .Append(p.visibility.ToString("0.######", CultureInfo.InvariantCulture))
              .AppendLine();
        }
    }

    private NormalizedLandmarks DetectOne(Texture2D inputTexture)
    {
        if (inputTexture == null || inputTexture.width <= 0 || inputTexture.height <= 0)
        {
            return default;
        }

        EnsurePoseLM(inputTexture.width, inputTexture.height, false);

        _tfAnalyzer.ReadTextureOnCPU(inputTexture, flipHorizontally: false, flipVertically: false);

        using (Mediapipe.Image cpuImage = _tfAnalyzer.BuildCPUImage())
        {
            PoseLandmarkerResult result = _offlinePoseLM.Detect(cpuImage);

            bool hasAny = result.poseLandmarks != null && result.poseLandmarks.Count > 0;
            if (hasAny)
            {
                return result.poseLandmarks[0];
            }
        }

        return default;
    }

    private Landmarks DetectOneWorld(Texture2D inputTexture)
    {
        if (inputTexture == null)
        {
            return default;
        }

        EnsurePoseLM(inputTexture.width, inputTexture.height, false);

        _tfAnalyzer.ReadTextureOnCPU(inputTexture, flipHorizontally: false, flipVertically: false);

        using (Mediapipe.Image cpuImage = _tfAnalyzer.BuildCPUImage())
        {
            PoseLandmarkerResult result = _offlinePoseLM.Detect(cpuImage);

            if (result.poseWorldLandmarks != null && result.poseWorldLandmarks.Count > 0)
            {
                return result.poseWorldLandmarks[0];
            }
        }

        return default;
    }

    private void EnsurePoseLM(int pixelWidth, int pixelHeight, bool forVideoMode)
    {
        if (_offlinePoseLM == null || _poseLMForVideo != forVideoMode)
        {
            try
            {
                if (_offlinePoseLM != null)
                {
                    _offlinePoseLM.Close();
                }
            }
            catch { }

            _offlinePoseLM = null;

            TextAsset modelBytes = Resources.Load<TextAsset>("pose_landmarker_full");
            if (modelBytes == null)
            {
                Debug.LogError("[PNGAnalyze] pose_landmarker_full not found in Resources");
                return;
            }

            Mediapipe.Tasks.Core.BaseOptions baseOptions =
                new Mediapipe.Tasks.Core.BaseOptions(
                    Mediapipe.Tasks.Core.BaseOptions.Delegate.CPU,
                    modelAssetBuffer: modelBytes.bytes
                );

            Mediapipe.Tasks.Vision.Core.RunningMode runningMode =
                forVideoMode ? Mediapipe.Tasks.Vision.Core.RunningMode.VIDEO
                             : Mediapipe.Tasks.Vision.Core.RunningMode.IMAGE;

            PoseLandmarkerOptions poseOptions =
                new PoseLandmarkerOptions(
                    baseOptions: baseOptions,
                    runningMode: runningMode
                );

            _offlinePoseLM = PoseLandmarker.CreateFromOptions(poseOptions);
            _poseLMForVideo = forVideoMode;
        }

        if (_tfAnalyzer == null || _tfAnalyzer.width != pixelWidth || _tfAnalyzer.height != pixelHeight)
        {
            try
            {
                if (_tfAnalyzer != null)
                {
                    _tfAnalyzer.Dispose();
                }
            }
            catch { }

            _tfAnalyzer = new TextureFrame(pixelWidth, pixelHeight, TextureFormat.RGBA32);
        }
    }

    private Landmark2D[] ConvertToLandmark2DArray(NormalizedLandmarks normalized, bool isFront)
    {
        if (normalized.landmarks == null || normalized.landmarks.Count == 0)
        {
            return null;
        }

        RawImage refImage = isFront ? rawImageFrontRef : rawImageSideRef;
        if (refImage == null || refImage.rectTransform == null)
        {
            return null;
        }

        Landmark2D[] landmarks = new Landmark2D[33];
        UnityEngine.Rect rect = refImage.rectTransform.rect;

        for (int i = 0; i < 33; i++)
        {
            float vis = (float)normalized.landmarks[i].visibility;

            landmarks[i] = new Landmark2D();
            landmarks[i].positionOrg = new Vector2((float)normalized.landmarks[i].x, (float)normalized.landmarks[i].y);
            landmarks[i].visibility = vis;
            landmarks[i].position = rect.GetPoint(normalized.landmarks[i]);
        }

        return landmarks;
    }

    private Landmark3D[] ConvertToLandmark3DArray(Landmarks world)
    {
        if (world.landmarks == null || world.landmarks.Count == 0)
        {
            return null;
        }

        Landmark3D[] landmarks = new Landmark3D[33];

        for (int i = 0; i < 33; i++)
        {
            landmarks[i] = new Landmark3D();

            float wy = (float)world.landmarks[i].y;
            float wx = (float)world.landmarks[i].x;
            float wz = (float)world.landmarks[i].z;
            float visibility = (float)world.landmarks[i].visibility;

            Vector3 mapped = new Vector3(-wy, -wx, wz);

            landmarks[i].position = mapped;
            landmarks[i].visibility = visibility;
        }

        return landmarks;
    }

    private void OnDestroy()
    {
        try
        {
            if (_tfAnalyzer != null)
            {
                _tfAnalyzer.Dispose();
            }
        }
        catch { }

        _tfAnalyzer = null;

        try
        {
            if (_offlinePoseLM != null)
            {
                _offlinePoseLM.Close();
            }
        }
        catch { }

        _offlinePoseLM = null;
    }
}