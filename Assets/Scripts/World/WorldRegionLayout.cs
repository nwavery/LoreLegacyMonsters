using System.Collections.Generic;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using UnityEngine;

namespace LoreLegacyMonsters.World
{
    public readonly struct NpcSpawnAnchor
    {
        public readonly string NpcId;
        public readonly Vector2 Position;

        public NpcSpawnAnchor(string npcId, Vector2 position)
        {
            NpcId = npcId;
            Position = position;
        }
    }

    public readonly struct WorldMapEdge
    {
        public readonly string FromAreaId;
        public readonly string ToAreaId;

        public WorldMapEdge(string fromAreaId, string toAreaId)
        {
            FromAreaId = fromAreaId;
            ToAreaId = toAreaId;
        }

        public bool Contains(string areaId) => FromAreaId == areaId || ToAreaId == areaId;
    }

    public enum EncounterZoneType
    {
        Grass,
        Reeds,
        Rubble,
        RidgeNest,
        MoonwellGlade,
        QuarryPit,
        Crystal
    }

    public readonly struct EncounterZone
    {
        public readonly string Name;
        public readonly EncounterZoneType Type;
        public readonly Rect Bounds;

        public EncounterZone(string name, EncounterZoneType type, Rect bounds)
        {
            Name = name;
            Type = type;
            Bounds = bounds;
        }
    }

    public sealed class WorldRegionLayout
    {
        public readonly string AreaId;
        public readonly string ShortLabel;
        public readonly Rect Bounds;
        public readonly Vector2 SpawnPoint;
        public readonly Vector2 MapPosition;
        public readonly string Biome;
        public readonly bool PhaseTwo;
        public readonly NpcSpawnAnchor[] NpcAnchors;

        public WorldRegionLayout(string areaId, string shortLabel, Rect bounds, Vector2 spawnPoint, Vector2 mapPosition,
            string biome, bool phaseTwo = false, params NpcSpawnAnchor[] npcAnchors)
        {
            AreaId = areaId;
            ShortLabel = shortLabel;
            Bounds = bounds;
            SpawnPoint = spawnPoint;
            MapPosition = mapPosition;
            Biome = biome;
            PhaseTwo = phaseTwo;
            NpcAnchors = npcAnchors ?? System.Array.Empty<NpcSpawnAnchor>();
        }
    }

