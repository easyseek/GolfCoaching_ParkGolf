using UnityEngine;
using Enums;
using System.Collections.Generic;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Net;
using System.IO;
using System.Globalization;

public static class LandmarkCsvLoader
{
    public struct Landmarks
    {
        public Landmark2D[] front2D;
        public Landmark2D[] side2D;
        public Landmark3D[] front3D;
        public Landmark3D[] side3D;
    }

    public static bool Load(string csvPath, bool requireSide, out Landmarks result)
    {
        result = new Landmarks
        {
            front2D = CreateLandmark2DArray(),
            side2D = CreateLandmark2DArray(),
            front3D = CreateLandmark3DArray(),
            side3D = CreateLandmark3DArray(),
        };

        List<Dictionary<string, object>> rows = null;
        try
        {
            rows = CSVReader.ReadCSV(csvPath);
        }
        catch
        {
            //return false;
        }

        // if (rows == null || rows.Count == 0)
        //     return false;

        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row == null) continue;

            if (!row.TryGetValue("type", out object typeObj)) continue;
            if (!row.TryGetValue("view", out object viewObj)) continue;
            if (!row.TryGetValue("idx", out object idxObj)) continue;

            string type = (typeObj != null) ? typeObj.ToString() : string.Empty;
            string view = (viewObj != null) ? viewObj.ToString() : string.Empty;

            if (!TryParseInt(idxObj, out int idx)) continue;
            if (idx < 0 || idx >= 33) continue;

            bool isFront = (view == "F");
            if (!isFront && !requireSide)
                continue;

            if (type == "2D")
            {
                if (!row.TryGetValue("orgX", out object orgXObj)) orgXObj = null;
                if (!row.TryGetValue("orgY", out object orgYObj)) orgYObj = null;
                if (!row.TryGetValue("posX", out object posXObj)) posXObj = null;
                if (!row.TryGetValue("posY", out object posYObj)) posYObj = null;
                if (!row.TryGetValue("visibility", out object visObj)) visObj = null;

                Landmark2D lm = new Landmark2D();
                lm.positionOrg = new Vector2(ParseFloat(orgXObj), ParseFloat(orgYObj));
                lm.position = new Vector2(ParseFloat(posXObj), ParseFloat(posYObj));
                lm.visibility = ParseFloat(visObj);

                if (isFront) result.front2D[idx] = lm;
                else result.side2D[idx] = lm;
            }
            else if (type == "3D")
            {
                if (!row.TryGetValue("x", out object xObj)) xObj = null;
                if (!row.TryGetValue("y", out object yObj)) yObj = null;
                if (!row.TryGetValue("z", out object zObj)) zObj = null;
                if (!row.TryGetValue("visibility", out object visObj)) visObj = null;

                Landmark3D lm = new Landmark3D();
                lm.position = new Vector3(ParseFloat(xObj), ParseFloat(yObj), ParseFloat(zObj));
                lm.visibility = ParseFloat(visObj);

                if (isFront) result.front3D[idx] = lm;
                else result.side3D[idx] = lm;
            }
        }

        return true;
    }

    private static Landmark2D[] CreateLandmark2DArray()
    {
        Landmark2D[] a = new Landmark2D[33];
        for (int i = 0; i < 33; i++) a[i] = new Landmark2D();
        return a;
    }

    private static Landmark3D[] CreateLandmark3DArray()
    {
        Landmark3D[] a = new Landmark3D[33];
        for (int i = 0; i < 33; i++) a[i] = new Landmark3D();
        return a;
    }

    private static float ParseFloat(object obj)
    {
        if (obj == null) return 0f;

        string s = obj.ToString();
        if (string.IsNullOrEmpty(s)) return 0f;

        if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            return v;

        if (float.TryParse(s, out v))
            return v;

        return 0f;
    }

    private static bool TryParseInt(object obj, out int v)
    {
        v = 0;
        if (obj == null) return false;

        string s = obj.ToString();
        if (string.IsNullOrEmpty(s)) return false;

        return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)
            || int.TryParse(s, out v);
    }
}

public class ProData
{
    public int uid;
    public string name;
    public int hide;
}

public class ProInfoData
{
    public int uid;
    public string name;
    public int gender;
    public string info;
    public string introduce;
    public string history;
    public EFilter[] filters = new EFilter[3];
    public int favoriteCount;
    public int popularity;
    public int views;
    public string recently;
}

