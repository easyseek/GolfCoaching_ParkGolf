using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Burst;
using System.Collections;
using DG.Tweening;
using System.Threading;
using System.IO.Pipes;
using System.IO;
using System;
using System.Diagnostics;
using UnityEngine.DedicatedServer;
using Debug = UnityEngine.Debug;
using TMPro;
using System.Diagnostics.Tracing;
using System.Text;
using Unity.VisualScripting;
//using Michsky.LSS;

//[ShowOdinSerializedPropertiesInInspector]
[BurstCompile]


public class LoginDirector : MonoBehaviour//SerializedMonoBehaviour, ISerializationCallbackReceiver
{
    //[SerializeField]  webcamclient wcclient;

    // -----------------------------------------------------------
    public CanvasGroup m_LoginPanel;

    //private Thread pipe1ClientThread;
    //private Thread pipe2ClientThread;

    //public string PIPE1_NAME = "skeleton_pipe1";
    //public string PIPE2_NAME = "skeleton_pipe2";

    public float m_FadeDuration = 1.0f;

    private bool isRunning = false;
    private bool isConnected = false;

    [SerializeField] TextMeshProUGUI txtVer;
    [SerializeField] private QrLoginImage qrLoginImage;
    [SerializeField] private GameObject timePanel;

    [Header("Kiosk QR Login Test")]
    [SerializeField] private bool requestQrOnStart = true;
    /*
    [SerializeField] private string kioskBaseUrl = "http://develop.csdll.co.kr/Baekdori";
    [SerializeField] private string kioskId = "k1";
    [SerializeField] private string kioskPassword = "1234";
*/

/*    private const string KioskTokenKey = "KioskQrLogin.Token";
    private const string KioskTokenExpiresAtKey = "KioskQrLogin.TokenExpiresAt";
    private const string KioskTokenIdentityKey = "KioskQrLogin.TokenIdentity";
    private const long TokenExpiryMarginSeconds = 60;
    private bool isQrLoginLinkRequestRunning;
*/
    [Serializable]
    private class KioskApiResponse
    {
        public string State;
        public string Message;
        public KioskLinkData Data;
    }

    [Serializable]
    private class KioskLinkData
    {
        public string Token;
        public long ExpiresIn;
        public string Hash;
        public string LoginUrl;
    }

    [Serializable]
    private class DeviceAuthRequest
    {
        public string kioskId;
        public string password;
    }

    class appinfo
    {
        public string version;
    }

    private void Start()
    {
        GameManager.Instance.IsTutorial = false;

        if (m_LoginPanel != null)
            m_LoginPanel.alpha = 1f;

        SetTimePanel(false);

        if (requestQrOnStart)
        {
            //StartCoroutine(RequestQrLoginLinkForTest());
            SetQrLoginUrl(RestManager.Instance.QRLoginUrl);
            RestManager.Instance.LoginSuccess = LoginSuccess;
        }    

        GetVersion();

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            //StopPipeClient();
            Application.Quit();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            OnClick_TouchLoginTemp();
        }
    }

    void GetVersion()
    {
        try
        {
            string path = Path.Combine(Application.dataPath, "appinfo.json");
            if (File.Exists(path))
            {
                string data = File.ReadAllText(path);
                txtVer.text = JsonUtility.FromJson<appinfo>(data).version;
            }
            else
                txtVer.text = Application.version; //"0.1.0";
        }
        catch
        {
            txtVer.text = "0.1.0";
        }
    }

    private QrLoginImage ResolveQrLoginImage()
    {
        if (qrLoginImage != null)
            return qrLoginImage;

        QrLoginImage[] qrImages = Resources.FindObjectsOfTypeAll<QrLoginImage>();
        foreach (QrLoginImage qrImage in qrImages)
        {
            if (qrImage != null && qrImage.gameObject.scene.IsValid())
            {
                qrLoginImage = qrImage;
                return qrLoginImage;
            }
        }

        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform target in transforms)
        {
            if (target == null || target.name != "QR" || !target.gameObject.scene.IsValid())
                continue;

            Image image = target.GetComponent<Image>();
            if (image == null)
                continue;

            qrLoginImage = target.GetComponent<QrLoginImage>();
            if (qrLoginImage == null)
                qrLoginImage = target.gameObject.AddComponent<QrLoginImage>();

            return qrLoginImage;
        }

        return null;
    }

