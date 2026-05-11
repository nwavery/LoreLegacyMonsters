using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Combat;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters.SaveLoad;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Dialog.Llm;
using LoreLegacyMonsters.UI;
using SaveSvc = LoreLegacyMonsters.SaveSystem.SaveSystem;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class CampaignFoundationTests
    {
        [Test]
        public void SeededRandomSource_SameSeed_SameSequence()
        {
            var a = new SeededRandomSource(999);
            var b = new SeededRandomSource(999);
            for (var i = 0; i < 5; i++)
                Assert.AreEqual(a.Next01(), b.Next01(), i.ToString());
        }

        [Test]
        public void CampaignProgression_StartsIntro_WhenNoProgress()
        {
            var go = new GameObject("campaign_q");
            var qm = go.AddComponent<QuestManager>();
            StoryQuestPipeline.RegisterAll(qm);
            CampaignProgression.TryAdvance(qm, null, _ => { });
            Assert.IsTrue(qm.IsActive(ChapterOneIds.IntroQuest));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void StoryQuestPipeline_RegisterAll_IsIdempotentPerManager()
        {
            var go = new GameObject("quest-register-idempotent");
            try
            {
                var qm = go.AddComponent<QuestManager>();
                Assert.IsTrue(StoryQuestPipeline.RegisterAll(qm));
                Assert.IsFalse(StoryQuestPipeline.RegisterAll(qm));
                Assert.IsNotNull(qm.GetDefinition(ChapterOneIds.IntroQuest));
                Assert.IsNotNull(qm.GetDefinition(PhaseTwoIds.BindingChoiceQuest));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CombatSystem_SeededRng_CritIsDeterministic()
        {
            var sys = new CombatSystem(new SeededRandomSource(2026));
            var move = ScriptableObject.CreateInstance<MoveData>();
            move.Configure("move_test", "Test Strike", MonsterElement.Fire, MoveEffectType.Attack, 8, 0.5f);

            var d1 = sys.CalculateMoveDamage(10, 4, move, MonsterElement.Fire, MonsterElement.Nature,
                MonsterElement.None, out var c1, out _);
            var sys2 = new CombatSystem(new SeededRandomSource(2026));
            var d2 = sys2.CalculateMoveDamage(10, 4, move, MonsterElement.Fire, MonsterElement.Nature,
                MonsterElement.None, out var c2, out _);

            Assert.AreEqual(c1, c2);
            Assert.AreEqual(d1, d2);
            Object.DestroyImmediate(move);
        }

        [Test]
        public void CaptureRules_SeededRoll_IsDeterministic()
        {
            var data = ScriptableObject.CreateInstance<MonsterData>();
            data.Configure("monster_cap", "Capmon", 20, 5, 2);
            var rng = new SeededRandomSource(42);
            var r1 = CaptureRules.Roll(data, 10, 20, MonsterStatusEffect.None, 1f, false, rng);
            var r2 = CaptureRules.Roll(data, 10, 20, MonsterStatusEffect.None, 1f, false, new SeededRandomSource(42));
            Assert.AreEqual(r1.Success, r2.Success);
            Assert.AreEqual(r1.Roll, r2.Roll, 0.0001f);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void OnboardingHelpText_IncludesFirstSessionAndMapGuidance()
        {
            Assert.IsTrue(AlphaHelpText.ControlsBody.Contains("FIRST 20 MINUTES"));
            Assert.IsTrue(AlphaHelpText.ControlsBody.Contains("MAP QUICK TIP"));
            Assert.IsTrue(AlphaHelpText.ControlsBody.Contains("YOU is your current area"));
            Assert.IsTrue(AlphaHelpText.ControlsBody.Contains("Esc pause"));
        }

        [Test]
        public void GameSettings_Pause_DefaultsToEscape()
        {
            PlayerPrefs.DeleteKey("settings.key.pause");
            PlayerPrefs.Save();
            Assert.AreEqual(Key.Escape, GameSettings.Pause);
        }

        [Test]
        public void CombatFeedback_BuildAttackFeedback_UsesStructuredLabels()
        {
            var method = typeof(CombatManager).GetMethod("BuildAttackFeedback", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "BuildAttackFeedback method not found.");

            var move = ScriptableObject.CreateInstance<MoveData>();
            move.Configure("move_test_feedback", "Test Strike", MonsterElement.Fire, MoveEffectType.Attack, 8, 0f);
            var defenderGo = new GameObject("defender");
            try
            {
                var defender = defenderGo.AddComponent<CombatEntity>();
                defender.SetStatus(MonsterStatusEffect.Burn);
                var feedback = method.Invoke(null, new object[] { move, 9, 1.5f, true, defender, true }) as string;
                Assert.IsNotNull(feedback);
                Assert.IsTrue(feedback.Contains("Type: Advantage"), feedback);
                Assert.IsTrue(feedback.Contains("Crit: Yes"), feedback);
                Assert.IsTrue(feedback.Contains("Status: Burn"), feedback);
                Assert.IsTrue(feedback.Contains("HP"), feedback);
            }
            finally
            {
                Object.DestroyImmediate(move);
                Object.DestroyImmediate(defenderGo);
            }
        }

        [Test]
        public void SaveSystem_TryLoad_MissingFile_ReturnsError()
        {
            const int slot = 19001;
            Assert.IsFalse(SaveSvc.TryLoad(slot, out _, out var err), err);
            Assert.IsFalse(string.IsNullOrEmpty(err));
        }

        [Test]
        public void DefaultGameContent_CreateFreshSave_SeedsPartyAreaAndInventory()
        {
            var s = DefaultGameContent.CreateFreshSave("Tester");
            Assert.AreEqual("Tester", s.PlayerName);
            Assert.AreEqual(9, s.Version);
            Assert.AreEqual(100, s.Gold);
            Assert.AreEqual(DefaultGameContent.TownId, s.CurrentAreaId);
            Assert.AreEqual(2f, s.PlayerPositionX);
            Assert.AreEqual(-1f, s.PlayerPositionY);
            Assert.Contains(DefaultGameContent.TownId, s.DiscoveredAreaIds);
            Assert.AreEqual(1, s.PartyMonsterIds.Count);
            Assert.AreEqual(DefaultGameContent.EmberFoxId, s.PartyMonsterIds[0]);
            Assert.AreEqual(2, s.Inventory.Count);
            var potion = s.Inventory.Find(x => x.itemId == DefaultGameContent.PotionId);
            var charm = s.Inventory.Find(x => x.itemId == DefaultGameContent.CaptureCharmId);
            Assert.IsNotNull(potion);
            Assert.AreEqual(3, potion.quantity);
            Assert.IsNotNull(charm);
            Assert.AreEqual(3, charm.quantity);
            Assert.IsNotNull(s.Loadout);
            Assert.AreEqual("", s.Loadout.outfitItemId);
            Assert.AreEqual(3, s.Loadout.charmItemIds.Count);
        }

        [Test]
        public void WorldMapLayout_ResolvesTwoDimensionalPhaseTwoRegions()
        {
            Assert.AreEqual(DefaultGameContent.TownId, WorldMapLayout.ResolveAreaId(new Vector2(2f, -1f)));
            Assert.AreEqual(DefaultGameContent.StonewakeId, WorldMapLayout.ResolveAreaId(new Vector2(14f, 9f)));
            Assert.AreEqual(DefaultGameContent.MoonwellId, WorldMapLayout.ResolveAreaId(new Vector2(39f, 23f)));
            Assert.AreEqual(DefaultGameContent.QuarryId, WorldMapLayout.ResolveAreaId(new Vector2(84f, 9f)));
            Assert.AreEqual(DefaultGameContent.StarfallId, WorldMapLayout.ResolveAreaId(new Vector2(68f, 25f)));
            Assert.IsTrue(WorldMapLayout.WorldBounds().height > 30f, "Phase 2 map should be taller than the old side-scroller strip.");
        }

        [Test]
        public void WorldMapLayout_ExposesBranchedPhaseTwoGraph()
        {
            Assert.IsTrue(HasEdge(DefaultGameContent.StonewakeId, DefaultGameContent.BramblewoodNorthId));
            Assert.IsTrue(HasEdge(DefaultGameContent.StonewakeId, DefaultGameContent.MarshBasinId));
            Assert.IsTrue(HasEdge(DefaultGameContent.MoonwellId, DefaultGameContent.StarfallId));
            Assert.IsTrue(HasEdge(DefaultGameContent.QuarryId, DefaultGameContent.CrossingId));
            Assert.IsTrue(HasEdge(DefaultGameContent.QuarryId, DefaultGameContent.StarfallId));
            Assert.GreaterOrEqual(WorldMapLayout.MapEdges.Count, WorldMapLayout.All.Count - 1);
        }

        [Test]
        public void WorldMapLayout_EncountersOnlyOccurInMarkedZones()
        {
            Assert.IsFalse(WorldMapLayout.IsEncounterPosition(DefaultGameContent.TownId, new Vector2(2f, -1f)));
            Assert.IsFalse(WorldMapLayout.IsEncounterPosition(DefaultGameContent.RouteId, WorldMapLayout.SpawnPoint(DefaultGameContent.RouteId)));
            Assert.IsTrue(WorldMapLayout.IsEncounterPosition(DefaultGameContent.RouteId, new Vector2(14f, -2.4f)));
            Assert.IsFalse(WorldMapLayout.IsEncounterPosition(DefaultGameContent.ForestId, WorldMapLayout.SpawnPoint(DefaultGameContent.ForestId)));
            Assert.IsTrue(WorldMapLayout.IsEncounterPosition(DefaultGameContent.ForestId, new Vector2(32f, -2.4f)));
            Assert.IsFalse(WorldMapLayout.IsEncounterPosition(DefaultGameContent.StonewakeId, WorldMapLayout.SpawnPoint(DefaultGameContent.StonewakeId)));
            Assert.IsTrue(WorldMapLayout.IsEncounterPosition(DefaultGameContent.MoonwellId, new Vector2(34f, 21f)));
            Assert.IsTrue(WorldMapLayout.IsEncounterPosition(DefaultGameContent.QuarryId, new Vector2(78f, 8f)));
        }

        [Test]
        public void WorldMapLayout_VisualRoadsSuppressEncountersInsideDangerZones()
        {
            Assert.IsTrue(WorldMapLayout.IsVisualRoadPosition(WorldMapLayout.SpawnPoint(DefaultGameContent.ForestId)));
            Assert.IsFalse(WorldMapLayout.IsEncounterPosition(DefaultGameContent.ForestId, WorldMapLayout.SpawnPoint(DefaultGameContent.ForestId)));
            Assert.IsFalse(WorldMapLayout.IsVisualRoadPosition(new Vector2(32f, -2.4f)));
            Assert.IsTrue(WorldMapLayout.IsEncounterPosition(DefaultGameContent.ForestId, new Vector2(32f, -2.4f)));
        }

        [Test]
        public void WorldMapLayout_VisualRoadSuppression_HasReadableBoundary()
        {
            var roadPoint = WorldMapLayout.SpawnPoint(DefaultGameContent.RouteId);
            var nearRoad = roadPoint + new Vector2(0f, 0.2f);
            var offRoad = new Vector2(14f, -2.4f);
            Assert.IsTrue(WorldMapLayout.IsVisualRoadPosition(roadPoint));
            Assert.IsTrue(WorldMapLayout.IsVisualRoadPosition(nearRoad));
            Assert.IsFalse(WorldMapLayout.IsVisualRoadPosition(offRoad));
        }

        [Test]
        public void WorldMapLayout_EncounterZonesHaveReadableTypes()
        {
            var moonwellZones = WorldMapLayout.EncounterZones(DefaultGameContent.MoonwellId);
            Assert.Greater(moonwellZones.Length, 0);
            Assert.AreEqual(EncounterZoneType.MoonwellGlade, moonwellZones[0].Type);
            Assert.IsFalse(WorldMapLayout.IsEncounterPosition(DefaultGameContent.StonewakeId, WorldMapLayout.SpawnPoint(DefaultGameContent.StonewakeId)));
        }

        [Test]
        public void WorldMapLayout_NavigationRejectsBlockerZones()
        {
            var from = new Vector2(14f, 8f);
            var blocked = new Vector2(16f, 9f);
            Assert.IsTrue(WorldMapLayout.IsBlockedPosition(blocked));
            var resolved = WorldMapLayout.ResolveNavigation(from, blocked);
            Assert.IsFalse(WorldMapLayout.IsBlockedPosition(resolved));
        }

        [Test]
        public void WorldMapLayout_NavigationUsesCollisionRadius()
        {
            var nearStonewakeHouse = new Vector2(15.45f, 8f);
            Assert.IsFalse(WorldMapLayout.IsBlockedPosition(nearStonewakeHouse));
            Assert.IsTrue(WorldMapLayout.IsBlockedPosition(nearStonewakeHouse, 0.28f));

            var resolved = WorldMapLayout.ResolveNavigation(new Vector2(14.9f, 8f), nearStonewakeHouse, 0.28f);
            Assert.IsFalse(WorldMapLayout.IsBlockedPosition(resolved, 0.28f));
            Assert.Less(resolved.x, nearStonewakeHouse.x);
        }

        [Test]
        public void WorldMapLayout_NavigationSlidesAlongOpenAxis()
        {
            var from = new Vector2(14.9f, 7.7f);
            var blockedDiagonal = new Vector2(15.7f, 8.25f);
            var resolved = WorldMapLayout.ResolveNavigation(from, blockedDiagonal, 0.28f);

            Assert.IsFalse(WorldMapLayout.IsBlockedPosition(resolved, 0.28f));
            Assert.Greater(resolved.y, from.y, "The player should keep vertical movement when the horizontal approach is blocked.");
        }

        [Test]
        public void QuestObjectiveTargetMap_ResolvesPhaseTwoObjectives()
        {
            Assert.AreEqual(DefaultGameContent.StonewakeId, QuestObjectiveTargetMap.ResolveAreaId(PhaseTwoIds.TalkCartographer));
            Assert.AreEqual(DefaultGameContent.MoonwellId, QuestObjectiveTargetMap.ResolveAreaId(PhaseTwoIds.VisitMoonwell));
            Assert.AreEqual(DefaultGameContent.CrossingId, QuestObjectiveTargetMap.ResolveAreaId(PhaseTwoIds.DefeatSable));
            Assert.AreEqual(DefaultGameContent.StarfallId, QuestObjectiveTargetMap.ResolveAreaId(PhaseTwoIds.JessaVisitStarfall));
        }

        [Test]
        public void StoryQuestPipeline_RuntimeObjectives_AreMappedOrExplicitlyNonMap()
        {
            var defs = StoryQuestPipeline.BuildRuntimeQuestDefinitions();
            try
            {
                for (var i = 0; i < defs.Count; i++)
                {
                    var objectives = defs[i] != null ? defs[i].Objectives : null;
                    if (objectives == null)
                        continue;
                    for (var j = 0; j < objectives.Length; j++)
                    {
                        var objectiveId = objectives[j].objectiveId;
                        Assert.IsTrue(
                            QuestObjectiveTargetMap.IsMappedOrExplicitNonMap(objectiveId),
                            $"Objective '{objectiveId}' should resolve to an area or be explicitly non-map-based.");
                    }
                }
            }
            finally
            {
                for (var i = 0; i < defs.Count; i++)
                    if (defs[i] != null)
                        Object.DestroyImmediate(defs[i]);
            }
        }

        [Test]
        public void StoryQuestPipeline_RuntimeDefinitions_HaveNoDuplicateQuestIds()
        {
            Assert.IsFalse(
                StoryQuestPipeline.TryGetDuplicateRuntimeQuestId(out var duplicateQuestId),
                $"Duplicate runtime quest id found: {duplicateQuestId}");
        }

        [Test]
        public void Registries_CoverPhaseTwoNpcsAndObjectives()
        {
            Assert.IsTrue(NpcContentRegistry.TryGet(NPCController.CartographerJessaId, out var jessa));
            Assert.AreEqual(PhaseTwoIds.CartographerDialog, jessa.DialogBuilder(null).DialogId);
            foreach (var pair in ObjectiveRegistry.ObjectiveAreaMap)
                Assert.IsFalse(string.IsNullOrWhiteSpace(pair.Value), pair.Key);
        }

        [Test]
        public void NpcContentRegistry_CoversBranchCriticalNpcDefinitions()
        {
            Assert.IsTrue(NpcContentRegistry.TryGet(NPCController.BossIonaId, out var iona));
            Assert.AreEqual(NpcRole.BossTrainer, iona.Role);
            Assert.IsTrue(NpcContentRegistry.TryGet(NPCController.RivalCorinId, out var corin));
            Assert.AreEqual(NpcRole.BossTrainer, corin.Role);
            Assert.IsTrue(NpcContentRegistry.TryGet(NPCController.StormTyrantId, out var varo));
            Assert.AreEqual(NpcRole.BossTrainer, varo.Role);
            Assert.IsTrue(NpcContentRegistry.TryGet(NPCController.EthicistThrenId, out var thren));
            Assert.IsNotNull(thren.DialogBuilder);
            Assert.IsTrue(NpcContentRegistry.TryGet(NPCController.SableRivalId, out var sable));
            Assert.IsNotNull(sable.DialogBuilder);
        }

        [Test]
        public void PhaseTwoWorldContent_ReflectsAdvisorAndSableBranchState()
        {
            StoryFlags.ApplySave(null);
            StoryState.SetAdvisor(StoryState.AdvisorThren);
            var threnDialog = PhaseTwoWorldContent.BuildDialog(NPCController.EthicistThrenId, null);
            Assert.IsNotNull(threnDialog);
            Assert.IsTrue(threnDialog.Entries[0].line.Contains("consent"), "Advisor-specific ethicist line should mention consent.");

            StoryFlags.SetFlag(PhaseTwoIds.FlagSablePeaceResolution);
            var sableDialog = PhaseTwoWorldContent.BuildDialog(NPCController.SableRivalId, null);
            Assert.IsNotNull(sableDialog);
            Assert.IsTrue(sableDialog.Entries[0].line.Contains("words"), "Sable should acknowledge peace resolution branch.");
        }

        [Test]
        public void NpcLlmPromptContext_StoryStateSummary_ContainsConsequenceSummary()
        {
            StoryFlags.ApplySave(null);
            StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroAlly);
            var method = typeof(NpcLlmPromptContext).GetMethod("BuildStoryStateSummary", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "BuildStoryStateSummary method not found.");
            var summary = method.Invoke(null, null) as string;
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Contains("consequence="), summary);
            Assert.IsTrue(summary.Contains("varo alliance"), summary);
        }

        [Test]
        public void NpcLlmPromptContext_StoryStateSummary_IncludesKnowledgeTags()
        {
            StoryFlags.ApplySave(null);
            StoryFlags.SetFlag(StoryState.NetworkAware);
            StoryFlags.SetFlag(StoryState.CorinTruthKnown);
            var method = typeof(NpcLlmPromptContext).GetMethod("BuildStoryStateSummary", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, "BuildStoryStateSummary method not found.");
            var summary = method.Invoke(null, null) as string;
            Assert.IsNotNull(summary);
            Assert.IsTrue(summary.Contains("knowledge="), summary);
            Assert.IsTrue(summary.Contains("network_aware"), summary);
            Assert.IsTrue(summary.Contains("corin_truth_known"), summary);
        }

        [Test]
        public void ObjectiveRegistry_DefaultBossOutcomes_AreStable()
        {
            Assert.IsTrue(ObjectiveRegistry.TryGetDefaultBossOutcome(ChapterOneIds.DefeatBoss, out var ionaKey, out var ionaValue));
            Assert.AreEqual(StoryState.IonaOutcomeKey, ionaKey);
            Assert.AreEqual(StoryState.IonaDefeat, ionaValue);

            Assert.IsTrue(ObjectiveRegistry.TryGetDefaultBossOutcome(ChapterTwoIds.DefeatRival, out var corinKey, out var corinValue));
            Assert.AreEqual(StoryState.CorinOutcomeKey, corinKey);
            Assert.AreEqual(StoryState.CorinHandRelicToSel, corinValue);

            Assert.IsTrue(ObjectiveRegistry.TryGetDefaultBossOutcome(ChapterThreeIds.DefeatSpireBoss, out var varoKey, out var varoValue));
            Assert.AreEqual(StoryState.VaroOutcomeKey, varoKey);
            Assert.AreEqual(StoryState.VaroDefeat, varoValue);
        }

        [Test]
        public void StoryFlags_RoundTripThroughSaveContributor()
        {
            StoryFlags.ApplySave(null);
            StoryFlags.SetFlag(PhaseTwoIds.FlagHelpedJessaLandmarks);
            var save = new SaveInfo();
            var coord = new SaveCoordinator();
            coord.Register(new StoryFlagTestContributor());
            coord.CaptureAll(save);

            StoryFlags.ApplySave(null);
            Assert.IsFalse(StoryFlags.HasFlag(PhaseTwoIds.FlagHelpedJessaLandmarks));
            coord.ApplyAll(save);
            Assert.IsTrue(StoryFlags.HasFlag(PhaseTwoIds.FlagHelpedJessaLandmarks));
        }

        [Test]
        public void StoryFlags_KeyValueAndIntHelpers_RoundTrip()
        {
            StoryFlags.ApplySave(null);
            StoryFlags.SetValue("iona_outcome", "spare");
            StoryFlags.AddInt("mira_trust", 2);
            StoryFlags.AddInt("mira_trust", 2, 0, 3);
            Assert.AreEqual("spare", StoryFlags.GetValue("iona_outcome"));
            Assert.AreEqual(3, StoryFlags.GetInt("mira_trust"));
        }

        [Test]
        public void DialogSystem_JumpTo_ResolvesChoiceBranches()
        {
            var go = new GameObject("dialog_system");
            try
            {
                var system = go.AddComponent<DialogSystem>();
                var data = ScriptableObject.CreateInstance<DialogData>();
                data.Configure("dlg_choice_test", new[]
                {
                    new DialogEntry
                    {
                        speaker = "A",
                        line = "Choose",
                        choiceLabels = new[] {"Left", "Right"},
                        choiceNextIds = new[] {"next:1", "next:2"}
                    },
                    new DialogEntry { speaker = "A", line = "Left branch" },
                    new DialogEntry { speaker = "A", line = "Right branch" }
                });
                system.Begin(data);
                Assert.IsTrue(system.JumpTo(2));
                Assert.IsTrue(system.TryGetLine(out var line));
                Assert.AreEqual("Right branch", line.line);
                Object.DestroyImmediate(data);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void EndingResolver_SuggestsBurn_ForHardlineState()
        {
            StoryFlags.ApplySave(null);
            StoryState.SetOutcome(StoryState.IonaOutcomeKey, StoryState.IonaWithdraw);
            StoryState.SetOutcome(StoryState.CorinOutcomeKey, StoryState.CorinSideWithCorin);
            StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroRefuseSpire);
            StoryState.SetAdvisor(StoryState.AdvisorJessa);
            StoryFlags.SetInt(StoryState.MiraTrustKey, 0);
            Assert.AreEqual(StoryEnding.Burn, EndingResolver.SuggestEnding());
        }

        [Test]
        public void CampaignProgression_ReconvergesAfterAlternativeBossResolutions()
        {
            var go = new GameObject("campaign_branch");
            try
            {
                var qm = go.AddComponent<QuestManager>();
                StoryQuestPipeline.RegisterAll(qm);
                qm.StartQuest(ChapterOneIds.BossQuest);
                qm.ReportObjectiveEvent(ChapterOneIds.VisitForest, 1);
                qm.ReportObjectiveEvent(ChapterOneIds.DefeatBoss, 1);
                CampaignProgression.TryAdvance(qm, null, _ => { });
                Assert.IsTrue(qm.IsActive(ChapterOneIds.ReturnQuest));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void LlmCommandParser_StripsInvalidMarkers()
        {
            var parsed = NpcLlmCommandParser.TryParseAndStrip("Meet me there. [[command:unknown|bad]]", out var display, out var command);
            Assert.IsFalse(parsed);
            Assert.IsNull(command);
            Assert.AreEqual("Meet me there.", display);
        }

        [Test]
        public void ObjectiveRegistry_CompletesSableBranchByTalking()
        {
            var go = new GameObject("branch-q");
            try
            {
                var qm = go.AddComponent<QuestManager>();
                StoryQuestPipeline.RegisterAll(qm);
                qm.StartQuest(PhaseTwoIds.SableRematchQuest);
                StoryFlags.ApplySave(null);

                ObjectiveRegistry.ReportNpcInteraction(qm, NPCController.SableRivalId);

                Assert.IsTrue(qm.IsCompleted(PhaseTwoIds.SableRematchQuest));
                Assert.IsTrue(StoryFlags.HasFlag(PhaseTwoIds.FlagSablePeaceResolution));
            }
            finally
            {
                Object.DestroyImmediate(go);
                StoryFlags.ApplySave(null);
            }
        }

        [Test]
        public void EncounterService_RejectsSafePlayerPositions()
        {
            var go = new GameObject("encounter-zone-test");
            var area = ScriptableObject.CreateInstance<WorldArea>();
            try
            {
                var world = go.AddComponent<WorldManager>();
                var svc = go.AddComponent<EncounterService>();
                area.Configure(DefaultGameContent.RouteId, "Route");
                area.SetEncounterChance(1f);
                area.SetWildEncounters(DefaultGameContent.SlimeId);
                world.RegisterArea(area);
                world.SetCurrentArea(DefaultGameContent.RouteId);
                world.SetCurrentPlayerPosition(WorldMapLayout.SpawnPoint(DefaultGameContent.RouteId));
                Assert.IsFalse(svc.CanEncounterAt(world));
                world.SetCurrentPlayerPosition(new Vector2(14f, -2.4f));
                Assert.IsTrue(svc.CanEncounterAt(world));
            }
            finally
            {
                Object.DestroyImmediate(area);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void WorldManager_DiscoveryTracksCurrentAndAppliedAreas()
        {
            var go = new GameObject("world-discovery");
            try
            {
                var world = go.AddComponent<WorldManager>();
                var town = ScriptableObject.CreateInstance<WorldArea>();
                town.Configure(DefaultGameContent.TownId, "Town", DefaultGameContent.RouteId);
                var route = ScriptableObject.CreateInstance<WorldArea>();
                route.Configure(DefaultGameContent.RouteId, "Eastern Route", DefaultGameContent.TownId);

                world.RegisterArea(town);
                world.RegisterArea(route);
                Assert.IsTrue(world.IsAreaDiscovered(DefaultGameContent.TownId));

                world.TryTravelTo(DefaultGameContent.RouteId);
                Assert.IsTrue(world.IsAreaDiscovered(DefaultGameContent.RouteId));

                world.ApplyDiscoveredAreaIds(new System.Collections.Generic.List<string> { DefaultGameContent.TownId });
                Assert.IsTrue(world.IsAreaDiscovered(DefaultGameContent.TownId));
                Assert.IsTrue(world.IsAreaDiscovered(DefaultGameContent.RouteId), "Current area remains discovered after loading older discovery lists.");

                Object.DestroyImmediate(town);
                Object.DestroyImmediate(route);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        static bool HasEdge(string a, string b)
        {
            for (var i = 0; i < WorldMapLayout.MapEdges.Count; i++)
            {
                var edge = WorldMapLayout.MapEdges[i];
                if ((edge.FromAreaId == a && edge.ToAreaId == b) || (edge.FromAreaId == b && edge.ToAreaId == a))
                    return true;
            }

            return false;
        }

        sealed class StoryFlagTestContributor : ISaveContributor
        {
            public void ApplySave(SaveInfo save) => StoryFlags.ApplySave(save.StoryFlags);
            public void CaptureSave(SaveInfo save) => save.StoryFlags = StoryFlags.ExportSave();
        }

        [Test]
        public void SaveLoadManager_NewGame_SetsAuthoritativeWorkingCopyAndGold()
        {
            if (SaveLoadManager.Instance != null)
                Object.DestroyImmediate(SaveLoadManager.Instance.gameObject);

            var go = new GameObject("slm_test");
            var sl = go.AddComponent<SaveLoadManager>();
            Assert.IsFalse(sl.HasAuthoritativeWorkingCopy);
            sl.NewGame("Tester");
            Assert.IsTrue(sl.HasAuthoritativeWorkingCopy);
            Assert.AreEqual(100, sl.WorkingCopy.Gold);
            Assert.AreEqual(DefaultGameContent.EmberFoxId, sl.WorkingCopy.PartyMonsterIds[0]);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void UIManager_BlockingState_ComesFromModalRegistry()
        {
            var go = new GameObject("ui-manager-test");
            try
            {
                var ui = go.AddComponent<UIManager>();
                Assert.IsFalse(ui.IsBlockingWorldInput);
                ui.SetModalOpen(UiModal.Map, true);
                Assert.IsTrue(ui.IsBlockingWorldInput);
                ui.SetModalOpen(UiModal.Map, false);
                Assert.IsFalse(ui.IsBlockingWorldInput);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SaveLoadManager_CanAutoSaveNow_RespectsBlockingModalUnlessExplicit()
        {
            if (SaveLoadManager.Instance != null)
                Object.DestroyImmediate(SaveLoadManager.Instance.gameObject);
            if (UIManager.Instance != null)
                Object.DestroyImmediate(UIManager.Instance.gameObject);

            var uiGo = new GameObject("ui-autosave-test");
            var saveGo = new GameObject("save-autosave-test");
            try
            {
                var ui = uiGo.AddComponent<UIManager>();
                var save = saveGo.AddComponent<SaveLoadManager>();
                ui.SetModalOpen(UiModal.Loading, true);
                Assert.IsFalse(save.CanAutoSaveNow());
                Assert.IsTrue(save.CanAutoSaveNow(true));
            }
            finally
            {
                Object.DestroyImmediate(saveGo);
                Object.DestroyImmediate(uiGo);
            }
        }
    }
}
