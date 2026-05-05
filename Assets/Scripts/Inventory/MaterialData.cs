using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Material", fileName = "MaterialData")]
    public class MaterialData : ItemData
    {
        [SerializeField] int tier;

        public int Tier => tier;
    }
}
