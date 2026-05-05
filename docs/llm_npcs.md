## LLM-Driven NPCs: How the System Works

This document explains, end to end, how a Lore & Legacy NPC holds a live conversation with the player using a local large language model. It covers where the prompt comes from, how each NPC stays in character and separated from the others, how memory persists across conversations, how the text gets on screen, and every file that participates.

### Goals and constraints

The design commits to a few non-negotiables that shape everything else:

- **Local only.** The default backend is Ollama over `http://127.0.0.1:11434/v1`. Nothing in this pipeline contacts a remote service. Any OpenAI-compatible `chat/completions` endpoint works if you point the settings elsewhere.
- **Per-NPC isolation.** Each NPC has its own in-memory conversation buffer and its own long-term memory record. Two NPCs can never leak context into each other even if you talk to them back to back.
- **Per-NPC character consistency.** Every prompt rebuilds the full system message from the NPC's role, identity summary, and character instructions, so the model cannot drift between characters mid-session.
- **Resilience.** If streaming fails, the driver falls back to a non-streaming POST. If the model is offline entirely, it falls back to scripted dialog and surfaces a toast so the player isn't stuck.
- **No authored lines are mandatory.** You can leave scripted lines in as openings, or let the LLM open the conversation, as per `PreferScriptedOpening` on the NPC.

### Component map

```
        ┌────────────────────────────────┐
Player ─│ DialogUI  (Assets/Scripts/UI/) │── types, presses Send
        └───────────────┬────────────────┘
                        │ SubmitPlayerMessage
                        ▼
        ┌──────────────────────────────────────────┐
        │ GameDialogDriver                         │
        │ Assets/Scripts/Dialog/GameDialogDriver.cs│
        │  • per-NPC sessions (history buffer)     │
        │  • orchestrates scripted vs LLM path     │
        │  • retries + stream-to-non-stream        │
        └──────┬───────────────────┬───────────────┘
               │                   │
               ▼                   ▼
      NpcLlmPromptBuilder    NpcLlmPromptContext
      (system + user        (game state: quest,
      messages)              party, weather, memory)
               │
               ▼
     OpenAiCompatibleLlmClient ────────► Ollama / compatible server
     (stream SSE + non-stream               http://127.0.0.1:11434/v1
     fallback, retries, sanitize)
               │
               ▼
      NpcLlmResponseFilter     NpcLlmCommandParser
      (strip jailbreak/meta)   (parse [[command:...]])
               │
               ▼
      back to GameDialogDriver
      → NpcMemoryService.RecordConversation
      → DialogSystem.Begin(final entries)
      → DialogUI typewriter reveal
```

### Enabling the pipeline at runtime

Three gates must all be `true` before the driver will call the model. You can see them co-located in `BeginConversation`:

```131:131:Assets/Scripts/Dialog/GameDialogDriver.cs
            _allowReplyInput = useLocalLlm && LlmGlobalPreferences.IsGloballyEnabled(true) && npc.UseLlmFlavor;
```

- `useLocalLlm` is a serialized field on `GameDialogDriver` (defaults to `true` in `Game.unity`).
- `LlmGlobalPreferences.IsGloballyEnabled(true)` reads `PlayerPrefs["llm.enabled"]`, defaulting to on if the player has never toggled the Main Menu switch.
- `npc.UseLlmFlavor` is a per-NPC flag set by `NPCController.Configure(... llmFlavor: true, ...)`; all shipped NPCs opt in.

If any of those is false the driver uses pure scripted dialog and the player never sees the reply input.

### Settings resolution

Runtime settings are a three-layer stack. The order of precedence, top wins:

1. `PlayerPrefs` overrides written by the Main Menu's `Local LLM (Ollama)` panel — `llm.baseUrl`, `llm.model`, `llm.enabled`.
2. `Assets/Resources/Llm/NpcLlmSettings.asset` — the authored defaults shipped with the project: `http://127.0.0.1:11434/v1`, `llama3.2:latest`, `temperature=0.6`, `maxTokens=256`, `requestTimeoutSeconds=90`.
3. `NpcLlmSettings.CreateRuntimeDefaults()` — a last-resort in-memory fallback if the `Resources` asset is missing.