/*
    public void RefreshQrLoginLinkForTest()
    {
        StartCoroutine(RequestQrLoginLinkForTest());
    }

    private IEnumerator RequestQrLoginLinkForTest()
    {
        if (isQrLoginLinkRequestRunning)
            yield break;

        isQrLoginLinkRequestRunning = true;
        yield return RequestQrLoginLink();
        isQrLoginLinkRequestRunning = false;
    }

    private IEnumerator RequestQrLoginLink()
    {
        if (string.IsNullOrWhiteSpace(kioskBaseUrl) || string.IsNullOrWhiteSpace(kioskId) || string.IsNullOrWhiteSpace(kioskPassword))
        {
            Debug.LogWarning("[LoginDirector] Kiosk QR login test settings are empty.");
            yield break;
        }

        string token;
        if (TryGetCachedKioskToken(out token))
        {
            KioskApiResponse cachedLinkResponse = null;
            yield return RequestKioskLink(token, response => cachedLinkResponse = response);

            if (TryDisplayQrLoginLink(cachedLinkResponse))
            {
                Debug.Log("[LoginDirector] QR login link loaded with cached kiosk token.");
                yield break;
            }

            ClearCachedKioskToken();
        }

        string authUrl = BuildKioskApiUrl("DeviceAuth");
        DeviceAuthRequest authRequest = new DeviceAuthRequest
        {
            kioskId = kioskId,
            password = kioskPassword
        };

        KioskApiResponse authResponse = null;
        yield return PostKioskJson(authUrl, JsonUtility.ToJson(authRequest), null, response => authResponse = response);

        if (!IsSuccessResponse(authResponse) || authResponse.Data == null || string.IsNullOrWhiteSpace(authResponse.Data.Token))
        {
            Debug.LogWarning("[LoginDirector] DeviceAuth failed. " + GetResponseMessage(authResponse));
            yield break;
        }

        token = authResponse.Data.Token;
        SaveCachedKioskToken(token, authResponse.Data.ExpiresIn);

        KioskApiResponse linkResponse = null;
        yield return RequestKioskLink(token, response => linkResponse = response);

        if (!TryDisplayQrLoginLink(linkResponse))
        {
            Debug.LogWarning("[LoginDirector] GetKioskLink failed. " + GetResponseMessage(linkResponse));
            yield break;
        }
    }

    private IEnumerator RequestKioskLink(string token, Action<KioskApiResponse> onCompleted)
    {
        string linkUrl = BuildKioskApiUrl("GetKioskLink");
        yield return PostKioskJson(linkUrl, "{}", token, onCompleted);
    }

    private bool TryDisplayQrLoginLink(KioskApiResponse linkResponse)
    {
        if (!IsSuccessResponse(linkResponse) || linkResponse.Data == null || string.IsNullOrWhiteSpace(linkResponse.Data.LoginUrl))
            return false;

        Debug.Log("[LoginDirector] QR LoginUrl: " + linkResponse.Data.LoginUrl);
        if (!string.IsNullOrWhiteSpace(linkResponse.Data.Hash))
            Debug.Log("[LoginDirector] QR Hash: " + linkResponse.Data.Hash);

        SetQrLoginUrl(linkResponse.Data.LoginUrl);
        return true;
    }

    private bool TryGetCachedKioskToken(out string token)
    {
        token = PlayerPrefs.GetString(KioskTokenKey, string.Empty);
        string cachedIdentity = PlayerPrefs.GetString(KioskTokenIdentityKey, string.Empty);
        string expiresAtText = PlayerPrefs.GetString(KioskTokenExpiresAtKey, string.Empty);

        long expiresAt;
        bool isValid = !string.IsNullOrWhiteSpace(token)
            && string.Equals(cachedIdentity, GetKioskTokenIdentity(), StringComparison.Ordinal)
            && long.TryParse(expiresAtText, out expiresAt)
            && expiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + TokenExpiryMarginSeconds;

        if (!isValid)
        {
            token = null;
            ClearCachedKioskToken();
        }

        return isValid;
    }

    private void SaveCachedKioskToken(string token, long expiresIn)
    {
        if (string.IsNullOrWhiteSpace(token) || expiresIn <= TokenExpiryMarginSeconds)
            return;

        long expiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresIn;
        PlayerPrefs.SetString(KioskTokenKey, token);
        PlayerPrefs.SetString(KioskTokenExpiresAtKey, expiresAt.ToString());
        PlayerPrefs.SetString(KioskTokenIdentityKey, GetKioskTokenIdentity());
        PlayerPrefs.Save();
    }

    private void ClearCachedKioskToken()
    {
        PlayerPrefs.DeleteKey(KioskTokenKey);
        PlayerPrefs.DeleteKey(KioskTokenExpiresAtKey);
        PlayerPrefs.DeleteKey(KioskTokenIdentityKey);
    }

    private string GetKioskTokenIdentity()
    {
        return kioskBaseUrl.TrimEnd('/') + "|" + kioskId;
    }

    private IEnumerator PostKioskJson(string url, string json, string bearerToken, Action<KioskApiResponse> onCompleted)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            if (!string.IsNullOrWhiteSpace(bearerToken))
                request.SetRequestHeader("Authorization", "Bearer " + bearerToken);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[LoginDirector] Kiosk API request failed. " + url + " / " + request.error);
                onCompleted?.Invoke(null);
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log("[LoginDirector] Kiosk API response: " + responseText);

            try
            {
                onCompleted?.Invoke(JsonUtility.FromJson<KioskApiResponse>(responseText));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LoginDirector] Failed to parse kiosk API response. " + ex.Message);
                onCompleted?.Invoke(null);
            }
        }
    }

    private string BuildKioskApiUrl(string actionName)
    {
        return kioskBaseUrl.TrimEnd('/') + "/Kiosk/Api/" + actionName;
    }

    private static bool IsSuccessResponse(KioskApiResponse response)
    {
        return response != null && string.Equals(response.State, "Success", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetResponseMessage(KioskApiResponse response)
    {
        if (response == null)
            return "No response.";

        return string.IsNullOrWhiteSpace(response.Message) ? response.State : response.Message;
    }*/

    public void LoginSuccess()
    {
        Debug.Log("LoginSuccess()");
        ShowTimePanel();
    }

    public void SetQrLoginUrl(string loginUrl)
    {
        QrLoginImage target = ResolveQrLoginImage();
        if (target == null)
        {
            Debug.LogWarning("[LoginDirector] QR login image is not assigned.");
            return;
        }

        try
        {
            target.SetLoginUrl(loginUrl);
        }
        catch (Exception ex)
        {
            Debug.LogError("[LoginDirector] Failed to generate QR login image. " + ex.Message);
        }
    }

    public void ClearQrLoginUrl()
    {
        QrLoginImage target = ResolveQrLoginImage();
        if (target != null)
            target.Clear();
    }

    private IEnumerator TranstionToIntro()
    {
        Debug.Log("TranstionToIntro()");
        yield return m_LoginPanel.DOFade(0, m_FadeDuration).WaitForCompletion();

        yield return new WaitForSeconds(1f);

        StartCoroutine(LoginControl());
    }

    private IEnumerator LoginControl()
    {
        Debug.Log("LoginControl()");
        //yield return new WaitForSeconds(1.5f);
        /*
        GameManager.Instance.SelectedSceneName = "Login";
        SceneManager.LoadScene("ProSelect");
        */

        // 파크골프는 프로 선택 없이 UID 1001을 기본 프로로 사용한다.
        SelectProData defaultPro = new SelectProData();
        defaultPro.uid = 1001;
        defaultPro.infoData = GolfProDataManager.Instance.GetProInfoData(defaultPro.uid);
        defaultPro.videoData = GolfProDataManager.Instance.GetProVideoDataList(defaultPro.uid);
        defaultPro.imageData = GolfProDataManager.Instance.GetProImageDataList(defaultPro.uid);
        defaultPro.swingData = GolfProDataManager.Instance.GetSwingData(defaultPro.uid);
        defaultPro.aiSwingData = GolfProDataManager.Instance.GetAISwingData(defaultPro.uid);
        defaultPro.landmarkData = GolfProDataManager.Instance.GetLandmarkData(defaultPro.uid);

        GolfProDataManager.Instance.SelectProData = defaultPro;
        yield return null;

        GameManager.Instance.SelectedSceneName = "ModeSelect";
        SceneManager.LoadScene("ModeSelect");
    }

    public void OnClick_TouchLoginTemp()
    {
        LoginSuccess();
    }

    public void OnClick_TimeOK()
    {
        SetTimePanel(false);
        StartCoroutine(TranstionToIntro());
    }

    public void OnClick_TimeBack()
    {
        SetTimePanel(false);
    }

    private void ShowTimePanel()
    {
        SetTimePanel(true);
    }

    private void SetTimePanel(bool active)
    {
        if (timePanel == null)
        {
            return;
        }

        timePanel.SetActive(active);
    }

    public void OnClick_Jump()
    {
        GameManager.Instance.SelectedSceneName = "PracticeMode";
        SceneManager.LoadScene("PracticeMode");
    }

    public void OnClick_Mode(int idx)
    {
        if(idx == 0)
        {
            GameManager.Instance.IsTutorial = false;

            m_LoginPanel.interactable = true;
        }
        else
        {
            GameManager.Instance.IsTutorial = true;
        }
    }

    private void OnDestroy()
    {
        //StopPipeClient();
    }

    public void OnClick_Shutdown()
    {
        //StopPipeClient();

        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = "shutdown";
        psi.Arguments = "-s -t 5";
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;

        try
        {
            Process.Start(psi);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Shutdown Failed " + e.Message);
        }
        finally
        {
            Application.Quit();
        }
    }

    public void OnClick_Quit()
    {
        //StopPipeClient();
        Application.Quit();
    }
}
