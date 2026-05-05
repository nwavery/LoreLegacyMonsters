using System;

namespace LoreLegacyMonsters.Dialogue
{
    [Serializable]
    public class DialogueResponse
    {
        public string text;
        public string nextNodeId;
        public DialogueAction action;
    }
}
