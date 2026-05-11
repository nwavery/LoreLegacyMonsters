using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>
    /// Registers the shipped GGUF with bundled <c>ollama</c> as <see cref="BundledModelTag"/> via <c>create -f Modelfile</c>.
    /// </summary>
    public static class BundledOllamaModelProvisioner
    {
        public const string BundledModelTag = "lore-bundled";
        public const string BundledGgufFileName = "llama3.2-q4_k_m.gguf";
        public const string BundledModelfileName = "Modelfile";

        /// <summary>First bundled <c>create</c> imports a GGUF; must not block Unity's main thread (player appears frozen).</summary>
        internal const float CreateExitPollRealtimeSeconds = 0.33f;

        internal const float CreateHardTimeoutRealtimeSeconds = 900f;

        /// <summary>PlayerPrefs model values that collide with desktop defaults but are wrong for the bundled GGUF-only layout.</summary>
        public static bool LooksLikeLegacyDesktopDefaultModel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var t = raw.Trim().ToLowerInvariant();
            return t == "llama3.2:latest" || t == "llama3.2" || t.StartsWith("llama3.2:");
        }

        /// <summary>Coroutine: skips when not bundled; ensures <see cref="BundledModelTag"/> exists in running daemon.</summary>
        public static IEnumerator EnsureBundledModelRegistered()
        {
            if (!LlmRuntimeSupervisor.IsBundledRuntimeEnabled())
                yield break;

#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
            LlmRuntimeSupervisor.AppendDiagnostic("Bundled model provision: unsupported platform (expect Windows standalone).");
            yield break;
#endif

            var hasModel = false;
            yield return QueryBundledTags(list => hasModel = ListResponseContainsBundled(list));
            if (hasModel)
                yield break;

            if (!CanAttemptCreate(out var prerequisiteError))
            {
                LlmRuntimeSupervisor.AppendDiagnostic($"Bundled model provision skipped: {prerequisiteError}");
                yield break;
            }

            if (!TrySpawnOllamaCreateProcess(out var createProc, out var spawnErr))
            {
                LlmRuntimeSupervisor.AppendDiagnostic($"Bundled ollama create failed: {spawnErr}");
            }
            else
            {
                LlmRuntimeSupervisor.AppendDiagnostic(
                    $"Bundled ollama create: child spawned; awaiting import (max {CreateHardTimeoutRealtimeSeconds:0}s)…");

                var elapsedRm = 0f;
                var nextLogRm = 45f;

                try
                {
                    while (!createProc.HasExited && elapsedRm < CreateHardTimeoutRealtimeSeconds)
                    {
                        yield return new WaitForSecondsRealtime(CreateExitPollRealtimeSeconds);
                        elapsedRm += CreateExitPollRealtimeSeconds;
                        if (elapsedRm >= nextLogRm && !createProc.HasExited)
                        {
                            LlmRuntimeSupervisor.AppendDiagnostic(
                                $"Bundled ollama create: still running (~{elapsedRm:0}s elapsed)…");
                            nextLogRm += 45f;
                        }
                    }

                    if (!createProc.HasExited)
                    {
                        try
                        {
                            createProc.Kill();
                        }
                        catch
                        {
                            // ignore
                        }

                        yield return new WaitForSecondsRealtime(0.75f);

                        LlmRuntimeSupervisor.AppendDiagnostic(
                            $"Bundled ollama create timed out after {CreateHardTimeoutRealtimeSeconds:0}s");
                    }
                    else
                    {
                        try
                        {
                            if (createProc.ExitCode != 0)
                                LlmRuntimeSupervisor.AppendDiagnostic(
                                    $"Bundled ollama create exit code {createProc.ExitCode}");
                            else
                                LlmRuntimeSupervisor.AppendDiagnostic(
                                    $"Bundled ollama create finished for {BundledModelTag}");
                        }
                        catch (Exception ex)
                        {
                            LlmRuntimeSupervisor.AppendDiagnostic(
                                $"Bundled ollama create: could not read exit code: {ex.Message}");
                        }
                    }
                }
                finally
                {
                    try
                    {
                        createProc.Dispose();
                    }
                    catch
                    {
                        // ignore
                    }
                }

                for (var i = 0; i < 10; i++)
                {
                    hasModel = false;
                    yield return QueryBundledTags(list => hasModel = ListResponseContainsBundled(list));
                    if (hasModel)
                        yield break;
                    yield return new WaitForSecondsRealtime(1f);
                }

                yield break;
            }

            for (var i = 0; i < 10; i++)
            {
                hasModel = false;
                yield return QueryBundledTags(list => hasModel = ListResponseContainsBundled(list));
                if (hasModel)
                    yield break;
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        static bool CanAttemptCreate(out string error)
        {
            error = null;
            if (!File.Exists(LlmRuntimeSupervisor.BundledRuntimeExePath))
            {
                error = $"missing bundled ollama at {LlmRuntimeSupervisor.BundledRuntimeExePath}";
                return false;
            }

            var modelsDir = LlmRuntimeSupervisor.BundledModelsDirectory;
            if (!File.Exists(Path.Combine(modelsDir, BundledModelfileName)))
            {
                error = $"missing {BundledModelfileName} in {modelsDir}";
                return false;
            }

            if (!File.Exists(Path.Combine(modelsDir, BundledGgufFileName)))
            {
                error = $"missing {BundledGgufFileName} in {modelsDir}";
                return false;
            }

            return true;
        }

        static IEnumerator QueryBundledTags(Action<string> onBody)
        {
            var url = LlmRuntimeSupervisor.BundledNativeTagsUrl;
            using var req = UnityWebRequest.Get(url);
            req.timeout = 10;
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            var ok = req.result == UnityWebRequest.Result.Success;
#else
            var ok = !(req.isNetworkError || req.isHttpError);
#endif
            if (!ok)
            {
                onBody?.Invoke(null);
                yield break;
            }

            var text = req.downloadHandler?.text;
            onBody?.Invoke(text);
        }

        public static bool ListResponseContainsBundled(string jsonBody)
        {
            if (string.IsNullOrEmpty(jsonBody)) return false;
            return jsonBody.IndexOf(BundledModelTag, StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Launches bundled <c>ollama.exe create …</c>. Caller waits for exit on a coroutine (do not block the main thread).
        /// </summary>
        static bool TrySpawnOllamaCreateProcess(out Process procOut, out string error)
        {
            procOut = null;
            error = null;

            var exe = LlmRuntimeSupervisor.BundledRuntimeExePath;
            var modelsDir = LlmRuntimeSupervisor.BundledModelsDirectory;
            var runtimeDir = Path.GetDirectoryName(exe);
            var modelfileAbs = Path.GetFullPath(Path.Combine(modelsDir, BundledModelfileName))
                .Replace('/', Path.DirectorySeparatorChar);
            var port = LlmRuntimeSupervisor.BundledPort;

            if (string.IsNullOrEmpty(runtimeDir) || !File.Exists(exe))
            {
                error = "invalid bundled ollama path";
                return false;
            }

            void ApplyBundledEnv(ProcessStartInfo psi)
            {
                psi.EnvironmentVariables["OLLAMA_HOST"] = $"127.0.0.1:{port}";
                psi.EnvironmentVariables["OLLAMA_MODELS"] = modelsDir;
            }

            bool TryStart(ProcessStartInfo psi, out string runError, out Process proc)
            {
                runError = null;
                proc = null;
                try
                {
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;
                    psi.RedirectStandardOutput = false;
                    psi.RedirectStandardError = false;
                    psi.WorkingDirectory = runtimeDir;
                    ApplyBundledEnv(psi);

                    var p = Process.Start(psi);
                    if (p == null)
                    {
                        runError = "Process.Start returned null";
                        return false;
                    }

                    proc = p;
                    return true;
                }
                catch (Exception ex)
                {
                    runError = $"{ex.GetType().Name}: {ex.Message}";
                    return false;
                }
            }

            var createArgs = $"create {BundledModelTag} -f \"{modelfileAbs}\"";
            var psiExe = new ProcessStartInfo { FileName = exe, Arguments = createArgs };

            if (!TryStart(psiExe, out error, out procOut))
            {
                var directStartFail = error;
                LlmRuntimeSupervisor.AppendDiagnostic(
                    $"Bundled ollama create: direct Process.Start failed ({directStartFail}); retrying via cmd.exe");

                var cmdExePath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                var cmdArgs = $"/c \"\"{exe}\" {createArgs}\"";
                var psiCmd = new ProcessStartInfo { FileName = cmdExePath, Arguments = cmdArgs };

                if (!TryStart(psiCmd, out error, out procOut))
                {
                    var cmdFail = error;
                    if (LlmRuntimeSupervisor.TrySpawnBundledOllamaCli(createArgs, out procOut, out var win32Err))
                    {
                        LlmRuntimeSupervisor.AppendDiagnostic(
                            "Bundled ollama create: CreateProcessW after Process.Start/cmd failed.");
                    }
                    else
                    {
                        error = $"{cmdFail} | Win32: {win32Err} | direct: {directStartFail}";
                        return false;
                    }
                }
            }

            error = null;
            return procOut != null;
        }
    }
}