public class SelectProData
{
    public int uid;
    public ProInfoData infoData;
    public List<ProVideoData> videoData = new List<ProVideoData>();
    public List<ProImageData> imageData = new List<ProImageData>();
    public ProSwingData swingData = new ProSwingData();
    public ProSwingData aiSwingData = new ProSwingData();
    public ProLandmarkData landmarkData = new ProLandmarkData();
    public ProLandmarkData aiLandmarkData = new ProLandmarkData();
}

public class ProVideoData
{
    public int uid = 0;
    public int id = 0;
    public string name;
    public string path;
    public EPoseDirection direction;
    //public ESceneType sceneType;
    public EVideoType videoType;
    public ESwingType swingType;
    public EClub clubFilter;
    public EStance poseFilter;
    public int favoriteCount;
    public int views;
    public string recently;
}

public class ProImageData
{
    public int uid = 0;
    public string name;
    public string path;
    public EImageType imageType;
}

public class ProSwingData
{
    public int uid = 0;
    public Dictionary<EClub, ProSwingStepData> dicFull = new Dictionary<EClub, ProSwingStepData>();
    public Dictionary<EClub, ProSwingStepData> dicQuarter = new Dictionary<EClub, ProSwingStepData>();
    public Dictionary<EClub, ProSwingStepData> dicHalf = new Dictionary<EClub, ProSwingStepData>();
}

public class ProSwingStepData
{
    public int uid = 0;
    public Dictionary<string, int> dicAddress = new Dictionary<string, int>();
    public Dictionary<string, int> dicTakeback = new Dictionary<string, int>();
    public Dictionary<string, int> dicBackswing = new Dictionary<string, int>();
    public Dictionary<string, int> dicTop = new Dictionary<string, int>();
    public Dictionary<string, int> dicDownswing = new Dictionary<string, int>();
    public Dictionary<string, int> dicImpact = new Dictionary<string, int>();
    public Dictionary<string, int> dicFollow = new Dictionary<string, int>();
    public Dictionary<string, int> dicFinish = new Dictionary<string, int>();
}

public class ProLandmarkData
{
    public int uid = 0;

    public Dictionary<EClub, ProLandmarkStepPaths> dicFull = new Dictionary<EClub, ProLandmarkStepPaths>();
    public Dictionary<EClub, ProLandmarkStepPaths> dicQuarter = new Dictionary<EClub, ProLandmarkStepPaths>();
    public Dictionary<EClub, ProLandmarkStepPaths> dicHalf = new Dictionary<EClub, ProLandmarkStepPaths>();
}

public class ProLandmarkStepPaths
{
    public int uid = 0;

    public Dictionary<SWINGSTEP, LandmarkCsvLoader.Landmarks> stepLandmarks = new Dictionary<SWINGSTEP, LandmarkCsvLoader.Landmarks>();
}

public class GolfProDataManager : MonoBehaviourSingleton<GolfProDataManager>
{
    private SelectProData selectProData = new SelectProData();
    public SelectProData SelectProData
    {
        get { return selectProData; }
        set { selectProData = value; }
    }

    private List<ProData> proDataList = new List<ProData>();

    private Dictionary<int, ProInfoData> proInfoDataDic = null;
    private Dictionary<int, List<ProVideoData>> proVideoDataDic = null;
    private Dictionary<int, List<ProImageData>> proImageDataDic = null;
    private Dictionary<int, ProSwingData> proSwingDataDic = null;
    private Dictionary<int, ProSwingData> proAISwingDataDic = null;
    private Dictionary<int, ProLandmarkData> proLandmarkDataDic = null;
    private Dictionary<int, ProLandmarkData> proAILandmarkDataDic = null;

    private static readonly SWINGSTEP[] StepsFull =
    {
        SWINGSTEP.ADDRESS,
        SWINGSTEP.TAKEBACK,
        SWINGSTEP.BACKSWING,
        SWINGSTEP.TOP,
        SWINGSTEP.DOWNSWING,
        SWINGSTEP.IMPACT,
        SWINGSTEP.FOLLOW,
        SWINGSTEP.FINISH
    };