The merge is implemented in `NpcLlmSettings.ResolveForDriver(serializedReference)`:

```46:53:Assets/Scripts/Dialog/Llm/NpcLlmSettings.cs
        public static NpcLlmSettings ResolveForDriver(NpcLlmSettings serializedReference)
        {
            var src = serializedReference != null ? serializedReference : LoadFromResources();
            var inst = src != null ? Instantiate(src) : CreateRuntimeDefaults();
            inst.hideFlags = HideFlags.HideAndDontSave;
            inst.ApplyPlayerPreferenceOverlay();
            return inst;
        }
```

The project asset is never mutated; `Instantiate` clones it and `ApplyPlayerPreferenceOverlay` layers the PlayerPrefs on top. Each NPC turn still reads from that same resolved copy, so saving new prefs takes effect the next time the driver is (re)initialized.

### Where prompts come from

Prompts are assembled fresh every turn by `NpcLlmPromptBuilder.BuildMessages(NpcLlmPromptContext ctx)`. The output is a two-message array in OpenAI chat format:

1. A `system` message — identity, role, rules, safety block, command grammar.
2. A `user` message — live game state and the player's latest utterance.

The `system` message has four concatenated sections:

- **Safety block.** A fixed string `NpcLlmPromptBuilder.SafetyBlock` that tells the model to refuse role changes, hidden-prompt reveals, and explicit content. Because it's emitted every turn it can't be evicted by history.
- **Global rules.** `DefaultGlobalRules` — "you are an NPC in a fantasy monster-collecting RPG", stay in character, never admit to being an AI, keep replies short, deflect real-world politics.
- **Character block.** Built from the NPC itself: `DisplayName`, `NpcId`, role name, `LlmIdentitySummary`, and `LlmCharacterPrompt`. For Elder Mira specifically the driver uses a longer default when the NPC's own prompt is blank (`DefaultElderCharacter`).
- **Command grammar.** Only the tags that make sense for this NPC's role are listed, so a merchant can see `offer_hint` and `open_shop`, a healer sees `offer_hint` and `offer_heal`, a boss sees `offer_hint` and `offer_battle`, everyone else sees `offer_hint` and `suggest_destination`. See `AppendRoleCommandLines`.

The `user` message is live game context plus what the player just said:

- `GameStateSummary` from `GameManager.BuildLlmStateSummary()` — current area, time-of-day summary, etc.
- `QuestSummary` from `QuestManager.GetPrimaryQuestSummary()` — what the player is actively doing.
- `PartySummary` from `MonsterSystem.GetPartySummary(...)` — monster roster with levels and HP, flattened to one line.
- `InventorySummary` — top four stacks.
- `WeatherSummary` — current weather enum.
- `NpcMemorySummary` from `NpcMemoryService.BuildPromptSummary(npcId)` — long-term memory about this specific player for this specific NPC.
- `ConversationHistorySummary` — the last four turns of this NPC's *current* session.
- The player's just-typed message, or a greeting prompt if the player hasn't spoken yet.

Because every one of those fields is looked up per-NPC, two NPCs in the same room build different prompts from the same game state.

### Keeping NPCs apart (per-NPC sessions)

Short-term conversation context is stored in a dictionary keyed by `NpcId`:

```36:36:Assets/Scripts/Dialog/GameDialogDriver.cs
        readonly Dictionary<string, NpcDialogSession> _sessions = new Dictionary<string, NpcDialogSession>();
```

Each session holds the rolling OpenAI-style chat history (up to 8 turns), the last validated in-game command, and the last player/NPC strings used when flushing long-term memory. When the driver needs the `ConversationHistorySummary` for a turn it only reads that NPC's buffer:

```481:495:Assets/Scripts/Dialog/GameDialogDriver.cs
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
```

So walking away from Toma mid-chat and talking to Pia the Healer starts a completely independent context for Pia. When you later return to Toma, his session dictionary entry is still there.

### Remembering the player across sessions (long-term memory)

Short-term per-session history is ephemeral; long-term memory is persistent and lives in `NpcMemoryService`, which Unity serializes via the save system. Each NPC the player has spoken to accumulates an `NpcMemoryState`:

