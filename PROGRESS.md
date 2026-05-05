# Lore, Legacy, and Monsters - Progress Report

## Rebuild note (2025)

The **`Assets/`** tree (scripts, scenes, tests, asmdefs) was **rebuilt from scratch** after the original assets were not available in version control. Behavior is reimplemented to match the documented architecture; expect to re-hook prefabs, UI, and ScriptableObject content in the Editor.

### Runtime chapter bootstrap (current state)

`Game.unity` now boots a playable campaign-scale runtime slice: a persistent manager stack, player character, town/route/forest/grove plus Lantern Marsh, Sunken Archive, Flooded Delta, Stormbreak Ridge, and Skyglass Spire overworld zones, authored `Resources` content (monsters, items, areas, quests, dialogs, shop), party-backed combat, NPC interaction, healing/shop services, and HUD-driven save/load. `DefaultGameContent` remains as fallback/bootstrap content when authored assets are missing.

### Systems foundation pass (2026)

- **Campaign orchestration** – `CampaignProgression`, `CampaignChapterGates`, and `GameEvents.RuntimeRestored`/`QuestCompleted` drive story/NPC visibility updates instead of per-frame scans in `OverworldChapterController`.
- **Quest pipeline** – `StoryQuestPipeline.RegisterAll` unifies quest registration with `GameManager` bootstrap.
- **Combat** – Injectable `IRandomSource` for hits, crits, status procs, and capture; `CombatManager` exposes optional serialized service refs to reduce `FindFirstObjectByType` churn.
- **Saves** – `SaveCoordinator` / `ISaveContributor` modularize apply/capture previously inlined in `GameManager`.
- **Regression tests** – `CampaignFoundationTests` covers seeded combat/capture, intro progression, and missing save slot load errors.

### Internal PC alpha readiness (2026)

- **Reproducible Windows build** – `Assets/Editor/AlphaBuild.cs` (menu **Build → Alpha → Windows**) and `scripts/Build-Alpha.ps1`; `StreamingAssets/alpha_build_info.json` + runtime `AlphaBuildInfo`.
- **Tester UX** – main menu **Test LLM**, **Help**, **About build**; in-game **F1** help; LLM failure surfaces to toasts; auto-save failures toast instead of failing silently.
- **LLM runtime defaults** – bundled Ollama settings now target `llama3.2:latest` with a longer cold-start timeout after the local Ollama update path exposed model-load latency.
- **Debug safety** – `VerticalSliceDebugControls` requires explicit `enableHotkeys`; F10 replaces F9 for debug attack to avoid clashing with quick-load.
- **Docs** – `ALPHA_TESTING.md`, `SMOKE_CHECKLIST.md`, `docs/alpha_bug_report_template.md`; `requirements.txt` aligned with Unity 6 + JSON saves.

### Visual polish pass (2026)

- **Generated pixel assets** – `tools/gen_visual_assets.py` (run from repo root with Python 3) writes overworld tiles, parallax silhouettes, props, element silhouettes, and combat backdrops under `Assets/Resources/Sprites/`.
- **Area presets** – `OverworldAreaVisuals` + refactored `OverworldPixelVisuals` apply per-zone ground tint, tile strips, parallax bands, and prop clusters (lamps, wildflowers, shards, ridge rocks).
- **Combat stages** – `CombatStageVisuals` selects `Resources/Sprites/Combat/bg_*` from the current `WorldManager` area; `CombatUI` draws them behind tinted sky/ground bands.
- **Themed UI building blocks** – `RuntimeUiFactory` adds modal chrome, list row cards, pill badges, and primary action buttons; rolled into shop, inventory, quest log, party modal, help body panel, main menu strip, and dialog Send.
- **Area banner** – `WorldUI` listens to `GameEvents.AreaChanged` and briefly shows the entered area name.
- **Visual smoke** – `VisualSmokeCapture` saves under `Artifacts/VisualSmoke/<yyyy-MM-dd>/`, adds an overworld-only tour (three zones), batch hooks `CaptureFullSuiteBatch` / `CaptureOverworldTourBatch`, and logs `[ASSERT]` checks for dialog hit-testing, combat, quest, and shop UI.

## Implemented Systems

### Core Systems
- ✅ **Monster System** - Basic functionality for monster data, instances, and mechanics
- ✅ **Combat System** - Turn-based battle system with abilities and effects
- ✅ **Inventory System** - Item management system with categories and usage
- ✅ **Shop System** - Store system with buying/selling items and special offers
- ✅ **Quest System** - Objective tracking system with rewards and dependencies
- ✅ **World System** - Area management, connections, and player movement
- ✅ **Save/Load System** - Game state persistence with JSON serialization
- ✅ **Game Controller** - Central manager that coordinates all systems
- ✅ **Dialog System** - System for conversations with NPCs and story delivery
- ✅ **Weather System** - Dynamic weather that affects gameplay
- ✅ **Achievement System** - Tracking player accomplishments and milestones

