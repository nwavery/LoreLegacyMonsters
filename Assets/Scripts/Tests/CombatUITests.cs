using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.UI;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class CombatUITests
    {
        [Test]
        public void CombatUI_Exists_AsComponent()
        {
            var go = new GameObject("cui");
            var ui = go.AddComponent<CombatUI>();
            Assert.IsNotNull(ui);
            Object.DestroyImmediate(go);
        }
    }
}
