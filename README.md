# Lore, Legacy, and Monsters

A monster-collecting RPG game built in Unity where players can explore, battle, and collect monsters in a rich world full of adventures.

## Game Overview

Lore, Legacy, and Monsters is an RPG game inspired by classic monster-collecting games. Players explore different areas of the world, capturing and training monsters, battling trainers, completing quests, and advancing through the story.

## Project Setup

### Requirements

- **Unity 6000.4.5f1** (Unity 6; authoritative version in `ProjectSettings/ProjectVersion.txt`)
- C# IDE (Visual Studio, Visual Studio Code, Rider, etc.)

### Setup Instructions

1. Clone this repository
2. Open the project in Unity 6 and allow it to regenerate IDE projects
3. Open **`Assets/Scenes/MainMenu.unity`** (index 0 in Build Settings) or **`Assets/Scenes/Game.unity`**
4. **`Game.unity`** now boots a runtime campaign slice automatically: systems, player, party, authored `Resources` content, Hollowfen through Lantern Marsh and the Sunken Archive into the Flooded Delta, Stormbreak Ridge, and Skyglass Spire, plus shop/healing/services, encounters, quests, save/load hooks, and a runtime `Canvas`-based presentation layer.
5. Press Play to run the game.

### Runtime Controls

- **WASD**: move through Hollowfen, the route, forest, grove, Lantern Marsh, the Sunken Archive, the Flooded Delta, Stormbreak Ridge, and Skyglass Spire
- **E**: interact with NPCs / services
- **1-7**: battle commands (Move 1, Move 2, Guard, Potion, Capture, Switch, Flee)
- **Tab**: open / close the party manager
- **J**: open / close the quest log
- **M**: open / close the discovered-area map and current quest marker
- **I**: open / close the inventory screen
- **Esc**: close shop or the currently open non-combat menu
- **Space**: advance dialogue and close battle results
- **F5 / F9**: save / load slot 0
- **F1**: open / close in-game help (controls, saves, LLM notes)
- **Enter / Send button**: submit a follow-up question during LLM-enabled NPC conversations

### Steam-ready Windows build (RC / **local testing**)

Before the game has its **own Steam App ID** with launch rules on Steamworks, the Windows shipping layout is exercised as an **RC / local-testing** candidate (IL2CPP, `steam_appid.txt`, bundled Ollama, Steamworks shim).

| Item | Detail |
| ---- | ------ |
| **Output** | `Build/Steam/Windows/LoreLegacyMonsters.exe` (+ `_Data`; see **`docs/steam/release_handoff_checklist.md`** §2) |
| **Steam App ID during RC** | `480` (**Spacewar**) — Steam uses this ID so `SteamAPI_Init` succeeds while Steam is running. You **cannot** launch our build using the storefront “Play” tile for Spacewar; that always runs Valve’s demo. |
| **Recommended: Non-Steam Game shortcut** | Steam → **Games → Add a Non-Steam Game to My Library…** → pick `Build/Steam/Windows/LoreLegacyMonsters.exe` → rename the entry → **Play**. Overlay and Steamworks behave like a normal Steam session; Steam may still show activity as **Spacewar**. Full steps: **`docs/steam/release_handoff_checklist.md`** → **§6a. RC quick start for testers**. |
| **Alternative** | Run the same `.exe` from Explorer/PowerShell with **Steam running**; `steam_appid.txt` next to the exe still applies (see checklist §6a **Direct launch**). |

**Important:** Installing or “playing” via a **Steam library shortcut** alone does **not** replace uploading a depot build to your real App ID—that flow is documented in the handoff checklist when you swap off Spacewar (**§6. App ID swap**).

Produce the build locally (editor menu or CLI—see **`docs/steam/release_handoff_checklist.md`** §2):

```powershell
.\scripts\Build-Steam.ps1 `
    -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" `
    -BundleVersion "1.0.0" `
    -SteamBuildNumber "<your-rc-label>"
```

Or batch: **`.\scripts\Invoke-UnityBatchTask.ps1 -Task steam-build`**.

