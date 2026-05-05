using UnityEngine;

namespace LoreLegacyMonsters.UI
{
    public class SaveSlotUI : MonoBehaviour
    {
        [SerializeField] int slotIndex;
        [SerializeField] UnityEngine.UI.Text label;

        public int SlotIndex => slotIndex;

        public void BindLabel(string text)
        {
            if (label != null) label.text = text;
        }
    }
}
