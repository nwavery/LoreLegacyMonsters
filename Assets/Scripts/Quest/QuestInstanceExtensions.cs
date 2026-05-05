namespace LoreLegacyMonsters.Quest
{
    public static class QuestInstanceExtensions
    {
        public static void Fail(this QuestInstance q) => q?.Fail();
    }
}
