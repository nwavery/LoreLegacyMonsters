using System;

namespace LoreLegacyMonsters.Inventory
{
    /// <summary>Namespace-qualified item row (distinct from root LoreLegacyMonsters.Item).</summary>
    [Serializable]
    public class Item
    {
        public string itemId;
        public int count;
    }
}
