using System;
using UnityEngine;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    /// <summary>Factories for wardrobe slot visuals (runtime-generated uGUI).</summary>
    public static class EquipmentSlotUI
    {
        public static RectTransform CreateSlotColumn(Transform parent, string slotLabel, Color accentBorder,
            out Text nameLabel, out Text rarityLabel,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offset, Vector2 size)
        {
            var shell = RuntimeUiFactory.CreatePanel(parent, $"{slotLabel}Slot",
                GameVisualTheme.WithAlpha(accentBorder, 0.45f),
                anchorMin, anchorMax, pivot, offset, size);
            var inner = RuntimeUiFactory.CreatePanel(shell, $"{slotLabel}Inner",
                GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.96f),
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(3f, 3f), new Vector2(-6f, -6f));

            RuntimeUiFactory.CreateText(inner, $"{slotLabel}Title", slotLabel.ToUpperInvariant(), 12, TextAnchor.UpperLeft,
                GameVisualTheme.MutedText, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -6f), new Vector2(-16f, 18f));

            nameLabel = RuntimeUiFactory.CreateText(inner, $"{slotLabel}EquippedName", "(empty)", 15, TextAnchor.UpperLeft,
                GameVisualTheme.Text, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(8f, -28f), new Vector2(-16f, 22f));

            rarityLabel = RuntimeUiFactory.CreateText(inner, $"{slotLabel}EquippedRarity", "", 13, TextAnchor.LowerLeft,
                GameVisualTheme.MutedText, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(8f, 10f), new Vector2(-16f, 28f));

            return shell.GetComponent<RectTransform>();
        }

        public static Button CreateSmallButton(RectTransform parent, string name, string label, Vector2 offset,
            Vector2 size, Action onClick)
        {
            var btn = RuntimeUiFactory.CreateSecondaryActionButton(parent, name, label,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), offset, size);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }
    }
}
