using LoreLegacyMonsters.Inventory;

namespace LoreLegacyMonsters.Quest
{
    public static class QuestItemRewardExtensions
    {
        public static bool IsQuestBound(this QuestItemData d) =>
            d != null && !string.IsNullOrEmpty(d.LinkedQuestId);
    }
}
