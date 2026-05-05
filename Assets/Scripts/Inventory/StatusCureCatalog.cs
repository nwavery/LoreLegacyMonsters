using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Inventory
{
    /// <summary>Maps persistent status effects to the canonical cure item id (for LLM hints and UI).</summary>
    public static class StatusCureCatalog
    {
        public static string RecommendedCureItemId(MonsterStatusEffect status)
        {
            return status switch
            {
                MonsterStatusEffect.Burn => DefaultGameContent.ColdSalveId,
                MonsterStatusEffect.Poison => DefaultGameContent.AntidoteId,
                MonsterStatusEffect.Shock => DefaultGameContent.ShockTonicId,
                _ => string.Empty
            };
        }

        public static string RecommendedCureDisplayName(AssetRegistryManager registry, MonsterStatusEffect status)
        {
            var id = RecommendedCureItemId(status);
            if (string.IsNullOrEmpty(id) || registry == null) return string.Empty;
            var item = registry.GetItem(id);
            return item != null ? item.DisplayName : id;
        }
    }
}
