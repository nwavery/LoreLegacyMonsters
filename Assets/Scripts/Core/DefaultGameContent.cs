using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.Quests;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.Core
{
    /// <summary>Seeds minimum playable content when Resources folders are empty (README vertical slice).</summary>
    public static class DefaultGameContent
    {
        public const string SlimeId = "monster_slime";
        public const string EmberFoxId = "monster_emberfox";
        public const string ThornBeastId = "monster_thornbeast";
        public const string ReedfangId = "monster_reedfang";
        public const string LanternMothId = "monster_lanternmoth";
        public const string BogWyrmId = "monster_bogwyrm";
        public const string TidehornId = "monster_tidehorn";
        public const string StormlingId = "monster_stormling";
        public const string ShardRaptorId = "monster_shardraptor";
        public const string LanternOracleId = "monster_lanternoracle";
        public const string DeltaKingId = "monster_deltaking";
        public const string PotionId = "item_potion";
        public const string CaptureCharmId = "item_capture_charm";
        public const string ColdSalveId = "item_cold_salve";
        public const string AntidoteId = "item_antidote";
        public const string ShockTonicId = "item_shock_tonic";
        public const string TownId = "town";
        public const string RouteId = "route";
        public const string ForestId = "forest";
        public const string GroveId = "grove";
        public const string MarshId = "marsh";
        public const string RuinsId = "ruins";
        public const string DeltaId = "delta";
        public const string RidgeId = "ridge";
        public const string SpireId = "spire";
        public const string BramblewoodNorthId = "bramblewood_north";
        public const string MarshBasinId = "marsh_basin";
        public const string StonewakeId = "stonewake";
        public const string MoonwellId = "moonwell";
        public const string QuarryId = "quarry";
        public const string CrossingId = "crossing";
        public const string StarfallId = "starfall";
        public const string IntroQuestId = "quest_intro";

        /// <summary>
        /// Authoritative starting snapshot for a new game (party, inventory, area).
        /// Used by <see cref="SaveLoad.SaveLoadManager.NewGame"/> so save-apply matches runtime expectations.
        /// </summary>
        public static SaveInfo CreateFreshSave(string playerName, int startingGold = 100)
        {
            var name = string.IsNullOrWhiteSpace(playerName) ? "Hero" : playerName.Trim();
            return new SaveInfo
            {
                Version = 7,
                PlayerName = name,
                Gold = startingGold,
                CurrentAreaId = TownId,
                PlayerPositionX = 2f,
                PlayerPositionY = -1f,
                DiscoveredAreaIds = new List<string> { TownId },
                PartyMonsterIds = new List<string> { EmberFoxId },
                Party = new List<MonsterSaveEntry>(),
                Reserve = new List<MonsterSaveEntry>(),
                Inventory = new List<ItemStackDto>
                {
                    new ItemStackDto { itemId = PotionId, quantity = 3 },
                    new ItemStackDto { itemId = CaptureCharmId, quantity = 3 }
                }
            };
        }

        public static void RegisterAll(AssetRegistryManager reg, WorldManager world, ShopManager shop)
        {
            if (reg == null) return;

            LoadResourcesIntoRegistry(reg);

            if (reg.GetMonster(SlimeId) == null)
            {
                var m = ScriptableObject.CreateInstance<MonsterData>();
                m.Configure(SlimeId, "Slime", 18, 4, 2);
                m.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterMonster(m);
            }

            if (reg.GetMonster(EmberFoxId) == null)
            {
                var m = ScriptableObject.CreateInstance<MonsterData>();
                m.Configure(EmberFoxId, "Ember Fox", 22, 6, 3);
                m.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterMonster(m);
            }

            if (reg.GetMonster(ThornBeastId) == null)
            {
                var m = ScriptableObject.CreateInstance<MonsterData>();
                m.Configure(ThornBeastId, "Thorn Beast", 28, 7, 5);
                m.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterMonster(m);
            }

            if (reg.GetItem(PotionId) == null)
            {
                var p = ScriptableObject.CreateInstance<ConsumableData>();
                p.ConfigureConsumable(PotionId, "Potion", 15);
                p.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterItem(p);
            }

            if (reg.GetItem(CaptureCharmId) == null)
            {
                var c = ScriptableObject.CreateInstance<ItemData>();
                c.Configure(CaptureCharmId, "Capture Charm", ItemType.Key);
                c.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterItem(c);
            }

            if (reg.GetItem(ColdSalveId) == null)
            {
                var s = ScriptableObject.CreateInstance<ConsumableData>();
                s.ConfigureConsumable(ColdSalveId, "Cold Salve", 0, EffectType.CureStatus, MonsterStatusEffect.Burn);
                s.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterItem(s);
            }

            if (reg.GetItem(AntidoteId) == null)
            {
                var a = ScriptableObject.CreateInstance<ConsumableData>();
                a.ConfigureConsumable(AntidoteId, "Antidote", 0, EffectType.CureStatus, MonsterStatusEffect.Poison);
                a.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterItem(a);
            }

            if (reg.GetItem(ShockTonicId) == null)
            {
                var t = ScriptableObject.CreateInstance<ConsumableData>();
                t.ConfigureConsumable(ShockTonicId, "Shock Tonic", 0, EffectType.CureStatus, MonsterStatusEffect.Shock);
                t.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterItem(t);
            }

            if (world != null)
            {
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(TownId, "Town", RouteId),
                    "Prepare before heading east.", 0f));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(RouteId, "Eastern Route", TownId, ForestId),
                    "The route introduces early wild encounters.", 0.18f, SlimeId, "monster_voltjay", "monster_shadecub"));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(ForestId, "Forest", RouteId, GroveId),
                    "The forest thickens and stronger wild packs appear.", 0.24f, EmberFoxId, "monster_mossback", SlimeId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(GroveId, "Forest Grove", ForestId, MarshId),
                    "The grove still bears the Briar Warden's mark.", 0.28f, ThornBeastId, "monster_mireooze", "monster_blazelynx"));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(MarshId, "Lantern Marsh", GroveId, RuinsId),
                    "Lantern lights and floodwater hide old tracks.", 0.34f, "monster_mireooze", ReedfangId, LanternMothId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(RuinsId, "Sunken Archive", MarshId, DeltaId),
                    "Archive stones and deep shadows feed dangerous encounters.", 0.38f, BogWyrmId, LanternMothId, "monster_shadecub"));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(DeltaId, "Flooded Delta", RuinsId, RidgeId),
                    "Broken canals open into the delta beyond the archive.", 0.4f, TidehornId, ReedfangId, LanternOracleId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(RidgeId, "Stormbreak Ridge", DeltaId, SpireId),
                    "High winds and cliff roosts make the ridge dangerous.", 0.43f, StormlingId, ShardRaptorId, LanternOracleId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(SpireId, "Skyglass Spire", RidgeId),
                    "The spire gathers relic lightning and elite monsters.", 0.47f, DeltaKingId, ShardRaptorId, StormlingId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(BramblewoodNorthId, "Bramblewood North", ForestId, StonewakeId, MoonwellId),
                    "A northern woodland full of old road markers and hidden monster dens.", 0.34f, "monster_mossback", "monster_blazelynx", "monster_shadecub"));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(MarshBasinId, "Lantern Marsh Basin", MarshId, StonewakeId, StarfallId),
                    "Boardwalks wind through deeper lantern pools and half-sunken lore stones.", 0.38f, ReedfangId, LanternMothId, BogWyrmId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(StonewakeId, "Stonewake Hamlet", RouteId, BramblewoodNorthId, MarshBasinId),
                    "A safe crossroads hamlet where cartographers, runners, and traders gather.", 0f));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(MoonwellId, "Moonwell Grove", BramblewoodNorthId, StarfallId),
                    "A quiet sanctuary where monster bonds and old lore rites overlap.", 0.3f, EmberFoxId, LanternOracleId, "monster_blazelynx"));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(QuarryId, "Ironroot Quarry", RuinsId, CrossingId, StarfallId),
                    "Broken stone, deep roots, and territorial monsters make every step loud.", 0.46f, ShardRaptorId, StormlingId, ThornBeastId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(CrossingId, "Tideglass Crossing", DeltaId, RidgeId, QuarryId),
                    "An old bridge complex ties the delta road to new northern paths.", 0.36f, TidehornId, ReedfangId, LanternOracleId));
                world.RegisterArea(ConfigureAreaWorldData(CreateOrLoadArea(StarfallId, "Starfall Hollow", MoonwellId, MarshBasinId, QuarryId),
                    "A late frontier hollow where archive echoes fall out of the sky.", 0.5f, DeltaKingId, LanternOracleId, ShardRaptorId));
                if (string.IsNullOrEmpty(world.CurrentAreaId)) world.SetCurrentArea(TownId);
            }

            if (shop != null && shop.Current == null)
            {
                var sd = Resources.Load<ShopData>("Shops/shop_general");
                if (sd == null)
                {
                    sd = ScriptableObject.CreateInstance<ShopData>();
                    sd.Configure(SampleShops.GeneralStoreId);
                    sd.AddListing(PotionId, 12, 20);
                    sd.AddListing(CaptureCharmId, 18, 12);
                    sd.AddListing(ColdSalveId, 10, 12);
                    sd.AddListing(AntidoteId, 10, 12);
                    sd.AddListing(ShockTonicId, 10, 12);
                    sd.hideFlags = HideFlags.DontUnloadUnusedAsset;
                }
                shop.SetShop(sd);
            }
        }

        public static DialogData CreateElderGreetingDialog()
        {
            var d = ScriptableObject.CreateInstance<DialogData>();
            d.Configure(SampleDialogs.ElderGreetingId, new[]
            {
                new DialogEntry
                {
                    speaker = "Mira, Town Elder",
                    line =
                        "Welcome to Hollowfen. Wild monsters have been stirring in the forest—travelers rarely return unscathed."
                },
                new DialogEntry
                {
                    speaker = "Mira, Town Elder",
                    line =
                        "The guild asks every new trainer to prove themselves: win one battle beyond the gate, then we will trust you with real work."
                },
                new DialogEntry
                {
                    speaker = "Mira, Town Elder",
                    line =
                        "When you are ready, head east to the forest. Good luck—and do not linger after dark."
                }
            });
            d.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return d;
        }

        public static QuestData CreateIntroQuest()
        {
            var q = ScriptableObject.CreateInstance<QuestData>();
            q.Configure(IntroQuestId, "First Steps", "Win one battle in the forest.",
                new[]
                {
                    new QuestObjective
                    {
                        objectiveId = "win_battle",
                        description = "Win a wild battle",
                        requiredCount = 1,
                        currentCount = 0
                    }
                });
            q.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return q;
        }

        static void LoadResourcesIntoRegistry(AssetRegistryManager reg)
        {
            foreach (var m in Resources.LoadAll<MonsterData>("Monsters"))
                if (m != null && !string.IsNullOrEmpty(m.MonsterId))
                    reg.RegisterMonster(m);
            foreach (var i in Resources.LoadAll<ItemData>("Items"))
                if (i != null && !string.IsNullOrEmpty(i.ItemId))
                    reg.RegisterItem(i);
            foreach (var c in Resources.LoadAll<ConsumableData>("Items"))
                if (c != null && !string.IsNullOrEmpty(c.ItemId))
                    reg.RegisterItem(c);
        }

        static WorldArea CreateOrLoadArea(string id, string title, params string[] links)
        {
            var path = $"Areas/{id}";
            var existing = Resources.Load<WorldArea>(path);
            if (existing != null)
            {
                existing.Configure(id, title, links);
                return existing;
            }
            var a = ScriptableObject.CreateInstance<WorldArea>();
            a.Configure(id, title, links);
            a.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return a;
        }

        static WorldArea ConfigureAreaWorldData(WorldArea area, string hint, float encounterChance, params string[] encounters)
        {
            if (area == null) return null;
            area.SetTravelHint(hint);
            area.SetEncounterChance(encounterChance);
            area.SetWildEncounters(encounters);
            return area;
        }
    }
}
