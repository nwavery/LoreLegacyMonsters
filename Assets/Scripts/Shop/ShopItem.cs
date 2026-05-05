using System;

namespace LoreLegacyMonsters.Shop
{
    [Serializable]
    public class ShopItem
    {
        public string itemId;
        public int price;
        public int stock = 99;
    }
}
