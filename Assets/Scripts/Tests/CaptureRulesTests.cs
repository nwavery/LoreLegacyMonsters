using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Tests
{
    public class CaptureRulesTests
    {
        [Test]
        public void CaptureChance_GetsBetterAtLowHp()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.Configure("monster_test", "Testmon", 20, 5, 2);
            data.ConfigureIdentity(MonsterRole.Support, MonsterElement.Water, GrowthBias.HpHeavy,
                "move_strike", "move_wave_pulse", 0.4f, 4, 1);

            var healthy = CaptureRules.CalculateChance(data, 20, 20, MonsterStatusEffect.None, 1f, false);
            var weakened = CaptureRules.CalculateChance(data, 4, 20, MonsterStatusEffect.None, 1f, false);

            Assert.Greater(weakened, healthy);

            Object.DestroyImmediate(data);
        }

        [Test]
        public void CaptureChance_BossIsAlwaysZero()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.Configure("monster_test", "Testmon", 20, 5, 2);

            Assert.AreEqual(0f, CaptureRules.CalculateChance(data, 1, 20, MonsterStatusEffect.Poison, 1.2f, true));

            Object.DestroyImmediate(data);
        }
    }
}
