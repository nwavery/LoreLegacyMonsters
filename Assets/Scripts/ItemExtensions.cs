namespace LoreLegacyMonsters
{
    public static partial class ItemExtensions
    {
        public static Item Clone(this Item i) =>
            i == null ? null : new Item { itemId = i.itemId, quantity = i.quantity };

        public static bool IsEmpty(this Item i) =>
            i == null || string.IsNullOrEmpty(i.itemId) || i.quantity <= 0;
    }
}
