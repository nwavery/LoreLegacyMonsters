using UnityEngine;

namespace LoreLegacyMonsters
{
    public partial class InventoryManager
    {
        void Awake()
        {
            if (system == null)
                system = GetComponent<InventorySystem>();
            if (system == null)
                system = gameObject.AddComponent<InventorySystem>();
        }
    }
}
