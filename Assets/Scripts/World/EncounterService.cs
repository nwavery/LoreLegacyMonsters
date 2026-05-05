using UnityEngine;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.World
{
    public class EncounterService : MonoBehaviour
    {
        [SerializeField] string defaultWildMonsterId = DefaultGameContent.SlimeId;
        [SerializeField] string[] routeMonsterIds = { DefaultGameContent.SlimeId, "monster_voltjay", "monster_shadecub" };
        [SerializeField] string[] forestMonsterIds = { DefaultGameContent.EmberFoxId, "monster_mossback", DefaultGameContent.SlimeId };
        [SerializeField] string[] groveMonsterIds = { DefaultGameContent.ThornBeastId, "monster_mireooze", "monster_blazelynx" };
        [SerializeField] string[] marshMonsterIds = { "monster_mireooze", DefaultGameContent.ReedfangId, DefaultGameContent.LanternMothId };
        [SerializeField] string[] ruinsMonsterIds = { DefaultGameContent.BogWyrmId, DefaultGameContent.LanternMothId, "monster_shadecub" };

        public bool TryRollWildEncounter(AssetRegistryManager registry, WorldManager world, WeatherSystem weather,
            out MonsterData encounter, IRandomSource rng = null)
        {
            rng ??= UnityRandomSource.Default;
            encounter = null;
            if (registry == null || world == null) return false;
            var area = world.GetCurrentArea();
            if (area == null) return false;
            if (!CanEncounterAt(world)) return false;
            float chance = area.WildEncounterChance;
            if (weather != null && weather.Current == WeatherType.Stormy)
                chance = Mathf.Clamp01(chance * 1.15f);
            if (rng.Next01() > chance) return false;
            var monsterId = PickFromArea(area, registry, rng);
            encounter = registry.GetMonster(monsterId);
            return encounter != null;
        }

        public bool CanEncounterAt(WorldManager world)
        {
            if (world == null) return false;
            var area = world.GetCurrentArea();
            var zones = WorldMapLayout.EncounterZones(world.CurrentAreaId);
            if (zones.Length == 0)
                return area != null && area.WildEncounterChance > 0f;
            return WorldMapLayout.IsEncounterPosition(world.CurrentAreaId, world.CurrentPlayerPosition);
        }

        string PickFromArea(WorldArea area, AssetRegistryManager registry, IRandomSource rng)
        {
            if (area != null && area.WildMonsterIds != null && area.WildMonsterIds.Count > 0)
                return PickValid(ToArray(area.WildMonsterIds), registry, rng);

            var fallbackPool = area.AreaId switch
            {
                var id when id == DefaultGameContent.RouteId => routeMonsterIds,
                var id when id == DefaultGameContent.ForestId => forestMonsterIds,
                var id when id == DefaultGameContent.GroveId => groveMonsterIds,
                var id when id == DefaultGameContent.MarshId => marshMonsterIds,
                var id when id == DefaultGameContent.RuinsId => ruinsMonsterIds,
                _ => null
            };
            return PickValid(fallbackPool, registry, rng);
        }

        string PickValid(string[] ids, AssetRegistryManager registry, IRandomSource rng)
        {
            if (ids != null && ids.Length > 0)
            {
                var start = rng.NextInt(0, ids.Length);
                for (var i = 0; i < ids.Length; i++)
                {
                    var candidate = ids[(start + i) % ids.Length];
                    if (string.IsNullOrWhiteSpace(candidate))
                        continue;
                    if (registry == null || registry.GetMonster(candidate) != null)
                        return candidate;
                }
            }

            return defaultWildMonsterId;
        }

        static string[] ToArray(System.Collections.Generic.IReadOnlyList<string> ids)
        {
            var array = new string[ids.Count];
            for (var i = 0; i < ids.Count; i++)
                array[i] = ids[i];
            return array;
        }
    }
}
