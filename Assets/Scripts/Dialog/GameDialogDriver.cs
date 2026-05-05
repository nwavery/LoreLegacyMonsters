using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using LoreLegacyMonsters.Core;
using LoreLegacyMonsters.World;
using LoreLegacyMonsters.Dialog.Llm;
using LoreLegacyMonsters.Monster;

namespace LoreLegacyMonsters.Dialog
{
    /// <summary>
    /// Central dialog presenter for authored lines and optional local-LLM flavor replies.
    /// </summary>
    public class GameDialogDriver : MonoBehaviour
    {
        [SerializeField] DialogSystem dialogSystem;
        [SerializeField] NPCController elderNpc;
        [SerializeField] bool playIntroOnStart;
        [SerializeField] bool useLegacyImGui;
        [SerializeField] KeyCode advanceKey = KeyCode.Space;
        [SerializeField] KeyCode replayKey = KeyCode.T;

        [Header("Local LLM (optional)")]
        [SerializeField] bool useLocalLlm = true;
        [SerializeField] NpcLlmSettings llmSettings;
        [TextArea(4, 12)] [SerializeField] string elderCharacterPrompt;
        [SerializeField] string elderSpeakerName = "Mira, Town Elder";

        DialogData _elderDialog;
        DialogData _llmRuntimeDialog;
        DialogData _lastScriptedDialog;
        NPCController _activeNpc;
        NpcLlmSettings _resolvedLlm;
        MonsterSystem _cachedMonsterSystem;
        QuestManager _cachedQuestManager;
        InventorySystem _cachedInventorySystem;

        readonly Dictionary<string, NpcDialogSession> _sessions = new Dictionary<string, NpcDialogSession>();
        string[] _suggestedReplies = Array.Empty<string>();
        bool _inDialog;
        bool _llmBusy;
        bool _awaitingPlayerReply;
        bool _allowReplyInput;
        Coroutine _llmCoroutine;
        UnityWebRequest _streamingWebRequest;
        int _conversationGeneration;

        public bool IsConversationOpen => _inDialog || _llmBusy || _awaitingPlayerReply;
        public bool IsBusy => _llmBusy;
        public NPCController ActiveNpc => _activeNpc;
        public bool CanAcceptPlayerReply => _awaitingPlayerReply && _allowReplyInput && _activeNpc != null;
        public IReadOnlyList<string> SuggestedReplies => _suggestedReplies;
        public NpcLlmValidatedCommand LastValidatedCommand =>
            _activeNpc != null && _sessions.TryGetValue(_activeNpc.NpcId, out var s) ? s.LastValidatedCommand : null;

        public NpcLlmSettings LlmSettingsAsset => _resolvedLlm;
        public event Action<NPCController> ConversationClosed;
        public event Action<NPCController, string> ChoiceCommandIssued;

        void Awake()
        {
            LlmRuntimeSupervisor.EnsureStarted();
            if (dialogSystem == null) dialogSystem = GetComponent<DialogSystem>();
            _resolvedLlm = NpcLlmSettings.ResolveForDriver(llmSettings);
            _elderDialog = DefaultGameContent.CreateElderGreetingDialog();
            _llmRuntimeDialog = ScriptableObject.CreateInstance<DialogData>();
            _llmRuntimeDialog.hideFlags = HideFlags.DontUnloadUnusedAsset;
            if (elderNpc != null && elderNpc.Dialog == null)
                elderNpc.BindRuntimeDialog(_elderDialog);

            _cachedMonsterSystem = FindFirstObjectByType<MonsterSystem>();
            _cachedQuestManager = FindFirstObjectByType<QuestManager>();
            _cachedInventorySystem = FindFirstObjectByType<InventorySystem>();
        }

        void OnDestroy()
        {
            AbortStreamingRequest();
            if (_llmCoroutine != null) StopCoroutine(_llmCoroutine);
        }

        void Start()
        {
            if (playIntroOnStart && elderNpc != null)
                BeginConversation(elderNpc, elderNpc.Dialog ?? _elderDialog);
        }

        void Update()
        {
            if (_activeNpc == elderNpc && WasPressedThisFrame(replayKey) && !_inDialog && !_llmBusy && _lastScriptedDialog != null)
                BeginConversation(elderNpc, _lastScriptedDialog);

            if (_llmBusy || !_inDialog || dialogSystem == null) return;
            if (WasPressedThisFrame(advanceKey))
                AdvanceOrClose();
        }

