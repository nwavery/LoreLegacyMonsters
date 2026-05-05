using NUnit.Framework;
using LoreLegacyMonsters.Quests;

namespace LoreLegacyMonsters.Tests
{
    public class MainStoryQuestTests
    {
        [Test]
        public void MainStoryQuests_Intro_IsStable()
        {
            Assert.AreEqual("main_intro", MainStoryQuests.Intro);
        }
    }
}
