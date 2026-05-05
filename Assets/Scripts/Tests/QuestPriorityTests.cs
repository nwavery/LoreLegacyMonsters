using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Questing;
using LoreLegacyMonsters.Quests;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class QuestPriorityTests
    {
        [Test]
        public void PrimaryQuest_PrefersHigherChapterStoryQuest()
        {
            var go = new GameObject("quest-priority");
            try
            {
                var manager = go.AddComponent<QuestManager>();

                var chapterOne = ScriptableObject.CreateInstance<QuestData>();
                chapterOne.Configure("quest_return", "Return To Hollowfen", "Wrap up Chapter One.",
                    new[] { new QuestObjective { objectiveId = "return", description = "Return home", requiredCount = 1 } });
                var chapterTwo = ScriptableObject.CreateInstance<QuestData>();
                chapterTwo.Configure("quest_ch2_archive", "Echoes In Stone", "Push into the archive.",
                    new[] { new QuestObjective { objectiveId = "archive", description = "Enter the archive", requiredCount = 1 } });

                manager.RegisterQuestDefinition(chapterOne);
                manager.RegisterQuestDefinition(chapterTwo);
                manager.StartQuest("quest_return");
                manager.StartQuest("quest_ch2_archive");

                Assert.AreEqual("quest_ch2_archive", manager.GetPrimaryQuestId());
                Assert.AreEqual("Chapter 2", manager.GetQuestChapterLabel(manager.GetPrimaryQuestId()));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrimaryQuest_PrefersMainArcOverOptionalChapterQuest()
        {
            var go = new GameObject("quest-priority-optional");
            try
            {
                var manager = go.AddComponent<QuestManager>();

                var mainArc = ScriptableObject.CreateInstance<QuestData>();
                mainArc.Configure("quest_ch3_spire", "Skyglass Reckoning", "Main arc.",
                    new[] { new QuestObjective { objectiveId = "spire", description = "Climb", requiredCount = 1 } });
                var optional = ScriptableObject.CreateInstance<QuestData>();
                optional.Configure("quest_ch3_collector", "Delta Specimens", "Optional work.",
                    new[] { new QuestObjective { objectiveId = "catch", description = "Catch", requiredCount = 2 } });

                manager.RegisterQuestDefinition(mainArc);
                manager.RegisterQuestDefinition(optional);
                manager.StartQuest("quest_ch3_spire");
                manager.StartQuest("quest_ch3_collector");

                Assert.AreEqual("quest_ch3_spire", manager.GetPrimaryQuestId());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void PrimaryQuestObjectiveId_ReturnsNextIncompleteObjective()
        {
            var go = new GameObject("quest-primary-objective");
            try
            {
                var manager = go.AddComponent<QuestManager>();
                var quest = ScriptableObject.CreateInstance<QuestData>();
                quest.Configure("quest_ch2_archive", "Echoes In Stone", "Push into the archive.",
                    new[]
                    {
                        new QuestObjective { objectiveId = "talk_archivist", description = "Speak", requiredCount = 1 },
                        new QuestObjective { objectiveId = "visit_ruins", description = "Enter", requiredCount = 1 }
                    });

                manager.RegisterQuestDefinition(quest);
                manager.StartQuest("quest_ch2_archive");
                Assert.AreEqual("talk_archivist", manager.GetPrimaryQuestObjectiveId());

                manager.ReportObjectiveEvent("talk_archivist", 1);
                Assert.AreEqual("visit_ruins", manager.GetPrimaryQuestObjectiveId());
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
