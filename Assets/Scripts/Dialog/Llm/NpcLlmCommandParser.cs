using System;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class NpcLlmCommandParser
    {
        public static bool TryParseAndStrip(string raw, out string displayText, out NpcLlmValidatedCommand command)
        {
            displayText = StripCommandMarkers(raw);
            command = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            var markerStart = raw.LastIndexOf("[[command:", StringComparison.OrdinalIgnoreCase);
            if (markerStart < 0) return false;
            var markerEnd = raw.IndexOf("]]", markerStart, StringComparison.OrdinalIgnoreCase);
            if (markerEnd < 0) return false;

            var token = raw.Substring(markerStart + 10, markerEnd - markerStart - 10).Trim();
            var parts = token.Split('|');
            if (parts.Length == 0) return false;

            var parsed = new NpcLlmValidatedCommand
            {
                Type = ParseType(parts[0]),
                Payload = parts.Length > 1 ? parts[1].Trim() : string.Empty
            };

            if (!parsed.IsValid)
                return false;

            command = parsed;
            return true;
        }

        public static string StripCommandMarkers(string raw)
        {
            var text = raw ?? string.Empty;
            var cursor = 0;
            while (cursor < text.Length)
            {
                var markerStart = text.IndexOf("[[command:", cursor, StringComparison.OrdinalIgnoreCase);
                if (markerStart < 0) break;
                var markerEnd = text.IndexOf("]]", markerStart, StringComparison.OrdinalIgnoreCase);
                if (markerEnd < 0)
                {
                    text = text.Substring(0, markerStart).Trim();
                    break;
                }

                text = (text.Substring(0, markerStart) + text.Substring(markerEnd + 2)).Trim();
                cursor = markerStart;
            }

            return text.Trim();
        }

        static NpcLlmCommandType ParseType(string rawType)
        {
            if (string.IsNullOrWhiteSpace(rawType)) return NpcLlmCommandType.None;
            return rawType.Trim().ToLowerInvariant() switch
            {
                "offer_hint" => NpcLlmCommandType.OfferHint,
                "suggest_destination" => NpcLlmCommandType.SuggestDestination,
                "open_shop" => NpcLlmCommandType.OpenShop,
                "offer_heal" => NpcLlmCommandType.OfferHeal,
                "offer_battle" => NpcLlmCommandType.OfferBattle,
                _ => NpcLlmCommandType.None
            };
        }
    }
}