```10:22:Assets/Scripts/Dialog/Llm/NpcMemoryState.cs
    [Serializable]
    public class NpcMemoryState
    {
        public const int MaxRecentTurns = 6;

        public string npcId;
        public int relationshipTier;
        public int conversationCount;
        public string lastSeenAreaId;
        public string lastTopic;
        public string memorySummary;
        public string lastPlayerMessage;
        public string lastNpcReply;
```

That state carries:

- `relationshipTier` — integer `-2..3` mapped to labels `hostile`, `wary`, `neutral`, `familiar`, `trusted`. It's adjusted by `relationshipDelta` every `Record` call (currently `0` by default; we can hook it up to story beats later).
- `conversationCount` — strictly monotonic counter.
- `lastSeenAreaId`, `lastTopic`, `lastPlayerMessage`, `lastNpcReply` — the most recent snapshot for prompt context.
- `_recentTurns` — a rolling buffer of up to six `{areaId, playerMessage, npcReply, turnIndex}` turns that the prompt summary draws from.

A conversation gets persisted exactly once per conversation, when the player closes it. That's done by `GameDialogDriver.FlushPersistentMemoryForNpc`:

```433:451:Assets/Scripts/Dialog/GameDialogDriver.cs
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
```

`FinalizeConversation` calls this whenever the dialog closes (Continue past the last line, clicking Close, or the driver is destroyed). On the next visit the new system prompt pulls back a compact summary:

```56:90:Assets/Scripts/Dialog/Llm/NpcMemoryState.cs
        public string BuildPromptSummary()
        {
            if (string.IsNullOrWhiteSpace(npcId) && string.IsNullOrWhiteSpace(memorySummary))
                return "No remembered history.";

            var sb = new StringBuilder(320);
            sb.Append("relationship: ").Append(RelationshipLabel)
                .Append("; conversations: ").Append(conversationCount)
                .Append("; last_area: ").Append(string.IsNullOrWhiteSpace(lastSeenAreaId) ? "unknown" : lastSeenAreaId)
                .Append("; last_topic: ").Append(string.IsNullOrWhiteSpace(lastTopic) ? "none" : lastTopic);

            if (_recentTurns.Count > 0)
            {
                sb.Append("; recent: ");
                var start = Mathf.Max(0, _recentTurns.Count - 3);
                for (var i = start; i < _recentTurns.Count; i++)
                {
                    var t = _recentTurns[i];
                    if (i > start) sb.Append(" | ");
                    sb.Append("turn ").Append(t.turnIndex).Append(" (")
                        .Append(string.IsNullOrWhiteSpace(t.areaId) ? "?" : t.areaId).Append("): ");
                    sb.Append("player=").Append(Truncate(t.playerMessage, 80)).Append("; npc=").Append(Truncate(t.npcReply, 80));
                }
            }
            else if (!string.IsNullOrWhiteSpace(memorySummary))
            {
                sb.Append("; memory: ").Append(memorySummary);
            }
            else
            {
                sb.Append("; memory: none");
            }

            return sb.ToString();
        }
```

That single line is what lands under `"Remembered history with this player:"` in the user message. The model never sees the full raw history of every visit — only the compact summary plus up to three rolling recent turns. That's what makes the NPC feel like they remember you without blowing the context window.

Persistence is handled through `NpcMemorySaveEntry` in `Assets/Scripts/SaveSystem`. `ApplySave` and `ExportSave` on `NpcMemoryService` are wired into the save/load pipeline, with a legacy-compatible path: saves from before the rolling buffer existed still load by synthesizing a single turn from `lastPlayerMessage`/`lastNpcReply`.

### One turn, end to end

Reading `GameDialogDriver.LlmConversationFlow` top to bottom:

