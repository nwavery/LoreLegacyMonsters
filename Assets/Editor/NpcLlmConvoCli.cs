using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using LoreLegacyMonsters;
using LoreLegacyMonsters.Dialog.Llm;
using UnityEditor;
using UnityEngine;

namespace LoreLegacyMonsters.Editor
{
    /// <summary>
    /// Batch NPC LLM turns using shipped prompt logic. Set env <c>NPC_LLM_CONVO_REQUEST</c>
    /// / <c>NPC_LLM_CONVO_RESPONSE</c> or rely on defaults under <c>tools/convo/request.json</c>, then launch Unity with:
    /// <c>-batchmode -nographics -quit -executeMethod LoreLegacyMonsters.Editor.NpcLlmConvoCli.RunOneTurnBatch</c>
    /// Endpoint resolution matches integration tests (<see cref="NpcLlmDevEndpointResolver"/>).
    /// </summary>
    public static class NpcLlmConvoCli
    {
        public const string RequestEnv = "NPC_LLM_CONVO_REQUEST";
        public const string ResponseEnv = "NPC_LLM_CONVO_RESPONSE";

        [Serializable]
        sealed class NpcLlmConvoRequestDto
        {
            public string playerMessage;
            public string conversationHistorySummary;
            public string npcRole;
            public string npcId;
            public string displayName;
            public string roleName;
            public string characterInstructions;
            public string identitySummary;
            public string gameStateSummary;
            public string questSummary;
            public string inventorySummary;
            public string partySummary;
            public string weatherSummary;
            public string npcMemorySummary;
            public string storyStateSummary;
            public string statusEffectsSummary;
            public string shopStockSummary;
            public string playerGearSummary;
            public string playerVibeTags;
            /// <summary>Optional; omit or blank to inherit <see cref="NpcLlmSettings"/></summary>
            public string temperature;
            /// <summary>Optional; omit or blank to inherit <see cref="NpcLlmSettings"/></summary>
            public string maxTokens;
        }

        [Serializable]
        sealed class NpcLlmConvoResponseDto
        {
            public bool ok;
            public string error;
            public string rawAssistant;
            public string sanitized;
            public string cleanBeforeCommands;
            public string cleanedForUi;
            public string completionsUrl;
            public string model;
            public int timeoutSecondsUsed;
            public float temperatureUsed;
            public int maxTokensUsed;
        }

        struct ParsedSampling
        {
            public float temperature;
            public int maxTokens;
        }

        static bool pumping;
        static int exitPending = -1;

        public static void RunOneTurnBatch()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError("[NpcLlmConvoCli] RunOneTurnBatch expects -batchmode (use Tools menu runner).");
                return;
            }

            if (pumping)
                return;

            pumping = true;
            exitPending = 1;

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            IEnumerator routine = BuildRoutineStarting(projectRoot);

