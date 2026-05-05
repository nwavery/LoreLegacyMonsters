using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.World;
using UnityEngine;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    public class EndingUI : MonoBehaviour
    {
        OverworldChapterController controller;
        RectTransform root;
        Text titleText;
        Text bodyText;

        public void Bind(OverworldChapterController chapterController)
        {
            controller = chapterController;
        }

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            if (controller == null || UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            var open = controller.EndingChoiceOpen;
            UIManager.Instance.SetModalOpen(UiModal.Ending, open);
            SetVisible(open);
            if (!open) return;

            titleText.text = $"Ending Suggestion: {controller.SuggestedEnding}";
            bodyText.text = controller.EndingSuggestionText;
            root.SetAsLastSibling();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "EndingRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.97f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 430f), GameVisualTheme.Accent);
            titleText = RuntimeUiFactory.CreateText(root, "EndingTitle", "Final Decision", 24, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -20f), new Vector2(-20f, 30f));
            bodyText = RuntimeUiFactory.CreateText(root, "EndingBody", string.Empty, 18, TextAnchor.UpperLeft, GameVisualTheme.Text,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(20f, 120f), new Vector2(-20f, -80f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;

            AddEndingButton("Merge", StoryEnding.Merge, 20f);
            AddEndingButton("Seal", StoryEnding.Seal, 224f);
            AddEndingButton("Replace", StoryEnding.Replace, 428f);
            AddEndingButton("Burn", StoryEnding.Burn, 632f);
            root.gameObject.SetActive(false);
        }

        void AddEndingButton(string label, StoryEnding ending, float x)
        {
            var button = RuntimeUiFactory.CreatePrimaryActionButton(root, $"Ending_{label}", label,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 20f), new Vector2(190f, 42f));
            button.onClick.AddListener(() => controller?.ChooseEnding(ending));
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
