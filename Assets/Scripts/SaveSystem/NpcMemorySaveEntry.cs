using System;

namespace LoreLegacyMonsters.SaveSystem
{
    [Serializable]
    public class NpcMemorySaveEntry
    {
        public string npcId;
        public int relationshipTier;
        public int conversationCount;
        public string lastSeenAreaId;
        public string lastTopic;
        public string memorySummary;
        public string lastPlayerMessage;
        public string lastNpcReply;
        /// <summary>Rolling window (max 6); older saves leave this null.</summary>
        public NpcMemoryTurn[] recentTurns;
    }
}
