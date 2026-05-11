using UnityEngine;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.Monster
{
    public static class CaptureRules
    {
        public static float CalculateChance(MonsterData species, int currentHp, int maxHp, MonsterStatusEffect status,
            float charmModifier = 1f, bool isBoss = false, float gearChanceBonus = 0f)
        {
            if (species == null || isBoss) return 0f;

            var hpRatio = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 1f;
            var hpBonus = (1f - hpRatio) * 0.45f;
            var statusBonus = status switch
            {
                MonsterStatusEffect.Burn => 0.08f,
                MonsterStatusEffect.Poison => 0.1f,
                MonsterStatusEffect.Shock => 0.12f,
                MonsterStatusEffect.GuardBreak => 0.05f,
                _ => 0f
            };

            var rarityPenalty = Mathf.Clamp01((species.Rarity - 1) * 0.06f);
            var baseChance = species.CatchRate + hpBonus + statusBonus - rarityPenalty;
            return Mathf.Clamp(baseChance * Mathf.Max(0.1f, charmModifier) + gearChanceBonus, 0.05f, 0.95f);
        }

        public static CaptureResult Roll(MonsterData species, int currentHp, int maxHp, MonsterStatusEffect status,
            float charmModifier = 1f, bool isBoss = false, IRandomSource rng = null, float gearChanceBonus = 0f)
        {
            rng ??= UnityRandomSource.Default;
            if (isBoss)
            {
                return new CaptureResult
                {
                    Chance = 0f,
                    Roll = 1f,
                    Success = false,
                    Reason = "Boss monsters cannot be captured."
                };
            }

            var chance = CalculateChance(species, currentHp, maxHp, status, charmModifier, isBoss, gearChanceBonus);
            var roll = rng.Next01();
            return new CaptureResult
            {
                Chance = chance,
                Roll = roll,
                Success = roll <= chance,
                Reason = roll <= chance ? "Captured." : "Broke free."
            };
        }
    }
}
