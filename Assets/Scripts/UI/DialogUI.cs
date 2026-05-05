using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.UI
{
    public class DialogUI : MonoBehaviour
    {
        [SerializeField] Dialog.DialogSystem system;
        [SerializeField] Dialog.GameDialogDriver driver;
        [SerializeField] float typewriterCharsPerSecond = 60f;

        RectTransform root;
        Text speakerText;
        Text lineText;
        Button continueButton;
        Button viewWaresButton;
        Button closeButton;
        RectTransform choiceRoot;
        Text thinkingText;
        Text helperText;
        InputField replyInput;
        Button sendButton;
        RectTransform suggestionRoot;
        string suggestionSignature;
        float typedChars;
        string typedSource = string.Empty;

        public bool IsTypewriterRevealComplete =>
            string.IsNullOrEmpty(typedSource) || typedChars >= typedSource.Length;

        void Start() => EnsureUi();

        void Update()
        {
            driver ??= FindFirstObjectByType<Dialog.GameDialogDriver>();
            system ??= driver != null ? driver.GetComponent<Dialog.DialogSystem>() : FindFirstObjectByType<Dialog.DialogSystem>();
            if (UIManager.Instance == null || driver == null)
            {
                SetVisible(false);
                return;
            }

            if (UIManager.Instance.IsModalOpen(UiModal.Combat))
            {
                SetVisible(false);
                return;
            }

            EnsureUi();

            var kbThis = Keyboard.current;
            if (kbThis != null && kbThis.escapeKey.wasPressedThisFrame && driver.IsConversationOpen)
            {
                driver.CloseConversation();
                SetVisible(false);
                ResetTypewriter();
                return;
            }

            if (driver.IsBusy)
            {
                SetVisible(true);
                root.SetAsLastSibling();
                if (viewWaresButton != null) viewWaresButton.gameObject.SetActive(false);
                if (choiceRoot != null) choiceRoot.gameObject.SetActive(false);
                thinkingText.gameObject.SetActive(true);
                continueButton.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(true);
                helperText.gameObject.SetActive(false);
                replyInput.gameObject.SetActive(false);
                sendButton.gameObject.SetActive(false);
                suggestionRoot.gameObject.SetActive(false);

                if (driver.TryGetCurrentLine(out var streaming) && !string.IsNullOrEmpty(streaming.line))
                {
                    speakerText.text = streaming.speaker;
                    var shownStream = AdvanceTypewriter(streaming.line);
                    lineText.text = shownStream;
                }
                else
                {
                    speakerText.text = driver.ActiveNpc != null ? driver.ActiveNpc.DisplayName : "Local Model";
                    lineText.text = "Contacting the local model...";
                    ResetTypewriter();
                }
                return;
            }

            if (!driver.IsConversationOpen)
            {
                SetVisible(false);
                ResetTypewriter();
                return;
            }

            SetVisible(true);
            root.SetAsLastSibling();
            UpdateViewWaresVisibility();
            thinkingText.gameObject.SetActive(false);
            if (driver.TryGetCurrentLine(out var entry))
            {
                var hasChoices = entry.HasChoices();
                continueButton.gameObject.SetActive(!hasChoices);
                closeButton.gameObject.SetActive(true);
                helperText.gameObject.SetActive(false);
                replyInput.gameObject.SetActive(false);
                sendButton.gameObject.SetActive(false);
                suggestionRoot.gameObject.SetActive(false);
                if (choiceRoot != null) choiceRoot.gameObject.SetActive(hasChoices);
                if (hasChoices) RebuildChoices(entry);
                speakerText.text = entry.speaker;
                var shownEntry = AdvanceTypewriter(entry.line);
                lineText.text = shownEntry;
                return;
            }

            ResetTypewriter();
            UpdateViewWaresVisibility();
            continueButton.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(true);
            speakerText.text = driver.ActiveNpc != null ? driver.ActiveNpc.DisplayName : "Conversation";
            lineText.text = "Ask a follow-up question, use a quick prompt, or leave the conversation.";
            helperText.gameObject.SetActive(driver.CanAcceptPlayerReply);
            replyInput.gameObject.SetActive(driver.CanAcceptPlayerReply);
            sendButton.gameObject.SetActive(driver.CanAcceptPlayerReply);
            suggestionRoot.gameObject.SetActive(driver.CanAcceptPlayerReply);
            helperText.text = "Ask a short question or use a quick prompt below.";

            if (driver.CanAcceptPlayerReply)
            {
                RebuildSuggestions();
                var kb = Keyboard.current;
                if (kb != null && kb.enterKey.wasPressedThisFrame && !string.IsNullOrWhiteSpace(replyInput.text))
                    SendReply();
            }
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        string AdvanceTypewriter(string fullLine)
        {
            var line = fullLine ?? string.Empty;
            if (line.Length == 0)
            {
                typedSource = string.Empty;
                typedChars = 0f;
                return string.Empty;
            }

            var commonPrefix = CountCommonPrefix(typedSource, line);
            if (commonPrefix < Mathf.Min(typedSource.Length, line.Length))
                typedChars = commonPrefix;
            typedSource = line;
            typedChars = Mathf.Min(typedChars + typewriterCharsPerSecond * Time.unscaledDeltaTime, line.Length);
            var shown = Mathf.Clamp(Mathf.CeilToInt(typedChars), 0, line.Length);
            return line.Substring(0, shown);
        }

        static int CountCommonPrefix(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
            var n = Mathf.Min(a.Length, b.Length);
            for (var i = 0; i < n; i++) if (a[i] != b[i]) return i;
            return n;
        }

        void ResetTypewriter()
        {
            typedChars = 0f;
            typedSource = string.Empty;
        }

        void UpdateViewWaresVisibility()
        {
            if (viewWaresButton == null || driver == null) return;
            var show = driver.IsConversationOpen && !driver.IsBusy && driver.ActiveNpc != null && driver.ActiveNpc.Shop != null;
            viewWaresButton.gameObject.SetActive(show);
        }

        void OnViewWaresClicked()
        {
            if (driver == null) return;
            var npc = driver.ActiveNpc;
            var shop = npc?.Shop;
            if (shop == null) return;
            var oc = FindFirstObjectByType<OverworldChapterController>();
            driver.EndConversationFromUi();
            oc?.OpenShopForNpc(shop);
        }

        void OnContinueClicked()
        {
            if (driver == null) return;
            if (!IsTypewriterRevealComplete && !string.IsNullOrEmpty(typedSource))
            {
                typedChars = typedSource.Length;
                return;
            }
            driver.AdvanceConversation();
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "DialogRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1100f, 380f), GameVisualTheme.Accent);
            RuntimeUiFactory.CreatePanel(root, "TopTrim", GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 5f));
            var portrait = RuntimeUiFactory.CreatePanel(root, "PortraitFrame",
                GameVisualTheme.PanelInner, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(20f, -24f), new Vector2(110f, 110f));
            RuntimeUiFactory.CreatePanel(portrait, "PortraitFace",
                GameVisualTheme.Parchment, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(54f, 54f));
            RuntimeUiFactory.CreatePanel(portrait, "PortraitBody",
                GameVisualTheme.AccentGreen, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(72f, 34f));
            speakerText = RuntimeUiFactory.CreateText(root, "SpeakerText", string.Empty, 24, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(148f, -20f), new Vector2(500f, 30f));
            var bubble = RuntimeUiFactory.CreateCard(root, "DialogBubble", GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.72f),
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(66f, 30f), new Vector2(-186f, -160f),
                GameVisualTheme.WithAlpha(GameVisualTheme.Parchment, 0.34f));
            lineText = RuntimeUiFactory.CreateText(bubble, "LineText", string.Empty, 19, TextAnchor.UpperLeft, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            lineText.rectTransform.offsetMin = new Vector2(20f, 18f);
            lineText.rectTransform.offsetMax = new Vector2(-20f, -18f);
            lineText.horizontalOverflow = HorizontalWrapMode.Wrap;
            lineText.verticalOverflow = VerticalWrapMode.Overflow;
            continueButton = RuntimeUiFactory.CreateButton(root, "ContinueButton", "Continue [Space]",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 20f), new Vector2(140f, 40f));
            RuntimeUiFactory.ApplyActionButton(continueButton, primary: true);
            continueButton.onClick.AddListener(OnContinueClicked);
            viewWaresButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "ViewWaresButton", "View Wares",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-176f, -20f), new Vector2(140f, 36f));
            viewWaresButton.onClick.AddListener(OnViewWaresClicked);
            viewWaresButton.gameObject.SetActive(false);
            closeButton = RuntimeUiFactory.CreateSecondaryActionButton(root, "CloseButton", "Leave [Esc]",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(132f, 36f));
            closeButton.onClick.AddListener(() => driver?.CloseConversation());
            thinkingText = RuntimeUiFactory.CreateText(root, "ThinkingText", "Local model is thinking...", 18, TextAnchor.LowerRight, GameVisualTheme.Accent,
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 70f), new Vector2(260f, 20f));
            helperText = RuntimeUiFactory.CreateText(root, "HelperText", string.Empty, 16, TextAnchor.LowerLeft, GameVisualTheme.MutedText,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(148f, 94f), new Vector2(690f, 20f));
            RuntimeUiFactory.ApplyHintTextStyle(helperText);
            replyInput = RuntimeUiFactory.CreateInputField(root, "ReplyInput", string.Empty, "Ask about the town, quests, rumors, or your party...",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(148f, 60f), new Vector2(692f, 34f));
            sendButton = RuntimeUiFactory.CreatePrimaryActionButton(root, "SendButton", "Send",
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 60f), new Vector2(120f, 34f));
            sendButton.onClick.AddListener(SendReply);
            suggestionRoot = RuntimeUiFactory.CreatePanel(root, "SuggestionRoot",
                new Color(0f, 0f, 0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(148f, 20f), new Vector2(692f, 32f));
            choiceRoot = RuntimeUiFactory.CreatePanel(root, "ChoiceRoot",
                new Color(0f, 0f, 0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(148f, 20f), new Vector2(692f, 76f));
            choiceRoot.gameObject.SetActive(false);
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }

        void SendReply()
        {
            if (driver == null || replyInput == null) return;
            var text = replyInput.text;
            if (string.IsNullOrWhiteSpace(text)) return;
            driver.SubmitPlayerMessage(text.Trim());
            replyInput.text = string.Empty;
        }

        void RebuildSuggestions()
        {
            if (driver == null || suggestionRoot == null) return;
            var replies = driver.SuggestedReplies;
            var newSignature = replies == null ? string.Empty : string.Join("|", replies);
            if (newSignature == suggestionSignature) return;
            suggestionSignature = newSignature;

            RuntimeUiFactory.DestroyChildren(suggestionRoot);
            if (replies == null) return;

            const float chipWidth = 220f;
            const float chipGap = 12f;
            var x = 0f;
            for (var i = 0; i < replies.Count && i < 3; i++)
            {
                var reply = replies[i];
                if (string.IsNullOrWhiteSpace(reply)) continue;
                var button = RuntimeUiFactory.CreateButton(suggestionRoot, $"Suggestion_{i}", reply,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 0f), new Vector2(chipWidth, 32f));
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.fontSize = 15;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                }
                var captured = reply;
                button.onClick.AddListener(() =>
                {
                    if (replyInput != null) replyInput.text = captured;
                    SendReply();
                });
                x += chipWidth + chipGap;
            }
        }

        void RebuildChoices(Dialog.DialogEntry entry)
        {
            if (entry == null || choiceRoot == null) return;
            RuntimeUiFactory.DestroyChildren(choiceRoot);
            const float buttonHeight = 32f;
            const float gap = 8f;
            var y = 0f;
            for (var i = 0; i < entry.choiceNextIds.Length; i++)
            {
                var label = entry.GetChoiceLabel(i);
                if (string.IsNullOrWhiteSpace(label)) continue;
                var button = RuntimeUiFactory.CreateButton(choiceRoot, $"Choice_{i}", label,
                    new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, y), new Vector2(692f, buttonHeight));
                var capturedIndex = i;
                button.onClick.AddListener(() => driver?.SelectDialogChoice(capturedIndex));
                y += buttonHeight + gap;
            }
        }
    }
}
