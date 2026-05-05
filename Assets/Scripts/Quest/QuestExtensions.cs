namespace LoreLegacyMonsters.Quest
{
    public static class QuestExtensions
    {
        public static bool IsTerminal(this QuestStatus s) =>
            s == QuestStatus.Completed || s == QuestStatus.Failed;
    }
}
