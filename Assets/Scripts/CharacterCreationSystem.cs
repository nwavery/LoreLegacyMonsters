using UnityEngine;

namespace LoreLegacyMonsters
{
    public class CharacterCreationSystem : MonoBehaviour
    {
        [SerializeField] string playerName = "Hero";

        public string PlayerName => playerName;

        public void SetName(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
                playerName = name.Trim();
        }
    }
}
