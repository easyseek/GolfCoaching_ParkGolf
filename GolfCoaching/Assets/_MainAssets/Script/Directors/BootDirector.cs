using UnityEngine;
using System;
using System.Threading;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class BootDirector : MonoBehaviour
{
#if UNITY_STANDALONE_WIN
    private static Mutex mutex;
#endif

    private int width = 1080;
    private int height = 1920;

    private void Awake()
    {
#if UNITY_STANDALONE_WIN
        bool createdNew;
        mutex = new Mutex(true, "GolfCoaching24", out createdNew);

        if(!createdNew)
        {
            Application.Quit();
            return;
        }

        DontDestroyOnLoad(gameObject);
#endif
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => Utillity.Instance.isInit == true);

        yield return LocalizationSettings.InitializationOperation;
        AppLocaleBootstrap.EnsureSelectedLocale();
        yield return PreloadLocalizationTables();

        //키오스크 로그인
        /*
        float timeout = 15f;
        while(RestManager.Instance.KioskLogin == false)
        {
            if(timeout > 0)
            {
                timeout -= 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                Utillity.Instance.ShowToast("Kiosk Login Failed...");
                yield break;
            }
        }
        */

        yield return new WaitForSeconds(3f);

        Init();
    }

    private IEnumerator PreloadLocalizationTables()
    {
        System.Collections.Generic.IList<Locale> locales = null;
        try
        {
            locales = LocalizationSettings.AvailableLocales.Locales;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[BootDirector] Failed to get localization locales. " + ex.Message);
            yield break;
        }

        if (locales == null || locales.Count == 0)
            yield break;

        for (var i = 0; i < locales.Count; i++)
        {
            Locale locale = locales[i];
            if (locale == null || !AppLocaleSettings.IsSupportedLocaleCode(locale.Identifier.Code))
                continue;

            var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(AppLocaleSettings.MainStringTable, locale);
            yield return tableOp;
        }
    }

    private void Init()
    {
        if(Utillity.Instance.CheckInternet())
        {
            Debug.Log($"인터넷 접속 O");
            //Debug.Log($"{DateTime.UtcNow.ToString("O")}");
        }
        else
        {
            Debug.Log($"인터넷 접속 X");
        }

#if !UNITY_EDITOR
        Application.targetFrameRate = -1;//Utillity.Instance.frameLimit;
        //QualitySettings.vSyncCount = 0;
#else
        Application.targetFrameRate = Utillity.Instance.frameLimit;
        QualitySettings.vSyncCount = 0;
#endif
        Utillity.Instance.SetResolution(width, height);

        //프로 데이터 로드 임시 스킵
        //GolfProDataManager.Instance.LoadProData();

        GameManager.Instance.SelectedSceneName = "Login";
        UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
    }

    private void OnApplicationQuit()
    {
#if UNITY_STANDALONE_WIN
        try
        {
            mutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            
        }
        finally
        {
            mutex = null;
        }
#endif
    }
}
