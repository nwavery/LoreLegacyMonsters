using UnityEngine;
using UnityEngine.UI;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog.Llm;
using UnityEngine.InputSystem;

namespace LoreLegacyMonsters.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] MainMenuController menuController;

        RectTransform root;
        RectTransform helpOverlayRoot;
        RectTransform aboutOverlayRoot;
        InputField nameField;
        Button newGameButton;
        Button loadButton;
        Button llmCheckButton;
        Button llmSettingsButton;
        Button helpButton;
        Button aboutButton;
        Button quitButton;
        Text llmStatusText;

        public void Bind(MainMenuController controller) => menuController = controller;

        void Start() => EnsureUi();

        void Update()
        {
            menuController ??= FindFirstObjectByType<MainMenuController>();
            if (menuController == null || UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            SetVisible(true);
            if (nameField != null && !nameField.isFocused)
                nameField.text = menuController.PendingPlayerName;
            if (loadButton != null)
                loadButton.interactable = menuController.CanLoadSlot0;
            if (llmStatusText != null)
                llmStatusText.text = LlmRuntimeStatus.HasProbeResult
                    ? (LlmRuntimeStatus.LastProbeOk ? $"LLM ready: {LlmRuntimeStatus.LastProbeMessage}" : $"LLM offline: {LlmRuntimeStatus.LastProbeMessage}")
                    : LlmRuntimeStatus.BootProbeInProgress
                        ? "Local LLM is starting… First bundled import needs a minute or longer on slower disks."
                        : "Local LLM has not been checked yet. Use \"Test LLM connection\" before starting.";
            HandleEscapeFromMenu();

        }

        void OnDestroy()
        {
            CloseOverlays();
            if (root != null) Destroy(root.gameObject);
        }

        void HandleEscapeFromMenu()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame || menuController == null) return;

            if (helpOverlayRoot != null)
            {
                Destroy(helpOverlayRoot.gameObject);
                helpOverlayRoot = null;
                return;
            }

            if (aboutOverlayRoot != null)
            {
                Destroy(aboutOverlayRoot.gameObject);
                aboutOverlayRoot = null;
                return;
            }

            if (menuController.CloseLlmSettingsOverlayIfOpen())
                return;

            if (nameField != null && nameField.isFocused)
                return;

            menuController.OnQuit();
        }

        void CloseOverlays()
        {
            if (helpOverlayRoot != null)
            {
                Destroy(helpOverlayRoot.gameObject);
                helpOverlayRoot = null;
            }

            if (aboutOverlayRoot != null)
            {
                Destroy(aboutOverlayRoot.gameObject);
                aboutOverlayRoot = null;
            }
        }

        void OpenHelp()
        {
            if (UIManager.Instance?.Root == null) return;
            if (helpOverlayRoot != null)
            {
                Destroy(helpOverlayRoot.gameObject);
                helpOverlayRoot = null;
                return;
            }

            if (aboutOverlayRoot != null)
            {
                Destroy(aboutOverlayRoot.gameObject);
                aboutOverlayRoot = null;
            }
            helpOverlayRoot = HelpOverlayUtility.Create(UIManager.Instance.Root.transform, AlphaHelpText.ControlsTitle,
                AlphaHelpText.ControlsBody, () => helpOverlayRoot = null);
        }

        void OpenAbout()
        {
            if (UIManager.Instance?.Root == null) return;
            if (aboutOverlayRoot != null)
            {
                Destroy(aboutOverlayRoot.gameObject);
                aboutOverlayRoot = null;
                return;
            }

            if (helpOverlayRoot != null)
            {
                Destroy(helpOverlayRoot.gameObject);
                helpOverlayRoot = null;
            }
            aboutOverlayRoot = HelpOverlayUtility.Create(UIManager.Instance.Root.transform, "About this build",
                AlphaBuildInfo.FormatAboutText(), () => aboutOverlayRoot = null);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreatePanel(UIManager.Instance.Root.transform, "MainMenuRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.98f), Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            BuildBackdrop(root);
            var menuCard = RuntimeUiFactory.CreateCard(root, "MainMenuCard", GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.96f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 0f), new Vector2(620f, 650f),
                GameVisualTheme.Accent);
            var titleCard = RuntimeUiFactory.CreateCard(root, "TitleCard", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.94f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, 116f), new Vector2(560f, 320f),
                GameVisualTheme.AccentBlue);
            RuntimeUiFactory.CreateText(titleCard, "Title", "Lore,\nLegacy,\n& Monsters", 42, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(32f, -30f), new Vector2(-64f, 168f));
            RuntimeUiFactory.CreateDivider(titleCard, "TitleDivider", new Vector2(0f, -204f), 480f, GameVisualTheme.WithAlpha(GameVisualTheme.Accent, 0.7f));
            RuntimeUiFactory.CreateText(titleCard, "Subtitle", "A cozy monster RPG where local lore talks back.", 19, TextAnchor.UpperLeft,
                GameVisualTheme.Text, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(32f, -222f), new Vector2(-64f, 56f),
                VerticalWrapMode.Truncate);
            RuntimeUiFactory.CreateStatusBadge(titleCard, "BuildBadge", "Internal PC alpha", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(32f, 26f), new Vector2(190f, 28f), GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.9f));
            var firstRunCard = RuntimeUiFactory.CreateCard(root, "FirstRunCard", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.88f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -178f), new Vector2(560f, 302f),
                GameVisualTheme.AccentGreen);
            RuntimeUiFactory.CreateStatusBadge(firstRunCard, "FirstRunBadge", "First Session", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(160f, 28f), GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.86f));
            var firstRunText = RuntimeUiFactory.CreateText(firstRunCard, "FirstRunText",
                "1) Enter your trainer name and start New Game.\n2) Talk to Mira in town, then follow the route marker.\n3) Use M for map and J for quest log whenever you lose the thread.",
                16, TextAnchor.UpperLeft, GameVisualTheme.Text, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -52f), new Vector2(-38f, 170f), VerticalWrapMode.Truncate);
            RuntimeUiFactory.ApplyHintTextStyle(firstRunText);

            RuntimeUiFactory.CreateText(menuCard, "NameLabel", "Trainer Name", 18, TextAnchor.UpperLeft, GameVisualTheme.MutedText,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -42f), new Vector2(220f, 24f));
            nameField = RuntimeUiFactory.CreateInputField(root, "NameField", "Hero", "Hero",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(70f, 236f), new Vector2(360f, 44f));
            nameField.onValueChanged.AddListener(value => menuController?.SetPlayerName(value));

            newGameButton = RuntimeUiFactory.CreatePrimaryActionButton(root, "NewGameButton", "New Game",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 152f), new Vector2(460f, 58f));
            loadButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "LoadButton", "Load Slot 0",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 82f), new Vector2(460f, 50f));
            var statusCard = RuntimeUiFactory.CreateCard(root, "LlmStatusCard", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.72f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, -4f), new Vector2(460f, 70f),
                GameVisualTheme.AccentGreen);
            llmStatusText = RuntimeUiFactory.CreateText(statusCard, "LlmStatusText", string.Empty, 15, TextAnchor.MiddleCenter,
                GameVisualTheme.MutedText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-20f, -8f),
                VerticalWrapMode.Truncate);
            llmSettingsButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "LlmSettingsButton", "Local LLM settings",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(92f, -96f), new Vector2(224f, 42f));
            llmCheckButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "LlmCheckButton", "Test LLM connection",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(328f, -96f), new Vector2(224f, 42f));
            helpButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "HelpButton", "Help",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(92f, -150f), new Vector2(224f, 42f));
            aboutButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "AboutButton", "About build",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(328f, -150f), new Vector2(224f, 42f));
            quitButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "QuitButton", "Quit",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, -222f), new Vector2(260f, 42f));
            newGameButton.onClick.AddListener(() => menuController?.OnNewGame());
            loadButton.onClick.AddListener(() => menuController?.OnLoadSlot(0));
            llmSettingsButton.onClick.AddListener(() => menuController?.OpenLlmSettings());
            llmCheckButton.onClick.AddListener(() => menuController?.RunLlmHealthCheck());
            helpButton.onClick.AddListener(OpenHelp);
            aboutButton.onClick.AddListener(OpenAbout);
            quitButton.onClick.AddListener(() => menuController?.OnQuit());
        }

        static void BuildBackdrop(RectTransform parent)
        {
            RuntimeUiFactory.CreatePanel(parent, "BackSkyTop", GameVisualTheme.SkyTop,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreatePanel(parent, "BackSkyGlow", GameVisualTheme.WithAlpha(GameVisualTheme.SkyBottom, 0.88f),
                new Vector2(0f, 0.24f), new Vector2(1f, 0.78f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreatePanel(parent, "FarHills", GameVisualTheme.WithAlpha(GameVisualTheme.Forest, 0.78f),
                new Vector2(0f, 0.28f), new Vector2(1f, 0.42f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            RuntimeUiFactory.CreatePanel(parent, "NearGround", GameVisualTheme.Hex(0x45, 0x6B, 0x3A),
                new Vector2(0f, 0f), new Vector2(1f, 0.3f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            for (var i = 0; i < 7; i++)
            {
                var x = -600f + i * 190f;
                RuntimeUiFactory.CreatePanel(parent, $"BackTreeTrunk_{i}", GameVisualTheme.RoadDark,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 185f), new Vector2(22f, 78f));
                RuntimeUiFactory.CreatePanel(parent, $"BackTreeCrown_{i}", i % 2 == 0 ? GameVisualTheme.Forest : GameVisualTheme.Moss,
                    new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 250f + (i % 2) * 18f), new Vector2(112f, 86f));
            }
            RuntimeUiFactory.CreatePanel(parent, "RoadGlow", GameVisualTheme.WithAlpha(GameVisualTheme.Road, 0.55f),
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-120f, 52f), new Vector2(840f, 90f));
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
