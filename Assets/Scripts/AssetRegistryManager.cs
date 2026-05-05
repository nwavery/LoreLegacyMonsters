using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.Inventory;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters
{
    public class AssetRegistryManager : MonoBehaviour
    {
        [SerializeField] List<ItemData> items = new List<ItemData>();
        [SerializeField] List<MonsterData> monsters = new List<MonsterData>();

        readonly Dictionary<string, ItemData> itemMap = new Dictionary<string, ItemData>();
        readonly Dictionary<string, MonsterData> monsterMap = new Dictionary<string, MonsterData>();

        void Awake()
        {
            RebuildMaps();
        }

        void RebuildMaps()
        {
            itemMap.Clear();
            monsterMap.Clear();
            foreach (var i in items)
                if (i != null && !string.IsNullOrEmpty(i.ItemId))
                    itemMap[i.ItemId] = i;
            foreach (var m in monsters)
                if (m != null && !string.IsNullOrEmpty(m.MonsterId))
                    monsterMap[m.MonsterId] = m;
        }

        public void RegisterItem(ItemData data)
        {
            if (data == null || string.IsNullOrEmpty(data.ItemId)) return;
            if (!items.Contains(data)) items.Add(data);
            itemMap[data.ItemId] = data;
        }

        public void RegisterMonster(MonsterData data)
        {
            if (data == null || string.IsNullOrEmpty(data.MonsterId)) return;
            if (!monsters.Contains(data)) monsters.Add(data);
            monsterMap[data.MonsterId] = data;
        }

        public ItemData GetItem(string id) => itemMap.TryGetValue(id, out var d) ? d : null;

        public MonsterData GetMonster(string id) => monsterMap.TryGetValue(id, out var d) ? d : null;
    }
}
