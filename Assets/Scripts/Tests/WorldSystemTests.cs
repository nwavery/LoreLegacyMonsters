using NUnit.Framework;
using LoreLegacyMonsters.World;

namespace LoreLegacyMonsters.Tests
{
    public class WorldSystemTests
    {
        [Test]
        public void SampleAreas_Town_IsDefined()
        {
            Assert.AreEqual("town", SampleAreas.Town);
        }
    }
}
