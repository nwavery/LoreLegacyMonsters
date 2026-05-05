using UnityEngine;

namespace LoreLegacyMonsters
{
    public partial class InventoryManager : MonoBehaviour
    {
        [SerializeField] InventorySystem system;

        public InventorySystem Inventory => system;
    }
}
