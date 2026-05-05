using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.UI
{
    /// <summary>Main menu overlay: Ollama URL, model, enable toggle, test connection.</summary>
    public static class LlmSettingsOverlay
    {
        public static RectTransform Create(Transform parent, System.Action onClosed)
        {
            var defaults = NpcLlmSettings.LoadFromResources();
            var useLlm = LlmGlobalPreferences.IsGloballyEnabled(true);

            var root = RuntimeUiFactory.CreatePanel(parent, "LlmSettingsOverlayRoot",
                new Color(0f, 0f, 0f, 0.82f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            RuntimeUiFactory.CreateText(root, "LlmTitle", "Local LLM (Ollama)", 26, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f),
                new Vector2(640f, 36f));

            var panel = RuntimeUiFactory.CreatePanel(root, "LlmPanel",
                new Color(0.06f, 0.08f, 0.12f, 0.98f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(640f, 400f));

            var y = -20f;
            var modeBtn = RuntimeUiFactory.CreateButton(panel, "LlmModeBtn", useLlm ? "Use local LLM: ON" : "Use local LLM: OFF",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 40f));
            var modeLabel = modeBtn.GetComponentInChildren<Text>();
            y -= 52f;

            modeBtn.onClick.AddListener(() =>
            {
                useLlm = !useLlm;
                if (modeLabel != null)
                    modeLabel.text = useLlm ? "Use local LLM: ON" : "Use local LLM: OFF";
            });

            RuntimeUiFactory.CreateText(panel, "UrlLabel", "Base URL (OpenAI-compatible)", 16, TextAnchor.UpperLeft, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, y), new Vector2(560f, 22f));
            y -= 28f;
            var urlField = RuntimeUiFactory.CreateInputField(panel, "BaseUrlField",
                PlayerPrefs.HasKey(LlmGlobalPreferences.KeyBaseUrl) ? PlayerPrefs.GetString(LlmGlobalPreferences.KeyBaseUrl) : defaults.BaseUrl,
                "http://127.0.0.1:11434/v1",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(592f, 38f));
            y -= 52f;

            RuntimeUiFactory.CreateText(panel, "ModelLabel", "Model name", 16, TextAnchor.UpperLeft, Color.white,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, y), new Vector2(560f, 22f));
            y -= 28f;
            var modelField = RuntimeUiFactory.CreateInputField(panel, "ModelField",
                PlayerPrefs.HasKey(LlmGlobalPreferences.KeyModel) ? PlayerPrefs.GetString(LlmGlobalPreferences.KeyModel) : defaults.Model,
                "llama3.2:latest",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(592f, 38f));
            y -= 64f;

            var statusText = RuntimeUiFactory.CreateText(panel, "StatusText",
                LlmRuntimeStatus.HasProbeResult
                    ? (LlmRuntimeStatus.LastProbeOk ? "Last check: OK" : "Last check: failed")
                    : "Last check: (not run)",
                15, TextAnchor.UpperLeft, new Color(0.85f, 0.9f, 1f, 1f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, y), new Vector2(592f, 56f),
                VerticalWrapMode.Truncate);

            var host = Object.FindFirstObjectByType<MainMenuController>();

            var testBtn = RuntimeUiFactory.CreateButton(panel, "TestLlmButton", "Test connection",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-140f, 24f), new Vector2(180f, 40f));
            var applyBtn = RuntimeUiFactory.CreateButton(panel, "ApplyLlmButton", "Save",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(140f, 24f), new Vector2(160f, 40f));
            var closeBtn = RuntimeUiFactory.CreateButton(root, "CloseLlmButton", "Close",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(200f, 44f));

            void ApplyPrefs()
            {
                LlmGlobalPreferences.SetGloballyEnabled(useLlm);
                LlmGlobalPreferences.SetBaseUrl(urlField.text);
                LlmGlobalPreferences.SetModel(modelField.text);
                PlayerPrefs.Save();
                GameEvents.RaiseToast("LLM settings saved.");
            }

            IEnumerator TestRoutine()
            {
                ApplyPrefs();
                var resolved = NpcLlmSettings.ResolveForDriver(null);
                var ok = false;
                string msg = null;
                yield return NpcLlmHealthCheck.Probe(resolved, (success, m) =>
                {
                    ok = success;
                    msg = m;
                });
                LlmRuntimeStatus.SetProbeResult(ok, ok ? "OK" : (msg ?? ""));
                statusText.text = ok
                    ? $"OK — {resolved.CompletionsUrl}"
                    : $"Failed: {(string.IsNullOrWhiteSpace(msg) ? "unknown" : (msg.Length > 120 ? msg.Substring(0, 117) + "…" : msg))}";
                GameEvents.RaiseToast(ok ? "LLM connection OK." : "LLM test failed.");
            }

            if (host != null)
            {
                testBtn.onClick.AddListener(() => host.StartCoroutine(TestRoutine()));
                applyBtn.onClick.AddListener(ApplyPrefs);
            }

            closeBtn.onClick.AddListener(() =>
            {
                Object.Destroy(root.gameObject);
                onClosed?.Invoke();
            });

            return root;
        }
    }
}