    public static class WorldMapLayout
    {
        static readonly WorldRegionLayout[] Regions =
        {
            new WorldRegionLayout(DefaultGameContent.TownId, "Hollowfen", new Rect(-8f, -6f, 18f, 11f),
                new Vector2(2f, -1f), new Vector2(-360f, -28f), "town", false,
                new NpcSpawnAnchor(NPCController.ElderMiraId, new Vector2(3.8f, 0.8f)),
                new NpcSpawnAnchor(NPCController.MerchantTomaId, new Vector2(0.2f, -1.3f)),
                new NpcSpawnAnchor(NPCController.HealerPiaId, new Vector2(-2.4f, -1.1f))),
            new WorldRegionLayout(DefaultGameContent.RouteId, "Eastfen", new Rect(10f, -7f, 18f, 9f),
                new Vector2(18f, -1f), new Vector2(-230f, -55f), "route", false,
                new NpcSpawnAnchor(NPCController.ScoutRinId, new Vector2(18f, -0.8f))),
            new WorldRegionLayout(DefaultGameContent.ForestId, "Bramblewood", new Rect(28f, -7f, 15f, 10f),
                new Vector2(34f, -1f), new Vector2(-90f, -70f), "forest"),
            new WorldRegionLayout(DefaultGameContent.GroveId, "Old Grove", new Rect(43f, -6f, 14f, 10f),
                new Vector2(48f, -0.8f), new Vector2(30f, -35f), "grove", false,
                new NpcSpawnAnchor(NPCController.BossIonaId, new Vector2(49f, 0.2f))),
            new WorldRegionLayout(DefaultGameContent.MarshId, "Lantern Marsh", new Rect(54f, -8f, 15f, 12f),
                new Vector2(58f, -1f), new Vector2(120f, -90f), "marsh", false,
                new NpcSpawnAnchor(NPCController.ArchivistSelId, new Vector2(59f, -0.8f))),
            new WorldRegionLayout(DefaultGameContent.RuinsId, "Archive", new Rect(70f, -7f, 16f, 11f),
                new Vector2(78f, -1f), new Vector2(225f, -70f), "ruins", false,
                new NpcSpawnAnchor(NPCController.RivalCorinId, new Vector2(78f, -0.9f))),
            new WorldRegionLayout(DefaultGameContent.DeltaId, "Delta", new Rect(86f, -8f, 16f, 12f),
                new Vector2(90f, -1f), new Vector2(300f, -5f), "delta", false,
                new NpcSpawnAnchor(NPCController.WardenNerisId, new Vector2(91f, -0.9f)),
                new NpcSpawnAnchor(NPCController.CollectorVeyaId, new Vector2(87f, -1.2f))),
            new WorldRegionLayout(DefaultGameContent.RidgeId, "Ridge", new Rect(102f, -6f, 17f, 11f),
                new Vector2(106f, -1f), new Vector2(370f, 70f), "ridge", false,
                new NpcSpawnAnchor(NPCController.MentorCaelId, new Vector2(107f, -0.8f)),
                new NpcSpawnAnchor(NPCController.RumorIrisId, new Vector2(101f, -1f))),
            new WorldRegionLayout(DefaultGameContent.SpireId, "Spire", new Rect(119f, -5f, 16f, 12f),
                new Vector2(126f, -1f), new Vector2(420f, 155f), "spire", false,
                new NpcSpawnAnchor(NPCController.StormTyrantId, new Vector2(126f, -0.8f))),
            new WorldRegionLayout(DefaultGameContent.BramblewoodNorthId, "Northwood", new Rect(22f, 4f, 24f, 14f),
                new Vector2(31f, 6f), new Vector2(-80f, 55f), "forest_north", true),
            new WorldRegionLayout(DefaultGameContent.MarshBasinId, "Marsh Basin", new Rect(48f, 5f, 24f, 15f),
                new Vector2(58f, 7f), new Vector2(95f, 78f), "marsh_basin", true,
                new NpcSpawnAnchor(NPCController.RunnerNiaId, new Vector2(55.5f, 8.4f))),
            new WorldRegionLayout(DefaultGameContent.StonewakeId, "Stonewake", new Rect(8f, 6f, 16f, 13f),
                new Vector2(14f, 9f), new Vector2(-230f, 95f), "hamlet", true,
                new NpcSpawnAnchor(NPCController.CartographerJessaId, new Vector2(12.3f, 9.2f)),
                new NpcSpawnAnchor(NPCController.QuartermasterBramId, new Vector2(16.8f, 8.5f))),
            new WorldRegionLayout(DefaultGameContent.MoonwellId, "Moonwell", new Rect(30f, 19f, 22f, 14f),
                new Vector2(39f, 22f), new Vector2(-10f, 180f), "moonwell", true,
                new NpcSpawnAnchor(NPCController.MoonwellLumaId, new Vector2(39f, 23.1f))),
            new WorldRegionLayout(DefaultGameContent.QuarryId, "Ironroot", new Rect(74f, 6f, 22f, 14f),
                new Vector2(83f, 9f), new Vector2(250f, 105f), "quarry", true,
                new NpcSpawnAnchor(NPCController.ForemanOrloId, new Vector2(84f, 9.3f))),
            new WorldRegionLayout(DefaultGameContent.CrossingId, "Tideglass", new Rect(95f, 5f, 22f, 14f),
                new Vector2(104f, 8f), new Vector2(355f, 135f), "crossing", true,
                new NpcSpawnAnchor(NPCController.SableRivalId, new Vector2(105f, 8.6f))),
            new WorldRegionLayout(DefaultGameContent.StarfallId, "Starfall", new Rect(58f, 21f, 23f, 15f),
                new Vector2(68f, 25f), new Vector2(150f, 210f), "starfall", true,
                new NpcSpawnAnchor(NPCController.EthicistThrenId, new Vector2(67f, 25.6f))),
        };