### User Interface Components
- ✅ **Main Menu UI** - Game start, options, and credits screens
- ✅ **World UI** - Area navigation and interaction interface
- ✅ **Shop UI** - Item purchasing and selling interface
- ✅ **Save/Load UI** - Game saving and loading interface
- ✅ **Inventory UI** - Item management interface
- ✅ **Dialog UI** - Interface for scripted lines plus multi-turn local-LLM follow-up
- ✅ **Weather UI** - Interface for displaying current weather conditions
- ✅ **Achievement UI** - Interface for viewing completed achievements

### Testing
- ✅ **Shop, World, Save/Load, Weather, Achievement** - See `Assets/Scripts/Tests/` and `Assets/Scripts/Tests/Editor/`
- ✅ **Inventory, Combat, Quest, Dialog, Dialogue, Combat UI, Quest priority, UI manager, Main story constants, Quest integration** - NUnit tests in the same test assembly

## Systems in Progress

### Core System Enhancements
- 🔄 **Monster Evolution** - System for monsters to evolve based on conditions
- 🔄 **Advanced Combat Mechanics** - Status effects, type advantages, and special abilities
- 🔄 **Quest Chains** - Interconnected quests with branching storylines
- 🔄 **World Events** - Special events that occur in the world based on time or conditions
- 🔄 **Day/Night Cycle** - Time progression system with day and night effects
- ✅ **NPC LLM Grounding / Memory / Safety Pass** - Hybrid scripted-plus-LLM NPC flow, compact NPC memory saves, validated command parsing, and richer prompt context are now implemented

### User Interface Enhancements
- ✅ **Combat UI** - Dedicated runtime battle panel now exposes clearer move, type, status, capture, and battle-result feedback
- ✅ **Monster Collection UI** - Party manager now exposes reserve state and evolution-forward detail for longer campaign planning
- ✅ **Quest Log UI** - Quest tracker and quest log now prioritize the current chapter goal and group story state more cleanly
- 🔄 **Map UI** - Exploration HUD now exposes route progress through the Chapter Three frontier, but a standalone map screen is still pending

### Content
- 🔄 **Monster Database** - Expanded into a broader campaign roster with late-slice evolutions and storm-region monsters, but still growing
- 🔄 **Item Database** - Adding more items with unique effects
- 🔄 **World Areas** - The world now reaches the Flooded Delta, Stormbreak Ridge, and Skyglass Spire, with authored encounter pools on area assets
- 🔄 **Main Story Quests** - Chapter One and Chapter Two now flow into a larger Chapter Three campaign arc
- 🔄 **Side Quests** - Optional collector, rumor, and mentor quest lines now exist, with room for more regional breadth

## Systems To Be Implemented

### Testing
- ✅ **Combat / Inventory / Quest / Save-Load / Shop / Dialog / Dialogue** - Baseline tests in `Assets/Scripts/Tests/`
- ✅ **Systems foundation** - `CampaignFoundationTests` (story bootstrap, deterministic combat/capture RNG, save load failure path)
- ❌ **Monster System Tests** - Dedicated monster/evolution coverage still thin
- 🔄 **Presentation Layer Tests** - `UIManager` modal/toast/loading coverage plus quest-priority checks now exist, but end-to-end UI flow tests are still thin
- ❌ **Game Controller Tests** - End-to-end flow tests not yet added

### Content
- 🔄 **Special Monsters** - Rare late-slice monsters now exist, but true legendary or endgame-tier content is still open
- 🔄 **Boss Trainers** - Multiple major challenge trainers now exist, but a broader challenge ladder is still open

## Next Steps

To complete the game, the following tasks need to be prioritized:

1. **Create More Unit Tests** - Implement tests for the core systems that don't yet have tests (Monster, Combat, Inventory, Quest, Dialog systems)
2. **Implement Day/Night Cycle** - Create a time system that affects monster encounters and gameplay
3. **Expand Monster Database** - Add more monster species with unique abilities and evolution paths
4. **Develop Main Story Quests** - Create a cohesive main storyline with progression and challenges
5. **Create Side Quests** - Add optional quests for additional rewards and gameplay variety
6. **Add Special Monsters and Boss Trainers** - Create endgame content and challenges
7. **Polish UI Components** - Refine the user interface for better player experience
8. **Balance Gameplay** - Adjust difficulty curves, monster stats, and progression

## Known Issues

1. Monster balancing needs adjustment
2. Some UI elements need polish and refinement
3. Save system still needs broader end-to-end failure-path coverage beyond the new atomic write path and regression tests
4. World areas need more visual distinction beyond the route-progress HUD and authored encounter identity
5. Performance optimization for larger areas

## Milestones

- **Alpha Release**: Core systems fully functional with minimal content
- **Beta Release**: All systems implemented with substantial content
- **Content Complete**: All planned content implemented and tested
- **Final Release**: Fully polished game with all features and content

