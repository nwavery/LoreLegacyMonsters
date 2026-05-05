using LoreLegacyMonsters.Shop;
using UnityEngine;

namespace LoreLegacyMonsters
{
    public enum NpcRole
    {
        Story,
        Ambient,
        Shopkeeper,
        Healer,
        BossTrainer
    }

    public class NPCController : MonoBehaviour
    {
        public const string ElderMiraId = "elder_mira";
        public const string ScoutRinId = "scout_rin";
        public const string MerchantTomaId = "merchant_toma";
        public const string HealerPiaId = "healer_pia";
        public const string BossIonaId = "boss_iona";
        public const string ArchivistSelId = "archivist_sel";
        public const string RivalCorinId = "rival_corin";
        public const string WardenNerisId = "warden_neris";
        public const string MentorCaelId = "mentor_cael";
        public const string StormTyrantId = "storm_tyrant";
        public const string CollectorVeyaId = "collector_veya";
        public const string RumorIrisId = "rumor_iris";
        public const string CartographerJessaId = "cartographer_jessa";
        public const string QuartermasterBramId = "quartermaster_bram";
        public const string RunnerNiaId = "runner_nia";
        public const string ForemanOrloId = "foreman_orlo";
        public const string EthicistThrenId = "ethicist_thren";
        public const string MoonwellLumaId = "moonwell_luma";
        public const string SableRivalId = "sable_rival";

        [SerializeField] string npcId;
        [SerializeField] string displayName = "NPC";
        [SerializeField] NpcRole role = NpcRole.Ambient;
        [SerializeField] float interactionRadius = 2.25f;
        [SerializeField] bool useLlmFlavor;
        [TextArea(3, 10)] [SerializeField] string llmCharacterPrompt;
        [SerializeField] string llmIdentitySummary;
        [SerializeField] string[] llmSuggestedTopics = System.Array.Empty<string>();
        [SerializeField] bool preferScriptedOpening = true;
        [SerializeField] [Range(-1f, 2f)] float llmTemperatureOverride = -1f;
        [SerializeField] int llmMaxTokensOverride;
        [SerializeField] Dialog.DialogData dialog;
        [SerializeField] ShopData shop;

        public string NpcId => npcId;
        public string DisplayName => displayName;
        public NpcRole Role => role;
        public float InteractionRadius => interactionRadius;
        public bool UseLlmFlavor => useLlmFlavor;
        public string LlmCharacterPrompt => llmCharacterPrompt;
        public string LlmIdentitySummary => string.IsNullOrWhiteSpace(llmIdentitySummary) ? displayName : llmIdentitySummary;
        public string[] LlmSuggestedTopics => llmSuggestedTopics ?? System.Array.Empty<string>();
        public bool PreferScriptedOpening => preferScriptedOpening;
        /// <summary>Negative = use global LLM temperature from settings.</summary>
        public float LlmTemperatureOverride => llmTemperatureOverride;
        /// <summary>0 or negative = use global max tokens from settings.</summary>
        public int LlmMaxTokensOverride => llmMaxTokensOverride;
        public Dialog.DialogData Dialog => dialog;
        public ShopData Shop => shop;

        public void Configure(string id, string title, NpcRole npcRole, float radius = 2.25f,
            bool llmFlavor = false, string llmPrompt = null, string identitySummary = null,
            bool scriptedOpening = true, params string[] suggestedTopics)
        {
            npcId = id;
            displayName = title;
            role = npcRole;
            interactionRadius = radius;
            useLlmFlavor = llmFlavor;
            llmCharacterPrompt = llmPrompt ?? string.Empty;
            llmIdentitySummary = string.IsNullOrWhiteSpace(identitySummary) ? title : identitySummary;
            preferScriptedOpening = scriptedOpening;
            llmSuggestedTopics = suggestedTopics ?? System.Array.Empty<string>();
        }

        public void BindRuntimeDialog(Dialog.DialogData data) => dialog = data;

        public void BindShop(ShopData shopData) => shop = shopData;
    }
}
