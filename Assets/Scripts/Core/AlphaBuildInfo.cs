using System;
using System.IO;
using UnityEngine;

namespace LoreLegacyMonsters.Core
{
    /// <summary>Written to StreamingAssets/alpha_build_info.json by the alpha build pipeline.</summary>
    [Serializable]
    public class AlphaBuildInfoRecord
    {
        public string version;
        public string builtAtUtc;
        public string gitCommitShort;
        public string unityVersion;
    }

    public static class AlphaBuildInfo
    {
        const string FileName = "alpha_build_info.json";

        public static bool TryLoad(out AlphaBuildInfoRecord record)
        {
            record = null;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, FileName);
                if (!File.Exists(path)) return false;
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                record = JsonUtility.FromJson<AlphaBuildInfoRecord>(json);
                return record != null;
            }
            catch
            {
                return false;
            }
        }

        public static string FormatAboutText()
        {
            if (TryLoad(out var r) && r != null)
            {
                var git = string.IsNullOrWhiteSpace(r.gitCommitShort) ? "(unknown)" : r.gitCommitShort;
                return $"Version: {r.version}\nBuilt (UTC): {r.builtAtUtc}\nGit: {git}\nUnity: {r.unityVersion}";
            }

            return "Build: development / editor\n(No alpha_build_info.json in StreamingAssets — use a packaged alpha build for full build identity.)";
        }
    }
}
