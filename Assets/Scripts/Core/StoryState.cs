namespace LoreLegacyMonsters.Core
{
    public enum StoryEnding
    {
        None,
        Merge,
        Seal,
        Replace,
        Burn
    }

    public static class StoryState
    {
        public const string NetworkAware = "network_aware";
        public const string CorinTruthKnown = "corin_truth_known";
        public const string JessaFormerMiraKnown = "jessa_is_former_mira_known";
        public const string VaroJournalRead = "varo_journal_read";
        public const string VeyaRelease = "veya_release";
        public const string IrisSuppress = "iris_suppress";
        public const string PiaDoorOpenEarly = "pia_door_open_early";

        public const string IonaOutcomeKey = "iona_outcome";
        public const string CorinOutcomeKey = "corin_outcome";
        public const string VaroOutcomeKey = "varo_outcome";
        public const string AdvisorKey = "phase2_advisor";
        public const string MiraTrustKey = "mira_trust";
        public const string EndingKey = "ending";

        public const string IonaDefeat = "defeat";
        public const string IonaSpare = "spare";
        public const string IonaWithdraw = "withdraw";

        public const string CorinHandRelicToSel = "hand_relic_to_sel";
        public const string CorinBreakRelic = "break_relic";
        public const string CorinSideWithCorin = "side_with_corin";
        public const string CorinTalkDown = "talk_down_corin";

        public const string VaroDefeat = "defeat_varo";
        public const string VaroAlly = "ally_with_varo";
        public const string VaroDefeatKeepRelic = "defeat_and_keep_relic";
        public const string VaroRefuseSpire = "refuse_spire";

        public const string AdvisorLuma = "luma";
        public const string AdvisorThren = "thren";
        public const string AdvisorBram = "bram";
        public const string AdvisorJessa = "jessa";

        public const string EndingMergeFlag = "ending_merge";
        public const string EndingSealFlag = "ending_seal";
        public const string EndingReplaceFlag = "ending_replace";
        public const string EndingBurnFlag = "ending_burn";

        public static void SetOutcome(string key, string value)
        {
            StoryFlags.SetValue(key, value);
        }

        public static string GetOutcome(string key) => StoryFlags.GetValue(key);

        public static bool HasOutcome(string key) => !string.IsNullOrEmpty(GetOutcome(key));

        public static int GetMiraTrust() => StoryFlags.GetInt(MiraTrustKey);

        public static int AddMiraTrust(int delta) => StoryFlags.AddInt(MiraTrustKey, delta, 0, 3);

        public static void SetAdvisor(string advisor) => SetOutcome(AdvisorKey, advisor);

        public static string GetAdvisor() => GetOutcome(AdvisorKey);

        public static void SetEnding(StoryEnding ending)
        {
            StoryFlags.Clear(EndingMergeFlag);
            StoryFlags.Clear(EndingSealFlag);
            StoryFlags.Clear(EndingReplaceFlag);
            StoryFlags.Clear(EndingBurnFlag);

            switch (ending)
            {
                case StoryEnding.Merge:
                    StoryFlags.SetFlag(EndingMergeFlag);
                    StoryFlags.SetValue(EndingKey, "merge");
                    break;
                case StoryEnding.Seal:
                    StoryFlags.SetFlag(EndingSealFlag);
                    StoryFlags.SetValue(EndingKey, "seal");
                    break;
                case StoryEnding.Replace:
                    StoryFlags.SetFlag(EndingReplaceFlag);
                    StoryFlags.SetValue(EndingKey, "replace");
                    break;
                case StoryEnding.Burn:
                    StoryFlags.SetFlag(EndingBurnFlag);
                    StoryFlags.SetValue(EndingKey, "burn");
                    break;
                default:
                    StoryFlags.SetValue(EndingKey, string.Empty);
                    break;
            }
        }

        public static StoryEnding GetEnding()
        {
            return StoryFlags.GetValue(EndingKey) switch
            {
                "merge" => StoryEnding.Merge,
                "seal" => StoryEnding.Seal,
                "replace" => StoryEnding.Replace,
                "burn" => StoryEnding.Burn,
                _ => StoryEnding.None
            };
        }
    }
}
