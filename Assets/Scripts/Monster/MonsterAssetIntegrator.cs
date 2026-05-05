using UnityEngine;

namespace LoreLegacyMonsters.Monster
{
    public class MonsterAssetIntegrator : MonoBehaviour
    {
        [SerializeField] MonsterData[] preload;

        public MonsterData[] Preload => preload;
    }
}
