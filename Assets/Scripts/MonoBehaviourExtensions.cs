using UnityEngine;

namespace LoreLegacyMonsters
{
    public static class MonoBehaviourExtensions
    {
        public static T GetOrAddComponent<T>(this Component c) where T : Component
        {
            if (c == null) return null;
            var t = c.GetComponent<T>();
            return t != null ? t : c.gameObject.AddComponent<T>();
        }

        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            if (go == null) return null;
            var t = go.GetComponent<T>();
            return t != null ? t : go.AddComponent<T>();
        }
    }
}
