using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.World;
using UnityEngine;

namespace LoreLegacyMonsters.Platform.Steam
{
    [DefaultExecutionOrder(-500)]
    public sealed class SteamRichPresenceBridge : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBridge()
        {
            if (FindFirstObjectByType<SteamRichPresenceBridge>() != null)
                return;
            var go = new GameObject("SteamRichPresenceBridge");
            go.AddComponent<SteamRichPresenceBridge>();
            DontDestroyOnLoad(go);
        }

        void OnEnable()
        {
            GameEvents.AreaChanged += OnAreaChanged;
            GameEvents.QuestCompleted += OnQuestCompleted;
            RefreshPresence();
        }

        void OnDisable()
        {
            GameEvents.AreaChanged -= OnAreaChanged;
            GameEvents.QuestCompleted -= OnQuestCompleted;
        }

        void OnAreaChanged(string _) => RefreshPresence();
        void OnQuestCompleted(string _) => RefreshPresence();

        void RefreshPresence()
        {
            var world = FindFirstObjectByType<WorldManager>();
            var quests = FindFirstObjectByType<QuestManager>();
            var area = world != null ? world.GetCurrentArea() : null;
            var areaName = area != null ? area.DisplayName : "Unknown Area";
            var questTitle = quests != null ? quests.GetPrimaryQuestTitle() : "No active quest";
            var status = $"Exploring {areaName}";
            var details = string.IsNullOrWhiteSpace(questTitle) ? "No active quest" : questTitle;
            SteamAchievementBackend.SetRichPresence("status", status);
            SteamAchievementBackend.SetRichPresence("steam_display", "#StatusWithQuest");
            SteamAchievementBackend.SetRichPresence("quest", details);
        }
    }
}