        public bool TryGetCurrentLine(out DialogEntry entry)
        {
            entry = null;
            return dialogSystem != null && dialogSystem.TryGetLine(out entry);
        }

        public void AdvanceConversation()
        {
            if (_llmBusy || !_inDialog || dialogSystem == null) return;
            if (dialogSystem.TryGetLine(out var current) && current.HasChoices())
                return;
            AdvanceOrClose();
        }

        public bool TryGetCurrentChoices(out string[] labels)
        {
            labels = Array.Empty<string>();
            if (dialogSystem == null || !dialogSystem.TryGetLine(out var entry) || !entry.HasChoices())
                return false;
            var built = new List<string>();
            for (var i = 0; i < entry.choiceNextIds.Length; i++)
                built.Add(entry.GetChoiceLabel(i));
            labels = built.ToArray();
            return labels.Length > 0;
        }

        public void SelectDialogChoice(int index)
        {
            if (_llmBusy || !_inDialog || dialogSystem == null) return;
            if (!dialogSystem.TryGetLine(out var entry) || !entry.HasChoices()) return;
            if (index < 0 || index >= entry.choiceNextIds.Length) return;
            var token = entry.choiceNextIds[index];
            if (string.IsNullOrWhiteSpace(token))
            {
                AdvanceOrClose();
                return;
            }

            token = token.Trim();
            if (token.StartsWith("next:", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(token.Substring(5), out var targetIndex))
            {
                dialogSystem.JumpTo(targetIndex);
                return;
            }

            ChoiceCommandIssued?.Invoke(_activeNpc, token);
            FinalizeConversation();
        }

        public void SubmitPlayerMessage(string playerMessage)
        {
            if (!CanAcceptPlayerReply || string.IsNullOrWhiteSpace(playerMessage) || _activeNpc == null)
            {
                Debug.LogWarning($"[LLM] SubmitPlayerMessage ignored (canReply={CanAcceptPlayerReply}, " +
                                 $"awaiting={_awaitingPlayerReply}, allow={_allowReplyInput}, activeNpc={(_activeNpc != null ? _activeNpc.NpcId : "<null>")}).");
                return;
            }

            _awaitingPlayerReply = false;
            if (_llmCoroutine != null) StopCoroutine(_llmCoroutine);
            var generation = ++_conversationGeneration;
            _llmCoroutine = StartCoroutine(LlmConversationFlow(_activeNpc, _lastScriptedDialog, playerMessage.Trim(), generation));
        }

        public void EndConversationFromUi() => FinalizeConversation();

        public void BeginConversation(NPCController npc, DialogData scriptedDialog = null, string playerMessage = null)
        {
            if (dialogSystem == null || npc == null) return;

            var generation = ++_conversationGeneration;
            AbortStreamingRequest();
            if (_llmCoroutine != null)
            {
                StopCoroutine(_llmCoroutine);
                _llmCoroutine = null;
            }

            var sess = GetOrCreateSession(npc.NpcId);
            sess.LastPlayerForMemory = null;
            sess.LastAssistantForMemory = null;
            sess.LastValidatedCommand = null;

            _activeNpc = npc;
            _lastScriptedDialog = scriptedDialog ?? npc.Dialog ?? BuildFallbackDialogForNpc(npc);
            _allowReplyInput = useLocalLlm && LlmGlobalPreferences.IsGloballyEnabled(true) && npc.UseLlmFlavor;
            _awaitingPlayerReply = false;
            _suggestedReplies = BuildSuggestedReplies(npc);

            if (_allowReplyInput && npc.PreferScriptedOpening && _lastScriptedDialog != null && string.IsNullOrWhiteSpace(playerMessage))
            {
                dialogSystem.Begin(_lastScriptedDialog);
                _inDialog = true;
                npc.BindRuntimeDialog(_lastScriptedDialog);
                AppendConversationTurn(npc.NpcId, "assistant", SummarizeDialog(_lastScriptedDialog));
                return;
            }

            if (_allowReplyInput)
            {
                _llmCoroutine = StartCoroutine(LlmConversationFlow(npc, _lastScriptedDialog, playerMessage, generation));
                return;
            }

            dialogSystem.Begin(_lastScriptedDialog);
            _inDialog = true;
            npc.BindRuntimeDialog(_lastScriptedDialog);
        }

        public void CloseConversation()
        {
            AbortStreamingRequest();
            _conversationGeneration++;
            if (_llmCoroutine != null)
            {
                StopCoroutine(_llmCoroutine);
                _llmCoroutine = null;
            }

            FinalizeConversation();
        }

        IEnumerator LlmConversationFlow(NPCController npc, DialogData fallback, string playerMessage, int generation)
        {
            _llmBusy = true;
            _inDialog = true;
            _awaitingPlayerReply = false;

            var settings = _resolvedLlm;
            var instructions = GetNpcPrompt(npc);
            var sess = GetOrCreateSession(npc.NpcId);
            var ctx = NpcLlmPromptContext.ForNpc(npc, instructions, playerMessage, BuildConversationHistorySummary(npc.NpcId),
                new NpcLlmPromptSystemRefs(_cachedMonsterSystem, _cachedQuestManager, _cachedInventorySystem));
            var temp = npc.LlmTemperatureOverride >= 0f ? npc.LlmTemperatureOverride : settings.Temperature;
            var maxTok = npc.LlmMaxTokensOverride > 0 ? npc.LlmMaxTokensOverride : settings.MaxTokens;

            var speaker = npc.NpcId == NPCController.ElderMiraId && !string.IsNullOrWhiteSpace(elderSpeakerName)
                ? elderSpeakerName.Trim()
                : npc.DisplayName;

            var messages = NpcLlmPromptBuilder.BuildMessages(ctx);
            var streamJson = OpenAiCompatibleLlmClient.BuildRequestJson(settings.Model, messages, temp, maxTok, true);

            string fullText = null;
            string lastError = null;
            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (_activeNpc != npc || generation != _conversationGeneration)
                {
                    Debug.LogWarning($"[LLM] Aborting stream: active NPC changed from {npc.NpcId}.");
                    _llmBusy = false;
                    _llmCoroutine = null;
                    yield break;
                }

                fullText = null;
                var streamOk = false;
                _llmRuntimeDialog.Configure($"dlg_llm_stream_{npc.NpcId}", new[]
                {
                    new DialogEntry { speaker = speaker, line = string.Empty }
                });
                dialogSystem.Begin(_llmRuntimeDialog);
                npc.BindRuntimeDialog(_llmRuntimeDialog);
                dialogSystem.NotifyLineContentChanged();

                yield return OpenAiCompatibleLlmClient.StreamChatCompletion(
                    settings.CompletionsUrl,
                    streamJson,
                    settings.RequestTimeoutSeconds,
                    delta =>
                    {
                        if (generation != _conversationGeneration || _activeNpc != npc) return;
                        if (_llmRuntimeDialog?.Entries == null || _llmRuntimeDialog.Entries.Length == 0) return;
                        _llmRuntimeDialog.Entries[0].line += delta;
                        dialogSystem.NotifyLineContentChanged();
                    },
                    (success, text) =>
                    {
                        streamOk = success;
                        fullText = text;
                        if (!success) lastError = text;
                    },
                    r => _streamingWebRequest = r);

                _streamingWebRequest = null;

                if (streamOk && !string.IsNullOrWhiteSpace(fullText))
                    break;

                Debug.LogWarning($"[LLM] Stream attempt {attempt + 1}/3 failed for {npc.NpcId}: {lastError ?? "<no text>"}");
                if (attempt < 2)
                    yield return new WaitForSecondsRealtime(attempt == 0 ? 0.4f : 1.2f);
            }

            if (string.IsNullOrWhiteSpace(fullText))
            {
                Debug.LogWarning($"[LLM] Streaming produced no text for {npc.NpcId}; falling back to non-stream POST.");
                var nonStreamJson = OpenAiCompatibleLlmClient.BuildRequestJson(settings.Model, messages, temp, maxTok, false);
                yield return OpenAiCompatibleLlmClient.SendChatCompletion(
                    settings.CompletionsUrl,
                    nonStreamJson,
                    settings.RequestTimeoutSeconds,
                    (ok, text) =>
                    {
                        if (ok) fullText = text;
                        else lastError = text;
                    });
            }

            _llmBusy = false;
            _llmCoroutine = null;
            AbortStreamingRequest();

            if (_activeNpc != npc || generation != _conversationGeneration)
                yield break;

            if (!string.IsNullOrWhiteSpace(fullText))
            {
                var clean = OpenAiCompatibleLlmClient.SanitizeReply(fullText);
                clean = NpcLlmResponseFilter.Clean(clean);
                NpcLlmValidatedCommand command = null;
                if (NpcLlmCommandParser.TryParseAndStrip(clean, out var displayText, out var parsedCmd))
                {
                    clean = displayText;
                    command = parsedCmd;
                }
                else
                {
                    clean = NpcLlmCommandParser.StripCommandMarkers(clean);
                }

                if (command != null && command.IsValid)
                {
                    sess.LastValidatedCommand = command;
                    ExecuteValidatedCommand(command, npc);
                }

                if (!NpcLlmResponseFilter.IsTooShortToDisplay(clean))
                {
                    AppendConversationTurn(npc.NpcId, "user", string.IsNullOrWhiteSpace(playerMessage) ? "(conversation opened)" : playerMessage);
                    AppendConversationTurn(npc.NpcId, "assistant", clean);
                    var entries = SplitIntoDialogEntries(clean, speaker);
                    _llmRuntimeDialog.Configure($"dlg_llm_{npc.NpcId}", entries);
                    dialogSystem.Begin(_llmRuntimeDialog);
                    npc.BindRuntimeDialog(_llmRuntimeDialog);
                    dialogSystem.NotifyLineContentChanged();
                    yield break;
                }

                Debug.LogWarning($"[LLM] Cleaned reply too short for {npc.NpcId} ({clean.Length}c). Falling back.");
            }
            else
            {
                Debug.LogWarning($"[LLM] No reply for {npc.NpcId}. Last error: {lastError ?? "<unknown>"}. Falling back to scripted dialog.");
                GameEvents.RaiseToast($"{npc.DisplayName} is using scripted dialog. Check Local LLM settings.");
            }

            var fallbackDialog = BuildFallbackTurnDialog(npc, fallback, playerMessage);
            dialogSystem.Begin(fallbackDialog);
            npc.BindRuntimeDialog(fallbackDialog);
            _allowReplyInput = false;
        }

