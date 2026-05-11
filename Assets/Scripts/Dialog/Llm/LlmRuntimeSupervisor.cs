using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LoreLegacyMonsters.Dialog.Llm
{
    /// <summary>
    /// Optional supervisor for a bundled local Ollama runtime shipped with the game build.
    /// It starts a local process on a non-default port and tears it down on app quit.
    /// </summary>
    public static class LlmRuntimeSupervisor
    {
        public const int DefaultBundledPort = 11436;
        static readonly object Sync = new object();
        static Process process;
        static bool quitHooked;
        static string lastFailure;
        static DateTime nextRetryUtc = DateTime.MinValue;
        static LlmRuntimeSupervisorDriver driver;

        public static bool IsBundledRuntimeEnabled()
        {
            if (LlmGlobalPreferences.HasBundledRuntimePreference())
                return LlmGlobalPreferences.IsBundledRuntimeEnabled(false);
            return HasBundledRuntimeStaged();
        }

        public static int BundledPort => LlmGlobalPreferences.GetBundledRuntimePort(DefaultBundledPort);

        public static string BundledBaseUrl => $"http://127.0.0.1:{BundledPort}/v1";

        /// <summary>OpenAI-compatible tags are under <c>/v1</c>; native Ollama list is <c>/api/tags</c>.</summary>
        public static string BundledNativeTagsUrl => $"http://127.0.0.1:{BundledPort}/api/tags";

        public static string BundledRuntimeExePath =>
            Path.GetFullPath(Path.Combine(GetRuntimeDirectory(), "ollama.exe"))
                .Replace('/', Path.DirectorySeparatorChar);

        public static string BundledModelsDirectory =>
            Path.GetFullPath(Path.Combine(GetLlmRootDirectory(), "models"))
                .Replace('/', Path.DirectorySeparatorChar);

        /// <summary>True when something is listening on <see cref="BundledPort"/> (e.g. bundled <c>ollama serve</c>).</summary>
        public static bool IsBundledListenerReachable() => IsBundledEndpointReachable();

        public static bool IsProcessAlive
        {
            get
            {
                lock (Sync)
                    return process != null && !process.HasExited;
            }
        }

        public static string LastFailure => lastFailure;

        public static void EnsureStarted()
        {
            if (!IsBundledRuntimeEnabled())
                return;
            if (IsExternalRuntimeManaged())
                return;

            HookQuitOnce();
            EnsureDriver();

            if (IsBundledListenerReachable())
            {
                lastFailure = null;
                nextRetryUtc = DateTime.MinValue;
                return;
            }

            if (DateTime.UtcNow < nextRetryUtc)
                return;

            lock (Sync)
            {
                if (process != null && !process.HasExited)
                    return;

                var runtimeDir = Path.GetFullPath(GetRuntimeDirectory())
                    .Replace('/', Path.DirectorySeparatorChar);
                var exePath = BundledRuntimeExePath;
                var modelsDir = BundledModelsDirectory;

                try
                {
                    if (!File.Exists(exePath))
                    {
                        lastFailure = $"Bundled runtime missing: {exePath}";
                        WriteSupervisorLog(lastFailure);
                        return;
                    }

                    Directory.CreateDirectory(modelsDir);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(exePath) ?? runtimeDir
                    };
                    startInfo.EnvironmentVariables["OLLAMA_HOST"] = $"127.0.0.1:{BundledPort}";
                    startInfo.EnvironmentVariables["OLLAMA_MODELS"] = modelsDir;

                    WriteSupervisorLog($"Starting bundled Ollama. exe={exePath}; cwd={startInfo.WorkingDirectory}; models={modelsDir}");
                    process = Process.Start(startInfo);
                    if (process == null)
                        throw new InvalidOperationException("Process.Start returned null for bundled Ollama.");
                    lastFailure = null;
                    nextRetryUtc = DateTime.MinValue;
                    WriteSupervisorLog($"Started bundled Ollama at 127.0.0.1:{BundledPort}");
                }
                catch (Exception ex)
                {
                    try
                    {
                        Environment.SetEnvironmentVariable("OLLAMA_HOST", $"127.0.0.1:{BundledPort}");
                        Environment.SetEnvironmentVariable("OLLAMA_MODELS", modelsDir);
                        var cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");
                        var shellStart = new ProcessStartInfo
                        {
                            FileName = cmdPath,
                            Arguments = $"/c \"\"{exePath}\" serve\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetDirectoryName(exePath) ?? runtimeDir
                        };
                        WriteSupervisorLog($"Primary start failed ({ex.GetType().Name}: {ex.Message}). Trying cmd fallback.");
                        process = Process.Start(shellStart);
                        if (process != null)
                        {
                            lastFailure = null;
                            nextRetryUtc = DateTime.MinValue;
                            WriteSupervisorLog($"Started bundled Ollama with shell fallback at 127.0.0.1:{BundledPort}");
                            return;
                        }

                        if (TryCreateProcessWin32(exePath, runtimeDir, "serve", out var nativeProcess, out var nativeError))
                        {
                            process = nativeProcess;
                            lastFailure = null;
                            nextRetryUtc = DateTime.MinValue;
                            WriteSupervisorLog($"Started bundled Ollama with CreateProcessW fallback at 127.0.0.1:{BundledPort}");
                            return;
                        }
                        WriteSupervisorLog($"CreateProcessW fallback failed: {nativeError}");
                    }
                    catch (Exception fallbackEx)
                    {
                        if (TryCreateProcessWin32(exePath, runtimeDir, "serve", out var nativeProcess, out var nativeError))
                        {
                            process = nativeProcess;
                            lastFailure = null;
                            nextRetryUtc = DateTime.MinValue;
                            WriteSupervisorLog($"Started bundled Ollama with CreateProcessW fallback at 127.0.0.1:{BundledPort}");
                            return;
                        }

                        lastFailure = $"{ex.GetType().Name}: {ex.Message} | fallback {fallbackEx.GetType().Name}: {fallbackEx.Message}";
                        WriteSupervisorLog($"Failed to start bundled runtime: {lastFailure} | native {nativeError}");
                        nextRetryUtc = DateTime.UtcNow.AddSeconds(30);
                        process = null;
                        return;
                    }

                    process = null;
                    lastFailure = $"{ex.GetType().Name}: {ex.Message}";
                    WriteSupervisorLog($"Failed to start bundled runtime: {lastFailure}");
                    nextRetryUtc = DateTime.UtcNow.AddSeconds(30);
                }
            }
        }

        public static void Stop()
        {
            lock (Sync)
            {
                if (process == null)
                    return;

                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (Exception ex)
                {
                    WriteSupervisorLog($"Failed to stop bundled runtime: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                    process = null;
                    nextRetryUtc = DateTime.MinValue;
                }
            }
        }

        public static void Tick()
        {
            if (!IsBundledRuntimeEnabled())
                return;

            if (IsBundledListenerReachable())
                return;

            if (IsProcessAlive)
                return;

            EnsureStarted();
        }

        static void HookQuitOnce()
        {
            if (quitHooked)
                return;
            quitHooked = true;
            Application.quitting += Stop;
        }

        static void EnsureDriver()
        {
            if (driver != null)
                return;

            var go = new GameObject("LlmRuntimeSupervisorDriver");
            UnityEngine.Object.DontDestroyOnLoad(go);
            driver = go.AddComponent<LlmRuntimeSupervisorDriver>();
        }

        static string GetLlmRootDirectory() =>
            Path.Combine(Application.streamingAssetsPath, "llm");

        static string GetRuntimeDirectory() =>
            Path.Combine(GetLlmRootDirectory(), "runtime");

        static bool HasBundledRuntimeStaged()
        {
            var exePath = Path.Combine(GetRuntimeDirectory(), "ollama.exe");
            return File.Exists(exePath);
        }

        static string GetSupervisorLogPath()
        {
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Application.companyName,
                Application.productName,
                "Logs");
            Directory.CreateDirectory(root);
            return Path.Combine(root, "llm-supervisor.log");
        }

        static bool IsBundledEndpointReachable()
        {
            try
            {
                using var client = new TcpClient();
                var task = client.ConnectAsync("127.0.0.1", BundledPort);
                var connected = task.Wait(TimeSpan.FromMilliseconds(200));
                return connected && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        static bool IsExternalRuntimeManaged()
        {
            try
            {
                return string.Equals(
                    Environment.GetEnvironmentVariable("LLM_EXTERNAL_RUNTIME"),
                    "1",
                    StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Diagnostics for bundled model provisioning; same file as supervisor boot.</summary>
        public static void AppendDiagnostic(string message) => WriteSupervisorLog(message);

        /// <summary>
        /// Spawns bundled <c>ollama.exe</c> with CLI <paramref name="arguments"/> via <c>CreateProcessW</c>
        /// (same native path used when <see cref="Process.Start"/> rejects <c>serve</c> in the player).
        /// Temporarily assigns <c>OLLAMA_HOST</c>/<c>OLLAMA_MODELS</c> so the child inherits them.
        /// </summary>
        public static bool TrySpawnBundledOllamaCli(string arguments, out Process started, out string error)
        {
            started = null;
            error = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var exePath = BundledRuntimeExePath;
            var runtimeDir = Path.GetFullPath(GetRuntimeDirectory()).Replace('/', Path.DirectorySeparatorChar);
            if (!File.Exists(exePath))
            {
                error = $"Bundled runtime missing: {exePath}";
                return false;
            }

            var prevHost = SafeGetEnvironmentVariable("OLLAMA_HOST");
            var prevModels = SafeGetEnvironmentVariable("OLLAMA_MODELS");
            try
            {
                Environment.SetEnvironmentVariable("OLLAMA_HOST", $"127.0.0.1:{BundledPort}");
                Environment.SetEnvironmentVariable("OLLAMA_MODELS", BundledModelsDirectory);
                return TryCreateProcessWin32(exePath, runtimeDir, arguments, out started, out error);
            }
            finally
            {
                RestoreEnvironmentVariable("OLLAMA_HOST", prevHost);
                RestoreEnvironmentVariable("OLLAMA_MODELS", prevModels);
            }
#else
            error = "Bundled CLI spawn requires Windows standalone/editor.";
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        static string SafeGetEnvironmentVariable(string key)
        {
            try { return Environment.GetEnvironmentVariable(key); }
            catch { return null; }
        }

        static void RestoreEnvironmentVariable(string key, string previous)
        {
            try
            {
                if (previous == null)
                    Environment.SetEnvironmentVariable(key, null);
                else
                    Environment.SetEnvironmentVariable(key, previous);
            }
            catch
            {
                // ignore
            }
        }
#endif

        static void WriteSupervisorLog(string message)
        {
            try
            {
                File.AppendAllText(GetSupervisorLogPath(), $"[{DateTime.UtcNow:O}] {message}\n");
            }
            catch
            {
                // Logging should never crash gameplay.
            }
        }

        static bool TryCreateProcessWin32(string exePath, string runtimeDir, string arguments, out Process started, out string error)
        {
            started = null;
            error = string.Empty;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf<STARTUPINFO>();
            var processInfo = new PROCESS_INFORMATION();
            arguments ??= string.Empty;
            arguments = arguments.TrimStart();
            var commandLine = string.IsNullOrEmpty(arguments)
                ? $"\"{exePath}\""
                : $"\"{exePath}\" {arguments}";
            const uint CREATE_NO_WINDOW = 0x08000000;
            var ok = CreateProcessW(
                lpApplicationName: exePath,
                lpCommandLine: commandLine,
                lpProcessAttributes: IntPtr.Zero,
                lpThreadAttributes: IntPtr.Zero,
                bInheritHandles: false,
                dwCreationFlags: CREATE_NO_WINDOW,
                lpEnvironment: IntPtr.Zero,
                lpCurrentDirectory: runtimeDir,
                lpStartupInfo: ref startupInfo,
                lpProcessInformation: out processInfo);

            if (!ok)
            {
                error = $"CreateProcessW failed with Win32 error {Marshal.GetLastWin32Error()}";
                return false;
            }

            try
            {
                started = Process.GetProcessById((int)processInfo.dwProcessId);
                return started != null;
            }
            catch (Exception ex)
            {
                error = $"Process.GetProcessById failed: {ex.Message}";
                return false;
            }
            finally
            {
                if (processInfo.hThread != IntPtr.Zero)
                    CloseHandle(processInfo.hThread);
                if (processInfo.hProcess != IntPtr.Zero)
                    CloseHandle(processInfo.hProcess);
            }
#else
            error = "CreateProcessW fallback is only supported on Windows targets.";
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool CreateProcessW(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }
#endif
    }

    sealed class LlmRuntimeSupervisorDriver : MonoBehaviour
    {
        float timer;

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer < 1f)
                return;
            timer = 0f;
            LlmRuntimeSupervisor.Tick();
        }
    }
}
