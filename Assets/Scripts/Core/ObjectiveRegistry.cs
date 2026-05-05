using System.Collections.Generic;
using LoreLegacyMonsters;
using LoreLegacyMonsters.World;
using UnityEngine;

namespace LoreLegacyMonsters.Core
{
    public readonly struct BossBattleSpec
    {
        public readonly string MonsterId;
        public readonly string ObjectiveId;

        public BossBattleSpec(string monsterId, string objectiveId)
        {
            MonsterId = monsterId;
            ObjectiveId = objectiveId;
        }
    }

    public static class ObjectiveRegistry
    {
        readonly struct DefaultBossOutcome
        {
            public readonly string ObjectiveId;
            public readonly string OutcomeKey;
            public readonly string OutcomeValue;

            public DefaultBossOutcome(string objectiveId, string outcomeKey, string outcomeValue)
            {
                ObjectiveId = objectiveId;
                OutcomeKey = outcomeKey;
                OutcomeValue = outcomeValue;
            }
        }

        static readonly Dictionary<string, string> ObjectiveArea = new Dictionary<string, string>
        {
            { ChapterOneIds.TalkElder, DefaultGameContent.TownId },
            { ChapterOneIds.ReturnToElder, DefaultGameContent.TownId },
            { ChapterTwoIds.TalkElder, DefaultGameContent.TownId },
            { ChapterTwoIds.ReturnToElder, DefaultGameContent.TownId },
            { ChapterThreeIds.TalkElder, DefaultGameContent.TownId },
            { ChapterThreeIds.ReturnToElder, DefaultGameContent.TownId },
            { PhaseTwoIds.ReturnToMira, DefaultGameContent.TownId },
            { ChapterOneIds.VisitRoute, DefaultGameContent.RouteId },
            { ChapterOneIds.TalkScout, DefaultGameContent.RouteId },
            { ChapterOneIds.VisitForest, DefaultGameContent.ForestId },
            { ChapterOneIds.DefeatBoss, DefaultGameContent.GroveId },
            { ChapterTwoIds.VisitMarsh, DefaultGameContent.MarshId },
            { ChapterTwoIds.TalkArchivist, DefaultGameContent.MarshId },
            { ChapterTwoIds.VisitRuins, DefaultGameContent.RuinsId },
            { ChapterTwoIds.DefeatRival, DefaultGameContent.RuinsId },
            { ChapterThreeIds.VisitDelta, DefaultGameContent.DeltaId },
            { ChapterThreeIds.TalkWarden, DefaultGameContent.DeltaId },
            { ChapterThreeIds.TalkCollector, DefaultGameContent.DeltaId },
            { ChapterThreeIds.CatchDeltaMonsters, DefaultGameContent.DeltaId },
            { ChapterThreeIds.VisitRidge, DefaultGameContent.RidgeId },
            { ChapterThreeIds.TalkMentor, DefaultGameContent.RidgeId },
            { ChapterThreeIds.WinRidgeBattles, DefaultGameContent.RidgeId },
            { ChapterThreeIds.TalkRumorKeeper, DefaultGameContent.DeltaId },
            { ChapterThreeIds.VisitSpire, DefaultGameContent.SpireId },
            { ChapterThreeIds.DefeatSpireBoss, DefaultGameContent.SpireId },
            { "evolve_monster", DefaultGameContent.RidgeId },
            { PhaseTwoIds.TalkCartographer, DefaultGameContent.StonewakeId },
            { PhaseTwoIds.DiscoverStonewake, DefaultGameContent.StonewakeId },
            { PhaseTwoIds.TalkRunner, DefaultGameContent.MarshBasinId },
            { PhaseTwoIds.ClearRoadHazards, DefaultGameContent.MarshBasinId },
            { PhaseTwoIds.VisitMoonwell, DefaultGameContent.MoonwellId },
            { PhaseTwoIds.TalkMoonwellKeeper, DefaultGameContent.MoonwellId },
            { PhaseTwoIds.VisitQuarry, DefaultGameContent.QuarryId },
            { PhaseTwoIds.TalkForeman, DefaultGameContent.QuarryId },
            { PhaseTwoIds.WinQuarryBattles, DefaultGameContent.QuarryId },
            { PhaseTwoIds.VisitStarfall, DefaultGameContent.StarfallId },
            { PhaseTwoIds.TalkEthicist, DefaultGameContent.StarfallId },
            { PhaseTwoIds.DefeatSable, DefaultGameContent.CrossingId },
            { PhaseTwoIds.JessaVisitMoonwell, DefaultGameContent.MoonwellId },
            { PhaseTwoIds.JessaVisitQuarry, DefaultGameContent.QuarryId },
            { PhaseTwoIds.JessaVisitStarfall, DefaultGameContent.StarfallId },
            { PhaseTwoIds.LumaEvolveMonsters, DefaultGameContent.MoonwellId },
            { PhaseTwoIds.SableTalkOutcome, DefaultGameContent.CrossingId },
            { PhaseTwoIds.SableBattleOutcome, DefaultGameContent.CrossingId },
            { PhaseTwoIds.JessaEscortStonewake, DefaultGameContent.StonewakeId },
        };