        void AbortStreamingRequest()
        {
            if (_streamingWebRequest != null)
            {
                try { _streamingWebRequest.Abort(); } catch { /* ignored */ }
                _streamingWebRequest = null;
            }
        }

        string GetNpcPrompt(NPCController npc)
        {
            if (npc == null) return string.Empty;
            var basePrompt = npc.NpcId == NPCController.ElderMiraId && string.IsNullOrWhiteSpace(npc.LlmCharacterPrompt)
                ? (string.IsNullOrWhiteSpace(elderCharacterPrompt) ? NpcLlmPromptBuilder.DefaultElderCharacter : elderCharacterPrompt)
                : npc.LlmCharacterPrompt;
            if (string.IsNullOrWhiteSpace(basePrompt))
                basePrompt = npc.LlmIdentitySummary;
            return basePrompt;
        }

        DialogData BuildFallbackDialogForNpc(NPCController npc)
        {
            if (npc == null) return _elderDialog;
            if (npc.NpcId == NPCController.ElderMiraId) return _elderDialog;

            var data = ScriptableObject.CreateInstance<DialogData>();
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;
            data.Configure($"dlg_fallback_{npc.NpcId}", new[]
            {
                new DialogEntry
                {
                    speaker = npc.DisplayName,
                    line = npc.Role == NpcRole.Ambient
                        ? "The road ahead is dangerous, but the town still stands. Keep your monsters ready."
                        : "I have nothing more to say right now."
                }
            });
            return data;
        }

