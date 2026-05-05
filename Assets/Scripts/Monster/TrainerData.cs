using UnityEngine;

namespace LoreLegacyMonsters.Monster
{
    [CreateAssetMenu(menuName = "LLM/Trainer", fileName = "TrainerData")]
    public class TrainerData : ScriptableObject
    {
        [SerializeField] string trainerId;
        [SerializeField] MonsterData[] team;

        public string TrainerId => trainerId;
        public MonsterData[] Team => team;
    }
}