    private static readonly SWINGSTEP[] StepsHalf =
    {
        SWINGSTEP.ADDRESS,
        SWINGSTEP.TAKEBACK,
        SWINGSTEP.IMPACT,
        SWINGSTEP.FOLLOW
    };

    private static readonly SWINGSTEP[] StepsThreeQuarter =
    {
        SWINGSTEP.ADDRESS,
        SWINGSTEP.TAKEBACK,
        SWINGSTEP.BACKSWING,
        SWINGSTEP.DOWNSWING,
        SWINGSTEP.IMPACT,
        SWINGSTEP.FOLLOW
    };

    public void LoadProData()
    {
        StartCoroutine(LoadData());
    }

    private IEnumerator LoadData()
    {
        const float minimumLoadingTime = 2.0f;
        float loadingStartedAt = Time.realtimeSinceStartup;

        bool bProTable = LoadProTable();
        bool bProInfoTable = LoadProInfoData();
        bool bProVideoTable = LoadProVideoData();
        bool bProImageTable = LoadProImageData();
        bool bProSwingTable = LoadProSwingData();
        bool bProAISwingTable = LoadProSwingData(true);
        bool bProLandmarkTable = LoadProLandmarkData(false);
        //bool bProAILandmarkTable = LoadProLandmarkData(true);

        bool isLoaded = bProTable && bProInfoTable && bProVideoTable && bProImageTable &&
            bProSwingTable && bProAISwingTable && bProLandmarkTable;

        if (!isLoaded)
        {
            Debug.Log("프로 데이터 로딩에 실패했습니다.");
            yield break;
        }

        float remainingLoadingTime = minimumLoadingTime - (Time.realtimeSinceStartup - loadingStartedAt);
        if (remainingLoadingTime > 0f)
        {
            yield return new WaitForSecondsRealtime(remainingLoadingTime);
        }

        GameManager.Instance.SelectedSceneName = "Login";

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Login");

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private bool LoadProTable()
    {
        var tableData = CSVReader.ReadCSV(INI.proDataPath);

        foreach (var row in tableData)
        {
            if (row.ContainsKey("Uid") && row.ContainsKey("Name"))
            {
                ProData entry = new ProData();

                entry.uid = Convert.ToInt32(row["Uid"]);
                entry.name = row["Name"].ToString();
                entry.hide = Convert.ToInt32(row["Hide"]);

                //Debug.Log($"[LoadProTable] uid : {entry.uid}, name : {entry.name}, hide : {entry.hide}");

                if (entry.hide != 1)
                    proDataList.Add(entry);
            }
            else
            {
                Debug.LogWarning("TablePro.csv에 필수 컬럼이 누락되었습니다.");
                //return false;
            }
        }

        return true;
    }

    private bool LoadProInfoData()
    {
        if (proInfoDataDic == null)
            proInfoDataDic = new Dictionary<int, ProInfoData>();
        else
            proInfoDataDic.Clear();

        foreach (ProData list in proDataList)
        {
            var detailDataList = CSVReader.ReadCSV($"{INI.proInfoPath}{list.uid}");

            if (detailDataList != null && detailDataList.Count != 0)
            {
                foreach (var item in detailDataList)
                {
                    ProInfoData detailData = new ProInfoData();

                    try
                    {
                        detailData.uid = list.uid;
                        if (item.ContainsKey("Name")) detailData.name = item["Name"].ToString();
                        if (item.ContainsKey("Gender")) detailData.gender = Convert.ToInt32(item["Gender"]);
                        if (item.ContainsKey("Info")) detailData.info = item["Info"].ToString();
                        if (item.ContainsKey("Introduce")) detailData.introduce = item["Introduce"].ToString();
                        if (item.ContainsKey("History")) detailData.history = item["History"].ToString();

                        for (int i = 0; i < 3; i++)
                        {
                            if (item.ContainsKey($"Filter{i}"))
                            {
                                detailData.filters[i] = Utillity.Instance.StringToEnum<EFilter>(item[$"Filter{i}"].ToString());
                            }
                        }

                        if (item.ContainsKey("FavoriteCount")) detailData.favoriteCount = Convert.ToInt32(item["FavoriteCount"]);
                        if (item.ContainsKey("Popularity")) detailData.popularity = Convert.ToInt32(item["Popularity"]);
                        if (item.ContainsKey("Views")) detailData.views = Convert.ToInt32(item["Views"]);
                        if (item.ContainsKey("Recently")) detailData.recently = item["Recently"].ToString();


                        //Debug.Log($"[LoadProInfoData] {detailData.name}, {detailData.gender}, {detailData.info}, {detailData.proImage}, {detailData.profileImage}, {detailData.frontVideo}, {detailData.sideVideo}, {detailData.introduce}, {detailData.filters[0]}, {detailData.filters[1]}, {detailData.filters[2]}");

                        proInfoDataDic[list.uid] = detailData;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"프로 상세 CSV 파싱 중 예외 발생: uid {list.uid}, {ex.Message}");
                        //return false;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"프로 상세 CSV 파일이 비어있습니다. uid: {list.uid}");
                //return false;
            }
        }

        return true;
    }

    private bool LoadProVideoData()
    {
        if (proVideoDataDic == null)
            proVideoDataDic = new Dictionary<int, List<ProVideoData>>();
        else
            proVideoDataDic.Clear();

        foreach (ProData list in proDataList)
        {
            var detailDataList = CSVReader.ReadCSV($"{INI.proVideoPath}{list.uid}/{list.uid}");

            if (detailDataList != null && detailDataList.Count != 0)
            {
                List<ProVideoData> detailData = new List<ProVideoData>();

                foreach (var item in detailDataList)
                {
                    ProVideoData proVideoData = new ProVideoData();

                    try
                    {
                        proVideoData.uid = list.uid;
                        if (item.ContainsKey("Id")) proVideoData.id = Convert.ToInt32(item["Id"]);
                        if (item.ContainsKey("Name")) proVideoData.name = item["Name"].ToString();
                        if (item.ContainsKey("Path")) proVideoData.path = item["Path"].ToString();
                        if (item.ContainsKey("Direction")) proVideoData.direction = Utillity.Instance.StringToEnum<EPoseDirection>(item["Direction"].ToString());
                        if (item.ContainsKey("VideoType")) proVideoData.videoType = Utillity.Instance.StringToEnum<EVideoType>(item["VideoType"].ToString());
                        if (item.ContainsKey("SwingType")) proVideoData.swingType = Utillity.Instance.StringToEnum<ESwingType>(item["SwingType"].ToString());
                        if (item.ContainsKey("ClubFilter")) proVideoData.clubFilter = Utillity.Instance.StringToEnum<EClub>(item["ClubFilter"].ToString());
                        if (item.ContainsKey("PoseFilter")) proVideoData.poseFilter = Utillity.Instance.StringToEnum<EStance>(item["PoseFilter"].ToString());
                        if (item.ContainsKey("FavoriteCount")) proVideoData.favoriteCount = Convert.ToInt32(item["FavoriteCount"]);
                        if (item.ContainsKey("Views")) proVideoData.views = Convert.ToInt32(item["Views"]);
                        if (item.ContainsKey("Recently")) proVideoData.recently = item["Recently"].ToString();

                        //Debug.Log($"[LoadProVideoData] {detailData.name}, {detailData.path}, {detailData.sceneType}, {detailData.clubFilter}, {detailData.poseFilter}, {detailData.recommendFilter}, {detailData.priority}");

                        detailData.Add(proVideoData);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"프로 비디오 CSV 파싱 중 예외 발생: uid {list.uid}, {ex.Message}");
                        //return false;
                    }
                }

                proVideoDataDic.Add(list.uid, detailData);
            }
            else
            {
                Debug.LogWarning($"프로 비디오 CSV 파일이 비어있습니다. uid: {list.uid}");
                //return false;
            }
        }

        return true;
    }

    private bool LoadProImageData()
    {
        if (proImageDataDic == null)
            proImageDataDic = new Dictionary<int, List<ProImageData>>();
        else
            proImageDataDic.Clear();

        foreach (ProData list in proDataList)
        {
            var detailDataList = CSVReader.ReadCSV($"{INI.proImagePath}{list.uid}/{list.uid}");

            if (detailDataList != null && detailDataList.Count != 0)
            {
                List<ProImageData> detailData = new List<ProImageData>();

                foreach (var item in detailDataList)
                {
                    ProImageData proImageData = new ProImageData();

                    try
                    {
                        proImageData.uid = list.uid;
                        if (item.ContainsKey("Name")) proImageData.name = item["Name"].ToString();
                        if (item.ContainsKey("Path")) proImageData.path = item["Path"].ToString();
                        if (item.ContainsKey("ImageType")) proImageData.imageType = Utillity.Instance.StringToEnum<EImageType>(item["ImageType"].ToString());

                        detailData.Add(proImageData);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"프로 이미지 CSV 파싱 중 예외 발생: uid {list.uid}, {ex.Message}");
                        //return false;
                    }
                }

                proImageDataDic.Add(list.uid, detailData);
            }
            else
            {
                Debug.LogWarning($"프로 이미지 CSV 파일이 비어있습니다. uid: {list.uid}");
                //return false;
            }
        }

        return true;
    }

