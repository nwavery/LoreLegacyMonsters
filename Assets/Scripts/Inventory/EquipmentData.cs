using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Equipment", fileName = "EquipmentData")]
    [System.Obsolete("Use GearItemData. This alias remains so older equipment assets still load as gear.")]
    public class EquipmentData : GearItemData
    {
        [SerializeField] int attackBonus;
        [SerializeField] int defenseBonus;

        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;
    }
}
