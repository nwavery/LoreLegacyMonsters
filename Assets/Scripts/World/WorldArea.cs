using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters.World
{
    [CreateAssetMenu(menuName = "LLM/World Area", fileName = "WorldArea")]
    public class WorldArea : ScriptableObject
    {
        [SerializeField] string areaId;
        [SerializeField] string displayName;
        [SerializeField] List<string> connections = new List<string>();
        [SerializeField] List<string> wildMonsterIds = new List<string>();
        [SerializeField] [TextArea] string travelHint;

        public string AreaId => areaId;
        public string DisplayName => displayName;
        public IReadOnlyList<string> Connections => connections;
        public IReadOnlyList<string> WildMonsterIds => wildMonsterIds;
        public string TravelHint => travelHint;

        public void Configure(string id, string name, params string[] linkedAreaIds)
        {
            areaId = id;
            displayName = name;
            connections = linkedAreaIds != null ? new List<string>(linkedAreaIds) : new List<string>();
            wildMonsterIds = new List<string>();
            travelHint = string.Empty;
        }

        [SerializeField] [Range(0f, 1f)] float wildEncounterChance = 0.15f;

        public float WildEncounterChance => wildEncounterChance;

        public void SetEncounterChance(float c) => wildEncounterChance = Mathf.Clamp01(c);

        public void SetWildEncounters(params string[] monsterIds)
        {
            wildMonsterIds = monsterIds != null ? new List<string>(monsterIds) : new List<string>();
        }

        public void SetTravelHint(string hint) => travelHint = hint ?? string.Empty;
    }
}