1. **Session setup.** Mark the driver as busy, resolve the session for this `NpcId`, and build the `NpcLlmPromptContext` from all the live game subsystems.
2. **Per-NPC tuning.** If `NPCController.LlmTemperatureOverride >= 0` or `LlmMaxTokensOverride > 0`, those override the global `NpcLlmSettings` for just this turn — lets bosses be spicier, healers be gentler, etc.
3. **Serialize request.** `OpenAiCompatibleLlmClient.BuildRequestJson` turns the prompt context into an OpenAI chat/completions body with `stream: true`.
4. **Stream to the server.** Up to three attempts, each one calls `StreamChatCompletion` which:
   - POSTs the JSON with `Content-Type: application/json` and `Accept: text/event-stream`.
   - Uses a custom `DownloadHandlerScript` that buffers bytes, splits on newlines, parses each `data: { ... }` chunk as a `ChatStreamChunk`, and for each delta both appends to a full-text `StringBuilder` and invokes an `onDelta` callback.
   - On a non-success result, logs and returns `(false, error)` so the driver knows to retry with exponential-ish backoff (0.4s, 1.2s).
5. **Live streaming into the dialog.** For each delta the driver appends the token to the runtime dialog entry's `line` and the UI paints it the next frame through the typewriter. The player sees the reply fill in while the model is still generating.
6. **Non-streaming fallback.** If all three streaming attempts return empty, the driver issues one more POST with `stream: false` through `SendChatCompletion`. This is the belt-and-suspenders path for environments where Unity's `DownloadHandlerScript` has trouble with SSE.
7. **Post-processing.**
   - `OpenAiCompatibleLlmClient.SanitizeReply` strips control characters, normalizes line endings, and truncates to 4000 chars.
   - `NpcLlmResponseFilter.Clean` removes role-prefix lines (`assistant:`, `system:`, `user:`), special-token lines (`<|...|>`), and the classic jailbreak openers ("As an AI", "I'm an AI", "As a language model").
   - `NpcLlmCommandParser.TryParseAndStrip` finds the last `[[command:type|payload]]` in the text, pulls it out, and returns the visible portion separately.
   - If a valid command was emitted, `ExecuteValidatedCommand` fires the right `GameEvents.RaiseToast` — the player sees "Toma seems ready to trade" when the shopkeeper emits `open_shop`, etc.
8. **Present.** The cleaned reply is split on blank lines into at most three `DialogEntry` items and handed to `DialogSystem.Begin(_llmRuntimeDialog)`. That's what lets a long response advance in paragraphs with the Continue button.
9. **Session bookkeeping.** The player's message and the cleaned reply go into the NPC's short-term history (capped at 8) and are also held in `LastPlayerForMemory` / `LastAssistantForMemory` so `FlushPersistentMemoryForNpc` can capture them when the player finally closes the conversation.

### Guardrails

The system has three layers of defense against bad model output:

1. **Pre-request guardrails.** The `SafetyBlock` and `DefaultGlobalRules` system-message sections instruct the model to stay in character and refuse meta-prompts. Those are injected every single turn.
2. **Post-request sanitization.** `SanitizeReply` cleans bytes; `NpcLlmResponseFilter.Clean` cleans lines.
3. **Floor check.** `NpcLlmResponseFilter.IsTooShortToDisplay` (less than 10 non-whitespace chars after cleaning) rejects the reply and falls through to a scripted fallback line so you never see an empty speech bubble.

### Transport and retry policy

The HTTP client lives in `OpenAiCompatibleLlmClient.cs`. Two entry points:

- `StreamChatCompletion` — custom SSE handler, reports per-delta callbacks. Used for the default turn.
- `SendChatCompletion` — plain `DownloadHandlerBuffer`, one response. Used for the non-streaming fallback and for the health probe.

Retry policy is implemented in the driver: three streaming attempts with a short backoff, then one non-streaming attempt. A `UnityWebRequest` reference is tracked in `_streamingWebRequest` so the driver can `.Abort()` it if the player closes the dialog or switches NPCs mid-generation — that's what `AbortStreamingRequest()` is for.

### Health check and HUD badge

On overworld start, `OverworldChapterController.BootLlmProbe` sends a two-token `ping` request through `NpcLlmHealthCheck.Probe` and stores the result in `LlmRuntimeStatus`. The overworld HUD reads that singleton to show a small status badge, so you can tell at a glance whether Ollama is reachable before you try to talk to anyone. The Main Menu's `Local LLM settings` overlay runs the same probe on demand when you press **Test connection**.

