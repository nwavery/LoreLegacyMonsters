using System;
using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog.Llm;
using LoreLegacyMonsters.World;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    public class WorldUI : MonoBehaviour
    {
        const int WorldHudLayoutRevision = 3;
        const string WorldHudLayoutPrefsKey = "world_hud_layout_revision";

        [SerializeField] WorldManager world;
        [SerializeField] OverworldChapterController controller;

        RectTransform root;
        RectTransform promptPanel;
        RectTransform toastPanel;
        Text areaText;
        Text goldText;
        Text weatherText;
        Text routeSummaryText;
        Text routeHintText;
        Text llmStatusText;
        Text promptText;
        Text toastText;
        Button saveButton;
        Button loadButton;

        RectTransform areaBannerRoot;
        Text areaBannerLabel;
        float areaBannerUntil;
        bool areaBannerSubscribed;

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void OnEnable()
        {
            if (areaBannerSubscribed) return;
            GameEvents.AreaChanged += OnAreaChanged;
            areaBannerSubscribed = true;
        }

        void OnDisable()
        {
            if (!areaBannerSubscribed) return;
            GameEvents.AreaChanged -= OnAreaChanged;
            areaBannerSubscribed = false;
        }

        void OnAreaChanged(string areaId)
        {
            if (string.IsNullOrEmpty(areaId)) return;
            world ??= controller != null ? controller.World : FindFirstObjectByType<WorldManager>();
            var display = world != null ? world.GetArea(areaId)?.DisplayName : null;
            var label = string.IsNullOrEmpty(display) ? areaId : display;
            areaBannerUntil = Time.unscaledTime + 2.35f;
            if (areaBannerRoot != null)
            {
                if (areaBannerLabel != null) areaBannerLabel.text = label;
                areaBannerRoot.gameObject.SetActive(true);
            }
        }

        void Start()
        {
            EnsureUi();
        }

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            world ??= controller != null ? controller.World : FindFirstObjectByType<WorldManager>();
            if (controller == null || UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            if (UIManager.Instance.IsModalOpen(UiModal.Combat))
            {
                SetVisible(false);
                return;
            }

            var dialogOpen = controller.DialogDriver != null &&
                             (controller.DialogDriver.IsConversationOpen || controller.DialogDriver.IsBusy);
            if (UIManager.Instance.IsBlockingWorldInput || dialogOpen)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            if (areaText == null || goldText == null || weatherText == null || routeSummaryText == null ||
                routeHintText == null || llmStatusText == null || promptText == null || toastText == null)
                return;

            SetVisible(true);
            root.SetAsLastSibling();
            if (promptPanel != null)
                promptPanel.gameObject.SetActive(true);

            if (areaBannerRoot != null)
            {
                if (Time.unscaledTime > areaBannerUntil && areaBannerRoot.gameObject.activeSelf)
                    areaBannerRoot.gameObject.SetActive(false);
            }

            if (LlmRuntimeStatus.BootProbeInProgress && !LlmRuntimeStatus.HasProbeResult)
            {
                llmStatusText.color = new Color(0.92f, 0.82f, 0.55f, 1f);
                llmStatusText.text = FormatLlmHudLine(importingModel: true, ok: false);
            }
            else if (LlmRuntimeStatus.HasProbeResult)
            {
                llmStatusText.color = LlmRuntimeStatus.LastProbeOk ? new Color(0.65f, 0.95f, 0.75f, 1f) : new Color(1f, 0.65f, 0.55f, 1f);
                llmStatusText.text = FormatLlmHudLine(
                    importingModel: false,
                    ok: LlmRuntimeStatus.LastProbeOk,
                    details: LlmRuntimeStatus.LastProbeMessage);
            }
            else
            {
                llmStatusText.color = new Color(0.85f, 0.88f, 0.95f, 1f);
                llmStatusText.text = FormatLlmHudLine(importingModel: false, ok: false, unknown: true);
            }

            areaText.text = $"AREA\n{controller.AreaName}";
            goldText.text = $"GOLD\n{(GameManager.Instance != null ? GameManager.Instance.PlayerGold : 0)}";
            weatherText.text = $"SKY\n{(controller.Weather != null ? controller.Weather.Current.ToString() : "Clear")}";
            routeSummaryText.text = $"PATH\n{controller.RouteSummaryCompact}";
            routeHintText.text = string.IsNullOrWhiteSpace(controller.RouteHint)
                ? string.Empty
                : $"NEXT\n{controller.RouteHint}";
            promptText.text = string.IsNullOrWhiteSpace(controller.PromptText)
                ? "[WASD] Move  [E] Interact  [Tab] Party  [J] Quests  [M] Map  [I] Inventory  |  Roads are safer than danger patches."
                : controller.PromptText;
            var toast = UIManager.Instance.CurrentToast;
            toastText.text = toast;
            if (toastPanel != null)
                toastPanel.gameObject.SetActive(!string.IsNullOrWhiteSpace(toast));
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (UIManager.Instance == null || UIManager.Instance.Root == null) return;

            InvalidateWorldHudIfLayoutOutdated(UIManager.Instance, this);

            if (root != null && llmStatusText == null)
            {
                Destroy(root.gameObject);
                root = null;
            }

            if (root != null)
            {
                ConfigureLlmStatusLayout(llmStatusText);
                return;
            }

            root = RuntimeUiFactory.CreatePanel(UIManager.Instance.Root.transform, "WorldHudRoot",
                new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
            root.GetComponent<Image>().raycastTarget = false;

            var header = RuntimeUiFactory.CreateCard(root, "HeaderPanel",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.92f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(560f, 188f), GameVisualTheme.Accent);
            RuntimeUiFactory.CreatePanel(header, "HeaderTrim",
                GameVisualTheme.Accent, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, 5f));
            areaText = RuntimeUiFactory.CreateText(header, "AreaText", string.Empty, 18, TextAnchor.UpperLeft, GameVisualTheme.Text,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -16f), new Vector2(170f, 42f));
            goldText = RuntimeUiFactory.CreateText(header, "GoldText", string.Empty, 17, TextAnchor.UpperLeft, GameVisualTheme.Text,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(198f, -16f), new Vector2(95f, 42f));
            weatherText = RuntimeUiFactory.CreateText(header, "WeatherText", string.Empty, 16, TextAnchor.UpperLeft, GameVisualTheme.Text,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(310f, -16f), new Vector2(130f, 42f));
            routeSummaryText = RuntimeUiFactory.CreateText(header, "RouteSummaryText", string.Empty, 14, TextAnchor.UpperLeft, GameVisualTheme.MutedText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -68f), new Vector2(500f, 34f),
                VerticalWrapMode.Truncate);
            RuntimeUiFactory.ApplyHintTextStyle(routeSummaryText, compact: true);
            routeHintText = RuntimeUiFactory.CreateText(header, "RouteHintText", string.Empty, 14, TextAnchor.UpperLeft, GameVisualTheme.Cream,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -106f), new Vector2(250f, 50f),
                VerticalWrapMode.Truncate);
            RuntimeUiFactory.ApplyHintTextStyle(routeHintText, compact: true);
            llmStatusText = RuntimeUiFactory.CreateText(header, "LlmStatusText",
                FormatLlmHudLine(importingModel: false, ok: false, unknown: true), 11,
                TextAnchor.MiddleRight, GameVisualTheme.Text,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-12f, 48f), new Vector2(300f, 30f),
                VerticalWrapMode.Truncate);
            llmStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            ConfigureLlmStatusLayout(llmStatusText);
            saveButton = RuntimeUiFactory.CreateSecondaryActionButton(header, "SaveButton", "Save",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-196f, 14f), new Vector2(76f, 30f), compact: true);
            loadButton = RuntimeUiFactory.CreateSecondaryActionButton(header, "LoadButton", "Load",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-112f, 14f), new Vector2(76f, 30f), compact: true);
            saveButton.onClick.AddListener(() => controller?.SaveCurrentGame());
            loadButton.onClick.AddListener(() => controller?.LoadCurrentGame());

            promptPanel = RuntimeUiFactory.CreateCard(root, "PromptPanel",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.94f), new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-24f, 42f), GameVisualTheme.AccentGreen);
            promptPanel.offsetMin = new Vector2(12f, 12f);
            promptPanel.offsetMax = new Vector2(-12f, 54f);
            promptText = RuntimeUiFactory.CreateText(promptPanel, "PromptText", string.Empty, 18, TextAnchor.MiddleLeft, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-24f, -8f));

            toastPanel = RuntimeUiFactory.CreateCard(root, "ToastPanel",
                GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.96f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(620f, 48f), GameVisualTheme.Accent);
            toastText = RuntimeUiFactory.CreateText(toastPanel, "ToastText", string.Empty, 19, TextAnchor.MiddleCenter, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-16f, -8f));
            toastPanel.gameObject.SetActive(false);

            areaBannerRoot = RuntimeUiFactory.CreatePanel(root, "AreaBanner", GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.96f),
                new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 44f));
            RuntimeUiFactory.CreatePanel(areaBannerRoot, "AreaBannerTrim", GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f));
            areaBannerLabel = RuntimeUiFactory.CreateText(areaBannerRoot, "AreaBannerLabel", string.Empty, 22, TextAnchor.MiddleCenter,
                GameVisualTheme.Accent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero,
                VerticalWrapMode.Truncate);
            areaBannerRoot.gameObject.SetActive(false);
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }

        /// <summary>Single-line status (truncated) so accessibility text scale cannot stack overlapping lines.</summary>
        static string FormatLlmHudLine(bool importingModel, bool ok, string details = null, bool unknown = false)
        {
            const string tag = "Local LLM";
            if (importingModel)
                return $"{tag} — Importing model…";

            if (unknown)
                return $"{tag} — Not checked yet";

            if (ok)
            {
                var model = ProbeDetailAfterOkPrefix(details ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(model))
                    model = "connected";
                return $"{tag} — Ready · {model}";
            }

            var trimmed = details == null ? string.Empty : details.Replace('\n', ' ').Trim();
            var err = Shorten(trimmed, 72);
            if (string.IsNullOrEmpty(err))
                err = "unreachable";
            return $"{tag} — Offline · {err}";
        }

        static void InvalidateWorldHudIfLayoutOutdated(UIManager ui, WorldUI owner)
        {
            if (ui?.Root == null) return;
            try
            {
                if (PlayerPrefs.GetInt(WorldHudLayoutPrefsKey, 0) >= WorldHudLayoutRevision)
                    return;

                var tr = ui.Root.transform.Find("WorldHudRoot");
                if (tr != null)
                    UnityEngine.Object.Destroy(tr.gameObject);

                owner?.ClearWorldHudWidgetRefs();
                PlayerPrefs.SetInt(WorldHudLayoutPrefsKey, WorldHudLayoutRevision);
                PlayerPrefs.Save();
            }
            catch
            {
                // never block HUD
            }
        }

        void ClearWorldHudWidgetRefs()
        {
            root = null;
            promptPanel = null;
            toastPanel = null;
            areaText = null;
            goldText = null;
            weatherText = null;
            routeSummaryText = null;
            routeHintText = null;
            llmStatusText = null;
            promptText = null;
            toastText = null;
            saveButton = null;
            loadButton = null;
            areaBannerRoot = null;
            areaBannerLabel = null;
        }

        static string ProbeDetailAfterOkPrefix(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            var s = raw.Trim();
            if (s.Length < 2 || !s.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
                return s;

            var i = 2;
            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
            while (i < s.Length && (s[i] == '—' || s[i] == '-' || s[i] == ':'))
            {
                i++;
                while (i < s.Length && char.IsWhiteSpace(s[i]))
                    i++;
            }

            return i >= s.Length ? string.Empty : s.Substring(i).TrimStart();
        }

        static void ConfigureLlmStatusLayout(Text t)
        {
            if (t == null) return;
            var scale = Mathf.Clamp(AccessibilitySettings.TextScale, 0.9f, 1.6f);
            var rowHeight = Mathf.RoundToInt(26f + 10f * ((scale - 0.9f) / 0.7f));
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-12f, 48f);
            rt.sizeDelta = new Vector2(300f, Mathf.Clamp(rowHeight, 26, 44));
            t.alignment = TextAnchor.MiddleRight;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.fontSize = Mathf.RoundToInt(11f * scale);
            t.resizeTextForBestFit = false;
        }

        static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ").Trim();
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }
    }
}
