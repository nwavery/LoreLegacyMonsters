using UnityEngine;

#if STEAMWORKS_NET
using Steamworks;
#endif

namespace LoreLegacyMonsters.Platform.Steam
{
    public static class SteamAchievementBackend
    {
        public static void Initialize()
        {
#if STEAMWORKS_NET
            // Steamworks.NET API surface differs by package revision;
            // defer stat prefetch and rely on unlock/store calls for compatibility.
#endif
        }

        public static void Unlock(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
                return;
#if STEAMWORKS_NET
            if (!SteamManagerReady())
                return;
            SteamUserStats.SetAchievement(achievementId);
            SteamUserStats.StoreStats();
#endif
        }

        public static void SetRichPresence(string key, string value)
        {
#if STEAMWORKS_NET
            if (!SteamManagerReady())
                return;
            SteamFriends.SetRichPresence(key, value ?? string.Empty);
#endif
        }

        static bool SteamManagerReady()
        {
            var bootstrap = Object.FindFirstObjectByType<SteamBootstrap>();
            return bootstrap != null && bootstrap.Initialized;
        }
    }
}
