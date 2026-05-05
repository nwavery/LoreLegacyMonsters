using LoreLegacyMonsters.Quests;

namespace LoreLegacyMonsters.Quest
{
    public static class QuestObjectiveExtensions
    {
        public static void AddProgress(this QuestObjective o, int delta)
        {
            if (o == null) return;
            o.currentCount += delta;
        }
    }
}