        static readonly Dictionary<string, string[]> AreaVisitObjectives = new Dictionary<string, string[]>
        {
            { DefaultGameContent.RouteId, new[] { ChapterOneIds.VisitRoute } },
            { DefaultGameContent.ForestId, new[] { ChapterOneIds.VisitForest } },
            { DefaultGameContent.GroveId, new[] { ChapterOneIds.VisitForest } },
            { DefaultGameContent.MarshId, new[] { ChapterTwoIds.VisitMarsh } },
            { DefaultGameContent.RuinsId, new[] { ChapterTwoIds.VisitRuins } },
            { DefaultGameContent.DeltaId, new[] { ChapterThreeIds.VisitDelta } },
            { DefaultGameContent.RidgeId, new[] { ChapterThreeIds.VisitRidge } },
            { DefaultGameContent.SpireId, new[] { ChapterThreeIds.VisitSpire } },
            { DefaultGameContent.StonewakeId, new[] { PhaseTwoIds.DiscoverStonewake } },
            { DefaultGameContent.BramblewoodNorthId, new[] { PhaseTwoIds.ClearRoadHazards } },
            { DefaultGameContent.MarshBasinId, new[] { PhaseTwoIds.ClearRoadHazards } },
            { DefaultGameContent.MoonwellId, new[] { PhaseTwoIds.VisitMoonwell, PhaseTwoIds.JessaVisitMoonwell } },
            { DefaultGameContent.QuarryId, new[] { PhaseTwoIds.VisitQuarry, PhaseTwoIds.JessaVisitQuarry } },
            { DefaultGameContent.StarfallId, new[] { PhaseTwoIds.VisitStarfall, PhaseTwoIds.JessaVisitStarfall } },
        };

        static readonly Dictionary<string, string[]> NpcTalkObjectives = new Dictionary<string, string[]>
        {
            { NPCController.ElderMiraId, new[] { ChapterOneIds.TalkElder, ChapterOneIds.ReturnToElder, ChapterTwoIds.TalkElder, ChapterTwoIds.ReturnToElder, ChapterThreeIds.TalkElder, ChapterThreeIds.ReturnToElder, PhaseTwoIds.ReturnToMira } },
            { NPCController.ScoutRinId, new[] { ChapterOneIds.TalkScout } },
            { NPCController.ArchivistSelId, new[] { ChapterTwoIds.TalkArchivist } },
            { NPCController.WardenNerisId, new[] { ChapterThreeIds.TalkWarden } },
            { NPCController.MentorCaelId, new[] { ChapterThreeIds.TalkMentor } },
            { NPCController.CollectorVeyaId, new[] { ChapterThreeIds.TalkCollector } },
            { NPCController.RumorIrisId, new[] { ChapterThreeIds.TalkRumorKeeper } },
            { NPCController.CartographerJessaId, new[] { PhaseTwoIds.TalkCartographer } },
            { NPCController.RunnerNiaId, new[] { PhaseTwoIds.TalkRunner } },
            { NPCController.MoonwellLumaId, new[] { PhaseTwoIds.TalkMoonwellKeeper } },
            { NPCController.ForemanOrloId, new[] { PhaseTwoIds.TalkForeman } },
            { NPCController.EthicistThrenId, new[] { PhaseTwoIds.TalkEthicist } },
            { NPCController.SableRivalId, new[] { PhaseTwoIds.SableTalkOutcome } },
        };

