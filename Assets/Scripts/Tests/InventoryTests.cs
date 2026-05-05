using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class InventoryTests
    {
        [Test]
        public void AddItem_IncreasesCount()
        {
            var go = new GameObject("inv");
            var inv = go.AddComponent<InventorySystem>();
            Assert.IsTrue(inv.AddItem("a", 2));
            Assert.AreEqual(2, inv.Count("a"));
            Object.DestroyImmediate(go);
        }
    }
}
