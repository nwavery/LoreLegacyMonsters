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
                if (RolePrefixLine.IsMatch(t)) continue;
                if (SpecialTokenLine.IsMatch(t)) continue;
                var trim = t.TrimStart();
                if (trim.StartsWith("As an AI", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("I'm an AI", StringComparison.OrdinalIgnoreCase)) continue;
                if (trim.StartsWith("As a language model", StringComparison.OrdinalIgnoreCase)) continue;

                if (string.IsNullOrWhiteSpace(t))
                {
                    blankRun++;
                    if (blankRun <= 2) sb.Append('\n');
                    continue;
                }

                blankRun = 0;
                sb.Append(t).Append('\n');
            }

            return sb.ToString().Trim();
        }

        public static bool IsTooShortToDisplay(string cleaned)
        {
            return string.IsNullOrWhiteSpace(cleaned) || cleaned.Trim().Length < 10;
        }
    }
}
