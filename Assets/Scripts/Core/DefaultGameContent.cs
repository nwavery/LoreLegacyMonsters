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

        /// <summary>Gear vendor shop id (Wandering Tailor).</summary>
        public const string GearShopId = "shop_gear";

        // Signature gear ids (quests / drops / docs)
        public const string GearOutfitScholarCoatId = "gear_outfit_scholar_coat";
        public const string GearOutfitForagerGreensId = "gear_outfit_forager_greens";
        public const string GearOutfitRoyalMantleId = "gear_outfit_royal_mantle";
        public const string GearOutfitStormwalkerId = "gear_outfit_stormwalker_cloak";
        public const string GearCharmLuckyFoxboneId = "gear_charm_lucky_foxbone";
        public const string GearCharmCalmingBellId = "gear_charm_calming_bell";
        public const string GearCharmIronTokenId = "gear_charm_iron_token";

        public static readonly string[] GearLootCommonIds =
        {
            GearCharmLuckyFoxboneId, GearCharmCalmingBellId, "gear_charm_swift_pin", "gear_charm_plain_loop",
            "gear_charm_coal_ember", "gear_charm_stone_knot", "gear_charm_mire_glass"
        };

        public static readonly string[] GearLootUncommonIds =
        {
            GearCharmIronTokenId, "gear_charm_solar_pendant", "gear_charm_shade_thread", "gear_charm_root_twist",
            "gear_charm_brine_drop"
        };

        public static readonly string[] GearLootRareIds =
        {
            "gear_charm_quickdraw_feather", "gear_charm_volt_amber", "gear_charm_rimer_bauble", "gear_charm_null_chip",
            "gear_outfit_marsh_waders", "gear_outfit_archive_robes"
        };

        public static readonly string[] GearLootLegendaryIds =
        {
            GearOutfitStormwalkerId, "gear_charm_gold_weevil", "gear_charm_star_splinter", "gear_outfit_crownling"
        };

        /// <summary>
        /// Authoritative starting snapshot for a new game (party, inventory, area).
        /// Used by <see cref="SaveLoad.SaveLoadManager.NewGame"/> so save-apply matches runtime expectations.
        /// </summary>
        public static SaveInfo CreateFreshSave(string playerName, int startingGold = 100)
        {
            var name = string.IsNullOrWhiteSpace(playerName) ? "Hero" : playerName.Trim();
            return new SaveInfo
            {
                Version = 9,
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
                },
                Loadout = new LoadoutDto { outfitItemId = "", charmItemIds = new List<string> { "", "", "" } }
            };
        }

        public static void RegisterAll(AssetRegistryManager reg, WorldManager world, ShopManager shop)
        {
            if (reg == null) return;

            LoadResourcesIntoRegistry(reg);
            EnsureProceduralGearCatalog(reg);

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

            BindMonsterGearDropTables(reg);

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
            q.SetGearRewards(GearCharmLuckyFoxboneId);
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
            foreach (var g in Resources.LoadAll<GearItemData>("Items"))
                if (g != null && !string.IsNullOrEmpty(g.ItemId))
                    reg.RegisterItem(g);
        }

        static void EnsureProceduralGearCatalog(AssetRegistryManager reg)
        {
            if (reg == null) return;

            void TryReg(GearItemData g)
            {
                if (g == null || string.IsNullOrEmpty(g.ItemId)) return;
                if (reg.GetItem(g.ItemId) != null) return;
                g.hideFlags = HideFlags.DontUnloadUnusedAsset;
                reg.RegisterItem(g);
            }

            GearItemData O(string id, string name, GearSlot slot, Rarity rarity, GearEffect[] fx,
                string[] tags = null, string flavor = "", string cosmetic = "")
            {
                var g = ScriptableObject.CreateInstance<GearItemData>();
                var tagList = tags == null ? new List<string>() : new List<string>(tags);
                g.ConfigureGear(id, name, slot, rarity, fx, tagList, flavor, cosmetic);
                return g;
            }

            // ----- Outfits (12) -----
            TryReg(O(GearOutfitScholarCoatId, "Tattered Scholar's Coat", GearSlot.Outfit, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.XpGainMult, 1.05f) }, new[] { "Scholar" },
                "Ink-stained and comfortable. Ideas stick better when you've walked a mile.",
                ""));
            TryReg(O(GearOutfitForagerGreensId, "Forager's Greens", GearSlot.Outfit, Rarity.Uncommon,
                new[]
                {
                    new GearEffect(GearEffectKind.EncounterRateMult, 1.1f),
                    new GearEffect(GearEffectKind.EncounterTypeBias, 0.35f, MonsterElement.Nature)
                }, new[] { "Wild", "Nature" }, "Leaves in your hair—and more beasts on your path.", ""));
            TryReg(O(GearOutfitRoyalMantleId, "Royal Mantle", GearSlot.Outfit, Rarity.Rare,
                new[]
                {
                    new GearEffect(GearEffectKind.MonsterAggressionMult, 0.75f),
                    new GearEffect(GearEffectKind.GoldGainMult, 1.2f)
                }, new[] { "Regal" }, "Authority calms the wild… or buys it off.", ""));
            TryReg(O(GearOutfitStormwalkerId, "Stormwalker Cloak", GearSlot.Outfit, Rarity.Legendary,
                new[]
                {
                    new GearEffect(GearEffectKind.MoveSpeedMult, 1.15f),
                    new GearEffect(GearEffectKind.TypeDamageMult, 1.2f, MonsterElement.Lightning)
                }, new[] { "Storm", "Bold" }, "You leave a thin ozone trail. Clouds notice.", ""));

            TryReg(O("gear_outfit_traveler_vest", "Traveler's Vest", GearSlot.Outfit, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.MoveSpeedMult, 1.05f) }, new[] { "Plain" },
                "Reliable seams. Your feet remember the road.", ""));
            TryReg(O("gear_outfit_guard_coat", "Shieldward Coat", GearSlot.Outfit, Rarity.Common,
                new[]
                {
                    new GearEffect(GearEffectKind.StatusResistMult, 1.08f, MonsterElement.None,
                        MonsterStatusEffect.None)
                }, new[] { "Stalwart" }, "Quilted lining turns stings into stories.", ""));
            TryReg(O("gear_outfit_marsh_waders", "Marsh Waders", GearSlot.Outfit, Rarity.Uncommon,
                new[]
                {
                    new GearEffect(GearEffectKind.EncounterRateMult, 0.92f),
                    new GearEffect(GearEffectKind.CaptureRateBonus, 0.02f)
                }, new[] { "Marsh" }, "Waterproof up to the ego.", ""));
            TryReg(O("gear_outfit_archive_robes", "Archive Robes", GearSlot.Outfit, Rarity.Uncommon,
                new[]
                {
                    new GearEffect(GearEffectKind.XpGainMult, 1.08f),
                    new GearEffect(GearEffectKind.TypeDamageMult, 1.1f, MonsterElement.Shadow)
                }, new[] { "Scholar", "Shadow" }, "Dust motes spell out yesterday's footnotes.", ""));
            TryReg(O("gear_outfit_sunstrider", "Sunstrider Tunic", GearSlot.Outfit, Rarity.Rare,
                new[]
                {
                    new GearEffect(GearEffectKind.MoveSpeedMult, 1.08f),
                    new GearEffect(GearEffectKind.TypeDamageMult, 1.12f, MonsterElement.Fire)
                }, new[] { "Solar" }, "Warmth without the burn—mostly.", ""));
            TryReg(O("gear_outfit_tidecloth", "Tidecloth Wrap", GearSlot.Outfit, Rarity.Rare,
                new[]
                {
                    new GearEffect(GearEffectKind.TypeDamageMult, 1.15f, MonsterElement.Water),
                    new GearEffect(GearEffectKind.InitiativeBonus, 0.15f)
                }, new[] { "Tide" }, "Salt memory clings to the weave.", ""));
            TryReg(O("gear_outfit_geode_mail", "Geode Mail", GearSlot.Outfit, Rarity.Rare,
                new[]
                {
                    new GearEffect(GearEffectKind.TypeDamageMult, 1.12f, MonsterElement.Stone),
                    new GearEffect(GearEffectKind.StatusResistMult, 1.12f, MonsterElement.None,
                        MonsterStatusEffect.None)
                }, new[] { "Stone" }, "Facets catch threats and throw them back.", ""));
            TryReg(O("gear_outfit_crownling", "Crownling Parade Silks", GearSlot.Outfit, Rarity.Legendary,
                new[]
                {
                    new GearEffect(GearEffectKind.GoldGainMult, 1.25f),
                    new GearEffect(GearEffectKind.LuckMult, 1.2f)
                }, new[] { "Regal", "Lucky" }, "Parade silks that never quite wrinkle.", ""));

            // ----- Charms (18) -----
            TryReg(O(GearCharmLuckyFoxboneId, "Lucky Foxbone", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.CaptureRateBonus, 0.03f) }, new[] { "Lucky" },
                "Whittled smooth by someone who believed.", ""));
            TryReg(O(GearCharmCalmingBellId, "Calming Bell", GearSlot.Charm, Rarity.Common,
                new[]
                {
                    new GearEffect(GearEffectKind.MonsterAggressionMult, 0.7f),
                    new GearEffect(GearEffectKind.EncounterRateMult, 0.95f)
                }, new[] { "Calm" }, "Tiny ring. Huge pause.", ""));
            TryReg(O(GearCharmIronTokenId, "Iron Token", GearSlot.Charm, Rarity.Uncommon,
                new[]
                {
                    new GearEffect(GearEffectKind.StatusResistMult, 1.15f, MonsterElement.None,
                        MonsterStatusEffect.None)
                }, new[] { "Stalwart" }, "Cold iron, warm reassurance.", ""));
            TryReg(O("gear_charm_solar_pendant", "Solar Pendant", GearSlot.Charm, Rarity.Uncommon,
                new[] { new GearEffect(GearEffectKind.TypeDamageMult, 1.12f, MonsterElement.Fire) },
                new[] { "Solar" }, "A sunflower pressed into glass.", ""));
            TryReg(O("gear_charm_swift_pin", "Swift Sandal Pin", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.MoveSpeedMult, 1.08f) }, new[] { "Swift" },
                "Pins your soles to optimism.", ""));
            TryReg(O("gear_charm_quickdraw_feather", "Quickdraw Feather", GearSlot.Charm, Rarity.Rare,
                new[] { new GearEffect(GearEffectKind.InitiativeBonus, 0.25f) }, new[] { "Bold" },
                "Acts before you approve.", ""));
            TryReg(O("gear_charm_mire_glass", "Mireglass Charm", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.TypeDamageMult, 1.08f, MonsterElement.Water) },
                new[] { "Marsh" }, "Tiny lens of still water.", ""));
            TryReg(O("gear_charm_root_twist", "Root-Twist Band", GearSlot.Charm, Rarity.Uncommon,
                new[]
                {
                    new GearEffect(GearEffectKind.EncounterTypeBias, 0.25f, MonsterElement.Nature),
                    new GearEffect(GearEffectKind.XpGainMult, 1.03f)
                }, new[] { "Wild" }, "The forest tugs your sleeve.", ""));
            TryReg(O("gear_charm_volt_amber", "Volt Amber", GearSlot.Charm, Rarity.Rare,
                new[] { new GearEffect(GearEffectKind.TypeDamageMult, 1.15f, MonsterElement.Lightning) },
                new[] { "Storm" }, "Static that hums a lullaby.", ""));
            TryReg(O("gear_charm_shade_thread", "Shade Thread", GearSlot.Charm, Rarity.Uncommon,
                new[] { new GearEffect(GearEffectKind.TypeDamageMult, 1.1f, MonsterElement.Shadow) },
                new[] { "Shadow" }, "Knots tied in twilight.", ""));
            TryReg(O("gear_charm_rimer_bauble", "Rimer Bauble", GearSlot.Charm, Rarity.Rare,
                new[]
                {
                    new GearEffect(GearEffectKind.CaptureRateBonus, 0.04f),
                    new GearEffect(GearEffectKind.LuckMult, 1.1f)
                }, new[] { "Lucky" }, "Frost on the outside, hope within.", ""));
            TryReg(O("gear_charm_gold_weevil", "Gilded Weevil", GearSlot.Charm, Rarity.Legendary,
                new[]
                {
                    new GearEffect(GearEffectKind.GoldGainMult, 1.18f),
                    new GearEffect(GearEffectKind.LuckMult, 1.25f)
                }, new[] { "Lucky", "Regal" }, "Crawls toward coin as if it were sun.", ""));
            TryReg(O("gear_charm_stone_knot", "Stone Knot", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.TypeDamageMult, 1.06f, MonsterElement.Stone) },
                new[] { "Stone" }, "You can skip it across memory.", ""));
            TryReg(O("gear_charm_coal_ember", "Coal Ember Clip", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.TypeDamageMult, 1.06f, MonsterElement.Fire) },
                new[] { "Spark" }, "Warm pocket, dangerous ideas.", ""));
            TryReg(O("gear_charm_brine_drop", "Brine Drop", GearSlot.Charm, Rarity.Uncommon,
                new[]
                {
                    new GearEffect(GearEffectKind.MonsterAggressionMult, 0.85f),
                    new GearEffect(GearEffectKind.TypeDamageMult, 1.08f, MonsterElement.Water)
                }, new[] { "Calm", "Tide" }, "Salt teaches patience.", ""));
            TryReg(O("gear_charm_null_chip", "Null Chip", GearSlot.Charm, Rarity.Rare,
                new[]
                {
                    new GearEffect(GearEffectKind.StatusResistMult, 1.18f, MonsterElement.None,
                        MonsterStatusEffect.None),
                    new GearEffect(GearEffectKind.XpGainMult, 1.05f)
                }, new[] { "Scholar" }, "Cancels noise. Includes drama.", ""));
            TryReg(O("gear_charm_star_splinter", "Star Splinter", GearSlot.Charm, Rarity.Legendary,
                new[]
                {
                    new GearEffect(GearEffectKind.InitiativeBonus, 0.2f),
                    new GearEffect(GearEffectKind.XpGainMult, 1.12f),
                    new GearEffect(GearEffectKind.LuckMult, 1.15f)
                }, new[] { "Bold", "Lucky" }, "A sliver of something that fell politely.", ""));
            TryReg(O("gear_charm_plain_loop", "Plain Loop", GearSlot.Charm, Rarity.Common,
                new[] { new GearEffect(GearEffectKind.EncounterRateMult, 0.98f) }, new[] { "Plain" },
                "Honest metal. No promises.", ""));
        }

        static void BindMonsterGearDropTables(AssetRegistryManager reg)
        {
            if (reg == null) return;

            void Bind(string monsterId, GearDropTable table)
            {
                if (string.IsNullOrEmpty(monsterId) || table == null) return;
                if (reg.GetMonster(monsterId) is MonsterData md)
                    md.BindGearDropRuntime(table);
            }

            var commonWild = ScriptableObject.CreateInstance<GearDropTable>();
            commonWild.hideFlags = HideFlags.DontUnloadUnusedAsset;
            commonWild.AddEntry(GearCharmLuckyFoxboneId, Rarity.Common, 2.5f);
            commonWild.AddEntry("gear_charm_swift_pin", Rarity.Common, 2f);
            commonWild.AddEntry(GearCharmCalmingBellId, Rarity.Common, 1.8f);

            var groveBoss = ScriptableObject.CreateInstance<GearDropTable>();
            groveBoss.hideFlags = HideFlags.DontUnloadUnusedAsset;
            groveBoss.AddEntry("gear_outfit_marsh_waders", Rarity.Uncommon, 2f, bossOnly: true);
            groveBoss.AddEntry(GearCharmIronTokenId, Rarity.Rare, 1.25f, bossOnly: true);

            var spireBoss = ScriptableObject.CreateInstance<GearDropTable>();
            spireBoss.hideFlags = HideFlags.DontUnloadUnusedAsset;
            spireBoss.AddEntry(GearOutfitStormwalkerId, Rarity.Legendary, 0.85f, bossOnly: true);
            spireBoss.AddEntry("gear_charm_star_splinter", Rarity.Legendary, 1.1f, bossOnly: true);
            spireBoss.AddEntry(GearOutfitRoyalMantleId, Rarity.Rare, 2f);

            Bind(SlimeId, commonWild);
            Bind(EmberFoxId, commonWild);
            Bind(ThornBeastId, groveBoss);
            Bind(DeltaKingId, spireBoss);

            Bind("monster_voltjay", commonWild);
            Bind("monster_mossback", commonWild);
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
