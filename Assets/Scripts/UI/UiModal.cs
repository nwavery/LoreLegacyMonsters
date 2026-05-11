using System.Collections.Generic;

namespace LoreLegacyMonsters.UI
{
    public enum UiModal
    {
        Dialog,
        Shop,
        Party,
        QuestLog,
        Map,
        Inventory,
        Loadout,
        Combat,
        Loading,
        Help,
        Ending,
        Pause,
        Settings
    }

    public readonly struct UiModalSpec
    {
        public readonly bool BlocksWorldInput;

        public UiModalSpec(bool blocksWorldInput)
        {
            BlocksWorldInput = blocksWorldInput;
        }
    }

    public static class UiModalRegistry
    {
        static readonly Dictionary<UiModal, UiModalSpec> Specs = new Dictionary<UiModal, UiModalSpec>
        {
            { UiModal.Dialog, new UiModalSpec(true) },
            { UiModal.Shop, new UiModalSpec(true) },
            { UiModal.Party, new UiModalSpec(true) },
            { UiModal.QuestLog, new UiModalSpec(true) },
            { UiModal.Map, new UiModalSpec(true) },
            { UiModal.Inventory, new UiModalSpec(true) },
            { UiModal.Loadout, new UiModalSpec(true) },
            { UiModal.Combat, new UiModalSpec(true) },
            { UiModal.Loading, new UiModalSpec(true) },
            { UiModal.Help, new UiModalSpec(true) },
            { UiModal.Ending, new UiModalSpec(true) },
            { UiModal.Pause, new UiModalSpec(true) },
            { UiModal.Settings, new UiModalSpec(true) }
        };

        public static UiModalSpec Get(UiModal modal)
        {
            // Default to blocking so newly added modal values are fail-safe.
            return Specs.TryGetValue(modal, out var spec) ? spec : new UiModalSpec(true);
        }
    }
}
