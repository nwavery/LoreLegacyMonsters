# Steam AI Disclosure Draft

Use this draft to complete Steam's AI disclosure questions for this title.

## Product behavior

- The game includes NPC dialogue generated at runtime by a **local** open-source language model.
- Inference is performed on the player's machine using a bundled local runtime.
- Core progression systems (quests, rewards, inventory, save state, and ending logic) remain authoritative in scripted/gameplay systems.
- If the local model is unavailable, NPCs fall back to scripted authored dialogue.

## Safety and moderation posture

- The game does not rely on cloud inference services.
- The game does not upload player prompts to our servers as part of dialogue inference.
- Branch-critical reveals are kept in authored lines and deterministic branch state.

## Suggested Steam form text

### What AI-generated content is present in your game?

This game uses a local open-source language model to generate optional follow-up NPC dialogue lines in real time. Narrative-critical content remains authored/scripted.

### Is generated content reviewed?

Runtime-generated follow-up lines are constrained by system prompts, factual context from saved game state, and fallback rules. Major story reveals and progression outcomes are authored and validated in deterministic systems.

### Is user data sent externally for generation?

No. Runtime inference runs on a local model bundled with the game. Player prompt text is not transmitted to our servers for AI generation.

### Can users disable AI functionality?

Yes. Players can disable local AI dialogue and use scripted dialogue only.
