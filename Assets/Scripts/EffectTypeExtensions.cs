using LoreLegacyMonsters.Inventory;

namespace LoreLegacyMonsters
{
    public static class EffectTypeExtensionsRoot
    {
        public static string Code(this EffectType e) => e.ToString().ToLowerInvariant();
    }
}
