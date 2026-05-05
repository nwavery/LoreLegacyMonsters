using System;
using LoreLegacyMonsters.Audio;
using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.UI
{
    public static class PauseMenuUI
    {
        public static RectTransform Create(Transform parent, Action onResume, Action onOpenSettings, Action onQuitToMain)
        {
            var root = RuntimeUiFactory.CreatePanel(parent, "PauseOverlayRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.85f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            var panel = RuntimeUiFactory.CreateCard(root, "PausePanel", GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 420f));

            RuntimeUiFactory.CreateText(panel, "PauseTitle", "Paused", 34, TextAnchor.UpperCenter, GameVisualTheme.Accent,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(280f, 50f));
            RuntimeUiFactory.CreateText(panel, "PauseSub",
                "Game is paused. Adjust settings, then resume or return to main menu.",
                16, TextAnchor.UpperCenter, GameVisualTheme.MutedText,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(500f, 44f));

            var resume = RuntimeUiFactory.CreatePrimaryActionButton(panel, "PauseResumeBtn", "Resume",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(300f, 44f));
            var settings = RuntimeUiFactory.CreateSecondaryActionButton(panel, "PauseSettingsBtn", "Settings",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -216f), new Vector2(300f, 44f));
            var quit = RuntimeUiFactory.CreateSecondaryActionButton(panel, "PauseMainMenuBtn", "Quit to Main Menu",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -272f), new Vector2(300f, 44f));

            var settingsSummary = RuntimeUiFactory.CreateText(panel, "PauseSettingsSummary", BuildSettingsSummary(), 14,
                TextAnchor.MiddleCenter, GameVisualTheme.MutedText,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(560f, 34f));

            resume.onClick.AddListener(() =>
            {
                AudioManager.EnsureExists().PlayUiSfx(0);
                onResume?.Invoke();
            });
            settings.onClick.AddListener(() =>
            {
                AudioManager.EnsureExists().PlayUiSfx(0);
                onOpenSettings?.Invoke();
                settingsSummary.text = BuildSettingsSummary();
            });
            quit.onClick.AddListener(() =>
            {
                AudioManager.EnsureExists().PlayUiSfx(0);
                onQuitToMain?.Invoke();
            });

            return root;
        }

        static string BuildSettingsSummary()
        {
            GameSettings.ApplyDisplay();
            return $"Audio {Mathf.RoundToInt(GameSettings.MasterVolume * 100f)}% | {GameSettings.ScreenMode} | " +
                   $"VSync {(GameSettings.Vsync ? "On" : "Off")} | FPS {GameSettings.FrameCap}";
        }
    }
}
