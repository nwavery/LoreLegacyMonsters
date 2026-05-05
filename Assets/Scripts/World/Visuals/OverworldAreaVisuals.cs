using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.UI;
using UnityEngine;

namespace LoreLegacyMonsters.World.Visuals
{
    /// <summary>
    /// Deterministic palette, ground tiles, and parallax per overworld area segment.
    /// </summary>
    public static class OverworldAreaVisuals
    {
        public readonly struct SegmentPreset
        {
            public readonly string AreaId;
            public readonly Color Ground;
            public readonly Color GroundHighlight;
            public readonly string GroundTile;
            public readonly float TileSpacing;
            public readonly Color TileTint;
            public readonly string Parallax;
            public readonly Color ParallaxTint;

            public SegmentPreset(string areaId, Color ground, Color groundHighlight, string groundTile, float tileSpacing,
                Color tileTint, string parallax, Color parallaxTint)
            {
                AreaId = areaId;
                Ground = ground;
                GroundHighlight = groundHighlight;
                GroundTile = groundTile;
                TileSpacing = tileSpacing;
                TileTint = tileTint;
                Parallax = parallax;
                ParallaxTint = parallaxTint;
            }
        }

        /// <summary>Preset for the nine overworld zones in eastward order.</summary>
        public static SegmentPreset[] SegmentsOrdered => new[]
        {
            new SegmentPreset(DefaultGameContent.TownId,
                GameVisualTheme.Grass, GameVisualTheme.Brighten(GameVisualTheme.Grass, 0.08f),
                "ground_grass_tile", 2.1f, Color.white, "parallax_town", GameVisualTheme.Hex(0xFF, 0xFF, 0xFF, 0.75f)),
            new SegmentPreset(DefaultGameContent.RouteId,
                GameVisualTheme.Hex(0x8F, 0xB7, 0x58), GameVisualTheme.Hex(0x9E, 0xC6, 0x62),
                "ground_grass_tile", 2.0f, GameVisualTheme.Hex(0xFF, 0xFF, 0xFF, 0.55f), "parallax_route", Color.white),
            new SegmentPreset(DefaultGameContent.ForestId,
                GameVisualTheme.Forest, GameVisualTheme.Brighten(GameVisualTheme.Forest, 0.1f),
                "ground_grass_tile", 2.2f, GameVisualTheme.Hex(0x88, 0xAA, 0x77, 0.5f), "parallax_forest", Color.white),
            new SegmentPreset(DefaultGameContent.GroveId,
                GameVisualTheme.Hex(0x67, 0x9B, 0x45), GameVisualTheme.Hex(0x7A, 0xB2, 0x55),
                "ground_grass_tile", 1.9f, GameVisualTheme.Hex(0xDD, 0xEE, 0xCC, 0.45f), "parallax_grove", Color.white),
            new SegmentPreset(DefaultGameContent.MarshId,
                GameVisualTheme.Hex(0x59, 0x8E, 0x72), GameVisualTheme.Hex(0x6A, 0x9E, 0x82),
                "ground_marsh", 2.0f, Color.white, "parallax_marsh", GameVisualTheme.Hex(0xDD, 0xEE, 0xFF, 0.35f)),
            new SegmentPreset(DefaultGameContent.RuinsId,
                GameVisualTheme.Hex(0x8B, 0x84, 0x70), GameVisualTheme.Hex(0x9A, 0x94, 0x80),
                "ground_ruins", 1.85f, Color.white, "parallax_ruins", GameVisualTheme.Hex(0xFF, 0xFF, 0xFF, 0.65f)),
            new SegmentPreset(DefaultGameContent.DeltaId,
                GameVisualTheme.Hex(0x58, 0x95, 0x87), GameVisualTheme.Hex(0x6A, 0xA5, 0x95),
                "ground_delta", 2.0f, Color.white, "parallax_delta", GameVisualTheme.Hex(0xE0, 0xFF, 0xFF, 0.4f)),
            new SegmentPreset(DefaultGameContent.RidgeId,
                GameVisualTheme.Hex(0x72, 0x7B, 0x80), GameVisualTheme.Hex(0x84, 0x8E, 0x94),
                "ground_ridge", 2.0f, GameVisualTheme.Hex(0xEE, 0xEE, 0xEE, 0.55f), "parallax_ridge", Color.white),
            new SegmentPreset(DefaultGameContent.SpireId,
                GameVisualTheme.Hex(0x72, 0x69, 0x91), GameVisualTheme.Hex(0x86, 0x7E, 0xA5),
                "ground_spire", 1.8f, GameVisualTheme.Hex(0xDD, 0xDD, 0xFF, 0.55f), "parallax_spire", Color.white),
        };

        public static SegmentPreset PresetForAreaId(string areaId)
        {
            if (string.IsNullOrEmpty(areaId)) return SegmentsOrdered[1];
            for (var i = 0; i < SegmentsOrdered.Length; i++)
                if (SegmentsOrdered[i].AreaId == areaId)
                    return SegmentsOrdered[i];
            return SegmentsOrdered[1];
        }
    }
}
