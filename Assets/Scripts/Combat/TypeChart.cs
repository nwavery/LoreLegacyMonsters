using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Combat
{
    public static class TypeChart
    {
        public static float GetMultiplier(MonsterElement attack, MonsterElement defenderPrimary, MonsterElement defenderSecondary = MonsterElement.None)
        {
            var mult = GetSingle(attack, defenderPrimary);
            if (defenderSecondary != MonsterElement.None)
                mult *= GetSingle(attack, defenderSecondary);
            return mult;
        }

        static float GetSingle(MonsterElement attack, MonsterElement defend)
        {
            if (attack == MonsterElement.None || attack == MonsterElement.Neutral || defend == MonsterElement.None)
                return 1f;

            if (attack == MonsterElement.Fire && defend == MonsterElement.Nature) return 1.5f;
            if (attack == MonsterElement.Water && defend == MonsterElement.Fire) return 1.5f;
            if (attack == MonsterElement.Nature && defend == MonsterElement.Water) return 1.5f;
            if (attack == MonsterElement.Lightning && defend == MonsterElement.Water) return 1.5f;
            if (attack == MonsterElement.Stone && defend == MonsterElement.Lightning) return 1.5f;
            if (attack == MonsterElement.Shadow && defend == MonsterElement.Neutral) return 1.25f;

            if (attack == MonsterElement.Fire && defend == MonsterElement.Water) return 0.75f;
            if (attack == MonsterElement.Water && defend == MonsterElement.Nature) return 0.75f;
            if (attack == MonsterElement.Nature && defend == MonsterElement.Fire) return 0.75f;
            if (attack == MonsterElement.Lightning && defend == MonsterElement.Stone) return 0.75f;

            return 1f;
        }
    }
}
