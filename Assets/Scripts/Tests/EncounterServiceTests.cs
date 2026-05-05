using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.World;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class EncounterServiceTests
    {
        [Test]
        public void EncounterService_UsesAreaWildMonsterIds_WhenProvided()
        {
            var root = new GameObject("encounters");
            var registry = root.AddComponent<AssetRegistryManager>();
            var world = root.AddComponent<WorldManager>();
            var service = root.AddComponent<EncounterService>();

            var monster = ScriptableObject.CreateInstance<MonsterData>();
            monster.Configure("monster_area_test", "Area Test", 20, 5, 3);
            registry.RegisterMonster(monster);

            var area = ScriptableObject.CreateInstance<WorldArea>();
            area.Configure("area_test", "Area Test");
            area.SetEncounterChance(1f);
            area.SetWildEncounters("monster_area_test");
            world.RegisterArea(area);
            world.SetCurrentArea("area_test");

            Assert.IsTrue(service.TryRollWildEncounter(registry, world, null, out var encounter));
            Assert.AreEqual("monster_area_test", encounter.MonsterId);

            Object.DestroyImmediate(monster);
            Object.DestroyImmediate(area);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void EncounterService_SkipsUnknownWildIds_AndUsesRegisteredFallback()
        {
            var root = new GameObject("encounters-fallback");
            var registry = root.AddComponent<AssetRegistryManager>();
            var world = root.AddComponent<WorldManager>();
            var service = root.AddComponent<EncounterService>();

            var monster = ScriptableObject.CreateInstance<MonsterData>();
            monster.Configure("monster_known", "Known", 20, 5, 3);
            registry.RegisterMonster(monster);

            var area = ScriptableObject.CreateInstance<WorldArea>();
            area.Configure("area_fallback", "Fallback");
            area.SetEncounterChance(1f);
            area.SetWildEncounters("monster_missing", "monster_known");
            world.RegisterArea(area);
            world.SetCurrentArea("area_fallback");

            Assert.IsTrue(service.TryRollWildEncounter(registry, world, null, out var encounter, new SeededRandomSource(123)));
            Assert.AreEqual("monster_known", encounter.MonsterId);

            Object.DestroyImmediate(monster);
            Object.DestroyImmediate(area);
            Object.DestroyImmediate(root);
        }
    }
}
