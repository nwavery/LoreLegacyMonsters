using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Quest Item", fileName = "QuestItemData")]
    public class QuestItemData : ItemData
    {
        [SerializeField] string linkedQuestId;

        public string LinkedQuestId => linkedQuestId;
    }
}
