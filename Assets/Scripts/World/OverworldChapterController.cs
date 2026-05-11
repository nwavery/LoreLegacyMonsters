using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Audio;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Platform.Steam;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.SaveLoad;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.SceneManagement;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.UI;
using LoreLegacyMonsters.World.Visuals;
using UnityEngine.InputSystem;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.World
{
    /// <summary>
    /// Boots a complete first chapter at runtime: overworld zones, NPC interactions, encounters, shop/heal, and quest flow.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class OverworldChapterController : MonoBehaviour
    {
        [SerializeField] GameDialogDriver dialogDriver;
        [SerializeField] PlayerController player;
        [SerializeField] float encounterDistanceStep = 7f;

        AssetRegistryManager registry;
        MonsterSystem monsterSystem;
        InventorySystem inventory;
        QuestManager questManager;
        WorldManager worldManager;
        AchievementSystem achievements;
        WeatherSystem weather;
        ShopManager shopManager;
        SaveLoadManager saveLoad;
        CombatManager combat;
        EncounterService encounters;
        GameManager gameManager;
        Camera mainCamera;

        NPCController elder;
        NPCController scout;
        NPCController merchant;
        NPCController healer;
        NPCController boss;
        NPCController archivist;
        NPCController rival;
        NPCController warden;
        NPCController mentor;
        NPCController stormBoss;
        NPCController collector;
        NPCController rumorKeeper;
        NPCController cartographer;
        NPCController quartermaster;
        NPCController runner;
        NPCController foreman;
        NPCController ethicist;
        NPCController moonwellKeeper;
        NPCController sable;
        NPCController tailor;

        readonly Dictionary<string, DialogData> dialogCache = new Dictionary<string, DialogData>();
        readonly OverworldEncounterPacer encounterPacer = new OverworldEncounterPacer();
        string promptText;
        string statusText;
        float statusUntil;
        bool shopOpen;
        bool pendingBossBattle;
        bool pendingShopOpenAfterDialog;
        string pendingBossMonsterId;
        string pendingBossObjectiveId;
        bool dialogChoiceHooked;
        bool endingChoiceOpen;
        StoryEnding suggestedEnding;
        string endingSuggestionText = string.Empty;

        public string PromptText => promptText;
        public string StatusText => Time.time <= statusUntil ? statusText : string.Empty;
        public bool ShopOpen => shopOpen;
        public CombatManager Combat => combat;
        public InventorySystem Inventory => inventory;
        public ShopManager Shop => shopManager;
        public MonsterSystem MonsterSystem => monsterSystem;
        public AssetRegistryManager Registry => registry;
        public QuestManager Quests => questManager;
        public WorldManager World => worldManager;
        public WeatherSystem Weather => weather;
        public PlayerController Player => player;
        public GameDialogDriver DialogDriver => dialogDriver;
        public string AreaName => worldManager != null ? worldManager.GetCurrentArea()?.DisplayName ?? worldManager.CurrentAreaId : "Unknown";
        public string QuestSummary => questManager != null ? questManager.GetPrimaryQuestTrackerText() : "No quest manager";
        public string QuestTitle => questManager != null ? questManager.GetPrimaryQuestTitle() : "No active quest";
        public string RouteSummary => BuildRouteSummary();
        /// <summary>Short window around the current area for the HUD (avoids a full east-line wrap).</summary>
        public string RouteSummaryCompact => BuildRouteSummaryCompact();
        public string RouteHint => BuildRouteHint();
        public string PartySummary => monsterSystem != null ? monsterSystem.GetPartySummary(registry) : "No party";
        public bool EndingChoiceOpen => endingChoiceOpen;
        public StoryEnding SuggestedEnding => suggestedEnding;
        public string EndingSuggestionText => endingSuggestionText;

        void Awake()
        {
            EnsureRuntimeWorld();
            EnsureHud();
        }

        void Start()
        {
            LlmRuntimeSupervisor.EnsureStarted();
            GameEvents.MonsterLeveled += OnMonsterLeveled;
            GameEvents.MonsterEvolved += OnMonsterEvolved;
            EnsureNewGameState();
            EnsureGearShopRuntime();
            ConfigureNpcs();
            SyncPlayerToSavedArea();
            SyncCampaignState();
            EnsureMinimalWorldVisuals();
            SubscribePlayerGearVisuals();
            SetStatus("Explore Hollowfen. Speak to townsfolk and head east for your first hunt.");
            AudioManager.EnsureExists().PlayMusicForArea(worldManager != null ? worldManager.CurrentAreaId : DefaultGameContent.TownId);
            StartCoroutine(BootLlmProbe());
        }

        IEnumerator BootLlmProbe()
        {
            var ok = false;
            string lastMsg = null;
            NpcLlmSettings settings = null;
            LlmRuntimeStatus.SetBootProbeInProgress(true);
            try
            {
                yield return new WaitForSecondsRealtime(0.35f);

                var bundled = LlmRuntimeSupervisor.IsBundledRuntimeEnabled();

                var tcpCap = Time.unscaledTime +
                             (bundled ? LlmBootProbePolicy.BundledTcpWaitCapSeconds : 12f);
                while (bundled && Time.unscaledTime < tcpCap && !LlmRuntimeSupervisor.IsBundledListenerReachable())
                {
                    LlmRuntimeSupervisor.EnsureStarted();
                    yield return new WaitForSecondsRealtime(0.4f);
                }

                if (bundled)
                    yield return BundledOllamaModelProvisioner.EnsureBundledModelRegistered();

                var deadline = Time.unscaledTime + LlmBootProbePolicy.DeadlineSeconds(bundled);
                var retryDelay = 0.25f;
                var attemptIndex = 0;

                while (Time.unscaledTime < deadline && !ok)
                {
                    settings = NpcLlmSettings.ResolveForDriver(null);
                    LlmRuntimeSupervisor.EnsureStarted();
                    var attemptOk = false;
                    string attemptMsg = null;
                    var httpTimeout = LlmBootProbePolicy.ProbeHttpTimeoutSeconds(bundled, attemptIndex);
                    yield return NpcLlmHealthCheck.Probe(settings, httpTimeout, (success, m) =>
                    {
                        attemptOk = success;
                        attemptMsg = m;
                    });
                    ok = attemptOk;
                    lastMsg = attemptMsg;
                    attemptIndex++;

                    if (ok)
                        break;

                    yield return new WaitForSecondsRealtime(retryDelay);
                    retryDelay = Mathf.Min(2f, retryDelay * 1.55f);
                }

                settings ??= NpcLlmSettings.ResolveForDriver(null);
            }
            finally
            {
                try
                {
                    settings ??= NpcLlmSettings.ResolveForDriver(null);
                }
                catch
                {
                    // If settings resolution fails, still clear boot state.
                }

                var modelLabel = settings != null ? settings.Model : "unknown";
                LlmRuntimeStatus.SetProbeResult(ok, ok ? $"OK — {modelLabel}" : (lastMsg ?? "failed"));
            }

            try
            {
                settings ??= NpcLlmSettings.ResolveForDriver(null);
            }
            catch
            {
                // ignore — toasts are best-effort
            }

            if (ok)
                GameEvents.RaiseToast($"Local LLM ready ({settings?.Model}).");
            else
            {
                var detail = lastMsg ?? "unknown error";
                if (detail.Length > 120)
                    detail = detail.Substring(0, 117) + "…";
                detail = detail.Replace('\n', ' ').Trim();
                GameEvents.RaiseToast(
                    $"Local LLM not ready — scripted dialog fallback. Detail: {detail}");
            }
        }

        void OnDestroy()
        {
            GameEvents.MonsterLeveled -= OnMonsterLeveled;
            GameEvents.MonsterEvolved -= OnMonsterEvolved;
            UnsubscribePlayerGearVisuals();
        }

        void OnEnable()
        {
            GameEvents.QuestCompleted += OnQuestCompletedForCampaign;
            GameEvents.RuntimeRestored += OnRuntimeRestored;
            HookDialogChoiceEvents();
        }

        void OnDisable()
        {
            GameEvents.QuestCompleted -= OnQuestCompletedForCampaign;
            GameEvents.RuntimeRestored -= OnRuntimeRestored;
            UnhookDialogChoiceEvents();
        }

        void OnRuntimeRestored()
        {
            SyncCampaignState();
            EnsurePartyAndInventoryFallback();
            RefreshPlayerGearVisuals();
        }

        void OnQuestCompletedForCampaign(string _) => SyncCampaignState();

        void SyncCampaignState()
        {
            OverworldCampaignRefresher.SyncCampaignState(questManager, achievements, msg => SetStatus(msg));
            RefreshChapterTwoNpcState();
            RefreshChapterThreeNpcState();
            RefreshPhaseTwoNpcState();
            ReplenishGearVendorIfNeeded();
        }

        void Update()
        {
            if (player == null) return;
            var kb = Keyboard.current;

            if (dialogDriver == null) dialogDriver = FindFirstObjectByType<GameDialogDriver>();
            HookDialogChoiceEvents();
            var ui = UIManager.Instance != null ? UIManager.Instance : FindFirstObjectByType<UIManager>();
            if (ui != null)
            {
                ui.SetModalOpen(UiModal.Dialog, dialogDriver != null && dialogDriver.IsConversationOpen);
                ui.SetModalOpen(UiModal.Shop, shopOpen);
                ui.SetModalOpen(UiModal.Combat, combat != null && combat.IsBattleActive);
                ui.SetModalOpen(UiModal.Ending, endingChoiceOpen);
            }

            UpdateBattleWorldVisibility();

            player.InputLocked = ui != null
                ? ui.IsBlockingWorldInput
                : (dialogDriver != null && dialogDriver.IsConversationOpen) || shopOpen || (combat != null && combat.IsBattleActive);

            OverworldInputShortcuts.Handle(kb, ref shopOpen, SaveCurrentGame, LoadCurrentGame);

            if (pendingBossBattle && dialogDriver != null && !dialogDriver.IsConversationOpen &&
                combat != null && !combat.IsBattleActive)
            {
                pendingBossBattle = false;
                var bossMonster = registry != null ? registry.GetMonster(string.IsNullOrWhiteSpace(pendingBossMonsterId)
                    ? DefaultGameContent.ThornBeastId
                    : pendingBossMonsterId) : null;
                combat.BeginBattle(bossMonster, null, true, pendingBossObjectiveId);
                pendingBossMonsterId = null;
                pendingBossObjectiveId = null;
                return;
            }

            if (pendingShopOpenAfterDialog && dialogDriver != null && !dialogDriver.IsConversationOpen && !shopOpen)
            {
                pendingShopOpenAfterDialog = false;
                shopOpen = true;
                SetStatus("Merchant stock is available.");
            }

            if (player.InputLocked)
            {
                OverworldCharacterVisuals.SetHighlightedNpc(null);
                promptText = shopOpen ? "Merchant window open." : string.Empty;
                return;
            }

            UpdateAreaFromPosition();
            HandleExplorationAndEncounters();
            var nearby = FindNearbyNpc();
            OverworldCharacterVisuals.SetHighlightedNpc(nearby);
            promptText = BuildPrompt(nearby);
            var gamepadInteract = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            if (nearby != null && ((kb != null && kb[GameSettings.Interact].wasPressedThisFrame) || gamepadInteract))
                InteractWithNpc(nearby);
        }

        void LateUpdate()
        {
            UpdateCameraFollow();
        }

        void EnsureRuntimeWorld()
        {
            dialogDriver ??= FindFirstObjectByType<GameDialogDriver>();

            registry = FindFirstObjectByType<AssetRegistryManager>();
            monsterSystem = FindFirstObjectByType<MonsterSystem>();
            inventory = FindFirstObjectByType<InventorySystem>();
            questManager = FindFirstObjectByType<QuestManager>();
            worldManager = FindFirstObjectByType<WorldManager>();
            achievements = FindFirstObjectByType<AchievementSystem>();
            weather = FindFirstObjectByType<WeatherSystem>();
            shopManager = FindFirstObjectByType<ShopManager>();
            saveLoad = SaveLoadManager.Instance != null ? SaveLoadManager.Instance : FindFirstObjectByType<SaveLoadManager>();
            combat = FindFirstObjectByType<CombatManager>();
            encounters = FindFirstObjectByType<EncounterService>();

            if (registry == null || monsterSystem == null || inventory == null || questManager == null ||
                worldManager == null || achievements == null || weather == null || shopManager == null ||
                saveLoad == null || combat == null || encounters == null || FindFirstObjectByType<GameManager>() == null)
            {
                var root = FindFirstObjectByType<GameManager>() != null
                    ? FindFirstObjectByType<GameManager>().gameObject
                    : new GameObject("RuntimeSystems");
                if (root.GetComponent<AssetRegistryManager>() == null) registry = root.AddComponent<AssetRegistryManager>();
                else registry = root.GetComponent<AssetRegistryManager>();
                if (root.GetComponent<MonsterSystem>() == null) monsterSystem = root.AddComponent<MonsterSystem>();
                else monsterSystem = root.GetComponent<MonsterSystem>();
                if (root.GetComponent<InventorySystem>() == null) inventory = root.AddComponent<InventorySystem>();
                else inventory = root.GetComponent<InventorySystem>();
                if (root.GetComponent<QuestManager>() == null) questManager = root.AddComponent<QuestManager>();
                else questManager = root.GetComponent<QuestManager>();
                if (root.GetComponent<WorldManager>() == null) worldManager = root.AddComponent<WorldManager>();
                else worldManager = root.GetComponent<WorldManager>();
                if (root.GetComponent<AchievementSystem>() == null) achievements = root.AddComponent<AchievementSystem>();
                else achievements = root.GetComponent<AchievementSystem>();
                if (root.GetComponent<WeatherSystem>() == null) weather = root.AddComponent<WeatherSystem>();
                else weather = root.GetComponent<WeatherSystem>();
                if (root.GetComponent<NpcMemoryService>() == null) root.AddComponent<NpcMemoryService>();
                if (root.GetComponent<ShopManager>() == null) shopManager = root.AddComponent<ShopManager>();
                else shopManager = root.GetComponent<ShopManager>();
                if (root.GetComponent<SceneLoader>() == null) root.AddComponent<SceneLoader>();
                if (root.GetComponent<GameController>() == null) root.AddComponent<GameController>();
                saveLoad = SaveLoadManager.EnsureExists();
                if (root.GetComponent<EncounterService>() == null) encounters = root.AddComponent<EncounterService>();
                else encounters = root.GetComponent<EncounterService>();

                if (root.GetComponent<GameManager>() == null)
                    gameManager = root.AddComponent<GameManager>();
                else
                    gameManager = root.GetComponent<GameManager>();

                if (combat == null)
                {
                    var combatRoot = new GameObject("RuntimeCombat");
                    var playerEntity = new GameObject("PlayerCombatant").AddComponent<CombatEntity>();
                    var enemyEntity = new GameObject("EnemyCombatant").AddComponent<CombatEntity>();
                    playerEntity.transform.SetParent(combatRoot.transform);
                    enemyEntity.transform.SetParent(combatRoot.transform);
                    combat = combatRoot.AddComponent<CombatManager>();
                    combat.ConfigureEntities(playerEntity, enemyEntity);
                }
            }

            gameManager ??= FindFirstObjectByType<GameManager>();

            if (player == null)
            {
                var playerGo = new GameObject("Player");
                playerGo.transform.position = new Vector3(2f, -1f, 0f);
                player = playerGo.AddComponent<PlayerController>();
            }

            mainCamera ??= Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        }

        void EnsureHud()
        {
            var uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager == null) uiManager = new GameObject("UIManager").AddComponent<UIManager>();

            var hud = GetComponent<GameHudUI>();
            if (hud == null) hud = gameObject.AddComponent<LoreLegacyMonsters.UI.GameHudUI>();
            hud.Bind(this);

            var worldUi = GetComponent<WorldUI>();
            if (worldUi == null) worldUi = gameObject.AddComponent<WorldUI>();
            worldUi.Bind(this);

            var trackerUi = GetComponent<MainStoryQuestUI>();
            if (trackerUi == null) trackerUi = gameObject.AddComponent<MainStoryQuestUI>();
            trackerUi.Bind(this);

            var questUi = GetComponent<QuestUI>();
            if (questUi == null) questUi = gameObject.AddComponent<QuestUI>();
            questUi.Bind(this);

            var mapUi = GetComponent<MapUI>();
            if (mapUi == null) mapUi = gameObject.AddComponent<MapUI>();
            mapUi.Bind(this);

            if (GetComponent<CombatUI>() == null) gameObject.AddComponent<CombatUI>();

            var monsterUi = GetComponent<MonsterUI>();
            if (monsterUi == null) monsterUi = gameObject.AddComponent<MonsterUI>();
            monsterUi.Bind(this);

            var inventoryUi = GetComponent<InventoryUI>();
            if (inventoryUi == null) inventoryUi = gameObject.AddComponent<InventoryUI>();
            inventoryUi.Bind(this);

            var loadoutUi = GetComponent<LoadoutUI>();
            if (loadoutUi == null) loadoutUi = gameObject.AddComponent<LoadoutUI>();
            loadoutUi.Bind(this);

            var shopUi = GetComponent<ShopUI>();
            if (shopUi == null) shopUi = gameObject.AddComponent<ShopUI>();
            shopUi.Bind(this);

            if (GetComponent<DialogUI>() == null) gameObject.AddComponent<DialogUI>();
            var endingUi = GetComponent<EndingUI>();
            if (endingUi == null) endingUi = gameObject.AddComponent<EndingUI>();
            endingUi.Bind(this);
        }

        void EnsureNewGameState()
        {
            if (saveLoad == null) return;
            if (!SaveSystem.SaveSystem.SlotExists(0) && monsterSystem != null && monsterSystem.Party.Count == 0)
                saveLoad.NewGame(player != null ? player.PlayerName : "Hero");
        }

        /// <summary>
        /// If save data left the party or consumables empty (legacy saves / edge cases), restore a playable minimum.
        /// Fresh games use <see cref="DefaultGameContent.CreateFreshSave"/>; this is a safety net after <see cref="GameEvents.RuntimeRestored"/>.
        /// </summary>
        void EnsurePartyAndInventoryFallback()
        {
            if (registry == null || monsterSystem == null || inventory == null) return;
            if (monsterSystem.Party.Count == 0)
                monsterSystem.EnsureStarterParty(registry, DefaultGameContent.EmberFoxId);
            if (inventory.Count(DefaultGameContent.PotionId) <= 0)
                inventory.AddItem(DefaultGameContent.PotionId, 3);
            if (inventory.Count(DefaultGameContent.CaptureCharmId) <= 0)
                inventory.AddItem(DefaultGameContent.CaptureCharmId, 3);
        }

        [SerializeField] ShopData runtimeGearShop;

        void EnsureGearShopRuntime()
        {
            if (runtimeGearShop != null) return;
            // Always runtime-owned so Replenish() never mutates a Resources-backed asset instance.
            runtimeGearShop = ScriptableObject.CreateInstance<ShopData>();
            runtimeGearShop.hideFlags = HideFlags.DontUnloadUnusedAsset;
        }

        void ReplenishGearVendorIfNeeded()
        {
            if (questManager == null) return;
            EnsureGearShopRuntime();
            GearVendorCatalog.Replenish(runtimeGearShop, questManager);
        }

        void SubscribePlayerGearVisuals()
        {
            var lo = gameManager != null ? gameManager.Loadout : null;
            if (lo == null) return;
            lo.LoadoutChanged -= OnLoadoutVisualsChanged;
            lo.LoadoutChanged += OnLoadoutVisualsChanged;
        }

        void UnsubscribePlayerGearVisuals()
        {
            var lo = gameManager != null ? gameManager.Loadout : null;
            if (lo == null) return;
            lo.LoadoutChanged -= OnLoadoutVisualsChanged;
        }

        void OnLoadoutVisualsChanged(GearSlot _, int __, string ___, string ____) => RefreshPlayerGearVisuals();

        void RefreshPlayerGearVisuals()
        {
            if (player == null) return;
            OverworldCharacterVisuals.ApplyPlayerGear(player.gameObject, registry, gameManager?.Loadout);
        }

        void ConfigureNpcs()
        {
            var shopGeneral = Resources.Load<ShopData>("Shops/shop_general");
            var shopHealer = Resources.Load<ShopData>("Shops/shop_healer");
            EnsureGearShopRuntime();
            ReplenishGearVendorIfNeeded();

            foreach (var def in NpcContentRegistry.All)
            {
                var shop = NpcContentRegistry.ResolveShop(def, shopGeneral, shopHealer, runtimeGearShop);
                var npc = EnsureNpc(def, shop);
                AssignNpcReference(npc);
            }
        }

        NPCController EnsureNpc(NpcContentDefinition def, ShopData shopData = null)
        {
            var pos = NpcContentRegistry.Anchor(def.NpcId, def.FallbackPosition);
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
                if (npc != null && npc.NpcId == def.NpcId)
                {
                    npc.Configure(def.NpcId, def.DisplayName, def.Role, 2.5f, def.UseLlmFlavor, def.LlmPrompt, def.IdentitySummary, true, def.SuggestedTopics);
                    npc.transform.position = pos;
                    if (shopData != null) npc.BindShop(shopData);
                    npc.BindRuntimeDialog(BuildDialogForNpc(npc));
                    return npc;
                }

            var go = new GameObject(def.GameObjectName);
            go.transform.position = pos;
            var created = go.AddComponent<NPCController>();
            created.Configure(def.NpcId, def.DisplayName, def.Role, 2.5f, def.UseLlmFlavor, def.LlmPrompt, def.IdentitySummary, true, def.SuggestedTopics);
            if (shopData != null) created.BindShop(shopData);
            created.BindRuntimeDialog(BuildDialogForNpc(created));
            return created;
        }

        void AssignNpcReference(NPCController npc)
        {
            if (npc == null) return;
            switch (npc.NpcId)
            {
                case NPCController.ElderMiraId: elder = npc; break;
                case NPCController.ScoutRinId: scout = npc; break;
                case NPCController.MerchantTomaId: merchant = npc; break;
                case NPCController.HealerPiaId: healer = npc; break;
                case NPCController.BossIonaId: boss = npc; break;
                case NPCController.ArchivistSelId: archivist = npc; break;
                case NPCController.RivalCorinId: rival = npc; break;
                case NPCController.WardenNerisId: warden = npc; break;
                case NPCController.MentorCaelId: mentor = npc; break;
                case NPCController.StormTyrantId: stormBoss = npc; break;
                case NPCController.CollectorVeyaId: collector = npc; break;
                case NPCController.RumorIrisId: rumorKeeper = npc; break;
                case NPCController.CartographerJessaId: cartographer = npc; break;
                case NPCController.QuartermasterBramId: quartermaster = npc; break;
                case NPCController.RunnerNiaId: runner = npc; break;
                case NPCController.ForemanOrloId: foreman = npc; break;
                case NPCController.EthicistThrenId: ethicist = npc; break;
                case NPCController.MoonwellLumaId: moonwellKeeper = npc; break;
                case NPCController.SableRivalId: sable = npc; break;
                case NPCController.TailorSerinId: tailor = npc; break;
            }
        }

        void SyncPlayerToSavedArea()
        {
            if (player == null || worldManager == null) return;
            var saved = worldManager.CurrentPlayerPosition;
            var savedArea = WorldMapLayout.ResolveAreaId(saved);
            if (!string.IsNullOrEmpty(savedArea) && savedArea == worldManager.CurrentAreaId)
            {
                player.transform.position = new Vector3(saved.x, saved.y, 0f);
                return;
            }

            var spawn = WorldMapLayout.SpawnPoint(worldManager.CurrentAreaId);
            player.transform.position = new Vector3(spawn.x, spawn.y, 0f);
            worldManager.SetCurrentPlayerPosition(spawn);
        }

        void UpdateAreaFromPosition()
        {
            if (player == null || worldManager == null) return;
            var position = new Vector2(player.transform.position.x, player.transform.position.y);
            worldManager.SetCurrentPlayerPosition(position);
            var target = ResolveAreaTarget(position);

            if (worldManager.CurrentAreaId == target) return;
            worldManager.SetCurrentArea(target);
            AudioManager.EnsureExists().PlayMusicForArea(target);
            ReportVisitObjective(target);
            SetStatus(BuildAreaArrivalStatus(target));
        }

        void ReportVisitObjective(string target)
        {
            ObjectiveRegistry.ReportAreaVisit(questManager, target);
        }

        void HandleExplorationAndEncounters()
        {
            if (combat == null || combat.IsBattleActive || player == null || encounters == null || registry == null || worldManager == null)
                return;
            if (worldManager.CurrentAreaId == DefaultGameContent.TownId) return;
            if (worldManager.CurrentAreaId == DefaultGameContent.GroveId &&
                !questManager.IsActive(ChapterOneIds.BossQuest) &&
                !questManager.IsCompleted(ChapterOneIds.BossQuest))
                return;
            if (worldManager.CurrentAreaId == DefaultGameContent.MarshId && !IsChapterTwoUnlocked())
                return;
            if (worldManager.CurrentAreaId == DefaultGameContent.RuinsId && !IsRuinsUnlocked())
                return;
            if (worldManager.CurrentAreaId == DefaultGameContent.DeltaId && !IsChapterThreeUnlocked())
                return;
            if (worldManager.CurrentAreaId == DefaultGameContent.RidgeId && !IsRidgeUnlocked())
                return;
            if (worldManager.CurrentAreaId == DefaultGameContent.SpireId && !IsSpireUnlocked())
                return;
            var position = new Vector2(player.transform.position.x, player.transform.position.y);
            ObjectiveRegistry.ReportEscortProgress(questManager, worldManager.CurrentAreaId, position, player.DistanceMovedThisFrame);
            encounterPacer.TryStep(encounterDistanceStep, player, worldManager, encounters, registry, weather, combat, msg => SetStatus(msg));
        }

        NPCController FindNearbyNpc()
        {
            if (player == null) return null;
            NPCController best = null;
            var bestDist = float.MaxValue;
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
            {
                if (npc == null) continue;
                var dist = Vector3.Distance(player.transform.position, npc.transform.position);
                if (dist <= npc.InteractionRadius && dist < bestDist)
                {
                    best = npc;
                    bestDist = dist;
                }
            }
            return best;
        }

        string BuildPrompt(NPCController nearby)
        {
            if (nearby == null)
                return BuildAmbientPrompt();
            return nearby.Role switch
            {
                NpcRole.Shopkeeper => $"[E] Talk with {nearby.DisplayName} — \"Stock up before the wild patches.\"",
                NpcRole.Healer => $"[E] Visit {nearby.DisplayName} — \"Keep your party steady before long routes.\"",
                NpcRole.BossTrainer => $"[E] Challenge {nearby.DisplayName} — \"Strength decides this road.\"",
                _ => $"[E] Talk to {nearby.DisplayName} — \"Rumors change with every region.\""
            };
        }

        string BuildAmbientPrompt()
        {
            if (worldManager == null)
                return string.Empty;
            var area = worldManager.CurrentAreaId;
            return area switch
            {
                var id when id == DefaultGameContent.RouteId =>
                    "Route clear. Roads are safer; danger patches sit in the grass.",
                var id when id == DefaultGameContent.ForestId =>
                    "Bramblewood rustles ahead. Keep to roads if your party is low.",
                var id when id == DefaultGameContent.MarshId =>
                    "Lantern Marsh glows in pockets. Reeds hide most encounters.",
                var id when id == DefaultGameContent.RidgeId =>
                    "Stormbreak winds rise quickly. Heal before climbing farther.",
                _ => string.Empty
            };
        }

        void InteractWithNpc(NPCController npc)
        {
            if (npc == null) return;
            if (ShouldOpenEndingChoice(npc))
            {
                OpenEndingChoice();
                return;
            }
            if (TryBeginBranchConversation(npc))
            {
                ObjectiveRegistry.ReportNpcInteraction(questManager, npc.NpcId);
                return;
            }
            var dialog = BuildDialogForNpc(npc);
            npc.BindRuntimeDialog(dialog);

            switch (npc.Role)
            {
                case NpcRole.Shopkeeper:
                    pendingShopOpenAfterDialog = true;
                    dialogDriver?.BeginConversation(npc, dialog);
                    if (dialogDriver == null)
                    {
                        pendingShopOpenAfterDialog = false;
                        shopOpen = true;
                        SetStatus("Merchant stock is available.");
                    }
                    break;
                case NpcRole.Healer:
                    monsterSystem?.HealAll(registry);
                    dialogDriver?.BeginConversation(npc, dialog);
                    SetStatus("Your party was fully healed.");
                    break;
                case NpcRole.BossTrainer:
                    dialogDriver?.BeginConversation(npc, dialog);
                    if (ObjectiveRegistry.TryGetBossBattle(npc.NpcId, questManager, out var bossSpec))
                    {
                        pendingBossBattle = true;
                        pendingBossMonsterId = bossSpec.MonsterId;
                        pendingBossObjectiveId = bossSpec.ObjectiveId;
                    }
                    break;
                default:
                    dialogDriver?.BeginConversation(npc, dialog);
                    break;
            }

            ObjectiveRegistry.ReportNpcInteraction(questManager, npc.NpcId);
        }

        bool ShouldOpenEndingChoice(NPCController npc)
        {
            if (npc == null || questManager == null) return false;
            if (npc.NpcId != NPCController.ElderMiraId) return false;
            if (!questManager.IsActive(PhaseTwoIds.BindingChoiceQuest) || questManager.IsCompleted(PhaseTwoIds.BindingChoiceQuest))
                return false;
            if (questManager.GetNextObjectiveId(PhaseTwoIds.BindingChoiceQuest) != PhaseTwoIds.ReturnToMira)
                return false;
            return StoryState.GetEnding() == StoryEnding.None;
        }

        void OpenEndingChoice()
        {
            suggestedEnding = EndingResolver.SuggestEnding();
            var advisor = StoryState.GetAdvisor();
            var trust = StoryState.GetMiraTrust();
            endingSuggestionText = $"{EndingResolver.Describe(suggestedEnding)}\n\n" +
                                  $"Advisor: {(string.IsNullOrEmpty(advisor) ? "none" : advisor)}\n" +
                                  $"Mira trust: {trust}/3\n" +
                                  "Choose the final doctrine for Hollowfen.";
            endingChoiceOpen = true;
            SetStatus("Final decision required.");
        }

        public void ChooseEnding(StoryEnding ending)
        {
            StoryState.SetEnding(ending);
            StoryState.SetOutcome("ending_epilogue_state", "pending_return");
            achievements?.Unlock(ending switch
            {
                StoryEnding.Merge => SteamAchievementIds.EndingMerge,
                StoryEnding.Seal => SteamAchievementIds.EndingSeal,
                StoryEnding.Replace => SteamAchievementIds.EndingReplace,
                StoryEnding.Burn => SteamAchievementIds.EndingBurn,
                _ => string.Empty
            });
            endingChoiceOpen = false;
            questManager?.ReportObjectiveEvent(PhaseTwoIds.ReturnToMira, 1);
            SyncCampaignState();
            SetStatus($"Ending locked: {ending}.");
        }

        public void ForceOpenEndingChoiceForDebug() => OpenEndingChoice();

        bool TryBeginBranchConversation(NPCController npc)
        {
            if (npc == null || dialogDriver == null || questManager == null) return false;
            DialogData choiceDialog = null;
            switch (npc.NpcId)
            {
                case NPCController.BossIonaId:
                    if (questManager.IsActive(ChapterOneIds.BossQuest) && !questManager.IsCompleted(ChapterOneIds.BossQuest) &&
                        !StoryState.HasOutcome(StoryState.IonaOutcomeKey))
                    {
                        choiceDialog = CreateChoiceDialog("dlg_choice_iona", npc.DisplayName,
                            "The grove obeys no one for free. How do you answer Iona?",
                            new[] { "Fight for passage", "Spare Iona and share the grove", "Withdraw and fortify Hollowfen" },
                            new[] { "branch:iona:defeat", "branch:iona:spare", "branch:iona:withdraw" });
                    }
                    break;
                case NPCController.RivalCorinId:
                    if (questManager.IsActive(ChapterTwoIds.RivalQuest) && !questManager.IsCompleted(ChapterTwoIds.RivalQuest) &&
                        !StoryState.HasOutcome(StoryState.CorinOutcomeKey))
                    {
                        choiceDialog = CreateChoiceDialog("dlg_choice_corin", npc.DisplayName,
                            "Corin has reached the archive core. What do you do?",
                            new[] { "Defeat Corin and hand the relic to Sel", "Defeat Corin and break the relic", "Side with Corin against Sel", "Talk Corin down" },
                            new[] { "branch:corin:hand_relic_to_sel", "branch:corin:break_relic", "branch:corin:side_with_corin", "branch:corin:talk_down_corin" });
                    }
                    break;
                case NPCController.StormTyrantId:
                    if (questManager.IsActive(ChapterThreeIds.SpireQuest) && !questManager.IsCompleted(ChapterThreeIds.SpireQuest) &&
                        !StoryState.HasOutcome(StoryState.VaroOutcomeKey))
                    {
                        choiceDialog = CreateChoiceDialog("dlg_choice_varo", npc.DisplayName,
                            "The Skyglass storm is about to break. Choose your approach.",
                            new[] { "Defeat Varo", "Ally with Varo to contain the network", "Defeat Varo but keep the relic", "Refuse the climb and confront Mira" },
                            new[] { "branch:varo:defeat_varo", "branch:varo:ally_with_varo", "branch:varo:defeat_and_keep_relic", "branch:varo:refuse_spire" });
                    }
                    break;
                case NPCController.MoonwellLumaId:
                    if (questManager.IsActive(PhaseTwoIds.BindingChoiceQuest) && !StoryState.HasOutcome(StoryState.AdvisorKey))
                        choiceDialog = BuildAdvisorChoiceDialog(npc.DisplayName);
                    break;
                case NPCController.EthicistThrenId:
                    if (questManager.IsActive(PhaseTwoIds.BindingChoiceQuest) && !StoryState.HasOutcome(StoryState.AdvisorKey))
                        choiceDialog = BuildAdvisorChoiceDialog(npc.DisplayName);
                    break;
                case NPCController.QuartermasterBramId:
                    if (questManager.IsActive(PhaseTwoIds.BindingChoiceQuest) && !StoryState.HasOutcome(StoryState.AdvisorKey))
                        choiceDialog = BuildAdvisorChoiceDialog(npc.DisplayName);
                    break;
                case NPCController.CartographerJessaId:
                    if (questManager.IsActive(PhaseTwoIds.BindingChoiceQuest) && !StoryState.HasOutcome(StoryState.AdvisorKey))
                        choiceDialog = BuildAdvisorChoiceDialog(npc.DisplayName);
                    break;
                case NPCController.CollectorVeyaId:
                    if (!StoryFlags.HasFlag(StoryState.VeyaRelease))
                        choiceDialog = CreateChoiceDialog("dlg_choice_veya", npc.DisplayName,
                            "Veya asks how to handle rare captures.",
                            new[] { "Record and release them", "Keep specimens for study" },
                            new[] { "side:veya:release", "side:veya:record" });
                    break;
                case NPCController.RumorIrisId:
                    if (!StoryFlags.HasFlag(StoryState.IrisSuppress))
                        choiceDialog = CreateChoiceDialog("dlg_choice_iris", npc.DisplayName,
                            "A dangerous rumor could spread panic. What do you do?",
                            new[] { "Suppress the rumor", "Spread it for leverage" },
                            new[] { "side:iris:suppress", "side:iris:spread" });
                    break;
                case NPCController.HealerPiaId:
                    if (!StoryFlags.HasFlag(StoryState.PiaDoorOpenEarly))
                        choiceDialog = CreateChoiceDialog("dlg_choice_pia", npc.DisplayName,
                            "Pia asks whether to open the locked clinic room now.",
                            new[] { "Open it now", "Wait until after Phase 2" },
                            new[] { "side:pia:early", "side:pia:late" });
                    break;
            }

            if (choiceDialog == null) return false;
            npc.BindRuntimeDialog(choiceDialog);
            dialogDriver.BeginConversation(npc, choiceDialog);
            return true;
        }

        DialogData BuildAdvisorChoiceDialog(string speaker)
        {
            return CreateChoiceDialog("dlg_choice_advisor", speaker,
                "Whose guidance will shape your binding decision?",
                new[] { "Luma: trust and bonds", "Thren: consent and ethics", "Bram: practical survival", "Jessa: truth-first resistance" },
                new[] { "advisor:luma", "advisor:thren", "advisor:bram", "advisor:jessa" });
        }

        DialogData CreateChoiceDialog(string id, string speaker, string line, string[] labels, string[] commands)
        {
            var data = ScriptableObject.CreateInstance<DialogData>();
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;
            data.Configure(id, new[]
            {
                new DialogEntry
                {
                    speaker = speaker,
                    line = line,
                    choiceLabels = labels,
                    choiceNextIds = commands
                }
            });
            return data;
        }

        void HookDialogChoiceEvents()
        {
            if (dialogDriver == null || dialogChoiceHooked) return;
            dialogDriver.ChoiceCommandIssued += OnDialogChoiceCommandIssued;
            dialogChoiceHooked = true;
        }

        void UnhookDialogChoiceEvents()
        {
            if (dialogDriver == null || !dialogChoiceHooked) return;
            dialogDriver.ChoiceCommandIssued -= OnDialogChoiceCommandIssued;
            dialogChoiceHooked = false;
        }

        void OnDialogChoiceCommandIssued(NPCController npc, string command)
        {
            if (string.IsNullOrWhiteSpace(command)) return;
            switch (command.Trim())
            {
                case "branch:iona:defeat":
                    StoryState.SetOutcome(StoryState.IonaOutcomeKey, StoryState.IonaDefeat);
                    pendingBossBattle = true;
                    pendingBossMonsterId = DefaultGameContent.ThornBeastId;
                    pendingBossObjectiveId = ChapterOneIds.DefeatBoss;
                    StoryState.AddMiraTrust(1);
                    break;
                case "branch:iona:spare":
                    StoryState.SetOutcome(StoryState.IonaOutcomeKey, StoryState.IonaSpare);
                    questManager?.ReportObjectiveEvent(ChapterOneIds.DefeatBoss, 1);
                    StoryState.AddMiraTrust(-1);
                    achievements?.Unlock(SteamAchievementIds.IonaSpare);
                    SetStatus("Iona stands down. The grove watches your mercy.");
                    break;
                case "branch:iona:withdraw":
                    StoryState.SetOutcome(StoryState.IonaOutcomeKey, StoryState.IonaWithdraw);
                    questManager?.ReportObjectiveEvent(ChapterOneIds.DefeatBoss, 1);
                    StoryState.AddMiraTrust(-1);
                    achievements?.Unlock(SteamAchievementIds.IonaWithdraw);
                    SetStatus("You withdraw from the grove and prepare Hollowfen.");
                    break;
                case "branch:corin:hand_relic_to_sel":
                    StoryState.SetOutcome(StoryState.CorinOutcomeKey, StoryState.CorinHandRelicToSel);
                    pendingBossBattle = true;
                    pendingBossMonsterId = DefaultGameContent.BogWyrmId;
                    pendingBossObjectiveId = ChapterTwoIds.DefeatRival;
                    StoryState.AddMiraTrust(1);
                    break;
                case "branch:corin:break_relic":
                    StoryState.SetOutcome(StoryState.CorinOutcomeKey, StoryState.CorinBreakRelic);
                    questManager?.ReportObjectiveEvent(ChapterTwoIds.DefeatRival, 1);
                    StoryFlags.SetFlag(StoryState.CorinTruthKnown);
                    StoryState.AddMiraTrust(-1);
                    achievements?.Unlock(SteamAchievementIds.CorinTruth);
                    SetStatus("You shatter the relic before anyone can claim it.");
                    break;
                case "branch:corin:side_with_corin":
                    StoryState.SetOutcome(StoryState.CorinOutcomeKey, StoryState.CorinSideWithCorin);
                    questManager?.ReportObjectiveEvent(ChapterTwoIds.DefeatRival, 1);
                    StoryFlags.SetFlag(StoryState.CorinTruthKnown);
                    StoryState.AddMiraTrust(-1);
                    achievements?.Unlock(SteamAchievementIds.CorinTruth);
                    SetStatus("You side with Corin against Sel's plan.");
                    break;
                case "branch:corin:talk_down_corin":
                    StoryState.SetOutcome(StoryState.CorinOutcomeKey, StoryState.CorinTalkDown);
                    questManager?.ReportObjectiveEvent(ChapterTwoIds.DefeatRival, 1);
                    StoryFlags.SetFlag(StoryState.CorinTruthKnown);
                    StoryState.AddMiraTrust(1);
                    achievements?.Unlock(SteamAchievementIds.CorinTruth);
                    SetStatus("Corin backs down from forcing the archive.");
                    break;
                case "branch:varo:defeat_varo":
                    StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroDefeat);
                    pendingBossBattle = true;
                    pendingBossMonsterId = DefaultGameContent.DeltaKingId;
                    pendingBossObjectiveId = ChapterThreeIds.DefeatSpireBoss;
                    StoryState.AddMiraTrust(1);
                    break;
                case "branch:varo:ally_with_varo":
                    StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroAlly);
                    questManager?.ReportObjectiveEvent(ChapterThreeIds.DefeatSpireBoss, 1);
                    StoryFlags.SetFlag(StoryState.VaroJournalRead);
                    StoryState.AddMiraTrust(-1);
                    SetStatus("You help Varo reinforce Skyglass containment.");
                    break;
                case "branch:varo:defeat_and_keep_relic":
                    StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroDefeatKeepRelic);
                    pendingBossBattle = true;
                    pendingBossMonsterId = DefaultGameContent.DeltaKingId;
                    pendingBossObjectiveId = ChapterThreeIds.DefeatSpireBoss;
                    StoryFlags.SetFlag(StoryState.NetworkAware);
                    SetStatus("You prepare to claim the Skyglass relic after victory.");
                    break;
                case "branch:varo:refuse_spire":
                    StoryState.SetOutcome(StoryState.VaroOutcomeKey, StoryState.VaroRefuseSpire);
                    questManager?.ReportObjectiveEvent(ChapterThreeIds.DefeatSpireBoss, 1);
                    StoryFlags.SetFlag(StoryState.NetworkAware);
                    StoryState.AddMiraTrust(-1);
                    achievements?.Unlock(SteamAchievementIds.VaroRefuseSpire);
                    SetStatus("You refuse the spire assault and confront Mira instead.");
                    break;
                case "advisor:luma":
                case "advisor:thren":
                case "advisor:bram":
                case "advisor:jessa":
                    StoryState.SetAdvisor(command.Substring("advisor:".Length));
                    if (command.Trim() == "advisor:jessa")
                        StoryFlags.SetFlag(StoryState.JessaFormerMiraKnown);
                    achievements?.Unlock(SteamAchievementIds.AdvisorChosen);
                    SetStatus($"Advisor selected: {StoryState.GetAdvisor()}.");
                    break;
                case "side:veya:release":
                    StoryFlags.SetFlag(StoryState.VeyaRelease);
                    SetStatus("You ask Veya to record and release rare captures.");
                    break;
                case "side:veya:record":
                    StoryState.SetOutcome("veya_outcome", "record");
                    SetStatus("You support Veya's specimen study approach.");
                    break;
                case "side:iris:suppress":
                    StoryFlags.SetFlag(StoryState.IrisSuppress);
                    SetStatus("You suppress the rumor to reduce panic.");
                    break;
                case "side:iris:spread":
                    StoryState.SetOutcome("iris_outcome", "spread");
                    SetStatus("You spread the rumor to pressure local action.");
                    break;
                case "side:pia:early":
                    StoryFlags.SetFlag(StoryState.PiaDoorOpenEarly);
                    StoryFlags.SetFlag(StoryState.NetworkAware);
                    SetStatus("Pia opens the clinic's locked room early.");
                    break;
                case "side:pia:late":
                    StoryState.SetOutcome("pia_outcome", "late");
                    SetStatus("Pia keeps the clinic room sealed for now.");
                    break;
            }

            SyncCampaignState();
        }

        public void SaveCurrentGame()
        {
            string error = null;
            if (saveLoad != null && saveLoad.SaveSlot(0, out error))
                SetStatus("Game saved.");
            else
                SetStatus(string.IsNullOrEmpty(error) ? "Save failed." : $"Save failed: {error}");
        }

        public void LoadCurrentGame()
        {
            string error = null;
            if (saveLoad != null && saveLoad.LoadSlot(0, out error))
            {
                SyncPlayerToSavedArea();
                SetStatus("Game loaded.");
            }
            else
                SetStatus(string.IsNullOrEmpty(error) ? "Load failed." : $"Load failed: {error}");
        }

        public void BuyShopItem(string itemId, int _)
        {
            if (shopManager == null || inventory == null)
            {
                SetStatus("Purchase failed.");
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null)
            {
                SetStatus("Purchase failed (no game state).");
                return;
            }

            var shop = shopManager.Current;
            var row = ShopManager.FindListing(shop, itemId);
            if (row == null)
            {
                SetStatus("Item not in stock.");
                return;
            }

            var price = ShopManager.QuoteUnitPrice(registry, row);
            if (gm.PlayerGold < price)
            {
                SetStatus($"Need {price}g (you have {gm.PlayerGold}g).");
                return;
            }

            if (shopManager.TryBuy(inventory, shop, itemId))
                SetStatus($"Bought {registry?.GetItem(itemId)?.DisplayName ?? itemId}.");
            else
                SetStatus("Purchase failed.");
        }

        public void CloseShop() => shopOpen = false;

        /// <summary>Opens the shop UI with the given stock (e.g. from the active NPC).</summary>
        public void OpenShopForNpc(ShopData shopData)
        {
            if (shopData == null || shopManager == null) return;
            EnsureGearShopRuntime();
            if (ReferenceEquals(shopData, runtimeGearShop) || shopData.ShopId == DefaultGameContent.GearShopId)
            {
                GearVendorCatalog.Replenish(runtimeGearShop, questManager);
                shopManager.SetShop(runtimeGearShop);
            }
            else
                shopManager.SetShop(shopData);
            shopOpen = true;
            SetStatus("Browse stock and buy what you need.");
        }

        /// <summary>Uses a consumable from inventory on the active party monster (overworld).</summary>
        public bool TryUseConsumableOnActiveMonster(string itemId)
        {
            if (monsterSystem == null || inventory == null || registry == null) return false;
            if (!monsterSystem.TryApplyConsumableFromInventory(itemId, registry, inventory)) return false;
            var name = registry.GetItem(itemId)?.DisplayName ?? itemId;
            SetStatus($"Used {name}.");
            return true;
        }

        /// <summary>Selects a party slot and uses the matching cure item from inventory if the monster has that status.</summary>
        public bool TryUseMatchingCureForPartyMember(int partyIndex)
        {
            if (monsterSystem == null || inventory == null || registry == null) return false;
            if (partyIndex < 0 || partyIndex >= monsterSystem.Party.Count) return false;
            monsterSystem.SetActiveIndex(partyIndex);
            var m = monsterSystem.Party[partyIndex];
            if (m == null) return false;
            var itemId = StatusCureCatalog.RecommendedCureItemId(m.persistentStatus);
            if (string.IsNullOrEmpty(itemId) || inventory.Count(itemId) <= 0) return false;
            return TryUseConsumableOnActiveMonster(itemId);
        }

        void OnMonsterLeveled(string instanceId)
        {
            var monster = monsterSystem != null ? monsterSystem.GetActiveMonster() : null;
            if (monster != null && monster.instanceId == instanceId)
                SetStatus($"{monster.GetDisplayName(registry != null ? registry.GetMonster(monster.monsterDataId) : null)} reached Lv{monster.level}.");
        }

        void OnMonsterEvolved(string instanceId)
        {
            if (monsterSystem == null || registry == null) return;
            foreach (var monster in monsterSystem.Party)
            {
                if (monster != null && monster.instanceId == instanceId)
                {
                    var data = registry.GetMonster(monster.monsterDataId);
                    SetStatus($"{monster.GetDisplayName(data)} evolved.");
                    return;
                }
            }
        }

        void SetStatus(string text, float seconds = 4f)
        {
            statusText = text;
            statusUntil = Time.time + seconds;
            GameEvents.RaiseToast(text);
        }

        DialogData BuildDialogForNpc(NPCController npc)
        {
            if (npc == null) return DefaultGameContent.CreateElderGreetingDialog();
            if (dialogCache.TryGetValue(npc.NpcId, out var existing) && existing != null &&
                npc.Role != NpcRole.Story && npc.Role != NpcRole.BossTrainer)
                return existing;

            if (NpcContentRegistry.TryGet(npc.NpcId, out var def) && def.DialogBuilder != null)
            {
                var registryDialog = def.DialogBuilder(questManager);
                dialogCache[npc.NpcId] = registryDialog;
                return registryDialog;
            }

            DialogData data = npc.NpcId switch
            {
                NPCController.ElderMiraId => BuildElderDialog(),
                NPCController.ScoutRinId => LoadDialogOrFallback(ChapterOneIds.ScoutDialog, BuildScoutDialog),
                NPCController.MerchantTomaId => LoadDialogOrFallback(ChapterOneIds.MerchantDialog, BuildMerchantDialog),
                NPCController.HealerPiaId => LoadDialogOrFallback(ChapterOneIds.HealerDialog, BuildHealerDialog),
                NPCController.BossIonaId => BuildBossDialog(),
                NPCController.ArchivistSelId => LoadDialogOrFallback(ChapterTwoIds.ArchivistDialog, BuildArchivistDialog),
                NPCController.RivalCorinId => LoadDialogOrFallback(ChapterTwoIds.RivalDialog, BuildRivalDialog),
                NPCController.WardenNerisId => LoadDialogOrFallback(ChapterThreeIds.WardenDialog, BuildWardenDialog),
                NPCController.MentorCaelId => LoadDialogOrFallback(ChapterThreeIds.MentorDialog, BuildMentorDialog),
                NPCController.StormTyrantId => LoadDialogOrFallback(ChapterThreeIds.StormBossDialog, BuildStormBossDialog),
                NPCController.CollectorVeyaId => LoadDialogOrFallback(ChapterThreeIds.CollectorDialog, BuildCollectorDialog),
                NPCController.RumorIrisId => LoadDialogOrFallback(ChapterThreeIds.RumorDialog, BuildRumorDialog),
                _ => LoadDialogOrFallback(ChapterOneIds.ElderDialog, DefaultGameContent.CreateElderGreetingDialog)
            };

            dialogCache[npc.NpcId] = data;
            return data;
        }

        DialogData BuildElderDialog()
        {
            var ionaOutcome = StoryState.GetOutcome(StoryState.IonaOutcomeKey);
            var corinOutcome = StoryState.GetOutcome(StoryState.CorinOutcomeKey);
            var varoOutcome = StoryState.GetOutcome(StoryState.VaroOutcomeKey);
            var advisor = StoryState.GetAdvisor();

            if (questManager != null && questManager.IsActive(PhaseTwoIds.BindingChoiceQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "The Wilderward is no longer a rumor on our old maps. Tell me what Sable, Thren, and the hollow signal forced you to decide.",
                    "Whatever choice we make about the lore network, Hollowfen must be able to live with it after the victory songs fade.");

            if (questManager != null && questManager.IsActive(PhaseTwoIds.WiderMapQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "The spire's storm broke, but its echo opened old roads north of Hollowfen.",
                    "Find Jessa Vale in Stonewake. If her map is right, our home is only one corner of the Wilderward.");

            if (questManager != null && questManager.IsCompleted(PhaseTwoIds.BindingChoiceQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    StoryState.GetEnding() != StoryEnding.None
                        ? $"You chose the {StoryState.GetEnding().ToString().ToLowerInvariant()} path. Hollowfen will inherit that choice for generations."
                        : "You gave Hollowfen more than a route through the Wilderward. You gave us a way to think before we bind what we barely understand.",
                    "Phase 2 has changed the map, and it has changed us with it.",
                    BuildBindingAftermathLine());

            if (questManager != null && questManager.IsCompleted(ChapterThreeIds.ReturnQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    varoOutcome == StoryState.VaroAlly
                        ? "You chose to hold the spire instead of simply winning it. I did not expect restraint at that height."
                        : varoOutcome == StoryState.VaroRefuseSpire
                            ? "You refused the climb and forced this town to answer the spire from the ground. That choice changed us."
                            : "You crossed the delta, the ridge, and the spire, then still found your way home with the storm behind you.",
                    !string.IsNullOrEmpty(advisor)
                        ? $"When the north opens, we will need voices like {advisor} beside us."
                        : "Hollowfen can breathe again for a while. When we plan the next march, it will be beyond anything this town has faced before.");

            if (questManager != null && questManager.IsActive(ChapterThreeIds.ReturnQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "You made it back from the spire. Tell me what Varo tried to wake and whether the skyglass still stands.");

            if (questManager != null && questManager.IsCompleted(ChapterTwoIds.ReturnQuest) &&
                !questManager.IsCompleted(ChapterThreeIds.BeaconQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "Sel's warning did not end in the archive. The river watch says stormlight now rolls past the delta like marching fire.",
                    "Cross the ruins and find Warden Neris in the Flooded Delta. If the spire has woken, we need its truth before the sky breaks over Hollowfen.");

            if (questManager != null && questManager.IsCompleted(ChapterTwoIds.ReturnQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    corinOutcome == StoryState.CorinSideWithCorin
                        ? "You came back with Corin's version of the archive truth. I hear it, even if I do not trust all of it yet."
                        : "So the Sunken Archive woke, and you still carried the news home. Hollowfen will remember that.",
                    "Rest while you can. The town's troubles have grown larger than one grove now.");

            if (questManager != null && questManager.IsActive(ChapterTwoIds.ReturnQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "You made it back from the archive. Speak plainly now. What did Sel uncover, and what was Corin chasing?");

            if (questManager != null && questManager.IsCompleted(ChapterOneIds.ReturnQuest) &&
                !questManager.IsCompleted(ChapterTwoIds.SignalQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "The grove quieted, but lantern fires now burn beyond the marsh. Hollowfen needs a second answer from you.",
                    "Follow the old path past the grove and find Archivist Sel in Lantern Marsh. If the archive is waking, we must know why.");

            if (questManager != null && questManager.IsActive(ChapterOneIds.ReturnQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    ionaOutcome == StoryState.IonaSpare
                        ? "You spared Iona and still brought the grove to heel. Hollowfen owes you thanks, and perhaps patience."
                        : ionaOutcome == StoryState.IonaWithdraw
                            ? "You withdrew from the grove and chose caution over glory. Hollowfen still owes you thanks for honesty."
                            : "You faced the Briar Warden and returned standing. Hollowfen owes you thanks.",
                    "Take this reward and know that the forest now watches you with a different eye.");

            if (questManager != null && questManager.IsCompleted(ChapterOneIds.BossQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "I heard the grove fell quiet. Return to me when you are ready.");

            if (questManager != null && questManager.IsCompleted(ChapterOneIds.IntroQuest))
                return CreateRuntimeDialog(ChapterOneIds.ElderDialog, "Mira, Town Elder",
                    "Good. You handled a live battle. Scout Rin waits on the eastern route with your next task.",
                    "If you lose track of your lead, tap M for the route marker and J for your full quest notes.");

            return LoadDialogOrFallback(ChapterOneIds.ElderDialog, DefaultGameContent.CreateElderGreetingDialog);
        }

        DialogData BuildScoutDialog() =>
            questManager != null && IsChapterThreeUnlocked()
                ? CreateRuntimeDialog(ChapterOneIds.ScoutDialog, "Rin, Field Scout",
                    "Tracks do not stop at the ruins anymore. Whole herds are veering south, away from the delta storms.",
                    "If Neris asks for help, take it seriously. Ridge weather does not forgive bad planning.")
                : questManager != null && IsChapterTwoUnlocked()
                ? CreateRuntimeDialog(ChapterOneIds.ScoutDialog, "Rin, Field Scout",
                    "The marsh path is open again, but the ground out there is wrong. Too many lights, not enough birdsong.",
                    "If Sel asks for help, listen. She reads old stones better than any of us read tracks.")
                : CreateRuntimeDialog(ChapterOneIds.ScoutDialog, "Rin, Field Scout",
                    "Tracks all converge on the grove ahead. Something stronger than the usual wild packs has claimed it.",
                    "Push through the forest and deal with the Briar Warden before it drives the smaller monsters into town.",
                    "Stay stocked and keep your active monster healthy between fights.");

        DialogData BuildMerchantDialog() =>
            IsChapterThreeUnlocked()
                ? CreateRuntimeDialog(ChapterOneIds.MerchantDialog, "Toma, Merchant",
                    "Delta ferries want dry wraps, ridge climbers want tonics, and everyone wants the last charm when lightning starts chasing them.",
                    "If you are heading for the spire, buy like you expect trouble twice over.")
                : IsChapterTwoUnlocked()
                ? CreateRuntimeDialog(ChapterOneIds.MerchantDialog, "Toma, Merchant",
                    "Marsh travel eats through bandages and nerve alike. If lantern bugs start circling you, keep moving and keep a potion ready.",
                    "Word is Corin ran past here in a hurry. Ambition like that always ends with someone buying supplies too late.")
                : CreateRuntimeDialog(ChapterOneIds.MerchantDialog, "Toma, Merchant",
                    "Potions keep a party standing. Capture Charms are dear, but worth every coin if you meet a rare beast.",
                    "If you're heading east, stock up now. Hollowfen's roads are kinder than its woods.",
                    "Press I to check what you already carry before spending coin.");

        DialogData BuildHealerDialog() =>
            IsChapterThreeUnlocked()
                ? CreateRuntimeDialog(ChapterOneIds.HealerDialog, "Pia, Healer",
                    "Storm exposure lingers even after the bruises fade. Let me steady your monsters before the ridge takes more out of them.",
                    StoryFlags.HasFlag(StoryState.PiaDoorOpenEarly)
                        ? "I opened the clinic room early and found records linking symptoms to the lore network. Do not ignore those patterns."
                        : "There. Their breathing is easier now. Do not rush a long campaign like it is one more route fight.")
                : IsChapterTwoUnlocked()
                ? CreateRuntimeDialog(ChapterOneIds.HealerDialog, "Pia, Healer",
                    "Marsh sickness settles in quietly. Let me know if your monsters come back sluggish or glassy-eyed.",
                    "There. They are steadier now. The archive path will test calm as much as strength.")
                : CreateRuntimeDialog(ChapterOneIds.HealerDialog, "Pia, Healer",
                    "There. Your monsters are steady again.",
                    "Do not mistake a healed wound for endless strength. Rest matters, too.",
                    "If status effects linger, carry cures before you leave town.");

        DialogData BuildBossDialog()
        {
            var outcome = StoryState.GetOutcome(StoryState.IonaOutcomeKey);
            if (outcome == StoryState.IonaSpare)
                return CreateRuntimeDialog(ChapterOneIds.BossDialog, "Iona, Briar Warden",
                    "You chose restraint when force was easier. The grove does not forget that.");
            if (outcome == StoryState.IonaWithdraw)
                return CreateRuntimeDialog(ChapterOneIds.BossDialog, "Iona, Briar Warden",
                    "You walked away once. The grove remembers unfinished choices.");
            if (questManager != null && questManager.IsCompleted(ChapterOneIds.BossQuest))
                return CreateRuntimeDialog(ChapterOneIds.BossDialog, "Iona, Briar Warden",
                    "You won your right to pass. The grove remembers strength.");

            return CreateRuntimeDialog(ChapterOneIds.BossDialog, "Iona, Briar Warden",
                "So Hollowfen sends another hopeful trainer.",
                "If you want the grove's monsters to stand down, prove your claim in battle.");
        }

        DialogData BuildArchivistDialog() =>
            questManager != null && questManager.IsCompleted(ChapterTwoIds.ArchiveQuest)
                ? CreateRuntimeDialog(ChapterTwoIds.ArchivistDialog, "Sel, Marsh Archivist",
                    "The archive doors answered your steps. Good. That means the wardstones still recognize living intent.",
                    StoryFlags.HasFlag(StoryState.CorinTruthKnown)
                        ? "Corin forced the truth into daylight. We can disagree on method, but not on what the relic tried to become."
                        : "Corin pressed deeper before I could stop him. Catch him in the ruins before he wakes something we cannot quiet twice.")
                : CreateRuntimeDialog(ChapterTwoIds.ArchivistDialog, "Sel, Marsh Archivist",
                    "You must be Mira's runner. The lantern beacon started after the grove fell quiet, as if one old ward traded its burden for another.",
                    "Head east into the Sunken Archive. Read the stones if you can, but if you meet Corin first, do not let him force the relic open.");

        string BuildBindingAftermathLine()
        {
            if (StoryFlags.HasFlag(StoryState.NetworkAware) && StoryFlags.HasFlag(StoryState.CorinTruthKnown))
                return "You now know how the old network manipulates both records and memory. That knowledge must stay public.";
            if (StoryFlags.HasFlag(StoryState.PiaDoorOpenEarly))
                return "Pia's early clinic records gave us warning signs we never had before. Keep sharing what you learn from the field.";
            if (StoryFlags.HasFlag(StoryState.JessaFormerMiraKnown))
                return "Jessa's history changed how I read this crisis. Old leadership mistakes cannot stay buried.";
            return "People in Stonewake, Moonwell, and Hollowfen are already reacting to your final doctrine.";
        }

        DialogData BuildRivalDialog()
        {
            var outcome = StoryState.GetOutcome(StoryState.CorinOutcomeKey);
            if (outcome == StoryState.CorinTalkDown)
                return CreateRuntimeDialog(ChapterTwoIds.RivalDialog, "Corin, Ambitious Rival",
                    "I still hate that you were right to slow me down.",
                    "But you talked me out of forcing the relic, and that debt is real.");
            if (outcome == StoryState.CorinSideWithCorin)
                return CreateRuntimeDialog(ChapterTwoIds.RivalDialog, "Corin, Ambitious Rival",
                    "You saw what Sel would not say out loud. Archive truth is never neutral.",
                    "If we do this, we do it on our terms.");
            if (questManager != null && questManager.IsCompleted(ChapterTwoIds.RivalQuest))
                return CreateRuntimeDialog(ChapterTwoIds.RivalDialog, "Corin, Ambitious Rival",
                    "You won this one. Enjoy the praise while it lasts.");

            return CreateRuntimeDialog(ChapterTwoIds.RivalDialog, "Corin, Ambitious Rival",
                "Sel would bury this place in caution and Mira would bury it in rules. I am taking the archive's prize before either of them blinks.",
                "If you want to stop me, prove you are stronger than the marsh stories trailing behind you.");
        }

        DialogData BuildWardenDialog() =>
            questManager != null && questManager.IsCompleted(ChapterThreeIds.DeltaQuest)
                ? CreateRuntimeDialog(ChapterThreeIds.WardenDialog, "Neris, Delta Warden",
                    "Good. You crossed the canals without folding. Cael waits on Stormbreak Ridge, and he will judge whether you are ready for the climb.",
                    "The spire's light is bending the weather. Move quickly, but not blindly.")
                : CreateRuntimeDialog(ChapterThreeIds.WardenDialog, "Neris, Delta Warden",
                    "Hollowfen sent you late, but not too late. The delta has been evacuating villages every night the skyglass flares.",
                    "Hold the crossings, learn the storm's path, and then push to the ridge. We need someone who can reach the spire alive.");

        DialogData BuildMentorDialog() =>
            questManager != null && questManager.IsCompleted(ChapterThreeIds.RidgeQuest)
                ? CreateRuntimeDialog(ChapterThreeIds.MentorDialog, "Cael, Veteran Mentor",
                    "Your footing held through the ridge, which means you might survive the spire itself.",
                    "Varo is waiting above. Do not mistake his confidence for calm.")
                : CreateRuntimeDialog(ChapterThreeIds.MentorDialog, "Cael, Veteran Mentor",
                    "The ridge only rewards parties that can last longer than a single burst of strength.",
                    "Win two hard fights up here, then I will mark you ready for the spire climb.");

        DialogData BuildStormBossDialog()
        {
            var outcome = StoryState.GetOutcome(StoryState.VaroOutcomeKey);
            if (outcome == StoryState.VaroAlly)
                return CreateRuntimeDialog(ChapterThreeIds.StormBossDialog, "Varo, Storm Tyrant",
                    "Containment is uglier than hero stories, but it kept the network from spreading in one surge.",
                    "If Hollowfen wants truth, it must survive it first.");
            if (outcome == StoryState.VaroRefuseSpire)
                return CreateRuntimeDialog(ChapterThreeIds.StormBossDialog, "Varo, Storm Tyrant",
                    "You refused the final climb. That may be wisdom, or fear. Time will name it.");
            if (questManager != null && questManager.IsCompleted(ChapterThreeIds.SpireQuest))
                return CreateRuntimeDialog(ChapterThreeIds.StormBossDialog, "Varo, Storm Tyrant",
                    "You shattered my claim. Remember that the storm was older than me, and it will outlive both of us.");

            return CreateRuntimeDialog(ChapterThreeIds.StormBossDialog, "Varo, Storm Tyrant",
                "I did not wake the spire to hand it back to frightened towns and wardens.",
                "If you want the storm to kneel, climb the last steps and take that right from me.");
        }

        DialogData BuildCollectorDialog() =>
            questManager != null && questManager.IsCompleted(ChapterThreeIds.CollectorQuest)
                ? CreateRuntimeDialog(ChapterThreeIds.CollectorDialog, "Veya, Delta Collector",
                    "You really caught enough delta specimens to prove the stories right. I owe you more than excitement now.",
                    "Keep an eye out in the ridge roosts too. Rare monsters always travel ahead of disaster.")
                : CreateRuntimeDialog(ChapterThreeIds.CollectorDialog, "Veya, Delta Collector",
                    "Everyone runs from storms. I run toward the creatures they flush out.",
                    "If you catch a few delta rarities for me, I will trade every rumor I have about what lives near the spire.");

        DialogData BuildRumorDialog() =>
            questManager != null && questManager.IsCompleted(ChapterThreeIds.RumorQuest)
                ? CreateRuntimeDialog(ChapterThreeIds.RumorDialog, "Iris, Rumor Keeper",
                    "You followed the ridge stories and came back with proof. That is how legends stop being gossip.",
                    "If another road opens beyond the spire, I will hear it first.")
                : CreateRuntimeDialog(ChapterThreeIds.RumorDialog, "Iris, Rumor Keeper",
                    "Ferry hands say the delta lights walk upstream. Ridge climbers say they hear wings inside the thunder itself.",
                    "Bring me proof from beyond the archive and I will point you toward every frightened soul still hiding work.");

        DialogData BuildCartographerDialog() =>
            CreateRuntimeDialog(PhaseTwoIds.CartographerDialog, "Jessa Vale, Cartographer",
                "The old east road was never the whole map. It was just the safest line Hollowfen remembered.",
                "Stonewake is awake again, the northwood trails are passable, and your map should grow with every place you dare to mark.");

        DialogData BuildQuartermasterDialog() =>
            CreateRuntimeDialog(PhaseTwoIds.QuartermasterDialog, "Bram Kettle, Quartermaster",
                "If you are walking north, carry cures and charms. The Wilderward does not care how heroic your pockets feel.",
                "Stonewake can keep you supplied, but the roads need reopening before trade feels normal again.");

        DialogData BuildRunnerDialog() =>
            CreateRuntimeDialog(PhaseTwoIds.RunnerDialog, "Nia Reed, Marsh Runner",
                "The basin boardwalks still hold if you step where the reeds bend. Follow my markers and you will keep your boots.",
                "I cleared one hazard already. The next is yours: prove the northern road can carry more than rumors.");

        DialogData BuildForemanDialog() =>
            CreateRuntimeDialog(PhaseTwoIds.ForemanDialog, "Orlo Flint, Quarry Foreman",
                "Ironroot has been shaking like something underneath learned to breathe.",
                "Win a few fights among the stone nests and I will believe this is a monster problem, not the hill itself waking up.");

        DialogData BuildEthicistDialog() =>
            CreateRuntimeDialog(PhaseTwoIds.EthicistDialog, "Thren, Monster Ethicist",
                "The archive network does not merely record monsters. It nudges them, remembers them, and perhaps edits what they become.",
                "Before Hollowfen binds that power, someone needs to ask whether the monsters agreed.");

        DialogData BuildMoonwellDialog() =>
            CreateRuntimeDialog(PhaseTwoIds.MoonwellDialog, "Luma, Moonwell Keeper",
                "The Moonwell reflects bonds more clearly than faces. Your party leaves ripples before you speak.",
                "If the lore network is pulling monsters out of balance, trust may be the only thing it cannot imitate.");

        DialogData BuildSableDialog() =>
            questManager != null && questManager.IsCompleted(PhaseTwoIds.BindingChoiceQuest)
                ? CreateRuntimeDialog(PhaseTwoIds.SableDialog, "Sable, Wandering Rival",
                    "You made your choice. I still do not know if it was right, but at least it was yours.")
                : CreateRuntimeDialog(PhaseTwoIds.SableDialog, "Sable, Wandering Rival",
                    "Everyone in the Wilderward wants you to choose carefully. I want to know if you can choose under pressure.",
                    "Meet me on Tideglass Crossing and prove your conviction can survive a real fight.");

        DialogData LoadDialogOrFallback(string dialogId, System.Func<DialogData> fallback)
        {
            foreach (var asset in Resources.LoadAll<DialogData>("Dialogs"))
                if (asset != null && asset.DialogId == dialogId)
                    return asset;
            return fallback();
        }

        DialogData CreateRuntimeDialog(string id, string speaker, params string[] lines)
        {
            var data = ScriptableObject.CreateInstance<DialogData>();
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;
            var entries = new List<DialogEntry>();
            foreach (var line in lines)
                entries.Add(new DialogEntry { speaker = speaker, line = line });
            data.Configure(id, entries.ToArray());
            return data;
        }

        string ResolveAreaTarget(Vector2 position)
        {
            var target = WorldMapLayout.ResolveAreaId(position);
            return IsAreaUnlocked(target) ? target : NearestUnlockedArea(position);
        }

        string NearestUnlockedArea(Vector2 position)
        {
            var best = DefaultGameContent.TownId;
            var bestDistance = float.MaxValue;
            foreach (var region in WorldMapLayout.All)
            {
                if (!IsAreaUnlocked(region.AreaId)) continue;
                var d = Vector2.SqrMagnitude(region.SpawnPoint - position);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = region.AreaId;
            }

            return best;
        }

        bool IsAreaUnlocked(string areaId)
        {
            if (string.IsNullOrEmpty(areaId)) return false;
            if (areaId == DefaultGameContent.MarshId) return IsChapterTwoUnlocked();
            if (areaId == DefaultGameContent.RuinsId) return IsRuinsUnlocked();
            if (areaId == DefaultGameContent.DeltaId) return IsChapterThreeUnlocked();
            if (areaId == DefaultGameContent.RidgeId) return IsRidgeUnlocked();
            if (areaId == DefaultGameContent.SpireId) return IsSpireUnlocked();
            var region = WorldMapLayout.Get(areaId);
            return !region.PhaseTwo || IsPhaseTwoUnlocked();
        }

        bool IsChapterTwoUnlocked() => CampaignChapterGates.IsChapterTwoUnlocked(questManager);

        bool IsRuinsUnlocked() => CampaignChapterGates.IsRuinsUnlocked(questManager);

        bool IsChapterThreeUnlocked() => CampaignChapterGates.IsChapterThreeUnlocked(questManager);

        bool IsRidgeUnlocked() => CampaignChapterGates.IsRidgeUnlocked(questManager);

        bool IsSpireUnlocked() => CampaignChapterGates.IsSpireUnlocked(questManager);

        bool IsPhaseTwoUnlocked() => CampaignChapterGates.IsPhaseTwoUnlocked(questManager);

        void RefreshChapterTwoNpcState()
        {
            OverworldCampaignRefresher.RefreshChapterTwoNpcState(questManager, archivist, rival);
        }

        void RefreshChapterThreeNpcState()
        {
            OverworldCampaignRefresher.RefreshChapterThreeNpcState(
                questManager, warden, collector, rumorKeeper, mentor, stormBoss);
        }

        void RefreshPhaseTwoNpcState()
        {
            OverworldCampaignRefresher.RefreshPhaseTwoNpcState(
                questManager, cartographer, quartermaster, runner, foreman, ethicist, moonwellKeeper, sable, rival);
        }

        string BuildRouteSummary()
        {
            var nodes = BuildRouteNodeLabels();
            return string.Join(" -> ", nodes);
        }

        string BuildRouteSummaryCompact()
        {
            var nodes = BuildRouteNodeLabels();
            if (worldManager == null || nodes.Length == 0)
                return string.Join(" → ", nodes);

            var regions = WorldMapLayout.All;

            var idx = -1;
            for (var i = 0; i < regions.Count; i++)
            {
                if (worldManager.CurrentAreaId != regions[i].AreaId) continue;
                idx = i;
                break;
            }

            if (idx < 0)
                return string.Join(" → ", nodes);

            const int before = 2;
            const int after = 3;
            var start = Mathf.Max(0, idx - before);
            var end = Mathf.Min(nodes.Length, idx + after + 1);
            var parts = new List<string>();
            for (var i = start; i < end; i++)
                parts.Add(nodes[i]);
            var head = start > 0 ? "… → " : "";
            var tail = end < nodes.Length ? " → …" : "";
            return head + string.Join(" → ", parts) + tail;
        }

        string[] BuildRouteNodeLabels()
        {
            var regions = WorldMapLayout.All;
            var labels = new string[regions.Count];
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                labels[i] = BuildRouteNode(region.AreaId, region.ShortLabel, IsAreaUnlocked(region.AreaId));
            }

            return labels;
        }

        string BuildRouteHint()
        {
            if (worldManager == null || questManager == null) return string.Empty;
            var areaId = worldManager.CurrentAreaId;
            var area = worldManager.GetCurrentArea();
            if (areaId == DefaultGameContent.GroveId && !IsChapterTwoUnlocked())
                return "Push east to finish Hollowfen's grove crisis. Stay on roads to limit random encounters.";
            if (areaId == DefaultGameContent.GroveId && IsChapterTwoUnlocked())
                return "The marsh path is now open beyond the grove.";
            if (areaId == DefaultGameContent.MarshId && !IsRuinsUnlocked())
                return "Find Archivist Sel to open the archive path. Reeds mark danger patches off the road.";
            if (areaId == DefaultGameContent.MarshId && IsRuinsUnlocked())
                return "Press east into the Sunken Archive.";
            if (areaId == DefaultGameContent.RuinsId)
                return IsChapterThreeUnlocked()
                    ? "The road beyond the archive is open. Push east into the Flooded Delta."
                    : "Search the archive depths and settle the rivalry here.";
            if (areaId == DefaultGameContent.DeltaId && !IsRidgeUnlocked())
                return "Find Warden Neris and stabilize the crossings toward Stormbreak Ridge.";
            if (areaId == DefaultGameContent.DeltaId && IsRidgeUnlocked())
                return "The ridge ascent is open. Keep pushing east.";
            if (areaId == DefaultGameContent.RidgeId && !IsSpireUnlocked())
                return "Train with Cael and prove your party can survive the climb. Keep cures ready.";
            if (areaId == DefaultGameContent.RidgeId && IsSpireUnlocked())
                return "Skyglass Spire is now reachable. Prepare for the final ascent.";
            if (areaId == DefaultGameContent.SpireId)
                return "Break the relic storm at the summit and confront Varo.";
            if (areaId == DefaultGameContent.StonewakeId)
                return "Meet Jessa and use Stonewake as your northern base camp.";
            if (areaId == DefaultGameContent.MarshBasinId)
                return "Nia's boardwalk routes point toward Moonwell and Starfall.";
            if (areaId == DefaultGameContent.MoonwellId)
                return "Speak with Luma and learn what monster bonds reveal.";
            if (areaId == DefaultGameContent.QuarryId)
                return "Win quarry battles and help Orlo understand the tremors.";
            if (areaId == DefaultGameContent.CrossingId)
                return "Tideglass is where Sable tests your Phase 2 conviction.";
            if (areaId == DefaultGameContent.StarfallId)
                return "Thren is studying the hollow signal at the ruin mouth.";
            if (area != null && !string.IsNullOrWhiteSpace(area.TravelHint))
                return area.TravelHint;
            return "Follow your current story objective through the Wilderward.";
        }

        string BuildRouteNode(string areaId, string label, bool unlocked)
        {
            if (worldManager != null && worldManager.CurrentAreaId == areaId)
                return $"[{label}]";
            return unlocked ? label : "Locked";
        }

        static string BuildAreaArrivalStatus(string areaId)
        {
            var layout = WorldMapLayout.Get(areaId);
            var dangerZones = WorldMapLayout.EncounterZones(areaId).Length;
            var dangerNote = dangerZones <= 0
                ? "No nearby danger patches."
                : dangerZones == 1
                    ? "One danger patch nearby."
                    : $"{dangerZones} danger patches nearby.";
            return $"Entered {layout.ShortLabel}. {dangerNote} Roads remain the safest route.";
        }

        bool _minimalVisualsApplied;
        Transform overworldBackdropRoot;

        void EnsureMinimalWorldVisuals()
        {
            if (_minimalVisualsApplied) return;
            _minimalVisualsApplied = true;

            overworldBackdropRoot = OverworldPixelVisuals.Build(transform);

            if (player != null)
            {
                OverworldCharacterVisuals.AddPlayer(player.gameObject);
                RefreshPlayerGearVisuals();
            }

            OverworldCharacterVisuals.AddNpc(elder);
            OverworldCharacterVisuals.AddNpc(merchant);
            OverworldCharacterVisuals.AddNpc(healer);
            OverworldCharacterVisuals.AddNpc(tailor);
            OverworldCharacterVisuals.AddNpc(scout);
            OverworldCharacterVisuals.AddNpc(boss);
            OverworldCharacterVisuals.AddNpc(archivist);
            OverworldCharacterVisuals.AddNpc(rival);
            OverworldCharacterVisuals.AddNpc(warden);
            OverworldCharacterVisuals.AddNpc(mentor);
            OverworldCharacterVisuals.AddNpc(stormBoss);
            OverworldCharacterVisuals.AddNpc(collector);
            OverworldCharacterVisuals.AddNpc(rumorKeeper);
            OverworldCharacterVisuals.AddNpc(cartographer);
            OverworldCharacterVisuals.AddNpc(quartermaster);
            OverworldCharacterVisuals.AddNpc(runner);
            OverworldCharacterVisuals.AddNpc(foreman);
            OverworldCharacterVisuals.AddNpc(ethicist);
            OverworldCharacterVisuals.AddNpc(moonwellKeeper);
            OverworldCharacterVisuals.AddNpc(sable);
        }

        void UpdateCameraFollow()
        {
            if (player == null) return;
            mainCamera ??= Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (mainCamera == null || !mainCamera.orthographic) return;

            mainCamera.backgroundColor = GameVisualTheme.SkyTop;
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, 3.55f, 4f * Time.deltaTime);
            var halfWidth = mainCamera.orthographicSize * Mathf.Max(1f, mainCamera.aspect);
            var bounds = WorldMapLayout.WorldBounds();
            var camMinX = bounds.xMin + halfWidth;
            var camMaxX = bounds.xMax - halfWidth;
            var halfHeight = mainCamera.orthographicSize;
            var camMinY = bounds.yMin + halfHeight;
            var camMaxY = bounds.yMax - halfHeight;
            if (camMaxX < camMinX)
                camMaxX = camMinX;
            if (camMaxY < camMinY)
                camMaxY = camMinY;

            var target = player.transform.position + new Vector3(1.2f, 0.35f, 0f);
            var camPos = mainCamera.transform.position;
            camPos.x = Mathf.Clamp(target.x, camMinX, camMaxX);
            camPos.y = Mathf.Clamp(target.y, camMinY, camMaxY);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, camPos, 6f * Time.deltaTime);
        }

        void UpdateBattleWorldVisibility()
        {
            if (combat == null) return;
            var hide = combat.IsBattleActive;
            if (overworldBackdropRoot == null && _minimalVisualsApplied)
                overworldBackdropRoot = transform.Find("OverworldBackdrop");
            if (overworldBackdropRoot != null)
            {
                var showBackdrop = !hide;
                if (overworldBackdropRoot.gameObject.activeSelf != showBackdrop)
                    overworldBackdropRoot.gameObject.SetActive(showBackdrop);
            }

            if (player != null)
            {
                var showPlayer = !hide;
                if (player.gameObject.activeSelf != showPlayer)
                    player.gameObject.SetActive(showPlayer);
            }
        }
    }

}
