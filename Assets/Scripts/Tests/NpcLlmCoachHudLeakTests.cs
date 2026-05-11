using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    public class NpcLlmCoachHudLeakTests
    {
        [Test]
        public void TryDetectCoachHudLeak_FlagsYouSay()
        {
            Assert.True(NpcLlmResponseFilter.TryDetectCoachHudLeak(
                "The marsh wind cuts cold.\n\nYou say: what next?", out var reason));
            Assert.That(reason, Does.Contain("you say"));
        }

        [Test]
        public void TryDetectCoachHudLeak_FlagsPlayerJustSaid_Compacted()
        {
            Assert.True(NpcLlmResponseFilter.TryDetectCoachHudLeak(
                "Hmm. The player just said wolves worry them.", out _));
        }

        [Test]
        public void TryDetectCoachHudLeak_PassesCleanProse()
        {
            Assert.False(NpcLlmResponseFilter.TryDetectCoachHudLeak(
                "Steel your nerves—east trail is quieter after dawn.", out _));
        }

        [Test]
        public void TryDetectCoachHudLeak_AllowsInDialogueCommaYouSay()
        {
            var line =
                "\"Restless briar movements eastward, you say? That's not like our folk. What've you learned about it?\"";
            Assert.False(NpcLlmResponseFilter.TryDetectCoachHudLeak(line, out _));
        }
    }
}
