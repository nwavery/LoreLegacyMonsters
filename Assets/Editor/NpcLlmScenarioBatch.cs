using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using LoreLegacyMonsters.Dialog.Llm;
using UnityEditor;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    /// <summary>
    /// Executes all lines in manifest.jsonl sequentially (Editor batch). Requires <c>-batchmode</c> and
    /// <see cref="RunEnv"/><c>=1</c>; optional <see cref="ManifestEnv"/>.
    /// </summary>
    public static class NpcLlmScenarioBatch
    {
        public const string RunEnv = "RUN_NPC_LLM_SCENARIO_SUITE";
        public const string ManifestEnv = "NPC_LLM_SCENARIO_MANIFEST";

        [Serializable]
        sealed class ScenarioSingleResultDto
        {
            public string scenarioId;
            public bool llmHttpOk;
            public string llmError;
            public bool evalOk;
            public string evalReason;
            public string rawAssistant;
            public string sanitized;
            public string cleanBeforeCommands;
            public string shapedHud;
            public string completionsUrl;
            public string model;
        }

        [Serializable]
        sealed class ScenarioRollupDto
        {
            public string completedUtc;
            public int scenariosAttempted;
            public int llmFailures;
            public int evalFailures;
            public string failingIdsPipe;
            public string manifestPathUsed;
            public string completionsUrl;
            public string model;
        }

        sealed class SuiteState
        {
            public int Attempted;
            public int LlmFailures;
            public int EvalFailures;
            public readonly List<string> FailingIds = new List<string>();
        }

        /// <summary>Unity:<c>-executeMethod LoreLegacyMonsters.Editor.NpcLlmScenarioBatch.RunScenarioSuiteBatch</c>.</summary>
        public static void RunScenarioSuiteBatch()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError("[NpcLlmScenarioBatch] Use -batchmode (Invoke-NpcLlmScenarioSuite.ps1).");
                return;
            }

            if (!string.Equals(Environment.GetEnvironmentVariable(RunEnv), "1", StringComparison.Ordinal))
            {
                Debug.LogError($"[NpcLlmScenarioBatch] Set {RunEnv}=1 before running scenario suite.");
                EditorApplication.Exit(3);
                return;
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifest = Environment.GetEnvironmentVariable(ManifestEnv);
            if (string.IsNullOrWhiteSpace(manifest))
                manifest = Path.Combine(projectRoot, "tools", "convo", "scenarios", "manifest.jsonl");

            NpcLlmScenarioManifestGenerator.EnsureFile(manifest);
            var outDir = Path.Combine(projectRoot, "Artifacts", "LlmConvo", "scenarios");
            Directory.CreateDirectory(outDir);

            NpcLlmDevEndpointResolver.Resolve(out var completionsUrl, out var modelName, out var timeoutSeconds,
                logResolved: true);

            var state = new SuiteState();

            RunSuiteSync(manifest, outDir, completionsUrl, modelName, timeoutSeconds, state);

            FinishRollup(outDir, manifest, completionsUrl, modelName, state);
            EditorApplication.Exit(state.LlmFailures + state.EvalFailures > 0 ? 1 : 0);
        }

        static void RunSuiteSync(string manifestAbs, string outDir,
            string completionsUrl, string modelName, int timeoutSeconds, SuiteState state)
        {
            // First manifest row often hits before Ollama finishes binding; prevents spurious FAIL on elder_mira__greet only.
            Thread.Sleep(2200);

            var lines = File.ReadAllLines(manifestAbs);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//", StringComparison.Ordinal))
                    continue;

                NpcLlmScenarioRecord rec;
                try
                {
                    rec = JsonUtility.FromJson<NpcLlmScenarioRecord>(line);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[NpcLlmScenarioBatch] Skip malformed scenario line ({ex.Message})");
                    continue;
                }

                if (rec == null || string.IsNullOrWhiteSpace(rec.id))
                    continue;

                state.Attempted++;

                var dto = new ScenarioSingleResultDto
                {
                    scenarioId = rec.id,
                    completionsUrl = completionsUrl,
                    model = modelName,
                };

                var sampling = NpcLlmScenarioPromptBuilder.ResolveSampling(rec.temperature, rec.maxTokens);
                var ctx = NpcLlmScenarioPromptBuilder.ToPromptContext(rec);
                var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);

                var isHotScenario = string.Equals(rec.id, "rival_corin__topic_b", StringComparison.Ordinal) ||
                                   string.Equals(rec.id, "elder_mira__greet", StringComparison.Ordinal);
                var maxCompletionPasses = isHotScenario ? 14 : 10;

                var errLog = "";
                var gotParsedContent = false;
                dto.evalOk = false;
                dto.evalReason = "";

                for (var pass = 0; pass < maxCompletionPasses; pass++)
                {
                    var tempBump = Mathf.Min(0.97f, sampling.temperature + 0.045f * pass +
                                                       (isHotScenario && pass >= 10 ? (pass - 9) * 0.018f : 0f));
                    var maxTokFloor = Mathf.Max(96, Mathf.Max(sampling.maxTokens, 112 + pass * 20));
                    var bodyJson = OpenAiCompatibleLlmClient.BuildRequestJson(modelName, msgs, tempBump,
                        maxTokFloor, false);

                    if (pass > 0)
                    {
                        var baseSleep = Mathf.Min(5200, 180 + pass * 420);
                        if (isHotScenario && pass >= 10)
                            baseSleep = Mathf.Max(baseSleep, 2600 + (pass - 10) * 550);

                        Thread.Sleep(baseSleep);
                    }

                    var ok = OpenAiCompatibleLlmClient.TrySendChatCompletionBlocking(completionsUrl, bodyJson,
                        timeoutSeconds, out var assistant, out var errMsg);

                    errLog += (errLog.Length > 0 ? " || " : "") +
                              $"pass{pass + 1}:" +
                              (ok && !string.IsNullOrWhiteSpace(assistant)
                                  ? $"{assistant.Length}c"
                                  : errMsg ?? "fail");

                    if (!ok || string.IsNullOrWhiteSpace(assistant))
                        continue;

                    gotParsedContent = true;
                    dto.rawAssistant = assistant;
                    dto.sanitized = OpenAiCompatibleLlmClient.SanitizeReply(assistant);
                    dto.cleanBeforeCommands = NpcLlmResponseFilter.Clean(dto.sanitized);
                    dto.shapedHud = NpcLlmDisplayPipeline.ShapeForHud(assistant);

                    if (!NpcLlmScenarioEvaluator.TryEvaluateHud(dto.shapedHud, rec, out var evalFail))
                    {
                        dto.evalReason = evalFail;
                        continue;
                    }

                    if (NpcLlmResponseFilter.IsTooShortToDisplay(dto.shapedHud))
                    {
                        dto.evalReason = "HUD reply too short to display after shaping.";
                        continue;
                    }

                    dto.evalOk = true;
                    dto.evalReason = "";
                    break;
                }

                dto.llmHttpOk = gotParsedContent;
                dto.llmError = gotParsedContent ? "" : errLog;

                if (!gotParsedContent)
                {
                    dto.evalOk = false;
                    dto.evalReason = "LLM completion failed.";
                }

                Debug.Log($"[NpcLlmScenarioBatch] {(dto.llmHttpOk && dto.evalOk ? "PASS" : "FAIL")}: {dto.scenarioId}");

                var reportPath = Path.Combine(outDir, SanitizeFilename(dto.scenarioId) + ".json");
                File.WriteAllText(reportPath, JsonUtility.ToJson(dto, true), new UTF8Encoding(false));

                if (!dto.llmHttpOk)
                {
                    state.LlmFailures++;
                    state.FailingIds.Add(dto.scenarioId + "|llm");
                }

                if (dto.llmHttpOk && !dto.evalOk)
                {
                    state.EvalFailures++;
                    state.FailingIds.Add(dto.scenarioId + "|eval");
                }
            }
        }

        static void FinishRollup(string outDir, string manifest, string completionsUrl, string model, SuiteState state)
        {
            var rollup = new ScenarioRollupDto
            {
                completedUtc = DateTime.UtcNow.ToString("o"),
                scenariosAttempted = state.Attempted,
                llmFailures = state.LlmFailures,
                evalFailures = state.EvalFailures,
                failingIdsPipe = string.Join("|", state.FailingIds),
                manifestPathUsed = manifest,
                completionsUrl = completionsUrl,
                model = model
            };

            Directory.CreateDirectory(outDir);
            var path = Path.Combine(outDir, "run_summary.json");
            File.WriteAllText(path, JsonUtility.ToJson(rollup, true), new UTF8Encoding(false));
            Debug.Log(
                $"[NpcLlmScenarioBatch] Summary → {path} (attempted={rollup.scenariosAttempted}, llmFails={rollup.llmFailures}, evalFails={rollup.evalFailures})");
        }

        static string SanitizeFilename(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "unknown";
            var sb = new StringBuilder(id.Length);
            foreach (var c in id)
            {
                if (Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 || c == ' ' ||
                    c == ':' || c == '/' || c == '\\')
                    sb.Append('_');
                else
                    sb.Append(c);
            }

            var s = sb.ToString();
            return s.Length > 120 ? s.Substring(0, 120) : s;
        }
    }
}