Logs and bundled LLM diagnostics (Windows): **`%LocalAppData%\NA Dev\Lore, Legacy, and Monsters\Logs\`** (see checklist §0).

---

### Internal PC alpha builds

- **Menu**: **Build → Alpha → Windows Standalone (64-bit)** (writes `StreamingAssets/alpha_build_info.json` then builds).
- **CLI**: `.\scripts\Build-Alpha.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe"`
  Output: `Build/Windows/LoreLegacyMonsters.exe` (log: `Build/unity-alpha-build.log`).
- **Testers**: see **[ALPHA_TESTING.md](ALPHA_TESTING.md)** and **[SMOKE_CHECKLIST.md](SMOKE_CHECKLIST.md)**.
- **Bug template**: [docs/alpha_bug_report_template.md](docs/alpha_bug_report_template.md)

### Runtime UI Shell

The alpha no longer relies only on a single IMGUI HUD script. A persistent `UIManager` now creates a shared runtime `Canvas` and drives:

- exploration HUD with area, gold, weather, route progress, current-goal priority, prompts, and toast/status messaging
- compact party summary plus a larger party manager screen with reserve and evolution detail
- quest tracker and quest log with chapter-aware story emphasis
- dedicated combat panel with clearer move/result feedback
- shop and inventory panels
- dialog overlay and loading overlay

The old gameplay systems still own logic; the presentation layer reads their state and sends player actions back into them.

### Art pipeline (visual polish)

- Regenerate cozy pixel placeholders: `python tools/gen_visual_assets.py` (outputs to `Assets/Resources/Sprites/**`).
- **Visual smoke / screenshots**: Unity menu **Build → Visual Smoke** (full tour, main menu, or 3-zone overworld). Batchmode: `-executeMethod LoreLegacyMonsters.Editor.VisualSmokeCapture.CaptureOverworld` (full) or `CaptureOverworldTourBatch` (town/route/forest only). Optional `-visualCaptureDir <path>` overrides the output folder (default includes a date subfolder under `Artifacts/VisualSmoke/`).

### Persistence

- Saves use **JSON** under the player persistent data path (`SaveLoadManager` / `SaveSystem`). SQLite listed in `requirements.txt` is optional and not required for the rebuilt codebase.

### Local LLM for NPC dialogue (PC, optional)

The **`GameDialogDriver`** on **`DialogBootstrap`** can call a **separate OpenAI-compatible** HTTP server (defaults target **Ollama** on **`127.0.0.1`** to avoid IPv6/`localhost` quirks on Windows).

