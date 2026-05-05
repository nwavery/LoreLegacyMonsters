namespace LoreLegacyMonsters.Dialog
{
    public static class DialogChoiceExtensions
    {
        public static bool HasChoices(this DialogEntry e) =>
            e?.choiceNextIds != null && e.choiceNextIds.Length > 0;

        public static string GetChoiceLabel(this DialogEntry e, int index)
        {
            if (e == null || index < 0 || e.choiceNextIds == null || index >= e.choiceNextIds.Length)
                return string.Empty;
            if (e.choiceLabels != null && index < e.choiceLabels.Length && !string.IsNullOrWhiteSpace(e.choiceLabels[index]))
                return e.choiceLabels[index].Trim();
            var token = e.choiceNextIds[index];
            return string.IsNullOrWhiteSpace(token) ? $"Choice {index + 1}" : token.Trim();
        }
    }
}
