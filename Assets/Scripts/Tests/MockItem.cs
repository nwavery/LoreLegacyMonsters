using UnityEngine;
using LoreLegacyMonsters.Inventory;

namespace LoreLegacyMonsters.Tests
{
    public static class MockItem
    {
        public static ItemData CreatePotion()
        {
            var d = ScriptableObject.CreateInstance<ConsumableData>();
            return d;
        }
    }
}
