using System.Collections;
using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.UI;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.SaveLoad
{
    public class SaveLoadManager : MonoBehaviour
    {
        public static SaveLoadManager Instance { get; private set; }

        /// <summary>
        /// True after <see cref="NewGame"/> or a successful <see cref="LoadSlot"/>.
        /// Prevents applying the default empty <see cref="WorkingCopy"/> before any snapshot exists.
        /// </summary>
        public bool HasAuthoritativeWorkingCopy { get; private set; }

        public SaveInfo WorkingCopy { get; private set; } = new SaveInfo();

        /// <summary>
        /// Ensures a persistent manager exists (e.g. from Main Menu before the Game scene loads).
        /// </summary>
        public static SaveLoadManager EnsureExists()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SaveLoadManager");
            return go.AddComponent<SaveLoadManager>();
        }

        [SerializeField] float autoSaveIntervalSeconds = 90f;
        [SerializeField] int autoSaveSlotIndex = 0;
        [SerializeField] bool enableAutoSave = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void NewGame(string playerName)
        {
            WorkingCopy = DefaultGameContent.CreateFreshSave(playerName);
            HasAuthoritativeWorkingCopy = true;
            if (GameManager.Instance != null) GameManager.Instance.PlayerGold = WorkingCopy.Gold;
            GameManager.Instance?.ApplySaveToRuntime(WorkingCopy);
        }

        public bool LoadSlot(int slot, out string error)
        {
            if (!SaveSystem.SaveSystem.TryLoad(slot, out var data, out error))
                return false;
            WorkingCopy = data;
            HasAuthoritativeWorkingCopy = true;
            GameManager.Instance?.ApplySaveToRuntime(WorkingCopy);
            return true;
        }

        public bool SaveSlot(int slot, out string error)
        {
            GameManager.Instance?.CaptureRuntimeToSave(WorkingCopy);
            return SaveSystem.SaveSystem.TrySave(slot, WorkingCopy, out error);
        }

        Coroutine _autoSaveRoutine;

        IEnumerator AutoSaveLoop()
        {
            var wait = new WaitForSeconds(autoSaveIntervalSeconds);
            while (enabled)
            {
                yield return wait;
                if (!CanAutoSaveNow())
                    continue;
                if (GameManager.Instance != null &&
                    !SaveSlot(autoSaveSlotIndex, out var autoErr))
                    GameEvents.RaiseToast(string.IsNullOrWhiteSpace(autoErr)
                        ? "Auto-save failed."
                        : $"Auto-save failed: {autoErr}");
            }
        }

        void OnEnable()
        {
            if (enableAutoSave && autoSaveIntervalSeconds > 2f && _autoSaveRoutine == null)
                _autoSaveRoutine = StartCoroutine(AutoSaveLoop());
        }

        void OnDisable()
        {
            if (_autoSaveRoutine != null)
            {
                StopCoroutine(_autoSaveRoutine);
                _autoSaveRoutine = null;
            }
        }

        public bool CanAutoSaveNow(bool explicitRequest = false)
        {
            if (explicitRequest)
                return true;

            var ui = UIManager.Instance != null ? UIManager.Instance : FindFirstObjectByType<UIManager>();
            if (ui != null && (ui.IsModalOpen(UiModal.Loading) || ui.IsModalOpen(UiModal.Ending)))
                return false;

            var chapter = FindFirstObjectByType<OverworldChapterController>();
            if (chapter == null)
                return true;

            if (chapter.Combat != null && chapter.Combat.IsBattleActive)
                return false;
            if (chapter.EndingChoiceOpen)
                return false;

            var dialog = chapter.DialogDriver;
            return dialog == null || (!dialog.IsConversationOpen && !dialog.IsBusy);
        }
    }
}
