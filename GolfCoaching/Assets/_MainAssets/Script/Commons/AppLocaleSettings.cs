using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public static class AppLocaleSettings
{
    public const string PlayerPreferenceKey = "GolfCoaching.SelectedLocale";
    public const string MainStringTable = "backdori_lan_table";

    public static readonly string[] SupportedLocaleCodes = { "ko-KR", "en", "th-TH" };

    public static bool IsSupportedLocaleCode(string localeCode)
    {
        if (string.IsNullOrEmpty(localeCode))
            return false;

        for (var i = 0; i < SupportedLocaleCodes.Length; i++)
        {
            if (SupportedLocaleCodes[i] == localeCode)
                return true;
        }

        return false;
    }

    public static bool TrySetLocale(string localeCode)
    {
        if (string.IsNullOrEmpty(localeCode))
            return false;

        var locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);

        if (locale == null)
            return false;

        LocalizationSettings.SelectedLocale = locale;

        return true;
    }

    public static string GetSelectedCode()
    {
        var selected = LocalizationSettings.SelectedLocale;
        
        return selected != null ? selected.Identifier.Code : null;
    }
}

public static class AppLocaleBootstrap
{
    private const string DefaultLocaleCode = "ko-KR";

    public static bool EnsureSelectedLocale()
    {
        try
        {
            if (LocalizationSettings.SelectedLocale != null)
                return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[AppLocaleBootstrap] Localization is not ready. " + ex.Message);
            return false;
        }

        string savedLocaleCode = PlayerPrefs.GetString(AppLocaleSettings.PlayerPreferenceKey, string.Empty);
        if (TryApplyLocale(savedLocaleCode))
            return true;

        if (TryApplyLocale(DefaultLocaleCode))
            return true;

        try
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales != null && locales.Count > 0 && locales[0] != null)
            {
                LocalizationSettings.SelectedLocale = locales[0];
                PlayerPrefs.SetString(AppLocaleSettings.PlayerPreferenceKey, locales[0].Identifier.Code);
                PlayerPrefs.Save();
                return true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[AppLocaleBootstrap] Failed to get available locales. " + ex.Message);
        }

        return false;
    }

    private static bool TryApplyLocale(string localeCode)
    {
        if (string.IsNullOrEmpty(localeCode) || !AppLocaleSettings.IsSupportedLocaleCode(localeCode))
            return false;

        try
        {
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(localeCode);
            if (locale == null)
                return false;

            LocalizationSettings.SelectedLocale = locale;
            PlayerPrefs.SetString(AppLocaleSettings.PlayerPreferenceKey, locale.Identifier.Code);
            PlayerPrefs.Save();
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AppLocaleBootstrap] Failed to apply locale '{localeCode}'. {ex.Message}");
            return false;
        }
    }
}

