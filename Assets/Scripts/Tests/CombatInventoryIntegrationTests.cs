using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Inventory;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class CombatInventoryIntegrationTests
    {
        [Test]
        public void Inventory_FeedsCombatConsumableConcept()
        {
            var invGo = new GameObject("inv");
            var inv = invGo.AddComponent<InventorySystem>();
            inv.AddItem(SampleItems.PotionId, 1);
            Assert.IsTrue(inv.HasAtLeast(SampleItems.PotionId, 1));
            Object.DestroyImmediate(invGo);
        }
    }
}
