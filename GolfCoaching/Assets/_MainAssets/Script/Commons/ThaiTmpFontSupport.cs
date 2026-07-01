using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.TextCore.LowLevel;

public static class ThaiTmpFontSupport
{
    const string ThaiFontResourcePath = "Fonts/ThaiNotoSans";

    static bool s_Registered;
    static TMP_FontAsset s_ThaiFontAsset;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RegisterOnFirstSceneLoaded()
    {
        EnsureThaiFallbackRegistered();
    }

    public static void EnsureThaiFallbackRegistered()
    {
        if (s_Registered)
            return;

        Font sourceFont = Resources.Load<Font>(ThaiFontResourcePath);

        if (sourceFont == null)
        {
            Debug.LogWarning($"[ThaiTmpFontSupport] Missing Resources font at '{ThaiFontResourcePath}'. Thai text may show as squares.");
            return;
        }

        s_ThaiFontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize: 72,
            atlasPadding: 5,
            GlyphRenderMode.SDFAA,
            atlasWidth: 2048,
            atlasHeight: 2048,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        if (s_ThaiFontAsset == null)
        {
            Debug.LogWarning("[ThaiTmpFontSupport] TMP_FontAsset.CreateFontAsset failed for Thai font.");
            return;
        }

        s_ThaiFontAsset.name = "NotoSansThai_Dynamic_TMP";

        List<TMP_FontAsset> globalFallbacks = TMP_Settings.fallbackFontAssets;
        if (globalFallbacks == null)
        {
            globalFallbacks = new List<TMP_FontAsset>();
            TMP_Settings.fallbackFontAssets = globalFallbacks;
        }

        if (!globalFallbacks.Contains(s_ThaiFontAsset))
            globalFallbacks.Add(s_ThaiFontAsset);

        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        s_Registered = true;
    }

    static void OnSelectedLocaleChanged(Locale _)
    {
        TMP_Text[] texts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text t = texts[i];
            if (t == null)
                continue;
            t.ForceMeshUpdate(true);
        }
    }
}
