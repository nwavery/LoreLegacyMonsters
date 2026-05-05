using System;
using UnityEngine.InputSystem;

namespace LoreLegacyMonsters.World
{
    /// <summary>
    /// Handles frame-level overworld shortcuts that are not tied to content logic.
    /// </summary>
    public static class OverworldInputShortcuts
    {
        public static void Handle(Keyboard keyboard, ref bool shopOpen, Action saveAction, Action loadAction)
        {
            if (keyboard == null)
                return;

            if (keyboard.f5Key.wasPressedThisFrame)
                saveAction?.Invoke();
            if (keyboard.f9Key.wasPressedThisFrame)
                loadAction?.Invoke();

            if (shopOpen && keyboard.escapeKey.wasPressedThisFrame)
                shopOpen = false;
        }
    }
}
