namespace LoreLegacyMonsters
{
    public static class InventorySystemExtensions
    {
        public static void ClearAll(this InventorySystem inv)
        {
            inv?.LoadFromSave(null);
        }
    }
}
