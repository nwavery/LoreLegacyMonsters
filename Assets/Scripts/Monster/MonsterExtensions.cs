namespace LoreLegacyMonsters.Monster
{
    public static class MonsterExtensions
    {
        public static bool IsAlive(this MonsterInstance m) => m != null && m.currentHp > 0;
    }
}
