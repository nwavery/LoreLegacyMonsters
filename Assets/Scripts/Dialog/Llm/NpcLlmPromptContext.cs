using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.Shop;
using System.Text;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>Optional cached scene singletons to avoid per-turn FindFirstObjectByType.</summary>
    public readonly struct NpcLlmPromptSystemRefs
    {
        public readonly Monster.MonsterSystem Monster;
        public readonly QuestManager Quest;
        public readonly InventorySystem Inventory;

        public NpcLlmPromptSystemRefs(Monster.MonsterSystem monster, QuestManager quest, InventorySystem inventory)
        {
            Monster = monster;
            Quest = quest;
            Inventory = inventory;
        }
    }

    /// <summary>Inputs for building NPC chat prompts (compact; no full save blobs).</summary>
    public sealed class NpcLlmPromptContext
    {
        public string NpcId;
        public string DisplayName;
        public string RoleName;
        public string CharacterInstructions;
        public string IdentitySummary;
        public string PlayerMessage;
        public string GameStateSummary;
        public string QuestSummary;
        public string InventorySummary;
        public string PartySummary;
        public string WeatherSummary;
        public string NpcMemorySummary;
        public string ConversationHistorySummary;
        public string StatusEffectsSummary;
        public string ShopStockSummary;
        public string StoryStateSummary;
        /// <summary>Equipped gear one-liner for LLM flavor.</summary>
        public string PlayerGearSummary;
        /// <summary>Comma-separated vibe tags from loadout.</summary>
        public string PlayerVibeTags;
        public NpcRole Role;

        public static NpcLlmPromptContext ForNpc(
            NPCController npc,
            string characterInstructions,
            string playerMessage = null,
            string conversationHistorySummary = null,
            NpcLlmPromptSystemRefs? systemRefs = null)
        {
            var gm = GameManager.Instance;
            var questSummary = gm != null && gm.World != null ? gm.BuildLlmStateSummary() : "GameManager missing; no live state.";
            var monster = systemRefs?.Monster ?? FindMonsterSystem();
            var partySummary = gm != null && gm.Assets != null && monster != null
                ? monster.GetPartySummary(gm.Assets).Replace("\n", " | ")
                : "No party data.";
            var inventorySummary = gm != null ? BuildInventorySummary(gm, systemRefs?.Inventory) : "No inventory data.";
            var memorySummary = gm != null && gm.NpcMemories != null && npc != null
                ? gm.NpcMemories.BuildPromptSummary(npc.NpcId)
                : "No remembered history.";
            var questMgr = systemRefs?.Quest ?? FindQuestManager();
            var statusFx = BuildStatusEffectsSummary(gm, monster);
            var shopStock = BuildShopStockSummary(gm, npc);
            var storyState = BuildStoryStateSummary();
            var gearSummary = gm != null
                ? GearPromptFormatter.EquippedSummary(gm.Assets, gm.Loadout)
                : "Gear unavailable.";
            var vibeTags = gm != null ? GearPromptFormatter.VibeTagsBracketed(gm.Loadout) : "";
            return new NpcLlmPromptContext
            {
                NpcId = npc != null ? npc.NpcId : "npc",
                DisplayName = npc != null ? npc.DisplayName : "NPC",
                RoleName = npc != null ? npc.Role.ToString() : "Ambient",
                Role = npc != null ? npc.Role : NpcRole.Ambient,
                CharacterInstructions = characterInstructions ?? string.Empty,
                IdentitySummary = npc != null ? npc.LlmIdentitySummary : "Unknown NPC.",
                PlayerMessage = playerMessage,
                GameStateSummary = questSummary,
                QuestSummary = gm != null && questMgr != null ? questMgr.GetPrimaryQuestSummary() : "No active quest.",
                InventorySummary = inventorySummary,
                PartySummary = partySummary,
                WeatherSummary = gm != null && gm.Weather != null ? gm.Weather.Current.ToString() : "Unknown weather.",
                NpcMemorySummary = memorySummary,
                ConversationHistorySummary = string.IsNullOrWhiteSpace(conversationHistorySummary)
                    ? "No recent conversation."
                    : conversationHistorySummary.Trim(),
                StatusEffectsSummary = statusFx,
                ShopStockSummary = shopStock,
                StoryStateSummary = storyState,
                PlayerGearSummary = gearSummary,
                PlayerVibeTags = vibeTags
            };
        }

        static string BuildStoryStateSummary()
        {
            var iona = StoryState.GetOutcome(StoryState.IonaOutcomeKey);
            var corin = StoryState.GetOutcome(StoryState.CorinOutcomeKey);
            var varo = StoryState.GetOutcome(StoryState.VaroOutcomeKey);
            var advisor = StoryState.GetAdvisor();
            var ending = StoryState.GetEnding();
            var trust = StoryState.GetMiraTrust();
            var consequence = BuildConsequenceSummary(iona, corin, varo, advisor);
            var knowledge = BuildKnowledgeSummary();
            return $"iona={ValueOrUnknown(iona)}, corin={ValueOrUnknown(corin)}, varo={ValueOrUnknown(varo)}, " +
                   $"advisor={ValueOrUnknown(advisor)}, ending={ending}, mira_trust={trust}, consequence={consequence}, knowledge={knowledge}";
        }

        static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

        static string BuildConsequenceSummary(string iona, string corin, string varo, string advisor)
        {
            if (varo == StoryState.VaroAlly)
                return "varo alliance calmed some phase-two nests and changed local patrol posture";
            if (varo == StoryState.VaroRefuseSpire)
                return "refusing spire control shifted NPC reactions toward restraint";
            if (corin == StoryState.CorinSideWithCorin)
                return "side-with-corin choice split trust in archive decisions";
            if (corin == StoryState.CorinTalkDown)
                return "talking corin down increased support for caution";
            if (iona == StoryState.IonaSpare)
                return "sparing iona made grove NPCs reference mercy";
            if (iona == StoryState.IonaWithdraw)
                return "withdrawing from iona made nearby NPCs react to caution";
            if (!string.IsNullOrWhiteSpace(advisor))
                return $"advisor {advisor} now shapes late-phase recommendations";
            return "major branch consequence pending";
        }

        static string BuildKnowledgeSummary()
        {
            var tags = new StringBuilder();
            if (StoryFlags.HasFlag(StoryState.NetworkAware)) tags.Append("network_aware;");
            if (StoryFlags.HasFlag(StoryState.CorinTruthKnown)) tags.Append("corin_truth_known;");
            if (StoryFlags.HasFlag(StoryState.VaroJournalRead)) tags.Append("varo_journal_read;");
            if (StoryFlags.HasFlag(StoryState.PiaDoorOpenEarly)) tags.Append("pia_door_open_early;");
            if (StoryFlags.HasFlag(StoryState.JessaFormerMiraKnown)) tags.Append("jessa_former_mira_known;");
            return tags.Length > 0 ? tags.ToString().TrimEnd(';') : "baseline_public_facts_only";
        }

        static string BuildStatusEffectsSummary(GameManager gm, Monster.MonsterSystem monster)
        {
            if (gm?.Assets == null || monster == null) return "No party status data.";
            var sb = new StringBuilder();
            for (var i = 0; i < monster.Party.Count; i++)
            {
                var m = monster.Party[i];
                if (m == null || m.persistentStatus == MonsterStatusEffect.None) continue;
                var data = gm.Assets.GetMonster(m.monsterDataId);
                var name = m.GetDisplayName(data);
                var cure = StatusCureCatalog.RecommendedCureDisplayName(gm.Assets, m.persistentStatus);
                sb.Append(name).Append(": ").Append(m.persistentStatus);
                if (!string.IsNullOrEmpty(cure))
                    sb.Append(" (cure item: ").Append(cure).Append(')');
                sb.AppendLine();
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "No persistent status conditions on party.";
        }

        static string BuildShopStockSummary(GameManager gm, NPCController npc)
        {
            var shop = npc?.Shop;
            if (shop == null || shop.Stock == null || shop.Stock.Count == 0)
                return "This NPC has no shop inventory.";
            if (gm?.Assets == null) return "Shop stock unavailable (no registry).";
            var sb = new StringBuilder();
            for (var i = 0; i < shop.Stock.Count; i++)
            {
                var row = shop.Stock[i];
                if (row == null || string.IsNullOrEmpty(row.itemId)) continue;
                var item = gm.Assets.GetItem(row.itemId);
                var label = item != null ? item.DisplayName : row.itemId;
                sb.Append("- ").Append(label).Append(" ").Append(row.price).Append("g, stock ").Append(row.stock)
                    .AppendLine();
            }

            return sb.Length > 0 ? sb.ToString().TrimEnd() : "Empty shop.";
        }

        static InventorySystem FindInventory() => Object.FindFirstObjectByType<InventorySystem>();
        static Monster.MonsterSystem FindMonsterSystem() => Object.FindFirstObjectByType<Monster.MonsterSystem>();
        static QuestManager FindQuestManager() => Object.FindFirstObjectByType<QuestManager>();

        static string BuildInventorySummary(GameManager gm, InventorySystem cachedInventory = null)
        {
            var inv = cachedInventory ?? FindInventory();
            if (inv == null) return "No inventory data.";
            var stacks = inv.GetStacksSnapshot();
            if (stacks.Count == 0) return "Inventory empty.";

            var sb = new StringBuilder();
            for (var i = 0; i < stacks.Count && i < 4; i++)
            {
                var item = stacks[i];
                var data = gm.Assets != null ? gm.Assets.GetItem(item.itemId) : null;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(data != null ? data.DisplayName : item.itemId).Append(" x").Append(item.quantity);
            }

            return sb.ToString();
        }
    }
}