## Current Status

The project is in **playable alpha moving toward beta-scale scope**. A campaign-length runtime slice now exists in `Game.unity`: move through overworld zones, talk to NPCs, trigger encounters, fight battles, buy supplies, heal the party, advance Chapter One through Chapter Three, save/load, and optionally layer local-LLM flavor onto selected NPC conversations.

Key accomplishments:
- Runtime bootstrap creates a functional game loop without relying on debug travel keys
- Monster party state now persists as structured instances with level/experience/current HP
- Combat supports multiple player actions (attack, skill, guard, potion, capture, switch, flee)
- Authored `Resources` content exists for core monsters, items, areas, quests, dialogs, and shop stock
- Save migration and content validation tests were added alongside the existing baseline suite
- A persistent `UIManager` and runtime `Canvas` presentation shell now drive the exploration HUD, quest tracker/log, combat panel, party manager, inventory, shop, dialog overlay, and loading overlay
- NPC conversations now support authored openings, player follow-up prompts, compact saved relationship memory, and validated non-authoritative command tags for hints/service flavor
- Local Ollama integration was revalidated against `127.0.0.1:11434/v1`; default settings now use `llama3.2:latest` and a 90-second request timeout.
- Chapter Two now adds Lantern Marsh, the Sunken Archive, Archivist Sel, rival trainer Corin, and a second authored quest spine with new monsters and moves
- The clarity polish pass added chapter-aware quest priority, readable route progression in the HUD, clearer combat result messaging, and stronger save/load/menu feedback
- The campaign expansion pass added Chapter Three progression, the Flooded Delta / Stormbreak Ridge / Skyglass Spire frontier, side-quest breadth, authored area encounter tables, new evolutions, and stronger regression coverage

The game has a solid foundation, and with the completion of the tasks outlined in the Next Steps section, it will be ready for beta testing and eventual release.

## Weather System

The Weather System enhances gameplay by adding environmental conditions that affect various aspects of the game. It's now fully implemented with the following components:

### Core Weather Functionality
- `WeatherSystem.cs`: Central class managing weather conditions, transitions, and gameplay effects
- `WeatherType` enum: Defines different types of weather (Clear, Cloudy, Rainy, Foggy, Stormy, Snowy, Windy, Sandstorm)
- Weather transitions with smooth visual/audio effects
- Area-specific weather probabilities
- Weather persistence in save files

### Weather Effects on Gameplay
- Movement speed modifiers based on weather conditions (slower in snow, etc.)
- Monster encounter rate adjustments in different weather
- Combat damage modifications (weather-appropriate monsters get bonuses)
- Monster catch rate modifications based on weather
- Weather-based environmental damage (sandstorms)
- Visibility range adjustments

### Weather Visual Effects
- Skybox changes for different weather conditions
- Particle effects for rain, snow, fog, etc.
- Screen overlay effects for visibility
- Special effect animations like lightning flashes

### Weather UI
- `WeatherUI.cs`: User interface for displaying current weather and effects
- Weather change notifications
- Visual indicators of current conditions
- Effect descriptions

### Weather System Testing
- `WeatherSystemTests.cs`: Comprehensive unit tests for the Weather System
- Tests for weather changes, effects on gameplay, saving/loading

## Achievement System

The Achievement System has been fully implemented to track player accomplishments and provide rewards. The system includes the following components:

### Core Achievement Functionality
- `AchievementSystem.cs`: Central manager for tracking and unlocking achievements
- `AchievementData.cs`: ScriptableObject that defines achievement properties
- `Achievement.cs`: Runtime instance of an achievement with progress tracking
- Integration with all major game systems via event subscriptions
- Save/load functionality to persist achievement state

### Achievement Categories & Types
- Collection achievements (monster catching, type specialists)
- Combat achievements (battles won, streaks, special victories)
- Exploration achievements (area discovery)
- Quest achievements (quest completion tracking)
- Training achievements (monster evolution, leveling)
- Shopping achievements (spending and collecting)
- Special achievements (secret discoveries, playtime)

### Achievement Rewards
- Money rewards
- Item rewards (with quantity control)
- Special monster rewards
- Custom reward descriptions

### Achievement UI
- `AchievementUI.cs`: User interface for displaying achievements
- Achievement list view with filtering by category and completion status
- Achievement details view with progress information
- Achievement unlock notifications
- Progress tracking display
- Tier-based visual indicators (Bronze, Silver, Gold, etc.)

### Testing & Development
- `AchievementSystemTests.cs`: Comprehensive unit tests covering all aspects of the system
- `SampleAchievements.cs`: Generator for creating test achievements

The Achievement System provides a robust framework for tracking player progress, encouraging exploration of all game features, and rewarding players for their accomplishments. It enhances player engagement and provides additional goals beyond the main story. 