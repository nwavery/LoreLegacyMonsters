using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using UnityEngine;

namespace LoreLegacyMonsters.Shop
{
    public static class ShopPriceResolver
    {
        public static int UnitPriceForListing(AssetRegistryManager reg, ShopItem row)
        {
            if (row == null) return 0;
            if (reg == null) return Mathf.Max(0, row.price);
            var item = reg.GetItem(row.itemId);
            if (item is GearItemData gear)
                return Mathf.Max(0, Mathf.RoundToInt(row.price * gear.Rarity.PriceMultiplier()));
            return Mathf.Max(0, row.price);
        }
    }
}
