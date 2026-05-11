using System.Collections.Generic;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Monster;
using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    /// <summary>Rare monster gear drops independent of scripted loot tables.</summary>
    public static class GearLootRoller
    {
        public static bool TryRollAndAward(GameManager gm, MonsterData defeatedEnemy, bool wasBoss,
            LoadoutModifiers loadoutMods, IRandomSource rng)
        {
            if (gm == null || gm.Inventory == null || defeatedEnemy == null) return false;
            rng ??= UnityRandomSource.Default;
            loadoutMods ??= LoadoutModifiers.Empty;

            var luck = Mathf.Max(0.1f, loadoutMods.LuckMult);
            var baseProb = wasBoss ? 0.12f : 0.035f;
            if (rng.Next01() >= Mathf.Clamp01(baseProb * luck)) return false;

            var table = defeatedEnemy.GearDropTable;

            GearDropRollEntry choice = null;

            if (table != null && table.Entries != null && table.Entries.Count > 0)
            {
                choice = WeightedRollFromTable(table.Entries, wasBoss, rng);
            }

            if (choice == null || string.IsNullOrEmpty(choice.itemId))
            {
                if (!WeightedRollFromFallbackPools(rng, out var fallbackId))
                    return false;
                gm.Inventory.AddItem(fallbackId, 1);
                AnnounceDrop(gm, fallbackId);
                return true;
            }

            gm.Inventory.AddItem(choice.itemId, 1);
            AnnounceDrop(gm, choice.itemId);
            return true;
        }

        static void AnnounceDrop(GameManager gm, string itemId)
        {
            var data = gm.Assets?.GetItem(itemId);
            var name = data != null && !string.IsNullOrWhiteSpace(data.DisplayName) ? data.DisplayName : itemId;
            GameEvents.RaiseToast($"Rare drop: {name}!");
        }

        static GearDropRollEntry WeightedRollFromTable(IReadOnlyList<GearDropRollEntry> rows, bool wasBoss,
            IRandomSource rng)
        {
            float sum = 0f;
            for (var i = 0; i < rows.Count; i++)
            {
                var e = rows[i];
                if (e == null || string.IsNullOrEmpty(e.itemId)) continue;
                if (e.requiresBoss && !wasBoss) continue;
                sum += Mathf.Max(0.01f, e.weight);
            }

            if (sum <= 0f) return null;
            var pick = rng.Next01() * sum;
            float acc = 0f;
            for (var i = 0; i < rows.Count; i++)
            {
                var e = rows[i];
                if (e == null || string.IsNullOrEmpty(e.itemId)) continue;
                if (e.requiresBoss && !wasBoss) continue;
                acc += Mathf.Max(0.01f, e.weight);
                if (pick <= acc)
                    return e;
            }

            return null;
        }

        static bool WeightedRollFromFallbackPools(IRandomSource rng, out string itemId)
        {
            itemId = null;

            static string Pick(RandomPickSet set, IRandomSource r)
            {
                if (set.Ids.Length == 0) return null;
                return set.Ids[r.NextInt(0, set.Ids.Length)];
            }

            var roll = rng.Next01();
            Rarity rarity;
            if (roll < 0.62f)
                rarity = Rarity.Common;
            else if (roll < 0.88f)
                rarity = Rarity.Uncommon;
            else if (roll < 0.97f)
                rarity = Rarity.Rare;
            else rarity = Rarity.Legendary;

            RandomPickSet set = rarity switch
            {
                Rarity.Common => new RandomPickSet(DefaultGameContent.GearLootCommonIds),
                Rarity.Uncommon => new RandomPickSet(DefaultGameContent.GearLootUncommonIds),
                Rarity.Rare => new RandomPickSet(DefaultGameContent.GearLootRareIds),
                Rarity.Legendary => new RandomPickSet(DefaultGameContent.GearLootLegendaryIds),
                _ => new RandomPickSet(DefaultGameContent.GearLootCommonIds)
            };

            itemId = Pick(set, rng);
            return itemId != null;
        }

        readonly struct RandomPickSet
        {
            public readonly string[] Ids;
            public RandomPickSet(string[] ids) => Ids = ids ?? System.Array.Empty<string>();
        }
    }
}
