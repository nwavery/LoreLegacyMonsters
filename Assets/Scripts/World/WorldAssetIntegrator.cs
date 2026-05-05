using UnityEngine;

namespace LoreLegacyMonsters.World
{
    public class WorldAssetIntegrator : MonoBehaviour
    {
        [SerializeField] WorldArea[] areas;

        public WorldArea[] Areas => areas;
    }
}
