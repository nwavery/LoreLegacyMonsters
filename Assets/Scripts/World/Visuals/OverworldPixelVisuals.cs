using System.Collections.Generic;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.UI;
using UnityEngine;

namespace LoreLegacyMonsters.World.Visuals
{
    public readonly struct OverworldVisualBounds
    {
        public readonly float TownEdgeX;
        public readonly float ForestEdgeX;
        public readonly float GroveEdgeX;
        public readonly float MarshEdgeX;
        public readonly float RuinsEdgeX;
        public readonly float DeltaEdgeX;
        public readonly float RidgeEdgeX;
        public readonly float SpireEdgeX;

        public OverworldVisualBounds(float townEdgeX, float forestEdgeX, float groveEdgeX, float marshEdgeX,
            float ruinsEdgeX, float deltaEdgeX, float ridgeEdgeX, float spireEdgeX)
        {
            TownEdgeX = townEdgeX;
            ForestEdgeX = forestEdgeX;
            GroveEdgeX = groveEdgeX;
            MarshEdgeX = marshEdgeX;
            RuinsEdgeX = ruinsEdgeX;
            DeltaEdgeX = deltaEdgeX;
            RidgeEdgeX = ridgeEdgeX;
            SpireEdgeX = spireEdgeX;
        }
    }

    public static class OverworldPixelVisuals
    {
        const float LeftEdge = -12f;
        const float SkylineCenter = 62f;
        const float GroundY = -1.1f;
        const float RoadY = -1.2f;
        static Sprite pixel;
        static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public static Transform Build(Transform host)
        {
            var backdrop = EnsureRoot(host, "OverworldBackdrop");
            RuntimeUiFactory.DestroyChildren(backdrop);

            BuildMapSky(backdrop);
            BuildMapGround(backdrop);
            BuildMapRegions(backdrop);
            BuildEncounterPatches(backdrop);
            BuildMapRoads(backdrop);
            BuildMapLandmarks(backdrop);
            BuildMapAtmosphere(backdrop);
            return backdrop;
        }

        public static Transform Build(Transform host, OverworldVisualBounds bounds)
        {
            var backdrop = EnsureRoot(host, "OverworldBackdrop");
            RuntimeUiFactory.DestroyChildren(backdrop);

            BuildSky(backdrop, bounds);
            BuildGround(backdrop, bounds);
            BuildAreaIdentity(backdrop, bounds);
            BuildLandmarks(backdrop, bounds);
            BuildAtmosphere(backdrop, bounds);
            return backdrop;
        }

        static void BuildMapSky(Transform backdrop)
        {
            var bounds = WorldMapLayout.WorldBounds();
            SizedRect(backdrop, "SkyTop", new Vector3(bounds.center.x, bounds.center.y + 4f, 0f),
                new Vector2(bounds.width + 20f, bounds.height + 20f), GameVisualTheme.SkyTop, -80);
            SizedRect(backdrop, "SkyGlow", new Vector3(bounds.center.x, bounds.center.y - 4f, 0f),
                new Vector2(bounds.width + 20f, bounds.height + 10f), GameVisualTheme.SkyBottom, -79);
        }

        static void BuildMapGround(Transform backdrop)
        {
            var bounds = WorldMapLayout.WorldBounds();
            SizedRect(backdrop, "WorldBaseShadow", new Vector3(bounds.center.x, bounds.center.y, 0f), new Vector2(bounds.width + 10f, bounds.height + 8f),
                GameVisualTheme.Hex(0x34, 0x4A, 0x38), -72);
        }

