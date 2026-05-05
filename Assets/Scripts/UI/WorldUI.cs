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

            if (LlmRuntimeStatus.HasProbeResult)
            {
                llmStatusText.color = LlmRuntimeStatus.LastProbeOk ? new Color(0.65f, 0.95f, 0.75f, 1f) : new Color(1f, 0.65f, 0.55f, 1f);
                llmStatusText.text = LlmRuntimeStatus.LastProbeOk
                    ? $"LLM\n{Shorten(LlmRuntimeStatus.LastProbeMessage, 28)}"
                    : "LLM\noffline";
            }
            else
            {
                llmStatusText.color = new Color(0.85f, 0.88f, 0.95f, 1f);
                llmStatusText.text = "LLM\n…";
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
            if (root != null && llmStatusText == null)
            {
                Destroy(root.gameObject);
                root = null;
            }

            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;

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
            llmStatusText = RuntimeUiFactory.CreateText(header, "LlmStatusText", "LLM\n...", 12, TextAnchor.UpperRight, GameVisualTheme.Text,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(105f, 34f),
                VerticalWrapMode.Truncate);
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

        static string Shorten(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\n", " ").Trim();
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }
    }
}
