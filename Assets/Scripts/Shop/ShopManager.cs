using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using UnityEngine;

namespace LoreLegacyMonsters.Shop
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] ShopData currentShop;

        public ShopData Current => currentShop;

        public void SetShop(ShopData shop) => currentShop = shop;

        /// <summary>Uses <see cref="GameManager.PlayerGold"/>; decrements shop stock.</summary>
        public bool TryBuy(InventorySystem inv, ShopData shop, string itemId)
        {
            var gm = GameManager.Instance;
            var reg = gm != null ? gm.Assets : null;
            if (gm == null || inv == null || shop == null || string.IsNullOrEmpty(itemId)) return false;

            var listing = FindListingByItemId(shop, itemId);
            if (listing == null || listing.stock <= 0) return false;

            var unitCharged = ShopPriceResolver.UnitPriceForListing(reg, listing);
            if (gm.PlayerGold < unitCharged) return false;

            gm.PlayerGold -= unitCharged;
            listing.stock--;
            inv.AddItem(itemId, 1);
            GameEvents.RaiseGoldChanged(gm.PlayerGold);
            return true;
        }

        /// <summary>Tests / alternate wallets without a live GameManager.</summary>
        public bool TryBuy(InventorySystem inv, ShopData shop, string itemId, ref int playerGold)
        {
            if (inv == null || shop == null || string.IsNullOrEmpty(itemId)) return false;
            var listing = FindListingByItemId(shop, itemId);
            if (listing == null || listing.stock <= 0) return false;

            var reg = GameManager.Instance != null ? GameManager.Instance.Assets : null;
            var unitCharged = ShopPriceResolver.UnitPriceForListing(reg, listing);
            if (playerGold < unitCharged) return false;
            playerGold -= unitCharged;
            listing.stock--;
            inv.AddItem(itemId, 1);
            return true;
        }

        public static int QuoteUnitPrice(AssetRegistryManager reg, ShopItem listing) =>
            ShopPriceResolver.UnitPriceForListing(reg, listing);

        static ShopItem FindListingByItemId(ShopData shop, string itemId)
        {
            if (shop?.Stock == null) return null;
            for (var i = 0; i < shop.Stock.Count; i++)
            {
                var row = shop.Stock[i];
                if (row != null && row.itemId == itemId)
                    return row;
            }

            return null;
        }

        public static ShopItem FindListing(ShopData shop, string itemId) => FindListingByItemId(shop, itemId);
    }
}
