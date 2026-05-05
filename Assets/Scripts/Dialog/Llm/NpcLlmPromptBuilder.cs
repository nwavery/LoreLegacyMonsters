using System.Text;
using LoreLegacyMonsters;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class NpcLlmPromptBuilder
    {
        public const string SafetyBlock =
            NpcLlmPromptRules.SafetyBlock;

        public const string DefaultGlobalRules =
            NpcLlmPromptRules.DefaultGlobalRules;

        public const string DefaultElderCharacter =
            "You are Mira, the town elder of Hollowfen—a cautious but kind leader. " +
            "You worry about wild monsters in the nearby forest and want new trainers to prove themselves before taking dangerous work. " +
            "You speak plainly, with warmth and a hint of old-fashioned formality.";

        public static ChatMessageJson[] BuildMessages(NpcLlmPromptContext ctx)
        {
            var sys = new StringBuilder();
            sys.AppendLine(SafetyBlock);
            sys.AppendLine();
            sys.AppendLine(DefaultGlobalRules);
            sys.AppendLine();
            sys.Append("Character: ").AppendLine(ctx.DisplayName);
            sys.Append("NPC id: ").AppendLine(ctx.NpcId);
            sys.Append("Role: ").AppendLine(ctx.RoleName);
            if (!string.IsNullOrWhiteSpace(ctx.IdentitySummary))
            {
                sys.AppendLine("Identity summary:");
                sys.AppendLine(ctx.IdentitySummary.Trim());
            }

            if (!string.IsNullOrWhiteSpace(ctx.CharacterInstructions))
                sys.AppendLine(ctx.CharacterInstructions.Trim());
            sys.AppendLine();
            sys.AppendLine("Optional command syntax:");
            sys.AppendLine("If useful, append one final line using exactly one of these tags (role-appropriate only):");
            AppendRoleCommandLines(sys, ctx.Role);
            sys.AppendLine("Only append a command when it clearly matches the NPC role and current context.");

            var user = new StringBuilder();
            user.AppendLine("Current game state (facts only, do not repeat verbatim as a list unless the player asked):");
            user.AppendLine(ctx.GameStateSummary ?? "(none)");
            user.AppendLine();
            user.Append("Weather: ").AppendLine(ctx.WeatherSummary ?? "unknown");
            user.Append("Primary quest context: ").AppendLine(ctx.QuestSummary ?? "none");
            user.Append("Party summary: ").AppendLine(ctx.PartySummary ?? "none");
            user.Append("Inventory highlights: ").AppendLine(ctx.InventorySummary ?? "none");
            user.Append("Persistent status & typical cures: ").AppendLine(ctx.StatusEffectsSummary ?? "none");
            user.Append("This NPC shop stock (if any): ").AppendLine(ctx.ShopStockSummary ?? "none");
            user.Append("Story branch state: ").AppendLine(ctx.StoryStateSummary ?? "none");
            user.Append("Remembered history with this player: ").AppendLine(ctx.NpcMemorySummary ?? "none");
            user.Append("Recent conversation: ").AppendLine(ctx.ConversationHistorySummary ?? "none");
            user.AppendLine();
            user.AppendLine("Speak your next line(s) to the player as this NPC.");
            if (!string.IsNullOrWhiteSpace(ctx.PlayerMessage))
            {
                user.AppendLine();
                user.Append("The player just said: ");
                user.AppendLine(ctx.PlayerMessage.Trim());
            }
            else
            {
                user.AppendLine();
                user.AppendLine("The player has initiated a conversation. Greet them naturally and offer a useful next topic.");
            }

            return new[]
            {
                new ChatMessageJson { role = "system", content = sys.ToString() },
                new ChatMessageJson { role = "user", content = user.ToString() }
            };
        }

        /// <summary>Exposed for unit tests.</summary>
        public static void AppendRoleCommandLines(StringBuilder sys, NpcRole role)
        {
            switch (role)
            {
                case NpcRole.Shopkeeper:
                    sys.AppendLine("[[command:offer_hint|short hint text]]");
                    sys.AppendLine("[[command:open_shop|invites the player to trade]]");
                    break;
                case NpcRole.Healer:
                    sys.AppendLine("[[command:offer_hint|short hint text]]");
                    sys.AppendLine("[[command:offer_heal|invites the player to recover]]");
                    sys.AppendLine(
                        "When the party has Burn, Poison, or Shock, name the matching cure from Shop stock (Cold Salve, Antidote, Shock Tonic). Do not invent items.");
                    break;
                case NpcRole.BossTrainer:
                    sys.AppendLine("[[command:offer_hint|short hint text]]");
                    sys.AppendLine("[[command:offer_battle|invites the player to battle]]");
                    break;
                default:
                    sys.AppendLine("[[command:offer_hint|short hint text]]");
                    sys.AppendLine("[[command:suggest_destination|where the player should go next]]");
                    break;
            }
        }
    }
}
