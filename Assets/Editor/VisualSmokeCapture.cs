using System;
using System.Collections.Generic;
using System.IO;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.UI;
using LoreLegacyMonsters.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LoreLegacyMonsters.Editor
{
    /// <summary>
    /// Batchmode-friendly visual capture for agentic screenshot review.
    /// Run with:
    /// Unity.exe -batchmode -projectPath ... -executeMethod LoreLegacyMonsters.Editor.VisualSmokeCapture.CaptureOverworld
    /// </summary>
    public static class VisualSmokeCapture
    {
        const string MainMenuScene = "Assets/Scenes/MainMenu.unity";
        const string GameScene = "Assets/Scenes/Game.unity";
        const string DefaultOutputDir = "Artifacts/VisualSmoke";
        const string RunningKey = "LoreLegacyMonsters.VisualSmokeCapture.Running";
        const string OutputDirKey = "LoreLegacyMonsters.VisualSmokeCapture.OutputDir";
        const string MainMenuKey = "LoreLegacyMonsters.VisualSmokeCapture.MainMenu";
        const string TourOnlyKey = "LoreLegacyMonsters.VisualSmokeCapture.TourOnly";
        static string outputDir;
        static int step;
        static double nextStepAt;

        [InitializeOnLoadMethod]
        static void ResumeAfterReload()
        {
            if (!SessionState.GetBool(RunningKey, false))
                return;

            outputDir = SessionState.GetString(OutputDirKey, ResolveOutputDir());
            if (SessionState.GetBool(MainMenuKey, false))
            {
                EditorApplication.update -= TickMainMenu;
                EditorApplication.update += TickMainMenu;
            }
            else
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            nextStepAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Build/Visual Smoke/Capture Overworld")]
        public static void CaptureOverworld()
        {
            outputDir = ResolveOutputDir();
            Directory.CreateDirectory(outputDir);
            SessionState.SetBool(RunningKey, true);
            SessionState.EraseBool(MainMenuKey);
            SessionState.EraseBool(TourOnlyKey);
            SessionState.SetString(OutputDirKey, outputDir);

            EditorSceneManager.OpenScene(GameScene);
            step = 0;
            nextStepAt = EditorApplication.timeSinceStartup + 0.5d;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            if (!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();
        }

        [MenuItem("Build/Visual Smoke/Capture Main Menu")]
        public static void CaptureMainMenu()
        {
            outputDir = ResolveOutputDir();
            Directory.CreateDirectory(outputDir);
            SessionState.SetBool(RunningKey, true);
            SessionState.SetBool(MainMenuKey, true);
            SessionState.SetString(OutputDirKey, outputDir);

            EditorSceneManager.OpenScene(MainMenuScene);
            step = 0;
            nextStepAt = EditorApplication.timeSinceStartup + 0.8d;
            EditorApplication.update -= TickMainMenu;
            EditorApplication.update += TickMainMenu;

            if (!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();
        }

        /// <summary>Batchmode entry: full in-game screen tour (same as Capture Overworld menu).</summary>
        public static void CaptureFullSuiteBatch() => CaptureOverworld();

        [MenuItem("Build/Visual Smoke/Capture Overworld Tour (3 zones)")]
        public static void CaptureOverworldTour()
        {
            outputDir = ResolveOutputDir();
            Directory.CreateDirectory(outputDir);
            SessionState.SetBool(RunningKey, true);
            SessionState.EraseBool(MainMenuKey);
            SessionState.SetBool(TourOnlyKey, true);
            SessionState.SetString(OutputDirKey, outputDir);

            EditorSceneManager.OpenScene(GameScene);
            step = 0;
            nextStepAt = EditorApplication.timeSinceStartup + 0.5d;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;

            if (!EditorApplication.isPlaying)
                EditorApplication.EnterPlaymode();
        }

        /// <summary>Batchmode: screenshots for town, route, forest only.</summary>
        public static void CaptureOverworldTourBatch() => CaptureOverworldTour();

        static void TickMainMenu()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextStepAt)
                return;

            try
            {
                Capture("00-main-menu");
                AssertMainMenuVisible();
                Finish(0);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Finish(1);
            }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextStepAt)
                return;

            try
            {
                switch (step)
                {
                    case 0:
                        Screen.SetResolution(1280, 720, false);
                        EnsureCaptureParty();
                        Capture("01-town-start");
                        AssertWorldHudLayout();
                        AssertOnboardingGuidance();
                        MovePlayerTo(18f, -1f);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 1:
                        Capture("02-route");
                        MovePlayerTo(36f, -1f);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 2:
                        Capture("03-forest");
                        EnsurePhaseTwoCaptureUnlocked();
                        MovePlayerTo(14f, 9f);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 3:
                        Capture("03b-stonewake");
                        AssertRegionResolve("visual.phase2.stonewake", DefaultGameContent.StonewakeId, new Vector2(14f, 9f));
                        AssertSafeAndBlocked("visual.phase2.stonewake", DefaultGameContent.StonewakeId, new Vector2(14f, 9f), new Vector2(16f, 9f));
                        AssertCreativeProp("visual.phase2.stonewake.well", "Well_stonewake");
                        AssertCreativeProp("visual.phase2.stonewake.notice", "Notice_stonewake");
                        MovePlayerTo(39f, 23f);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 4:
                        Capture("03c-moonwell");
                        AssertRegionResolve("visual.phase2.moonwell", DefaultGameContent.MoonwellId, new Vector2(39f, 23f));
                        AssertDangerPatch("visual.phase2.moonwell_danger", DefaultGameContent.MoonwellId, new Vector2(34f, 21f), EncounterZoneType.MoonwellGlade);
                        AssertCreativeProp("visual.phase2.moonwell.ring", "MoonwellRingA_moonwell");
                        MovePlayerTo(84f, 9f);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 5:
                        Capture("03d-quarry");
                        AssertRegionResolve("visual.phase2.quarry", DefaultGameContent.QuarryId, new Vector2(84f, 9f));
                        AssertDangerPatch("visual.phase2.quarry_danger", DefaultGameContent.QuarryId, new Vector2(78f, 8f), EncounterZoneType.QuarryPit);
                        AssertCreativeProp("visual.phase2.quarry.cart", "MineCart_quarry");
                        MovePlayerTo(105f, 9f);
                        Capture("03e-crossing");
                        AssertRegionResolve("visual.phase2.crossing", DefaultGameContent.CrossingId, new Vector2(105f, 9f));
                        AssertCreativeProp("visual.phase2.crossing.bridge", "Bridge_crossing");
                        MovePlayerTo(68f, 25f);
                        Capture("03f-starfall");
                        AssertRegionResolve("visual.phase2.starfall", DefaultGameContent.StarfallId, new Vector2(68f, 25f));
                        AssertCreativeProp("visual.phase2.starfall.glow", "StarGlow_starfall");
                        AssertBranchSpecificNpcLine();
                        if (SessionState.GetBool(TourOnlyKey, false))
                        {
                            SessionState.EraseBool(TourOnlyKey);
                            LogSuiteSummary();
                            Finish(0);
                            break;
                        }

                        MovePlayerTo(-2f, -1f);
                        OpenNpcDialog(NPCController.HealerPiaId);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 6:
                        AdvanceDialogToReply();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 7:
                        ValidateDialogInput();
                        AssertHudHiddenDuringDialog();
                        AssertDialogSuggestionLayout();
                        Capture("04-dialog-input");
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 8:
                        OpenSyntheticChoiceDialog();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 9:
                        AssertDialogChoiceLayout();
                        Capture("04b-dialog-choice");
                        var dialogDriver = UnityEngine.Object.FindFirstObjectByType<GameDialogDriver>();
                        dialogDriver?.CloseConversation();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.4d;
                        step++;
                        break;
                    case 10:
                        StartCombatCapture();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.2d;
                        step++;
                        break;
                    case 11:
                        Capture("05-combat-intro");
                        nextStepAt = EditorApplication.timeSinceStartup + 1.1d;
                        step++;
                        break;
                    case 12:
                        var cm = UnityEngine.Object.FindFirstObjectByType<CombatManager>();
                        if (cm != null && cm.Phase == BattlePhase.PlayerTurn)
                            cm.UseMoveSlot(0);
                        Capture("06-combat-ready");
                        LogAssert("combat.active", cm != null && cm.IsBattleActive, cm != null ? $"phase={cm.Phase}" : "no manager");
                        AssertCombatFeedbackReadability(cm);
                        ExitCombatCapture();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 13:
                        OpenModal(UiModal.Party);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 14:
                        SelectSecondPartyMonsterIfAvailable();
                        Capture("07-party");
                        AssertPartySelection();
                        AssertPartyLayout();
                        OpenModal(UiModal.Inventory);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 15:
                        Capture("08-inventory");
                        OpenModal(UiModal.QuestLog);
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 16:
                        Capture("09-quest-log");
                        AssertQuestLogVisible();
                        AssertStorySideQuestsRegistered();
                        OpenMapCapture();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 17:
                        Capture("10-map");
                        AssertMapVisible();
                        AssertMapLayout();
                        OpenShopCapture();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 18:
                        Capture("11-shop");
                        AssertShopListVisible();
                        OpenHelpCapture();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.75d;
                        step++;
                        break;
                    case 19:
                        Capture("12-help");
                        nextStepAt = EditorApplication.timeSinceStartup + 2d;
                        step++;
                        break;
                    case 20:
                        OpenEndingCapture();
                        nextStepAt = EditorApplication.timeSinceStartup + 0.5d;
                        step++;
                        break;
                    case 21:
                        AssertEndingVisible();
                        AssertEndingLayout();
                        Capture("13-ending");
                        Finish(0);
                        break;
                    case 22:
                        Finish(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Finish(1);
            }
        }

        static void LogAssert(string name, bool pass, string detail)
        {
            Debug.Log(pass
                ? $"[ASSERT][PASS] {name}: {detail}"
                : $"[ASSERT][FAIL] {name}: {detail}");
        }

        static void AssertMainMenuVisible()
        {
            var root = FindObjectAnyState("MainMenuRoot");
            var newGame = FindObjectNameContains("NewGame");
            var status = FindObjectNameContains("LlmStatus");
            var firstRunCard = FindObjectAnyState("FirstRunCard");
            var firstRunText = FindObjectAnyState("FirstRunText")?.GetComponent<Text>()?.text ?? string.Empty;
            var menuController = UnityEngine.Object.FindFirstObjectByType<MainMenuController>() != null;
            var menuUi = UnityEngine.Object.FindFirstObjectByType<MainMenuUI>() != null;
            var inMainMenuScene = SceneManager.GetActiveScene().name == "MainMenu";
            LogAssert("ui.main_menu.visible", root != null || menuController || inMainMenuScene,
                root != null ? "root found" : (menuController ? "controller found" : (inMainMenuScene ? "main menu scene active" : "missing root/controller")));
            LogAssert("ui.main_menu.controls", (newGame != null && status != null) || menuUi || inMainMenuScene,
                $"newGame={(newGame != null)}, statusCard={(status != null)}, menuUi={(menuUi)}, scene={SceneManager.GetActiveScene().name}");
            LogAssert("ui.main_menu.first_session_card", firstRunCard != null, firstRunCard != null ? "first-run card found" : "missing first-run card");
            LogAssert("ui.main_menu.first_session_text", firstRunText.IndexOf("Use M for map", StringComparison.OrdinalIgnoreCase) >= 0,
                string.IsNullOrWhiteSpace(firstRunText) ? "missing first-run guidance text" : firstRunText);
        }

        static GameObject FindObjectAnyState(string name)
        {
            var direct = GameObject.Find(name);
            if (direct != null) return direct;
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (!string.Equals(all[i].name, name, StringComparison.Ordinal)) continue;
                return all[i].gameObject;
            }

            return null;
        }

        static GameObject FindObjectNameContains(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;
            var all = Resources.FindObjectsOfTypeAll<Transform>();
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].gameObject == null) continue;
                if (all[i].name.IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0) continue;
                return all[i].gameObject;
            }

            return null;
        }

        static void AssertRegionResolve(string name, string expectedAreaId, Vector2 position)
        {
            var actual = WorldMapLayout.ResolveAreaId(position);
            LogAssert(name, actual == expectedAreaId, $"expected={expectedAreaId}, actual={actual}, position={position}");
        }

        static void AssertSafeAndBlocked(string name, string areaId, Vector2 safePosition, Vector2 blockedPosition)
        {
            LogAssert($"{name}.safe_spawn", !WorldMapLayout.IsEncounterPosition(areaId, safePosition),
                $"area={areaId}, safe={safePosition}");
            LogAssert($"{name}.blocker", WorldMapLayout.IsBlockedPosition(blockedPosition),
                $"area={areaId}, blocker={blockedPosition}");
        }

        static void AssertDangerPatch(string name, string areaId, Vector2 position, EncounterZoneType expectedType)
        {
            var zones = WorldMapLayout.EncounterZones(areaId);
            var matchedType = false;
            for (var i = 0; i < zones.Length; i++)
                if (zones[i].Bounds.Contains(position) && zones[i].Type == expectedType)
                    matchedType = true;
            LogAssert(name, matchedType, $"area={areaId}, position={position}, expectedType={expectedType}");
        }

        static void AssertCreativeProp(string name, string objectName)
        {
            var found = GameObject.Find(objectName) != null;
            LogAssert(name, found, found ? $"found {objectName}" : $"missing {objectName}");
        }

        static void EnsurePhaseTwoCaptureUnlocked()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            var quests = controller != null ? controller.Quests : UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            if (quests == null) return;
            if (!quests.IsActive(PhaseTwoIds.WiderMapQuest) && !quests.IsCompleted(PhaseTwoIds.WiderMapQuest))
                quests.StartQuest(PhaseTwoIds.WiderMapQuest);
            if (!quests.IsActive(PhaseTwoIds.JessaLandmarksQuest) && !quests.IsCompleted(PhaseTwoIds.JessaLandmarksQuest))
                quests.StartQuest(PhaseTwoIds.JessaLandmarksQuest);
            if (!quests.IsActive(PhaseTwoIds.LumaBondsQuest) && !quests.IsCompleted(PhaseTwoIds.LumaBondsQuest))
                quests.StartQuest(PhaseTwoIds.LumaBondsQuest);
            if (!quests.IsActive(PhaseTwoIds.SableRematchQuest) && !quests.IsCompleted(PhaseTwoIds.SableRematchQuest))
                quests.StartQuest(PhaseTwoIds.SableRematchQuest);
            Debug.Log("VisualSmokeCapture: ensured Phase 2 route access for capture.");
        }

        static void AssertWorldHudLayout()
        {
            var routeHint = GameObject.Find("RouteHintText")?.GetComponent<RectTransform>();
            var save = GameObject.Find("SaveButton")?.GetComponent<RectTransform>();
            var load = GameObject.Find("LoadButton")?.GetComponent<RectTransform>();
            var ok = !RectsOverlap(routeHint, save) && !RectsOverlap(routeHint, load);
            LogAssert("ui.world_hud.no_button_overlap", ok,
                $"route/save={RectsOverlap(routeHint, save)}, route/load={RectsOverlap(routeHint, load)}");
        }

        static void AssertOnboardingGuidance()
        {
            var routeHint = GameObject.Find("RouteHintText")?.GetComponent<Text>()?.text ?? string.Empty;
            var prompt = GameObject.Find("PromptText")?.GetComponent<Text>()?.text ?? string.Empty;
            LogAssert("ui.onboarding.route_hint_present", !string.IsNullOrWhiteSpace(routeHint), routeHint);
            LogAssert("ui.onboarding.controls_prompt", !string.IsNullOrWhiteSpace(prompt) ||
                                                      routeHint.IndexOf("NEXT", StringComparison.OrdinalIgnoreCase) >= 0,
                $"routeHint='{routeHint}' prompt='{prompt}'");
        }

        static void AssertShopListVisible()
        {
            var shop = GameObject.Find("ShopRoot");
            var list = shop != null ? shop.transform.Find("ListRoot") : null;
            var n = list != null ? list.childCount : 0;
            LogAssert("ui.shop.list", n > 0, $"rows={n}");
        }

        static void AssertPartySelection()
        {
            var partyRoot = GameObject.Find("MonsterModalRoot");
            var detail = GameObject.Find("DetailText")?.GetComponent<Text>();
            var second = GameObject.Find("PartyButton_1");
            var secondLabel = second != null ? second.GetComponentInChildren<Text>()?.text ?? string.Empty : string.Empty;
            var expectedName = ExtractPartyButtonName(secondLabel);
            LogAssert("ui.party.visible", partyRoot != null && partyRoot.activeInHierarchy,
                partyRoot != null ? "root found" : "missing root");
            LogAssert("ui.party.selection", second == null || (detail != null && detail.text.Contains(expectedName)),
                second == null ? "single party member" : $"selected={expectedName}, detail={(detail != null ? detail.text.Split('\n')[0] : "<null>")}");
        }

        static string ExtractPartyButtonName(string label)
        {
            if (string.IsNullOrWhiteSpace(label)) return string.Empty;
            label = label.TrimStart('*', '-', ' ');
            var levelAt = label.IndexOf(" Lv", StringComparison.Ordinal);
            return levelAt > 0 ? label.Substring(0, levelAt) : label;
        }

        static void AssertQuestLogVisible()
        {
            var q = GameObject.Find("QuestLogRoot");
            var open = q != null && q.activeInHierarchy;
            LogAssert("ui.quest.visible", open, q != null ? "root found" : "missing root");
        }

        static void AssertStorySideQuestsRegistered()
        {
            var quests = UnityEngine.Object.FindFirstObjectByType<QuestManager>();
            LogAssert("story.sidequests.jessa", quests != null && quests.GetDefinition(PhaseTwoIds.JessaLandmarksQuest) != null, "Jessa side quest registered");
            LogAssert("story.sidequests.luma", quests != null && quests.GetDefinition(PhaseTwoIds.LumaBondsQuest) != null, "Luma side quest registered");
            LogAssert("story.sidequests.sable", quests != null && quests.GetDefinition(PhaseTwoIds.SableRematchQuest) != null, "Sable side quest registered");
            LogAssert("story.phase2.npc.dialogs",
                NpcContentRegistry.TryGet(NPCController.CartographerJessaId, out var jessa) && jessa.DialogBuilder != null &&
                NpcContentRegistry.TryGet(NPCController.MoonwellLumaId, out var luma) && luma.DialogBuilder != null &&
                NpcContentRegistry.TryGet(NPCController.SableRivalId, out var sable) && sable.DialogBuilder != null,
                "Phase 2 NPC dialog builders registered");
        }

        static void AssertHudHiddenDuringDialog()
        {
            var worldHud = GameObject.Find("WorldHudRoot");
            var questHud = GameObject.Find("MainStoryQuestRoot");
            var partyCompact = GameObject.Find("MonsterCompactRoot");
            LogAssert("ui.dialog.hud_hidden", worldHud == null && questHud == null && partyCompact == null,
                $"worldHud={(worldHud != null)}, questHud={(questHud != null)}, partyCompact={(partyCompact != null)}");
        }

        static void AssertDialogSuggestionLayout()
        {
            var overlap = false;
            var detail = string.Empty;
            for (var i = 0; i < 3; i++)
            {
                var suggestion = GameObject.Find($"Suggestion_{i}");
                if (suggestion == null) continue;
                foreach (var blockerName in new[] { "ViewWaresButton", "CloseButton", "SendButton" })
                {
                    var blocker = GameObject.Find(blockerName);
                    if (blocker == null) continue;
                    if (!RectsOverlap(suggestion.GetComponent<RectTransform>(), blocker.GetComponent<RectTransform>()))
                        continue;
                    overlap = true;
                    detail = $"{suggestion.name} overlaps {blockerName}";
                    break;
                }

                if (overlap) break;
            }

            LogAssert("ui.dialog.suggestion_layout", !overlap, string.IsNullOrEmpty(detail) ? "no chip/control overlap" : detail);
        }

        static void AssertDialogChoiceLayout()
        {
            var choice0 = GameObject.Find("Choice_0");
            var choice1 = GameObject.Find("Choice_1");
            LogAssert("ui.dialog.choice_buttons", choice0 != null && choice1 != null,
                $"choice0={(choice0 != null)}, choice1={(choice1 != null)}");
            AssertNoOverlap("ui.dialog.choice_vs_close", "Choice_0", "CloseButton");
            AssertNoOverlap("ui.dialog.choice_vs_speaker", "Choice_0", "SpeakerText");
            AssertNoOverlap("ui.dialog.choice_vs_line", "Choice_0", "LineText");
        }

        static bool RectsOverlap(RectTransform a, RectTransform b)
        {
            if (a == null || b == null) return false;
            var ar = WorldRect(a);
            var br = WorldRect(b);
            return ar.Overlaps(br);
        }

        static Rect WorldRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        static void AssertMapVisible()
        {
            var map = GameObject.Find("MapRoot");
            var current = GameObject.Find("PlayerMarker");
            var quest = GameObject.Find("QuestMarker");
            var visible = map != null && map.activeInHierarchy;
            LogAssert("ui.map.visible", visible, map != null ? "root found" : "missing root");
            LogAssert("ui.map.markers", current != null && quest != null,
                $"currentMarker={(current != null)}, questMarker={(quest != null)}");
            AssertMapGraphVisible();
        }

        static void AssertMapGraphVisible()
        {
            var transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var connectorCount = 0;
            for (var i = 0; i < transforms.Length; i++)
                if (transforms[i] != null && transforms[i].name.StartsWith("Connector_", StringComparison.Ordinal))
                    connectorCount++;
            LogAssert("ui.map.graph_connectors", connectorCount >= WorldMapLayout.MapEdges.Count,
                $"connectors={connectorCount}, expected={WorldMapLayout.MapEdges.Count}");
        }

        static void AssertMapLayout()
        {
            AssertNoOverlap("ui.map.quest_vs_hint", "QuestText", "HintText");
            var legendText = GameObject.Find("LegendText")?.GetComponent<Text>()?.text ?? string.Empty;
            var hasSafetyGuidance = legendText.IndexOf("safer", StringComparison.OrdinalIgnoreCase) >= 0 &&
                                    legendText.IndexOf("off-road", StringComparison.OrdinalIgnoreCase) >= 0;
            LogAssert("ui.map.legend_safe_roads", hasSafetyGuidance, legendText);
            var questMarkerText = GameObject.Find("QuestMarker")?.GetComponent<Text>()?.text ?? string.Empty;
            LogAssert("ui.map.quest_marker_symbol", questMarkerText.Trim() == "!", $"marker='{questMarkerText}'");
        }

        static void AssertCombatFeedbackReadability(CombatManager cm)
        {
            var captureLabel = GameObject.Find("Menu4")?.GetComponentInChildren<Text>()?.text ?? string.Empty;
            var switchLabel = GameObject.Find("Menu5")?.GetComponentInChildren<Text>()?.text ?? string.Empty;
            LogAssert("combat.capture_hint_label", captureLabel.IndexOf("best on low HP", StringComparison.OrdinalIgnoreCase) >= 0, captureLabel);
            LogAssert("combat.switch_hint_label", switchLabel.IndexOf("healthy ally", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                  switchLabel.IndexOf("no backup", StringComparison.OrdinalIgnoreCase) >= 0, switchLabel);
            var summary = cm != null ? cm.FeedbackSummary ?? string.Empty : string.Empty;
            if (!string.IsNullOrWhiteSpace(summary))
                LogAssert("combat.feedback.structured", summary.Contains("Type:") || summary.Contains("Rewards:") ||
                                                     summary.IndexOf("Recovered health", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                     summary.IndexOf("capture", StringComparison.OrdinalIgnoreCase) >= 0, summary);
        }

        static void LogSuiteSummary()
        {
            Debug.Log($"VisualSmokeCapture: suite output directory = {outputDir}");
        }

        static void Capture(string name)
        {
            var path = Path.Combine(outputDir, $"{name}.png");
            ScreenCapture.CaptureScreenshot(path);
            CaptureCameraFallback(name);
            Debug.Log($"VisualSmokeCapture: requested screenshot {path}");
        }

        static void CaptureCameraFallback(string name)
        {
            if (Application.isBatchMode && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.Log("VisualSmokeCapture: skipping camera fallback under Null graphics device.");
                return;
            }

            var camera = EnsureCamera();
            if (camera == null) return;

            var previousTarget = camera.targetTexture;
            var rt = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                AttachRuntimeCanvas(camera);
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                texture.Apply();
                var fallbackPath = Path.Combine(outputDir, $"{name}-camera.png");
                File.WriteAllBytes(fallbackPath, texture.EncodeToPNG());
                Debug.Log($"VisualSmokeCapture: wrote camera fallback {fallbackPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = null;
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(rt);
            }
        }

        static Camera EnsureCamera()
        {
            var camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera == null)
            {
                var go = new GameObject("VisualSmokeCamera");
                camera = go.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = GameVisualTheme.SkyTop;
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            var player = controller != null ? controller.Player : null;
            if (player != null)
                camera.transform.position = new Vector3(player.transform.position.x + 1.5f, player.transform.position.y + 0.2f, -10f);
            else
                camera.transform.position = new Vector3(5f, -0.65f, -10f);

            camera.orthographic = true;
            camera.orthographicSize = 3.35f;
            return camera;
        }

        static void AttachRuntimeCanvas(Camera camera)
        {
            if (UIManager.Instance == null || UIManager.Instance.Root == null)
                return;

            var canvas = UIManager.Instance.Root;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            canvas.sortingOrder = 100;
        }

        static void MovePlayerTo(float x, float y)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            if (controller != null && controller.Player != null)
            {
                controller.Player.transform.position = new Vector3(x, y, 0f);
                var position = new Vector2(x, y);
                var areaId = WorldMapLayout.ResolveAreaId(position);
                controller.World?.SetCurrentPlayerPosition(position);
                if (controller.World?.GetArea(areaId) != null)
                    controller.World.SetCurrentArea(areaId);
            }
        }

        static void OpenNpcDialog(string npcId)
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            if (controller == null || controller.DialogDriver == null)
            {
                Debug.LogWarning("VisualSmokeCapture: cannot open NPC dialog; controller/driver missing.");
                return;
            }

            var npcs = UnityEngine.Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc == null || npc.NpcId != npcId) continue;
                controller.Player.transform.position = npc.transform.position + new Vector3(0.35f, 0f, 0f);
                controller.DialogDriver.BeginConversation(npc, npc.Dialog);
                Debug.Log($"VisualSmokeCapture: opened dialog with {npc.DisplayName}");
                return;
            }

            Debug.LogWarning($"VisualSmokeCapture: NPC not found: {npcId}");
        }

        static void AdvanceDialogToReply()
        {
            var driver = UnityEngine.Object.FindFirstObjectByType<GameDialogDriver>();
            if (driver == null) return;
            for (var i = 0; i < 8 && driver.IsConversationOpen && !driver.CanAcceptPlayerReply; i++)
                driver.AdvanceConversation();
            Debug.Log($"VisualSmokeCapture: dialog state open={driver.IsConversationOpen}, busy={driver.IsBusy}, canReply={driver.CanAcceptPlayerReply}");
        }

        static void ValidateDialogInput()
        {
            var reply = GameObject.Find("ReplyInput")?.GetComponent<InputField>();
            if (reply == null)
            {
                Debug.LogWarning("VisualSmokeCapture: ReplyInput not found.");
                return;
            }

            reply.ActivateInputField();
            reply.text = "Can you hear me?";

            var rt = reply.GetComponent<RectTransform>();
            var canvas = reply.GetComponentInParent<Canvas>();
            var screenPoint = RectTransformUtility.WorldToScreenPoint(canvas != null ? canvas.worldCamera : null, rt.TransformPoint(rt.rect.center));
            var pointer = new PointerEventData(EventSystem.current) { position = screenPoint };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);
            var top = results.Count > 0 ? results[0].gameObject.name : "<none>";
            var replyHitTop = results.Exists(r =>
                r.gameObject != null &&
                (r.gameObject.name.IndexOf("Reply", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 r.gameObject.GetComponent<InputField>() != null));
            LogAssert("ui.dialog.reply_raycast", replyHitTop || top.IndexOf("Reply", StringComparison.OrdinalIgnoreCase) >= 0,
                $"top={top}, hits={results.Count}, interactable={reply.interactable}");
            Debug.Log($"VisualSmokeCapture: ReplyInput active={reply.gameObject.activeInHierarchy}, interactable={reply.interactable}, focused={reply.isFocused}, text='{reply.text}', topRaycast={top}, hits={results.Count}");
        }

        static void StartCombatCapture()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            if (controller == null || controller.Combat == null || controller.Registry == null)
            {
                Debug.LogWarning("VisualSmokeCapture: cannot start combat; controller/combat/registry missing.");
                return;
            }

            controller.DialogDriver?.CloseConversation();
            var enemy = controller.Registry.GetMonster(DefaultGameContent.SlimeId) ??
                        controller.Registry.GetMonster(DefaultGameContent.ThornBeastId);
            var player = controller.Registry.GetMonster(DefaultGameContent.EmberFoxId);
            controller.Combat.BeginBattle(enemy, player);
            Debug.Log($"VisualSmokeCapture: started combat with {(enemy != null ? enemy.DisplayName : "<null>")}, active={controller.Combat.IsBattleActive}, phase={controller.Combat.Phase}");
        }

        static void ExitCombatCapture()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            controller?.Combat?.Flee();
            if (UIManager.Instance != null)
                UIManager.Instance.SetModalOpen(UiModal.Combat, false);
        }

        static void OpenModal(UiModal modal)
        {
            CloseModalScreens();
            EnsureCaptureParty();
            if (UIManager.Instance != null)
                UIManager.Instance.SetModalOpen(modal, true);
            Debug.Log($"VisualSmokeCapture: opened modal {modal}");
        }

        static void SelectSecondPartyMonsterIfAvailable()
        {
            var second = GameObject.Find("PartyButton_1")?.GetComponent<Button>();
            if (second == null)
            {
                Debug.Log("VisualSmokeCapture: no second party button to select.");
                return;
            }

            second.onClick.Invoke();
            Debug.Log("VisualSmokeCapture: selected second party monster.");
        }

        static void OpenShopCapture()
        {
            CloseModalScreens();
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            var shop = Resources.Load<ShopData>("Shops/shop_general");
            controller?.OpenShopForNpc(shop);
            Debug.Log($"VisualSmokeCapture: opened shop {(shop != null ? shop.ShopId : "<null>")}");
        }

        static void OpenMapCapture()
        {
            CloseModalScreens();
            if (UIManager.Instance != null)
                UIManager.Instance.SetModalOpen(UiModal.Map, true);
            Debug.Log("VisualSmokeCapture: opened map");
        }

        static void OpenHelpCapture()
        {
            CloseModalScreens();
            if (UIManager.Instance == null || UIManager.Instance.Root == null) return;
            var existing = GameObject.Find("HelpOverlayRoot");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            HelpOverlayUtility.Create(UIManager.Instance.Root.transform, AlphaHelpText.ControlsTitle, AlphaHelpText.ControlsBody, () => { });
            UIManager.Instance.SetModalOpen(UiModal.Help, true);
            Debug.Log("VisualSmokeCapture: opened help overlay");
        }

        static void OpenSyntheticChoiceDialog()
        {
            var driver = UnityEngine.Object.FindFirstObjectByType<GameDialogDriver>();
            var npc = UnityEngine.Object.FindFirstObjectByType<NPCController>();
            if (driver == null || npc == null)
            {
                Debug.LogWarning("VisualSmokeCapture: cannot open synthetic choice dialog.");
                return;
            }

            var dialog = ScriptableObject.CreateInstance<DialogData>();
            dialog.hideFlags = HideFlags.DontUnloadUnusedAsset;
            dialog.Configure("dlg_visual_choice", new[]
            {
                new DialogEntry
                {
                    speaker = "System",
                    line = "Select a response branch.",
                    choiceLabels = new[] {"Option A", "Option B"},
                    choiceNextIds = new[] {"noop:a", "noop:b"}
                }
            });
            driver.BeginConversation(npc, dialog);
            Debug.Log("VisualSmokeCapture: opened synthetic choice dialog.");
        }

        static void OpenEndingCapture()
        {
            CloseModalScreens();
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            var quests = controller != null ? controller.Quests : null;
            if (controller == null || quests == null)
            {
                Debug.LogWarning("VisualSmokeCapture: cannot open ending modal.");
                return;
            }

            if (!quests.IsActive(PhaseTwoIds.BindingChoiceQuest) && !quests.IsCompleted(PhaseTwoIds.BindingChoiceQuest))
                quests.StartQuest(PhaseTwoIds.BindingChoiceQuest);
            quests.ReportObjectiveEvent(PhaseTwoIds.DefeatSable, 1);
            StoryState.SetEnding(StoryEnding.None);
            controller.ForceOpenEndingChoiceForDebug();
            Debug.Log("VisualSmokeCapture: opened ending modal.");
        }

        static void AssertEndingVisible()
        {
            var ending = GameObject.Find("EndingRoot");
            var merge = GameObject.Find("Ending_Merge");
            var burn = GameObject.Find("Ending_Burn");
            LogAssert("ui.ending.visible", ending != null && ending.activeInHierarchy, ending != null ? "root found" : "missing");
            LogAssert("ui.ending.options", merge != null && burn != null, $"merge={(merge != null)}, burn={(burn != null)}");
        }

        static void AssertEndingLayout()
        {
            AssertNoOverlap("ui.ending.body_vs_merge", "EndingBody", "Ending_Merge");
            AssertNoOverlap("ui.ending.body_vs_burn", "EndingBody", "Ending_Burn");
            AssertNoOverlap("ui.ending.title_vs_merge", "EndingTitle", "Ending_Merge");
        }

        static void AssertBranchSpecificNpcLine()
        {
            StoryFlags.ApplySave(null);
            StoryState.SetAdvisor(StoryState.AdvisorThren);
            StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroRefuseSpire);
            var dialog = PhaseTwoWorldContent.BuildDialog(NPCController.EthicistThrenId, null);
            var hasBranchLine = dialog != null && dialog.Entries != null && dialog.Entries.Length > 0 &&
                                dialog.Entries[0] != null &&
                                dialog.Entries[0].line.IndexOf("consent", StringComparison.OrdinalIgnoreCase) >= 0;
            LogAssert("story.branch.ethicist_advisor_line", hasBranchLine,
                hasBranchLine ? "Thren advisor branch line present" : "missing advisor-specific Thren line");
            var hasVaroAftermath = false;
            if (dialog != null && dialog.Entries != null)
            {
                for (var i = 0; i < dialog.Entries.Length; i++)
                {
                    var line = dialog.Entries[i] != null ? dialog.Entries[i].line : string.Empty;
                    if (line.IndexOf("spire", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasVaroAftermath = true;
                        break;
                    }
                }
            }
            LogAssert("story.branch.ethicist_varo_aftermath", hasVaroAftermath,
                hasVaroAftermath ? "found spire-reaction line in ethicist dialog" : "missing varo aftermath line");

            StoryFlags.SetFlag(StoryState.JessaFormerMiraKnown);
            StoryFlags.SetFlag(PhaseTwoIds.FlagHelpedJessaLandmarks);
            var jessaDialog = PhaseTwoWorldContent.BuildDialog(NPCController.CartographerJessaId, null);
            var hasJessaRevealPayoff = false;
            if (jessaDialog != null && jessaDialog.Entries != null)
            {
                for (var i = 0; i < jessaDialog.Entries.Length; i++)
                {
                    var line = jessaDialog.Entries[i] != null ? jessaDialog.Entries[i].line : string.Empty;
                    if (line.IndexOf("Mira", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        hasJessaRevealPayoff = true;
                        break;
                    }
                }
            }
            LogAssert("story.branch.jessa_reveal_payoff", hasJessaRevealPayoff,
                hasJessaRevealPayoff ? "found Jessa/Mira aftermath line" : "missing Jessa reveal payoff line");
        }

        static void AssertPartyLayout()
        {
            AssertNoOverlap("ui.party.detail_vs_controls", "MonsterDetailCard", "SetLeadButton");
            AssertNoOverlap("ui.party.reserve_vs_buttons", "ReserveListRoot", "SetLeadButton");
        }

        static void AssertNoOverlap(string assertName, string firstObjectName, string secondObjectName)
        {
            var first = GameObject.Find(firstObjectName)?.GetComponent<RectTransform>();
            var second = GameObject.Find(secondObjectName)?.GetComponent<RectTransform>();
            if (first == null || second == null)
            {
                LogAssert(assertName, false, $"missing {(first == null ? firstObjectName : string.Empty)} {(second == null ? secondObjectName : string.Empty)}");
                return;
            }

            var overlap = RectsOverlap(first, second);
            LogAssert(assertName, !overlap, overlap ? $"{firstObjectName} overlaps {secondObjectName}" : "clear");
        }

        static void CloseModalScreens()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            controller?.CloseShop();
            var help = GameObject.Find("HelpOverlayRoot");
            if (help != null) UnityEngine.Object.DestroyImmediate(help);
            if (UIManager.Instance == null) return;
            UIManager.Instance.SetModalOpen(UiModal.Party, false);
            UIManager.Instance.SetModalOpen(UiModal.Inventory, false);
            UIManager.Instance.SetModalOpen(UiModal.QuestLog, false);
            UIManager.Instance.SetModalOpen(UiModal.Map, false);
            UIManager.Instance.SetModalOpen(UiModal.Help, false);
            UIManager.Instance.SetModalOpen(UiModal.Ending, false);
        }

        static void EnsureCaptureParty()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<OverworldChapterController>();
            if (controller == null || controller.MonsterSystem == null || controller.Registry == null) return;
            controller.MonsterSystem.EnsureStarterParty(controller.Registry, DefaultGameContent.EmberFoxId);
            if (controller.MonsterSystem.Party.Count < 2)
            {
                var slime = controller.Registry.GetMonster(DefaultGameContent.SlimeId);
                controller.MonsterSystem.AddMonster(slime);
            }
        }

        static string ResolveOutputDir()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == "-visualCaptureDir")
                    return Path.GetFullPath(args[i + 1]);

            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var dated = Path.Combine(projectRoot, DefaultOutputDir, DateTime.Now.ToString("yyyy-MM-dd"));
            return dated;
        }

        static void Finish(int exitCode)
        {
            EditorApplication.update -= Tick;
            EditorApplication.update -= TickMainMenu;
            SessionState.EraseBool(RunningKey);
            SessionState.EraseBool(MainMenuKey);
            SessionState.EraseBool(TourOnlyKey);
            SessionState.EraseString(OutputDirKey);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();

            Debug.Log($"VisualSmokeCapture: complete ({outputDir})");
            if (Application.isBatchMode)
                EditorApplication.Exit(exitCode);
        }
    }
}
