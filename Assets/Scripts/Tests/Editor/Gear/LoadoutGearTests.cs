using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog.Llm;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.Quests;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.World;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests.Editor
{
    public class LoadoutModifiersTests
    {
        [Test]
        public void MoveSpeed_MultipliesAcrossMultipleCharms()
        {
            GearItemData M(string id, float speedMult)
            {
                var g = ScriptableObject.CreateInstance<GearItemData>();
                g.ConfigureGear(id, id, GearSlot.Charm, Rarity.Common,
                    new[] { new GearEffect(GearEffectKind.MoveSpeedMult, speedMult) });
                return g;
            }

            var a = M("gear_tmp_ms_a", 1.1f);
            var b = M("gear_tmp_ms_b", 1.2f);
            try
            {
                var m = LoadoutModifiers.FromGearItems(new[] { a, b });
                Assert.That(m.MoveSpeedMult, Is.EqualTo(1.32f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
            }
        }
    }

    public class GearPromptFormatterTests
    {
        [Test]
        public void EquippedSummary_IncludesRarityLabels()
        {
            var ring = ScriptableObject.CreateInstance<GearItemData>();
            ring.ConfigureGear("gear_ut_ring", "UT Ring", GearSlot.Charm, Rarity.Uncommon,
                System.Array.Empty<GearEffect>());
            var coat = ScriptableObject.CreateInstance<GearItemData>();
            coat.ConfigureGear("gear_ut_coat", "UT Coat", GearSlot.Outfit, Rarity.Common,
                System.Array.Empty<GearEffect>());

            var root = new GameObject("gear_fmt");
            try
            {
                var reg = root.AddComponent<AssetRegistryManager>();
                reg.RegisterItem(ring);
                reg.RegisterItem(coat);
                var lo = root.AddComponent<LoadoutSystem>();
                var inv = root.AddComponent<InventorySystem>();
                lo.Bind(inv, reg);
                inv.AddItem(ring.ItemId, 1);
                inv.AddItem(coat.ItemId, 1);
                lo.TryEquip(coat.ItemId);
                lo.TryEquip(ring.ItemId, 1);

                var line = GearPromptFormatter.EquippedSummary(reg, lo);
                Assert.IsTrue(line.Contains("UT Coat (Common)"));
                Assert.IsTrue(line.Contains("UT Ring (Uncommon)"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(ring);
                Object.DestroyImmediate(coat);
            }
        }
    }

    public class LoadoutSystemTests
    {
        [Test]
        public void EquipFails_WhenItemNotHeld()
        {
            var ring = ScriptableObject.CreateInstance<GearItemData>();
            ring.ConfigureGear("gear_ut_a", "A", GearSlot.Charm, Rarity.Common, System.Array.Empty<GearEffect>());
            var root = new GameObject("lo_test");
            try
            {
                var reg = root.AddComponent<AssetRegistryManager>();
                var inv = root.AddComponent<InventorySystem>();
                var lo = root.AddComponent<LoadoutSystem>();
                reg.RegisterItem(ring);
                lo.Bind(inv, reg);
                Assert.IsFalse(lo.TryEquip(ring.ItemId));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(ring);
            }
        }

        [Test]
        public void EquipSucceeds_WhenHeld_AndSnapshotUpdates()
        {
            var ring = ScriptableObject.CreateInstance<GearItemData>();
            ring.ConfigureGear("gear_ut_b", "B", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.CaptureRateBonus, 0.03f) });
            var root = new GameObject("lo_test2");
            try
            {
                var reg = root.AddComponent<AssetRegistryManager>();
                var inv = root.AddComponent<InventorySystem>();
                var lo = root.AddComponent<LoadoutSystem>();
                reg.RegisterItem(ring);
                lo.Bind(inv, reg);
                inv.AddItem(ring.ItemId, 1);
                Assert.IsTrue(lo.TryEquip(ring.ItemId));
                Assert.AreEqual(ring.ItemId, lo.GetCharmEquippedId(0));
                Assert.That(lo.Snapshot.CaptureRateBonus, Is.EqualTo(0.03f).Within(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(ring);
            }
        }
    }

    public class GearDropRollTests
    {
        [Test]
        public void BossDropTable_AwardsDeterministicGear()
        {
            var runtime = GearTestRuntime.Create("gear_drop_runtime");
            var defeated = ScriptableObject.CreateInstance<MonsterData>();
            var table = ScriptableObject.CreateInstance<GearDropTable>();
            try
            {
                defeated.Configure("monster_ut_dropper", "Dropper", 10, 1, 1);
                table.AddEntry(DefaultGameContent.GearCharmLuckyFoxboneId, Rarity.Common, 1f, bossOnly: true);
                defeated.BindGearDropRuntime(table);

                var ok = GearLootRoller.TryRollAndAward(runtime.Manager, defeated, true, LoadoutModifiers.Empty,
                    new SequenceRandomSource(0f, 0f));

                Assert.IsTrue(ok);
                Assert.AreEqual(1, runtime.Inventory.Count(DefaultGameContent.GearCharmLuckyFoxboneId));
            }
            finally
            {
                Object.DestroyImmediate(table);
                Object.DestroyImmediate(defeated);
                runtime.Dispose();
            }
        }
    }

    public class QuestGearRewardTests
    {
        [Test]
        public void CompletingQuest_AddsConfiguredGearReward()
        {
            var runtime = GearTestRuntime.Create("quest_gear_runtime");
            var quest = ScriptableObject.CreateInstance<QuestData>();
            try
            {
                quest.Configure("quest_ut_gear_reward", "Gear Reward", "Reward test",
                    System.Array.Empty<QuestObjective>());
                quest.SetGearRewards(DefaultGameContent.GearCharmLuckyFoxboneId);

                runtime.Quests.RegisterQuestDefinition(quest);
                runtime.Quests.StartQuest(quest.QuestId);
                runtime.Quests.CompleteQuest(quest.QuestId);

                Assert.AreEqual(1, runtime.Inventory.Count(DefaultGameContent.GearCharmLuckyFoxboneId));
            }
            finally
            {
                Object.DestroyImmediate(quest);
                runtime.Dispose();
            }
        }

        [Test]
        public void RuntimeStoryQuestDefinitions_IncludeSignatureGearRewards()
        {
            var intro = DefaultGameContent.CreateIntroQuest();
            System.Collections.Generic.List<QuestData> runtime = null;
            try
            {
                Assert.Contains(DefaultGameContent.GearCharmLuckyFoxboneId,
                    new System.Collections.Generic.List<string>(intro.GearRewardItemIds));

                runtime = StoryQuestPipeline.BuildRuntimeQuestDefinitions();
                var foundReturnReward = false;
                foreach (var q in runtime)
                {
                    if (q == null || q.QuestId != ChapterOneIds.ReturnQuest) continue;
                    foundReturnReward = new System.Collections.Generic.List<string>(q.GearRewardItemIds)
                        .Contains(DefaultGameContent.GearOutfitScholarCoatId);
                    break;
                }

                Assert.IsTrue(foundReturnReward);
            }
            finally
            {
                Object.DestroyImmediate(intro);
                if (runtime != null)
                    foreach (var q in runtime)
                        Object.DestroyImmediate(q);
            }
        }
    }

    public class ProceduralContentGearTests
    {
        [Test]
        public void RegisterAll_BindsFallbackMonsterDropTables()
        {
            var root = new GameObject("procedural_gear_registry");
            try
            {
                var registry = root.AddComponent<AssetRegistryManager>();
                DefaultGameContent.RegisterAll(registry, null, null);

                Assert.IsNotNull(registry.GetItem(DefaultGameContent.GearCharmLuckyFoxboneId));
                Assert.IsNotNull(registry.GetMonster(DefaultGameContent.SlimeId)?.GearDropTable);
                Assert.IsNotNull(registry.GetMonster(DefaultGameContent.ThornBeastId)?.GearDropTable);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    public class GearVendorCatalogTests
    {
        [Test]
        public void Replenish_PreservesExistingStockCounts()
        {
            var root = new GameObject("gear_vendor_test");
            var shop = ScriptableObject.CreateInstance<ShopData>();
            try
            {
                var quests = root.AddComponent<QuestManager>();
                shop.Configure(DefaultGameContent.GearShopId);
                shop.AddListing(DefaultGameContent.GearCharmLuckyFoxboneId, 12, 0);

                GearVendorCatalog.Replenish(shop, quests);

                var listing = ShopManager.FindListing(shop, DefaultGameContent.GearCharmLuckyFoxboneId);
                Assert.IsNotNull(listing);
                Assert.AreEqual(0, listing.stock);
            }
            finally
            {
                Object.DestroyImmediate(shop);
                Object.DestroyImmediate(root);
            }
        }
    }

    sealed class SequenceRandomSource : IRandomSource
    {
        readonly float[] values;
        int index;

        public SequenceRandomSource(params float[] sequence)
        {
            values = sequence == null || sequence.Length == 0 ? new[] { 0f } : sequence;
        }

        public float Next01()
        {
            var value = values[Mathf.Min(index, values.Length - 1)];
            index++;
            return value;
        }

        public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
    }

    sealed class GearTestRuntime
    {
        readonly GameObject root;

        public GameManager Manager { get; }
        public InventorySystem Inventory { get; }
        public QuestManager Quests { get; }

        GearTestRuntime(GameObject root, GameManager manager, InventorySystem inventory, QuestManager quests)
        {
            this.root = root;
            Manager = manager;
            Inventory = inventory;
            Quests = quests;
        }

        public static GearTestRuntime Create(string name)
        {
            SetStaticInstance(null);
            foreach (var existing in Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);

            var root = new GameObject(name);
            var registry = root.AddComponent<AssetRegistryManager>();
            var monsters = root.AddComponent<MonsterSystem>();
            var inventory = root.AddComponent<InventorySystem>();
            var quests = root.AddComponent<QuestManager>();
            var world = root.AddComponent<WorldManager>();
            var achievements = root.AddComponent<AchievementSystem>();
            var weather = root.AddComponent<WeatherSystem>();
            var shop = root.AddComponent<ShopManager>();
            var gm = root.AddComponent<GameManager>();
            SetField(gm, "assetRegistry", registry);
            SetField(gm, "monsterSystem", monsters);
            SetField(gm, "inventorySystem", inventory);
            SetField(gm, "questManager", quests);
            SetField(gm, "worldManager", world);
            SetField(gm, "achievementSystem", achievements);
            SetField(gm, "worldWeather", weather);
            SetField(gm, "shopManager", shop);
            SetStaticInstance(gm);
            DefaultGameContent.RegisterAll(registry, world, shop);
            return new GearTestRuntime(root, gm, inventory, quests);
        }

        static void SetField(GameManager gm, string fieldName, object value)
        {
            typeof(GameManager).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(gm, value);
        }

        static void SetStaticInstance(GameManager gm)
        {
            typeof(GameManager).GetField("<Instance>k__BackingField",
                    BindingFlags.Static | BindingFlags.NonPublic)
                ?.SetValue(null, gm);
        }

        public void Dispose()
        {
            if (root != null)
                Object.DestroyImmediate(root);
        }
    }
}
