using System;

namespace LoreLegacyMonsters.Dialogue
{
    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string text;
        public DialogueResponse[] responses;
    }
}
