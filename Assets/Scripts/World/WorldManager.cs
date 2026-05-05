using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.World
{
    public class WorldManager : MonoBehaviour
    {
        readonly Dictionary<string, WorldArea> areas = new Dictionary<string, WorldArea>();
        readonly HashSet<string> discoveredAreaIds = new HashSet<string>();

        public string CurrentAreaId { get; private set; } = DefaultGameContent.TownId;
        public Vector2 CurrentPlayerPosition { get; private set; } = new Vector2(2f, -1f);

        public void RegisterArea(WorldArea area)
        {
            if (area == null || string.IsNullOrEmpty(area.AreaId)) return;
            areas[area.AreaId] = area;
            if (area.AreaId == CurrentAreaId)
                MarkAreaDiscovered(area.AreaId);
        }

        public WorldArea GetArea(string id) =>
            string.IsNullOrEmpty(id) ? null : areas.TryGetValue(id, out var a) ? a : null;

        public WorldArea GetCurrentArea() => GetArea(CurrentAreaId);

        public void SetCurrentArea(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            CurrentAreaId = id;
            MarkAreaDiscovered(CurrentAreaId);
            GameEvents.RaiseAreaChanged(CurrentAreaId);
        }

        public bool TryTravelTo(string targetAreaId)
        {
            if (string.IsNullOrEmpty(targetAreaId)) return false;
            var cur = GetCurrentArea();
            if (cur == null)
            {
                if (areas.ContainsKey(targetAreaId))
                {
                    CurrentAreaId = targetAreaId;
                    MarkAreaDiscovered(CurrentAreaId);
                    GameEvents.RaiseAreaChanged(CurrentAreaId);
                    return true;
                }

                return false;
            }

            if (!cur.ConnectsTo(targetAreaId)) return false;
            CurrentAreaId = targetAreaId;
            MarkAreaDiscovered(CurrentAreaId);
            GameEvents.RaiseAreaChanged(CurrentAreaId);
            return true;
        }

        public bool IsAreaDiscovered(string areaId) =>
            !string.IsNullOrEmpty(areaId) && discoveredAreaIds.Contains(areaId);

        public List<string> GetDiscoveredAreaIds() => new List<string>(discoveredAreaIds);

        public void SetCurrentPlayerPosition(Vector2 position) => CurrentPlayerPosition = position;

        public void ApplyDiscoveredAreaIds(List<string> areaIds)
        {
            discoveredAreaIds.Clear();
            if (areaIds != null)
                for (var i = 0; i < areaIds.Count; i++)
                    MarkAreaDiscovered(areaIds[i]);
            MarkAreaDiscovered(CurrentAreaId);
        }

        void MarkAreaDiscovered(string areaId)
        {
            if (string.IsNullOrEmpty(areaId)) return;
            discoveredAreaIds.Add(areaId);
        }
    }
}
