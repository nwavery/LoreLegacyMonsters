using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters
{
    public static class StatusEffectTypeExtensions
    {
        public static bool IsHarmful(this StatusEffectType t) =>
            t is StatusEffectType.Burn or StatusEffectType.Poison;
    }
}
