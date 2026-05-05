using System;
using System.IO;
using System.Linq;
using LoreLegacyMonsters.Core;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    /// <summary>
    /// Reproducible Windows standalone builds for internal alpha distribution.
    /// Batch: <c>-batchmode -quit -projectPath ... -executeMethod LoreLegacyMonsters.Editor.AlphaBuild.BuildWindowsPlayer</c>
    /// </summary>
    public static class AlphaBuild
    {
        const string DefaultRelativeOutputDir = "Build/Windows";
        const string ExecutableName = "LoreLegacyMonsters.exe";
        const string BuildInfoFileName = "alpha_build_info.json";

        [MenuItem("Build/Alpha/Windows Standalone (64-bit)")]
        public static void BuildWindowsPlayerMenu() => BuildWindowsPlayer();

        /// <summary>CI / PowerShell entrypoint.</summary>
        public static void BuildWindowsPlayer()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var outputDir = Path.Combine(projectRoot, DefaultRelativeOutputDir);
            Directory.CreateDirectory(outputDir);
            var locationPath = Path.Combine(outputDir, ExecutableName);

            WriteBuildManifest(projectRoot);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("AlphaBuild: No enabled scenes in EditorBuildSettings.");
                EditorApplication.Exit(1);
                return;
            }

            var options = BuildOptions.None;
            if (Application.isBatchMode)
                options |= BuildOptions.StrictMode;

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPath,
                target = BuildTarget.StandaloneWindows64,
                options = options
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"AlphaBuild failed: {report.summary.result} — {report.summary.totalErrors} error(s).");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            Debug.Log($"AlphaBuild: succeeded → {locationPath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void WriteBuildManifest(string projectRoot)
        {
            var streamingAssets = Path.Combine(projectRoot, "Assets", "StreamingAssets");
            Directory.CreateDirectory(streamingAssets);
            var path = Path.Combine(streamingAssets, BuildInfoFileName);

            var dto = new AlphaBuildInfoRecord
            {
                version = PlayerSettings.bundleVersion,
                builtAtUtc = DateTime.UtcNow.ToString("o"),
                gitCommitShort = TryGitShortSha(projectRoot),
                unityVersion = Application.unityVersion
            };

            File.WriteAllText(path, JsonUtility.ToJson(dto, true));
            AssetDatabase.Refresh();
            Debug.Log($"AlphaBuild: wrote {path}");
        }

        static string TryGitShortSha(string workingDir)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "rev-parse --short HEAD",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null) return string.Empty;
                var stdout = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(5000);
                return p.ExitCode == 0 ? stdout : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

    }
}
