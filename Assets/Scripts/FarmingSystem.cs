using UnityEngine;

namespace LoreLegacyMonsters
{
    public class FarmingSystem : MonoBehaviour
    {
        [SerializeField] int cropGrowthDays;

        public int CropGrowthDays => cropGrowthDays;

        public void PlantSeed(string cropId) => Debug.Log($"Planted {cropId}");
    }
}
