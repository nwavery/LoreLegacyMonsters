using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Combat;
using LoreLegacyMonsters.Monster;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class CombatSystemTests
    {
        [Test]
        public void CalculateDamage_IsAtLeastOne()
        {
            var sys = CombatTestHelpers.CreateSystem();
            Assert.GreaterOrEqual(sys.CalculateDamage(5, 10), 1);
        }

        [Test]
        public void TypeMultiplier_FireBeatsNature()
        {
            var sys = CombatTestHelpers.CreateSystem();
            Assert.Greater(sys.GetTypeMultiplier(MonsterElement.Fire, MonsterElement.Nature), 1f);
            Assert.Less(sys.GetTypeMultiplier(MonsterElement.Fire, MonsterElement.Water), 1f);
        }

        [Test]
        public void StatusTick_BurnDealsDamage()
        {
            var sys = CombatTestHelpers.CreateSystem();
            Assert.Greater(sys.GetStatusTickDamage(40, MonsterStatusEffect.Burn), 0);
            Assert.AreEqual(0, sys.GetStatusTickDamage(40, MonsterStatusEffect.None));
        }

        [Test]
        public void CalculateMoveDamage_ReportsCritAndTypeMultiplier()
        {
            var sys = CombatTestHelpers.CreateSystem();
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.Configure("move_test", "Test Strike", MonsterElement.Fire, MoveEffectType.Attack, 8, 1f);

            var damage = sys.CalculateMoveDamage(10, 4, move, MonsterElement.Fire, MonsterElement.Nature,
                MonsterElement.None, out var wasCrit, out var typeMultiplier);

            Assert.Greater(damage, 0);
            Assert.IsTrue(wasCrit);
            Assert.Greater(typeMultiplier, 1f);
            Object.DestroyImmediate(move);
        }
    }
}
