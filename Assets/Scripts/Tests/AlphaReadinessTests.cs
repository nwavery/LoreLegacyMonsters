using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters.Tests
{
    public class AlphaReadinessTests
    {
        [Test]
        public void AlphaBuildInfoRecord_JsonRoundTrip()
        {
            var src = new AlphaBuildInfoRecord
            {
                version = "0.2.0-alpha",
                builtAtUtc = "2026-03-24T12:00:00Z",
                gitCommitShort = "abc1234",
                unityVersion = "6000.0.41f1"
            };
            var json = JsonUtility.ToJson(src);
            var back = JsonUtility.FromJson<AlphaBuildInfoRecord>(json);
            Assert.AreEqual(src.version, back.version);
            Assert.AreEqual(src.builtAtUtc, back.builtAtUtc);
            Assert.AreEqual(src.gitCommitShort, back.gitCommitShort);
            Assert.AreEqual(src.unityVersion, back.unityVersion);
        }

        [Test]
        public void AlphaDiagnostics_IncludesProductAndPaths()
        {
            var block = AlphaDiagnostics.FormatTesterDiagnosticsBlock();
            StringAssert.Contains("Product:", block);
            StringAssert.Contains("Player.log", block);
            StringAssert.Contains("Saves", block);
        }
    }
}
