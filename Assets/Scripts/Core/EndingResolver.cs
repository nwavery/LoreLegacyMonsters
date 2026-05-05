namespace LoreLegacyMonsters.Core
{
    public static class EndingResolver
    {
        public static StoryEnding SuggestEnding()
        {
            var merge = 0;
            var seal = 0;
            var replace = 0;
            var burn = 0;

            var iona = StoryState.GetOutcome(StoryState.IonaOutcomeKey);
            var corin = StoryState.GetOutcome(StoryState.CorinOutcomeKey);
            var varo = StoryState.GetOutcome(StoryState.VaroOutcomeKey);
            var advisor = StoryState.GetAdvisor();
            var trust = StoryState.GetMiraTrust();

            if (iona == StoryState.IonaDefeat) merge += 2;
            if (iona == StoryState.IonaSpare) { replace += 2; seal += 1; }
            if (iona == StoryState.IonaWithdraw) { burn += 2; seal += 1; }

            if (corin == StoryState.CorinHandRelicToSel) merge += 2;
            if (corin == StoryState.CorinBreakRelic) seal += 2;
            if (corin == StoryState.CorinTalkDown) replace += 2;
            if (corin == StoryState.CorinSideWithCorin) burn += 3;

            if (varo == StoryState.VaroDefeat) merge += 2;
            if (varo == StoryState.VaroAlly) seal += 3;
            if (varo == StoryState.VaroDefeatKeepRelic) replace += 2;
            if (varo == StoryState.VaroRefuseSpire) burn += 3;

            if (advisor == StoryState.AdvisorBram) merge += 2;
            if (advisor == StoryState.AdvisorThren) seal += 2;
            if (advisor == StoryState.AdvisorLuma) replace += 2;
            if (advisor == StoryState.AdvisorJessa) burn += 2;

            if (trust >= 2) { merge += 1; replace += 1; }
            if (trust == 0) { seal += 1; burn += 1; }

            if (burn >= replace && burn >= seal && burn >= merge) return StoryEnding.Burn;
            if (replace >= seal && replace >= merge) return StoryEnding.Replace;
            if (seal >= merge) return StoryEnding.Seal;
            return StoryEnding.Merge;
        }

        public static string Describe(StoryEnding ending)
        {
            return ending switch
            {
                StoryEnding.Merge => "Merge: become the next network voice and stabilize Hollowfen through succession.",
                StoryEnding.Seal => "Seal: break the inherited lock and put the network to sleep.",
                StoryEnding.Replace => "Replace: rewrite the network under consent-first rules.",
                StoryEnding.Burn => "Burn: destroy the archive substrate and rebuild without it.",
                _ => "No ending suggested yet."
            };
        }
    }
}
