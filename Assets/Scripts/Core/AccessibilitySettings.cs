using UnityEngine;

namespace LoreLegacyMonsters.Core
{
    public static class AccessibilitySettings
    {
        const string KeyTextScale = "access.textScale";
        const string KeyColorBlindSafe = "access.colorBlindSafe";
        const string KeyReduceFlash = "access.reduceFlash";
        const string KeyReduceShake = "access.reduceShake";

        public static float TextScale => Mathf.Clamp(GetFloatSafe(KeyTextScale, 1f), 0.9f, 1.6f);
        public static bool ColorBlindSafe => GetIntSafe(KeyColorBlindSafe, 0) != 0;
        public static bool ReduceFlash => GetIntSafe(KeyReduceFlash, 0) != 0;
        public static bool ReduceShake => GetIntSafe(KeyReduceShake, 0) != 0;

        public static void SetTextScale(float value) => PlayerPrefs.SetFloat(KeyTextScale, Mathf.Clamp(value, 0.9f, 1.6f));
        public static void SetColorBlindSafe(bool value) => PlayerPrefs.SetInt(KeyColorBlindSafe, value ? 1 : 0);
        public static void SetReduceFlash(bool value) => PlayerPrefs.SetInt(KeyReduceFlash, value ? 1 : 0);
        public static void SetReduceShake(bool value) => PlayerPrefs.SetInt(KeyReduceShake, value ? 1 : 0);
        public static void Save() => PlayerPrefs.Save();

        static int GetIntSafe(string key, int fallback)
        {
            try { return PlayerPrefs.GetInt(key, fallback); }
            catch { return fallback; }
        }

        static float GetFloatSafe(string key, float fallback)
        {
            try { return PlayerPrefs.GetFloat(key, fallback); }
            catch { return fallback; }
        }
    }
}
