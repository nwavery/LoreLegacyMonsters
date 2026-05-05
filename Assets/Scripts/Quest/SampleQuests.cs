using UnityEngine;

namespace LoreLegacyMonsters.Quest
{
    public static class SampleQuests
    {
        public static void RegisterDefaults(QuestManager mgr)
        {
            if (mgr == null) return;
            mgr.StartQuest(Quests.MainStoryQuests.Intro);
        }
    }
}
