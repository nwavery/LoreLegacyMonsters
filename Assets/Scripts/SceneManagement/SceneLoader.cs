using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoreLegacyMonsters.SceneManagement
{
    public class SceneLoader : MonoBehaviour
    {
        public void Load(string sceneName)
        {
            if (UIManager.Instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            UIManager.Instance.StartCoroutine(LoadAsync(sceneName));
        }

        public IEnumerator LoadAsync(string sceneName)
        {
            var ui = UIManager.Instance != null ? UIManager.Instance : FindFirstObjectByType<UIManager>();
            ui?.BeginLoading($"Loading {sceneName}...");
            yield return null;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            yield return null;
            ui?.EndLoading();
        }
    }
}
