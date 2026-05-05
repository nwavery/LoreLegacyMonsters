namespace LoreLegacyMonsters.Shop
{
    public static class ShopItemExtensions
    {
        public static bool InStock(this ShopItem s) => s != null && s.stock > 0;
    }
}
