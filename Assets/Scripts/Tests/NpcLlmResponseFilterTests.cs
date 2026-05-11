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
        public void Clean_StripsEchoPlayerSaidLines()
        {
            var raw = "The player just said: made up garbage.\nI hear hesitation in your voice—the road east is risky.";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.ToLowerInvariant().Contains("the player"));
            Assert.IsFalse(s.Contains("made up garbage"));
            Assert.IsTrue(s.Contains("hesitation"));
        }

        [Test]
        public void Clean_StripsYouSayPrefixFromLine()
        {
            var raw = "You say:\nFog thickens east of Hollowfen.";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.ToLowerInvariant().Contains("you say"));
            Assert.IsTrue(s.Contains("Fog thickens"));
        }

        [Test]
        public void SanitizeThenClean_RemovesCoachBlockSomeModelsEcho()
        {
            var raw =
                "The player just said: I'm anxious about travelling east—is the fog dangerous?\nYou say:\nFog thickens after the ridge; stay on the flagged path.\nStay sharp.";
            var s = NpcLlmResponseFilter.Clean(OpenAiCompatibleLlmClient.SanitizeReply(raw));
            Assert.IsFalse(s.ToLowerInvariant().Contains("the player"));
            Assert.IsFalse(s.ToLowerInvariant().Contains("you say"));
            Assert.IsTrue(s.Contains("Fog thickens"));
            Assert.IsTrue(s.Contains("Stay sharp"));
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
        public void Clean_StripsLineWithBillionsOfParameters()
        {
            var raw = "My weights span billions of parameters.\nCarry Antidote for the marsh.";
            var s = NpcLlmResponseFilter.Clean(raw);
            Assert.IsFalse(s.Contains("billions"));
            Assert.IsTrue(s.Contains("Antidote"));
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
