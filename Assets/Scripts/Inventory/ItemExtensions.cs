namespace LoreLegacyMonsters.Inventory
{
    public static class ItemExtensionsInv
    {
        public static bool IsEmpty(this Item i) =>
            i == null || string.IsNullOrEmpty(i.itemId) || i.count <= 0;
    }
}
