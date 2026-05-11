using UnityEngine;
using System.Text;
using LoreLegacyMonsters.Shop;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] ShopManager shop;
        [SerializeField] OverworldChapterController controller;

        RectTransform root;
        RectTransform listRoot;
        Text titleText;
        Button closeButton;
        bool _shopListBuilt;
        string _lastShopStockSig = "!";

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            shop ??= controller != null ? controller.Shop : FindFirstObjectByType<ShopManager>();
            if (controller == null || UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            if (titleText == null || listRoot == null)
                return;

            SetVisible(controller.ShopOpen);
            if (controller.ShopOpen)
                root.SetAsLastSibling();
            if (!controller.ShopOpen || shop == null || shop.Current == null)
            {
                _shopListBuilt = false;
                _lastShopStockSig = "!";
                return;
            }

            var stockSig = BuildShopStockSignature(shop.Current);
            if (stockSig != _lastShopStockSig)
            {
                _lastShopStockSig = stockSig;
                _shopListBuilt = false;
            }

            titleText.text = $"Shop - {shop.Current.ShopId}";
            // Rebuild once per open until stock changes (so Buy works and sold-out rows refresh).
            if (!_shopListBuilt)
            {
                RebuildList();
                _shopListBuilt = true;
            }
        }

        static string BuildShopStockSignature(ShopData currentShop)
        {
            if (currentShop?.Stock == null) return "";
            var sb = new StringBuilder(128);
            for (var i = 0; i < currentShop.Stock.Count; i++)
            {
                var row = currentShop.Stock[i];
                if (row == null) continue;
                sb.Append(row.itemId).Append('=').Append(row.stock).Append(':').Append(row.price).Append('|');
            }

            return sb.ToString();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreatePanel(UIManager.Instance.Root.transform, "ShopRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(720f, 460f));
            var chrome = RuntimeUiFactory.CreateModalWindowChrome(root, "Shop", GameVisualTheme.Accent, "Close [Esc]",
                () => controller?.CloseShop());
            titleText = chrome.Title;
            closeButton = chrome.CloseButton;
            listRoot = RuntimeUiFactory.CreatePanel(root, "ListRoot", new Color(0f, 0f, 0f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(-40f, 320f));
        }

        void RebuildList()
        {
            RuntimeUiFactory.DestroyChildren(listRoot);
            var currentShop = shop.Current;
            if (currentShop == null) return;

            for (var i = 0; i < currentShop.Stock.Count; i++)
            {
                var item = currentShop.Stock[i];
                if (item == null) continue;
                var row = RuntimeUiFactory.CreateListRowCard(listRoot, $"Row_{i}", i, 54f, 660f, out _, out var nameLabel,
                    out var priceLabel);
                var name = controller != null && controller.Registry != null
                    ? controller.Registry.GetItem(item.itemId)?.DisplayName ?? item.itemId
                    : item.itemId;
                nameLabel.text = name;
                var q = controller != null ? ShopManager.QuoteUnitPrice(controller.Registry, item) : item.price;
                priceLabel.text = $"{q}g  Stock {item.stock}";
                var button = RuntimeUiFactory.CreateButton(row, "BuyButton", "Buy",
                    new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(100f, 32f));
                var itemId = item.itemId;
                button.onClick.AddListener(() => controller?.BuyShopItem(itemId, 0));
            }
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
