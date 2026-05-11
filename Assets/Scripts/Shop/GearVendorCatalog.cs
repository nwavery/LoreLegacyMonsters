using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using System.Collections.Generic;

namespace LoreLegacyMonsters.Shop
{
    /// <summary>Wandering Tailor stock: rotates with campaign chapter gates; base prices are scaled by <see cref="Inventory.RarityExtensions.PriceMultiplier"/> at purchase.</summary>
    public static class GearVendorCatalog
    {
        public static void Replenish(ShopData shop, QuestManager quests)
        {
            if (shop == null || quests == null) return;
            var priorStock = CapturePriorStock(shop);
            shop.Configure(DefaultGameContent.GearShopId);

            // Chapter 1 (always once first hunt exists / early game stays rich)
            AddListing(shop, priorStock, DefaultGameContent.GearOutfitScholarCoatId, 28, 6);
            AddListing(shop, priorStock, "gear_outfit_traveler_vest", 22, 8);
            AddListing(shop, priorStock, "gear_outfit_guard_coat", 24, 6);
            AddListing(shop, priorStock, DefaultGameContent.GearCharmLuckyFoxboneId, 12, 10);
            AddListing(shop, priorStock, DefaultGameContent.GearCharmCalmingBellId, 14, 8);
            AddListing(shop, priorStock, "gear_charm_plain_loop", 9, 12);

            if (CampaignChapterGates.IsChapterTwoUnlocked(quests))
            {
                AddListing(shop, priorStock, DefaultGameContent.GearOutfitForagerGreensId, 55, 3);
                AddListing(shop, priorStock, "gear_outfit_archive_robes", 48, 3);
                AddListing(shop, priorStock, DefaultGameContent.GearCharmIronTokenId, 32, 4);
                AddListing(shop, priorStock, "gear_charm_solar_pendant", 30, 3);
                AddListing(shop, priorStock, "gear_outfit_marsh_waders", 52, 2);
            }

            if (CampaignChapterGates.IsChapterThreeUnlocked(quests))
            {
                AddListing(shop, priorStock, DefaultGameContent.GearOutfitRoyalMantleId, 95, 2);
                AddListing(shop, priorStock, "gear_outfit_sunstrider", 88, 2);
                AddListing(shop, priorStock, DefaultGameContent.GearOutfitStormwalkerId, 260, 1);
                AddListing(shop, priorStock, "gear_charm_gold_weevil", 120, 1);
                AddListing(shop, priorStock, "gear_charm_star_splinter", 140, 1);
            }
        }

        static Dictionary<string, int> CapturePriorStock(ShopData shop)
        {
            var stock = new Dictionary<string, int>();
            if (shop?.Stock == null) return stock;
            for (var i = 0; i < shop.Stock.Count; i++)
            {
                var row = shop.Stock[i];
                if (row == null || string.IsNullOrEmpty(row.itemId)) continue;
                stock[row.itemId] = row.stock;
            }

            return stock;
        }

        static void AddListing(ShopData shop, IReadOnlyDictionary<string, int> priorStock, string itemId, int price,
            int quantity)
        {
            var qty = priorStock != null && priorStock.TryGetValue(itemId, out var remaining) ? remaining : quantity;
            shop.AddListing(itemId, price, qty);
        }
    }
}
