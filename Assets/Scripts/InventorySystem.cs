namespace LoreLegacyMonsters
{
    public partial class InventorySystem
    {
        public bool HasAtLeast(string itemId, int quantity) => Count(itemId) >= quantity;
    }
}