    private bool LoadProSwingData(bool isAISwing = false)
    {
        if (!isAISwing)
        {
            if (proSwingDataDic == null)
                proSwingDataDic = new Dictionary<int, ProSwingData>();
            else
                proSwingDataDic.Clear();
        }
        else
        {
            if (proAISwingDataDic == null)
                proAISwingDataDic = new Dictionary<int, ProSwingData>();
            else
                proAISwingDataDic.Clear();
        }

        foreach (ProData list in proDataList)
        {
            try
            {
                EnsureProSwingFile(list.uid);

                List<Dictionary<string, object>> detailDataList = null;
                detailDataList = CSVReader.ReadCSV($"{INI.proSwingPath}{list.uid}/{list.uid}");

                if (detailDataList != null && detailDataList.Count != 0)
                {
                    ProSwingData detailData = new ProSwingData();
                    detailData.uid = list.uid;

                    foreach (var item in detailDataList)
                    {
                        int swingIndex = 0;
                        int clbuIndex = 0;
                        string dataPath = string.Empty;
                        if (item.ContainsKey("SWING")) swingIndex = Convert.ToInt32(item["SWING"]);
                        if (item.ContainsKey("CLUB")) clbuIndex = Convert.ToInt32(item["CLUB"]);
                        if (item.ContainsKey("PATH")) dataPath = item["PATH"].ToString();

                        if (isAISwing)
                            dataPath = dataPath.Replace(".csv", "_ai").Replace(".CSV", "_ai");
                        else
                            dataPath = dataPath.Replace(".csv", "").Replace(".CSV", "");

                        var detailStepDataList = CSVReader.ReadCSV($"{INI.proSwingPath}{list.uid}/{dataPath}");
                        ProSwingStepData proSwingStepData = new ProSwingStepData();

                        foreach (var data in detailStepDataList)
                        {
                            proSwingStepData.dicAddress.Add(data["NAME"].ToString(), int.Parse(data["ADDRESS"].ToString()));
                            proSwingStepData.dicTakeback.Add(data["NAME"].ToString(), int.Parse(data["TAKEBACK"].ToString()));
                            proSwingStepData.dicBackswing.Add(data["NAME"].ToString(), int.Parse(data["BACKSWING"].ToString()));
                            proSwingStepData.dicTop.Add(data["NAME"].ToString(), int.Parse(data["TOP"].ToString()));
                            proSwingStepData.dicDownswing.Add(data["NAME"].ToString(), int.Parse(data["DOWNSWING"].ToString()));
                            proSwingStepData.dicImpact.Add(data["NAME"].ToString(), int.Parse(data["IMPACT"].ToString()));
                            proSwingStepData.dicFollow.Add(data["NAME"].ToString(), int.Parse(data["FOLLOW"].ToString()));
                            proSwingStepData.dicFinish.Add(data["NAME"].ToString(), int.Parse(data["FINISH"].ToString()));
                        }

                        if (swingIndex == 0)
                            detailData.dicFull.Add((EClub)clbuIndex, proSwingStepData);
                        else if (swingIndex == 1)
                            detailData.dicQuarter.Add((EClub)clbuIndex, proSwingStepData);
                        else // if (swingIndex == 2)
                            detailData.dicHalf.Add((EClub)clbuIndex, proSwingStepData);
                    }

                    if (isAISwing)
                        proAISwingDataDic.Add(list.uid, detailData);
                    else
                        proSwingDataDic.Add(list.uid, detailData);
                }
                else
                {
                    Debug.LogWarning($"프로 스윙 데이터 파일이 비어있습니다. uid: {list.uid}");
                    //return false;
                }
            }
            catch
            {
                Debug.LogWarning($"프로 스윙 데이터 파일이 없습니다. uid: {list.uid}");
                //return false;
            }

        }

        return true;
    }

