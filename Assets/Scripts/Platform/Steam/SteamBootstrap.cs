using UnityEngine;
using System;
using System.IO;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace LoreLegacyMonsters.Platform.Steam
{
    [DefaultExecutionOrder(-9999)]
    public sealed class SteamBootstrap : MonoBehaviour
    {
        static SteamBootstrap instance;

        public bool Initialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void BootstrapRuntime()
        {
            if (instance != null)
                return;
            var go = new GameObject("SteamBootstrap");
            instance = go.AddComponent<SteamBootstrap>();
            DontDestroyOnLoad(go);
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSteam();
        }

        void OnApplicationQuit()
        {
#if STEAMWORKS_NET
            if (Initialized)
                SteamAPI.Shutdown();
#endif
            Initialized = false;
        }

        public bool IsOverlayEnabled()
        {
#if STEAMWORKS_NET
            return Initialized && SteamUtils.IsOverlayEnabled();
#else
            return false;
#endif
        }

        void InitializeSteam()
        {
#if STEAMWORKS_NET
            var skipRestartForLocalQa = HasLocalAppIdFile();
            if (!skipRestartForLocalQa && SteamAPI.RestartAppIfNecessary((AppId_t)SteamConfig.AppId))
            {
                Application.Quit();
                return;
            }

            Initialized = SteamAPI.Init();
            if (!Initialized)
                Debug.LogWarning("SteamBootstrap: SteamAPI.Init failed; continuing without Steam features.");
            else
                SteamAchievementBackend.Initialize();
#else
            Debug.Log("SteamBootstrap: STEAMWORKS_NET not defined. Steam integration is disabled.");
            Initialized = false;
#endif
        }

        static bool HasLocalAppIdFile()
        {
            try
            {
                var executableDir = AppDomain.CurrentDomain.BaseDirectory;
                var path = Path.Combine(executableDir, "steam_appid.txt");
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }
    }
}
