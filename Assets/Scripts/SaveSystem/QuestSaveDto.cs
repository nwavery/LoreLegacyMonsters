using System;
using System.Collections.Generic;

namespace LoreLegacyMonsters.SaveSystem
{
    [Serializable]
    public class QuestSaveEntry
    {
        public string questId;
        public List<int> objectiveProgress = new List<int>();
    }
}
