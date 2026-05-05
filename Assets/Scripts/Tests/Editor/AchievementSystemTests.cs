using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Achievements;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests.Editor
{
    public class AchievementSystemTests
    {
        [Test]
        public void Unlock_AddsId()
        {
            var go = new GameObject("ach");
            var sys = go.AddComponent<AchievementSystem>();
            Assert.IsTrue(sys.Unlock(SampleAchievements.FirstSteps));
            Assert.Contains(SampleAchievements.FirstSteps, sys.GetUnlockedIds());
            Object.DestroyImmediate(go);
        }
    }
}
