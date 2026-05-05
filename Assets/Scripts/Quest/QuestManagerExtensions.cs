using LoreLegacyMonsters;

namespace LoreLegacyMonsters.Quest
{
    public static class QuestManagerExtensions
    {
        public static void Abandon(this QuestManager mgr, string id) => mgr?.CancelQuest(id);
    }
}
