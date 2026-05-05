using NUnit.Framework;
using SaveSvc = LoreLegacyMonsters.SaveSystem.SaveSystem;
using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters.Tests
{
    public class SaveLoadSystemTests
    {
        [Test]
        public void SaveInfo_DefaultVersion_IsPositive()
        {
            var s = new SaveInfo();
            Assert.GreaterOrEqual(s.Version, 1);
        }

        [Test]
        public void SaveSystem_SaveAndLoad_RoundTripsPlayerName()
        {
            var data = new SaveInfo { PlayerName = "Tester", Gold = 42 };
            const int slot = 7;
            Assert.IsTrue(SaveSvc.TrySave(slot, data, out var err), err);
            Assert.IsTrue(SaveSvc.TryLoad(slot, out var loaded, out var err2), err2);
            Assert.AreEqual("Tester", loaded.PlayerName);
            Assert.AreEqual(42, loaded.Gold);
        }

        [Test]
        public void SaveSystem_SaveAndLoad_RoundTripsCampaignState()
        {
            const int slot = 8;
            var data = new SaveInfo
            {
                PlayerName = "CampaignTester",
                Gold = 125,
                CurrentAreaId = "ridge",
                ActiveQuestIds = new System.Collections.Generic.List<string> { "quest_ch3_spire" },
                CompletedQuestIds = new System.Collections.Generic.List<string> { "quest_ch2_return" },
                ActiveQuestProgress = new System.Collections.Generic.List<QuestSaveEntry>
                {
                    new QuestSaveEntry
                    {
                        questId = "quest_ch3_spire",
                        objectiveProgress = new System.Collections.Generic.List<int> { 1, 0 }
                    }
                },
                NpcMemories = new System.Collections.Generic.List<NpcMemorySaveEntry>
                {
                    new NpcMemorySaveEntry
                    {
                        npcId = "warden_neris",
                        relationshipTier = 1,
                        conversationCount = 3,
                        lastSeenAreaId = "delta",
                        lastTopic = "spire",
                        memorySummary = "Warned the player about the storm."
                    }
                }
            };

            Assert.IsTrue(SaveSvc.TrySave(slot, data, out var err), err);
            Assert.IsTrue(SaveSvc.TryLoad(slot, out var loaded, out var err2), err2);
            Assert.AreEqual("ridge", loaded.CurrentAreaId);
            CollectionAssert.Contains(loaded.ActiveQuestIds, "quest_ch3_spire");
            CollectionAssert.Contains(loaded.CompletedQuestIds, "quest_ch2_return");
            Assert.AreEqual(1, loaded.ActiveQuestProgress.Count);
            Assert.AreEqual("quest_ch3_spire", loaded.ActiveQuestProgress[0].questId);
            Assert.AreEqual(1, loaded.NpcMemories.Count);
            Assert.AreEqual("warden_neris", loaded.NpcMemories[0].npcId);
        }
    }
}
