using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    public class NpcLlmDialogRegressionTests
    {
        [Test]
        public void FollowUp_NoLegacyEcho_AllRoles_Table()
        {
            foreach (NpcRole role in System.Enum.GetValues(typeof(NpcRole)))
            {
                var ctx = BaseContext(role);
                ctx.PlayerMessage = "I'm anxious about heading east—is the fog a real danger?";
                ctx.ConversationHistorySummary = "assistant: The Hollowfen gates stand open.";
                LlmNpcDialogRegressionAsserts.AssertUserPayloadsExcludeLegacyEchoWrapper(ctx);
            }
        }

        [Test]
        public void Greeting_NoLegacyEcho_Table()
        {
            foreach (NpcRole role in System.Enum.GetValues(typeof(NpcRole)))
            {
                var ctx = BaseContext(role);
                ctx.PlayerMessage = null;
                LlmNpcDialogRegressionAsserts.AssertUserPayloadsExcludeLegacyEchoWrapper(ctx);
            }
        }

        static NpcLlmPromptContext BaseContext(NpcRole role)
        {
            return new NpcLlmPromptContext
            {
                NpcId = "test_npc",
                DisplayName = "Tester",
                RoleName = role.ToString(),
                Role = role,
                CharacterInstructions = "Be brief.",
                IdentitySummary = "A test character.",
                PlayerMessage = null,
                GameStateSummary = "area: town",
                QuestSummary = "Recover the heirloom scroll.",
                InventorySummary = "empty",
                PartySummary = "none",
                WeatherSummary = "Clear",
                NpcMemorySummary = "none",
                ConversationHistorySummary = "none"
            };
        }
    }
}
