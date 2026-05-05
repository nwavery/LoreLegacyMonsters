using UnityEngine.InputSystem;

namespace LoreLegacyMonsters.UI
{
    public static class InputGlyphs
    {
        public static string ConfirmGlyph()
        {
            var pad = Gamepad.current;
            if (pad == null) return "[Enter]";
            var name = (pad.displayName ?? string.Empty).ToLowerInvariant();
            if (name.Contains("playstation") || name.Contains("dualsense") || name.Contains("dualshock"))
                return "[Cross]";
            return "[A]";
        }

        public static string CancelGlyph()
        {
            var pad = Gamepad.current;
            if (pad == null) return "[Esc]";
            var name = (pad.displayName ?? string.Empty).ToLowerInvariant();
            if (name.Contains("playstation") || name.Contains("dualsense") || name.Contains("dualshock"))
                return "[Circle]";
            return "[B]";
        }
    }
}
