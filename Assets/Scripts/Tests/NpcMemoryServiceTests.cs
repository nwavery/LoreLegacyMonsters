using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.Tests
{
    public class NpcMemoryServiceTests
    {
        [Test]
        public void RecordConversation_RoundTripsThroughSave()
        {
            var go = new GameObject("NpcMemoryServiceTests");
            try
            {
                var service = go.AddComponent<NpcMemoryService>();
                service.RecordConversation("merchant_toma", "town", "What should I buy?", "Bring potions if you head east.", "supplies");

                var save = service.ExportSave();
                Assert.AreEqual(1, save.Count);
                Assert.AreEqual("merchant_toma", save[0].npcId);
                Assert.AreEqual("town", save[0].lastSeenAreaId);
                Assert.AreEqual("supplies", save[0].lastTopic);

                var reloaded = go.AddComponent<NpcMemoryService>();
                reloaded.ApplySave(save);
                var summary = reloaded.BuildPromptSummary("merchant_toma");
                Assert.IsTrue(summary.Contains("relationship"));
                Assert.IsTrue(summary.Contains("supplies"));
                Assert.IsTrue(summary.Contains("Bring potions"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void RecentTurns_RoundTripInSave()
        {
            var go = new GameObject("NpcMemoryRecentTests");
            try
            {
                var service = go.AddComponent<NpcMemoryService>();
                service.RecordConversation("scout_rin", "route", "Any danger?", "Stay alert eastward.", "danger");
                service.RecordConversation("scout_rin", "route", "Tracks?", "Old prints, nothing fresh.", "tracks");

                var save = service.ExportSave();
                Assert.AreEqual(1, save.Count);
                Assert.IsNotNull(save[0].recentTurns);
                Assert.GreaterOrEqual(save[0].recentTurns.Length, 1);

                var reloaded = go.AddComponent<NpcMemoryService>();
                reloaded.ApplySave(save);
                var summary = reloaded.BuildPromptSummary("scout_rin");
                Assert.IsTrue(summary.Contains("recent:") || summary.Contains("tracks"));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
