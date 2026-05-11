using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    public class NpcLlmDisplayPipelineTests
    {
        [Test]
        public void ShapeForHud_ParsesValidCommandAndReturnsDisplayWithoutMarker()
        {
            var raw = "Come browse the shelf.\n[[command:open_shop|invites the player to trade]]";
            var hud = NpcLlmDisplayPipeline.ShapeForHud(raw, out var cmd);
            Assert.NotNull(cmd);
            Assert.AreEqual(NpcLlmCommandType.OpenShop, cmd.Type);
            Assert.IsFalse(hud.Contains("[[command:"));
            Assert.IsTrue(hud.Contains("browse"));
        }

        [Test]
        public void ShapeForHud_StripMalformedCommandMarkers()
        {
            var raw = "Hello there. [[command:";
            var hud = NpcLlmDisplayPipeline.ShapeForHud(raw);
            Assert.IsFalse(hud.Contains("[[command"));
        }
    }
}
