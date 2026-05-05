using System;
using System.Collections;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>Minimal reachability probe for OpenAI-compatible chat completions (Ollama, etc.).</summary>
    public static class NpcLlmHealthCheck
    {
        /// <summary>Sends a tiny completion request; <paramref name="done"/> receives (success, message).</summary>
        public static IEnumerator Probe(NpcLlmSettings settings, Action<bool, string> done)
        {
            var s = settings != null ? settings : NpcLlmSettings.CreateRuntimeDefaults();
            var messages = new[]
            {
                new ChatMessageJson { role = "user", content = "ping" }
            };
            var json = OpenAiCompatibleLlmClient.BuildRequestJson(s.Model, messages, 0f, 8, false);
            var timeout = Mathf.Clamp(s.RequestTimeoutSeconds, 5, 120);
            yield return OpenAiCompatibleLlmClient.SendChatCompletion(s.CompletionsUrl, json, timeout,
                (ok, msg) => done?.Invoke(ok, msg));
        }
    }
}
