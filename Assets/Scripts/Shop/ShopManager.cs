using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.Shop
{
    public class ShopManager : MonoBehaviour
    {
        [SerializeField] ShopData currentShop;

        public ShopData Current => currentShop;

        public void SetShop(ShopData shop) => currentShop = shop;

        /// <summary>Uses <see cref="GameManager.PlayerGold"/> (single economy source).</summary>
        public bool TryBuy(InventorySystem inv, string itemId, int unitPrice)
        {
            var gm = GameManager.Instance;
            if (gm == null || inv == null || gm.PlayerGold < unitPrice) return false;
            gm.PlayerGold -= unitPrice;
            inv.AddItem(itemId, 1);
            GameEvents.RaiseGoldChanged(gm.PlayerGold);
            return true;
        }

        /// <summary>Tests / alternate wallets without a live GameManager.</summary>
        public bool TryBuy(InventorySystem inv, string itemId, int unitPrice, ref int playerGold)
        {
            if (inv == null || playerGold < unitPrice) return false;
            playerGold -= unitPrice;
            inv.AddItem(itemId, 1);
            return true;
        }
    }
}
