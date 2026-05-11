using System;
using System.Collections.Generic;
using LoreLegacyMonsters.SaveSystem;
using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    /// <summary>Tracks equipped outfit (1) and charms (3); items remain in inventory as stacks.</summary>
    public class LoadoutSystem : MonoBehaviour
    {
        [SerializeField] string outfitItemId = "";
        [SerializeField] string charm0ItemId = "";
        [SerializeField] string charm1ItemId = "";
        [SerializeField] string charm2ItemId = "";

        InventorySystem _inventory;
        AssetRegistryManager _registry;

        LoadoutModifiers _snapshot;
        bool _snapshotDirty = true;

        /// <summary>Arguments: slot, charmIndex (-1 for outfit), old item id, new item id.</summary>
        public event Action<GearSlot, int, string, string> LoadoutChanged;

        public static LoadoutSystem FindOrResolve()
        {
            if (GameManager.Instance != null)
            {
                var lo = GameManager.Instance.GetComponent<LoadoutSystem>();
                if (lo != null)
                    return lo;
            }

            return FindFirstObjectByType<LoadoutSystem>();
        }

        public void Bind(InventorySystem inv, AssetRegistryManager reg)
        {
            _inventory = inv;
            _registry = reg;
            InvalidateSnapshot();
        }

        public string OutfitEquippedId => outfitItemId ?? "";

        public string GetCharmEquippedId(int index)
        {
            if (index < 0 || index > 2) return "";
            return index switch
            {
                0 => charm0ItemId ?? "",
                1 => charm1ItemId ?? "",
                2 => charm2ItemId ?? "",
                _ => ""
            };
        }

        public void ApplyFromDto(LoadoutDto dto)
        {
            outfitItemId = dto?.outfitItemId ?? "";
            var c = dto?.charmItemIds;
            charm0ItemId = c != null && c.Count > 0 ? c[0] ?? "" : "";
            charm1ItemId = c != null && c.Count > 1 ? c[1] ?? "" : "";
            charm2ItemId = c != null && c.Count > 2 ? c[2] ?? "" : "";
            InvalidateSnapshot();
        }

        public LoadoutDto ToDto()
        {
            return new LoadoutDto
            {
                outfitItemId = outfitItemId ?? "",
                charmItemIds = new List<string>(3)
                    { charm0ItemId ?? "", charm1ItemId ?? "", charm2ItemId ?? "" }
            };
        }

        public LoadoutModifiers Snapshot
        {
            get
            {
                if (_snapshotDirty || _snapshot == null)
                {
                    _snapshot = ComputeSnapshot();
                    _snapshotDirty = false;
                }

                return _snapshot;
            }
        }

        public LoadoutModifiers PeekSnapshotIgnoringCache() => ComputeSnapshot();

        void InvalidateSnapshot() => _snapshotDirty = true;

        LoadoutModifiers ComputeSnapshot()
        {
            var items = new List<GearItemData>(4);
            TryAddGear(outfitItemId, items);
            TryAddGear(charm0ItemId, items);
            TryAddGear(charm1ItemId, items);
            TryAddGear(charm2ItemId, items);
            return LoadoutModifiers.FromGearItems(items);
        }

        void TryAddGear(string id, ICollection<GearItemData> sink)
        {
            if (string.IsNullOrEmpty(id) || _registry == null) return;
            if (_registry.GetItem(id) is GearItemData { } g)
                sink.Add(g);
        }

        public bool TryUnequipOutfit() => TryUnequipInternal(GearSlot.Outfit, -1);

        public bool TryUnequipCharm(int charmIndex) => TryUnequipInternal(GearSlot.Charm, charmIndex);

        bool TryUnequipInternal(GearSlot slot, int charmIndex)
        {
            string oldVal;
            if (slot == GearSlot.Outfit)
            {
                oldVal = OutfitEquippedId;
                if (string.IsNullOrEmpty(oldVal)) return false;
                outfitItemId = "";
            }
            else
            {
                if (charmIndex < 0 || charmIndex > 2) return false;
                oldVal = GetCharmEquippedId(charmIndex);
                if (string.IsNullOrEmpty(oldVal)) return false;
                SetCharmId(charmIndex, "");
            }

            InvalidateSnapshot();
            LoadoutChanged?.Invoke(slot, charmIndex, oldVal, "");
            return true;
        }

        void SetCharmId(int index, string id)
        {
            switch (index)
            {
                case 0:
                    charm0ItemId = id ?? "";
                    break;
                case 1:
                    charm1ItemId = id ?? "";
                    break;
                case 2:
                    charm2ItemId = id ?? "";
                    break;
            }
        }

        /// <summary>Equip gear. Charm index optional; defaults to first empty or replaces charm slot 2 when full.</summary>
        public bool TryEquip(string itemId, int charmSlotIndexPreferred = -1)
        {
            if (string.IsNullOrEmpty(itemId) || _inventory == null || _registry == null) return false;
            if (_registry.GetItem(itemId) is not GearItemData gear) return false;
            if (_inventory.Count(itemId) <= 0) return false;

            if (gear.Slot == GearSlot.Outfit)
            {
                var old = outfitItemId ?? "";
                if (old == itemId) return true;
                if (!CanAssign(outfitItemId, itemId)) return false;
                outfitItemId = itemId;
                InvalidateSnapshot();
                LoadoutChanged?.Invoke(GearSlot.Outfit, -1, old, itemId);
                return true;
            }

            var slotIx = ResolveCharmEquipIndex(charmSlotIndexPreferred, out var replacedId);
            if (slotIx < 0) return false;
            if (replacedId == itemId) return true;
            if (!CanAssign(replacedId, itemId)) return false;
            SetCharmId(slotIx, itemId);
            InvalidateSnapshot();
            LoadoutChanged?.Invoke(GearSlot.Charm, slotIx, replacedId ?? "", itemId);
            return true;
        }

        int ResolveCharmEquipIndex(int charmSlotIndexPreferred, out string replaced)
        {
            replaced = "";
            if (charmSlotIndexPreferred >= 0 && charmSlotIndexPreferred <= 2)
            {
                replaced = GetCharmEquippedId(charmSlotIndexPreferred);
                return charmSlotIndexPreferred;
            }

            if (string.IsNullOrEmpty(charm0ItemId)) return 0;
            if (string.IsNullOrEmpty(charm1ItemId)) return 1;
            if (string.IsNullOrEmpty(charm2ItemId)) return 2;
            replaced = charm2ItemId ?? "";
            return 2;
        }

        int CountEquipped(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            var n = 0;
            if (OutfitEquippedId == itemId) n++;
            for (var i = 0; i < 3; i++)
                if (GetCharmEquippedId(i) == itemId)
                    n++;
            return n;
        }

        /// <summary>After assignment, count of <paramref name="equipId"/> must not exceed bag quantity.</summary>
        bool CanAssign(string currentOccupant, string equipId)
        {
            var qty = _inventory.Count(equipId);
            var k = CountEquipped(equipId);
            var after = k;
            if (string.IsNullOrEmpty(currentOccupant)) after = k + 1;
            else if (currentOccupant == equipId) after = k;
            else after = k + 1;
            return qty >= after;
        }

        public bool IsFullLoadout()
        {
            if (string.IsNullOrEmpty(OutfitEquippedId)) return false;
            for (var i = 0; i < 3; i++)
                if (string.IsNullOrEmpty(GetCharmEquippedId(i)))
                    return false;
            return true;
        }
    }
}
