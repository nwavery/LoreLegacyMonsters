using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters.SaveLoad;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters
{
    [DefaultExecutionOrder(50)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public int PlayerGold { get; set; }

        public WeatherSystem Weather => worldWeather;
        public WorldManager World => worldManager;
        public AssetRegistryManager Assets => assetRegistry;
        public NpcMemoryService NpcMemories => npcMemoryService;

        [SerializeField] AssetRegistryManager assetRegistry;
        [SerializeField] ShopManager shopManager;
        [SerializeField] Monster.MonsterSystem monsterSystem;
        [SerializeField] InventorySystem inventorySystem;
        [SerializeField] QuestManager questManager;
        [SerializeField] WorldManager worldManager;
        [SerializeField] AchievementSystem achievementSystem;
        [SerializeField] WeatherSystem worldWeather;
        [SerializeField] NpcMemoryService npcMemoryService;

        LoadoutSystem loadoutSystem;

        SaveCoordinator _saveCoordinator;
        bool gearAchievementHooks;

        public InventorySystem Inventory => inventorySystem;
        public QuestManager Quests => questManager;
        public Monster.MonsterSystem Monsters => monsterSystem;
        public AchievementSystem Achievements => achievementSystem;
        public LoadoutSystem Loadout => loadoutSystem;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureBootstrap();
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            UnregisterGearAchievementHooks();
            Instance = null;
        }

        void Start()
        {
            var sl = SaveLoadManager.Instance != null ? SaveLoadManager.Instance : FindFirstObjectByType<SaveLoadManager>();
            if (sl != null && sl.HasAuthoritativeWorkingCopy)
                ApplySaveToRuntime(sl.WorkingCopy);
            // Main-story intro bootstrap is owned by CampaignProgression (overworld / save restore).
        }

        void EnsureBootstrap()
        {
            monsterSystem ??= FindFirstObjectByType<Monster.MonsterSystem>();
            inventorySystem ??= FindFirstObjectByType<InventorySystem>();
            questManager ??= FindFirstObjectByType<QuestManager>();
            worldManager ??= FindFirstObjectByType<WorldManager>();
            achievementSystem ??= FindFirstObjectByType<AchievementSystem>();
            worldWeather ??= FindFirstObjectByType<WeatherSystem>();
            assetRegistry ??= FindFirstObjectByType<AssetRegistryManager>();
            npcMemoryService ??= FindFirstObjectByType<NpcMemoryService>();
            if (npcMemoryService == null)
                npcMemoryService = gameObject.GetComponent<NpcMemoryService>() ?? gameObject.AddComponent<NpcMemoryService>();
            var shop = shopManager != null ? shopManager : FindFirstObjectByType<ShopManager>();
            shopManager = shop;
            loadoutSystem ??= GetComponent<LoadoutSystem>() ?? gameObject.AddComponent<LoadoutSystem>();
            loadoutSystem.Bind(inventorySystem, assetRegistry);
            DefaultGameContent.RegisterAll(assetRegistry, worldManager, shop);
            StoryQuestPipeline.RegisterAll(questManager);
            RegisterGearAchievementHooks();
        }

        void EnsureSaveCoordinator()
        {
            if (_saveCoordinator != null) return;
            _saveCoordinator = SaveCoordinator.CreateDefault(this, inventorySystem, questManager, worldManager,
                achievementSystem, monsterSystem, worldWeather, npcMemoryService, loadoutSystem);
        }

        public void ApplySaveToRuntime(SaveInfo save)
        {
            if (save == null) return;
            EnsureBootstrap();
            EnsureSaveCoordinator();
            _saveCoordinator.ApplyAll(save);
            GameEvents.RaiseRuntimeRestored();
        }

        /// <summary>Compact facts for local LLM prompts (no full save dump).</summary>
        public string BuildLlmStateSummary()
        {
            var sb = new StringBuilder(256);
            var area = worldManager != null ? worldManager.GetCurrentArea() : null;
            sb.Append("area_id: ").Append(worldManager != null ? worldManager.CurrentAreaId : "unknown");
            if (area != null && !string.IsNullOrWhiteSpace(area.DisplayName))
                sb.Append("; area_name: ").Append(area.DisplayName);
            sb.Append("; weather: ").Append(worldWeather != null ? worldWeather.Current.ToString() : "unknown");
            sb.Append("; gold: ").Append(PlayerGold);
            if (questManager != null)
            {
                sb.Append("; active_quests: ").Append(string.Join(", ", questManager.GetActiveIds()));
                sb.Append("; completed_quests: ").Append(string.Join(", ", questManager.GetCompletedIds()));
                if (questManager.GetActiveIds().Count > 0)
                    sb.Append("; primary_objective: ").Append(questManager.GetPrimaryQuestSummary().Replace("\n", " | "));
            }
            else sb.Append("; quests: unavailable");
            if (monsterSystem != null)
                sb.Append("; party: ").Append(monsterSystem.GetPartySummary(assetRegistry).Replace("\n", " | "));
            if (inventorySystem != null)
                sb.Append("; inventory: ").Append(BuildInventoryHighlights());
            if (loadoutSystem != null && assetRegistry != null)
            {
                sb.Append("; player_gear: ").Append(GearPromptFormatter.EquippedSummary(assetRegistry, loadoutSystem));
                var tags = GearPromptFormatter.VibeTagsBracketed(loadoutSystem);
                if (!string.IsNullOrWhiteSpace(tags))
                    sb.Append("; gear_vibe_tags: ").Append(tags);
            }

            return sb.ToString();
        }

        string BuildInventoryHighlights()
        {
            if (inventorySystem == null) return "unavailable";
            var stacks = inventorySystem.GetStacksSnapshot();
            if (stacks.Count == 0) return "empty";

            var parts = new List<string>();
            for (var i = 0; i < stacks.Count && i < 4; i++)
            {
                var item = stacks[i];
                var data = assetRegistry != null ? assetRegistry.GetItem(item.itemId) : null;
                parts.Add($"{(data != null ? data.DisplayName : item.itemId)} x{item.quantity}");
            }

            return string.Join(", ", parts);
        }

        public void CaptureRuntimeToSave(SaveInfo save)
        {
            if (save == null) return;
            EnsureBootstrap();
            EnsureSaveCoordinator();
            _saveCoordinator.CaptureAll(save);
        }

        void RegisterGearAchievementHooks()
        {
            if (gearAchievementHooks) return;
            gearAchievementHooks = true;
            GameEvents.InventoryItemAdded += OnInventoryItemAddedForGearAchievements;
            if (loadoutSystem != null)
                loadoutSystem.LoadoutChanged += OnLoadoutChangedForGearAchievements;
        }

        void UnregisterGearAchievementHooks()
        {
            if (!gearAchievementHooks) return;
            gearAchievementHooks = false;
            GameEvents.InventoryItemAdded -= OnInventoryItemAddedForGearAchievements;
            if (loadoutSystem != null)
                loadoutSystem.LoadoutChanged -= OnLoadoutChangedForGearAchievements;
        }

        void OnInventoryItemAddedForGearAchievements(string _, int __) =>
            AchievementGearEvaluator.EvaluateFromRuntime(inventorySystem, loadoutSystem, achievementSystem,
                assetRegistry);

        void OnLoadoutChangedForGearAchievements(GearSlot _, int __, string ___, string ____) =>
            AchievementGearEvaluator.EvaluateFromRuntime(inventorySystem, loadoutSystem, achievementSystem,
                assetRegistry);
    }
}
