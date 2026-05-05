using UnityEngine;
using UnityEngine.UI;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.UI
{
    /// <summary>
    /// Shared cozy pixel-RPG colors and lightweight uGUI styling helpers.
    /// </summary>
    public static class GameVisualTheme
    {
        public static readonly Color Ink = Hex(0x2B, 0x1E, 0x18);
        public static readonly Color InkSoft = Hex(0x4B, 0x35, 0x27);
        public static readonly Color Cream = Hex(0xFA, 0xE8, 0xB8);
        public static readonly Color Parchment = Hex(0xE7, 0xC8, 0x86);
        public static readonly Color ParchmentDark = Hex(0x9C, 0x6B, 0x3E);
        public static readonly Color Moss = Hex(0x5F, 0x8D, 0x4E);
        public static readonly Color Grass = Hex(0x7E, 0xAD, 0x57);
        public static readonly Color GrassLight = Hex(0xA8, 0xCF, 0x6A);
        public static readonly Color Forest = Hex(0x2F, 0x67, 0x43);
        public static readonly Color Water = Hex(0x4D, 0xA6, 0x9A);
        public static readonly Color WaterDeep = Hex(0x2D, 0x70, 0x77);
        public static readonly Color SkyTop = Hex(0x79, 0xB8, 0xD8);
        public static readonly Color SkyBottom = Hex(0xD6, 0xEE, 0xD0);
        public static readonly Color Road = Hex(0xB7, 0x86, 0x56);
        public static readonly Color RoadDark = Hex(0x7B, 0x56, 0x36);
        public static readonly Color Stone = Hex(0x92, 0x91, 0x83);
        public static readonly Color Glass = Hex(0xB6, 0xD9, 0xFF);
        public static readonly Color Shadow = new Color(0.12f, 0.08f, 0.06f, 0.45f);
        public static readonly Color Panel = Hex(0x46, 0x2F, 0x25, 0.97f);
        public static readonly Color PanelInner = Hex(0x73, 0x49, 0x2F, 0.96f);
        public static readonly Color PanelLight = Hex(0xD9, 0xA7, 0x62, 0.96f);
        public static readonly Color Accent = Hex(0xF3, 0xC0, 0x57);
        static readonly Color AccentGreenBase = Hex(0x9F, 0xC9, 0x67);
        public static readonly Color AccentBlue = Hex(0x75, 0xB7, 0xD8);
        static readonly Color DangerBase = Hex(0xD9, 0x5E, 0x45);
        static readonly Color ColorBlindDanger = Hex(0xC4, 0x56, 0xB5);
        static readonly Color ColorBlindSafe = Hex(0x4F, 0xB4, 0xD8);
        public static readonly Color Text = Cream;
        public static Color AccentGreen => AccessibilitySettings.ColorBlindSafe ? ColorBlindSafe : AccentGreenBase;
        public static Color Danger => AccessibilitySettings.ColorBlindSafe ? ColorBlindDanger : DangerBase;

        public static readonly Color TextDark = Ink;
        public static readonly Color MutedText = Hex(0xE5, 0xD2, 0xA4);

        public static Color Hex(byte r, byte g, byte b, float a = 1f) =>
            new Color(r / 255f, g / 255f, b / 255f, a);

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Brighten(Color color, float amount)
        {
            return new Color(
                Mathf.Clamp01(color.r + amount),
                Mathf.Clamp01(color.g + amount),
                Mathf.Clamp01(color.b + amount),
                color.a);
        }

        public static Color Darken(Color color, float amount) => Brighten(color, -amount);

        public static void ApplyPanel(Image image, bool light = false)
        {
            if (image == null) return;
            image.color = light ? PanelLight : Panel;
        }

        public static void ApplyButton(Button button, bool primary = false)
        {
            if (button == null) return;
            var baseColor = primary ? Accent : PanelInner;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = baseColor;

            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Brighten(baseColor, 0.08f);
            colors.pressedColor = Darken(baseColor, 0.08f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = WithAlpha(Darken(baseColor, 0.2f), 0.55f);
            button.colors = colors;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.color = primary ? TextDark : Text;
        }

        public static void ApplyText(Text text, bool dark = false)
        {
            if (text == null) return;
            text.color = dark ? TextDark : Text;
        }
    }
}
