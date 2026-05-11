using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>Walls and timeouts for overworld boot-time LLM health checks.</summary>
    public static class LlmBootProbePolicy
    {
        public const float NonBundledDeadlineSeconds = 45f;
        public const float BundledDeadlineSeconds = 300f;
        public const float BundledTcpWaitCapSeconds = 90f;

        public static float DeadlineSeconds(bool bundledRuntime) =>
            bundledRuntime ? BundledDeadlineSeconds : NonBundledDeadlineSeconds;

        /// <summary>HTTP timeout per <see cref="NpcLlmHealthCheck.Probe"/> attempt during boot.</summary>
        public static int ProbeHttpTimeoutSeconds(bool bundledRuntime, int attemptIndex)
        {
            attemptIndex = Mathf.Max(0, attemptIndex);
            if (!bundledRuntime)
                return 12;

            return attemptIndex switch
            {
                0 => 45,
                1 => 75,
                _ => 120
            };
        }
    }
}
