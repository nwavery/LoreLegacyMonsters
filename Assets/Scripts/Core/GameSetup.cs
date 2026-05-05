using UnityEngine;
using LoreLegacyMonsters.SaveLoad;

namespace LoreLegacyMonsters.Core
{
    public class GameSetup : MonoBehaviour
    {
        [SerializeField] GameManager gameManager;
        [SerializeField] SaveLoadManager saveLoad;

        void Start()
        {
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
            if (saveLoad == null) saveLoad = FindFirstObjectByType<SaveLoadManager>();
        }
    }
}
