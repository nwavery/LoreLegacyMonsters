using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace LoreLegacyMonsters.Platform.Steam
{
    [DefaultExecutionOrder(-9998)]
    public sealed class SteamCallbackPump : MonoBehaviour
    {
        SteamBootstrap bootstrap;

        void Awake()
        {
            bootstrap = FindFirstObjectByType<SteamBootstrap>();
            if (bootstrap == null)
            {
                var go = new GameObject("SteamBootstrap");
                bootstrap = go.AddComponent<SteamBootstrap>();
                DontDestroyOnLoad(go);
            }
        }

        void Update()
        {
#if STEAMWORKS_NET
            if (bootstrap != null && bootstrap.Initialized)
                SteamAPI.RunCallbacks();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnsurePump()
        {
            if (FindFirstObjectByType<SteamCallbackPump>() != null)
                return;
            var go = new GameObject("SteamCallbackPump");
            go.AddComponent<SteamCallbackPump>();
            DontDestroyOnLoad(go);
        }
    }
}
