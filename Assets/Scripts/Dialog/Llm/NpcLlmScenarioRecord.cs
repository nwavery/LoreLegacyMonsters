using System;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>Serializable scenario line consumed by CLI/batch manifests (Unity <c>JsonUtility</c>).</summary>
    [Serializable]
    public sealed class NpcLlmScenarioRecord
    {
        public string id;
        public string playerMessage;
        public string conversationHistorySummary;
        public string npcRole;
        public string npcId;
        public string displayName;
        public string roleName;
        public string characterInstructions;
        public string identitySummary;
        public string gameStateSummary;
        public string questSummary;
        public string inventorySummary;
        public string partySummary;
        public string weatherSummary;
        public string npcMemorySummary;
        public string storyStateSummary;
        public string statusEffectsSummary;
        public string shopStockSummary;
        public string playerGearSummary;
        public string playerVibeTags;
        public string temperature;
        public string maxTokens;
        /// <summary>Optional <c>|</c>-separated forbids (case-insensitive contains after compacting whitespace).</summary>
        public string forbidSubstringsPipe;
        /// <summary>Optional <c>|</c>-separate regex patterns; invalid patterns count as evaluator failure.</summary>
        public string forbidRegexPipe;
        /// <summary>Max double-newline-separated blocks; <c>0</c> = skip.</summary>
        public int maxParagraphs;
        public bool allowRawCommandsHud;
    }
}
