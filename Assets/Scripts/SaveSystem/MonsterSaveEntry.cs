using System;
using System.Collections.Generic;

namespace LoreLegacyMonsters.SaveSystem
{
    [Serializable]
    public class MonsterSaveEntry
    {
        public string instanceId;
        public string monsterDataId;
        public string nickname;
        public int level = 1;
        public int experience;
        public int currentHp;
        public int status;
        public List<string> learnedMoveIds = new List<string>();
    }
}
