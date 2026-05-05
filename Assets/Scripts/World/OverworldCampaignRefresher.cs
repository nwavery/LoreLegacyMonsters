using System;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.World
{
    public static class OverworldCampaignRefresher
    {
        public static void SyncCampaignState(QuestManager quests, AchievementSystem achievements, Action<string> setStatus)
        {
            CampaignProgression.TryAdvance(quests, achievements, msg => setStatus?.Invoke(msg));
        }

        public static void RefreshChapterTwoNpcState(QuestManager quests, NPCController archivist, NPCController rival)
        {
            if (archivist != null)
                archivist.gameObject.SetActive(CampaignChapterGates.IsChapterTwoUnlocked(quests));

            if (rival != null)
                rival.gameObject.SetActive(CampaignChapterGates.IsRuinsUnlocked(quests));
        }

        public static void RefreshChapterThreeNpcState(
            QuestManager quests,
            NPCController warden,
            NPCController collector,
            NPCController rumorKeeper,
            NPCController mentor,
            NPCController stormBoss)
        {
            var chapterThreeUnlocked = CampaignChapterGates.IsChapterThreeUnlocked(quests);
            if (warden != null)
                warden.gameObject.SetActive(chapterThreeUnlocked);
            if (collector != null)
                collector.gameObject.SetActive(chapterThreeUnlocked);
            if (rumorKeeper != null)
                rumorKeeper.gameObject.SetActive(chapterThreeUnlocked);
            if (mentor != null)
                mentor.gameObject.SetActive(CampaignChapterGates.IsRidgeUnlocked(quests));
            if (stormBoss != null)
                stormBoss.gameObject.SetActive(CampaignChapterGates.IsSpireUnlocked(quests));
        }

        public static void RefreshPhaseTwoNpcState(
            QuestManager quests,
            NPCController cartographer,
            NPCController quartermaster,
            NPCController runner,
            NPCController foreman,
            NPCController ethicist,
            NPCController moonwellKeeper,
            NPCController sable,
            NPCController rival)
        {
            var unlocked = CampaignChapterGates.IsPhaseTwoUnlocked(quests);
            if (cartographer != null) cartographer.gameObject.SetActive(unlocked);
            if (quartermaster != null) quartermaster.gameObject.SetActive(unlocked);
            if (runner != null) runner.gameObject.SetActive(unlocked);
            if (foreman != null) foreman.gameObject.SetActive(unlocked);
            if (ethicist != null) ethicist.gameObject.SetActive(unlocked);
            if (moonwellKeeper != null) moonwellKeeper.gameObject.SetActive(unlocked);
            if (sable != null) sable.gameObject.SetActive(unlocked);
            if (rival == null)
                return;

            var corinOutcome = StoryState.GetOutcome(StoryState.CorinOutcomeKey);
            var shouldAppear = unlocked &&
                               (corinOutcome == StoryState.CorinSideWithCorin || corinOutcome == StoryState.CorinTalkDown);
            rival.gameObject.SetActive(shouldAppear);
            if (!shouldAppear)
                return;

            var crossing = WorldMapLayout.SpawnPoint(DefaultGameContent.CrossingId);
            rival.transform.position = new Vector3(crossing.x - 1.7f, crossing.y + 0.35f, 0f);
        }
    }
}
