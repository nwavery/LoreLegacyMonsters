using UnityEngine;

namespace LoreLegacyMonsters.Dialogue
{
    [CreateAssetMenu(menuName = "LLM/Dialogue Graph", fileName = "DialogueData")]
    public class DialogueData : ScriptableObject
    {
        [SerializeField] string dialogueId;
        [SerializeField] DialogueNode startNode;

        public string DialogueId => dialogueId;
        public DialogueNode StartNode => startNode;
    }
}
