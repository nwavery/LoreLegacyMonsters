using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.World;
using UnityEngine.InputSystem;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Audio;

namespace LoreLegacyMonsters.UI
{
    /// <summary>
    /// Input coordinator for the runtime presentation layer.
    /// Battle digit/Space/Enter keys are handled in <see cref="CombatUI"/> during combat.
    /// </summary>
    public class GameHudUI : MonoBehaviour
    {
        OverworldChapterController controller;
        RectTransform helpOverlayRoot;
        RectTransform pauseOverlayRoot;
        RectTransform settingsOverlayRoot;

        public bool IsPartyManagerOpen => UIManager.Instance != null && UIManager.Instance.IsModalOpen(UiModal.Party);

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void CloseHelpOverlay()
        {
            if (helpOverlayRoot == null) return;
            Destroy(helpOverlayRoot.gameObject);
            helpOverlayRoot = null;
            UIManager.Instance?.SetModalOpen(UiModal.Help, false);
        }

        void ToggleHelpOverlay(UIManager ui)
        {
            if (ui?.Root == null) return;
            if (helpOverlayRoot != null)
            {
                CloseHelpOverlay();
                return;
            }

            helpOverlayRoot = HelpOverlayUtility.Create(ui.Root.transform, AlphaHelpText.ControlsTitle,
                AlphaHelpText.ControlsBody, CloseHelpOverlay);
            ui.SetModalOpen(UiModal.Help, true);
        }

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            if (controller == null) return;
            AudioManager.EnsureExists();
            GameSettings.ApplyDisplay();

            var kb = Keyboard.current;
            if (kb == null) return;
            var ui = UIManager.Instance;
            var combat = controller.Combat;

            var nonCombatUiAllowed = ui != null && !ui.IsModalOpen(UiModal.Dialog) && !ui.IsModalOpen(UiModal.Loading) &&
                                     !ui.IsModalOpen(UiModal.Pause) && !ui.IsModalOpen(UiModal.Settings) &&
                                     !controller.ShopOpen && (combat == null || !combat.IsBattleActive);

            if (kb.f1Key.wasPressedThisFrame && nonCombatUiAllowed)
                ToggleHelpOverlay(ui);

            if (kb.tabKey.wasPressedThisFrame && nonCombatUiAllowed)
            {
                ui.SetModalOpen(UiModal.QuestLog, false);
                ui.SetModalOpen(UiModal.Map, false);
                ui.SetModalOpen(UiModal.Inventory, false);
                ui.ToggleModal(UiModal.Party);
            }

            if (kb.jKey.wasPressedThisFrame && nonCombatUiAllowed)
            {
                ui.SetModalOpen(UiModal.Party, false);
                ui.SetModalOpen(UiModal.Map, false);
                ui.SetModalOpen(UiModal.Inventory, false);
                ui.ToggleModal(UiModal.QuestLog);
            }

            if (kb.mKey.wasPressedThisFrame && nonCombatUiAllowed)
            {
                ui.SetModalOpen(UiModal.Party, false);
                ui.SetModalOpen(UiModal.QuestLog, false);
                ui.SetModalOpen(UiModal.Inventory, false);
                ui.ToggleModal(UiModal.Map);
            }

            if (kb.iKey.wasPressedThisFrame && nonCombatUiAllowed)
            {
                ui.SetModalOpen(UiModal.Party, false);
                ui.SetModalOpen(UiModal.QuestLog, false);
                ui.SetModalOpen(UiModal.Map, false);
                ui.ToggleModal(UiModal.Inventory);
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (helpOverlayRoot != null) CloseHelpOverlay();
                else if (settingsOverlayRoot != null) CloseSettingsOverlay();
                else if (pauseOverlayRoot != null) ClosePauseOverlay();
                else if (ui != null && ui.IsModalOpen(UiModal.Inventory)) ui.SetModalOpen(UiModal.Inventory, false);
                else if (ui != null && ui.IsModalOpen(UiModal.Map)) ui.SetModalOpen(UiModal.Map, false);
                else if (ui != null && ui.IsModalOpen(UiModal.QuestLog)) ui.SetModalOpen(UiModal.QuestLog, false);
                else if (ui != null && ui.IsModalOpen(UiModal.Party)) ui.SetModalOpen(UiModal.Party, false);
                else if (controller.ShopOpen) controller.CloseShop();
            }

            if (kb[GameSettings.Pause].wasPressedThisFrame && nonCombatUiAllowed && pauseOverlayRoot == null)
            {
                OpenPauseOverlay(ui);
            }
            else if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame && nonCombatUiAllowed && pauseOverlayRoot == null)
            {
                OpenPauseOverlay(ui);
            }
        }

        void OpenPauseOverlay(UIManager ui)
        {
            if (ui?.Root == null || pauseOverlayRoot != null) return;
            Time.timeScale = 0f;
            pauseOverlayRoot = PauseMenuUI.Create(ui.Root.transform,
                onResume: ClosePauseOverlay,
                onOpenSettings: () => OpenSettingsOverlay(ui),
                onQuitToMain: QuitToMainMenu);
            ui.SetModalOpen(UiModal.Pause, true);
        }

        void ClosePauseOverlay()
        {
            if (pauseOverlayRoot != null)
                Destroy(pauseOverlayRoot.gameObject);
            pauseOverlayRoot = null;
            UIManager.Instance?.SetModalOpen(UiModal.Pause, false);
            if (settingsOverlayRoot == null)
                Time.timeScale = 1f;
        }

        void OpenSettingsOverlay(UIManager ui)
        {
            if (ui?.Root == null || settingsOverlayRoot != null) return;
            settingsOverlayRoot = SettingsMenuUI.Create(ui.Root.transform, CloseSettingsOverlay);
            ui.SetModalOpen(UiModal.Settings, true);
        }

        void CloseSettingsOverlay()
        {
            if (settingsOverlayRoot != null)
                Destroy(settingsOverlayRoot.gameObject);
            settingsOverlayRoot = null;
            UIManager.Instance?.SetModalOpen(UiModal.Settings, false);
        }

        void QuitToMainMenu()
        {
            CloseSettingsOverlay();
            ClosePauseOverlay();
            Time.timeScale = 1f;
            var gameController = FindFirstObjectByType<GameController>();
            gameController?.ToMainMenu();
        }

        void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}
