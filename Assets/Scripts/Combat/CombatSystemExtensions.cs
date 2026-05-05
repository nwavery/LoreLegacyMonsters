using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Combat
{
    public static class CombatSystemExtensions
    {
        public static float WeatherDamageMultiplier(this CombatSystem _, World.WeatherType w) =>
            w == World.WeatherType.Stormy ? 1.05f : 1f;
    }
}
