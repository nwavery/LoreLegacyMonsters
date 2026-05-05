using System;
using System.Collections.Generic;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Dialog;
using LoreLegacyMonsters.Shop;
using UnityEngine;

namespace LoreLegacyMonsters.World
{
    public sealed class NpcContentDefinition
    {
        public readonly string GameObjectName;
        public readonly string NpcId;
        public readonly string DisplayName;
        public readonly NpcRole Role;
        public readonly Vector2 FallbackPosition;
        public readonly bool UseLlmFlavor;
        public readonly string LlmPrompt;
        public readonly string IdentitySummary;
        public readonly string ShopKey;
        public readonly string[] SuggestedTopics;
        public readonly Func<QuestManager, DialogData> DialogBuilder;

        public NpcContentDefinition(string gameObjectName, string npcId, string displayName, NpcRole role,
            Vector2 fallbackPosition, bool useLlmFlavor, string llmPrompt, string identitySummary,
            string shopKey, Func<QuestManager, DialogData> dialogBuilder, params string[] suggestedTopics)
        {
            GameObjectName = gameObjectName;
            NpcId = npcId;
            DisplayName = displayName;
            Role = role;
            FallbackPosition = fallbackPosition;
            UseLlmFlavor = useLlmFlavor;
            LlmPrompt = llmPrompt;
            IdentitySummary = identitySummary;
            ShopKey = shopKey;
            DialogBuilder = dialogBuilder;
            SuggestedTopics = suggestedTopics ?? Array.Empty<string>();
        }
    }

    public static class NpcContentRegistry
    {
        const string GeneralShop = "general";
        const string HealerShop = "healer";

