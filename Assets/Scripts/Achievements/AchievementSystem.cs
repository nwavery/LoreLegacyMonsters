using System.Collections.Generic;
using UnityEngine;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.Platform.Steam;

namespace LoreLegacyMonsters.Achievements
{
    public class AchievementSystem : MonoBehaviour
    {
        [SerializeField] List<string> unlocked = new List<string>();

        public void LoadFromSave(List<string> ids)
        {
            unlocked = ids != null ? new List<string>(ids) : new List<string>();
        }

        public List<string> GetUnlockedIds() => new List<string>(unlocked);

        public bool Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || unlocked.Contains(id)) return false;
            unlocked.Add(id);
            GameEvents.RaiseAchievementUnlocked(id);
            SteamAchievementBackend.Unlock(id);
            return true;
        }
    }
}