    private bool LoadProLandmarkData(bool isAISwing)
    {
        if (!isAISwing)
        {
            if (proLandmarkDataDic == null) proLandmarkDataDic = new Dictionary<int, ProLandmarkData>();
            else proLandmarkDataDic.Clear();
        }
        else
        {
            if (proAILandmarkDataDic == null) proAILandmarkDataDic = new Dictionary<int, ProLandmarkData>();
            else proAILandmarkDataDic.Clear();
        }

        Dictionary<int, ProLandmarkData> targetDic = isAISwing ? proAILandmarkDataDic : proLandmarkDataDic;

        foreach (ProData list in proDataList)
        {
            try
            {
                ProLandmarkData landmarkData = new ProLandmarkData();
                landmarkData.uid = list.uid;

                // DataBase_park/ProSwing/{uid}/landmark/
                string landmarkFolderName = isAISwing ? "landmark_ai" : "landmark";
                string landmarkDir = $"{INI.proSwingPath}{list.uid}/{landmarkFolderName}/";

                ESwingType[] swingTypes = { ESwingType.Full, ESwingType.ThreeQuarter, ESwingType.Half };
                EClub[] clubs = { EClub.Driver, EClub.MiddleIron };

                for (int si = 0; si < swingTypes.Length; si++)
                {
                    ESwingType swingType = swingTypes[si];
                    int swingIndex = (int)swingType;

                    SWINGSTEP[] steps = GetStepsForSwingType(swingType);

                    for (int ci = 0; ci < clubs.Length; ci++)
                    {
                        EClub club = clubs[ci];
                        int clubIndex = (int)club;

                        ProLandmarkStepPaths stepData = new ProLandmarkStepPaths();
                        stepData.uid = list.uid;

                        string prefix = $"{swingIndex}_{clubIndex}";

                        for (int k = 0; k < steps.Length; k++)
                        {
                            SWINGSTEP step = steps[k];
                            int stepIndex = (int)step;
                            string stepName = step.ToString();

                            string stepCsvPath = $"{landmarkDir}{prefix}_landmark_{stepIndex:00}_{stepName}";

                            if (LandmarkCsvLoader.Load(stepCsvPath, requireSide: true, out var lm))
                            {
                                stepData.stepLandmarks[step] = lm;
                            }
                            else
                            {
                                Debug.Log($"[ProLandmark] Missing: uid={list.uid}, isAI={isAISwing}, path={stepCsvPath}");
                            }
                        }

                        if (swingType == ESwingType.Full)
                            landmarkData.dicFull[club] = stepData;
                        else if (swingType == ESwingType.ThreeQuarter)
                            landmarkData.dicQuarter[club] = stepData;
                        else
                            landmarkData.dicHalf[club] = stepData;
                    }
                }

                targetDic[list.uid] = landmarkData;
            }
            catch
            {
            }
        }

        return true;
    }

