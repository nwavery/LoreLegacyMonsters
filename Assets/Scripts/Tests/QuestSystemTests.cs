using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class QuestSystemTests
    {
        [Test]
        public void QuestManager_StartAndComplete()
        {
            var go = new GameObject("q");
            var q = go.AddComponent<QuestManager>();
            q.StartQuest("a");
            Assert.IsTrue(q.HasActive("a"));
            q.CompleteQuest("a");
            Assert.IsTrue(q.IsCompleted("a"));
            Object.DestroyImmediate(go);
        }
    }
}
