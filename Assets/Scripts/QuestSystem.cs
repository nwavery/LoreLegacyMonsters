using UnityEngine;

namespace LoreLegacyMonsters
{
    /// <summary>Legacy facade; prefer QuestManager for runtime.</summary>
    public class QuestSystem : MonoBehaviour
    {
        [SerializeField] QuestManager manager;

        public QuestManager Manager
        {
            get
            {
                ResolveManager();
                return manager;
            }
        }

        void Awake() => ResolveManager();

        void Start() => ResolveManager();

        void ResolveManager()
        {
            if (manager == null) manager = GetComponent<QuestManager>();
            if (manager == null) manager = FindFirstObjectByType<QuestManager>();
        }
    }
}
