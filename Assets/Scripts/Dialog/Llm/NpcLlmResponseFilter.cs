using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class NpcLlmResponseFilter
    {
        static readonly Regex RolePrefixLine = new Regex(@"^\s*(system|assistant|user)\s*:\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex SpecialTokenLine = new Regex(@"^\s*<\|.*\|>\s*$",
            RegexOptions.Compiled);
        /// <summary>Models echoed our legacy prompt scaffolding as assistant text—drop entire line.</summary>
        static readonly Regex PlayerEchoMetaLead = new Regex(
            @"^\s*(?:The\s+)?(?:player|traveller|traveler)\s+(?:just\s+)?said\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex YouSayPrefix = new Regex(
            @"^\s*you\s+say\s*[:\u2014\-]\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        /// <summary>Legacy narrator line like <c>You say:</c> — not in-dialogue echoes like "<c>, you say?</c>".</summary>
        static readonly Regex NarratorYouSayCoach = new Regex(
            @"^\s*you\s+say\s*[:\u2014\-]",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>Strip common jailbreak / meta lines after <see cref="OpenAiCompatibleLlmClient.SanitizeReply"/>.</summary>
        public static string Clean(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var lines = raw.Replace("\r\n", "\n").Split('\n');
            var sb = new StringBuilder(raw.Length);
            var blankRun = 0;
            foreach (var line in lines)
            {
                var t = line.TrimEnd();
                if (string.IsNullOrWhiteSpace(t))
                {
                    blankRun++;
                    if (blankRun <= 2)
                        sb.Append('\n');
                    continue;
                }

                blankRun = 0;
                if (RolePrefixLine.IsMatch(t)) continue;
                if (SpecialTokenLine.IsMatch(t)) continue;
                var trim = t.TrimStart();
                if (trim.StartsWith("As an AI", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("I'm an AI", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("As a language model", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("I'm ChatGPT", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("I'm GPT", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("I'm a large language model", StringComparison.OrdinalIgnoreCase)) continue;

                if (PlayerEchoMetaLead.IsMatch(trim))
                    continue;

                if (LooksLikeAiVendorLeak(trim))
                    continue;

                if (LooksLikeAssistantRoleplayScaffold(trim))
                    continue;

                if (trim.IndexOf("choose your response", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Screenplay stubs some models emit instead of prose.
                if (string.Equals(trim, "(PLAYER)", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(trim, "(NPC)", StringComparison.OrdinalIgnoreCase))
                    continue;

                trim = YouSayPrefix.Replace(trim, string.Empty).TrimStart();
                if (string.IsNullOrWhiteSpace(trim))
                    continue;

                if (LooksLikeStandalonePlayerEchoMeta(trim))
                    continue;

                // Placeholder stubs ("..." only lines).
                if (Regex.IsMatch(trim, @"^\.{2,}$")) continue;

                sb.Append(trim).Append('\n');
            }

            return sb.ToString().Trim();
        }

        static bool LooksLikeAiVendorLeak(string trim)
        {
            if (string.IsNullOrEmpty(trim)) return false;
            if (trim.IndexOf("anthropic", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (trim.IndexOf("openai", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (trim.IndexOf("chatgpt", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (trim.IndexOf("billions of parameters", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (trim.IndexOf("training dataset", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (trim.IndexOf("simulate human-like", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return Regex.IsMatch(trim, @"\bGPT-?\d\b", RegexOptions.IgnoreCase);
        }

        static bool LooksLikeAssistantRoleplayScaffold(string trim)
        {
            if (string.IsNullOrEmpty(trim)) return false;
            var low = trim.ToLowerInvariant();
            if (low.IndexOf("please simulate", StringComparison.Ordinal) >= 0) return true;
            if (low.IndexOf("please create response", StringComparison.Ordinal) >= 0) return true;
            if (low.IndexOf("player response:", StringComparison.Ordinal) >= 0) return true;
            if (low.IndexOf("here is my response as the npc", StringComparison.Ordinal) >= 0) return true;
            if (low.IndexOf("the player will respond", StringComparison.Ordinal) >= 0) return true;
            if (trim.StartsWith("In response, I", StringComparison.OrdinalIgnoreCase)) return true;
            if (trim.StartsWith("(Remember:", StringComparison.OrdinalIgnoreCase)) return true;
            if (Regex.IsMatch(trim, @"^\s*[\p{Pd}]\s*traveler\s*$", RegexOptions.IgnoreCase)) return true;
            if (Regex.IsMatch(trim, @"^\s*\*{0,2}your turn\*{0,2}\.?\s*$", RegexOptions.IgnoreCase)) return true;
            if (trim.StartsWith("(Note:", StringComparison.OrdinalIgnoreCase)) return true;
            if (trim.StartsWith("---", StringComparison.Ordinal)) return true;
            if (trim.StartsWith("Before you make your decision", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        /// <summary>Drop whole paragraphs that are purely "The player …" scaffolding from old prompts.</summary>
        static bool LooksLikeStandalonePlayerEchoMeta(string trim)
        {
            if (!trim.StartsWith("The ", StringComparison.OrdinalIgnoreCase) &&
                !trim.StartsWith("Player ", StringComparison.OrdinalIgnoreCase))
                return false;
            var low = trim.ToLowerInvariant();
            return low.StartsWith("the player ") && low.Contains("said");
        }

        public static bool IsTooShortToDisplay(string cleaned)
        {
            return string.IsNullOrWhiteSpace(cleaned) || cleaned.Trim().Length < 10;
        }

        /// Same compaction as the regression test helper—use after full HUD shaping pipeline.
        public static string CompactHudForCoachLeakScan(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var t = raw.Replace('\n', ' ').Replace('\r', ' ')
                .Replace("—", string.Empty).Replace("-", string.Empty).Replace(" ", string.Empty);
            return t.ToLowerInvariant();
        }

        /// <summary>Detects legacy prompt scaffolding echoed into NPC bubble text.</summary>
        public static bool TryDetectCoachHudLeak(string shapedHudForDisplay, out string failureReason)
        {
            failureReason = null;
            var compact = CompactHudForCoachLeakScan(shapedHudForDisplay ?? string.Empty);
            if (compact.IndexOf("theplayerjustsaid", StringComparison.Ordinal) >= 0)
            {
                failureReason = "HUD contains scaffold phrase like `the player just said`.";
                return true;
            }

            var rawHud = shapedHudForDisplay ?? string.Empty;
            if (NarratorYouSayCoach.IsMatch(rawHud) ||
                compact.IndexOf("yousay:", StringComparison.Ordinal) >= 0)
            {
                failureReason = "HUD contains narrator `you say:` style scaffolding.";
                return true;
            }

            return false;
        }
    }
}
