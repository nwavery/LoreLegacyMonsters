using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.Core
{
    public static class ContentCatalogValidator
    {
        public static List<string> ValidateResources()
        {
            var issues = new List<string>();
            var monsters = Resources.LoadAll<MonsterData>("Monsters");
            var monsterIds = ValidateIds(monsters, m => m.MonsterId, "monster", issues);
            ValidateIds(Resources.LoadAll<MoveData>("Moves"), m => m.MoveId, "move", issues);
            ValidateIds(Resources.LoadAll<ItemData>("Items"), i => i.ItemId, "item", issues);
            var areas = Resources.LoadAll<WorldArea>("Areas");
            var areaIds = ValidateIds(areas, a => a.AreaId, "area", issues);
            ValidateIds(Resources.LoadAll<QuestData>("Quests"), q => q.QuestId, "quest", issues);
            ValidateAreaContent(areas, areaIds, monsterIds, issues);
            return issues;
        }

        static HashSet<string> ValidateIds<T>(T[] items, System.Func<T, string> getId, string label, List<string> issues)
            where T : Object
        {
            var seen = new HashSet<string>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    issues.Add($"Null {label} asset in Resources.");
                    continue;
                }

                var id = getId(item);
                if (string.IsNullOrWhiteSpace(id))
                {
                    issues.Add($"{label} asset {item.name} is missing an id.");
                    continue;
                }

                if (!seen.Add(id))
                    issues.Add($"Duplicate {label} id: {id}");
            }

            return seen;
        }

        static void ValidateAreaContent(WorldArea[] areas, HashSet<string> areaIds, HashSet<string> monsterIds, List<string> issues)
        {
            foreach (var area in areas)
            {
                if (area == null || string.IsNullOrWhiteSpace(area.AreaId))
                    continue;

                foreach (var connection in area.Connections)
                {
                    if (string.IsNullOrWhiteSpace(connection))
                    {
                        issues.Add($"Area {area.AreaId} has a blank connection.");
                        continue;
                    }

                    if (!areaIds.Contains(connection))
                        issues.Add($"Area {area.AreaId} links to missing area id: {connection}");
                }

                foreach (var monsterId in area.WildMonsterIds)
                {
                    if (string.IsNullOrWhiteSpace(monsterId))
                    {
                        issues.Add($"Area {area.AreaId} has a blank wild encounter id.");
                        continue;
                    }

                    if (!monsterIds.Contains(monsterId))
                        issues.Add($"Area {area.AreaId} references missing monster id: {monsterId}");
                }
            }
        }
    }
}
