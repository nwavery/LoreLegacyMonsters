using System;

namespace LoreLegacyMonsters.Core
{
    /// <summary>Lightweight cross-system notifications (README “event-based” baseline).</summary>
    public static class GameEvents
    {
        public static event Action<int> GoldChanged;
        public static event Action<string> AreaChanged;
        public static event Action<string> QuestCompleted;
        public static event Action BattleEnded;
        public static event Action<string> AchievementUnlocked;
        public static event Action<string> MonsterLeveled;
        public static event Action<string> MonsterEvolved;
        public static event Action<string> ToastRequested;
        /// <summary>Raised after <see cref="GameManager.ApplySaveToRuntime"/> finishes applying a save snapshot.</summary>
        public static event Action RuntimeRestored;

        public static void RaiseGoldChanged(int amount) => GoldChanged?.Invoke(amount);
        public static void RaiseAreaChanged(string areaId) => AreaChanged?.Invoke(areaId);
        public static void RaiseQuestCompleted(string questId) => QuestCompleted?.Invoke(questId);
        public static void RaiseBattleEnded() => BattleEnded?.Invoke();
        public static void RaiseAchievementUnlocked(string id) => AchievementUnlocked?.Invoke(id);
        public static void RaiseMonsterLeveled(string instanceId) => MonsterLeveled?.Invoke(instanceId);
        public static void RaiseMonsterEvolved(string instanceId) => MonsterEvolved?.Invoke(instanceId);
        public static void RaiseToast(string message) => ToastRequested?.Invoke(message);
        public static void RaiseRuntimeRestored() => RuntimeRestored?.Invoke();
    }
}