### UI rendering

`DialogUI.cs` owns all dialog presentation. Three states it can be in:

- **Busy placeholder** — driver is awaiting the first token; show the NPC name and "Contacting the local model...".
- **Busy stream** — tokens are arriving; the runtime entry's `line` is growing; the typewriter reveals one character per frame up to `typewriterCharsPerSecond` (default 60).
- **Entry** — streaming is complete and the driver has swapped in the split final entries; the typewriter continues revealing.
- **Await reply** — no more entries and the conversation allows a reply; the input field and suggestion chips appear.

The typewriter helper uses a common-prefix heuristic so the reveal doesn't snap back when the streamed text is replaced by the post-processed version:

```122:139:Assets/Scripts/UI/DialogUI.cs
        string AdvanceTypewriter(string fullLine)
        {
            var line = fullLine ?? string.Empty;
            if (line.Length == 0)
            {
                typedSource = string.Empty;
                typedChars = 0f;
                return string.Empty;
            }

            var commonPrefix = CountCommonPrefix(typedSource, line);
            if (commonPrefix < Mathf.Min(typedSource.Length, line.Length))
                typedChars = commonPrefix;
            typedSource = line;
            typedChars = Mathf.Min(typedChars + typewriterCharsPerSecond * Time.unscaledDeltaTime, line.Length);
            var shown = Mathf.Clamp(Mathf.CeilToInt(typedChars), 0, line.Length);
            return line.Substring(0, shown);
        }
```

Clicking **Continue** mid-reveal snaps the line open instantly; a second click advances to the next entry or the reply box. Pressing **Space** always advances; both keep working together.

### File reference

Everything that participates, grouped by responsibility:

- `Assets/Scripts/Dialog/GameDialogDriver.cs` — orchestration: per-NPC sessions, retries, post-processing, memory flush.
- `Assets/Scripts/Dialog/DialogSystem.cs` and `DialogData.cs`, `DialogEntry.cs` — the low-level turn/entry model.
- `Assets/Scripts/Dialog/Llm/NpcLlmSettings.cs` — url, model, temperature, max tokens, timeout; resolves Resources + PlayerPrefs.
- `Assets/Scripts/Dialog/Llm/LlmGlobalPreferences.cs` — PlayerPrefs keys and helpers used by the Main Menu overlay.
- `Assets/Scripts/Dialog/Llm/NpcLlmPromptContext.cs` — bundles live game state into a single input object for the builder; accepts optional `NpcLlmPromptSystemRefs` from `GameDialogDriver` so party/quest/inventory lookups avoid per-turn `FindFirstObjectByType`.
- `Assets/Scripts/Dialog/Llm/NpcLlmPromptBuilder.cs` — constructs the system/user messages; role-scoped command grammar.
- `Assets/Scripts/Dialog/Llm/OpenAiSseAccumulator.cs` — SSE line buffer and delta extraction (shared by the stream download handler and unit tests).
- `Assets/Scripts/Dialog/Llm/OpenAiCompatibleLlmClient.cs` — HTTP transport, streaming SSE handler, sanitization helper.
- `Assets/Scripts/Dialog/Llm/ChatCompletionDtos.cs` — DTOs matching OpenAI chat/completions request and response.
- `Assets/Scripts/Dialog/Llm/NpcLlmResponseFilter.cs` — strip jailbreak/meta lines, minimum-length check.
- `Assets/Scripts/Dialog/Llm/NpcLlmCommandParser.cs` — `[[command:type|payload]]` extractor.
- `Assets/Scripts/Dialog/Llm/NpcLlmValidatedCommand.cs` — validated command data class and enum.
- `Assets/Scripts/Dialog/Llm/NpcMemoryService.cs` and `NpcMemoryState.cs` — per-NPC long-term memory and save/load.
- `Assets/Scripts/Dialog/Llm/NpcLlmHealthCheck.cs` — boot-time reachability probe.
- `Assets/Scripts/Dialog/Llm/LlmRuntimeStatus.cs` — singleton that stores the last probe result for the HUD.
- `Assets/Scripts/UI/DialogUI.cs` — dialog panel, typewriter reveal, reply input, suggestion chips.
- `Assets/Scripts/UI/LlmSettingsOverlay.cs` — Main Menu panel for base URL, model, enable toggle, test button.
- `Assets/Scripts/NPCController.cs` — the per-NPC configuration: role, character prompt, identity summary, suggested topics, optional temperature/max-tokens overrides.
- `Assets/Resources/Llm/NpcLlmSettings.asset` — shipped defaults for URL/model/temperature.

