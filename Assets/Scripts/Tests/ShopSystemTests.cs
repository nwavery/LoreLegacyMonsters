using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Shop;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class ShopSystemTests
    {
        [Test]
        public void ShopManager_TryBuy_SpendsGold()
        {
            var go = new GameObject("shop");
            var shop = go.AddComponent<ShopManager>();
            var invGo = new GameObject("inv");
            var inv = invGo.AddComponent<InventorySystem>();
            int gold = 50;
            Assert.IsTrue(shop.TryBuy(inv, "potion", 10, ref gold));
            Assert.AreEqual(40, gold);
            Assert.AreEqual(1, inv.Count("potion"));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(invGo);
        }
    }
}
