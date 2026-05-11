using System.Text;
using UnityEngine;
using UnityEngine.UI;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters.Audio;

namespace LoreLegacyMonsters.UI
{
    /// <summary>Wardrobe modal: outfit + three charms.</summary>
    public class LoadoutUI : MonoBehaviour
    {
        [SerializeField] OverworldChapterController controller;
        [SerializeField] InventorySystem inventory;

        RectTransform root;
        RectTransform leftColumn;
        RectTransform listRoot;
        RectTransform actionBar;
        Text charmSlotBanner;
        Text detailText;

        string selectedGearId = "";
        int charmTargetSlot;
        string _equipDirtySig = "!";
        string _listDirtySig = "!";

        AssetRegistryManager Registry => controller != null ? controller.Registry : GameManager.Instance?.Assets;

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        LoadoutSystem Lo => GameManager.Instance != null ? GameManager.Instance.Loadout : LoadoutSystem.FindOrResolve();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            inventory ??= controller != null ? controller.Inventory : FindFirstObjectByType<InventorySystem>();
            var ui = UIManager.Instance;
            if (ui == null)
            {
                SetVisible(false);
                return;
            }

            EnsureChrome();
            var open = ui.IsModalOpen(UiModal.Loadout);
            SetVisible(open);
            if (!open || root == null)
            {
                _equipDirtySig = "!";
                _listDirtySig = "!";
                return;
            }

            root.SetAsLastSibling();
            var lo = Lo;
            var equipSig = BuildEquipSig(lo);
            if (equipSig != _equipDirtySig)
            {
                RefreshEquippedColumn();
                _equipDirtySig = equipSig;
            }

            var listSig = BuildListSig();
            if (listSig != _listDirtySig)
            {
                RefreshGearRows();
                _listDirtySig = listSig;
            }

            UpdateCharmBanner();
            UpdateDetail();
        }

        void OnDestroy()
        {
            if (root != null)
                Destroy(root.gameObject);
        }

        void EnsureChrome()
        {
            if (root != null || UIManager.Instance?.Root == null) return;

            charmTargetSlot = 0;

            root = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "LoadoutRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(880f, 520f), GameVisualTheme.AccentBlue);

            RuntimeUiFactory.CreateModalWindowChrome(root, "Loadout", GameVisualTheme.AccentBlue, "Close [G / Esc]",
                () => UIManager.Instance?.SetModalOpen(UiModal.Loadout, false));

            RuntimeUiFactory.CreateText(root, "Blurb", "Gear changes how the world treats you—speed, wealth, wild tempers, and rare finds.",
                14, TextAnchor.UpperLeft, GameVisualTheme.MutedText, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(24f, -44f), new Vector2(-48f, 24f));

            var body = RuntimeUiFactory.CreatePanel(root, "Body", GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.04f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(20f, -76f), new Vector2(-40f, -128f));

            leftColumn = RuntimeUiFactory.CreatePanel(body.transform, "EquippedColumn",
                GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.92f),
                new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(8f, 8f), new Vector2(300f, -16f));

