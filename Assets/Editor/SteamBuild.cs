#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    public static class SteamBuild
    {
        const string DefaultOutputDir = "Build/Steam/Windows";
        const string ExecutableName = "LoreLegacyMonsters.exe";
        const string BuildInfoFileName = "build_info.json";
        const string SteamBuildArg = "-steamBuildNumber";
        const string VersionArg = "-bundleVersion";

        [MenuItem("Build/Steam/Windows Release (IL2CPP)")]
        public static void BuildWindowsReleaseMenu() => BuildWindowsRelease();

        public static void BuildWindowsRelease()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            var outputDir = Path.Combine(projectRoot, DefaultOutputDir);
            Directory.CreateDirectory(outputDir);

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("SteamBuild: No enabled scenes.");
                EditorApplication.Exit(1);
                return;
            }

            ApplyBuildPlayerSettings();
            WriteBuildInfo(projectRoot);

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outputDir, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.StrictMode
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"SteamBuild failed: {report.summary.result} ({report.summary.totalErrors} error(s)).");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            var builtExePath = Path.Combine(outputDir, ExecutableName);
            TryStampWindowsExeMetadata(projectRoot, builtExePath);

            Debug.Log($"SteamBuild: succeeded -> {builtExePath}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void ApplyBuildPlayerSettings()
        {
            var bundleVersion = GetArgValue(VersionArg);
            if (!string.IsNullOrWhiteSpace(bundleVersion))
                PlayerSettings.bundleVersion = bundleVersion.Trim();

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Standalone, Il2CppCompilerConfiguration.Release);
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.MTRendering = true;
            SteamBrandingProfile.ApplyBrandingProfile();
            SteamBrandingProfile.EnsureStandaloneSteamworksDefine();
        }

        static void WriteBuildInfo(string projectRoot)
        {
            var streamingAssets = Path.Combine(projectRoot, "Assets", "StreamingAssets");
            Directory.CreateDirectory(streamingAssets);
            var path = Path.Combine(streamingAssets, BuildInfoFileName);
            var dto = new BuildInfoRecord
            {
                version = PlayerSettings.bundleVersion,
                steamBuildNumber = GetArgValue(SteamBuildArg),
                builtAtUtc = DateTime.UtcNow.ToString("O"),
                gitCommitShort = TryGitShortSha(projectRoot),
                unityVersion = Application.unityVersion
            };
            File.WriteAllText(path, JsonUtility.ToJson(dto, true));
            AssetDatabase.Refresh();
            Debug.Log($"SteamBuild: wrote {path}");
        }

        static string GetArgValue(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return string.Empty;
        }

        static void TryStampWindowsExeMetadata(string projectRoot, string exePath)
        {
            var scriptPath = Path.Combine(projectRoot, "scripts", "Stamp-SteamWindowsExeMetadata.ps1");
            if (!File.Exists(scriptPath))
            {
                Debug.LogWarning($"SteamBuild: stamp script not found ({scriptPath}); skipping Windows EXE metadata.");
                return;
            }

            try
            {
                var version = PlayerSettings.bundleVersion.Trim();

                var args =
                    "-NoProfile -ExecutionPolicy Bypass " +
                    $"-File \"{scriptPath}\" " +
                    $"-ExePath \"{exePath}\" " +
                    $"-BundleVersion \"{version}\" " +
                    $"-ProjectPath \"{projectRoot}\"";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = projectRoot,
                };

                using var p = System.Diagnostics.Process.Start(psi);
                if (p == null)
                    return;

                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit(60000);

                foreach (var line in stdout.Trim().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    Debug.Log($"[StampSteamExe] {line}");

                if (p.ExitCode != 0)
                {
                    Debug.LogWarning(
                        $"SteamBuild: Windows EXE metadata stamping failed with exit code {p.ExitCode}. stderr: {stderr}");
                }
                else
                    Debug.Log("SteamBuild: Windows EXE metadata stamped.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SteamBuild: Windows EXE metadata stamping skipped (non-fatal): {ex.Message}");
            }
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

        [Serializable]
        sealed class BuildInfoRecord
        {
            public string version;
            public string steamBuildNumber;
            public string builtAtUtc;
            public string gitCommitShort;
            public string unityVersion;
        }
    }
}
#endif
