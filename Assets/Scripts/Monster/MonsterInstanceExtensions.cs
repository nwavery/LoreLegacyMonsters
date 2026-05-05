namespace LoreLegacyMonsters.Monster
{
    public static class MonsterInstanceExtensions
    {
        public static void HealFull(this MonsterInstance m)
        {
            if (m == null) return;
            m.currentHp = m.maxHp;
        }
    }
}
