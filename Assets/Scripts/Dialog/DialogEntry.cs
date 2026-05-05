using System;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog
{
    [Serializable]
    public class DialogEntry
    {
        public string speaker;
        [TextArea] public string line;
        public string[] choiceLabels;
        public string[] choiceNextIds;
    }
}
