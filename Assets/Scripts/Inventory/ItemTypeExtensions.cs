namespace LoreLegacyMonsters.Inventory
{
    public static class ItemTypeExtensions
    {
        public static bool IsSellable(this ItemType t) => t != ItemType.Key && t != ItemType.Quest;
    }
}
