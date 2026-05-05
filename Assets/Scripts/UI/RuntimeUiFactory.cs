using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.UI
{
    public static class RuntimeUiFactory
    {
        static Font defaultFont;

        public static Font DefaultFont =>
            defaultFont != null ? defaultFont : defaultFont = ResolveDefaultFont();

        static Font ResolveDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) return font;
            try
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font != null) return font;
            }
            catch (System.ArgumentException)
            {
                // Arial was removed as a built-in resource in newer Unity runtimes.
            }

            try
            {
                return Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Segoe UI", "Liberation Sans" }, 16);
            }
            catch
            {
                Debug.LogError("RuntimeUiFactory: No UI font available (built-in or OS).");
                return null;
            }
        }

        public static Canvas EnsureCanvas(ref Canvas canvas, string canvasName = "RuntimeUI")
        {
            EnsureEventSystem();
            if (canvas != null) return canvas;

            var go = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            Object.DontDestroyOnLoad(go);
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            var image = go.GetComponent<Image>();
            image.color = color;
            return rect;
        }

        public static RectTransform CreateCard(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta,
            Color? trimColor = null)
        {
            var card = CreatePanel(parent, name, color, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            CreatePanel(card, "CardTopTrim", trimColor ?? GameVisualTheme.WithAlpha(GameVisualTheme.Accent, 0.82f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 4f));
            CreatePanel(card, "CardHighlight", GameVisualTheme.WithAlpha(GameVisualTheme.Parchment, 0.08f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(-12f, 14f));
            return card;
        }

        public static RectTransform CreateDivider(Transform parent, string name, Vector2 anchoredPosition, float width, Color? color = null)
        {
            return CreatePanel(parent, name, color ?? GameVisualTheme.WithAlpha(GameVisualTheme.Parchment, 0.26f),
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredPosition, new Vector2(width, 2f));
        }

        public static Text CreateText(Transform parent, string name, string initialText, int fontSize,
            TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta,
            VerticalWrapMode verticalOverflow = VerticalWrapMode.Overflow)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.font = DefaultFont;
            text.text = initialText;
            text.fontSize = Mathf.RoundToInt(fontSize * AccessibilitySettings.TextScale);
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = verticalOverflow;
            return text;
        }

        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = GameVisualTheme.PanelInner;

            var button = go.GetComponent<Button>();
            ApplyActionButton(button, primary: false);

            CreateText(go.transform, "Label", label, 18, TextAnchor.MiddleCenter, Color.white,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            ApplyActionButton(button, primary: false);
            return button;
        }

        public static InputField CreateInputField(Transform parent, string name, string initialText, string placeholderText,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = GameVisualTheme.Panel;

            var placeholder = CreateText(go.transform, "Placeholder", placeholderText, 18, TextAnchor.MiddleLeft,
                GameVisualTheme.WithAlpha(GameVisualTheme.MutedText, 0.62f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(-20f, -10f));
            placeholder.raycastTarget = false;
            placeholder.rectTransform.offsetMin = new Vector2(10f, 5f);
            placeholder.rectTransform.offsetMax = new Vector2(-10f, -5f);

            var text = CreateText(go.transform, "Text", initialText, 18, TextAnchor.MiddleLeft,
                GameVisualTheme.Text, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -10f));
            text.raycastTarget = false;
            text.rectTransform.offsetMin = new Vector2(10f, 5f);
            text.rectTransform.offsetMax = new Vector2(-10f, -5f);

            var input = go.GetComponent<InputField>();
            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = initialText;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        public static void DestroyChildren(Transform parent)
        {
            if (parent == null) return;
            for (var i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }

        /// <summary>Standard modal header: accent trim, title, bottom-right close.</summary>
        public sealed class ModalWindowChrome
        {
            public Text Title;
            public Button CloseButton;
        }

        public static ModalWindowChrome CreateModalWindowChrome(RectTransform root, string initialTitle, Color trimColor,
            string closeLabel, UnityAction onClose)
        {
            CreatePanel(root, "TopTrim", trimColor,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 5f));
            var ttl = CreateText(root, "ModalTitle", initialTitle, 24, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(420f, 28f));
            var close = CreateButton(root, "CloseButton", closeLabel,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(150f, 40f));
            close.onClick.RemoveAllListeners();
            if (onClose != null)
                close.onClick.AddListener(onClose);
            return new ModalWindowChrome { Title = ttl, CloseButton = close };
        }

        /// <summary>Themed list row with icon slot, primary/secondary text gutters.</summary>
        public static RectTransform CreateListRowCard(Transform parent, string name, int rowIndex, float rowHeight, float width,
            out RectTransform iconSlot, out Text primaryText, out Text secondaryText)
        {
            var row = CreatePanel(parent, name, GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.95f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -(rowIndex * rowHeight)), new Vector2(width, rowHeight - 6f));
            iconSlot = CreatePanel(row, "IconSlot", GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.18f),
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(36f, 36f));
            CreatePanel(iconSlot, "IconInner", GameVisualTheme.Parchment,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
            primaryText = CreateText(row, "Primary", string.Empty, 18, TextAnchor.MiddleLeft, GameVisualTheme.Text,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(260f, 26f));
            secondaryText = CreateText(row, "Secondary", string.Empty, 15, TextAnchor.MiddleRight, GameVisualTheme.MutedText,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-118f, 0f), new Vector2(96f, 24f));
            return row;
        }

        public static Text CreatePillBadge(Transform parent, string name, string label, Vector2 anchoredPosition)
        {
            var bg = CreatePanel(parent, name, GameVisualTheme.WithAlpha(GameVisualTheme.Accent, 0.92f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, new Vector2(120f, 26f));
            return CreateText(bg, "BadgeLabel", label, 13, TextAnchor.MiddleCenter, GameVisualTheme.TextDark,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, VerticalWrapMode.Truncate);
        }

        public static Text CreateStatusBadge(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, Color? color = null)
        {
            var bg = CreatePanel(parent, name, color ?? GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.82f),
                anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            CreatePanel(bg, "BadgeDot", GameVisualTheme.AccentGreen,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(10f, 10f));
            return CreateText(bg, "BadgeLabel", label, 13, TextAnchor.MiddleLeft, GameVisualTheme.MutedText,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(18f, 0f), new Vector2(-26f, -4f), VerticalWrapMode.Truncate);
        }

        public static Button CreatePrimaryActionButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var btn = CreateButton(parent, name, label, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            ApplyActionButton(btn, primary: true);
            return btn;
        }

        public static Button CreateSecondaryActionButton(Transform parent, string name, string label, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta, bool compact = false)
        {
            var btn = CreateButton(parent, name, label, anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            ApplyActionButton(btn, primary: false, compact);
            return btn;
        }

        public static void ApplyActionButton(Button button, bool primary, bool compact = false)
        {
            if (button == null) return;
            GameVisualTheme.ApplyButton(button, primary);
            var label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            label.fontSize = compact ? 14 : primary ? 17 : 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static void ApplyHintTextStyle(Text text, bool compact = false)
        {
            if (text == null) return;
            text.fontSize = compact ? 13 : 15;
            text.color = GameVisualTheme.MutedText;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static void ApplyBodyTextStyle(Text text, bool dark = false)
        {
            if (text == null) return;
            text.fontSize = 17;
            text.color = dark ? GameVisualTheme.TextDark : GameVisualTheme.Text;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }
    }
}
