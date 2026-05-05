using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Combat
{
    public readonly struct BattleVictorySummary
    {
        public readonly string AreaId;
        public readonly int GoldAwarded;
        public readonly int ExperienceAwarded;
        public readonly bool Captured;
        public readonly bool BossBattle;
        public readonly string BossObjectiveId;

        public BattleVictorySummary(string areaId, int goldAwarded, int experienceAwarded, bool captured, bool bossBattle, string bossObjectiveId)
        {
            AreaId = areaId;
            GoldAwarded = goldAwarded;
            ExperienceAwarded = experienceAwarded;
            Captured = captured;
            BossBattle = bossBattle;
            BossObjectiveId = bossObjectiveId;
        }
    }

    public static class CombatPostBattleReconciler
    {
        public static BattleVictorySummary Reconcile(
            GameManager gameManager,
            MonsterSystem monsters,
            QuestManager quests,
            AchievementSystem achievements,
            bool captured,
            bool enemyWasBoss,
            string bossObjectiveId,
            int goldReward,
            int experienceReward)
        {
            var areaId = gameManager != null && gameManager.World != null ? gameManager.World.CurrentAreaId : string.Empty;
            if (gameManager != null)
            {
                gameManager.PlayerGold += goldReward;
                GameEvents.RaiseGoldChanged(gameManager.PlayerGold);
                monsters?.GrantExperienceToActive(gameManager.Assets, experienceReward);
            }

            ObjectiveRegistry.ReportCombatVictory(quests, areaId, captured, enemyWasBoss, bossObjectiveId);
            achievements?.Unlock(SampleAchievements.FirstSteps);
            return new BattleVictorySummary(areaId, goldReward, experienceReward, captured, enemyWasBoss, bossObjectiveId);
        }
    }
}