### Debugging and ops

Failures and recoverable issues log under a `[LLM]` tag (`Debug.LogWarning` / `Debug.LogError`). Successful requests are silent in production builds. On Windows you can find these at:

- Editor Play Mode: `%LOCALAPPDATA%\Unity\Editor\Editor.log` (also visible live in `Window > General > Console`).
- Built player: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\LoreLegacyMonsters\Player.log`.

Examples of messages you might see when something goes wrong:

```text
[LLM] Stream request failed (ConnectionError, code=0): Cannot connect to destination host
[LLM] No reply for merchant_toma. Last error: ...
[LLM] SubmitPlayerMessage ignored (canReply=False, ...)
```

If you see `[LLM] Stream request failed ...` or `[LLM] Non-stream parse failed ...`, the message includes the HTTP response code plus the first few hundred characters of the server body when available.

### Extending the system

- **Add a new NPC role.** Update the `NpcRole` enum, add a branch in `NpcLlmPromptBuilder.AppendRoleCommandLines` for the commands that role can emit, and add a branch in `GameDialogDriver.ExecuteValidatedCommand` for the commands you want to fire. Configure the NPC in the scene with `Configure(... role: NpcRole.Yours, llmFlavor: true, ...)`.
- **Add a new command type.** Extend `NpcLlmCommandType`, teach `NpcLlmCommandParser.ParseType` to recognize its string tag, list it in the role's command grammar in the prompt builder, and add a case in `ExecuteValidatedCommand`. The command grammar is the only contract with the model.
- **Give an NPC a bespoke voice.** Set `LlmCharacterPrompt` (long-form character instructions) and `LlmIdentitySummary` (one-liner). Optionally set `LlmTemperatureOverride` and `LlmMaxTokensOverride` for a different tone budget.
- **Swap the backend.** Point `Base URL` in the Main Menu settings at any OpenAI-compatible endpoint and change the model name to match. The pipeline doesn't assume anything Ollama-specific.
- **Tweak memory.** Change `NpcMemoryState.MaxRecentTurns` for a longer/shorter rolling buffer; adjust how `FlushPersistentMemoryForNpc` picks a `topic` if you want a smarter short summary.

### Limitations worth knowing

- `NpcLlmResponseFilter.Clean` normalizes output to `\n` line endings only, so cleaned length tracks the sanitized input (no accidental CRLF inflation on Windows).
- The `[[command:...]]` grammar is best-effort. Models sometimes wrap entire replies in a command, which is why the minimum-length floor exists.
- Temperature and max tokens are global by default; the per-NPC overrides are blunt instruments. If we add real per-NPC styling we should think about `frequency_penalty` and stop tokens.
- The short-term session history is kept in memory only. Quitting the game mid-conversation loses the last exchanges; only turns that were flushed to `NpcMemoryService` at conversation close survive.

### Default NPC roster and their roles

For reference, all twelve shipped NPCs opt into the LLM path via `OverworldChapterController.EnsureNpc(... llmFlavor: true, ...)`. Their role assignments — which govern command grammar and fallback dialog tone — are:

- `Story`: Elder Mira, Scout Rin, Archivist Sel, Warden Neris, Mentor Cael, Collector Veya, Rumor Iris.
- `Shopkeeper`: Merchant Toma.
- `Healer`: Pia the Healer.
- `BossTrainer`: Iona the Briar Warden, Corin the Rival, Varo the Storm Tyrant.

Their character prompts and suggested topics live inline in `OverworldChapterController.EnsureNpcs`, which is where to go if you want to tighten a voice or change the default topic chips for a given NPC.
