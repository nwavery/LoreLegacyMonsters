using NUnit.Framework;
using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog;
using Object = UnityEngine.Object;

namespace LoreLegacyMonsters.Tests
{
    public class DialogSystemTests
    {
        [Test]
        public void DialogSystem_Begin_NullSafe()
        {
            var go = new GameObject("d");
            var sys = go.AddComponent<DialogSystem>();
            sys.Begin(null);
            Assert.IsFalse(sys.TryGetLine(out _));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DialogData_Configure_AndAdvance_Ends()
        {
            var d = ScriptableObject.CreateInstance<DialogData>();
            d.Configure("t", new[] { new DialogEntry { speaker = "A", line = "Hi" } });
            var go = new GameObject("d");
            var sys = go.AddComponent<DialogSystem>();
            sys.Begin(d);
            Assert.IsTrue(sys.TryGetLine(out var e));
            Assert.AreEqual("A", e.speaker);
            sys.Advance();
            Assert.IsFalse(sys.TryGetLine(out _));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(d);
        }

        [Test]
        public void DefaultGameContent_ElderDialog_HasLines()
        {
            var d = DefaultGameContent.CreateElderGreetingDialog();
            Assert.IsNotNull(d.Entries);
            Assert.GreaterOrEqual(d.Entries.Length, 1);
            Object.DestroyImmediate(d);
        }
    }
}
