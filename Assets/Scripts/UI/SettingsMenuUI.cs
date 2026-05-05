using System;
using LoreLegacyMonsters.Audio;
using LoreLegacyMonsters.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace LoreLegacyMonsters.UI
{
    public static class SettingsMenuUI
    {
        public static RectTransform Create(Transform parent, Action onClosed)
        {
            var root = RuntimeUiFactory.CreatePanel(parent, "SettingsOverlayRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.86f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var panel = RuntimeUiFactory.CreateCard(root, "SettingsPanel", GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 620f));

            RuntimeUiFactory.CreateText(panel, "SettingsTitle", "Settings", 30, TextAnchor.UpperCenter, GameVisualTheme.Accent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(380f, 44f));

            var y = -86f;
            var masterText = AddRow(panel, "MasterVolLabel", y, $"Master Volume: {Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%");
            var masterBtn = AddCycleButton(panel, "MasterVolBtn", y, "Cycle +10%");
            y -= 52f;

            var musicText = AddRow(panel, "MusicVolLabel", y, $"Music Volume: {Mathf.RoundToInt(GameSettings.MusicVolume * 100f)}%");
            var musicBtn = AddCycleButton(panel, "MusicVolBtn", y, "Cycle +10%");
            y -= 52f;

            var sfxText = AddRow(panel, "SfxVolLabel", y, $"SFX Volume: {Mathf.RoundToInt(GameSettings.SfxVolume * 100f)}%");
            var sfxBtn = AddCycleButton(panel, "SfxVolBtn", y, "Cycle +10%");
            y -= 52f;

            var modeText = AddRow(panel, "ScreenModeLabel", y, $"Display Mode: {GameSettings.ScreenMode}");
            var modeBtn = AddCycleButton(panel, "ScreenModeBtn", y, "Cycle");
            y -= 52f;

            var vsyncText = AddRow(panel, "VsyncLabel", y, $"VSync: {(GameSettings.Vsync ? "On" : "Off")}");
            var vsyncBtn = AddCycleButton(panel, "VsyncBtn", y, "Toggle");
            y -= 52f;

            var frameText = AddRow(panel, "FrameCapLabel", y, $"Frame Cap: {GameSettings.FrameCap}");
            var frameBtn = AddCycleButton(panel, "FrameCapBtn", y, "Cycle");
            y -= 52f;

            var movementText = AddRow(panel, "MoveKeysLabel", y, BuildMovementBindingLabel());
            var movementBtn = AddCycleButton(panel, "MoveKeysBtn", y, "Cycle preset");
            y -= 52f;

            var interactText = AddRow(panel, "InteractKeyLabel", y, $"Interact Key: {GameSettings.Interact}");
            var interactBtn = AddCycleButton(panel, "InteractKeyBtn", y, "Toggle E/F");
            y -= 52f;

            var pauseText = AddRow(panel, "PauseKeyLabel", y, $"Pause Key: {GameSettings.Pause}");
            var pauseBtn = AddCycleButton(panel, "PauseKeyBtn", y, "Toggle Esc/P");
            y -= 52f;

            var textScaleText = AddRow(panel, "TextScaleLabel", y, $"Text Scale: {AccessibilitySettings.TextScale:0.0}x");
            var textScaleBtn = AddCycleButton(panel, "TextScaleBtn", y, "Cycle");
            y -= 52f;

            var colorBlindText = AddRow(panel, "ColorBlindLabel", y, $"Color-blind-safe palette: {(AccessibilitySettings.ColorBlindSafe ? "On" : "Off")}");
            var colorBlindBtn = AddCycleButton(panel, "ColorBlindBtn", y, "Toggle");
            y -= 52f;

            var flashText = AddRow(panel, "FlashLabel", y, $"Reduce flashes: {(AccessibilitySettings.ReduceFlash ? "On" : "Off")}");
            var flashBtn = AddCycleButton(panel, "FlashBtn", y, "Toggle");
            y -= 52f;

            var shakeText = AddRow(panel, "ShakeLabel", y, $"Reduce camera shake: {(AccessibilitySettings.ReduceShake ? "On" : "Off")}");
            var shakeBtn = AddCycleButton(panel, "ShakeBtn", y, "Toggle");
            y -= 52f;

            var telemetryText = AddRow(panel, "TelemetryLabel", y, $"Crash telemetry (opt-in): {(CrashTelemetryReporter.IsOptedIn ? "On" : "Off")}");
            var telemetryBtn = AddCycleButton(panel, "TelemetryBtn", y, "Toggle");

            RuntimeUiFactory.CreateText(panel, "SettingsHint",
                $"Settings are saved to PlayerPrefs and applied immediately. Confirm {InputGlyphs.ConfirmGlyph()}  Cancel {InputGlyphs.CancelGlyph()}",
                14, TextAnchor.UpperLeft, GameVisualTheme.MutedText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 68f), new Vector2(760f, 42f));

            var close = RuntimeUiFactory.CreateSecondaryActionButton(panel, "SettingsCloseBtn", "Back",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(220f, 44f));
            close.onClick.AddListener(() =>
            {
                GameSettings.Save();
                AccessibilitySettings.Save();
                onClosed?.Invoke();
            });

            masterBtn.onClick.AddListener(() =>
            {
                GameSettings.SetMasterVolume(Cycle01(GameSettings.MasterVolume, 0.1f));
                masterText.text = $"Master Volume: {Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}%";
                AudioManager.EnsureExists().ApplyVolumeSettings();
            });
            musicBtn.onClick.AddListener(() =>
            {
                GameSettings.SetMusicVolume(Cycle01(GameSettings.MusicVolume, 0.1f));
                musicText.text = $"Music Volume: {Mathf.RoundToInt(GameSettings.MusicVolume * 100f)}%";
                AudioManager.EnsureExists().ApplyVolumeSettings();
            });
            sfxBtn.onClick.AddListener(() =>
            {
                GameSettings.SetSfxVolume(Cycle01(GameSettings.SfxVolume, 0.1f));
                sfxText.text = $"SFX Volume: {Mathf.RoundToInt(GameSettings.SfxVolume * 100f)}%";
                AudioManager.EnsureExists().ApplyVolumeSettings();
            });
            modeBtn.onClick.AddListener(() =>
            {
                var next = GameSettings.ScreenMode switch
                {
                    FullScreenMode.ExclusiveFullScreen => FullScreenMode.FullScreenWindow,
                    FullScreenMode.FullScreenWindow => FullScreenMode.MaximizedWindow,
                    FullScreenMode.MaximizedWindow => FullScreenMode.Windowed,
                    _ => FullScreenMode.ExclusiveFullScreen
                };
                GameSettings.SetScreenMode(next);
                GameSettings.ApplyDisplay();
                modeText.text = $"Display Mode: {GameSettings.ScreenMode}";
            });
            vsyncBtn.onClick.AddListener(() =>
            {
                GameSettings.SetVsync(!GameSettings.Vsync);
                GameSettings.ApplyDisplay();
                vsyncText.text = $"VSync: {(GameSettings.Vsync ? "On" : "Off")}";
            });
            frameBtn.onClick.AddListener(() =>
            {
                var next = GameSettings.FrameCap switch
                {
                    60 => 120,
                    120 => 144,
                    144 => 240,
                    _ => 60
                };
                GameSettings.SetFrameCap(next);
                GameSettings.ApplyDisplay();
                frameText.text = $"Frame Cap: {GameSettings.FrameCap}";
            });
            movementBtn.onClick.AddListener(() =>
            {
                CycleMovementPreset();
                movementText.text = BuildMovementBindingLabel();
            });
            interactBtn.onClick.AddListener(() =>
            {
                GameSettings.SetInteractKey(GameSettings.Interact == Key.E ? Key.F : Key.E);
                interactText.text = $"Interact Key: {GameSettings.Interact}";
            });
            pauseBtn.onClick.AddListener(() =>
            {
                GameSettings.SetPauseKey(GameSettings.Pause == Key.Escape ? Key.P : Key.Escape);
                pauseText.text = $"Pause Key: {GameSettings.Pause}";
            });
            textScaleBtn.onClick.AddListener(() =>
            {
                var next = AccessibilitySettings.TextScale switch
                {
                    <= 1.0f => 1.2f,
                    <= 1.2f => 1.4f,
                    _ => 1.0f
                };
                AccessibilitySettings.SetTextScale(next);
                textScaleText.text = $"Text Scale: {AccessibilitySettings.TextScale:0.0}x";
            });
            colorBlindBtn.onClick.AddListener(() =>
            {
                AccessibilitySettings.SetColorBlindSafe(!AccessibilitySettings.ColorBlindSafe);
                colorBlindText.text = $"Color-blind-safe palette: {(AccessibilitySettings.ColorBlindSafe ? "On" : "Off")}";
            });
            flashBtn.onClick.AddListener(() =>
            {
                AccessibilitySettings.SetReduceFlash(!AccessibilitySettings.ReduceFlash);
                flashText.text = $"Reduce flashes: {(AccessibilitySettings.ReduceFlash ? "On" : "Off")}";
            });
            shakeBtn.onClick.AddListener(() =>
            {
                AccessibilitySettings.SetReduceShake(!AccessibilitySettings.ReduceShake);
                shakeText.text = $"Reduce camera shake: {(AccessibilitySettings.ReduceShake ? "On" : "Off")}";
            });
            telemetryBtn.onClick.AddListener(() =>
            {
                CrashTelemetryReporter.SetOptIn(!CrashTelemetryReporter.IsOptedIn);
                telemetryText.text = $"Crash telemetry (opt-in): {(CrashTelemetryReporter.IsOptedIn ? "On" : "Off")}";
            });

            return root;
        }

        static Text AddRow(Transform parent, string name, float y, string label) =>
            RuntimeUiFactory.CreateText(parent, name, label, 17, TextAnchor.MiddleLeft, GameVisualTheme.Text,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-120f, y), new Vector2(540f, 34f));

        static Button AddCycleButton(Transform parent, string name, float y, string label) =>
            RuntimeUiFactory.CreateSecondaryActionButton(parent, name, label,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(250f, y), new Vector2(200f, 34f), compact: true);

        static float Cycle01(float current, float step)
        {
            var next = current + step;
            if (next > 1f) next = 0f;
            return Mathf.Clamp01(next);
        }

        static void CycleMovementPreset()
        {
            if (GameSettings.MoveUp == Key.W)
                GameSettings.SetMoveKeys(Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow);
            else if (GameSettings.MoveUp == Key.UpArrow)
                GameSettings.SetMoveKeys(Key.I, Key.K, Key.J, Key.L);
            else
                GameSettings.SetMoveKeys(Key.W, Key.S, Key.A, Key.D);
        }

        static string BuildMovementBindingLabel() =>
            $"Move Keys: {GameSettings.MoveUp}/{GameSettings.MoveDown}/{GameSettings.MoveLeft}/{GameSettings.MoveRight}";
    }
}