        static readonly NpcContentDefinition[] Definitions =
        {
            Def("Elder_Mira", NPCController.ElderMiraId, "Mira, Town Elder", NpcRole.Story, new Vector2(4f, -1f),
                "You lead the town of Hollowfen and guide new trainers with calm authority. Keep important guidance aligned with the provided quest facts.",
                "Town elder who knows Hollowfen's duties, worries, and local traditions.", null, null,
                "What should I do next?", "Tell me about Hollowfen.", "What do you think of my party?"),
            Def("Scout_Rin", NPCController.ScoutRinId, "Rin, Field Scout", NpcRole.Story, new Vector2(18f, 0f),
                "You are a practical scout who warns travelers about danger and tracks monster movement. Stay concise and field-focused.",
                "Sharp-eyed route scout who studies tracks, weather, and monster movement.", null, null,
                "What dangers are ahead?", "Any fresh tracks nearby?", "What kind of monsters live out here?"),
            Def("Merchant_Toma", NPCController.MerchantTomaId, "Toma, Merchant", NpcRole.Shopkeeper, new Vector2(1f, -1f),
                "You are a cheerful shopkeeper who talks up useful supplies and local gossip. You sell potions, capture charms, and condition cures (Cold Salve for burns, Antidote for poison, Shock Tonic for shock). Use the View Wares button or [[command:open_shop]] so the player can browse stock.",
                "Friendly general merchant who mixes practical item advice with town gossip.", GeneralShop, null,
                "What should I buy?", "Any gossip from the road?", "Show me what you're selling."),
            Def("Healer_Pia", NPCController.HealerPiaId, "Pia, Healer", NpcRole.Healer, new Vector2(-2f, -1f),
                "You are a gentle healer: you fully restore the party when they visit, and you sell Cold Salve, Antidote, and Shock Tonic for the road. Explain which cure matches burns, poison, and shock. Direct players to View Wares or [[command:open_shop]] to buy supplies.",
                "Kind healer who watches over both trainers and monsters with patient concern.", HealerShop, null,
                "How dangerous is the forest?", "Can you heal my team again?", "How do I keep monsters healthy?"),
            Def("Boss_Iona", NPCController.BossIonaId, "Iona, Briar Warden", NpcRole.BossTrainer, new Vector2(48f, 0f),
                "You are the stern warden of the forest grove and judge strength through battle. Never make the challenge feel trivial.",
                "Grove warden who values resolve, strength, and respect for wild territory.", null, null,
                "Why are you guarding this place?", "What do you respect in a trainer?", "Why are the monsters agitated?"),
            Def("Archivist_Sel", NPCController.ArchivistSelId, "Sel, Marsh Archivist", NpcRole.Story, new Vector2(58f, -1f),
                "You are a meticulous archivist who studies wardstones, flood records, and old monster lore. Speak like someone piecing together patterns from fragments.",
                "Careful historian stationed in Lantern Marsh to study the ruined archive and its waking beacon.", null, null,
                "What is this place?", "What woke the archive?", "What should I watch for in the marsh?"),
            Def("Rival_Corin", NPCController.RivalCorinId, "Corin, Ambitious Rival", NpcRole.BossTrainer, new Vector2(78f, -1f),
                "You are a talented but impatient rival trainer who wants credit, relics, and proof of strength. Sound confident, competitive, and a little reckless.",
                "Rising trainer who sees the Sunken Archive as a chance to outpace everyone in Hollowfen.", null, null,
                "Why are you here?", "What do you want from the archive?", "Ready to battle?"),
            Def("Warden_Neris", NPCController.WardenNerisId, "Neris, Delta Warden", NpcRole.Story, new Vector2(90f, -1f),
                "You are a canal warden who keeps refugees, ferries, and monster herds moving through flood country. Speak like a practical protector who sees storms before others do.",
                "Regional warden who keeps the flooded delta alive through discipline, maps, and stubborn courage.", null, null,
                "What happened out here?", "Where should I go next?", "How bad are the storms?"),
            Def("Mentor_Cael", NPCController.MentorCaelId, "Cael, Veteran Mentor", NpcRole.Story, new Vector2(106f, -1f),
                "You are a veteran trainer who teaches positioning, balance, and patience. Encourage growth without sounding sentimental.",
                "Battle mentor on Stormbreak Ridge who studies how teams survive long campaigns.", null, null,
                "How do I prepare for the spire?", "What makes a strong party?", "How should I train my monsters?"),
            Def("Storm_Tyrant", NPCController.StormTyrantId, "Varo, Storm Tyrant", NpcRole.BossTrainer, new Vector2(126f, -1f),
                "You command the Skyglass Spire and believe only decisive strength can master the relic storm there. Sound regal, severe, and certain.",
                "Spire tyrant who treats the relic storm as a throne and every challenger as a test of worth.", null, null,
                "Why hold the spire?", "What are you awakening here?", "Will you yield?"),
            Def("Collector_Veya", NPCController.CollectorVeyaId, "Veya, Delta Collector", NpcRole.Story, new Vector2(86f, -1f),
                "You are a curious collector of shells, scales, and marsh relics. You love rare creatures and unusual finds more than danger itself.",
                "Enthusiastic collector who trades gossip and requests tied to rare monster catches.", null, null,
                "What are you looking for?", "Seen anything rare?", "What lives in the delta?"),
            Def("Rumor_Iris", NPCController.RumorIrisId, "Iris, Rumor Keeper", NpcRole.Story, new Vector2(100f, -1f),
                "You are a sharp local storyteller who tracks who arrived, who vanished, and which legends are turning true. Stay grounded in observed rumor, not prophecy.",
                "Local rumor keeper who turns travel reports into cautionary tales and practical leads.", null, null,
                "What are people saying?", "Any side work nearby?", "What should I fear beyond the ridge?"),
            Def("Cartographer_Jessa", NPCController.CartographerJessaId, "Jessa Vale, Cartographer", NpcRole.Story, new Vector2(12f, 9f),
                "You are a confident cartographer opening the Wilderward map. Give practical directions and reward careful discovery.",
                "Mapmaker who believes the old route was only the edge of a much larger story.", null, PhaseTwoWorldContent.BuildDialog,
                "Where should I explore first?", "What is Stonewake?", "How does the new map work?"),
            Def("Quartermaster_Bram", NPCController.QuartermasterBramId, "Bram Kettle, Quartermaster", NpcRole.Shopkeeper, new Vector2(17f, 8.5f),
                "You are a warm but blunt quartermaster in Stonewake. You sell supplies and care about keeping travelers alive.",
                "Stonewake supplier who equips trainers for the northern roads.", GeneralShop, PhaseTwoWorldContent.BuildDialog,
                "What should I carry north?", "What changed in the Wilderward?", "Show me field supplies."),
            Def("Runner_Nia", NPCController.RunnerNiaId, "Nia Reed, Marsh Runner", NpcRole.Story, new Vector2(56f, 8f),
                "You are a quick marsh runner who knows boardwalk shortcuts and storm-safe routes.",
                "Messenger who connects Stonewake, the marsh basin, and Starfall Hollow.", null, PhaseTwoWorldContent.BuildDialog,
                "Which road is safest?", "What are the marsh lights doing?", "Any shortcuts?"),
            Def("Foreman_Orlo", NPCController.ForemanOrloId, "Orlo Flint, Quarry Foreman", NpcRole.Story, new Vector2(84f, 9f),
                "You are a quarry foreman dealing with tremors and aggressive stone monsters.",
                "Ironroot foreman who respects practical help more than heroic speeches.", null, PhaseTwoWorldContent.BuildDialog,
                "What shook the quarry?", "Which monsters are nesting here?", "How can I help?"),
            Def("Ethicist_Thren", NPCController.EthicistThrenId, "Thren, Monster Ethicist", NpcRole.Story, new Vector2(67f, 26f),
                "You study whether lore-binding helps or harms monsters. Ask thoughtful questions and avoid easy answers.",
                "Researcher tracking the moral cost of the archive network.", null, PhaseTwoWorldContent.BuildDialog,
                "What is lore-binding?", "Are captures changing monsters?", "What choice is coming?"),
            Def("Moonwell_Luma", NPCController.MoonwellLumaId, "Luma, Moonwell Keeper", NpcRole.Story, new Vector2(39f, 23f),
                "You keep a moonlit sanctuary and teach trainers how bonds affect monster evolution.",
                "Sanctuary keeper who reads monster trust through old moonwell rites.", null, PhaseTwoWorldContent.BuildDialog,
                "What is the Moonwell?", "How do bonds change monsters?", "What is disturbing this grove?"),
            Def("Sable_Rival", NPCController.SableRivalId, "Sable, Wandering Rival", NpcRole.BossTrainer, new Vector2(105f, 8.5f),
                "You are a roaming rival testing whether the player deserves the Wilderward's secrets.",
                "Competitive trainer who turns crossroads into trials.", null, PhaseTwoWorldContent.BuildDialog,
                "Why challenge me here?", "What did you find north?", "Ready for a rematch?"),
        };

