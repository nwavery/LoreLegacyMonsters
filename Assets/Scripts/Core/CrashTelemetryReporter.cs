using System;
using System.IO;
using UnityEngine;

namespace LoreLegacyMonsters.Core
{
    public sealed class CrashTelemetryReporter : MonoBehaviour
    {
        const string KeyTelemetryOptIn = "telemetry.optIn";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureInstance()
        {
            if (FindFirstObjectByType<CrashTelemetryReporter>() != null)
                return;
            var go = new GameObject("CrashTelemetryReporter");
            go.AddComponent<CrashTelemetryReporter>();
            DontDestroyOnLoad(go);
        }

        public static bool IsOptedIn => PlayerPrefs.GetInt(KeyTelemetryOptIn, 0) != 0;

        public static void SetOptIn(bool value)
        {
            PlayerPrefs.SetInt(KeyTelemetryOptIn, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        void OnEnable()
        {
            Application.logMessageReceived += OnLogMessage;
        }

        void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessage;
        }

        void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!IsOptedIn)
                return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            try
            {
                var dir = Path.Combine(Application.persistentDataPath, "Telemetry");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "crash-events.log");
                var line = $"[{DateTime.UtcNow:O}] {type}: {condition}\n{stackTrace}\n";
                File.AppendAllText(path, line);
            }
            catch
            {
                // Ignore telemetry write failures.
            }
        }
    }
}
