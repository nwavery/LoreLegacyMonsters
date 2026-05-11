using NUnit.Framework;
using LoreLegacyMonsters.SaveSystem;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.Tests
{
    public class SaveMigrationTests
    {
        [Test]
        public void SaveSystem_MigrateInPlace_PopulatesStructuredPartyFromLegacyIds()
        {
            var save = new SaveInfo
            {
                Version = 1,
                PartyMonsterIds = new System.Collections.Generic.List<string> { "monster_slime", "monster_emberfox" }
            };

            SaveSystem.SaveSystem.MigrateInPlace(save);

            Assert.AreEqual(2, save.Party.Count);
            Assert.AreEqual("monster_slime", save.Party[0].monsterDataId);
            Assert.AreEqual("monster_emberfox", save.Party[1].monsterDataId);
            Assert.IsNotNull(save.Reserve);
            Assert.IsNotNull(save.Party[0].learnedMoveIds);
            Assert.IsNotNull(save.NpcMemories);
        }

        [Test]
        public void SaveSystem_MigrateInPlace_InitializesStoryFlags_AndPreservesStoryKeys()
        {
            var save = new SaveInfo
            {
                Version = 1,
                StoryFlags = new System.Collections.Generic.List<string>
                {
                    "phase_two_test_flag",
                    "kv::iona_outcome=spare",
                    "kv::mira_trust=2"
                }
            };

            SaveSystem.SaveSystem.MigrateInPlace(save);
            StoryFlags.ApplySave(save.StoryFlags);

            Assert.IsNotNull(save.StoryFlags);
            Assert.IsTrue(StoryFlags.HasFlag("phase_two_test_flag"));
            Assert.AreEqual("spare", StoryFlags.GetValue("iona_outcome"));
            Assert.AreEqual(2, StoryFlags.GetInt("mira_trust"));
        }

        [Test]
        public void SaveSystem_MigrateInPlace_SetsV1SchemaTag_ForLegacySave()
        {
            var save = new SaveInfo
            {
                Version = 7,
                SaveSchemaTag = ""
            };

            SaveSystem.SaveSystem.MigrateInPlace(save);

            Assert.AreEqual("v1.0", save.SaveSchemaTag);
        }

        [Test]
        public void SaveInfo_Defaults_ToVersion9()
        {
            var save = new SaveInfo();
            Assert.AreEqual(9, save.Version);
            Assert.AreEqual("v1.0", save.SaveSchemaTag);
        }

        [Test]
        public void SaveSystem_MigrateInPlace_FromV8_Creates_LoadoutBuckets()
        {
            var save = new SaveInfo
            {
                Version = 8,
                SaveSchemaTag = "v1.0",
                Loadout = null
            };

            SaveSystem.SaveSystem.MigrateInPlace(save);

            Assert.IsNotNull(save.Loadout);
            Assert.IsNotNull(save.Loadout.charmItemIds);
            Assert.AreEqual(3, save.Loadout.charmItemIds.Count);
            Assert.AreEqual("", save.Loadout.outfitItemId);
            Assert.AreEqual("", save.Loadout.charmItemIds[0]);
        }
    }
}
