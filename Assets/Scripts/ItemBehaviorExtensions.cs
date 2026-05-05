using LoreLegacyMonsters.Inventory;

namespace LoreLegacyMonsters
{
    public static class ItemBehaviorExtensions
    {
        public static bool CanUseInBattle(this ItemData d) =>
            d != null && d.Type == ItemType.Consumable;
    }
}