        static readonly WorldMapEdge[] Edges =
        {
            new WorldMapEdge(DefaultGameContent.TownId, DefaultGameContent.RouteId),
            new WorldMapEdge(DefaultGameContent.RouteId, DefaultGameContent.ForestId),
            new WorldMapEdge(DefaultGameContent.ForestId, DefaultGameContent.GroveId),
            new WorldMapEdge(DefaultGameContent.GroveId, DefaultGameContent.MarshId),
            new WorldMapEdge(DefaultGameContent.MarshId, DefaultGameContent.RuinsId),
            new WorldMapEdge(DefaultGameContent.RuinsId, DefaultGameContent.DeltaId),
            new WorldMapEdge(DefaultGameContent.DeltaId, DefaultGameContent.RidgeId),
            new WorldMapEdge(DefaultGameContent.RidgeId, DefaultGameContent.SpireId),
            new WorldMapEdge(DefaultGameContent.RouteId, DefaultGameContent.StonewakeId),
            new WorldMapEdge(DefaultGameContent.ForestId, DefaultGameContent.BramblewoodNorthId),
            new WorldMapEdge(DefaultGameContent.MarshId, DefaultGameContent.MarshBasinId),
            new WorldMapEdge(DefaultGameContent.DeltaId, DefaultGameContent.CrossingId),
            new WorldMapEdge(DefaultGameContent.StonewakeId, DefaultGameContent.BramblewoodNorthId),
            new WorldMapEdge(DefaultGameContent.StonewakeId, DefaultGameContent.MarshBasinId),
            new WorldMapEdge(DefaultGameContent.BramblewoodNorthId, DefaultGameContent.MoonwellId),
            new WorldMapEdge(DefaultGameContent.MarshBasinId, DefaultGameContent.StarfallId),
            new WorldMapEdge(DefaultGameContent.MoonwellId, DefaultGameContent.StarfallId),
            new WorldMapEdge(DefaultGameContent.RuinsId, DefaultGameContent.QuarryId),
            new WorldMapEdge(DefaultGameContent.QuarryId, DefaultGameContent.CrossingId),
            new WorldMapEdge(DefaultGameContent.CrossingId, DefaultGameContent.RidgeId),
            new WorldMapEdge(DefaultGameContent.QuarryId, DefaultGameContent.StarfallId)
        };

        public static IReadOnlyList<WorldRegionLayout> All => Regions;

        public static IReadOnlyList<WorldMapEdge> MapEdges => Edges;

        public static WorldRegionLayout Get(string areaId)
        {
            if (string.IsNullOrEmpty(areaId)) return Regions[0];
            for (var i = 0; i < Regions.Length; i++)
                if (Regions[i].AreaId == areaId)
                    return Regions[i];
            return Regions[0];
        }

        public static string ResolveAreaId(Vector2 position)
        {
            for (var i = Regions.Length - 1; i >= 0; i--)
                if (Regions[i].Bounds.Contains(position))
                    return Regions[i].AreaId;
            return NearestRegion(position).AreaId;
        }

        public static WorldRegionLayout NearestRegion(Vector2 position)
        {
            var best = Regions[0];
            var bestDistance = float.MaxValue;
            for (var i = 0; i < Regions.Length; i++)
            {
                var d = Vector2.SqrMagnitude(ClosestPoint(Regions[i].Bounds, position) - position);
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = Regions[i];
            }

            return best;
        }

        public static Vector2 SpawnPoint(string areaId) => Get(areaId).SpawnPoint;