        static readonly DefaultBossOutcome[] DefaultBossOutcomes =
        {
            new DefaultBossOutcome(ChapterOneIds.DefeatBoss, StoryState.IonaOutcomeKey, StoryState.IonaDefeat),
            new DefaultBossOutcome(ChapterTwoIds.DefeatRival, StoryState.CorinOutcomeKey, StoryState.CorinHandRelicToSel),
            new DefaultBossOutcome(ChapterThreeIds.DefeatSpireBoss, StoryState.VaroOutcomeKey, StoryState.VaroDefeat)
        };

        static readonly Dictionary<string, string> NpcStartsQuest = new Dictionary<string, string>
        {
            { NPCController.MentorCaelId, ChapterThreeIds.MentorQuest },
            { NPCController.CollectorVeyaId, ChapterThreeIds.CollectorQuest },
            { NPCController.RumorIrisId, ChapterThreeIds.RumorQuest },
            { NPCController.CartographerJessaId, PhaseTwoIds.JessaLandmarksQuest },
            { NPCController.MoonwellLumaId, PhaseTwoIds.LumaBondsQuest },
            { NPCController.SableRivalId, PhaseTwoIds.SableRematchQuest },
        };

        public static string ResolveAreaId(string objectiveId) =>
            !string.IsNullOrEmpty(objectiveId) && ObjectiveArea.TryGetValue(objectiveId, out var areaId) ? areaId : string.Empty;

        public static IReadOnlyDictionary<string, string> ObjectiveAreaMap => ObjectiveArea;

        public static void ReportAreaVisit(QuestManager quests, string areaId)
        {
            if (quests == null || string.IsNullOrEmpty(areaId) || !AreaVisitObjectives.TryGetValue(areaId, out var ids)) return;
            for (var i = 0; i < ids.Length; i++)
                quests.ReportObjectiveEvent(ids[i], 1);
            if (quests.IsCompleted(PhaseTwoIds.JessaLandmarksQuest))
                StoryFlags.SetFlag(PhaseTwoIds.FlagHelpedJessaLandmarks);
        }

        public static void ReportNpcInteraction(QuestManager quests, string npcId)
        {
            if (quests == null || string.IsNullOrEmpty(npcId)) return;
            if (NpcStartsQuest.TryGetValue(npcId, out var questId) && !quests.IsActive(questId) && !quests.IsCompleted(questId))
                quests.StartQuest(questId);
            if (!NpcTalkObjectives.TryGetValue(npcId, out var ids)) return;
            for (var i = 0; i < ids.Length; i++)
            {
                quests.ReportObjectiveEvent(ids[i], 1);
                if (ids[i] == PhaseTwoIds.SableTalkOutcome && quests.IsActive(PhaseTwoIds.SableRematchQuest))
                {
                    StoryFlags.SetFlag(PhaseTwoIds.FlagSablePeaceResolution);
                    quests.CompleteQuest(PhaseTwoIds.SableRematchQuest);
                }
            }
        }

        public static bool TryGetBossBattle(string npcId, QuestManager quests, out BossBattleSpec spec)
        {
            spec = default;
            if (quests == null || string.IsNullOrEmpty(npcId)) return false;
            if (npcId == NPCController.BossIonaId && quests.IsActive(ChapterOneIds.BossQuest) && !quests.IsCompleted(ChapterOneIds.BossQuest))
                return SetBoss(out spec, DefaultGameContent.ThornBeastId, ChapterOneIds.DefeatBoss);
            if (npcId == NPCController.RivalCorinId && quests.IsActive(ChapterTwoIds.RivalQuest) && !quests.IsCompleted(ChapterTwoIds.RivalQuest))
                return SetBoss(out spec, DefaultGameContent.BogWyrmId, ChapterTwoIds.DefeatRival);
            if (npcId == NPCController.StormTyrantId && quests.IsActive(ChapterThreeIds.SpireQuest) && !quests.IsCompleted(ChapterThreeIds.SpireQuest))
                return SetBoss(out spec, DefaultGameContent.DeltaKingId, ChapterThreeIds.DefeatSpireBoss);
            if (npcId == NPCController.SableRivalId)
            {
                if (quests.IsActive(PhaseTwoIds.BindingChoiceQuest) && !quests.IsCompleted(PhaseTwoIds.BindingChoiceQuest))
                    return SetBoss(out spec, DefaultGameContent.ShardRaptorId, PhaseTwoIds.DefeatSable);
                if (quests.IsActive(PhaseTwoIds.SableRematchQuest) && !quests.IsCompleted(PhaseTwoIds.SableRematchQuest))
                    return SetBoss(out spec, DefaultGameContent.ShardRaptorId, PhaseTwoIds.SableBattleOutcome);
            }

            return false;
        }

