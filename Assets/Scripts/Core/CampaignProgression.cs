using System;
using System.Collections.Generic;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Platform.Steam;

namespace LoreLegacyMonsters.Core
{
    /// <summary>
    /// Event-driven main-story advancement: call after quest completion, save load, or initial boot.
    /// </summary>
    public static class CampaignProgression
    {
        readonly struct ProgressionStep
        {
            public readonly string PrerequisiteQuestId;
            public readonly string NextQuestId;
            public readonly string StatusText;

            public ProgressionStep(string prerequisiteQuestId, string nextQuestId, string statusText)
            {
                PrerequisiteQuestId = prerequisiteQuestId;
                NextQuestId = nextQuestId;
                StatusText = statusText;
            }
        }

        static readonly ProgressionStep[] MainlineSteps =
        {
            new(ChapterOneIds.IntroQuest, ChapterOneIds.ScoutQuest, "New quest: speak with Scout Rin on the eastern route."),
            new(ChapterOneIds.ScoutQuest, ChapterOneIds.BossQuest, "New quest: defeat the Briar Warden in the grove."),
            new(ChapterOneIds.BossQuest, ChapterOneIds.ReturnQuest, "Return to Elder Mira for your reward."),
            new(ChapterOneIds.ReturnQuest, ChapterTwoIds.SignalQuest, "New quest: speak with Mira about the lantern signal beyond the grove."),
            new(ChapterTwoIds.SignalQuest, ChapterTwoIds.ArchiveQuest, "New quest: find Archivist Sel in Lantern Marsh and reach the Sunken Archive."),
            new(ChapterTwoIds.ArchiveQuest, ChapterTwoIds.RivalQuest, "New quest: defeat Corin before he claims the archive relic."),
            new(ChapterTwoIds.RivalQuest, ChapterTwoIds.ReturnQuest, "Return to Elder Mira with news from the archive."),
            new(ChapterTwoIds.ReturnQuest, ChapterThreeIds.BeaconQuest, "New quest: speak with Mira about the storm beyond the archive."),
            new(ChapterThreeIds.BeaconQuest, ChapterThreeIds.DeltaQuest, "New quest: reach the Flooded Delta and find Warden Neris."),
            new(ChapterThreeIds.DeltaQuest, ChapterThreeIds.RidgeQuest, "New quest: climb to Stormbreak Ridge and seek Mentor Cael."),
            new(ChapterThreeIds.RidgeQuest, ChapterThreeIds.SpireQuest, "New quest: enter Skyglass Spire and defeat Varo, the Storm Tyrant."),
            new(ChapterThreeIds.SpireQuest, ChapterThreeIds.ReturnQuest, "Return to Elder Mira with news from the spire."),
            new(ChapterThreeIds.ReturnQuest, PhaseTwoIds.WiderMapQuest, "Phase 2 begins: find Jessa Vale in Stonewake and open the Wilderward map."),
            new(PhaseTwoIds.WiderMapQuest, PhaseTwoIds.RoadsQuest, "New quest: reopen the northern roads with Nia Reed."),
            new(PhaseTwoIds.RoadsQuest, PhaseTwoIds.MoonwellQuest, "New quest: investigate Moonwell Grove with Luma."),
            new(PhaseTwoIds.MoonwellQuest, PhaseTwoIds.QuarryQuest, "New quest: answer the tremors in Ironroot Quarry."),
            new(PhaseTwoIds.QuarryQuest, PhaseTwoIds.HollowSignalQuest, "New quest: follow the hollow signal to Starfall."),
            new(PhaseTwoIds.HollowSignalQuest, PhaseTwoIds.BindingChoiceQuest, "New quest: decide what Hollowfen should do with the lore network.")
        };

        static readonly Dictionary<string, string> CompletionAchievements = new Dictionary<string, string>
        {
            { ChapterOneIds.ReturnQuest, SteamAchievementIds.ChapterOneComplete },
            { ChapterTwoIds.ReturnQuest, SteamAchievementIds.ChapterTwoComplete },
            { ChapterThreeIds.ReturnQuest, SteamAchievementIds.ChapterThreeComplete },
            { PhaseTwoIds.BindingChoiceQuest, SteamAchievementIds.PhaseTwoComplete }
        };

        public static void TryAdvance(QuestManager questManager, AchievementSystem achievements, Action<string> setStatus)
        {
            if (questManager == null) return;

            if (ShouldStartIntro(questManager))
                questManager.StartQuest(ChapterOneIds.IntroQuest);

            for (var i = 0; i < MainlineSteps.Length; i++)
                TryStartStep(questManager, MainlineSteps[i], setStatus);

            foreach (var pair in CompletionAchievements)
            {
                if (questManager.IsCompleted(pair.Key))
                    achievements?.Unlock(pair.Value);
            }
        }

        static bool ShouldStartIntro(QuestManager questManager)
        {
            return !questManager.IsActive(ChapterOneIds.IntroQuest) &&
                   !questManager.IsCompleted(ChapterOneIds.IntroQuest) &&
                   questManager.GetCompletedIds().Count == 0;
        }

        static void TryStartStep(QuestManager questManager, ProgressionStep step, Action<string> setStatus)
        {
            if (!questManager.IsCompleted(step.PrerequisiteQuestId))
                return;
            if (questManager.IsActive(step.NextQuestId) || questManager.IsCompleted(step.NextQuestId))
                return;

            questManager.StartQuest(step.NextQuestId);
            setStatus?.Invoke(step.StatusText);
        }
    }
}
