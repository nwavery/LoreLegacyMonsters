namespace LoreLegacyMonsters
{
    public static class InventoryItemExtensions
    {
        public static bool IsValid(this Inventory.InventoryItem i) =>
            i != null && !string.IsNullOrEmpty(i.itemId) && i.quantity > 0;
    }
}
