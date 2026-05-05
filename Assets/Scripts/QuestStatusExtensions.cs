using LoreLegacyMonsters.Quest;

namespace LoreLegacyMonsters
{
    public static class QuestStatusExtensions
    {
        public static string Code(this QuestStatus s) => s.ToString().ToLowerInvariant();
    }
}
