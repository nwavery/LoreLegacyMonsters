using System;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>
    /// Resolves OpenAI-compatible chat URL/model/timeout from the same env vars as optional edit-mode integration tests:
    /// <c>NPC_LLM_TEST_CHAT_COMPLETIONS_URL</c>, <c>NPC_LLM_TEST_BASE_URL</c>, <c>OLLAMA_HOST</c>, <c>NPC_LLM_TEST_MODEL</c>.
    /// </summary>
    public static class NpcLlmDevEndpointResolver
    {
        public static void Resolve(out string completionsUrl, out string model, out int timeoutSeconds, bool logResolved = false)
        {
            completionsUrl = ResolveChatCompletionsUrl();
            model = ResolveModel(completionsUrl);
            var cfg = NpcLlmSettings.LoadFromResources();
            timeoutSeconds = Mathf.Clamp(cfg != null ? cfg.RequestTimeoutSeconds : 120, 30, 300);

            if (logResolved)
                Debug.Log($"[NpcLlmDev] completions={completionsUrl} model={model} timeout={timeoutSeconds}s");
        }

        public static string ResolveChatCompletionsUrl()
        {
            var full = Environment.GetEnvironmentVariable("NPC_LLM_TEST_CHAT_COMPLETIONS_URL");
            if (!string.IsNullOrWhiteSpace(full))
                return full.Trim();

            var root = Environment.GetEnvironmentVariable("NPC_LLM_TEST_BASE_URL");
            if (!string.IsNullOrWhiteSpace(root))
                return $"{root.Trim().TrimEnd('/')}/chat/completions";

            var ollamaHost = Environment.GetEnvironmentVariable("OLLAMA_HOST");
            if (!string.IsNullOrWhiteSpace(ollamaHost))
            {
                var h = ollamaHost.Trim();
                if (h.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    h.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return $"{h.TrimEnd('/')}/chat/completions";

                return $"http://{h}/v1/chat/completions";
            }

            return "http://127.0.0.1:11434/v1/chat/completions";
        }

        public static string ResolveModel(string completionsUrl)
        {
            var configured = Environment.GetEnvironmentVariable("NPC_LLM_TEST_MODEL");
            if (!string.IsNullOrWhiteSpace(configured))
                return configured.Trim();

            try
            {
                var uri = new Uri(completionsUrl);
                if (uri.Port == LlmRuntimeSupervisor.DefaultBundledPort)
                    return $"{BundledOllamaModelProvisioner.BundledModelTag}:latest";
            }
            catch (UriFormatException)
            {
            }

            var settings = NpcLlmSettings.LoadFromResources();
            if (settings != null && !string.IsNullOrWhiteSpace(settings.Model))
                return settings.Model.Trim();

            return $"{BundledOllamaModelProvisioner.BundledModelTag}:latest";
        }
    }
}
