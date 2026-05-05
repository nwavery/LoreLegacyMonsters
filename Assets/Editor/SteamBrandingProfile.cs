#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    /// <summary>
    /// Applies baseline shipping identity values used for Steam release builds.
    /// Icon assignment is handled separately in the Unity Player Settings UI.
    /// </summary>
    public static class SteamBrandingProfile
    {
        public const string ShippingCompanyName = "LoreLegacyStudios";
        public const string ShippingProductName = "LoreLegacyMonsters";
        public const string ShippingVersion = "1.0.0";
        public const string SteamworksDefine = "STEAMWORKS_NET";

        [MenuItem("Build/Steam/Apply Branding Profile")]
        public static void ApplyBrandingProfile()
        {
            PlayerSettings.companyName = ShippingCompanyName;
            PlayerSettings.productName = ShippingProductName;
            PlayerSettings.bundleVersion = ShippingVersion;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.08f, 0.11f, 0.14f, 1f);
            EnsureStandaloneSteamworksDefine();
            Debug.Log("SteamBrandingProfile: Applied shipping identity profile.");
        }

        public static void EnsureStandaloneSteamworksDefine()
        {
            var current = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone);
            var tokens = current.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
            if (!tokens.Contains(SteamworksDefine, StringComparer.Ordinal))
            {
                tokens.Add(SteamworksDefine);
                PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, string.Join(";", tokens));
                Debug.Log("SteamBrandingProfile: Added STEAMWORKS_NET define for Standalone.");
            }
        }
    }
}
#endif
