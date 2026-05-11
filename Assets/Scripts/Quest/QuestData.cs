using UnityEngine;
using LoreLegacyMonsters.Quests;
using System.Collections.Generic;

namespace LoreLegacyMonsters.Questing
{
    public class QuestData : ScriptableObject
    {
        [SerializeField] string questId;
        [SerializeField] string displayName;
        [SerializeField] [TextArea] string description;
        [SerializeField] QuestObjective[] objectives;
        [SerializeField] List<string> gearRewardItemIds = new List<string>();

        public string QuestId => questId;
        public string DisplayName => displayName;
        public string Description => description;
        public QuestObjective[] Objectives => objectives;
        public IReadOnlyList<string> GearRewardItemIds => gearRewardItemIds;

        public void Configure(string id, string title, string desc, QuestObjective[] objs)
        {
            questId = id;
            displayName = title;
            description = desc;
            objectives = objs;
        }

        /// <summary>Assigns scripted gear rewarded on quest completion.</summary>
        public void SetGearRewards(params string[] itemIds)
        {
            gearRewardItemIds ??= new List<string>();
            gearRewardItemIds.Clear();
            if (itemIds == null) return;
            foreach (var id in itemIds)
                if (!string.IsNullOrWhiteSpace(id))
                    gearRewardItemIds.Add(id.Trim());
        }
    }
}
