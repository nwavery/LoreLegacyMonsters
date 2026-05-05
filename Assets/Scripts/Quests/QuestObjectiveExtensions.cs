namespace LoreLegacyMonsters.Quests
{
    public static class QuestObjectiveExtensionsQuests
    {
        public static float Progress01(this QuestObjective o) =>
            o == null || o.requiredCount <= 0 ? 0f : UnityEngine.Mathf.Clamp01((float)o.currentCount / o.requiredCount);
    }
}
