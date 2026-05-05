using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    public enum ItemType
    {
        Consumable,
        Quest,
        Material,
        Equipment,
        Key
    }

    [CreateAssetMenu(menuName = "LLM/Item Data", fileName = "ItemData")]
    public class ItemData : ScriptableObject, IItem
    {
        [SerializeField] string itemId;
        [SerializeField] string displayName;
        [SerializeField] ItemType itemType;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public ItemType Type => itemType;

        public void Configure(string id, string name, ItemType type)
        {
            itemId = id;
            displayName = name;
            itemType = type;
        }
    }
}

