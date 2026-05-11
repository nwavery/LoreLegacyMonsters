using System;
using System.Collections;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace LoreLegacyMonsters.Tests
{
    /// <summary>
    /// Optional live scenarios against a local endpoint when <c>RUN_NPC_LLM_INTEGRATION=1</c>.
    /// Endpoint resolution matches <see cref="NpcLlmDevEndpointResolver"/>.
    /// </summary>
    [Category("NpcLlmIntegration")]
    public class NpcLlmLiveOllamaScenarioTests
    {
        const string EnableEnv = "RUN_NPC_LLM_INTEGRATION";

        static bool IntegrationEnabled =>
            string.Equals(Environment.GetEnvironmentVariable(EnableEnv), "1", StringComparison.Ordinal);

        [UnityTest]
        public IEnumerator Live_FollowUp_AfterPipeline_NoCoachScaffolding()
        {
            if (!IntegrationEnabled)
            {
                Assert.Ignore(
                    $"Set {EnableEnv}=1 and run a local OpenAI-compatible server (e.g. Ollama). Optional: NPC_LLM_TEST_CHAT_COMPLETIONS_URL, NPC_LLM_TEST_MODEL.");
                yield break;
            }

            ResolveEndpoints(out var url, out var model, out var timeoutSeconds);

            var ctx = BaseContext(NpcRole.Story);
            ctx.PlayerMessage = "I'm nervous about east road—is the fog truly dangerous?";
            ctx.ConversationHistorySummary = "assistant: Hollowfen still stands—careful steps matter.";

            LlmNpcDialogRegressionAsserts.AssertUserPayloadsExcludeLegacyEchoWrapper(ctx);

            var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);
            var body = OpenAiCompatibleLlmClient.BuildRequestJson(model, msgs, 0.35f, 320, false);

            var ok = false;
            string assistant = null;
            string err = null;
            yield return OpenAiCompatibleLlmClient.SendChatCompletion(url, body, timeoutSeconds, (success, text) =>
            {
                ok = success;
                if (success) assistant = text;
                else err = text;
            });

            Assert.IsTrue(ok, err ?? "LLM request failed");
            var cleaned = NpcLlmResponseFilter.Clean(OpenAiCompatibleLlmClient.SanitizeReply(assistant));
            LlmNpcDialogRegressionAsserts.AssertDisplayReplyExcludesCoachScaffolding(cleaned);
            Assert.GreaterOrEqual(cleaned.Trim().Length, 8,
                "Model returned an unexpectedly short reply; check server/model.");
        }

        [UnityTest]
        public IEnumerator Live_Greeting_AfterPipeline_NoCoachScaffolding()
        {
            if (!IntegrationEnabled)
            {
                Assert.Ignore($"Set {EnableEnv}=1 to run live NPC LLM scenarios.");
                yield break;
            }

            ResolveEndpoints(out var url, out var model, out var timeoutSeconds);

            var ctx = BaseContext(NpcRole.Ambient);
            ctx.PlayerMessage = null;
            ctx.ConversationHistorySummary = "none";

            LlmNpcDialogRegressionAsserts.AssertUserPayloadsExcludeLegacyEchoWrapper(ctx);

            var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);
            var body = OpenAiCompatibleLlmClient.BuildRequestJson(model, msgs, 0.45f, 256, false);

            var ok = false;
            string assistant = null;
            string err = null;
            yield return OpenAiCompatibleLlmClient.SendChatCompletion(url, body, timeoutSeconds, (success, text) =>
            {
                ok = success;
                if (success) assistant = text;
                else err = text;
            });

            Assert.IsTrue(ok, err ?? "LLM request failed");
            var cleaned = NpcLlmResponseFilter.Clean(OpenAiCompatibleLlmClient.SanitizeReply(assistant));
            LlmNpcDialogRegressionAsserts.AssertDisplayReplyExcludesCoachScaffolding(cleaned);
            Assert.GreaterOrEqual(cleaned.Trim().Length, 8, "Greeting reply too short.");
        }

        static void ResolveEndpoints(out string completionsUrl, out string model, out int timeoutSeconds) =>
            NpcLlmDevEndpointResolver.Resolve(out completionsUrl, out model, out timeoutSeconds, logResolved: true);

        static NpcLlmPromptContext BaseContext(NpcRole role)
        {
            return new NpcLlmPromptContext
            {
                NpcId = "live_test_npc",
                DisplayName = "Rin Hollowfen scout",
                RoleName = role.ToString(),
                Role = role,
                CharacterInstructions = "Stay in character as a weary scout who knows the eastern trails.",
                IdentitySummary = "Practical frontier guide.",
                PlayerMessage = null,
                GameStateSummary = "area: outskirts of Hollowfen; eastern fog rumored",
                QuestSummary = "Find safe passage east.",
                InventorySummary = "basic supplies",
                PartySummary = "one companion ready",
                WeatherSummary = "misty dawn",
                NpcMemorySummary = "once warned the traveller about marsh lights",
                ConversationHistorySummary = "none",
                StoryStateSummary = "chapter:test",
                StatusEffectsSummary = "none notable",
                ShopStockSummary = "no shop",
                PlayerGearSummary = "travel cloak",
                PlayerVibeTags = "Cautious"
            };
        }
    }
}
