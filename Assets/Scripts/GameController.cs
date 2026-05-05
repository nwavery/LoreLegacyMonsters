using UnityEngine;
using LoreLegacyMonsters.SceneManagement;
using LoreLegacyMonsters.SaveLoad;
using LoreLegacyMonsters.Core;

namespace LoreLegacyMonsters
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] SceneLoader sceneLoader;
        [SerializeField] SaveLoadManager saveLoad;
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Game";

        public void NewGame(string playerName)
        {
            saveLoad = SaveLoadManager.EnsureExists();
            saveLoad.NewGame(playerName);
            sceneLoader ??= FindFirstObjectByType<SceneLoader>();
            sceneLoader?.Load(GameScene);
        }

        public void LoadGame(int slot)
        {
            saveLoad = SaveLoadManager.EnsureExists();
            string error = null;
            if (saveLoad != null && saveLoad.LoadSlot(slot, out error))
            {
                sceneLoader ??= FindFirstObjectByType<SceneLoader>();
                sceneLoader?.Load(GameScene);
            }
            else
            {
                GameEvents.RaiseToast(string.IsNullOrWhiteSpace(error)
                    ? $"Load failed for slot {slot}."
                    : $"Load failed for slot {slot}: {error}");
            }
        }

        public void ToMainMenu()
        {
            sceneLoader ??= FindFirstObjectByType<SceneLoader>();
            sceneLoader?.Load(MainMenuScene);
        }
    }
}
