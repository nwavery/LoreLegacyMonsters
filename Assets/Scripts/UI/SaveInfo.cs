using UnityEngine;
using LoreLegacyMonsters.SaveSystem;

namespace LoreLegacyMonsters.UI
{
    /// <summary>View helper for save metadata (distinct from SaveSystem.SaveInfo DTO).</summary>
    public class SaveInfoUI : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Text title;

        public void Display(SaveInfo data)
        {
            if (title != null && data != null)
                title.text = $"{data.PlayerName} — {data.CurrentAreaId}";
        }
    }
}
