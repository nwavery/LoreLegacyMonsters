using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.World;
using UnityEngine.UI;

namespace LoreLegacyMonsters.UI
{
    public class QuestUI : MonoBehaviour
    {
        [SerializeField] QuestManager quests;
        [SerializeField] OverworldChapterController controller;

        RectTransform root;
        Text storyText;
        Text activeText;
        Text completedText;

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
            quests ??= controller != null ? controller.Quests : FindFirstObjectByType<QuestManager>();
            if (UIManager.Instance == null)
            {
                SetVisible(false);
                return;
            }

            EnsureUi();
            var open = UIManager.Instance.IsModalOpen(UiModal.QuestLog);
            SetVisible(open);
            if (!open || quests == null) return;
            root.SetAsLastSibling();

            storyText.text = BuildStorySection();
            activeText.text = BuildActiveSection();
            completedText.text = BuildCompletedSection();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "QuestLogRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 560f), GameVisualTheme.Accent);
            _ = RuntimeUiFactory.CreateModalWindowChrome(root, "Quest Log", GameVisualTheme.Accent, "Close [J]",
                () => UIManager.Instance?.SetModalOpen(UiModal.QuestLog, false));
            var storyCard = RuntimeUiFactory.CreateCard(root, "CurrentQuestCard", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.92f),
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -60f), new Vector2(402f, 160f),
                GameVisualTheme.AccentGreen);
            RuntimeUiFactory.CreateStatusBadge(storyCard, "CurrentBadge", "Current Story", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(160f, 28f), GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.9f));
            storyText = RuntimeUiFactory.CreateText(storyCard, "StoryText", string.Empty, 17, TextAnchor.UpperLeft, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-36f, -76f), VerticalWrapMode.Truncate);

            var activeCard = RuntimeUiFactory.CreateCard(root, "ActiveQuestCard", GameVisualTheme.WithAlpha(GameVisualTheme.PanelInner, 0.86f),
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(28f, 34f), new Vector2(402f, 300f),
                GameVisualTheme.AccentBlue);
            RuntimeUiFactory.CreateStatusBadge(activeCard, "ActiveBadge", "Side Leads", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(150f, 28f), GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.9f));
            activeText = RuntimeUiFactory.CreateText(activeCard, "ActiveText", string.Empty, 16, TextAnchor.UpperLeft, GameVisualTheme.Text,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-36f, -76f), VerticalWrapMode.Truncate);

            var doneCard = RuntimeUiFactory.CreateCard(root, "CompletedQuestCard", GameVisualTheme.WithAlpha(GameVisualTheme.InkSoft, 0.82f),
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-28f, -8f), new Vector2(410f, 462f),
                GameVisualTheme.ParchmentDark);
            RuntimeUiFactory.CreateStatusBadge(doneCard, "DoneBadge", "Completed", new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(18f, -16f), new Vector2(142f, 28f), GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.62f));
            completedText = RuntimeUiFactory.CreateText(doneCard, "CompletedText", string.Empty, 16, TextAnchor.UpperLeft, GameVisualTheme.MutedText,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(-36f, -76f), VerticalWrapMode.Truncate);
        }

        string BuildStorySection()
        {
            if (quests == null) return "Current Story";
            var id = quests.GetPrimaryQuestId();
            if (string.IsNullOrWhiteSpace(id)) return "No active quest.\n\nExplore Hollowfen or talk to Mira for the next lead.";
            return $"{quests.GetQuestChapterLabel(id)}\n\n{quests.GetQuestSummary(id)}\n\nNext: {quests.GetNextObjectiveText(id)}\n\nControls: M map marker · E interact · I inventory prep";
        }

        string BuildActiveSection()
        {
            if (quests == null) return "Other Active";
            var ids = quests.GetPrioritizedActiveIds();
            if (ids.Count == 0) return "No side leads yet.";

            var primaryId = quests.GetPrimaryQuestId();
            var lines = string.Empty;
            var found = false;
            foreach (var id in ids)
            {
                if (id == primaryId) continue;
                found = true;
                lines += $"- {quests.GetQuestChapterLabel(id)}\n{quests.GetQuestSummary(id)}\n\n";
            }

            return found ? lines.TrimEnd() : "No side leads yet.";
        }

        string BuildCompletedSection()
        {
            if (quests == null) return "Completed";
            var ids = quests.GetCompletedIds();
            if (ids == null || ids.Count == 0) return "Nothing completed yet.";

            var chapterOne = "Completed - Chapter 1\n";
            var chapterTwo = "Completed - Chapter 2\n";
            var other = "Completed - Other\n";
            foreach (var id in ids)
            {
                var label = quests.GetDefinition(id) != null ? quests.GetDefinition(id).DisplayName : id;
                switch (quests.GetQuestChapter(id))
                {
                    case 1:
                        chapterOne += $"- {label}\n";
                        break;
                    case 2:
                        chapterTwo += $"- {label}\n";
                        break;
                    default:
                        other += $"- {label}\n";
                        break;
                }
            }

            var lines = string.Empty;
            if (chapterOne != "Completed - Chapter 1\n") lines += chapterOne + "\n";
            if (chapterTwo != "Completed - Chapter 2\n") lines += chapterTwo + "\n";
            if (other != "Completed - Other\n") lines += other + "\n";
            return string.IsNullOrWhiteSpace(lines) ? "Completed\n- None" : lines.TrimEnd();
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
