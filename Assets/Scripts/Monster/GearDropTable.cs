using System.Collections.Generic;
using System;
using LoreLegacyMonsters.Inventory;
using UnityEngine;

namespace LoreLegacyMonsters.Monster
{
    [Serializable]
    public sealed class GearDropRollEntry
    {
        public string itemId;
        public Rarity rarity = Rarity.Common;
        public float weight = 1f;
        public bool requiresBoss;
    }

    [CreateAssetMenu(menuName = "LLM/Gear Drop Table", fileName = "GearDropTable")]
    public sealed class GearDropTable : ScriptableObject
    {
        [SerializeField] List<GearDropRollEntry> entries = new List<GearDropRollEntry>();

        public IReadOnlyList<GearDropRollEntry> Entries => entries;

        public void AddEntry(string itemId, Rarity rarity, float w = 1f, bool bossOnly = false)
        {
            if (string.IsNullOrWhiteSpace(itemId)) return;
            entries.Add(new GearDropRollEntry
            {
                itemId = itemId.Trim(),
                rarity = rarity,
                weight = w,
                requiresBoss = bossOnly
            });
        }
    }
}
