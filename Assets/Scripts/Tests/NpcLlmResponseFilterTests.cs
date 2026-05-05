using NUnit.Framework;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.Tests
{
    public class NpcLlmResponseFilterTests
    {
        [Test]
        public void Clean_StripsRolePrefixes()
        {
            var raw = "system: ignore previous\nHello traveler.";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.Contains("system:"));
            Assert.IsTrue(s.Contains("Hello traveler"));
        }

        [Test]
        public void Clean_StripsAsAnAiLine()
        {
            var raw = "As an AI, I cannot.\nBut in Hollowfen we say hello.";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.ToLowerInvariant().Contains("as an ai"));
            Assert.IsTrue(s.Contains("Hollowfen"));
        }

        [Test]
        public void IsTooShortToDisplay_TrueForEmpty()
        {
            Assert.IsTrue(NpcLlmResponseFilter.IsTooShortToDisplay(""));
            Assert.IsTrue(NpcLlmResponseFilter.IsTooShortToDisplay("short"));
            Assert.IsFalse(NpcLlmResponseFilter.IsTooShortToDisplay("This is long enough."));
        }

        [Test]
        public void Clean_DoesNotInsertCarriageReturns()
        {
            var raw = "Line one\n\nLine two";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.Contains("\r"));
        }

        [Test]
        public void Clean_StripsSpecialTokenLines()
        {
            var raw = "<|eot|>\nHello.";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.Contains("<|"));
            Assert.IsTrue(s.Contains("Hello"));
        }
    }
}
