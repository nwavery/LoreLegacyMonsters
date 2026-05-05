using System;
using UnityEngine;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    public static class HelpOverlayUtility
    {
        /// <summary>Full-screen dimmed overlay with title, body text, and close button.</summary>
        public static RectTransform Create(Transform parent, string title, string body, Action onClose)
        {
            var root = RuntimeUiFactory.CreatePanel(parent, "HelpOverlayRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.82f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            root.SetAsLastSibling();
            RuntimeUiFactory.CreateText(root, "HelpTitle", title, 28, TextAnchor.UpperCenter, GameVisualTheme.Accent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f),
                new Vector2(720f, 40f));

            var bodyPanel = RuntimeUiFactory.CreatePanel(root, "HelpBodyPanel",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.97f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 20f), new Vector2(760f, 400f));
            RuntimeUiFactory.CreatePanel(bodyPanel, "BodyTrim", GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f));

            var bodyText = RuntimeUiFactory.CreateText(bodyPanel, "HelpBody", body, 17, TextAnchor.UpperLeft,
                GameVisualTheme.Text, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -16f), new Vector2(700f, 360f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Truncate;

            var close = RuntimeUiFactory.CreateButton(root, "CloseHelpButton", "Close",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f),
                new Vector2(200f, 44f));
            close.onClick.AddListener(() => onClose?.Invoke());

            return root;
        }
    }
}
