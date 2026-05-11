using LoreLegacyMonsters;
using LoreLegacyMonsters.Achievements;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using UnityEngine;

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
            int experienceReward,
            MonsterData defeatedEnemy)
        {
            var areaId = gameManager != null && gameManager.World != null ? gameManager.World.CurrentAreaId : string.Empty;
            var mods = gameManager != null ? gameManager.Loadout?.Snapshot : null;
            mods ??= LoadoutModifiers.Empty;
            var goldScaled = Mathf.Max(0, Mathf.RoundToInt(goldReward * Mathf.Max(0f, mods.GoldGainMult)));
            var xpScaled = Mathf.Max(0, Mathf.RoundToInt(experienceReward * Mathf.Max(0f, mods.XpGainMult)));

            if (gameManager != null)
            {
                gameManager.PlayerGold += goldScaled;
                GameEvents.RaiseGoldChanged(gameManager.PlayerGold);
                monsters?.GrantExperienceToActive(gameManager.Assets, xpScaled);
            }

            ObjectiveRegistry.ReportCombatVictory(quests, areaId, captured, enemyWasBoss, bossObjectiveId);
            achievements?.Unlock(SampleAchievements.FirstSteps);

            if (!captured && gameManager != null && defeatedEnemy != null)
                GearLootRoller.TryRollAndAward(gameManager, defeatedEnemy, enemyWasBoss, mods, UnityRandomSource.Default);

            return new BattleVictorySummary(areaId, goldScaled, xpScaled, captured, enemyWasBoss, bossObjectiveId);
        }
    }
}
