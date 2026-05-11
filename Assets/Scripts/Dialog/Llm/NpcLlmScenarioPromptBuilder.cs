using System;
using System.Globalization;
using LoreLegacyMonsters;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class NpcLlmScenarioPromptBuilder
    {
        public struct Sampling
        {
            public float temperature;
            public int maxTokens;
        }

        public static Sampling ResolveSampling(string temperatureCsv, string maxTokensCsv)
        {
            var asset = NpcLlmSettings.LoadFromResources();
            var sampling = new Sampling
            {
                temperature = asset != null ? asset.Temperature : 0.45f,
                maxTokens = asset != null ? asset.MaxTokens : 256
            };

            if (!string.IsNullOrWhiteSpace(temperatureCsv) &&
                float.TryParse(temperatureCsv.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var tf))
                sampling.temperature = tf;

            if (!string.IsNullOrWhiteSpace(maxTokensCsv) &&
                int.TryParse(maxTokensCsv.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var imx) &&
                imx >= 16)
                sampling.maxTokens = imx;

            return sampling;
        }

        public static NpcLlmPromptContext ToPromptContext(NpcLlmScenarioRecord s)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.id))
                throw new ArgumentException("scenario id required", nameof(s));

            NormalizeStrings(s);

            if (!Enum.TryParse<NpcRole>(s.npcRole, true, out var role))
                role = NpcRole.Ambient;

            if (string.IsNullOrWhiteSpace(s.roleName))
                s.roleName = role.ToString();

            return new NpcLlmPromptContext
            {
                NpcId = s.npcId,
                DisplayName = s.displayName,
                RoleName = s.roleName,
                Role = role,
                CharacterInstructions = s.characterInstructions,
                IdentitySummary = s.identitySummary,
                PlayerMessage = string.IsNullOrWhiteSpace(s.playerMessage) ? null : s.playerMessage.Trim(),
                GameStateSummary = s.gameStateSummary,
                QuestSummary = s.questSummary,
                InventorySummary = s.inventorySummary,
                PartySummary = s.partySummary,
                WeatherSummary = s.weatherSummary,
                NpcMemorySummary = s.npcMemorySummary,
                ConversationHistorySummary = s.conversationHistorySummary,
                StoryStateSummary = s.storyStateSummary,
                StatusEffectsSummary = s.statusEffectsSummary,
                ShopStockSummary = s.shopStockSummary,
                PlayerGearSummary = string.IsNullOrWhiteSpace(s.playerGearSummary) ? "" : s.playerGearSummary,
                PlayerVibeTags = string.IsNullOrWhiteSpace(s.playerVibeTags) ? "" : s.playerVibeTags,
            };
        }

        static void NormalizeStrings(NpcLlmScenarioRecord s)
        {
            if (string.IsNullOrWhiteSpace(s.conversationHistorySummary)) s.conversationHistorySummary = "none";
            if (string.IsNullOrWhiteSpace(s.npcRole)) s.npcRole = NpcRole.Ambient.ToString();
            if (string.IsNullOrWhiteSpace(s.npcId)) s.npcId = "scenario_npc";
            if (string.IsNullOrWhiteSpace(s.displayName)) s.displayName = "NPC";
            if (string.IsNullOrWhiteSpace(s.characterInstructions))
                s.characterInstructions = "Be concise and grounded; stay fully in-character.";
            if (string.IsNullOrWhiteSpace(s.identitySummary)) s.identitySummary = "Hollowfen region NPC.";
            if (string.IsNullOrWhiteSpace(s.gameStateSummary)) s.gameStateSummary = "area: outskirts of Hollowfen";
            if (string.IsNullOrWhiteSpace(s.questSummary)) s.questSummary = "Primary story and side tasks as stated in briefing.";
            if (string.IsNullOrWhiteSpace(s.inventorySummary)) s.inventorySummary = "basic caravan supplies";
            if (string.IsNullOrWhiteSpace(s.partySummary)) s.partySummary = "Standard party-ready line-up.";
            if (string.IsNullOrWhiteSpace(s.weatherSummary)) s.weatherSummary = "Unsettled frontier weather.";
            if (string.IsNullOrWhiteSpace(s.npcMemorySummary)) s.npcMemorySummary = "none yet this session.";
            if (string.IsNullOrWhiteSpace(s.storyStateSummary)) s.storyStateSummary = "progression:scenario";
            if (string.IsNullOrWhiteSpace(s.statusEffectsSummary)) s.statusEffectsSummary = "none notable on road stock.";
            if (string.IsNullOrWhiteSpace(s.shopStockSummary)) s.shopStockSummary = "Town-standard supplies.";
        }
    }
}
