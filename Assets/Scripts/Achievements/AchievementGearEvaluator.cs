using LoreLegacyMonsters.Inventory;

namespace LoreLegacyMonsters.Achievements
{
    /// <summary>Optional gear-derived achievements keyed to <see cref="Platform.Steam.SteamAchievementIds"/>.</summary>
    public static class AchievementGearEvaluator
    {
        public static void EvaluateFromRuntime(InventorySystem inv, LoadoutSystem loadout, AchievementSystem ach,
            AssetRegistryManager reg)
        {
            if (ach == null || reg == null) return;
            if (loadout != null)
            {
                if (!string.IsNullOrEmpty(loadout.OutfitEquippedId) ||
                    !string.IsNullOrEmpty(loadout.GetCharmEquippedId(0)) ||
                    !string.IsNullOrEmpty(loadout.GetCharmEquippedId(1)) ||
                    !string.IsNullOrEmpty(loadout.GetCharmEquippedId(2)))
                    ach.Unlock(SampleAchievements.FirstEquip);

                if (!string.IsNullOrEmpty(loadout.OutfitEquippedId) &&
                    !string.IsNullOrEmpty(loadout.GetCharmEquippedId(0)) &&
                    !string.IsNullOrEmpty(loadout.GetCharmEquippedId(1)) &&
                    !string.IsNullOrEmpty(loadout.GetCharmEquippedId(2)))
                    ach.Unlock(SampleAchievements.FullLoadout);
            }

            if (inv == null) return;
            var stacks = inv.GetStacksSnapshot();
            bool hasCommon = false, hasUnc = false, hasRare = false, hasLegend = false, hasLegendaryItem = false;
            foreach (var s in stacks)
            {
                if (s == null || string.IsNullOrEmpty(s.itemId)) continue;
                if (!(reg.GetItem(s.itemId) is GearItemData g)) continue;
                switch (g.Rarity)
                {
                    case Rarity.Common: hasCommon = true; break;
                    case Rarity.Uncommon: hasUnc = true; break;
                    case Rarity.Rare: hasRare = true; break;
                    case Rarity.Legendary: hasLegend = true; hasLegendaryItem = true; break;
                }
            }

            if (hasLegendaryItem)
                ach.Unlock(SampleAchievements.LegendaryOwned);

            if (hasCommon && hasUnc && hasRare && hasLegend)
                ach.Unlock(SampleAchievements.MasterOutfitter);
        }
    }
}
