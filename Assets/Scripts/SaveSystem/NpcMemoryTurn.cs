using System;

namespace LoreLegacyMonsters.SaveSystem
{
    [Serializable]
    public class NpcMemoryTurn
    {
        public string areaId;
        public string playerMessage;
        public string npcReply;
        public int turnIndex;
    }
}