            charmSlotBanner = RuntimeUiFactory.CreateText(leftColumn, "CharmSlotBanner",
                "Next charm goes on hoop 1", 13, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -10f), new Vector2(-20f, 26f));

            var hop = RuntimeUiFactory.CreatePanel(leftColumn, "CharmHop", GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.08f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(10f, -40f), new Vector2(-20f, 36f));
            var hopRt = hop.GetComponent<RectTransform>();
            RuntimeUiFactory.CreateSecondaryActionButton(hopRt, "PrevCharmSlot", "◀ Hoop", new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(130f, 28f)).onClick
                .AddListener(() =>
                {
                    charmTargetSlot = (charmTargetSlot + 2) % 3;
                    AudioManager.EnsureExists().PlayUiSfx(0);
                });
            RuntimeUiFactory.CreateSecondaryActionButton(hopRt, "NextCharmSlot", "Hoop ▶", new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(130f, 28f)).onClick
                .AddListener(() =>
                {
                    charmTargetSlot = (charmTargetSlot + 1) % 3;
                    AudioManager.EnsureExists().PlayUiSfx(0);
                });

            var rightPane = RuntimeUiFactory.CreatePanel(body.transform, "RightPane",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.08f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-308f, 8f),
                new Vector2(-17f, -16f));

            listRoot = RuntimeUiFactory.CreatePanel(rightPane.transform, "GearScroll",
                GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.9f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(12f, -12f),
                new Vector2(-24f, -190f)).GetComponent<RectTransform>();

            actionBar = RuntimeUiFactory.CreatePanel(rightPane.transform, "GearActions",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.06f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(12f, 10f),
                new Vector2(-24f, 168f)).GetComponent<RectTransform>();

            var outfitEquip = RuntimeUiFactory.CreatePrimaryActionButton(actionBar, "ToOutfit", "Wear as outfit",
                new Vector2(0f, 1f), new Vector2(0.48f, 1f), new Vector2(0f, 1f), new Vector2(4f, -6f), new Vector2(-8f, 38f));
            outfitEquip.onClick.AddListener(() =>
            {
                if (TryEquipOutfitSelection())
                    AudioManager.EnsureExists().PlayUiSfx(1);
            });

            var charmEquip = RuntimeUiFactory.CreatePrimaryActionButton(actionBar, "ToCharm", "Attach to hoop",
                new Vector2(0.52f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -6f), new Vector2(-8f, 38f));
            charmEquip.onClick.AddListener(() =>
            {
                if (TryEquipCharmSelection())
                    AudioManager.EnsureExists().PlayUiSfx(1);
            });

            detailText = RuntimeUiFactory.CreateText(actionBar, "Detail", "Select gear from the list.", 12,
                TextAnchor.LowerLeft, GameVisualTheme.Text, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(6f, 6f), new Vector2(-14f, 108f), VerticalWrapMode.Overflow);
        }

        void RefreshEquippedColumn()
        {
            if (leftColumn == null) return;
            var equipHost = leftColumn.Find("EquippedRows");
            if (equipHost == null)
            {
                equipHost = new GameObject("EquippedRows").transform;
                equipHost.SetParent(leftColumn, false);
                var rt = equipHost.gameObject.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(8f, 12f);
                rt.offsetMax = new Vector2(-8f, -86f);
            }

            RuntimeUiFactory.DestroyChildren(equipHost);
            var lo = Lo;
            if (lo == null) return;

            float y = -4f;
            y = DrawEquippedCard(equipHost, "Outfit", lo.OutfitEquippedId, y, () => lo.TryUnequipOutfit());
            for (var i = 0; i < 3; i++)
            {
                var ci = i;
                y = DrawEquippedCard(equipHost, $"Charm {ci + 1}", lo.GetCharmEquippedId(ci), y,
                    () => lo.TryUnequipCharm(ci));
            }
        }

        float DrawEquippedCard(Transform host, string label, string itemId, float yOffset, System.Action unequip)
        {
            var reg = Registry;
            var gear = !string.IsNullOrEmpty(itemId) && reg != null ? reg.GetItem(itemId) as GearItemData : null;
            var accent = gear != null ? gear.Rarity.AccentColor() : GameVisualTheme.InkSoft;

            var shell = RuntimeUiFactory.CreatePanel(host, $"{label}Card",
                GameVisualTheme.WithAlpha(accent, 0.45f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, yOffset), new Vector2(-6f, 92f)).transform;

            var inner = RuntimeUiFactory.CreatePanel(shell, "Inner", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.95f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(2f, 2f), new Vector2(-4f, -4f)).transform;

            RuntimeUiFactory.CreateText(inner, "Lbl", label, 11, TextAnchor.UpperLeft, GameVisualTheme.MutedText,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(4f, -3f), new Vector2(-8f, 16f));

            RuntimeUiFactory.CreateText(inner, "Nm",
                gear != null ? gear.DisplayName : string.IsNullOrEmpty(itemId) ? "(none)" : itemId,
                14, TextAnchor.UpperLeft, GameVisualTheme.Text, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -22f), new Vector2(-8f, 30f));

            var un = RuntimeUiFactory.CreateSecondaryActionButton(inner, "Unequip", "Clear",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -6f), new Vector2(86f, 24f));
            un.onClick.AddListener(() =>
            {
                unequip?.Invoke();
                AudioManager.EnsureExists().PlayUiSfx(2);
            });

            return yOffset - 98f;
        }

        void RefreshGearRows()
        {
            if (listRoot == null || inventory == null) return;
            RuntimeUiFactory.DestroyChildren(listRoot);
            var reg = Registry;
            var stacks = inventory.GetStacksSnapshot();
            var row = 0;
            for (var i = 0; i < stacks.Count; i++)
            {
                var stack = stacks[i];
                if (reg == null || reg.GetItem(stack.itemId) is not GearItemData gear) continue;

                var line = RuntimeUiFactory.CreateListRowCard(listRoot, $"GearLine_{row}", row, 50f, 520f,
                    out var icon, out var primary, out var secondary);
                line.GetComponent<Image>().color = row % 2 == 0
                    ? GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.95f)
                    : GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.88f);
                icon.GetComponent<Image>().color = gear.Rarity.AccentColor();
                primary.text = gear.DisplayName;
                secondary.text = $"{gear.Slot} | {gear.Rarity.Label()} | {DescribeEffectShort(gear)}";

                var capture = stack.itemId;
                var btn = line.gameObject.AddComponent<Button>();
                btn.targetGraphic = line.GetComponent<Image>();
                btn.onClick.AddListener(() =>
                {
                    selectedGearId = capture;
                    UpdateDetail();
                });
                row++;
            }

            if (row == 0)
                RuntimeUiFactory.CreateText(listRoot, "EmptyGear", "No clothes or charms in your inventory yet.",
                    15, TextAnchor.MiddleCenter, GameVisualTheme.MutedText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(-24f, -40f));
        }

        void UpdateCharmBanner()
        {
            if (charmSlotBanner != null)
                charmSlotBanner.text = $"Next charm attachment targets hoop {charmTargetSlot + 1}.";
        }

        void UpdateDetail()
        {
            if (detailText == null) return;
            var reg = Registry;
            if (string.IsNullOrEmpty(selectedGearId) || reg == null || reg.GetItem(selectedGearId) is not GearItemData gear)
            {
                detailText.text = "Tap a gear row to inspect bonuses and flavor.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"{gear.DisplayName} ({gear.Rarity.Label()})");
            if (gear.VibeTags is { Count: > 0 })
                sb.AppendLine($"Tags: {string.Join(", ", gear.VibeTags)}");
            foreach (var fx in gear.Effects)
                sb.AppendLine(DescribeEffect(fx));
            if (!string.IsNullOrWhiteSpace(gear.FlavorText))
                sb.Append($"\"{gear.FlavorText}\"");
            detailText.text = sb.ToString();
        }

        bool TryEquipOutfitSelection()
        {
            var lo = Lo;
            var reg = Registry;
            if (lo == null || string.IsNullOrEmpty(selectedGearId) || reg?.GetItem(selectedGearId) is not GearItemData g)
            {
                GameEvents.RaiseToast("Select an outfit-capable gear piece.");
                return false;
            }

            if (g.Slot != GearSlot.Outfit)
            {
                GameEvents.RaiseToast("That's a charm, not an outfit.");
                return false;
            }

            var ok = lo.TryEquip(selectedGearId);
            if (!ok)
                GameEvents.RaiseToast("Equip failed—not enough duplicates or mismatched inventory.");
            return ok;
        }

        bool TryEquipCharmSelection()
        {
            var lo = Lo;
            var reg = Registry;
            if (lo == null || string.IsNullOrEmpty(selectedGearId) || reg?.GetItem(selectedGearId) is not GearItemData g)
            {
                GameEvents.RaiseToast("Select a charm piece.");
                return false;
            }

            if (g.Slot != GearSlot.Charm)
            {
                GameEvents.RaiseToast("That's an outfit, not a hoop charm.");
                return false;
            }

            var ok = lo.TryEquip(selectedGearId, Mathf.Clamp(charmTargetSlot, 0, 2));
            if (!ok)
                GameEvents.RaiseToast("Can't attach that charm (inventory limits).");
            return ok;
        }

        static string DescribeEffectShort(GearItemData gear)
        {
            if (gear == null || gear.Effects.Count == 0) return "vibes";
            return DescribeEffect(gear.Effects[0]);
        }

        static string DescribeEffect(GearEffect fx)
        {
            switch (fx.Kind)
            {
                case GearEffectKind.MoveSpeedMult: return $"SPD x{fx.Magnitude:0.##}";
                case GearEffectKind.EncounterRateMult: return $"Enc x{fx.Magnitude:0.##}";
                case GearEffectKind.MonsterAggressionMult: return $"Aggro x{fx.Magnitude:0.##}";
                case GearEffectKind.CaptureRateBonus: return $"+Catch {fx.Magnitude * 100f:0.#}%";
                case GearEffectKind.TypeDamageMult: return $"{fx.RelatedElement} x{fx.Magnitude:0.##}";
                case GearEffectKind.StatusResistMult: return $"Res x{fx.Magnitude:0.##}";
                case GearEffectKind.GoldGainMult: return $"Gold x{fx.Magnitude:0.##}";
                case GearEffectKind.XpGainMult: return $"XP x{fx.Magnitude:0.##}";
                case GearEffectKind.InitiativeBonus: return $"Init +{fx.Magnitude:0.##}";
                case GearEffectKind.LuckMult: return $"Luck x{fx.Magnitude:0.##}";
                default: return fx.Kind.ToString();
            }
        }

        static string BuildEquipSig(LoadoutSystem lo)
        {
            if (lo == null) return "null";
            return $"{lo.OutfitEquippedId}|{lo.GetCharmEquippedId(0)}|{lo.GetCharmEquippedId(1)}|{lo.GetCharmEquippedId(2)}";
        }

        string BuildListSig()
        {
            if (inventory == null) return "inv";
            var reg = Registry;
            var sb = new StringBuilder();
            foreach (var s in inventory.GetStacksSnapshot())
            {
                if (reg?.GetItem(s.itemId) is GearItemData)
                    sb.Append(s.itemId).Append(':').Append(s.quantity).Append(';');
            }

            return sb.Length == 0 ? "empty" : sb.ToString();
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
