using System.Collections;
using UnityEngine;

namespace LoreLegacyMonsters.SceneManagement
{
    public class SceneTransitionHandler : MonoBehaviour
    {
        [SerializeField] SceneLoader loader;

        public IEnumerator TransitionTo(string sceneName)
        {
            yield return new WaitForSecondsRealtime(0.05f);
            if (loader != null) yield return loader.LoadAsync(sceneName);
        }
    }
}
