using System;
using System.Collections;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>Minimal reachability probe for OpenAI-compatible chat completions (Ollama, etc.).</summary>
    public static class NpcLlmHealthCheck
    {
        /// <summary>Sends a tiny completion request; <paramref name="done"/> receives (success, message).</summary>
        public static IEnumerator Probe(NpcLlmSettings settings, Action<bool, string> done) =>
            Probe(settings, Mathf.Clamp(settings != null ? settings.RequestTimeoutSeconds : 90, 5, 120), done);

        /// <inheritdoc cref="Probe(NpcLlmSettings,Action{bool,string})"/>
        /// <param name="requestTimeoutSeconds">HTTP timeout per attempt (e.g. short values during boot retries).</param>
        public static IEnumerator Probe(NpcLlmSettings settings, int requestTimeoutSeconds,
            Action<bool, string> done)
        {
            var s = settings != null ? settings : NpcLlmSettings.CreateRuntimeDefaults();
            var messages = new[]
            {
                new ChatMessageJson { role = "user", content = "ping" }
            };
            var json = OpenAiCompatibleLlmClient.BuildRequestJson(s.Model, messages, 0f, 8, false);
            var timeout = Mathf.Clamp(requestTimeoutSeconds, 2, 300);
            yield return OpenAiCompatibleLlmClient.SendChatCompletion(s.CompletionsUrl, json, timeout,
                (ok, msg) => done?.Invoke(ok, msg));
        }
    }
}
