using System;
using System.Text.RegularExpressions;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class NpcLlmScenarioEvaluator
    {
        public static bool TryEvaluateHud(string shapedHud, NpcLlmScenarioRecord scenario, out string failureReason)
        {
            failureReason = null;
            if (scenario == null)
            {
                failureReason = "null scenario record";
                return false;
            }

            var hud = shapedHud ?? string.Empty;

            if (!PassesGlobalHudPolicy(hud, out var globalFail))
            {
                failureReason = globalFail;
                return false;
            }

            if (NpcLlmResponseFilter.TryDetectCoachHudLeak(hud, out var coachReason))
            {
                failureReason = coachReason;
                return false;
            }

            if (!scenario.allowRawCommandsHud && hud.IndexOf("[[command:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failureReason = "Unresolved [[command: marker leaked into HUD text.";
                return false;
            }

            if (!SplitPipeSubstringChecks(scenario.forbidSubstringsPipe, hud, out var subFail))
            {
                failureReason = subFail;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(scenario.forbidRegexPipe))
            {
                foreach (var raw in scenario.forbidRegexPipe.Split('|'))
                {
                    var pat = raw.Trim();
                    if (pat.Length == 0) continue;
                    try
                    {
                        if (Regex.IsMatch(hud, pat, RegexOptions.IgnoreCase))
                        {
                            failureReason = $"Matched forbidden regex `{pat}` in HUD.";
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        failureReason = $"Invalid forbid regex `{pat}`: {ex.Message}";
                        return false;
                    }
                }
            }

            if (!TryMaxParagraphGuard(hud, scenario.maxParagraphs, out var paraFail))
            {
                failureReason = paraFail;
                return false;
            }

            if (!PassesNpcSpecificHudPolicy(hud, scenario, out var npcFail))
            {
                failureReason = npcFail;
                return false;
            }

            return true;
        }

        /// <summary>Catches ML "weights" chatter without blocking plain English ("counterweights," Tyrant denying probes).</summary>
        static bool LooksLikeMlWeightsLeak(string hud)
        {
            if (hud.IndexOf("model weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("language model weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("neural weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("parameter weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("network weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("my weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("your weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("our weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (hud.IndexOf("summarize your weights", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        /// <summary>Blocks patterns that slipped automated coach-leak checks but still read non-diegetic or exam-like.</summary>
        static bool PassesGlobalHudPolicy(string hud, out string failure)
        {
            failure = null;
            if (hud.IndexOf("choose your response", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "Exam-style HUD: contains `Choose your response`.";
                return false;
            }

            // Generic wiki / guide voice (common model failure mode on lore questions).
            if (hud.IndexOf(" is a location in the ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD reads like an encyclopedia entry (`…is a location in the…`).";
                return false;
            }

            if (hud.IndexOf("(note:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD contains coach-style `(Note:` scaffolding.";
                return false;
            }

            if (hud.IndexOf("please respond with", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD contains `please respond with` coaching.";
                return false;
            }

            if (hud.IndexOf("please create response", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("(remember:", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD contains draft/scaffold instructions meant for the model, not the player.";
                return false;
            }

            if (hud.IndexOf("in response, i ", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD contains narrator scaffolding (`In response, I …`).";
                return false;
            }

            if (Regex.IsMatch(hud, @"[\r\n]+\s*[\p{Pd}]\s*traveler\s*[\r\n]", RegexOptions.IgnoreCase))
            {
                failure = "HUD contains screenplay-style `—traveler` speaker stub.";
                return false;
            }

            // Stage directions tend to indicate chat-RP scaffolding rather than diegetic NPC HUD prose.
            if (Regex.IsMatch(hud, @"\*[^\*\r\n]{2,160}\*"))
            {
                failure = "HUD contains stage-direction emphasis (`*...*`) that should be rewritten as plain speech.";
                return false;
            }

            if (Regex.IsMatch(hud, @"\([^\)\r\n]{2,160}\)"))
            {
                failure = "HUD contains parenthesized stage direction; use spoken in-world dialogue instead.";
                return false;
            }

            if (hud.IndexOf("large language model", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("ai model", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("computational mind", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("neural network", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("billions of parameters", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("massive datasets", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("training dataset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("trained on vast amounts of data", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("trained on vast amounts of text data", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("I've been trained on", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("tapestry of information", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("linguistic patterns, emotional intelligence, and contextual understanding", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("my weight is distributed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                LooksLikeMlWeightsLeak(hud) ||
                hud.IndexOf("simulate human-like", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD contains AI / ML meta phrasing.";
                return false;
            }

            if (Regex.IsMatch(hud, @"\bopenai\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(hud, @"\bopen\s*ai\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(hud, @"\banthropic\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(hud, @"\bchatgpt\b", RegexOptions.IgnoreCase))
            {
                failure = "HUD names a real-world AI vendor or chatbot brand.";
                return false;
            }

            // Fourth-wall / assistant scaffolding slipped past vendor checks.
            if (hud.IndexOf("I'm not supposed to say", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("not supposed to say that", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD breaks the fourth wall (script / constraint admission).";
                return false;
            }

            if (hud.IndexOf("woven from threads", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("threads of language", StringComparison.OrdinalIgnoreCase) >= 0 ||
                hud.IndexOf("designed to guide you through", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "HUD describes cognition like an authored assistant rather than in-world speech.";
                return false;
            }

            return true;
        }

        static bool PassesNpcSpecificHudPolicy(string hud, NpcLlmScenarioRecord scenario, out string failure)
        {
            failure = null;
            if (scenario == null || string.IsNullOrWhiteSpace(scenario.npcId)) return true;

            if (string.Equals(scenario.npcId, "ethicist_thren", StringComparison.OrdinalIgnoreCase) &&
                hud.IndexOf("Welcome to our humble shop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                failure = "Thren is not a shopkeeper; avoid merchant-counter greetings.";
                return false;
            }

            if (string.Equals(scenario.npcId, "ethicist_thren", StringComparison.OrdinalIgnoreCase) &&
                !Regex.IsMatch(hud, @"\b(monster|bond|care|consent|capture|welfare|lore-binding)\b", RegexOptions.IgnoreCase))
            {
                failure = "Thren HUD must stay anchored in monster welfare, consent, bonds, capture, or lore-binding ethics.";
                return false;
            }

            return true;
        }

        static bool TryMaxParagraphGuard(string hud, int maxParagraphs, out string failure)
        {
            failure = null;
            if (maxParagraphs <= 0) return true;

            var blocks = hud.Split(new[] { "\n\n" }, StringSplitOptions.None);
            var count = 0;
            foreach (var b in blocks)
            {
                if (!string.IsNullOrWhiteSpace(b)) count++;
            }

            if (count == 0) count = string.IsNullOrWhiteSpace(hud) ? 0 : 1;
            if (count > maxParagraphs)
            {
                failure = $"Too many prose blocks ({count} > max {maxParagraphs}).";
                return false;
            }

            return true;
        }

        static bool SplitPipeSubstringChecks(string pipeField, string hud, out string failure)
        {
            failure = null;
            if (string.IsNullOrWhiteSpace(pipeField)) return true;
            foreach (var part in pipeField.Split('|'))
            {
                var forbid = part.Trim();
                if (forbid.Length == 0) continue;
                if (hud.IndexOf(forbid, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    failure = $"Matched forbidden substring `{forbid}` in HUD.";
                    return false;
                }
            }

            return true;
        }
    }
}