        static DialogEntry[] SplitIntoDialogEntries(string text, string speaker, int maxParts = 3)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new[] { new DialogEntry { speaker = speaker, line = "..." } };

            var bits = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (bits.Length <= 1)
                return new[] { new DialogEntry { speaker = speaker, line = text.Trim() } };

            var list = new List<DialogEntry>();
            for (var i = 0; i < bits.Length && i < maxParts; i++)
            {
                var line = bits[i].Trim();
                if (line.Length > 0)
                    list.Add(new DialogEntry { speaker = speaker, line = line });
            }

            if (list.Count == 0)
                list.Add(new DialogEntry { speaker = speaker, line = text.Trim() });
            return list.ToArray();
        }

        void AdvanceOrClose()
        {
            dialogSystem.Advance();
            if (!dialogSystem.TryGetLine(out _))
            {
                _inDialog = false;
                if (CanAcceptPlayerReply || (_allowReplyInput && _activeNpc != null))
                {
                    _awaitingPlayerReply = true;
                    return;
                }

                FinalizeConversation();
            }
        }

        void OnGUI()
        {
            if (!useLegacyImGui) return;
            if (_llmBusy)
            {
                DrawThinkingOverlay();
                return;
            }

            if (!_inDialog || dialogSystem == null) return;
            if (!dialogSystem.TryGetLine(out var entry))
            {
                _inDialog = false;
                _activeNpc = null;
                return;
            }

            const float h = 130f;
            var area = new Rect(16f, Screen.height - h - 16f, Screen.width - 32f, h);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(area);
            GUILayout.Space(8f);
            GUILayout.Label($"<b>{entry.speaker}</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Label(entry.line, new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12 });
            GUILayout.FlexibleSpace();
            GUILayout.Label($"[{advanceKey}] Continue", GUI.skin.box);
            GUILayout.EndArea();
        }

        void DrawThinkingOverlay()
        {
            const float h = 100f;
            var area = new Rect(16f, Screen.height - h - 16f, Screen.width - 32f, h);
            GUI.Box(area, GUIContent.none);
            GUILayout.BeginArea(area);
            GUILayout.Space(8f);
            GUILayout.Label("<b>Contacting local model...</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 13 });
            GUILayout.Label(
                "Run Ollama (or another OpenAI-compatible server) on 127.0.0.1 and pull the configured model. On failure, scripted lines are used.",
                new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 11 });
            GUILayout.EndArea();
        }

        static bool WasPressedThisFrame(KeyCode key)
        {
            var kb = Keyboard.current;
            if (kb == null) return Input.GetKeyDown(key);
            return key switch
            {
                KeyCode.Space => kb.spaceKey.wasPressedThisFrame,
                KeyCode.T => kb.tKey.wasPressedThisFrame,
                KeyCode.E => kb.eKey.wasPressedThisFrame,
                _ => Input.GetKeyDown(key)
            };
        }

        void FinalizeConversation()
        {
            if (_activeNpc != null)
                FlushPersistentMemoryForNpc(_activeNpc);

            _inDialog = false;
            _llmBusy = false;
            _awaitingPlayerReply = false;
            _allowReplyInput = false;
            _suggestedReplies = Array.Empty<string>();
            var closedNpc = _activeNpc;
            if (closedNpc != null && _sessions.TryGetValue(closedNpc.NpcId, out var fs))
                fs.LastValidatedCommand = null;
            _activeNpc = null;
            ConversationClosed?.Invoke(closedNpc);
        }

        void FlushPersistentMemoryForNpc(NPCController npc)
        {
            var gm = GameManager.Instance;
            if (gm?.NpcMemories == null || npc == null) return;
            var sess = GetOrCreateSession(npc.NpcId);
            if (string.IsNullOrWhiteSpace(sess.LastAssistantForMemory)) return;

            var topic = string.IsNullOrWhiteSpace(sess.LastPlayerForMemory)
                ? "farewell"
                : sess.LastPlayerForMemory.Trim();
            if (topic.Length > 64)
                topic = topic.Substring(0, 64).TrimEnd();

            gm.NpcMemories.RecordConversation(npc.NpcId,
                gm.World != null ? gm.World.CurrentAreaId : "unknown",
                sess.LastPlayerForMemory ?? string.Empty,
                sess.LastAssistantForMemory,
                topic);
        }

        string[] BuildSuggestedReplies(NPCController npc)
        {
            if (npc == null) return Array.Empty<string>();
            if (npc.LlmSuggestedTopics != null && npc.LlmSuggestedTopics.Length > 0)
                return npc.LlmSuggestedTopics;

            return npc.Role switch
            {
                NpcRole.Shopkeeper => new[] { "What supplies do you recommend?", "Any gossip from the road?", "Show me what you're selling." },
                NpcRole.Healer => new[] { "How dangerous is the route ahead?", "Can you heal my team again?", "What should I watch for in the forest?" },
                NpcRole.BossTrainer => new[] { "What do you protect here?", "Why are the monsters restless?", "What kind of trainer do you respect?" },
                NpcRole.Story => new[] { "What should I do next?", "What is happening nearby?", "Tell me about this place." },
                _ => new[] { "Anything interesting happening?", "How is the town doing?", "What should I know before I leave?" }
            };
        }

        NpcDialogSession GetOrCreateSession(string npcId)
        {
            if (string.IsNullOrWhiteSpace(npcId)) npcId = "_unknown";
            if (!_sessions.TryGetValue(npcId, out var s))
            {
                s = new NpcDialogSession();
                _sessions[npcId] = s;
            }

            return s;
        }

        string BuildConversationHistorySummary(string npcId)
        {
            if (!_sessions.TryGetValue(npcId, out var sess) || sess.ConversationHistory.Count == 0)
                return "No recent conversation.";
            var start = Mathf.Max(0, sess.ConversationHistory.Count - 4);
            var parts = new List<string>();
            for (var i = start; i < sess.ConversationHistory.Count; i++)
            {
                var msg = sess.ConversationHistory[i];
                if (msg == null || string.IsNullOrWhiteSpace(msg.content)) continue;
                parts.Add($"{msg.role}: {msg.content.Trim()}");
            }

            return parts.Count > 0 ? string.Join(" | ", parts) : "No recent conversation.";
        }

        void AppendConversationTurn(string npcId, string role, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return;
            var sess = GetOrCreateSession(npcId);
            sess.ConversationHistory.Add(new ChatMessageJson { role = role, content = content.Trim() });
            if (sess.ConversationHistory.Count > 8)
                sess.ConversationHistory.RemoveAt(0);

            if (role == "user")
                sess.LastPlayerForMemory = content.Trim();
            if (role == "assistant")
                sess.LastAssistantForMemory = content.Trim();
        }

        static string SummarizeDialog(DialogData dialog)
        {
            if (dialog?.Entries == null || dialog.Entries.Length == 0) return "The conversation has started.";
            var parts = new List<string>();
            for (var i = 0; i < dialog.Entries.Length && i < 2; i++)
            {
                if (!string.IsNullOrWhiteSpace(dialog.Entries[i].line))
                    parts.Add(dialog.Entries[i].line.Trim());
            }

            return parts.Count > 0 ? string.Join(" ", parts) : "The conversation has started.";
        }

        DialogData BuildFallbackTurnDialog(NPCController npc, DialogData scriptedFallback, string playerMessage)
        {
            if (string.IsNullOrWhiteSpace(playerMessage))
                return scriptedFallback ?? BuildFallbackDialogForNpc(npc);

            var data = ScriptableObject.CreateInstance<DialogData>();
            data.hideFlags = HideFlags.DontUnloadUnusedAsset;
            data.Configure($"dlg_fallback_turn_{npc.NpcId}", new[]
            {
                new DialogEntry
                {
                    speaker = npc.DisplayName,
                    line = npc.Role switch
                    {
                        NpcRole.Shopkeeper => "I can still trade with you, even if I do not have much else to add right now.",
                        NpcRole.Healer => "I do not have a good answer for that right now, but I can still tend to your party.",
                        NpcRole.BossTrainer => "Words can wait. What matters is whether you can prove yourself.",
                        _ => "I do not have much more to add right now, but my earlier advice still stands."
                    }
                }
            });
            return data;
        }

        void ExecuteValidatedCommand(NpcLlmValidatedCommand command, NPCController npc)
        {
            if (command == null || !command.IsValid) return;
            switch (command.Type)
            {
                case NpcLlmCommandType.OfferHint:
                case NpcLlmCommandType.SuggestDestination:
                    if (!string.IsNullOrWhiteSpace(command.Payload))
                        GameEvents.RaiseToast(command.Payload);
                    break;
                case NpcLlmCommandType.OpenShop:
                    GameEvents.RaiseToast($"{(npc != null ? npc.DisplayName : "This merchant")} opens the shop.");
                    if (npc != null && npc.Shop != null)
                    {
                        var oc = FindFirstObjectByType<OverworldChapterController>();
                        if (oc != null)
                        {
                            CloseConversation();
                            oc.OpenShopForNpc(npc.Shop);
                        }
                    }
                    break;
                case NpcLlmCommandType.OfferHeal:
                    GameEvents.RaiseToast($"{(npc != null ? npc.DisplayName : "The healer")} offers to restore your party.");
                    break;
                case NpcLlmCommandType.OfferBattle:
                    GameEvents.RaiseToast($"{(npc != null ? npc.DisplayName : "The trainer")} is inviting you to battle.");
                    break;
            }
        }

        sealed class NpcDialogSession
        {
            public readonly List<ChatMessageJson> ConversationHistory = new List<ChatMessageJson>();
            public NpcLlmValidatedCommand LastValidatedCommand;
            public string LastPlayerForMemory;
            public string LastAssistantForMemory;
        }
    }
}
