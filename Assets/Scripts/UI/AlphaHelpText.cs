namespace LoreLegacyMonsters.UI
{
    /// <summary>Shared copy for main menu and in-game help (alpha testers).</summary>
    public static class AlphaHelpText
    {
        public const string ControlsTitle = "Controls & alpha notes";

        public const string ControlsBody =
            "MOVEMENT: WASD — explore the overworld.\n" +
            "INTERACT: E — talk to NPCs and use services.\n" +
            "BATTLE: 1–2 moves, 3 Guard, 4 Potion, 5 Capture, 6 Switch, 7 Flee — Space closes battle results.\n" +
            "MENUS: Tab party · J quest log · M map · I inventory · Esc closes menus / shop.\n" +
            "FIRST 20 MINUTES: Talk to Mira in Hollowfen, follow the tracker to the east route, then challenge the grove boss.\n" +
            "MAP QUICK TIP: On the map, YOU is your current area and X is your current quest lead.\n" +
            "SAVE / LOAD: F5 quick-save slot 0 · F9 quick-load slot 0 (also use main menu Load Slot 0).\n" +
            "AUTOSAVE: Slot 0 is auto-saved periodically while you play.\n" +
            "HELP: F1 in-game opens this panel.\n\n" +
            "LOCAL LLM (required for alpha): Run Ollama with the model from README (default llama3.2:latest). " +
            "Use Main Menu “Test LLM” before playing. If the LLM fails, NPCs fall back to scripted dialog.\n\n" +
            "REPORTS: See ALPHA_TESTING.md for logs location and bug-report template.";
    }
}
