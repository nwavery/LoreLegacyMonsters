using UnityEngine;
using UnityEngine.UI;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.UI
{
    public class MainStoryQuestUI : MonoBehaviour
    {
        [SerializeField] OverworldChapterController controller;

        RectTransform root;
        Text titleText;
        Text detailsText;

        public void Bind(OverworldChapterController chapterController) => controller = chapterController;

        void Start() => EnsureUi();

        void Update()
        {
            controller ??= FindFirstObjectByType<OverworldChapterController>();
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
            if (titleText == null || detailsText == null)
                return;

            SetVisible(true);
            root.SetAsLastSibling();
            titleText.text = controller.QuestTitle;
            detailsText.text = BuildGuidedSummary();
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }

        void EnsureUi()
        {
            if (root != null || UIManager.Instance == null || UIManager.Instance.Root == null) return;
            root = RuntimeUiFactory.CreateCard(UIManager.Instance.Root.transform, "MainStoryQuestRoot",
                GameVisualTheme.WithAlpha(GameVisualTheme.Panel, 0.9f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-12f, -154f), new Vector2(270f, 130f), GameVisualTheme.Accent);
            titleText = RuntimeUiFactory.CreateText(root, "QuestTitle", "Quest Tracker", 17, TextAnchor.UpperLeft, GameVisualTheme.Accent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(246f, 24f));
            detailsText = RuntimeUiFactory.CreateText(root, "QuestDetails", string.Empty, 13, TextAnchor.UpperLeft, GameVisualTheme.Text,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -40f), new Vector2(246f, 76f));
        }

        string BuildGuidedSummary()
        {
            if (controller == null || controller.Quests == null)
                return "No active quest.\nHint: Press J for full quest log.";

            var summary = controller.QuestSummary;
            var objectiveId = controller.Quests.GetPrimaryQuestObjectiveId();
            var controlHint = BuildControlHint(objectiveId);
            var whyHint = BuildWhyHint(objectiveId);
            var branchHint = BuildBranchHint();
            return string.IsNullOrWhiteSpace(branchHint)
                ? $"{summary}\n\nHint: {controlHint}\nWhy: {whyHint}"
                : $"{summary}\n\nHint: {controlHint}\nWhy: {whyHint}\nChoice: {branchHint}";
        }

        static string BuildControlHint(string objectiveId)
        {
            return objectiveId switch
            {
                ChapterOneIds.TalkElder or ChapterOneIds.TalkScout or ChapterTwoIds.TalkArchivist or ChapterThreeIds.TalkWarden
                    or ChapterThreeIds.TalkMentor or PhaseTwoIds.TalkCartographer or PhaseTwoIds.TalkEthicist
                    => "Use WASD to move to the marker and E to talk.",
                ChapterOneIds.DefeatBoss or ChapterTwoIds.DefeatRival or ChapterThreeIds.DefeatSpireBoss or PhaseTwoIds.DefeatSable
                    => "Prepare with I (inventory), then start the fight and use 1-7 commands in battle.",
                ChapterOneIds.VisitRoute or ChapterOneIds.VisitForest or ChapterTwoIds.VisitMarsh or ChapterTwoIds.VisitRuins
                    or ChapterThreeIds.VisitDelta or ChapterThreeIds.VisitRidge or ChapterThreeIds.VisitSpire
                    => "Press M to confirm route, then move with WASD toward X.",
                _ => "Press M to check the next lead, J for details, and E to interact."
            };
        }

        static string BuildWhyHint(string objectiveId)
        {
            return objectiveId switch
            {
                ChapterOneIds.TalkElder => "Mira unlocks your first battle path.",
                ChapterOneIds.TalkScout => "Rin points you toward the grove crisis.",
                ChapterOneIds.DefeatBoss => "Resolving Iona advances the main story arc.",
                ChapterTwoIds.DefeatRival => "Corin's outcome shapes Chapter 2 consequences.",
                ChapterThreeIds.DefeatSpireBoss => "Varo's resolution determines finale pressure.",
                PhaseTwoIds.DefeatSable => "Sable's trial gates the binding-choice finale.",
                _ => "This objective pushes campaign progression and unlocks new areas."
            };
        }

        static string BuildBranchHint()
        {
            var ending = StoryState.GetEnding();
            if (ending != StoryEnding.None)
                return $"Ending doctrine locked: {ending.ToString().ToLowerInvariant()} path now shapes nearby NPC reactions.";

            var varo = StoryState.GetOutcome(StoryState.VaroOutcomeKey);
            if (varo == StoryState.VaroAlly)
                return "Varo alliance active: phase-two patrols react to containment markers.";
            if (varo == StoryState.VaroRefuseSpire)
                return "Spire refusal active: NPCs now debate restraint over control.";

            var corin = StoryState.GetOutcome(StoryState.CorinOutcomeKey);
            if (corin == StoryState.CorinSideWithCorin)
                return "Corin alliance active: archivist and town lines now reflect divided trust.";
            if (corin == StoryState.CorinTalkDown)
                return "Corin talked down: nearby NPCs now emphasize caution and repair.";

            var iona = StoryState.GetOutcome(StoryState.IonaOutcomeKey);
            if (iona == StoryState.IonaSpare)
                return "Iona spared: grove NPC lines now react to your restraint.";
            if (iona == StoryState.IonaWithdraw)
                return "Iona withdrawal: route encounters and grove reactions are altered.";

            return string.Empty;
        }

        void SetVisible(bool visible)
        {
            if (root != null && root.gameObject.activeSelf != visible)
                root.gameObject.SetActive(visible);
        }
    }
}
