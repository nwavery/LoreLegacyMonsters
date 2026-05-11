using System.Collections.Generic;
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

        /// <summary>
        /// System + context user message always; when the traveller has spoken we add a <b>third</b> bare user message
        /// containing only their exact words (never "The player just said:" wrappers—models echoed those back into UX).
        /// </summary>
        public static ChatMessageJson[] BuildMessages(NpcLlmPromptContext ctx)
        {
            var sys = BuildSystemPrompt(ctx);

            var contextBody = new StringBuilder();
            contextBody.AppendLine("Current game state (facts only, do not repeat verbatim as a list unless the player asked):");
            contextBody.AppendLine(ctx.GameStateSummary ?? "(none)");
            contextBody.AppendLine();
            contextBody.Append("Weather: ").AppendLine(ctx.WeatherSummary ?? "unknown");
            contextBody.Append("Primary quest context: ").AppendLine(ctx.QuestSummary ?? "none");
            contextBody.Append("Party summary: ").AppendLine(ctx.PartySummary ?? "none");
            contextBody.Append("Player appearance & vibe: ").Append(ctx.PlayerGearSummary ?? "unknown").Append("; tags=[")
                .Append(string.IsNullOrWhiteSpace(ctx.PlayerVibeTags) ? "" : ctx.PlayerVibeTags.Trim()).AppendLine("]");
            contextBody.Append("Inventory highlights: ").AppendLine(ctx.InventorySummary ?? "none");
            contextBody.Append("Persistent status & typical cures: ").AppendLine(ctx.StatusEffectsSummary ?? "none");
            contextBody.Append("This NPC shop stock (if any): ").AppendLine(ctx.ShopStockSummary ?? "none");
            contextBody.Append("Story branch state: ").AppendLine(ctx.StoryStateSummary ?? "none");
            contextBody.Append("Remembered history with this player: ").AppendLine(ctx.NpcMemorySummary ?? "none");
            contextBody.Append("Recent conversation: ").AppendLine(ctx.ConversationHistorySummary ?? "none");
            contextBody.AppendLine();

            var hasTurn = !string.IsNullOrWhiteSpace(ctx.PlayerMessage);

            var messages = new List<ChatMessageJson>
            {
                new ChatMessageJson { role = "system", content = sys.ToString() }
            };

            if (hasTurn)
            {
                contextBody.AppendLine("Speak only as this NPC dialogue—no screenplay labels, ");
                contextBody.AppendLine("no lines like \"The player said …\" or \"You say …\", ");
                contextBody.AppendLine("and no restating headings from above.");
                contextBody.AppendLine();
                contextBody.AppendLine(
                    "The very next USER message contains ONLY the traveller's exact spoken line—nothing else.");

                messages.Add(new ChatMessageJson { role = "user", content = contextBody.ToString() });
                messages.Add(new ChatMessageJson { role = "user", content = ctx.PlayerMessage.Trim() });
            }
            else
            {
                contextBody.AppendLine("The traveller has opened this conversation.");
                contextBody.AppendLine(
                    "Greet them briefly in character; offer one grounded, useful cue from the facts above (no preamble labels).");

                messages.Add(new ChatMessageJson { role = "user", content = contextBody.ToString() });
            }

            return messages.ToArray();
        }

        static string BuildSystemPrompt(NpcLlmPromptContext ctx)
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

            return sys.ToString();
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
