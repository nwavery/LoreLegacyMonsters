using UnityEngine;

namespace LoreLegacyMonsters.Achievements
{
    [CreateAssetMenu(menuName = "LLM/Achievement", fileName = "AchievementData")]
    public class AchievementData : ScriptableObject
    {
        [SerializeField] string achievementId;
        [SerializeField] string title;
        [SerializeField] string description;

        public string AchievementId => achievementId;
        public string Title => title;
        public string Description => description;
    }
}
