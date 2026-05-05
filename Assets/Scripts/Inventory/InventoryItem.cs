using System;

namespace LoreLegacyMonsters.Inventory
{
    [Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity;
        public ItemData Data { get; set; }
    }
}
