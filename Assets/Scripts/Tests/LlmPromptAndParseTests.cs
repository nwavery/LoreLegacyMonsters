using NUnit.Framework;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog.Llm;

namespace LoreLegacyMonsters.Tests
{
    public class LlmPromptAndParseTests
    {
        [Test]
        public void NpcLlmPromptBuilder_ProducesSystemAndUserMessages()
        {
            var ctx = new NpcLlmPromptContext
            {
                NpcId = "test_npc",
                DisplayName = "Test NPC",
                RoleName = "Shopkeeper",
                CharacterInstructions = "You are a test.",
                IdentitySummary = "Friendly local merchant.",
                GameStateSummary = "area_id: forest; gold: 10",
                QuestSummary = "Talk to the scout on the route.",
                InventorySummary = "Potion x2, Capture Charm x1",
                PartySummary = "Emberfox Lv5",
                WeatherSummary = "Rainy",
                NpcMemorySummary = "relationship: familiar; memory: talked about supplies",
                ConversationHistorySummary = "assistant: Welcome back.",
                PlayerMessage = null
            };

            var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);
            Assert.AreEqual(2, msgs.Length);
            Assert.AreEqual("system", msgs[0].role);
            Assert.AreEqual("user", msgs[1].role);
            Assert.IsTrue(msgs[0].content.Contains("non-player character"));
            Assert.IsTrue(msgs[0].content.Contains("Test NPC"));
            Assert.IsTrue(msgs[0].content.Contains("Role: Shopkeeper"));
            Assert.IsTrue(msgs[0].content.Contains("command"));
            Assert.IsTrue(msgs[1].content.Contains("forest"));
            Assert.IsTrue(msgs[1].content.Contains("10"));
            Assert.IsTrue(msgs[1].content.Contains("Rainy"));
            Assert.IsTrue(msgs[1].content.Contains("Capture Charm"));
            Assert.IsTrue(msgs[1].content.Contains("Welcome back"));
        }

        [Test]
        public void NpcLlmPromptBuilder_WithPlayerSpeech_AddsBareThirdUserMessage()
        {
            var ctx = new NpcLlmPromptContext
            {
                NpcId = "scout_rin",
                DisplayName = "Rin",
                Role = NpcRole.Story,
                RoleName = "Story",
                CharacterInstructions = "You scout the route.",
                IdentitySummary = "Field scout.",
                GameStateSummary = "area_id: route",
                QuestSummary = "Head east.",
                InventorySummary = "Lantern x1",
                PartySummary = "none",
                WeatherSummary = "Clear",
                NpcMemorySummary = "none",
                ConversationHistorySummary = "assistant: Stay safe.",
                PlayerMessage = "Why should I?"
            };

            var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);
            Assert.AreEqual(3, msgs.Length);
            Assert.AreEqual("system", msgs[0].role);
            Assert.AreEqual("user", msgs[1].role);
            Assert.AreEqual("user", msgs[2].role);
            Assert.IsFalse(msgs[1].content.Contains("The player just said"), "Echo-prone preamble must stay out of prompts.");
            Assert.AreEqual("Why should I?", msgs[2].content);
        }

        [Test]
        public void TryExtractAssistantContent_ParsesOllamaStyleJson()
        {
            const string json =
                "{\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"Hello there.\"}}]}";
            Assert.IsTrue(OpenAiCompatibleLlmClient.TryExtractAssistantContent(json, out var c));
            Assert.AreEqual("Hello there.", c);
        }

        [Test]
        public void SanitizeReply_TrimsAndStripsControlChars()
        {
            var s = OpenAiCompatibleLlmClient.SanitizeReply("  hi\u0001\nthere  ");
            Assert.AreEqual("hi\nthere", s);
        }

        [Test]
        public void NpcLlmCommandParser_StripsAndParsesWhitelistedCommand()
        {
            const string raw = "Take a lantern before you leave.\n[[command:offer_hint|Bring extra supplies for the route.]]";

            Assert.IsTrue(NpcLlmCommandParser.TryParseAndStrip(raw, out var display, out var command));
            Assert.AreEqual("Take a lantern before you leave.", display);
            Assert.IsNotNull(command);
            Assert.AreEqual(NpcLlmCommandType.OfferHint, command.Type);
            Assert.AreEqual("Bring extra supplies for the route.", command.Payload);
        }
    }
}
