using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.World;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [SerializeField] InventorySystem inventory;
        [SerializeField] OverworldChapterController controller;

        RectTransform root;
        RectTransform listRoot;
        Text footerText;

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            inventory ??= controller != null ? controller.Inventory : FindFirstObjectByType<InventorySystem>();
            if (UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            var open = UIManager.Instance.IsModalOpen(UiModal.Inventory);
            SetVisible(open);
            if (!open || inventory == null) return;
            root.SetAsLastSibling();

            RebuildInventoryCards();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "InventoryRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 460f), GameVisualTheme.AccentGreen);
            _ = RuntimeUiFactory.CreateModalWindowChrome(root, "Inventory", GameVisualTheme.AccentGreen, "Close [I]",
                () => UIManager.Instance?.SetModalOpen(UiModal.Inventory, false));
            RuntimeUiFactory.CreateText(root, "PackHint", "Field supplies, charms, and cures collected during the journey.", 16,
                TextAnchor.UpperLeft, GameVisualTheme.MutedText, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -52f), new Vector2(-64f, 26f), VerticalWrapMode.Truncate);
            listRoot = RuntimeUiFactory.CreatePanel(root, "ItemsList", GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.16f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(-52f, 280f));
            footerText = RuntimeUiFactory.CreateText(root, "FooterText", "Items are used from battle and party screens in this alpha.", 16,
                TextAnchor.LowerLeft, GameVisualTheme.MutedText, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(32f, 28f), new Vector2(-64f, 34f), VerticalWrapMode.Truncate);
        }

        void RebuildInventoryCards()
        {
            if (listRoot == null) return;
            RuntimeUiFactory.DestroyChildren(listRoot);
            if (inventory == null)
            {
                CreateEmptyState("No inventory system is available.");
                return;
            }
            var stacks = inventory.GetStacksSnapshot();
            if (stacks.Count == 0)
            {
                CreateEmptyState("Your pack is empty. Check shops and quest rewards for supplies.");
                return;
            }

            for (var i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                var displayName = controller != null && controller.Registry != null
                    ? controller.Registry.GetItem(stack.itemId)?.DisplayName ?? stack.itemId
                    : stack.itemId;
                var row = RuntimeUiFactory.CreateListRowCard(listRoot, $"ItemRow_{i}", i, 58f, 626f,
                    out var icon, out var primary, out var secondary);
                row.GetComponent<Image>().color = i % 2 == 0
                    ? GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.95f)
                    : GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.86f);
                icon.GetComponent<Image>().color = ItemAccentColor(stack.itemId);
                primary.text = displayName;
                secondary.text = $"x{stack.quantity}";
            }
        }

        void CreateEmptyState(string message)
        {
            RuntimeUiFactory.CreateCard(listRoot, "EmptyInventory", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.85f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 120f),
                GameVisualTheme.AccentGreen);
            RuntimeUiFactory.CreateText(listRoot, "EmptyInventoryText", message, 18, TextAnchor.MiddleCenter, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-80f, -60f), VerticalWrapMode.Truncate);
        }

        static Color ItemAccentColor(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return GameVisualTheme.Parchment;
            if (itemId.Contains("potion") || itemId.Contains("salve")) return GameVisualTheme.Danger;
            if (itemId.Contains("charm")) return GameVisualTheme.Accent;
            if (itemId.Contains("tonic")) return GameVisualTheme.AccentBlue;
            return GameVisualTheme.AccentGreen;
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
