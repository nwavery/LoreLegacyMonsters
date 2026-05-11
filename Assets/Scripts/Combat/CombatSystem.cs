using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Combat
{
    public class CombatSystem
    {
        readonly IRandomSource _rng;

        public CombatSystem(IRandomSource rng = null) =>
            _rng = rng ?? UnityRandomSource.Default;

        public int CalculateDamage(int power, int defense) =>
            Mathf.Max(1, power - defense / 2);

        public bool RollHit(float accuracy) => _rng.Next01() <= Mathf.Clamp01(accuracy);

        public bool RollCrit(float chance) => _rng.Next01() <= Mathf.Clamp01(chance);

        public float GetTypeMultiplier(MonsterElement moveElement, MonsterElement defenderPrimary,
            MonsterElement defenderSecondary = MonsterElement.None) =>
            TypeChart.GetMultiplier(moveElement, defenderPrimary, defenderSecondary);

        public int CalculateMoveDamage(int attack, int defense, MoveData move, MonsterElement attackerPrimary,
            MonsterElement defenderPrimary, MonsterElement defenderSecondary = MonsterElement.None)
        {
            return CalculateMoveDamage(attack, defense, move, attackerPrimary, defenderPrimary, defenderSecondary,
                out _, out _, 1f);
        }

        public int CalculateMoveDamage(int attack, int defense, MoveData move, MonsterElement attackerPrimary,
            MonsterElement defenderPrimary, MonsterElement defenderSecondary, out bool wasCrit, out float typeMultiplier,
            float outgoingElementMult = 1f)
        {
            if (move == null)
            {
                wasCrit = false;
                typeMultiplier = 1f;
                return CalculateDamage(attack, defense);
            }
            var raw = CalculateDamage(Mathf.Max(1, attack + move.Power), defense);
            var stab = move.Element != MonsterElement.None && move.Element == attackerPrimary ? 1.25f : 1f;
            typeMultiplier = GetTypeMultiplier(move.Element, defenderPrimary, defenderSecondary) *
                             Mathf.Max(0.05f, outgoingElementMult);
            wasCrit = RollCrit(move.CritChance);
            var crit = wasCrit ? 1.4f : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(raw * stab * typeMultiplier * crit));
        }

        public int GetStatusTickDamage(int maxHp, MonsterStatusEffect status)
        {
            return status switch
            {
                MonsterStatusEffect.Burn => Mathf.Max(1, maxHp / 10),
                MonsterStatusEffect.Poison => Mathf.Max(1, maxHp / 8),
                _ => 0
            };
        }
    }
}