        public static Vector2 MapPosition(string areaId) => Get(areaId).MapPosition;

        public static EncounterZone[] EncounterZones(string areaId)
        {
            return areaId switch
            {
                DefaultGameContent.RouteId => new[]
                {
                    new EncounterZone("South verge grass", EncounterZoneType.Grass, new Rect(12.5f, -3.3f, 4.8f, 2.1f)),
                    new EncounterZone("Low roadside grass", EncounterZoneType.Grass, new Rect(21.4f, -3.9f, 4.2f, 2.4f))
                },
                DefaultGameContent.ForestId => new[]
                {
                    new EncounterZone("Bramble thicket", EncounterZoneType.Grass, new Rect(30.2f, -4.1f, 5.4f, 3.2f)),
                    new EncounterZone("North tree shade", EncounterZoneType.Grass, new Rect(36.6f, 0.1f, 4.6f, 2.4f))
                },
                DefaultGameContent.GroveId => new[]
                {
                    new EncounterZone("Warden briars", EncounterZoneType.Grass, new Rect(44.2f, -3.8f, 5.4f, 3.0f)),
                    new EncounterZone("Old grove hollow", EncounterZoneType.Grass, new Rect(50.5f, 0.1f, 4.0f, 2.8f))
                },
                DefaultGameContent.MarshId => new[]
                {
                    new EncounterZone("Lantern reeds", EncounterZoneType.Reeds, new Rect(55.2f, -5.3f, 4.8f, 2.8f)),
                    new EncounterZone("Floodgrass pool", EncounterZoneType.Reeds, new Rect(62.0f, -1.4f, 4.2f, 2.6f))
                },
                DefaultGameContent.RuinsId => new[]
                {
                    new EncounterZone("Archive rubble", EncounterZoneType.Rubble, new Rect(72.2f, -4.7f, 4.8f, 3.0f)),
                    new EncounterZone("Broken wardstone", EncounterZoneType.Rubble, new Rect(78.6f, 0.0f, 4.4f, 2.4f))
                },
                DefaultGameContent.DeltaId => new[]
                {
                    new EncounterZone("Delta reeds", EncounterZoneType.Reeds, new Rect(88.0f, -5.6f, 5.0f, 3.0f)),
                    new EncounterZone("Canal grass", EncounterZoneType.Reeds, new Rect(95.0f, -1.2f, 4.0f, 2.6f))
                },
                DefaultGameContent.RidgeId => new[]
                {
                    new EncounterZone("Ridge nest", EncounterZoneType.RidgeNest, new Rect(104.0f, -3.9f, 5.2f, 2.8f)),
                    new EncounterZone("Cliff roost", EncounterZoneType.RidgeNest, new Rect(111.0f, 0.6f, 4.6f, 2.6f))
                },
                DefaultGameContent.SpireId => new[]
                {
                    new EncounterZone("Skyglass shards", EncounterZoneType.Crystal, new Rect(122.0f, -2.7f, 4.8f, 2.5f)),
                    new EncounterZone("Stormglass nest", EncounterZoneType.Crystal, new Rect(128.0f, 1.5f, 4.4f, 2.8f))
                },
                DefaultGameContent.BramblewoodNorthId => new[]
                {
                    new EncounterZone("North bramble", EncounterZoneType.Grass, new Rect(25.0f, 6.0f, 6.0f, 3.8f)),
                    new EncounterZone("Highwood thicket", EncounterZoneType.Grass, new Rect(36.0f, 11.2f, 6.4f, 4.0f))
                },
                DefaultGameContent.MarshBasinId => new[]
                {
                    new EncounterZone("Basin reeds", EncounterZoneType.Reeds, new Rect(50.5f, 7.0f, 5.8f, 3.6f)),
                    new EncounterZone("Deep lantern grass", EncounterZoneType.Reeds, new Rect(62.8f, 12.0f, 5.6f, 4.0f))
                },
                DefaultGameContent.MoonwellId => new[]
                {
                    new EncounterZone("Moonwell glade west", EncounterZoneType.MoonwellGlade, new Rect(33.0f, 20.2f, 4.4f, 3.2f)),
                    new EncounterZone("Moonwell glade east", EncounterZoneType.MoonwellGlade, new Rect(44.2f, 24.2f, 4.8f, 3.4f))
                },
                DefaultGameContent.QuarryId => new[]
                {
                    new EncounterZone("Lower quarry pit", EncounterZoneType.QuarryPit, new Rect(76.0f, 7.0f, 5.2f, 3.3f)),
                    new EncounterZone("Ironroot rubble", EncounterZoneType.Rubble, new Rect(88.0f, 11.4f, 5.6f, 3.6f))
                },
                DefaultGameContent.CrossingId => new[]
                {
                    new EncounterZone("Bridge reeds", EncounterZoneType.Reeds, new Rect(100.0f, 6.5f, 5.0f, 3.0f)),
                    new EncounterZone("Tideglass grass", EncounterZoneType.Grass, new Rect(109.0f, 11.0f, 5.2f, 3.4f))
                },
                DefaultGameContent.StarfallId => new[]
                {
                    new EncounterZone("Starfall crystal grass", EncounterZoneType.Crystal, new Rect(61.0f, 23.0f, 5.2f, 3.8f)),
                    new EncounterZone("Hollow rubble", EncounterZoneType.Rubble, new Rect(72.0f, 28.0f, 5.6f, 3.8f))
                },
                _ => System.Array.Empty<EncounterZone>()
            };
        }

