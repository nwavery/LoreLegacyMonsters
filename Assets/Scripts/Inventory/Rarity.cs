using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public static class RarityExtensions
    {
        public static string Label(this Rarity r) => r switch
        {
            Rarity.Common => "Common",
            Rarity.Uncommon => "Uncommon",
            Rarity.Rare => "Rare",
            Rarity.Legendary => "Legendary",
            _ => r.ToString()
        };

        public static Color AccentColor(this Rarity r)
        {
            switch (r)
            {
                case Rarity.Common: return new Color(0.75f, 0.75f, 0.72f, 1f);
                case Rarity.Uncommon: return new Color(0.35f, 0.78f, 0.42f, 1f);
                case Rarity.Rare: return new Color(0.38f, 0.55f, 0.95f, 1f);
                case Rarity.Legendary: return new Color(0.94f, 0.62f, 0.28f, 1f);
                default: return Color.white;
            }
        }

        /// <summary>Vendor price multiplier on top of base price.</summary>
        public static float PriceMultiplier(this Rarity r) => r switch
        {
            Rarity.Common => 1f,
            Rarity.Uncommon => 2.25f,
            Rarity.Rare => 6f,
            Rarity.Legendary => 18f,
            _ => 1f
        };
    }
}
