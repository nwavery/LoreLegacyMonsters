using System;
using System.Collections.Generic;

namespace LoreLegacyMonsters.SaveSystem
{
    /// <summary>Serializable snapshot written to disk (JSON).</summary>
    [Serializable]
    public class SaveInfo
    {
        public int Version = 8;
        public string PlayerName = "Player";
        public int Gold;
        public string CurrentAreaId = "town";
        public float PlayerPositionX = 2f;
        public float PlayerPositionY = -1f;
        public List<string> DiscoveredAreaIds = new List<string>();
        public WeatherTypeDto Weather = new WeatherTypeDto();
        public List<string> PartyMonsterIds = new List<string>();
        public List<MonsterSaveEntry> Party = new List<MonsterSaveEntry>();
        public List<MonsterSaveEntry> Reserve = new List<MonsterSaveEntry>();
        public List<ItemStackDto> Inventory = new List<ItemStackDto>();
        public List<string> CompletedQuestIds = new List<string>();
        public List<string> ActiveQuestIds = new List<string>();
        public List<string> UnlockedAchievementIds = new List<string>();
        public List<QuestSaveEntry> ActiveQuestProgress = new List<QuestSaveEntry>();
        public List<NpcMemorySaveEntry> NpcMemories = new List<NpcMemorySaveEntry>();
        public List<string> StoryFlags = new List<string>();
        public string SaveSchemaTag = "v1.0";
        public bool TelemetryOptIn;
    }

    [Serializable]
    public class ItemStackDto
    {
        public string itemId;
        public int quantity;
    }

    [Serializable]
    public class WeatherTypeDto
    {
        public int Type; // WeatherType as int
    }
}