        public static bool IsEncounterPosition(string areaId, Vector2 position)
        {
            if (IsVisualRoadPosition(position))
                return false;
            var zones = EncounterZones(areaId);
            for (var i = 0; i < zones.Length; i++)
                if (zones[i].Bounds.Contains(position))
                    return true;
            return false;
        }

        public static bool IsVisualRoadPosition(Vector2 position)
        {
            const float roadHalfWidth = 0.42f;
            for (var i = 0; i < Edges.Length; i++)
            {
                var a = SpawnPoint(Edges[i].FromAreaId);
                var b = SpawnPoint(Edges[i].ToAreaId);
                if (DistanceToSegment(position, a, b) <= roadHalfWidth)
                    return true;
            }

            return false;
        }

        public static Rect[] BlockerZones(string areaId)
        {
            return areaId switch
            {
                DefaultGameContent.TownId => new[]
                {
                    new Rect(-1.5f, -1.8f, 3.2f, 2.1f),
                    new Rect(3.0f, -1.6f, 3.0f, 2.0f)
                },
                DefaultGameContent.StonewakeId => new[]
                {
                    new Rect(15.6f, 8.2f, 4.1f, 2.6f),
                    new Rect(10.8f, 9.0f, 2.2f, 1.5f)
                },
                DefaultGameContent.MoonwellId => new[]
                {
                    new Rect(40.2f, 21.5f, 3.8f, 2.0f)
                },
                DefaultGameContent.QuarryId => new[]
                {
                    new Rect(75.0f, 7.0f, 2.2f, 2.0f),
                    new Rect(83.0f, 10.4f, 3.6f, 2.0f)
                },
                DefaultGameContent.StarfallId => new[]
                {
                    new Rect(66.0f, 24.2f, 3.0f, 2.4f)
                },
                _ => System.Array.Empty<Rect>()
            };
        }

        public static bool IsBlockedPosition(Vector2 position) => IsBlockedPosition(position, 0f);

        public static bool IsBlockedPosition(Vector2 position, float radius)
        {
            var areaId = ResolveAreaId(position);
            var blockers = BlockerZones(areaId);
            for (var i = 0; i < blockers.Length; i++)
                if (Padded(blockers[i], radius).Contains(position))
                    return true;
            return false;
        }

