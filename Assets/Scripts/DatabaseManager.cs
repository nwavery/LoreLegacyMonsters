using System.Collections.Generic;
using UnityEngine;

namespace LoreLegacyMonsters
{
    /// <summary>Lightweight JSON-backed registry (no SQLite).</summary>
    public class DatabaseManager : MonoBehaviour
    {
        [SerializeField] TextAsset itemTableJson;

        readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        void Awake()
        {
            if (itemTableJson != null)
                cache["items_raw"] = itemTableJson.text;
        }

        public bool TryGetBlob(string key, out string json)
        {
            return cache.TryGetValue(key, out json);
        }

        public void SetBlob(string key, string json) => cache[key] = json;
    }
}
