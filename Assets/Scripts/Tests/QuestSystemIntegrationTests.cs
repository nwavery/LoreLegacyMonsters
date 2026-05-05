using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Quest;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class QuestSystemIntegrationTests
    {
        [Test]
        public void QuestSystem_FindsQuestManager()
        {
            var go = new GameObject("qs");
            var q = go.AddComponent<QuestManager>();
            var sys = go.AddComponent<QuestSystem>();
            Assert.AreEqual(q, sys.Manager);
            Object.DestroyImmediate(go);
        }
    }
}
