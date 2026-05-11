using System.Collections.Generic;
using System.Text;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Inventory;

namespace LoreLegacyMonsters.Dialog.Llm
{
    public static class GearPromptFormatter
    {
        /// <summary>NPC-facing one-liner: outfit + charms with rarity.</summary>
        public static string EquippedSummary(AssetRegistryManager reg, LoadoutSystem loadout)
        {
            if (loadout == null || reg == null) return "Gear: none equipped.";

            static string Label(AssetRegistryManager r, string id)
            {
                if (string.IsNullOrEmpty(id)) return null;
                var it = r.GetItem(id);
                if (it is GearItemData g)
                    return $"{g.DisplayName} ({g.Rarity.Label()})";
                return it?.DisplayName ?? id;
            }

            var sb = new StringBuilder();
            var outfit = Label(reg, loadout.OutfitEquippedId);
            if (!string.IsNullOrEmpty(outfit))
            {
                sb.Append("Wearing: ").Append(outfit).Append('.');
            }
            else sb.Append("No outfit equipped.");

            var charms = new List<string>(3);
            for (var i = 0; i < 3; i++)
            {
                var c = Label(reg, loadout.GetCharmEquippedId(i));
                if (!string.IsNullOrEmpty(c))
                    charms.Add(c);
            }

            if (charms.Count > 0)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append("Charms: ").Append(string.Join(", ", charms)).Append('.');
            }
            else sb.Append(" No charms equipped.");

            return sb.ToString();
        }

        /// <summary>Comma-separated vibe tags from current loadout snapshot.</summary>
        public static string VibeTagsBracketed(LoadoutSystem loadout)
        {
            var snap = loadout?.Snapshot;
            if (snap?.VibeTags == null || snap.VibeTags.Count == 0) return "";
            return string.Join(", ", snap.VibeTags);
        }
    }
}
