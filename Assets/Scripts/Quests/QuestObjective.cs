using System;

namespace LoreLegacyMonsters.Quests
{
    [Serializable]
    public class QuestObjective
    {
        public string objectiveId;
        public string description;
        public int requiredCount;
        public int currentCount;

        public bool IsMet => currentCount >= requiredCount;
    }
}
