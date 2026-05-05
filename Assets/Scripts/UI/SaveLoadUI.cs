using UnityEngine;
using LoreLegacyMonsters.SaveLoad;

namespace LoreLegacyMonsters.UI
{
    public class SaveLoadUI : MonoBehaviour
    {
        [SerializeField] SaveLoadManager saveLoad;

        public void SaveToSlot(int slot)
        {
            saveLoad ??= FindFirstObjectByType<SaveLoadManager>();
            if (saveLoad == null)
            {
                Core.GameEvents.RaiseToast("Save system unavailable.");
                return;
            }

            if (saveLoad.SaveSlot(slot, out var error))
                Core.GameEvents.RaiseToast($"Saved to slot {slot}.");
            else
                Core.GameEvents.RaiseToast(string.IsNullOrWhiteSpace(error) ? $"Save failed for slot {slot}." : $"Save failed: {error}");
        }

        public void LoadFromSlot(int slot)
        {
            saveLoad ??= FindFirstObjectByType<SaveLoadManager>();
            if (saveLoad == null)
            {
                Core.GameEvents.RaiseToast("Save system unavailable.");
                return;
            }

            if (saveLoad.LoadSlot(slot, out var error))
                Core.GameEvents.RaiseToast($"Loaded slot {slot}.");
            else
                Core.GameEvents.RaiseToast(string.IsNullOrWhiteSpace(error) ? $"Load failed for slot {slot}." : $"Load failed: {error}");
        }
    }
}