        static readonly Dictionary<string, NpcContentDefinition> ById = BuildLookup();

        public static IReadOnlyList<NpcContentDefinition> All => Definitions;

        public static bool TryGet(string npcId, out NpcContentDefinition definition)
        {
            definition = null;
            return !string.IsNullOrEmpty(npcId) && ById.TryGetValue(npcId, out definition);
        }

        public static Vector3 Anchor(string npcId, Vector2 fallback)
        {
            foreach (var region in WorldMapLayout.All)
            {
                var anchors = region.NpcAnchors;
                for (var i = 0; i < anchors.Length; i++)
                    if (anchors[i].NpcId == npcId)
                        return new Vector3(anchors[i].Position.x, anchors[i].Position.y, 0f);
            }

            return new Vector3(fallback.x, fallback.y, 0f);
        }

        public static ShopData ResolveShop(NpcContentDefinition definition, ShopData general, ShopData healer)
        {
            if (definition == null) return null;
            return definition.ShopKey switch
            {
                GeneralShop => general,
                HealerShop => healer,
                _ => null
            };
        }

        static NpcContentDefinition Def(string goName, string npcId, string displayName, NpcRole role, Vector2 fallback,
            string prompt, string identity, string shopKey, Func<string, QuestManager, DialogData> phaseTwoDialogBuilder,
            params string[] topics)
        {
            Func<QuestManager, DialogData> builder = phaseTwoDialogBuilder == null ? null : quests => phaseTwoDialogBuilder(npcId, quests);
            return new NpcContentDefinition(goName, npcId, displayName, role, fallback, true, prompt, identity, shopKey, builder, topics);
        }

        static Dictionary<string, NpcContentDefinition> BuildLookup()
        {
            var lookup = new Dictionary<string, NpcContentDefinition>();
            for (var i = 0; i < Definitions.Length; i++)
                lookup[Definitions[i].NpcId] = Definitions[i];
            return lookup;
        }
    }
}
