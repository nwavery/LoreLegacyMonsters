using System.Collections;
using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.UI
{
    /// <summary>Runs before default scripts so <see cref="UIManager"/> and menu UI exist when the scene loads.</summary>
    [DefaultExecutionOrder(-100)]
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameController controller;
        [SerializeField] string pendingPlayerName = "Hero";

        Coroutine _llmHealthRoutine;
        RectTransform _llmSettingsRoot;

        public string PendingPlayerName => string.IsNullOrWhiteSpace(pendingPlayerName) ? "Hero" : pendingPlayerName.Trim();
        public bool CanLoadSlot0 => LoreLegacyMonsters.SaveSystem.SaveSystem.SlotExists(0);

        void Awake()
        {
            LlmRuntimeSupervisor.EnsureStarted();
            if (FindFirstObjectByType<UIManager>() == null)
                new GameObject("UIManager").AddComponent<UIManager>();
            var menuUi = GetComponent<MainMenuUI>();
            if (menuUi == null) menuUi = gameObject.AddComponent<MainMenuUI>();
            menuUi.Bind(this);
        }

        public void SetPlayerName(string value)
        {
            pendingPlayerName = string.IsNullOrWhiteSpace(value) ? "Hero" : value.Trim();
        }

        public void OnNewGame()
        {
            controller ??= FindFirstObjectByType<GameController>();
            controller?.NewGame(PendingPlayerName);
        }

        public void OnLoadSlot(int slot)
        {
            controller ??= FindFirstObjectByType<GameController>();
            controller?.LoadGame(slot);
        }

        public void OnQuit() => Application.Quit();

        public void OpenLlmSettings()
        {
            if (UIManager.Instance?.Root == null) return;
            if (_llmSettingsRoot != null)
            {
                Destroy(_llmSettingsRoot.gameObject);
                _llmSettingsRoot = null;
                return;
            }

            _llmSettingsRoot = LlmSettingsOverlay.Create(UIManager.Instance.Root.transform, () => _llmSettingsRoot = null);
        }

        /// <summary>Alpha: verify Ollama / OpenAI-compatible endpoint before play.</summary>
        public void RunLlmHealthCheck()
        {
            if (_llmHealthRoutine != null) StopCoroutine(_llmHealthRoutine);
            _llmHealthRoutine = StartCoroutine(LlmHealthRoutine());
        }

        IEnumerator LlmHealthRoutine()
        {
            var settings = NpcLlmSettings.ResolveForDriver(null);

            var ok = false;
            string msg = null;
            yield return NpcLlmHealthCheck.Probe(settings, (success, m) =>
            {
                ok = success;
                msg = m;
            });

            LlmRuntimeStatus.SetProbeResult(ok, ok ? $"OK — {settings.Model}" : (msg ?? ""));
            if (ok)
                GameEvents.RaiseToast($"LLM OK — {settings.CompletionsUrl} (model: {settings.Model})");
            else
            {
                var detail = string.IsNullOrWhiteSpace(msg) ? "unknown error" : msg;
                if (detail.Length > 140) detail = detail.Substring(0, 137) + "…";
                GameEvents.RaiseToast($"LLM check failed: {detail}");
            }

            _llmHealthRoutine = null;
        }
    }
}
