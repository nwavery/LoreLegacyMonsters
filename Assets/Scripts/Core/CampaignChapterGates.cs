using LoreLegacyMonsters;

namespace LoreLegacyMonsters.Core
{
    /// <summary>
    /// Pure quest-state helpers for overworld routing and NPC visibility (no per-frame story logic).
    /// </summary>
    public static class CampaignChapterGates
    {
        public static bool IsChapterTwoUnlocked(QuestManager questManager)
        {
            if (questManager == null) return false;
            return questManager.IsCompleted(ChapterOneIds.ReturnQuest) ||
                   questManager.IsActive(ChapterTwoIds.SignalQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.SignalQuest) ||
                   questManager.IsActive(ChapterTwoIds.ArchiveQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.ArchiveQuest) ||
                   questManager.IsActive(ChapterTwoIds.RivalQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.RivalQuest) ||
                   questManager.IsActive(ChapterTwoIds.ReturnQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.ReturnQuest);
        }

        public static bool IsRuinsUnlocked(QuestManager questManager)
        {
            if (questManager == null) return false;
            return questManager.IsCompleted(ChapterTwoIds.SignalQuest) ||
                   questManager.IsActive(ChapterTwoIds.ArchiveQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.ArchiveQuest) ||
                   questManager.IsActive(ChapterTwoIds.RivalQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.RivalQuest) ||
                   questManager.IsActive(ChapterTwoIds.ReturnQuest) ||
                   questManager.IsCompleted(ChapterTwoIds.ReturnQuest);
        }

        public static bool IsChapterThreeUnlocked(QuestManager questManager)
        {
            if (questManager == null) return false;
            return questManager.IsCompleted(ChapterTwoIds.ReturnQuest) ||
                   questManager.IsActive(ChapterThreeIds.BeaconQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.BeaconQuest) ||
                   questManager.IsActive(ChapterThreeIds.DeltaQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.DeltaQuest) ||
                   questManager.IsActive(ChapterThreeIds.RidgeQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.RidgeQuest) ||
                   questManager.IsActive(ChapterThreeIds.SpireQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.SpireQuest) ||
                   questManager.IsActive(ChapterThreeIds.ReturnQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.ReturnQuest);
        }

        public static bool IsRidgeUnlocked(QuestManager questManager)
        {
            if (questManager == null) return false;
            return questManager.IsCompleted(ChapterThreeIds.BeaconQuest) ||
                   questManager.IsActive(ChapterThreeIds.DeltaQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.DeltaQuest) ||
                   questManager.IsActive(ChapterThreeIds.RidgeQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.RidgeQuest) ||
                   questManager.IsActive(ChapterThreeIds.SpireQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.SpireQuest) ||
                   questManager.IsActive(ChapterThreeIds.ReturnQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.ReturnQuest);
        }

        public static bool IsSpireUnlocked(QuestManager questManager)
        {
            if (questManager == null) return false;
            return questManager.IsCompleted(ChapterThreeIds.DeltaQuest) ||
                   questManager.IsActive(ChapterThreeIds.RidgeQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.RidgeQuest) ||
                   questManager.IsActive(ChapterThreeIds.SpireQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.SpireQuest) ||
                   questManager.IsActive(ChapterThreeIds.ReturnQuest) ||
                   questManager.IsCompleted(ChapterThreeIds.ReturnQuest);
        }

        public static bool IsPhaseTwoUnlocked(QuestManager questManager)
        {
            if (questManager == null) return false;
            return questManager.IsCompleted(ChapterThreeIds.ReturnQuest) ||
                   questManager.IsActive(PhaseTwoIds.WiderMapQuest) ||
                   questManager.IsCompleted(PhaseTwoIds.WiderMapQuest) ||
                   questManager.IsActive(PhaseTwoIds.RoadsQuest) ||
                   questManager.IsCompleted(PhaseTwoIds.RoadsQuest) ||
                   questManager.IsActive(PhaseTwoIds.MoonwellQuest) ||
                   questManager.IsCompleted(PhaseTwoIds.MoonwellQuest) ||
                   questManager.IsActive(PhaseTwoIds.QuarryQuest) ||
                   questManager.IsCompleted(PhaseTwoIds.QuarryQuest) ||
                   questManager.IsActive(PhaseTwoIds.HollowSignalQuest) ||
                   questManager.IsCompleted(PhaseTwoIds.HollowSignalQuest) ||
                   questManager.IsActive(PhaseTwoIds.BindingChoiceQuest) ||
                   questManager.IsCompleted(PhaseTwoIds.BindingChoiceQuest);
        }
    }
}
