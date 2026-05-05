using UnityEngine;

namespace LoreLegacyMonsters.Inventory
{
    [CreateAssetMenu(menuName = "LLM/Seed", fileName = "SeedData")]
    public class SeedData : ItemData
    {
        [SerializeField] string growsIntoCropId;

        public string GrowsIntoCropId => growsIntoCropId;
    }
}
