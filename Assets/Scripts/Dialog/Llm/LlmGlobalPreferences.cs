using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>PlayerPrefs keys for optional overrides on top of Resources/NpcLlmSettings.</summary>
    public static class LlmGlobalPreferences
    {
        public const string KeyEnabled = "llm.enabled";
        public const string KeyBaseUrl = "llm.baseUrl";
        public const string KeyModel = "llm.model";
        public const string KeyUseBundledRuntime = "llm.useBundledRuntime";
        public const string KeyBundledRuntimePort = "llm.bundledRuntimePort";

        public static bool IsGloballyEnabled(bool defaultWhenUnset = true)
        {
            if (!PlayerPrefs.HasKey(KeyEnabled)) return defaultWhenUnset;
            return PlayerPrefs.GetInt(KeyEnabled, 1) != 0;
        }

        public static void SetGloballyEnabled(bool value) => PlayerPrefs.SetInt(KeyEnabled, value ? 1 : 0);

        public static void SetBaseUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) PlayerPrefs.DeleteKey(KeyBaseUrl);
            else PlayerPrefs.SetString(KeyBaseUrl, url.Trim());
        }

        public static void SetModel(string model)
        {
            if (string.IsNullOrWhiteSpace(model)) PlayerPrefs.DeleteKey(KeyModel);
            else PlayerPrefs.SetString(KeyModel, model.Trim());
        }

        public static bool IsBundledRuntimeEnabled(bool defaultWhenUnset = false)
        {
            if (!PlayerPrefs.HasKey(KeyUseBundledRuntime)) return defaultWhenUnset;
            return PlayerPrefs.GetInt(KeyUseBundledRuntime, defaultWhenUnset ? 1 : 0) != 0;
        }

        public static bool HasBundledRuntimePreference() => PlayerPrefs.HasKey(KeyUseBundledRuntime);

        public static void SetBundledRuntimeEnabled(bool value) =>
            PlayerPrefs.SetInt(KeyUseBundledRuntime, value ? 1 : 0);

        public static int GetBundledRuntimePort(int fallback)
        {
            var port = PlayerPrefs.GetInt(KeyBundledRuntimePort, fallback);
            return Mathf.Clamp(port, 1024, 65535);
        }

        public static void SetBundledRuntimePort(int port)
        {
            PlayerPrefs.SetInt(KeyBundledRuntimePort, Mathf.Clamp(port, 1024, 65535));
        }
    }
}
