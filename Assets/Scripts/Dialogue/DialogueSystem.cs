using UnityEngine;

namespace LoreLegacyMonsters.Dialogue
{
    public class DialogueSystem : MonoBehaviour
    {
        [SerializeField] DialogueData data;
        DialogueNode current;

        public DialogueNode Current => current;

        public void StartDialogue(DialogueData d)
        {
            data = d;
            current = d != null ? d.StartNode : null;
        }

        public void JumpTo(string nodeId)
        {
            // Simplified: would traverse graph; stub keeps current
            if (data == null) return;
        }
    }
}
