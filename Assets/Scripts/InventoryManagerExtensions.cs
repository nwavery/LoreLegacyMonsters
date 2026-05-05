namespace LoreLegacyMonsters
{
    public static class InventoryManagerExtensions
    {
        public static bool TryAdd(this InventoryManager m, string itemId, int qty) =>
            m != null && m.Inventory != null && m.Inventory.AddItem(itemId, qty);
    }
}
