using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Monster;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class MonsterProgressionTests
    {
        [Test]
        public void MonsterSystem_AddMonster_StoresActiveParty()
        {
            var root = new GameObject("monster-root");
            var registry = root.AddComponent<AssetRegistryManager>();
            var system = root.AddComponent<MonsterSystem>();
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.Configure("monster_test", "Testmon", 20, 5, 2);
            registry.RegisterMonster(data);

            var added = system.AddMonster(data, 2);

            Assert.IsNotNull(added);
            Assert.AreEqual(1, system.Party.Count);
            Assert.AreEqual("monster_test", system.GetPartySaveIds()[0]);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MonsterSystem_GrantExperienceToActive_LevelsUp()
        {
            var root = new GameObject("monster-root");
            var registry = root.AddComponent<AssetRegistryManager>();
            var system = root.AddComponent<MonsterSystem>();
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.Configure("monster_test", "Testmon", 20, 5, 2);
            data.ConfigureIdentity(MonsterRole.Striker, MonsterElement.Fire, GrowthBias.AttackHeavy,
                "move_strike", "move_flame_bite", 0.45f, 6, 1, MonsterElement.None,
                new MonsterEvolutionRule { method = EvolutionMethod.Level, targetMonsterId = "monster_test_evo", requiredLevel = 3 },
                new MonsterMoveLearnEntry { moveId = "move_strike", unlockLevel = 1 },
                new MonsterMoveLearnEntry { moveId = "move_flame_bite", unlockLevel = 2 });
            var evo = ScriptableObject.CreateInstance<MonsterData>();
            evo.Configure("monster_test_evo", "Testmon Evo", 30, 8, 4);
            evo.ConfigureIdentity(MonsterRole.Striker, MonsterElement.Fire, GrowthBias.AttackHeavy,
                "move_flame_bite", "move_cinder_guard", 0.3f);
            registry.RegisterMonster(data);
            registry.RegisterMonster(evo);
            system.AddMonster(data, 1);

            Assert.IsTrue(system.GrantExperienceToActive(registry, 100));
            Assert.Greater(system.GetActiveMonster().level, 1);
            CollectionAssert.Contains(system.GetActiveMonster().learnedMoveIds, "move_flame_bite");
            Assert.IsTrue(system.TryEvolve(0, registry));
            Assert.AreEqual("monster_test_evo", system.GetActiveMonster().monsterDataId);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(evo);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void MonsterSystem_OverflowMonsters_GoToReserve()
        {
            var root = new GameObject("monster-root");
            var registry = root.AddComponent<AssetRegistryManager>();
            var system = root.AddComponent<MonsterSystem>();
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.Configure("monster_test", "Testmon", 20, 5, 2);
            registry.RegisterMonster(data);

            for (var i = 0; i < 5; i++)
                system.AddMonster(data, 1, $"Buddy{i}");

            Assert.AreEqual(4, system.Party.Count);
            Assert.AreEqual(1, system.Reserve.Count);

            Object.DestroyImmediate(data);
            Object.DestroyImmediate(root);
        }
    }
}