            EditorApplication.CallbackFunction updater = null;
            updater = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= updater;
                        pumping = false;
                        ScheduleExit();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    pumping = false;
                    exitPending = 1;
                    var fallbackResp = Path.Combine(projectRoot, "Artifacts", "LlmConvo", "last-response.json");
                    TryWriteFatal(fallbackResp, ex.Message);
                    EditorApplication.update -= updater;
                    ScheduleExit();
                }
            };

            EditorApplication.update += updater;
        }

        static IEnumerator BuildRoutineStarting(string projectRoot)
        {
            var reqPath = Environment.GetEnvironmentVariable(RequestEnv);
            if (string.IsNullOrWhiteSpace(reqPath))
                reqPath = Path.Combine(projectRoot, "tools", "convo", "request.json");

            var resPath = Environment.GetEnvironmentVariable(ResponseEnv);
            if (string.IsNullOrWhiteSpace(resPath))
                resPath = Path.Combine(projectRoot, "Artifacts", "LlmConvo", "last-response.json");

            return RunTurnRoutine(reqPath, resPath);
        }

        [MenuItem("Tools/Lore Legacy/NPC LLM/Conversation turn (tools/convo/request.json → Artifacts/LlmConvo/)")]
        static void MenuRunInteractive()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var req = Path.Combine(root, "tools", "convo", "request.json");
            var res = Path.Combine(root, "Artifacts", "LlmConvo", "last-response.json");
            var routine = RunTurnRoutine(req, res);

            EditorApplication.CallbackFunction updater = null;
            updater = () =>
            {
                try
                {
                    if (!routine.MoveNext())
                    {
                        EditorApplication.update -= updater;
                        Debug.Log($"[NpcLlmConvoCli] Wrote → {res}");
                        if (File.Exists(res))
                            EditorUtility.RevealInFinder(Path.GetFullPath(res));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    EditorApplication.update -= updater;
                }
            };

            EditorApplication.update += updater;
        }

        static IEnumerator RunTurnRoutine(string requestPathAbs, string responsePathAbs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(responsePathAbs) ?? ".");

            NpcLlmDevEndpointResolver.Resolve(out var completionsUrl, out var model, out var timeoutSeconds, logResolved: true);

            string jsonReq;
            try
            {
                jsonReq = File.ReadAllText(requestPathAbs);
            }
            catch (Exception ex)
            {
                TryWriteFatal(responsePathAbs, $"Unable to read '{requestPathAbs}': {ex.Message}");
                yield break;
            }

            NpcLlmConvoRequestDto dto;
            try
            {
                dto = JsonUtility.FromJson<NpcLlmConvoRequestDto>(jsonReq);
                if (dto == null)
                    dto = new NpcLlmConvoRequestDto();
            }
            catch (Exception ex)
            {
                TryWriteFatal(responsePathAbs, $"Invalid JSON ({requestPathAbs}): {ex.Message}");
                yield break;
            }

            NormalizeDto(dto, out var sampling);
            var ctx = BuildContext(dto);

            var msgs = NpcLlmPromptBuilder.BuildMessages(ctx);
            var body = OpenAiCompatibleLlmClient.BuildRequestJson(model, msgs, sampling.temperature,
                Mathf.Max(8, sampling.maxTokens), false);

            var ok = false;
            string assistant = null;
            string errMsg = null;
            yield return OpenAiCompatibleLlmClient.SendChatCompletion(completionsUrl, body, timeoutSeconds,
                (success, text) =>
                {
                    ok = success;
                    if (success) assistant = text;
                    else errMsg = text ?? "completion failed";
                });

            var outDto = new NpcLlmConvoResponseDto
            {
                ok = ok,
                error = ok ? "" : errMsg,
                rawAssistant = ok ? assistant : "",
                sanitized = "",
                cleanBeforeCommands = "",
                cleanedForUi = "",
                completionsUrl = completionsUrl,
                model = model,
                timeoutSecondsUsed = timeoutSeconds,
                temperatureUsed = sampling.temperature,
                maxTokensUsed = sampling.maxTokens,
            };

            if (ok)
            {
                outDto.sanitized = OpenAiCompatibleLlmClient.SanitizeReply(assistant);
                outDto.cleanBeforeCommands = NpcLlmResponseFilter.Clean(outDto.sanitized);
                outDto.cleanedForUi = NpcLlmDisplayPipeline.ShapeForHud(assistant);
            }

            File.WriteAllText(responsePathAbs, JsonUtility.ToJson(outDto, true), new UTF8Encoding(false));
            Debug.Log($"[NpcLlmConvoCli] {(ok ? "Ok" : "Failed")}: {responsePathAbs}");

            exitPending = ok ? 0 : 1;

            Console.WriteLine("\n--- cleanedForUi ---\n" + outDto.cleanedForUi + "\n--------------------\n");
            if (!ok && !string.IsNullOrWhiteSpace(outDto.error))
                Console.WriteLine("[NpcLlmConvoCli] error: " + outDto.error);
        }

        static void NormalizeDto(NpcLlmConvoRequestDto s, out ParsedSampling sampling)
        {
            if (string.IsNullOrWhiteSpace(s.conversationHistorySummary)) s.conversationHistorySummary = "none";
            if (string.IsNullOrWhiteSpace(s.npcRole)) s.npcRole = NpcRole.Ambient.ToString();
            if (string.IsNullOrWhiteSpace(s.npcId)) s.npcId = "convo_test_npc";
            if (string.IsNullOrWhiteSpace(s.displayName)) s.displayName = "Trail scout";
            if (string.IsNullOrWhiteSpace(s.characterInstructions))
                s.characterInstructions = "Be concise and grounded; stay fully in-character.";
            if (string.IsNullOrWhiteSpace(s.identitySummary)) s.identitySummary = "Frontier guide NPC.";
            if (string.IsNullOrWhiteSpace(s.gameStateSummary)) s.gameStateSummary = "area: outskirts of Hollowfen";
            if (string.IsNullOrWhiteSpace(s.questSummary)) s.questSummary = "Explore and pick up rumours.";
            if (string.IsNullOrWhiteSpace(s.inventorySummary)) s.inventorySummary = "basic supplies";
            if (string.IsNullOrWhiteSpace(s.partySummary)) s.partySummary = "single companion-ready party";
            if (string.IsNullOrWhiteSpace(s.weatherSummary)) s.weatherSummary = "misty dawn";
            if (string.IsNullOrWhiteSpace(s.npcMemorySummary)) s.npcMemorySummary = "none";
            if (string.IsNullOrWhiteSpace(s.storyStateSummary)) s.storyStateSummary = "sandbox";
            if (string.IsNullOrWhiteSpace(s.statusEffectsSummary)) s.statusEffectsSummary = "none notable";
            if (string.IsNullOrWhiteSpace(s.shopStockSummary)) s.shopStockSummary = "no shop";

            var asset = NpcLlmSettings.LoadFromResources();

            sampling = new ParsedSampling
            {
                temperature = asset != null ? asset.Temperature : 0.45f,
                maxTokens = asset != null ? asset.MaxTokens : 256
            };

            if (!string.IsNullOrWhiteSpace(s.temperature) &&
                float.TryParse(s.temperature.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture,
                    out var tf))
                sampling.temperature = tf;

            if (!string.IsNullOrWhiteSpace(s.maxTokens) &&
                int.TryParse(s.maxTokens.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi) &&
                mi >= 16)
                sampling.maxTokens = mi;

            if (!Enum.TryParse<NpcRole>(s.npcRole, true, out _))
                s.npcRole = NpcRole.Ambient.ToString();

            if (string.IsNullOrWhiteSpace(s.roleName))
            {
                Enum.TryParse<NpcRole>(s.npcRole, true, out var r);
                s.roleName = r.ToString();
            }
        }

        static NpcLlmPromptContext BuildContext(NpcLlmConvoRequestDto s)
        {
            if (!Enum.TryParse<NpcRole>(s.npcRole, true, out var role))
                role = NpcRole.Ambient;

            return new NpcLlmPromptContext
            {
                NpcId = s.npcId,
                DisplayName = s.displayName,
                RoleName = s.roleName,
                Role = role,
                CharacterInstructions = s.characterInstructions,
                IdentitySummary = s.identitySummary,
                PlayerMessage = string.IsNullOrWhiteSpace(s.playerMessage) ? null : s.playerMessage.Trim(),
                GameStateSummary = s.gameStateSummary,
                QuestSummary = s.questSummary,
                InventorySummary = s.inventorySummary,
                PartySummary = s.partySummary,
                WeatherSummary = s.weatherSummary,
                NpcMemorySummary = s.npcMemorySummary,
                ConversationHistorySummary = s.conversationHistorySummary,
                StoryStateSummary = s.storyStateSummary,
                StatusEffectsSummary = s.statusEffectsSummary,
                ShopStockSummary = s.shopStockSummary,
                PlayerGearSummary = string.IsNullOrWhiteSpace(s.playerGearSummary) ? "" : s.playerGearSummary,
                PlayerVibeTags = string.IsNullOrWhiteSpace(s.playerVibeTags) ? "" : s.playerVibeTags,
            };
        }

        static void TryWriteFatal(string responsePathAbs, string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(responsePathAbs) ?? ".");
                var dto = new NpcLlmConvoResponseDto { ok = false, error = message };
                File.WriteAllText(responsePathAbs, JsonUtility.ToJson(dto, true), new UTF8Encoding(false));
            }
            catch
            {
                /* ignore */
            }

            Debug.LogError("[NpcLlmConvoCli] " + message);
            exitPending = 1;
        }

        static void ScheduleExit()
        {
            var code = exitPending >= 0 ? exitPending : 0;
            exitPending = -1;
            EditorApplication.delayCall += () => EditorApplication.Exit(code);
        }
    }
}
