using System.Collections.Generic;
using System.Linq;

namespace LoreLegacyMonsters.Core
{
    public static class StoryFlags
    {
        const string KeyPrefix = "kv::";
        static readonly HashSet<string> Flags = new HashSet<string>();

        public static void SetFlag(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                Flags.Add(id);
        }

        public static bool HasFlag(string id) => !string.IsNullOrWhiteSpace(id) && Flags.Contains(id);

        public static void Clear(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
                Flags.Remove(id);
        }

        public static void SetValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            ClearValue(key);
            if (value == null) return;
            Flags.Add($"{KeyPrefix}{key}={value.Trim()}");
        }

        public static bool TryGetValue(string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(key)) return false;
            var prefix = $"{KeyPrefix}{key}=";
            var match = Flags.FirstOrDefault(f => f.StartsWith(prefix));
            if (string.IsNullOrEmpty(match)) return false;
            value = match.Substring(prefix.Length);
            return true;
        }

        public static string GetValue(string key, string defaultValue = "")
        {
            return TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return int.TryParse(GetValue(key), out var parsed) ? parsed : defaultValue;
        }

        public static void SetInt(string key, int value) => SetValue(key, value.ToString());

        public static int AddInt(string key, int delta, int min = int.MinValue, int max = int.MaxValue)
        {
            var current = GetInt(key);
            current += delta;
            if (current < min) current = min;
            if (current > max) current = max;
            SetInt(key, current);
            return current;
        }

        static void ClearValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            var prefix = $"{KeyPrefix}{key}=";
            foreach (var id in Flags.Where(f => f.StartsWith(prefix)).ToArray())
                Flags.Remove(id);
        }

        public static void ApplySave(IEnumerable<string> ids)
        {
            Flags.Clear();
            if (ids == null) return;
            foreach (var id in ids)
                SetFlag(id);
        }

        public static List<string> ExportSave() => new List<string>(Flags);
    }
}
