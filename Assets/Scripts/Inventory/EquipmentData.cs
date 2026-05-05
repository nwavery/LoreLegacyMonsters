using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Equipment", fileName = "EquipmentData")]
    public class EquipmentData : ItemData
    {
        [SerializeField] int attackBonus;
        [SerializeField] int defenseBonus;

        public int AttackBonus => attackBonus;
        public int DefenseBonus => defenseBonus;
    }
}