        static bool SetBoss(out BossBattleSpec spec, string monsterId, string objectiveId)
        {
            spec = new BossBattleSpec(monsterId, objectiveId);
            return true;
        }

        public static bool TryGetDefaultBossOutcome(string objectiveId, out string outcomeKey, out string outcomeValue)
        {
            outcomeKey = string.Empty;
            outcomeValue = string.Empty;
            if (string.IsNullOrWhiteSpace(objectiveId))
                return false;
            for (var i = 0; i < DefaultBossOutcomes.Length; i++)
            {
                if (DefaultBossOutcomes[i].ObjectiveId != objectiveId)
                    continue;
                outcomeKey = DefaultBossOutcomes[i].OutcomeKey;
                outcomeValue = DefaultBossOutcomes[i].OutcomeValue;
                return true;
            }

            return false;
        }

        public static void ReportCombatVictory(QuestManager quests, string currentAreaId, bool captured, bool enemyWasBoss, string activeBossObjectiveId)
        {
            quests?.ReportObjectiveEvent("win_battle", 1);
            if (currentAreaId == DefaultGameContent.RidgeId)
                quests?.ReportObjectiveEvent(ChapterThreeIds.WinRidgeBattles, 1);
            if (currentAreaId == DefaultGameContent.QuarryId)
                quests?.ReportObjectiveEvent(PhaseTwoIds.WinQuarryBattles, 1);
            if (captured && (currentAreaId == DefaultGameContent.DeltaId || currentAreaId == DefaultGameContent.RidgeId || currentAreaId == DefaultGameContent.SpireId))
                quests?.ReportObjectiveEvent(ChapterThreeIds.CatchDeltaMonsters, 1);
            if (enemyWasBoss)
            {
                quests?.ReportObjectiveEvent(activeBossObjectiveId, 1);
                if (TryGetDefaultBossOutcome(activeBossObjectiveId, out var key, out var value) && !StoryState.HasOutcome(key))
                    StoryState.SetOutcome(key, value);
                if (activeBossObjectiveId == PhaseTwoIds.DefeatSable || activeBossObjectiveId == PhaseTwoIds.SableBattleOutcome)
                    StoryFlags.SetFlag(PhaseTwoIds.FlagSableBattleResolution);
                if (activeBossObjectiveId == PhaseTwoIds.SableBattleOutcome && quests != null && quests.IsActive(PhaseTwoIds.SableRematchQuest))
                    quests.CompleteQuest(PhaseTwoIds.SableRematchQuest);
            }
        }

        public static void ReportEscortProgress(QuestManager quests, string currentAreaId, Vector2 playerPosition, float distanceMoved)
        {
            if (quests == null || currentAreaId != DefaultGameContent.StonewakeId || distanceMoved <= 0f) return;
            var jessa = WorldMapLayout.Get(DefaultGameContent.StonewakeId).NpcAnchors;
            for (var i = 0; i < jessa.Length; i++)
            {
                if (jessa[i].NpcId != NPCController.CartographerJessaId) continue;
                if (Vector2.Distance(playerPosition, jessa[i].Position) <= 4.2f)
                    quests.ReportObjectiveEvent(PhaseTwoIds.JessaEscortStonewake, Mathf.CeilToInt(distanceMoved * 10f));
            }
        }
    }
}