        public static Vector2 ResolveNavigation(Vector2 from, Vector2 to) => ResolveNavigation(from, to, 0f);

        public static Vector2 ResolveNavigation(Vector2 from, Vector2 to, float radius)
        {
            var bounds = WorldBounds();
            radius = Mathf.Max(0f, radius);
            to = new Vector2(
                Mathf.Clamp(to.x, bounds.xMin + radius, bounds.xMax - radius),
                Mathf.Clamp(to.y, bounds.yMin + radius, bounds.yMax - radius));
            if (!IsBlockedPosition(to, radius)) return to;

            var delta = to - from;
            var slideX = new Vector2(to.x, from.y);
            var slideY = new Vector2(from.x, to.y);
            var tryXFirst = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            if (tryXFirst)
            {
                if (!IsBlockedPosition(slideY, radius)) return slideY;
                if (!IsBlockedPosition(slideX, radius)) return slideX;
            }
            else
            {
                if (!IsBlockedPosition(slideX, radius)) return slideX;
                if (!IsBlockedPosition(slideY, radius)) return slideY;
            }

            var pushed = PushOutOfBlockers(from, to, radius);
            if (!IsBlockedPosition(pushed, radius)) return pushed;
            return from;
        }

        public static Rect WorldBounds()
        {
            var minX = Regions[0].Bounds.xMin;
            var minY = Regions[0].Bounds.yMin;
            var maxX = Regions[0].Bounds.xMax;
            var maxY = Regions[0].Bounds.yMax;
            for (var i = 1; i < Regions.Length; i++)
            {
                var r = Regions[i].Bounds;
                minX = Mathf.Min(minX, r.xMin);
                minY = Mathf.Min(minY, r.yMin);
                maxX = Mathf.Max(maxX, r.xMax);
                maxY = Mathf.Max(maxY, r.yMax);
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        static Vector2 ClosestPoint(Rect rect, Vector2 position) =>
            new Vector2(Mathf.Clamp(position.x, rect.xMin, rect.xMax), Mathf.Clamp(position.y, rect.yMin, rect.yMax));

        static Rect Padded(Rect rect, float padding)
        {
            if (padding <= 0f) return rect;
            return Rect.MinMaxRect(rect.xMin - padding, rect.yMin - padding, rect.xMax + padding, rect.yMax + padding);
        }

        static Vector2 PushOutOfBlockers(Vector2 from, Vector2 to, float radius)
        {
            var areaId = ResolveAreaId(to);
            var blockers = BlockerZones(areaId);
            var result = to;
            const float epsilon = 0.015f;
            for (var i = 0; i < blockers.Length; i++)
            {
                var rect = Padded(blockers[i], radius);
                if (!rect.Contains(result)) continue;

                var left = Mathf.Abs(result.x - rect.xMin);
                var right = Mathf.Abs(rect.xMax - result.x);
                var bottom = Mathf.Abs(result.y - rect.yMin);
                var top = Mathf.Abs(rect.yMax - result.y);

                if (from.x <= rect.xMin && to.x > rect.xMin)
                    result.x = rect.xMin - epsilon;
                else if (from.x >= rect.xMax && to.x < rect.xMax)
                    result.x = rect.xMax + epsilon;
                else if (from.y <= rect.yMin && to.y > rect.yMin)
                    result.y = rect.yMin - epsilon;
                else if (from.y >= rect.yMax && to.y < rect.yMax)
                    result.y = rect.yMax + epsilon;
                else
                {
                    var min = Mathf.Min(left, right, bottom, top);
                    if (min == left) result.x = rect.xMin - epsilon;
                    else if (min == right) result.x = rect.xMax + epsilon;
                    else if (min == bottom) result.y = rect.yMin - epsilon;
                    else result.y = rect.yMax + epsilon;
                }
            }

            return result;
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var lenSq = ab.sqrMagnitude;
            if (lenSq <= 0.0001f) return Vector2.Distance(p, a);
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