1. Install [Ollama](https://ollama.com/) and run it (tray app or `ollama serve`).
2. Pull a model that matches the **Model** field, e.g. `ollama pull llama3.2`.
3. In the Inspector, enable **Use Local Llm** on **`GameDialogDriver`**, or create an **`Npc Llm Settings`** asset (**Create → LLM → NPC LLM Settings**) and assign it. Defaults: base URL `http://127.0.0.1:11434/v1`, model `llama3.2:latest`, timeout **90** seconds to survive Ollama cold-starts after updates.
4. Other servers (e.g. **llama.cpp** in OpenAI-compatible mode) work if they expose **`POST …/v1/chat/completions`** with the same JSON shape.

The current NPC LLM layer is deliberately hybrid:

- story and service NPCs still open with authored quest-safe dialog
- LLM-enabled NPCs can then take short player follow-up questions in the runtime `DialogUI`
- prompts are grounded with area, weather, quest summary, party summary, inventory highlights, NPC role identity, and compact saved NPC memory
- NPC memory is lightweight and explicit in saves (`NpcMemorySaveEntry`) rather than full chat logs
- optional model commands are parsed only from a tiny validated whitelist such as hints or destination suggestions; quests, rewards, inventory, and save state remain authoritative in the existing game systems

The current authored slice now continues well past Hollowfen's first grove crisis:

- Elder Mira can hand off the next investigation after the Briar Warden arc resolves
- Lantern Marsh and the Sunken Archive extend the world path eastward
- Chapter Two introduces Archivist Sel and rival trainer Corin as new authored NPC anchors
- new marsh/archive monsters and moves provide a distinct combat identity for the second chapter
- Chapter Three expands beyond the archive into the Flooded Delta, Stormbreak Ridge, and Skyglass Spire
- new NPC anchors include Warden Neris, Mentor Cael, Veya the collector, Iris the rumor keeper, and Varo the Storm Tyrant
- side content now starts to branch outward with collector, rumor, and mentor quest lines alongside the main campaign arc
- new evolutions and late-slice monsters such as Tidehorn, Lantern Oracle, Stormling, Shard Raptor, and Delta King deepen the roster for longer play

If the request fails, times out, or returns empty text, the game **falls back** to scripted dialog instead of trusting empty model output. Keep the inference server bound to **localhost**; do not expose it to untrusted networks.

From the **Main Menu**, use **Local LLM settings** to enable or disable the model, set the OpenAI-compatible base URL (Ollama default: `http://127.0.0.1:11434/v1`), choose the model name, and **Save**. **Test connection** runs the same probe as **Test LLM connection**. Settings are stored in `PlayerPrefs` and overlay the bundled `Resources/Llm/NpcLlmSettings` asset. In the **Game** scene, the HUD shows a small **LLM** badge after the boot-time probe (green when reachable, orange when not).

#### Console setup (Windows)

From **PowerShell** in the repo root:

```powershell
# If Ollama is not installed yet:
winget install Ollama.Ollama --accept-package-agreements --accept-source-agreements

# Pull the same model family as in NpcLlmSettings / GameDialogDriver (default llama3.2:latest):
ollama pull llama3.2

# Optional: list models and hit the same API the game uses:
ollama list
```

Or run the bundled script (pull + smoke test against `127.0.0.1:11434`):

```powershell
.\scripts\Setup-Ollama.ps1
```

Override the model with `$env:OLLAMA_GAME_MODEL = 'mistral'` before running the script if needed.

#### NPC LLM quality loop (scenario suite + improvements)

Use this when you want **repeatable NPC dialog quality**: same prompt stack as shipped play (`NpcLlmPromptBuilder` → completions → **`NpcLlmDisplayPipeline.ShapeForHud`** → `NpcLlmResponseFilter.Clean` → command strip/parse), then **automated HUD gates** and optional manual review against **`docs/story_bible.md`**.

**Two different activities:**

| Activity | What changes | Typical scripts |
| --- | --- | --- |
| **Validate** | Nothing in C# by itself—re-runs tests + live LLM scenarios (and optional Steam build) | `Start-Improvements.ps1`, `Run-ImprovementsWatchdog.ps1` |
| **Improve (subjective)** | Prompts, global rules, evaluator gates, manifest forbids, regression tests | Edit **`NpcContentRegistry`**, **`NpcLlmPromptRules`**, **`NpcLlmScenarioEvaluator`**, **`NpcLlmHudQualityRegressionTests`**; then **`Start-Improvements.ps1`** to confirm green |

Long **watchdog** soaks only **validate** stability; they do **not** rewrite game code. **`Run-SubjectiveWritingAgentLoop.ps1`** asks Ollama for **JSON proposals** under `Artifacts/LlmConvo/writing_agent/`—a human or Cursor agent must **merge** those into the C# sources above, then validate.

##### Scenario suite (single run)

| What | Where |
| --- | --- |
| **Scenario manifest** (JSONL; regenerated from **`NpcContentRegistry`**) | `tools/convo/scenarios/manifest.jsonl` |
| **Regenerate manifest** | Unity menu **Tools → Lore Legacy → NPC LLM → Overwrite scenario manifest**, or batch: `-executeMethod LoreLegacyMonsters.Editor.NpcLlmScenarioManifestGenerator.ExportManifestToDefaultPath` |
| **Run all scenarios against a live endpoint** | `.\scripts\Invoke-NpcLlmScenarioSuite.ps1` (optional `-ManifestPath`) |
| **Per-scenario + rollup artifacts** | `Artifacts/LlmConvo/scenarios/<id>.json`, `run_summary.json` |
| **Batch entry only** | `-executeMethod LoreLegacyMonsters.Editor.NpcLlmScenarioBatch.RunScenarioSuiteBatch` with **`-batchmode`** and **`RUN_NPC_LLM_SCENARIO_SUITE=1`** |
| **Many iterations (older nightly helper)** | `.\scripts\Invoke-NpcLlmScenarioSuiteMany.ps1` → `Artifacts/LlmConvo/nightly-rollups/` |

**Environment**: **`NpcLlmDevEndpointResolver`** (`OLLAMA_HOST`, **`NPC_LLM_TEST_*`**, default **`http://127.0.0.1:11434/v1/chat/completions`**). Optional **`NPC_LLM_SCENARIO_MANIFEST`** for a non-default manifest path. Start Ollama (`ollama serve` or tray app) before any live suite.

Keep live LLM batches **local or overnight**—**`RUN_NPC_LLM_SCENARIO_SUITE` is off unless you opt in.**

**Automated gates**: per-scenario `forbidSubstringsPipe` / `forbidRegexPipe` / `maxParagraphs`; global policy in **`NpcLlmScenarioEvaluator`** (wiki voice, exam UI, meta-AI, walkthrough/tutorial copy, coach leaks, etc.). Offline tests: **`NpcLlmDisplayPipeline`**, **`NpcLlmScenarioEvaluator`**, **`NpcLlmHudQualityRegressionTests`**, **`NpcLlmCoachHudLeakTests`** (Edit Mode; no network).

**Suite runner notes**: HTTP on the Unity main thread so batch **`-quit`** writes artifacts; multiple completion passes per scenario with temperature/token backoff for Ollama empty replies. Unity exits **`0`** only when every scenario passes.

##### `Start-Improvements.ps1` (one full pipeline pass)

From repo root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Improvements.ps1
```

Steps: **Edit Mode tests** → **manifest export** → **LLM scenario suite** (retries) → optional **steam-build** → `Artifacts/LlmConvo/improvement_runs/report_<stamp>.md` and **`latest.json`**. A **cross-process mutex** avoids colliding with scheduled tasks or watchdog runs.

| Exit code | Meaning |
| --- | --- |
| **0** | All phases green |
| **1** | Edit Mode tests or manifest export failed |
| **2** | LLM suite failed (`llmFailures` / `evalFailures` in `run_summary.json`) |
| **3** | Steam build failed |
| **4** | LLM suite skipped (no Ollama listener) |
| **5** | Mutex wait timeout |

Useful flags:

- **`-SkipSteamBuild`** — faster iteration while tuning prompts (default for long soaks).
- **`-SkipLlmSuite`** — tests + manifest only.
- **`-SteamDespiteLlmSuiteFailure`** — run Steam when tests + manifest passed but suite failed or was skipped (exit **2** or **4**).
- **`-MaxUnityLockWaitMinutes`** (default **30**) — wait for other Unity processes before batch.
- **`-SkipUnityProcessIdleWait`** — do not wait for all `Unity.exe` to exit (risky if the **same project** is open in the Editor).
- **`-DisableImprovementMutex`** — only when you know no other improvement job is running.

Close the **Unity Editor** on this project if batch reports a project lock or lock-wait timeout.

##### `Run-ImprovementsWatchdog.ps1` (wall-clock soak)

Repeats **`Start-Improvements.ps1`** until **`DurationMinutes`** elapses. Appends one JSON line per iteration to **`Artifacts/LlmConvo/improvement_runs/watchdog_<stamp>.ndjson`**.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-ImprovementsWatchdog.ps1 `
    -DurationMinutes 600 -SkipSteamBuild -ContinueOnFailure
```

- **`-ContinueOnFailure`** — keep soaking after a non-zero iteration (typical for overnight runs).
- **`-SkipSteamBuild`** — recommended for multi-hour loops.

##### `Run-SubjectiveWritingAgentLoop.ps1` (Ollama proposals only)

Posts capped excerpts (story bible + prompt/evaluator/registry snippets) to Ollama on a timer; saves **`Artifacts/LlmConvo/writing_agent/proposal_*_iterN.md`** and **`writing_agent_<stamp>.ndjson`**. Expects **JSON-only** model replies (`branch`: GLOBAL \| NPC \| EVAL \| NO_EDIT). **Does not edit the repo**—merge proposals manually, then run **`Start-Improvements.ps1`**.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-SubjectiveWritingAgentLoop.ps1 `
    -DurationMinutes 240 -CycleSeconds 720
```

##### Windows scheduled task (optional)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Register-LoreImprovementsScheduledTask.ps1 -IntervalMinutes 20
```

Registers **`LoreLegacyMonsters_NPC_Improvements`** to run **`Start-Improvements.ps1`** on an interval (Steam skipped by default). **`-Unregister`** removes it.

##### Subjective tuning checklist (what to edit)

After reading failing **`Artifacts/LlmConvo/scenarios/<id>.json`** or playing a scene:

1. **`Assets/Scripts/World/NpcContentRegistry.cs`** — per-NPC `LlmPrompt` / `IdentitySummary` (voice, anti-briefing, role POV).
2. **`Assets/Scripts/Dialog/Llm/NpcLlmPromptRules.cs`** — global safety + tone (no wiki, exam UI, meta-AI, walkthrough voice).
3. **`Assets/Editor/NpcLlmScenarioManifestGenerator.cs`** — scenario flows, `forbidSubstringsPipe` / `forbidRegexPipe` when a pattern is NPC-specific.
4. **`Assets/Scripts/Dialog/Llm/NpcLlmScenarioEvaluator.cs`** — global HUD rejects for new failure modes.
5. **`Assets/Scripts/Tests/NpcLlmHudQualityRegressionTests.cs`** — lock new evaluator rules without a live LLM.
6. **`docs/story_bible.md`** — cast intent and themes (avoid spoiling twist architecture as player-facing fact).
7. Regenerate **`tools/convo/scenarios/manifest.jsonl`**, then **`Start-Improvements.ps1`** until **`latest.json`** shows **`exitCode: 0`** and **`run_summary.json`** has **`llmFailures: 0`**, **`evalFailures: 0`**.

Cursor agents: say **Start Improvements** to run the pipeline (see **`.cursor/rules/start-improvements.mdc`**).

## Architecture Overview

The game is built using a modular, component-based architecture with several key systems:

### Systems foundation (runtime contract)

Recent refactors focus on **maintainability** rather than expanding authored content:

- **Campaign progression** – `CampaignProgression` + `CampaignChapterGates` centralize main-story unlock rules; `OverworldChapterController` refreshes on `GameEvents.QuestCompleted` and `GameEvents.RuntimeRestored` instead of re-scanning every frame.
- **Quest bootstrap** – `StoryQuestPipeline.RegisterAll` is the single path for Resources + runtime-built campaign quests (invoked from `GameManager` and the overworld boot).
- **Combat determinism** – `IRandomSource` / `SeededRandomSource` feed `CombatSystem`, capture rolls, and optional encounter rolls; `CombatBattleRunner` bundles rules + RNG under `CombatManager` (optional **Combat Rng Seed** for fixed sequences).
- **Save modularity** – `SaveCoordinator` + `ISaveContributor` split apply/capture; `GameManager` wires default contributors.
- **Tests** – See `CampaignFoundationTests` for progression, RNG, capture, and save failure-path coverage.

### Core Systems

1. **Monster System** - Handles all monster data, instances, and mechanics
2. **Combat System** - Manages battles between monsters
3. **Inventory System** - Manages player items and equipment
4. **Shop System** - Provides buying and selling functionality
5. **Quest System** - Tracks missions and objectives
6. **World System** - Manages areas, exploration, and encounters
7. **Save/Load System** - Handles game state persistence
8. **Game Controller** - Coordinates all systems and manages game flow

### Data Management

- **ScriptableObjects** - Used for storing static game data (monster definitions, items, areas, etc.)
- **JSON Serialization** - Used for save data and dynamic content

### User Interface

- **Runtime Canvas UI** - `UIManager` creates a persistent shared canvas for menu, HUD, modal screens, and loading
- **Modular UI Components** - `WorldUI`, `CombatUI`, `MonsterUI`, `QuestUI`, `InventoryUI`, `ShopUI`, and `DialogUI` each own one presentation surface
- **Event-Based Updates** - UI toasts and state changes respond to `GameEvents` and existing manager state

## Key Systems Documentation

### Monster System

The Monster System manages monster definitions, instances, and evolution. It handles:

- Monster data (stats, abilities, types)
- Player's monster collection
- Wild monster generation
- Monster evolution
- Experience and leveling

### Combat System

The Combat System handles all aspects of monster battles:

- Turn-based battle logic
- Ability effects and damage calculation
- Status effects
- Monster switching
- Battle rewards
- Trainer battles

### Inventory System

The Inventory System manages the player's items:

- Item categories (potions, monster balls, key items, etc.)
- Item usage effects
- Inventory capacity and organization
- Item acquisition and removal

### Shop System

The Shop System provides commerce functionality:

- Different shop types (general, specialized)
- Buying and selling items
- Price multipliers
- Special offers and sales
- Item stock management

### Quest System

The Quest System tracks player objectives:

- Main story quests
- Side quests
- Quest rewards
- Quest dependencies and progression
- Quest tracking and UI

### World System

The World System manages the game world and player movement:

- Areas and connections
- Player travel
- Random encounters
- Trainers and NPCs
- Area features (shops, healers, etc.)
- Area unlocking and progression
- Area-authored encounter identity, with `WorldArea` assets now carrying their own wild encounter pools

### Save/Load System

The Save/Load System handles game state persistence:

- JSON-based save files
- Auto-saving
- Save file management
- Cross-system state saving and loading
- Menu/load feedback that now surfaces missing-slot and load-error states more explicitly

## Development Workflow

### Adding New Content

1. **New Monsters**: Create ScriptableObjects in `Assets/Resources/Monsters`
2. **New Items**: Create ScriptableObjects in `Assets/Resources/Items`
3. **New Areas**: Create ScriptableObjects in `Assets/Resources/Areas`
4. **New Quests**: Create ScriptableObjects in `Assets/Resources/Quests`

### Unit Testing

The project includes comprehensive unit tests for all core systems:

- Monster system tests
- Combat system tests
- Inventory system tests
- Shop system tests
- World system tests
- Save/Load system tests
- Encounter-table and quest-priority regression tests for the expanded campaign layer

Run tests from Unity Test Runner (**Window > General > Test Runner**). Tests live in `Assets/Scripts/Tests/` (assembly `LoreLegacyMonsters.Tests`).

For reliable local/CI batch validation, use the wrapper script from repo root:

```powershell
.\scripts\Invoke-UnityBatchTask.ps1 -Task edit-tests
.\scripts\Invoke-UnityBatchTask.ps1 -Task smoke-full
.\scripts\Invoke-UnityBatchTask.ps1 -Task smoke-main-menu
```

Edit-mode batch artifacts are exported to `Artifacts/TestResults/`:

- `unity-editmode-batch.log`
- `editmode-batch-summary.json`
- `editmode-batch-results.xml`

Smoke logs are exported to `Artifacts/VisualSmoke/` (`unity-smoke-full.log`, `unity-smoke-main-menu.log`, etc).

**Pre-alpha gate:** run the full Edit Mode suite and follow **[SMOKE_CHECKLIST.md](SMOKE_CHECKLIST.md)** before handing a Windows build to testers.

### Debugging

- **`VerticalSliceDebugControls`** is **off by default**; enable **`enableHotkeys`** on the component only for dev slices. Debug attack is **F10** (not F9) so F9 stays quick-load.
- **Standalone builds** have no Unity Console — use **Player.log** (see `AlphaDiagnostics` / **ALPHA_TESTING.md**).
- The runtime UI shell exposes quest priority, route progress, party, reserve/evolution detail, combat, shop, inventory, dialog, toast, loading, and save/load state at runtime.
- In the Editor, console logs and Inspector fields remain the main extra diagnostics.

## Credits

Developed by [Your Team Name]

## Legal and Policies

- End User License Agreement (draft): [docs/legal/EULA.md](docs/legal/EULA.md)
- Privacy Policy (draft): [docs/legal/PRIVACY_POLICY.md](docs/legal/PRIVACY_POLICY.md)
- Steam AI disclosure draft: [docs/steam/steam_ai_disclosure.md](docs/steam/steam_ai_disclosure.md)
- Open source notices (shipping path): `Assets/StreamingAssets/oss-notices.txt`