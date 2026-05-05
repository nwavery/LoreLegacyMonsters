using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    [InitializeOnLoad]
    public static class BatchAutomationBootstrap
    {
        const string ExecutedKey = "LoreLegacyMonsters.BatchAutomationBootstrap.Executed";
        const string TaskKey = "LoreLegacyMonsters.BatchAutomationBootstrap.Task";
        static readonly Dictionary<string, Action> TaskHandlers = new(StringComparer.OrdinalIgnoreCase)
        {
            { "edit-tests", BatchEditModeTestRunner.Run },
            { "smoke-full", VisualSmokeCapture.CaptureFullSuiteBatch },
            { "smoke-main-menu", VisualSmokeCapture.CaptureMainMenu },
            { "smoke-tour", VisualSmokeCapture.CaptureOverworldTourBatch },
            { "steam-build", SteamBuild.BuildWindowsRelease }
        };

        static BatchAutomationBootstrap()
        {
            if (!Application.isBatchMode)
                return;

            var task = GetArgValue("-batchTask");
            if (!string.IsNullOrWhiteSpace(task))
                SessionState.SetString(TaskKey, task.Trim().ToLowerInvariant());

            if (SessionState.GetBool(ExecutedKey, false))
                return;

            EditorApplication.update -= TryDispatchWhenReady;
            EditorApplication.update += TryDispatchWhenReady;
        }

        static void TryDispatchWhenReady()
        {
            if (SessionState.GetBool(ExecutedKey, false))
            {
                EditorApplication.update -= TryDispatchWhenReady;
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            var task = SessionState.GetString(TaskKey, string.Empty);
            if (string.IsNullOrWhiteSpace(task))
            {
                EditorApplication.update -= TryDispatchWhenReady;
                return;
            }

            SessionState.SetBool(ExecutedKey, true);
            EditorApplication.update -= TryDispatchWhenReady;
            Dispatch(task);
        }

        static void Dispatch(string task)
        {
            try
            {
                Debug.Log($"[BATCH-AUTO] Dispatching task: {task}");
                if (!TaskHandlers.TryGetValue(task, out var action))
                {
                    Debug.LogError($"[BATCH-AUTO] Unknown -batchTask value: {task}");
                    EditorApplication.Exit(2);
                    return;
                }

                action();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(3);
            }
            finally
            {
                SessionState.EraseString(TaskKey);
            }
        }

        static string GetArgValue(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    continue;
                return args[i + 1];
            }

            return string.Empty;
        }
    }
}