    public void ReloadProVideoData()
    {
        LoadProVideoData();

        SelectProData.videoData = GetProVideoDataList(selectProData.uid);
    }

    public void ReloadProSwingData()
    {
        if (SelectProData == null)
            return;

        int uid = SelectProData.uid;

        LoadProSwingData(false);
        LoadProSwingData(true);

        LoadProLandmarkData(false);
        //LoadProLandmarkData(true);

        SelectProData.swingData = GetSwingData(uid);
        SelectProData.aiSwingData = GetAISwingData(uid);

        SelectProData.landmarkData = GetLandmarkData(uid);
        //SelectProData.aiLandmarkData = GetAILandmarkData(uid);

        Debug.Log($"[GolfProDataManager] ReloadProSwingData complete uid={uid}");
    }

    public ProData GetProData(string value)
    {
        foreach (var data in proDataList)
        {
            if (data.name == value)
                return data;
        }

        return null;
    }

    public ProInfoData GetProInfoData(int uid)
    {
        this.proInfoDataDic.TryGetValue(uid, out ProInfoData temp);
        return temp;
    }

    public bool ContainsKey(int uid)
    {
        return proInfoDataDic.ContainsKey(uid);
    }

    public List<ProData> GetProDataList()
    {
        return proDataList;
    }

