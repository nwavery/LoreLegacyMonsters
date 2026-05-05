using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters.Shop
{
    [CreateAssetMenu(menuName = "LLM/Shop", fileName = "ShopData")]
    public class ShopData : ScriptableObject
    {
        [SerializeField] string shopId;
        [SerializeField] List<ShopItem> stock = new List<ShopItem>();

        public string ShopId => shopId;
        public IReadOnlyList<ShopItem> Stock => stock;

        public void Configure(string id)
        {
            shopId = id;
            stock ??= new List<ShopItem>();
            stock.Clear();
        }

        public void AddListing(string itemId, int price, int quantity = 99)
        {
            stock ??= new List<ShopItem>();
            stock.Add(new ShopItem { itemId = itemId, price = price, stock = quantity });
        }
    }
}
