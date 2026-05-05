using UnityEngine;
using LoreLegacyMonsters.Quests;

namespace LoreLegacyMonsters.Questing
{
    public class QuestData : ScriptableObject
    {
        [SerializeField] string questId;
        [SerializeField] string displayName;
        [SerializeField] [TextArea] string description;
        [SerializeField] QuestObjective[] objectives;

        public string QuestId => questId;
        public string DisplayName => displayName;
        public string Description => description;
        public QuestObjective[] Objectives => objectives;

        public void Configure(string id, string title, string desc, QuestObjective[] objs)
        {
            questId = id;
            displayName = title;
            description = desc;
            objectives = objs;
        }
    }
}
