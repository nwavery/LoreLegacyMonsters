namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>Last boot-time or manual LLM reachability probe (Ollama / OpenAI-compatible).</summary>
    public static class LlmRuntimeStatus
    {
        public static bool HasProbeResult { get; private set; }
        public static bool LastProbeOk { get; private set; }
        public static string LastProbeMessage { get; private set; }
        /// <summary>True while overworld boot is retrying reachability (avoid flashing "offline" during Ollama warm-up).</summary>
        public static bool BootProbeInProgress { get; private set; }

        public static void SetBootProbeInProgress(bool value) => BootProbeInProgress = value;

        public static void SetProbeResult(bool ok, string message)
        {
            HasProbeResult = true;
            LastProbeOk = ok;
            LastProbeMessage = message ?? string.Empty;
            BootProbeInProgress = false;
        }

        public static void Clear()
        {
            HasProbeResult = false;
            LastProbeOk = false;
            LastProbeMessage = string.Empty;
            BootProbeInProgress = false;
        }
    }
}
