using System;
using LoreLegacyMonsters.Dialog.Llm;
using NUnit.Framework;

namespace LoreLegacyMonsters.Tests
{
    /// <summary>Regression checks for leaked prompt scaffolding in NPC dialogue (unit tests — no networking).</summary>
    public static class LlmNpcDialogRegressionAsserts
    {
        /// <summary>
        /// Verifies we never reintroduce the legacy "The player just said:" wrapper and that follow-up turns use a bare final user line.
        /// Note: context messages may quote phrases like "You say …" inside rules on purpose; those are not failures.
        /// </summary>
        public static void AssertUserPayloadsExcludeLegacyEchoWrapper(NpcLlmPromptContext ctx)
        {
            Assert.NotNull(ctx);
            var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);

            foreach (var m in msgs)
            {
                if (!string.Equals(m.role, "user", StringComparison.Ordinal))
                    continue;
                var compact = CompactForScan(m.content);
                Assert.IsFalse(
                    compact.IndexOf("theplayerjustsaid", StringComparison.Ordinal) >= 0,
                    $"User prompt must never contain \"the player just said\" (models echoed it into UI). Payload head: {Head(m.content, 120)}");
            }

            var hasSpeech = !string.IsNullOrWhiteSpace(ctx.PlayerMessage);
            if (hasSpeech)
            {
                var lastUser = PickLastUserContent(msgs);
                Assert.IsNotNull(lastUser, "Follow-up prompts must end with traveller user speech.");
                Assert.AreEqual(ctx.PlayerMessage.Trim(), lastUser.Trim(), "Bare user utterance must match exactly.");
                var priorUsers = CountUserMessagesExceptLast(msgs);
                Assert.GreaterOrEqual(priorUsers, 1, "Follow-up prompts should have a prior context user message.");
                Assert.AreEqual(3, msgs.Length, "Follow-up: system + context user + bare speech user.");
            }
            else
            {
                Assert.AreEqual(2, msgs.Length, "Greeting: system + one context user.");
            }
        }

        /// <summary>After <see cref="OpenAiCompatibleLlmClient.SanitizeReply"/> then <see cref="NpcLlmResponseFilter.Clean"/>.</summary>
        public static void AssertDisplayReplyExcludesCoachScaffolding(string cleanedForUi)
        {
            if (NpcLlmResponseFilter.TryDetectCoachHudLeak(cleanedForUi ?? string.Empty, out var failure))
                Assert.Fail($"NPC bubble scaffolding leak: {failure}");
        }

        static string CompactForScan(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var t = raw.Replace('\n', ' ').Replace('\r', ' ')
                .Replace("—", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
            return t.ToLowerInvariant();
        }

        static string Head(string raw, int n)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            raw = raw.Replace('\r', ' ').Replace('\n', ' ');
            return raw.Length <= n ? raw : raw.Substring(0, n) + "…";
        }

        static string PickLastUserContent(ChatMessageJson[] msgs)
        {
            if (msgs == null) return null;
            for (var i = msgs.Length - 1; i >= 0; i--)
            {
                if (string.Equals(msgs[i]?.role, "user", StringComparison.Ordinal))
                    return msgs[i].content ?? string.Empty;
            }

            return null;
        }

        static int CountUserMessagesExceptLast(ChatMessageJson[] msgs)
        {
            var c = 0;
            var lastUserIndex = -1;
            if (msgs == null) return 0;
            for (var i = 0; i < msgs.Length; i++)
            {
                if (string.Equals(msgs[i]?.role, "user", StringComparison.Ordinal))
                    lastUserIndex = i;
            }

            for (var i = 0; i < msgs.Length; i++)
            {
                if (i == lastUserIndex) continue;
                if (string.Equals(msgs[i]?.role, "user", StringComparison.Ordinal))
                    c++;
            }

            return c;
        }
    }
}
