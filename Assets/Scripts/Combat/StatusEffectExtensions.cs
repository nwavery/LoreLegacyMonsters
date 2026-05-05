using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Combat
{
    public static class StatusEffectExtensions
    {
        public static int DamagePerTick(this StatusEffectType t) =>
            t == StatusEffectType.Burn ? 2 : t == StatusEffectType.Poison ? 3 : 0;
    }
}
