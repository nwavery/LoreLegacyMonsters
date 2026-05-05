using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters.Dialog.Llm
{
    [Serializable]
    public class NpcMemoryState
    {
        public const int MaxRecentTurns = 6;

        public string npcId;
        public int relationshipTier;
        public int conversationCount;
        public string lastSeenAreaId;
        public string lastTopic;
        public string memorySummary;
        public string lastPlayerMessage;
        public string lastNpcReply;

        readonly List<NpcMemoryTurn> _recentTurns = new List<NpcMemoryTurn>();

        public string RelationshipLabel => relationshipTier switch
        {
            <= -2 => "hostile",
            -1 => "wary",
            0 => "neutral",
            1 => "familiar",
            _ => "trusted"
        };

        public void Record(string areaId, string playerMessage, string npcReply, string topic, int relationshipDelta = 0)
        {
            conversationCount++;
            relationshipTier = Mathf.Clamp(relationshipTier + relationshipDelta, -2, 3);
            lastSeenAreaId = string.IsNullOrWhiteSpace(areaId) ? lastSeenAreaId : areaId;
            lastPlayerMessage = string.IsNullOrWhiteSpace(playerMessage) ? lastPlayerMessage : playerMessage.Trim();
            lastNpcReply = string.IsNullOrWhiteSpace(npcReply) ? lastNpcReply : npcReply.Trim();
            lastTopic = string.IsNullOrWhiteSpace(topic) ? lastTopic : topic.Trim();
            memorySummary = BuildSummary();

            var turn = new NpcMemoryTurn
            {
                areaId = string.IsNullOrWhiteSpace(areaId) ? lastSeenAreaId : areaId.Trim(),
                playerMessage = string.IsNullOrWhiteSpace(playerMessage) ? string.Empty : playerMessage.Trim(),
                npcReply = string.IsNullOrWhiteSpace(npcReply) ? string.Empty : npcReply.Trim(),
                turnIndex = conversationCount
            };
            _recentTurns.Add(turn);
            while (_recentTurns.Count > MaxRecentTurns)
                _recentTurns.RemoveAt(0);
        }

        public string BuildPromptSummary()
        {
            if (string.IsNullOrWhiteSpace(npcId) && string.IsNullOrWhiteSpace(memorySummary))
                return "No remembered history.";

            var sb = new StringBuilder(320);
            sb.Append("relationship: ").Append(RelationshipLabel)
                .Append("; conversations: ").Append(conversationCount)
                .Append("; last_area: ").Append(string.IsNullOrWhiteSpace(lastSeenAreaId) ? "unknown" : lastSeenAreaId)
                .Append("; last_topic: ").Append(string.IsNullOrWhiteSpace(lastTopic) ? "none" : lastTopic);

            if (_recentTurns.Count > 0)
            {
                sb.Append("; recent: ");
                var start = Mathf.Max(0, _recentTurns.Count - 3);
                for (var i = start; i < _recentTurns.Count; i++)
                {
                    var t = _recentTurns[i];
                    if (i > start) sb.Append(" | ");
                    sb.Append("turn ").Append(t.turnIndex).Append(" (")
                        .Append(string.IsNullOrWhiteSpace(t.areaId) ? "?" : t.areaId).Append("): ");
                    sb.Append("player=").Append(Truncate(t.playerMessage, 80)).Append("; npc=").Append(Truncate(t.npcReply, 80));
                }
            }
            else if (!string.IsNullOrWhiteSpace(memorySummary))
            {
                sb.Append("; memory: ").Append(memorySummary);
            }
            else
            {
                sb.Append("; memory: none");
            }

            return sb.ToString();
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ").Replace("\r", " ").Trim();
            return s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";
        }

        public NpcMemorySaveEntry ToSave()
        {
            return new NpcMemorySaveEntry
            {
                npcId = npcId,
                relationshipTier = relationshipTier,
                conversationCount = conversationCount,
                lastSeenAreaId = lastSeenAreaId,
                lastTopic = lastTopic,
                memorySummary = memorySummary,
                lastPlayerMessage = lastPlayerMessage,
                lastNpcReply = lastNpcReply,
                recentTurns = _recentTurns.Count > 0 ? _recentTurns.ToArray() : null
            };
        }

        public static NpcMemoryState FromSave(NpcMemorySaveEntry save)
        {
            if (save == null || string.IsNullOrWhiteSpace(save.npcId)) return null;
            var st = new NpcMemoryState
            {
                npcId = save.npcId,
                relationshipTier = Mathf.Clamp(save.relationshipTier, -2, 3),
                conversationCount = Mathf.Max(0, save.conversationCount),
                lastSeenAreaId = save.lastSeenAreaId,
                lastTopic = save.lastTopic,
                memorySummary = save.memorySummary,
                lastPlayerMessage = save.lastPlayerMessage,
                lastNpcReply = save.lastNpcReply
            };

            if (save.recentTurns != null && save.recentTurns.Length > 0)
            {
                foreach (var t in save.recentTurns)
                {
                    if (t != null)
                        st._recentTurns.Add(t);
                }

                while (st._recentTurns.Count > MaxRecentTurns)
                    st._recentTurns.RemoveAt(0);
            }
            else if (!string.IsNullOrWhiteSpace(save.memorySummary) || !string.IsNullOrWhiteSpace(save.lastNpcReply))
            {
                st._recentTurns.Add(new NpcMemoryTurn
                {
                    areaId = save.lastSeenAreaId,
                    playerMessage = save.lastPlayerMessage,
                    npcReply = save.lastNpcReply,
                    turnIndex = Mathf.Max(1, save.conversationCount)
                });
            }

            return st;
        }

        string BuildSummary()
        {
            var combined = $"{lastTopic}. Player said: {lastPlayerMessage}. You replied: {lastNpcReply}.";
            combined = combined.Replace("\n", " ").Replace("\r", " ").Trim();
            if (combined.Length > 220)
                combined = combined.Substring(0, 220).TrimEnd() + "...";
            return combined;
        }
    }
}
