using System.Collections.Generic;
using LoreLegacyMonsters.Monster;
using UnityEngine;

namespace LoreLegacyMonsters.UI
{
    /// <summary>
    /// Loads placeholder element silhouettes from Resources and provides tint colors for silhouettes and chips.
    /// </summary>
    public static class MonsterElementSprites
    {
        static readonly Dictionary<MonsterElement, Sprite> Cache = new Dictionary<MonsterElement, Sprite>();

        static readonly Color NeutralTint = GameVisualTheme.Hex(0xD7, 0xC8, 0xA1);
        static readonly Color FireTint = GameVisualTheme.Hex(0xE7, 0x6A, 0x45);
        static readonly Color WaterTint = GameVisualTheme.Hex(0x5A, 0xAD, 0xD6);
        static readonly Color NatureTint = GameVisualTheme.Hex(0x82, 0xBC, 0x5F);
        static readonly Color LightningTint = GameVisualTheme.Hex(0xF2, 0xCA, 0x55);
        static readonly Color StoneTint = GameVisualTheme.Hex(0xA6, 0x98, 0x7A);
        static readonly Color ShadowTint = GameVisualTheme.Hex(0x7E, 0x62, 0xA8);

        public static Color SilhouetteTint(MonsterElement element) => GameVisualTheme.Brighten(ChipColor(element), 0.03f);

        public static Color ChipColor(MonsterElement element)
        {
            return element switch
            {
                MonsterElement.Fire => FireTint,
                MonsterElement.Water => WaterTint,
                MonsterElement.Nature => NatureTint,
                MonsterElement.Lightning => LightningTint,
                MonsterElement.Stone => StoneTint,
                MonsterElement.Shadow => ShadowTint,
                MonsterElement.Neutral => NeutralTint,
                MonsterElement.None => NeutralTint,
                _ => NeutralTint
            };
        }

        public static string ChipLabel(MonsterElement element)
        {
            return element switch
            {
                MonsterElement.Lightning => "Electric",
                MonsterElement.None => string.Empty,
                _ => element.ToString()
            };
        }

        public static Sprite For(MonsterElement element)
        {
            var key = ResolvePrimaryForArt(element);
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            var path = $"Sprites/Elements/Silhouette_{key}";
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
                sprite = Resources.Load<Sprite>("Sprites/Elements/Silhouette_Neutral");
            Cache[key] = sprite;
            return sprite;
        }

        static MonsterElement ResolvePrimaryForArt(MonsterElement element)
        {
            if (element == MonsterElement.None)
                return MonsterElement.Neutral;
            return element;
        }
    }
}
