namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>
    /// Mirrors the assistant reply shaping used in <see cref="GameDialogDriver"/> before dialog UI sees text.
    /// Order: <see cref="OpenAiCompatibleLlmClient.SanitizeReply"/> → <see cref="NpcLlmResponseFilter.Clean"/> → role command strip/parse.
    /// </summary>
    public static class NpcLlmDisplayPipeline
    {
        /// <summary><see cref="OpenAiCompatibleLlmClient.SanitizeReply"/> → <see cref="NpcLlmResponseFilter.Clean"/> → command strip/parse.</summary>
        /// <remarks>Matches <see cref="Dialog.GameDialogDriver"/> streaming success path.</remarks>
        public static string ShapeForHud(string rawAssistantContent)
        {
            return ShapeForHud(rawAssistantContent, out _);
        }

        /// <inheritdoc cref="ShapeForHud(string)"/>
        public static string ShapeForHud(string rawAssistantContent, out NpcLlmValidatedCommand parsedCommand)
        {
            parsedCommand = null;
            var clean = OpenAiCompatibleLlmClient.SanitizeReply(rawAssistantContent ?? string.Empty);
            clean = NpcLlmResponseFilter.Clean(clean);
            if (NpcLlmCommandParser.TryParseAndStrip(clean, out var displayText, out var parsedCmd))
            {
                clean = displayText;
                parsedCommand = parsedCmd;
            }
            else
                clean = NpcLlmCommandParser.StripCommandMarkers(clean);

            return clean;
        }
    }
}