        static void BuildMapRegions(Transform backdrop)
        {
            foreach (var region in WorldMapLayout.All)
            {
                var color = RegionColor(region.Biome);
                SizedRect(backdrop, $"Region_{region.AreaId}", new Vector3(region.Bounds.center.x, region.Bounds.center.y, 0f),
                    new Vector2(region.Bounds.width, region.Bounds.height), color, -60);
                SizedRect(backdrop, $"RegionHi_{region.AreaId}", new Vector3(region.Bounds.center.x, region.Bounds.yMax - 0.25f, 0f),
                    new Vector2(region.Bounds.width, 0.18f), GameVisualTheme.Brighten(color, 0.1f), -59);
                SizedRect(backdrop, $"RegionEdgeTop_{region.AreaId}", new Vector3(region.Bounds.center.x, region.Bounds.yMax - 0.06f, 0f),
                    new Vector2(region.Bounds.width, 0.08f), GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.24f), -58);
                SizedRect(backdrop, $"RegionEdgeBottom_{region.AreaId}", new Vector3(region.Bounds.center.x, region.Bounds.yMin + 0.06f, 0f),
                    new Vector2(region.Bounds.width, 0.08f), GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.2f), -58);
            }
        }

        static void BuildEncounterPatches(Transform backdrop)
        {
            foreach (var region in WorldMapLayout.All)
            {
                var zones = WorldMapLayout.EncounterZones(region.AreaId);
                for (var i = 0; i < zones.Length; i++)
                {
                    var zone = zones[i];
                    var center = zone.Bounds.center;
                    SizedRect(backdrop, $"EncounterZone_{region.AreaId}_{i}", new Vector3(center.x, center.y, 0f),
                        new Vector2(zone.Bounds.width, zone.Bounds.height), EncounterPatchColor(zone.Type), -52);
                    SizedRect(backdrop, $"EncounterZoneHi_{region.AreaId}_{i}", new Vector3(center.x, zone.Bounds.yMax - 0.06f, 0f),
                        new Vector2(zone.Bounds.width, 0.08f), EncounterHighlightColor(zone.Type), -51);
                    AddEncounterCluster(backdrop, $"EncounterPatch_{region.AreaId}_{i}", zone, -50);
                }
            }
        }

        static void BuildMapRoads(Transform backdrop)
        {
            var road = GameVisualTheme.Hex(0xBD, 0x88, 0x55);
            for (var i = 0; i < WorldMapLayout.MapEdges.Count; i++)
            {
                var edge = WorldMapLayout.MapEdges[i];
                AddRoad(backdrop, $"Road_{edge.FromAreaId}_{edge.ToAreaId}",
                    WorldMapLayout.SpawnPoint(edge.FromAreaId), WorldMapLayout.SpawnPoint(edge.ToAreaId), road);
            }
        }

        static void AddRoad(Transform backdrop, string name, Vector2 a, Vector2 b, Color color)
        {
            var delta = b - a;
            var mid = (a + b) * 0.5f;
            var angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            var length = delta.magnitude;
            var shadow = SizedRect(backdrop, $"{name}_Shadow", new Vector3(mid.x, mid.y - 0.06f, 0f),
                new Vector2(length, 0.62f), GameVisualTheme.WithAlpha(GameVisualTheme.Ink, 0.18f), -44);
            shadow.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var edge = SizedRect(backdrop, $"{name}_Edge", new Vector3(mid.x, mid.y, 0f),
                new Vector2(length, 0.5f), GameVisualTheme.RoadDark, -43);
            edge.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var go = SizedRect(backdrop, name, new Vector3(mid.x, mid.y + 0.01f, 0f), new Vector2(length, 0.32f), color, -42);
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var centerLine = SizedRect(backdrop, $"{name}_CenterLine", new Vector3(mid.x, mid.y + 0.03f, 0f),
                new Vector2(length * 0.94f, 0.08f), GameVisualTheme.WithAlpha(GameVisualTheme.Parchment, 0.75f), -41);
            centerLine.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            var count = Mathf.Clamp(Mathf.RoundToInt(length / 4.5f), 1, 12);
            var normal = new Vector2(-delta.y, delta.x).normalized;
            for (var i = 1; i <= count; i++)
            {
                var t = i / (count + 1f);
                var p = Vector2.Lerp(a, b, t) + normal * ((i % 2 == 0) ? 0.22f : -0.22f);
                AddPebble(backdrop, $"{name}_Pebble_{i}", new Vector3(p.x, p.y, 0f), GameVisualTheme.WithAlpha(GameVisualTheme.ParchmentDark, 0.9f), -40);
            }
        }

        static GameObject SizedRect(Transform parent, string name, Vector3 localPosition, Vector2 worldSize,
            Color color, int sortingOrder)
        {
            var spriteSize = PixelSprite().bounds.size;
            var sx = spriteSize.x > 0.001f ? worldSize.x / spriteSize.x : worldSize.x;
            var sy = spriteSize.y > 0.001f ? worldSize.y / spriteSize.y : worldSize.y;
            return Rect(parent, name, localPosition, new Vector3(sx, sy, 1f), color, sortingOrder);
        }

        static void BuildMapLandmarks(Transform backdrop)
        {
            foreach (var region in WorldMapLayout.All)
            {
                var center = region.Bounds.center;
                switch (region.Biome)
                {
                    case "town":
                    case "hamlet":
                        AddHouse(backdrop, $"House_{region.AreaId}_A", new Vector3(center.x - 1.8f, center.y + 0.4f, 0f),
                            GameVisualTheme.Parchment, GameVisualTheme.Accent, -20, true);
                        AddHouse(backdrop, $"House_{region.AreaId}_B", new Vector3(center.x + 1.9f, center.y - 0.6f, 0f),
                            GameVisualTheme.ParchmentDark, GameVisualTheme.RoadDark, -20, false);
                        if (region.Biome == "hamlet")
                        {
                            AddHouse(backdrop, $"House_{region.AreaId}_Inn", new Vector3(region.SpawnPoint.x + 3.1f, region.SpawnPoint.y + 0.2f, 0f),
                                GameVisualTheme.Parchment, GameVisualTheme.RoadDark, -16, false);
                            AddSign(backdrop, $"Sign_{region.AreaId}", new Vector3(region.SpawnPoint.x - 1.6f, region.SpawnPoint.y - 0.55f, 0f), "!", -12);
                            AddWell(backdrop, $"Well_{region.AreaId}", new Vector3(region.SpawnPoint.x - 2.7f, region.SpawnPoint.y + 0.55f, 0f), -13);
                            AddNoticeBoard(backdrop, $"Notice_{region.AreaId}", new Vector3(region.SpawnPoint.x + 0.1f, region.SpawnPoint.y - 0.75f, 0f), -12);
                        }
                        break;
                    case "moonwell":
                        SizedRect(backdrop, $"MoonwellPool_{region.AreaId}", new Vector3(region.SpawnPoint.x + 3f, region.SpawnPoint.y + 0.45f, 0f),
                            new Vector2(3.6f, 2.0f), GameVisualTheme.WithAlpha(GameVisualTheme.AccentBlue, 0.86f), -20);
                        SizedRect(backdrop, $"MoonwellGlow_{region.AreaId}", new Vector3(region.SpawnPoint.x + 3f, region.SpawnPoint.y + 0.45f, 0f),
                            new Vector2(2.2f, 1.1f), GameVisualTheme.WithAlpha(GameVisualTheme.Parchment, 0.62f), -19);
                        SizedRect(backdrop, $"MoonwellStep_{region.AreaId}", new Vector3(region.SpawnPoint.x + 1.1f, region.SpawnPoint.y - 0.15f, 0f),
                            new Vector2(1.5f, 0.38f), GameVisualTheme.Stone, -18);
                        AddRingStone(backdrop, $"MoonwellRingA_{region.AreaId}", new Vector3(region.SpawnPoint.x + 1.25f, region.SpawnPoint.y + 1.4f, 0f), -15);
                        AddRingStone(backdrop, $"MoonwellRingB_{region.AreaId}", new Vector3(region.SpawnPoint.x + 4.75f, region.SpawnPoint.y + 1.35f, 0f), -15);
                        AddFirefly(backdrop, $"MoonwellFireflyA_{region.AreaId}", new Vector3(region.SpawnPoint.x + 2.0f, region.SpawnPoint.y + 1.3f, 0f), -10);
                        AddFirefly(backdrop, $"MoonwellFireflyB_{region.AreaId}", new Vector3(region.SpawnPoint.x + 4.1f, region.SpawnPoint.y + 0.9f, 0f), -10);
                        AddFlower(backdrop, $"MoonwellFlowerA_{region.AreaId}", new Vector3(region.SpawnPoint.x - 2.4f, region.SpawnPoint.y - 1.1f, 0f), -14);
                        AddFlower(backdrop, $"MoonwellFlowerB_{region.AreaId}", new Vector3(region.SpawnPoint.x + 3.1f, region.SpawnPoint.y - 0.9f, 0f), -14);
                        AddTreeCluster(backdrop, $"Trees_{region.AreaId}", region.Bounds.xMin + 2f, region.Bounds.xMax - 2f, 4,
                            GameVisualTheme.Hex(0x62, 0x86, 0xA8), -18);
                        break;
                    case "quarry":
                        AddCliffs(backdrop, $"Cliffs_{region.AreaId}", new Vector3(center.x, center.y + 0.5f, 0f), -18);
                        AddSprite(backdrop, $"Rock_{region.AreaId}", "prop_rock", new Vector3(center.x - 2.4f, center.y - 1.8f, 0f), 1.1f, -16);
                        AddSprite(backdrop, $"RockNear_{region.AreaId}", "prop_rock", new Vector3(region.SpawnPoint.x - 1.8f, region.SpawnPoint.y - 1.4f, 0f), 1.0f, -14);
                        AddSprite(backdrop, $"ShardNear_{region.AreaId}", "prop_shard", new Vector3(region.SpawnPoint.x + 2.4f, region.SpawnPoint.y - 1.2f, 0f), 0.8f, -14);
                        AddMineCart(backdrop, $"MineCart_{region.AreaId}", new Vector3(region.SpawnPoint.x + 3.4f, region.SpawnPoint.y + 0.05f, 0f), -13);
                        AddCrane(backdrop, $"Crane_{region.AreaId}", new Vector3(center.x + 2.4f, center.y + 1.15f, 0f), -15);
                        break;
                    case "crossing":
                        AddBridgePosts(backdrop, $"Bridge_{region.AreaId}", region.SpawnPoint, -13);
                        AddReedBand(backdrop, $"CrossingReeds_{region.AreaId}", region.Bounds.xMin + 1.4f, region.Bounds.xMax - 1.2f, -14);
                        break;
                    case "starfall":
                        AddRuins(backdrop, $"Ruins_{region.AreaId}", new Vector3(center.x, center.y + 0.2f, 0f), -18);
                        AddSprite(backdrop, $"Shard_{region.AreaId}", "prop_shard", new Vector3(center.x + 2.4f, center.y - 1.4f, 0f), 1.1f, -16);
                        AddSprite(backdrop, $"ShardBig_{region.AreaId}", "prop_shard", new Vector3(center.x - 2.8f, center.y + 0.8f, 0f), 1.35f, -16);
                        SizedRect(backdrop, $"StarGlow_{region.AreaId}", new Vector3(center.x + 1.2f, center.y - 1.0f, 0f),
                            new Vector2(2.6f, 0.7f), GameVisualTheme.WithAlpha(GameVisualTheme.AccentBlue, 0.42f), -17);
                        break;
                    case "spire":
                        AddSpire(backdrop, $"Spire_{region.AreaId}", new Vector3(center.x, center.y + 1.1f, 0f), -18);
                        break;
                    default:
                        AddTreeCluster(backdrop, $"Trees_{region.AreaId}", region.Bounds.xMin + 2f, region.Bounds.xMax - 2f, 3,
                            RegionColor(region.Biome), -18);
                        break;
                }

                AddNpcRoleProps(backdrop, region);
            }
        }

        static void AddEncounterCluster(Transform parent, string name, EncounterZone zone, int order)
        {
            var root = EnsureRoot(parent, name);
            RuntimeUiFactory.DestroyChildren(root);
            root.localPosition = Vector3.zero;
            var count = Mathf.Clamp(Mathf.RoundToInt(zone.Bounds.width + zone.Bounds.height), 5, 16);
            for (var i = 0; i < count; i++)
            {
                var x = zone.Bounds.xMin + 0.45f + (zone.Bounds.width - 0.9f) * ((i * 37) % 100) / 100f;
                var y = zone.Bounds.yMin + 0.35f + (zone.Bounds.height - 0.7f) * ((i * 53) % 100) / 100f;
                var pos = new Vector3(x, y, 0f);
                switch (zone.Type)
                {
                    case EncounterZoneType.Reeds:
                        AddReed(root, $"Reed_{i}", pos, order);
                        break;
                    case EncounterZoneType.Rubble:
                    case EncounterZoneType.QuarryPit:
                        AddPebble(root, $"Rubble_{i}", pos, i % 2 == 0 ? GameVisualTheme.Stone : GameVisualTheme.RoadDark, order);
                        if (i % 4 == 0) AddSprite(root, $"Rock_{i}", "prop_rock", pos + new Vector3(0.15f, 0.05f, 0f), 0.45f, order + 1);
                        break;
                    case EncounterZoneType.RidgeNest:
                        AddPebble(root, $"NestStone_{i}", pos, GameVisualTheme.Stone, order);
                        if (i % 3 == 0) AddGrassTuft(root, $"NestTuft_{i}", pos + new Vector3(0.1f, 0.02f, 0f), GameVisualTheme.Brighten(GameVisualTheme.Stone, 0.08f), order + 1);
                        break;
                    case EncounterZoneType.MoonwellGlade:
                        AddFlower(root, $"MoonFlower_{i}", pos, order);
                        if (i % 4 == 0) AddFirefly(root, $"GladeGlow_{i}", pos + new Vector3(0.2f, 0.18f, 0f), order + 2);
                        break;
                    case EncounterZoneType.Crystal:
                        AddSprite(root, $"Crystal_{i}", "prop_shard", pos, 0.45f + (i % 3) * 0.1f, order);
                        break;
                    default:
                        AddGrassTuft(root, $"Grass_{i}", pos, GameVisualTheme.GrassLight, order);
                        break;
                }
            }
        }

        static void AddNpcRoleProps(Transform parent, WorldRegionLayout region)
        {
            foreach (var anchor in region.NpcAnchors)
            {
                var p = new Vector3(anchor.Position.x, anchor.Position.y, 0f);
                switch (anchor.NpcId)
                {
                    case NPCController.CartographerJessaId:
                        AddMapTable(parent, $"MapTable_{anchor.NpcId}", p + new Vector3(-0.8f, -0.35f, 0f), -8);
                        break;
                    case NPCController.QuartermasterBramId:
                        AddCrates(parent, $"Crates_{anchor.NpcId}", p + new Vector3(0.85f, -0.45f, 0f), -8);
                        break;
                    case NPCController.RunnerNiaId:
                        AddSign(parent, $"RouteMarker_{anchor.NpcId}", p + new Vector3(-0.75f, -0.55f, 0f), ">", -8);
                        break;
                    case NPCController.ForemanOrloId:
                        AddTools(parent, $"Tools_{anchor.NpcId}", p + new Vector3(0.8f, -0.55f, 0f), -8);
                        break;
                    case NPCController.MoonwellLumaId:
                        AddFirefly(parent, $"KeeperGlow_{anchor.NpcId}", p + new Vector3(-0.55f, 0.5f, 0f), -8);
                        break;
                }
            }
        }

        static void BuildMapAtmosphere(Transform backdrop)
        {
            foreach (var region in WorldMapLayout.All)
            {
                for (var i = 0; i < 3; i++)
                {
                    var x = region.Bounds.xMin + 2.5f + i * Mathf.Max(2f, region.Bounds.width / 4f);
                    var y = region.Bounds.yMin + 1.2f + (i % 2) * 0.8f;
                    AddGrassTuft(backdrop, $"Tuft_{region.AreaId}_{i}", new Vector3(x, y, 0f),
                        GameVisualTheme.Brighten(RegionColor(region.Biome), 0.08f), -14);
                }
            }
        }

        static Color RegionColor(string biome)
        {
            return biome switch
            {
                "town" => GameVisualTheme.Grass,
                "route" => GameVisualTheme.Hex(0x8F, 0xB7, 0x58),
                "forest" => GameVisualTheme.Forest,
                "forest_north" => GameVisualTheme.Hex(0x3F, 0x79, 0x4E),
                "grove" => GameVisualTheme.Hex(0x67, 0x9B, 0x45),
                "marsh" => GameVisualTheme.Hex(0x59, 0x8E, 0x72),
                "marsh_basin" => GameVisualTheme.Hex(0x4E, 0x86, 0x7A),
                "ruins" => GameVisualTheme.Hex(0x8B, 0x84, 0x70),
                "delta" => GameVisualTheme.Hex(0x58, 0x95, 0x87),
                "ridge" => GameVisualTheme.Hex(0x72, 0x7B, 0x80),
                "spire" => GameVisualTheme.Hex(0x72, 0x69, 0x91),
                "hamlet" => GameVisualTheme.Hex(0x8E, 0xA8, 0x68),
                "moonwell" => GameVisualTheme.Hex(0x62, 0x86, 0xA8),
                "quarry" => GameVisualTheme.Hex(0x76, 0x70, 0x66),
                "crossing" => GameVisualTheme.Hex(0x64, 0x97, 0x9B),
                "starfall" => GameVisualTheme.Hex(0x70, 0x67, 0x92),
                _ => GameVisualTheme.Grass
            };
        }

        static Color EncounterPatchColor(EncounterZoneType type)
        {
            return type switch
            {
                EncounterZoneType.Reeds => GameVisualTheme.WithAlpha(GameVisualTheme.Hex(0x86, 0xB8, 0x8E), 0.92f),
                EncounterZoneType.Rubble => GameVisualTheme.WithAlpha(GameVisualTheme.Hex(0xA4, 0x9D, 0x72), 0.9f),
                EncounterZoneType.RidgeNest => GameVisualTheme.WithAlpha(GameVisualTheme.Hex(0x98, 0x9E, 0x8C), 0.9f),
                EncounterZoneType.MoonwellGlade => GameVisualTheme.WithAlpha(GameVisualTheme.Hex(0x78, 0x9E, 0xC8), 0.88f),
                EncounterZoneType.QuarryPit => GameVisualTheme.WithAlpha(GameVisualTheme.Hex(0x9A, 0x91, 0x7A), 0.92f),
                EncounterZoneType.Crystal => GameVisualTheme.WithAlpha(GameVisualTheme.Hex(0x8E, 0x82, 0xAA), 0.9f),
                _ => GameVisualTheme.WithAlpha(GameVisualTheme.GrassLight, 0.92f)
            };
        }

        static Color EncounterHighlightColor(EncounterZoneType type)
        {
            return type switch
            {
                EncounterZoneType.Reeds => GameVisualTheme.AccentBlue,
                EncounterZoneType.Rubble or EncounterZoneType.QuarryPit => GameVisualTheme.ParchmentDark,
                EncounterZoneType.Crystal or EncounterZoneType.MoonwellGlade => GameVisualTheme.Parchment,
                _ => GameVisualTheme.GrassLight
            };
        }

        public static Sprite PixelSprite()
        {
            if (pixel != null) return pixel;
            var texture = Texture2D.whiteTexture;
            pixel = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
            return pixel;
        }

        public static Transform EnsureRoot(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        public static GameObject Rect(Transform parent, string name, Vector3 localPosition, Vector3 localScale,
            Color color, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelSprite();
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            return go;
        }

        public static GameObject AddSprite(Transform parent, string name, string spriteName, Vector3 localPosition,
            float worldHeight, int sortingOrder, Color? tint = null)
        {
            var sprite = LoadWorldSprite(spriteName);
            if (sprite == null)
                return Rect(parent, name, localPosition, Vector3.one * worldHeight, tint ?? Color.magenta, sortingOrder);

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = tint ?? Color.white;
            sr.sortingOrder = sortingOrder;
            var height = Mathf.Max(0.01f, sprite.bounds.size.y);
            go.transform.localScale = Vector3.one * (worldHeight / height);
            return go;
        }

        static GameObject AddParallaxStrip(Transform parent, string name, string spriteName, float centerX, float targetWidth,
            float y, Color tint, int sortingOrder)
        {
            var go = AddSprite(parent, name, spriteName, new Vector3(centerX, y, 0f), 2.35f, sortingOrder, tint);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                var bw = sr.bounds.size.x;
                if (bw > 0.001f)
                {
                    var s = go.transform.localScale;
                    go.transform.localScale = new Vector3(s.x * (targetWidth / bw), s.y, 1f);
                }
            }

            return go;
        }

        static void PaintSegment(Transform backdrop, float startX, float endX, OverworldAreaVisuals.SegmentPreset p)
        {
            var width = Mathf.Max(1f, endX - startX);
            Rect(backdrop, $"Seg_{p.AreaId}_Land", new Vector3(startX + width * 0.5f, GroundY, 0f), new Vector3(width, 2.45f, 1f),
                p.Ground, -19);
            Rect(backdrop, $"Seg_{p.AreaId}_Hi", new Vector3(startX + width * 0.5f, GroundY + 1.08f, 0f), new Vector3(width, 0.16f, 1f),
                p.GroundHighlight, -18);
            if (!string.IsNullOrEmpty(p.GroundTile))
            {
                for (var x = startX + 0.8f; x < endX - 0.5f; x += p.TileSpacing)
                    AddSprite(backdrop, $"Tile_{p.AreaId}_{x:0.0}", p.GroundTile, new Vector3(x, GroundY - 0.05f, 0f), 1.28f, -17,
                        p.TileTint);
            }

            if (!string.IsNullOrEmpty(p.Parallax))
                AddParallaxStrip(backdrop, $"Para_{p.AreaId}", p.Parallax, startX + width * 0.5f, width * 0.98f, 0.48f, p.ParallaxTint, -56);
        }

        static Sprite LoadWorldSprite(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName)) return null;
            if (SpriteCache.TryGetValue(spriteName, out var cached)) return cached;
            var sprite = Resources.Load<Sprite>($"Sprites/World/{spriteName}");
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>($"Sprites/World/{spriteName}");
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Point;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            SpriteCache[spriteName] = sprite;
            if (sprite == null)
                Debug.LogWarning($"OverworldPixelVisuals: missing sprite Resources/Sprites/World/{spriteName}");
            return sprite;
        }

        static void BuildSky(Transform backdrop, OverworldVisualBounds bounds)
        {
            var width = bounds.SpireEdgeX + 40f;
            Rect(backdrop, "SkyTop", new Vector3(SkylineCenter, 4.1f, 0f), new Vector3(width, 5.2f, 1f),
                GameVisualTheme.SkyTop, -70);
            Rect(backdrop, "SkyGlow", new Vector3(SkylineCenter, 1.0f, 0f), new Vector3(width, 2.8f, 1f),
                GameVisualTheme.SkyBottom, -69);
            Rect(backdrop, "FarHillsA", new Vector3(SkylineCenter - 12f, 0.35f, 0f), new Vector3(width, 2.1f, 1f),
                GameVisualTheme.Hex(0x78, 0xA5, 0x68), -52);
            Rect(backdrop, "FarHillsB", new Vector3(SkylineCenter + 18f, -0.12f, 0f), new Vector3(width, 1.8f, 1f),
                GameVisualTheme.Hex(0x5F, 0x8D, 0x66), -51);

            for (var i = 0; i < 3; i++)
                AddCloud(backdrop, $"Cloud_{i}", new Vector3(4f + i * 42f, 2.8f + (i % 2) * 0.35f, 0f), -60);
        }

        static void BuildGround(Transform backdrop, OverworldVisualBounds bounds)
        {
            var width = bounds.SpireEdgeX + 42f;
            Rect(backdrop, "GroundShadow", new Vector3(SkylineCenter, -2.25f, 0f), new Vector3(width, 3.9f, 1f),
                GameVisualTheme.Hex(0x36, 0x48, 0x2E), -30);
            for (var x = LeftEdge; x < bounds.SpireEdgeX + 26f; x += 2f)
                AddSprite(backdrop, $"RoadTile_{x:0}", "road_tile", new Vector3(x, RoadY, 0f), 1.55f, -18);
        }

        static void BuildAreaIdentity(Transform backdrop, OverworldVisualBounds b)
        {
            var seg = OverworldAreaVisuals.SegmentsOrdered;
            PaintSegment(backdrop, LeftEdge, b.TownEdgeX, seg[0]);
            PaintSegment(backdrop, b.TownEdgeX, b.ForestEdgeX, seg[1]);
            PaintSegment(backdrop, b.ForestEdgeX, b.GroveEdgeX, seg[2]);
            PaintSegment(backdrop, b.GroveEdgeX, b.MarshEdgeX, seg[3]);
            PaintSegment(backdrop, b.MarshEdgeX, b.RuinsEdgeX, seg[4]);
            PaintSegment(backdrop, b.RuinsEdgeX, b.DeltaEdgeX, seg[5]);
            PaintSegment(backdrop, b.DeltaEdgeX, b.RidgeEdgeX, seg[6]);
            PaintSegment(backdrop, b.RidgeEdgeX, b.SpireEdgeX, seg[7]);
            PaintSegment(backdrop, b.SpireEdgeX, b.SpireEdgeX + 20f, seg[8]);

            for (var x = b.MarshEdgeX + 1f; x < b.RuinsEdgeX - 1f; x += 2f)
                AddSprite(backdrop, $"MarshWater_{x:0}", "water_tile", new Vector3(x, -1.98f, 0f), 0.9f, -15);
            for (var x = b.DeltaEdgeX + 1f; x < b.RidgeEdgeX - 1f; x += 2f)
                AddSprite(backdrop, $"DeltaWater_{x:0}", "water_tile", new Vector3(x, -1.98f, 0f), 1.0f, -15);
        }

        static void BuildLandmarks(Transform backdrop, OverworldVisualBounds b)
        {
            AddHouse(backdrop, "TownShop", new Vector3(0.2f, -0.78f, 0f), GameVisualTheme.Hex(0xE6, 0xB4, 0x72),
                GameVisualTheme.Hex(0x9F, 0x4E, 0x38), -10, true);
            AddHouse(backdrop, "TownHall", new Vector3(4.3f, -0.7f, 0f), GameVisualTheme.Hex(0xD6, 0xC0, 0x86),
                GameVisualTheme.Hex(0x7B, 0x54, 0x40), -10, false);
            AddFence(backdrop, "TownFence", -5f, 9f, -0.52f, -8);
            AddSign(backdrop, "TownSign", new Vector3(8f, -0.52f, 0f), "!", -5);
            AddSprite(backdrop, "TownLampA", "prop_lamp", new Vector3(-2.5f, -0.35f, 0f), 1.15f, -7);
            AddSprite(backdrop, "TownLampB", "prop_lamp", new Vector3(6.5f, -0.35f, 0f), 1.15f, -7);

            AddTreeCluster(backdrop, "RouteTrees", 16f, 26f, 3, GameVisualTheme.Grass, -9);
            AddSign(backdrop, "RouteSign", new Vector3(14.2f, -0.58f, 0f), ">", -5);
            for (var i = 0; i < 5; i++)
                AddPebble(backdrop, $"RoutePebble_{i}", new Vector3(12.5f + i * 2.4f, -0.72f + (i % 2) * 0.1f, 0f),
                    GameVisualTheme.RoadDark, -6);
            AddTreeCluster(backdrop, "ForestTrees", b.ForestEdgeX + 3f, b.GroveEdgeX - 2f, 5, GameVisualTheme.Forest, -9);
            AddSign(backdrop, "ForestWarningSign", new Vector3(b.ForestEdgeX + 1.4f, -0.58f, 0f), "!", -5);
            for (var i = 0; i < 6; i++)
                AddGrassTuft(backdrop, $"ForestPathTuft_{i}", new Vector3(b.ForestEdgeX + 2.4f + i * 1.5f, -0.68f + (i % 3) * 0.08f, 0f),
                    GameVisualTheme.Brighten(GameVisualTheme.Forest, 0.12f), -6);
            AddTreeCluster(backdrop, "GroveTrees", b.GroveEdgeX + 2f, b.MarshEdgeX - 3f, 4, GameVisualTheme.Hex(0x71, 0xB6, 0x4E), -9);

            for (var i = 0; i < 7; i++)
                AddFlower(backdrop, $"GroveFlower_{i}", new Vector3(b.GroveEdgeX + 2.2f + i * 1.35f, -0.86f + (i % 2) * 0.12f, 0f), -7);
            for (var i = 0; i < 5; i++)
                AddSprite(backdrop, $"GroveWild_{i}", "flower_wild", new Vector3(b.GroveEdgeX + 3f + i * 2.1f, -0.88f, 0f), 0.75f, -7);

            AddReedBand(backdrop, "MarshReeds", b.MarshEdgeX + 0.5f, b.RuinsEdgeX - 1f, -7);
            AddRuins(backdrop, "ArchiveRuins", new Vector3(b.RuinsEdgeX + 4f, -0.26f, 0f), -8);
            AddSprite(backdrop, "RuinShardA", "prop_shard", new Vector3(b.RuinsEdgeX + 2.2f, -0.55f, 0f), 0.85f, -6);
            AddSprite(backdrop, "RuinShardB", "prop_shard", new Vector3(b.RuinsEdgeX + 5.8f, -0.65f, 0f), 0.75f, -6);
            AddReedBand(backdrop, "DeltaReeds", b.DeltaEdgeX + 1f, b.RidgeEdgeX - 3f, -7);
            AddCliffs(backdrop, "RidgeCliffs", new Vector3(b.RidgeEdgeX + 3.2f, -0.18f, 0f), -8);
            AddSprite(backdrop, "RidgeRockA", "prop_rock", new Vector3(b.RidgeEdgeX - 1.5f, -0.82f, 0f), 1.0f, -6);
            AddSprite(backdrop, "RidgeRockB", "prop_rock", new Vector3(b.RidgeEdgeX + 6f, -0.78f, 0f), 0.9f, -6);
            AddSpire(backdrop, "SkyglassSpire", new Vector3(b.SpireEdgeX + 2.5f, 0.0f, 0f), -7);
            for (var i = 0; i < 5; i++)
                AddSprite(backdrop, $"SpireShard_{i}", "prop_shard", new Vector3(b.SpireEdgeX + 0.5f + i * 1.1f, -0.5f + (i % 2) * 0.1f, 0f),
                    0.65f, -5);
        }

        static void BuildAtmosphere(Transform backdrop, OverworldVisualBounds b)
        {
            for (var i = 0; i < 12; i++)
            {
                var x = LeftEdge + 3f + i * 10f;
                var y = -0.12f + ((i * 37) % 3) * 0.12f;
                var color = i % 5 == 0 ? GameVisualTheme.GrassLight : GameVisualTheme.Brighten(GameVisualTheme.Grass, 0.02f);
                AddGrassTuft(backdrop, $"Tuft_{i}", new Vector3(x, y, 0f), color, -6);
            }
        }

        static Transform Part(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, int order)
        {
            return Rect(parent, name, pos, scale, color, order).transform;
        }

        static void AddCloud(Transform parent, string name, Vector3 pos, int order)
        {
            AddSprite(parent, name, "cloud", pos, 1.1f, order);
        }

        static void AddPebble(Transform parent, string name, Vector3 pos, Color color, int order) =>
            Part(parent, name, pos, new Vector3(0.12f, 0.06f, 1f), color, order);

        static void AddGrassTuft(Transform parent, string name, Vector3 pos, Color color, int order)
        {
            AddSprite(parent, name, "grass_tuft", pos, 0.9f, order, color);
        }

        static void AddFlower(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Stem", new Vector3(0f, -0.04f, 0f), new Vector3(0.04f, 0.22f, 1f), GameVisualTheme.Forest, order);
            Part(root, "Bloom", new Vector3(0f, 0.1f, 0f), new Vector3(0.14f, 0.14f, 1f), GameVisualTheme.Accent, order + 1);
        }

        static void AddHouse(Transform parent, string name, Vector3 pos, Color bodyColor, Color roofColor, int order, bool shop)
        {
            AddSprite(parent, name, shop ? "house_shop" : "house_town", pos, 2.55f, order);
        }

        static void AddFence(Transform parent, string name, float startX, float endX, float y, int order)
        {
            var root = EnsureRoot(parent, name);
            var width = endX - startX;
            root.localPosition = new Vector3(startX + width * 0.5f, y, 0f);
            root.localScale = new Vector3(1f, 1.55f, 1f);
            Part(root, "RailA", new Vector3(0f, 0.12f, 0f), new Vector3(width, 0.08f, 1f), GameVisualTheme.ParchmentDark, order);
            Part(root, "RailB", new Vector3(0f, -0.08f, 0f), new Vector3(width, 0.08f, 1f), GameVisualTheme.ParchmentDark, order);
            for (var i = 0; i <= Mathf.RoundToInt(width / 1.2f); i++)
                Part(root, $"Post_{i}", new Vector3(-width * 0.5f + i * 1.2f, 0f, 0f), new Vector3(0.1f, 0.48f, 1f), GameVisualTheme.RoadDark, order + 1);
        }

        static void AddSign(Transform parent, string name, Vector3 pos, string mark, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            root.localScale = Vector3.one * 1.85f;
            Part(root, "Post", new Vector3(0f, -0.22f, 0f), new Vector3(0.1f, 0.64f, 1f), GameVisualTheme.RoadDark, order);
            Part(root, "Face", new Vector3(0f, 0.18f, 0f), new Vector3(0.7f, 0.36f, 1f), GameVisualTheme.Parchment, order + 1);
            Part(root, $"Mark_{mark}", new Vector3(0f, 0.18f, 0f), new Vector3(0.08f, 0.22f, 1f), GameVisualTheme.Ink, order + 2);
        }

        static void AddTreeCluster(Transform parent, string name, float startX, float endX, int count, Color canopy, int order)
        {
            var range = Mathf.Max(1f, endX - startX);
            for (var i = 0; i < count; i++)
                AddTree(parent, $"{name}_{i}", new Vector3(startX + (i + 0.5f) * range / count, -0.12f + (i % 3) * 0.12f, 0f),
                    GameVisualTheme.Brighten(canopy, (i % 2) * 0.06f), order);
        }

        static void AddTree(Transform parent, string name, Vector3 pos, Color canopy, int order)
        {
            var sprite = canopy.g < 0.45f ? "tree_forest" : canopy.r > 0.4f ? "tree_grove" : "tree_route";
            AddSprite(parent, name, sprite, pos, 2.9f, order);
        }

        static void AddReedBand(Transform parent, string name, float startX, float endX, int order)
        {
            var root = EnsureRoot(parent, name);
            var count = Mathf.RoundToInt((endX - startX) / 2.8f);
            for (var i = 0; i < count; i++)
                AddReed(root, $"Reed_{i}", new Vector3(startX + i * 2.8f, -0.42f + (i % 3) * 0.08f, 0f), order);
        }

        static void AddReed(Transform parent, string name, Vector3 pos, int order)
        {
            AddSprite(parent, name, "reeds", pos, 1.35f, order);
        }

        static void AddRuins(Transform parent, string name, Vector3 pos, int order)
        {
            AddSprite(parent, name, "ruins", pos, 2.8f, order);
        }

        static void AddCliffs(Transform parent, string name, Vector3 pos, int order)
        {
            AddSprite(parent, name, "cliffs", pos, 3.2f, order);
        }

        static void AddSpire(Transform parent, string name, Vector3 pos, int order)
        {
            AddSprite(parent, name, "spire", pos, 5.0f, order);
        }

        static void AddFirefly(Transform parent, string name, Vector3 pos, int order) =>
            Part(parent, name, pos, new Vector3(0.12f, 0.12f, 1f), GameVisualTheme.WithAlpha(GameVisualTheme.Accent, 0.75f), order);

        static void AddWell(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Base", new Vector3(0f, -0.08f, 0f), new Vector3(0.9f, 0.46f, 1f), GameVisualTheme.Stone, order);
            Part(root, "Water", new Vector3(0f, 0.04f, 0f), new Vector3(0.58f, 0.18f, 1f), GameVisualTheme.Water, order + 1);
            Part(root, "Roof", new Vector3(0f, 0.42f, 0f), new Vector3(1.05f, 0.26f, 1f), GameVisualTheme.RoadDark, order + 2);
            Part(root, "PostA", new Vector3(-0.34f, 0.2f, 0f), new Vector3(0.08f, 0.56f, 1f), GameVisualTheme.RoadDark, order + 1);
            Part(root, "PostB", new Vector3(0.34f, 0.2f, 0f), new Vector3(0.08f, 0.56f, 1f), GameVisualTheme.RoadDark, order + 1);
        }

        static void AddNoticeBoard(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Post", new Vector3(0f, -0.16f, 0f), new Vector3(0.1f, 0.72f, 1f), GameVisualTheme.RoadDark, order);
            Part(root, "Board", new Vector3(0f, 0.24f, 0f), new Vector3(1.1f, 0.62f, 1f), GameVisualTheme.ParchmentDark, order + 1);
            Part(root, "PaperA", new Vector3(-0.22f, 0.28f, 0f), new Vector3(0.28f, 0.28f, 1f), GameVisualTheme.Parchment, order + 2);
            Part(root, "PaperB", new Vector3(0.2f, 0.18f, 0f), new Vector3(0.32f, 0.22f, 1f), GameVisualTheme.Cream, order + 2);
        }

        static void AddRingStone(Transform parent, string name, Vector3 pos, int order) =>
            Part(parent, name, pos, new Vector3(0.34f, 0.72f, 1f), GameVisualTheme.Brighten(GameVisualTheme.Stone, 0.08f), order);

        static void AddMineCart(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Cart", new Vector3(0f, 0.05f, 0f), new Vector3(1.0f, 0.42f, 1f), GameVisualTheme.RoadDark, order);
            Part(root, "Ore", new Vector3(0.12f, 0.32f, 0f), new Vector3(0.55f, 0.24f, 1f), GameVisualTheme.Stone, order + 1);
            Part(root, "WheelA", new Vector3(-0.32f, -0.2f, 0f), new Vector3(0.18f, 0.18f, 1f), GameVisualTheme.Ink, order + 1);
            Part(root, "WheelB", new Vector3(0.36f, -0.2f, 0f), new Vector3(0.18f, 0.18f, 1f), GameVisualTheme.Ink, order + 1);
        }

        static void AddCrane(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Mast", new Vector3(0f, 0.35f, 0f), new Vector3(0.12f, 1.3f, 1f), GameVisualTheme.RoadDark, order);
            Part(root, "Beam", new Vector3(0.48f, 0.92f, 0f), new Vector3(1.2f, 0.1f, 1f), GameVisualTheme.RoadDark, order + 1);
            Part(root, "Hook", new Vector3(1.02f, 0.55f, 0f), new Vector3(0.12f, 0.34f, 1f), GameVisualTheme.InkSoft, order + 1);
        }

        static void AddBridgePosts(Transform parent, string name, Vector2 center, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = new Vector3(center.x, center.y - 0.3f, 0f);
            Part(root, "RailA", new Vector3(0f, 0.34f, 0f), new Vector3(4.4f, 0.12f, 1f), GameVisualTheme.ParchmentDark, order);
            Part(root, "RailB", new Vector3(0f, -0.1f, 0f), new Vector3(4.4f, 0.12f, 1f), GameVisualTheme.RoadDark, order);
            for (var i = 0; i < 5; i++)
                Part(root, $"Post_{i}", new Vector3(-2f + i, 0.08f, 0f), new Vector3(0.1f, 0.72f, 1f), GameVisualTheme.RoadDark, order + 1);
        }

        static void AddMapTable(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Table", Vector3.zero, new Vector3(0.95f, 0.42f, 1f), GameVisualTheme.RoadDark, order);
            Part(root, "Map", new Vector3(0.03f, 0.08f, 0f), new Vector3(0.72f, 0.26f, 1f), GameVisualTheme.Parchment, order + 1);
            Part(root, "Pin", new Vector3(0.26f, 0.14f, 0f), new Vector3(0.08f, 0.08f, 1f), GameVisualTheme.Danger, order + 2);
        }

        static void AddCrates(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "CrateA", new Vector3(-0.18f, -0.02f, 0f), new Vector3(0.42f, 0.38f, 1f), GameVisualTheme.ParchmentDark, order);
            Part(root, "CrateB", new Vector3(0.24f, 0.12f, 0f), new Vector3(0.36f, 0.34f, 1f), GameVisualTheme.RoadDark, order + 1);
            Part(root, "Charm", new Vector3(-0.18f, 0.1f, 0f), new Vector3(0.12f, 0.12f, 1f), GameVisualTheme.Accent, order + 2);
        }

        static void AddTools(Transform parent, string name, Vector3 pos, int order)
        {
            var root = EnsureRoot(parent, name);
            root.localPosition = pos;
            Part(root, "Handle", new Vector3(0f, 0.1f, 0f), new Vector3(0.09f, 0.72f, 1f), GameVisualTheme.RoadDark, order);
            Part(root, "Head", new Vector3(0.18f, 0.42f, 0f), new Vector3(0.5f, 0.12f, 1f), GameVisualTheme.Stone, order + 1);
            Part(root, "Rock", new Vector3(-0.38f, -0.16f, 0f), new Vector3(0.28f, 0.2f, 1f), GameVisualTheme.Stone, order);
        }
    }

    public static class OverworldCharacterVisuals
    {
        static readonly List<InteractionGlow> glows = new List<InteractionGlow>();

        public static void AddPlayer(GameObject host) =>
            AddCharacter(host, "PlayerSprite", GameVisualTheme.AccentBlue, GameVisualTheme.Cream, GameVisualTheme.Ink, 10, 1.05f);

        public static void AddNpc(NPCController npc)
        {
            if (npc == null) return;
            var (body, trim) = npc.Role switch
            {
                NpcRole.Shopkeeper => (GameVisualTheme.Hex(0x8E, 0x5B, 0xB4), GameVisualTheme.Accent),
                NpcRole.Healer => (GameVisualTheme.Hex(0x76, 0xB8, 0x74), GameVisualTheme.Cream),
                NpcRole.BossTrainer => (GameVisualTheme.Danger, GameVisualTheme.Ink),
                NpcRole.Story => (GameVisualTheme.Hex(0xA8, 0x78, 0x4E), GameVisualTheme.Parchment),
                _ => (GameVisualTheme.Hex(0x75, 0x91, 0xB8), GameVisualTheme.Cream)
            };
            AddCharacter(npc.gameObject, $"NpcSprite_{npc.NpcId}", body, trim, GameVisualTheme.Ink, 6, 0.95f, ResolveNpcSprite(npc.NpcId, npc.Role));
        }

        public static void SetHighlightedNpc(NPCController active)
        {
            for (var i = 0; i < glows.Count; i++)
                if (glows[i] != null)
                    glows[i].SetHighlighted(active != null && glows[i].Host == active.gameObject);
        }

        static void AddCharacter(GameObject host, string name, Color body, Color trim, Color outline, int order, float scale, string spriteOverride = null)
        {
            if (host == null) return;
            var root = OverworldPixelVisuals.EnsureRoot(host.transform, name);
            RuntimeUiFactory.DestroyChildren(root);
            root.localPosition = Vector3.zero;
            root.localScale = Vector3.one * scale;
            var spriteName = string.IsNullOrWhiteSpace(spriteOverride) ? ResolveCharacterSprite(body, trim) : spriteOverride;
            OverworldPixelVisuals.AddSprite(root, "Sprite", spriteName, Vector3.zero, 1.7f, order);

            var glow = host.GetComponent<InteractionGlow>();
            if (glow == null) glow = host.AddComponent<InteractionGlow>();
            glow.Bind(host, root);
            if (!glows.Contains(glow))
                glows.Add(glow);
        }

        static string ResolveNpcSprite(string npcId, NpcRole role)
        {
            if (role == NpcRole.BossTrainer)
                return "npc_boss";
            if (role == NpcRole.Healer || npcId == NPCController.HealerPiaId || npcId == NPCController.MoonwellLumaId)
                return "npc_healer";
            if (role == NpcRole.Shopkeeper || npcId == NPCController.QuartermasterBramId || npcId == NPCController.MerchantTomaId)
                return "npc_merchant";
            if (npcId == NPCController.CartographerJessaId || npcId == NPCController.ElderMiraId || npcId == NPCController.ArchivistSelId)
                return "npc_story";
            return "npc_ambient";
        }

        static string ResolveCharacterSprite(Color body, Color trim)
        {
            if (body.r > 0.7f && body.g < 0.45f) return "npc_boss";
            if (body.g > 0.55f && body.r < 0.55f) return "npc_healer";
            if (trim.r > 0.85f && trim.g > 0.65f && body.b > 0.45f) return "npc_merchant";
            if (body.r < 0.45f && body.g > 0.55f) return "player";
            if (body.b > 0.55f && body.r > 0.45f) return "npc_ambient";
            return "npc_story";
        }
    }

    public sealed class InteractionGlow : MonoBehaviour
    {
        Transform spriteRoot;
        Transform prompt;
        Vector3 baseLocalPosition;
        bool highlighted;

        public GameObject Host { get; private set; }

        public void Bind(GameObject host, Transform root)
        {
            Host = host;
            spriteRoot = root;
            baseLocalPosition = spriteRoot != null ? spriteRoot.localPosition : Vector3.zero;
            prompt = OverworldPixelVisuals.EnsureRoot(transform, "InteractPrompt");
            RuntimeUiFactory.DestroyChildren(prompt);
            prompt.localPosition = new Vector3(0f, 1.75f, 0f);
            prompt.localScale = Vector3.one * 1.3f;
            OverworldPixelVisuals.AddSprite(prompt, "Bubble", "prompt_bang", Vector3.zero, 0.7f, 20);
            prompt.gameObject.SetActive(false);
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;
            if (prompt != null && prompt.gameObject.activeSelf != highlighted)
                prompt.gameObject.SetActive(highlighted);
        }

        void Update()
        {
            if (spriteRoot == null) return;
            var bob = Mathf.Sin(Time.time * (highlighted ? 7f : 2.8f)) * (highlighted ? 0.035f : 0.012f);
            spriteRoot.localPosition = baseLocalPosition + new Vector3(0f, bob, 0f);
        }
    }
}
