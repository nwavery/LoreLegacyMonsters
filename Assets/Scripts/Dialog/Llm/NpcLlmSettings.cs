using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    [CreateAssetMenu(menuName = "LLM/NPC LLM Settings", fileName = "NpcLlmSettings")]
    public class NpcLlmSettings : ScriptableObject
    {
        [Tooltip("OpenAI-compatible root, e.g. http://127.0.0.1:11434/v1 (Ollama)")]
        [SerializeField] string baseUrl = "http://127.0.0.1:11434/v1";

        [SerializeField] string model = "llama3.2:latest";
        [SerializeField] [Range(0f, 2f)] float temperature = 0.6f;
        [SerializeField] int maxTokens = 256;
        [SerializeField] int requestTimeoutSeconds = 90;

        public string BaseUrl => baseUrl;
        public string Model => model;
        public float Temperature => temperature;
        public int MaxTokens => maxTokens;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;

        public string CompletionsUrl
        {
            get
            {
                var b = string.IsNullOrWhiteSpace(baseUrl) ? "http://127.0.0.1:11434/v1" : baseUrl.Trim();
                return b.TrimEnd('/') + "/chat/completions";
            }
        }

        public static NpcLlmSettings CreateRuntimeDefaults()
        {
            var s = CreateInstance<NpcLlmSettings>();
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        /// <summary>Resources asset at Llm/NpcLlmSettings, or runtime defaults if missing.</summary>
        public static NpcLlmSettings LoadFromResources()
        {
            var asset = Resources.Load<NpcLlmSettings>("Llm/NpcLlmSettings");
            return asset != null ? asset : CreateRuntimeDefaults();
        }

        /// <summary>Copy (never mutate the project asset) and overlay <see cref="LlmGlobalPreferences"/>.</summary>
        public static NpcLlmSettings ResolveForDriver(NpcLlmSettings serializedReference)
        {
            var src = serializedReference != null ? serializedReference : LoadFromResources();
            var inst = src != null ? Instantiate(src) : CreateRuntimeDefaults();
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.ApplyPlayerPreferenceOverlay();
            return inst;
        }

        void ApplyPlayerPreferenceOverlay()
        {
            var bundled = LlmRuntimeSupervisor.IsBundledRuntimeEnabled();
            if (bundled)
            {
                baseUrl = LlmRuntimeSupervisor.BundledBaseUrl;
                LlmRuntimeSupervisor.EnsureStarted();
            }

            if (PlayerPrefs.HasKey(LlmGlobalPreferences.KeyBaseUrl))
            {
                var prefUrl = PlayerPrefs.GetString(LlmGlobalPreferences.KeyBaseUrl);
                if (bundled && MatchesLegacyDesktopOllamaRoot(prefUrl))
                {
                    PlayerPrefs.DeleteKey(LlmGlobalPreferences.KeyBaseUrl);
                    PlayerPrefs.Save();
                }
                else
                    baseUrl = prefUrl.Trim();
            }

            if (PlayerPrefs.HasKey(LlmGlobalPreferences.KeyModel))
            {
                var prefModel = PlayerPrefs.GetString(LlmGlobalPreferences.KeyModel);
                if (bundled && BundledOllamaModelProvisioner.LooksLikeLegacyDesktopDefaultModel(prefModel))
                {
                    PlayerPrefs.DeleteKey(LlmGlobalPreferences.KeyModel);
                    PlayerPrefs.Save();
                    model = BundledOllamaModelProvisioner.BundledModelTag;
                }
                else
                    model = prefModel.Trim();
            }
            else if (bundled)
                model = BundledOllamaModelProvisioner.BundledModelTag;
        }

        static bool MatchesLegacyDesktopOllamaRoot(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            var t = url.Trim().TrimEnd('/').ToLowerInvariant();
            return t == "http://127.0.0.1:11434/v1" || t == "http://localhost:11434/v1";
        }
    }
}
