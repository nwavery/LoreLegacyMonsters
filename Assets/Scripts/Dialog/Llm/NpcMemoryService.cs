using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public class NpcMemoryService : MonoBehaviour
    {
        readonly Dictionary<string, NpcMemoryState> memories = new Dictionary<string, NpcMemoryState>();

        public NpcMemoryState Get(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId)) return null;
            return memories.TryGetValue(npcId, out var state) ? state : null;
        }

        public NpcMemoryState GetOrCreate(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId)) return null;
            if (!memories.TryGetValue(npcId, out var state))
            {
                state = new NpcMemoryState { npcId = npcId };
                memories[npcId] = state;
            }

            return state;
        }

        public void RecordConversation(string npcId, string areaId, string playerMessage, string npcReply, string topic, int relationshipDelta = 0)
        {
            var state = GetOrCreate(npcId);
            state?.Record(areaId, playerMessage, npcReply, topic, relationshipDelta);
        }

        public void ApplySave(List<NpcMemorySaveEntry> saves)
        {
            memories.Clear();
            if (saves == null) return;
            foreach (var save in saves)
            {
                var state = NpcMemoryState.FromSave(save);
                if (state != null)
                    memories[state.npcId] = state;
            }
        }

        public List<NpcMemorySaveEntry> ExportSave()
        {
            var list = new List<NpcMemorySaveEntry>();
            foreach (var pair in memories)
            {
                if (pair.Value != null)
                    list.Add(pair.Value.ToSave());
            }
            return list;
        }

        public string BuildPromptSummary(string npcId)
        {
            var state = Get(npcId);
            return state != null ? state.BuildPromptSummary() : "No remembered history.";
        }
    }
}
