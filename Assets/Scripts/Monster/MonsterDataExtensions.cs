namespace LoreLegacyMonsters.Monster
{
    public static class MonsterDataExtensions
    {
        public static MonsterInstance CreateInstance(this MonsterData data, int level = 1) =>
            data == null ? null : new MonsterInstance(data.MonsterId, data) { level = level };
    }
}