    public Dictionary<int, ProInfoData> GetProInfoList()
    {
        return proInfoDataDic;
    }

    public List<ProVideoData> GetProVideoDataList(int uid)
    {
        this.proVideoDataDic.TryGetValue(uid, out List<ProVideoData> temp);
        return temp;
    }

    public Dictionary<int, List<ProVideoData>> GetProVideoDic()
    {
        return proVideoDataDic;
    }

    public List<ProImageData> GetProImageDataList(int uid)
    {
        this.proImageDataDic.TryGetValue(uid, out List<ProImageData> temp);
        return temp;
    }

    public Dictionary<int, List<ProImageData>> GetProImageDic()
    {
        return proImageDataDic;
    }

    public ProImageData GetProImageData(int uid, EImageType type)
    {
        if (this.proImageDataDic.TryGetValue(uid, out var list))
        {
            return list.SingleOrDefault(v => v.imageType == type);
        }
        else
            return null;
    }

    public ProSwingData GetSwingData(int uid)
    {
        this.proSwingDataDic.TryGetValue(uid, out ProSwingData temp);
        return temp;
    }

    public ProSwingData GetAISwingData(int uid)
    {
        this.proAISwingDataDic.TryGetValue(uid, out ProSwingData temp);
        return temp;
    }

    public ProLandmarkData GetLandmarkData(int uid)
    {
        if (proLandmarkDataDic == null)
            return null;

        proLandmarkDataDic.TryGetValue(uid, out ProLandmarkData data);

        return data;
    }

    public ProLandmarkData GetAILandmarkData(int uid)
    {
        if (proAILandmarkDataDic == null)
            return null;

        proAILandmarkDataDic.TryGetValue(uid, out ProLandmarkData data);

        return data;
    }

    public ProLandmarkStepPaths GetLandmarkStepData(int uid, bool isAISwing, ESwingType swing, EClub club)
    {
        Debug.Log($"[GetLandmarkStepData]{uid}, {isAISwing}, {swing}, {club}");
        ProLandmarkData lm = isAISwing ? GetAILandmarkData(uid) : GetLandmarkData(uid);

        if (lm == null)
            return null;

        if (swing == ESwingType.Full)
        {
            if (!lm.dicFull.TryGetValue(club, out ProLandmarkStepPaths s) || s == null)
                return null;

            return s;
        }

        if (swing == ESwingType.ThreeQuarter)
        {
            if (!lm.dicQuarter.TryGetValue(club, out ProLandmarkStepPaths s) || s == null)
                return null;

            return s;
        }

        if (!lm.dicHalf.TryGetValue(club, out ProLandmarkStepPaths h) || h == null)
            return null;

        return h;
    }

    private SWINGSTEP[] GetStepsForSwingType(ESwingType swingType)
    {
        if (swingType == ESwingType.Full)
            return StepsFull;

        if (swingType == ESwingType.ThreeQuarter)
            return StepsThreeQuarter;

        if (swingType == ESwingType.Half)
            return StepsHalf;

        return StepsFull;
    }

    private void EnsureProSwingFile(int uid)
    {
        string homeDir = Environment.GetEnvironmentVariable("HOME");
        string rootBasePath = string.Empty;

        if (!string.IsNullOrEmpty(homeDir))
        {
            string homePath = Path.Combine(homeDir, INI.proSwingPath);

            if (Directory.Exists(homePath) || INI.proSwingPath.StartsWith("DataBase_park"))
            {
                rootBasePath = homePath;
            }
        }

        if (string.IsNullOrEmpty(rootBasePath))
        {
            rootBasePath = Path.Combine(@"C:\", INI.proSwingPath);
        }

        string uidFolderPath = Path.Combine(rootBasePath, uid.ToString());
        string uidCsvPath = Path.Combine(uidFolderPath, $"{uid}.csv");

        try
        {
            if (!Directory.Exists(uidFolderPath))
            {
                Directory.CreateDirectory(uidFolderPath);
            }

            if (!File.Exists(uidCsvPath))
            {
                File.WriteAllText(uidCsvPath, "SWING,CLUB,PATH\r\n", new System.Text.UTF8Encoding(true));
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EnsureProSwingFile] uid={uid}, error={ex.Message}");
        }
    }
}
