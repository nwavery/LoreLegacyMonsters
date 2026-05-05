# Internal PC alpha — tester guide

## What you need

- **Windows 10/11** PC.
- **Unity-built alpha** from the team (`LoreLegacyMonsters.exe` under `Build/Windows/`), not the Unity Editor.
- **Ollama** (or another OpenAI-compatible server on `127.0.0.1`) with the model the build expects — default **`llama3.2:latest`**. See [README.md](README.md) and `scripts/Setup-Ollama.ps1`.

## Before first launch

1. Install/start Ollama and pull the model family (e.g. `ollama pull llama3.2`, exposed to the game as `llama3.2:latest`).
2. Run the game.
3. On the **main menu**, tap **Test LLM connection**. You should see a toast that the endpoint is OK.
4. Read **Help** on the main menu (or press **F1** in-game).

## Saves

- **Slot 0** is the supported alpha slot: main menu **Load Slot 0**, in-game **F5** / **F9**, and **auto-save** all use slot 0.
- Saves are JSON files under:  
  `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Saves\`
- **Mid-combat save** is not a design goal for this alpha; use F5 when not in battle for predictable results.

## Logs & bug reports

When something breaks, attach:

1. **Build identity** from main menu **About build** (or `StreamingAssets/alpha_build_info.json` next to the `.exe`).
2. **Player.log** (typical Windows path):  
   `%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Player.log`
3. Steps to reproduce, and whether **Test LLM** passed.

### Bug report template (copy/paste)

```
Build: (from About build — version + git sha)
OS:
LLM: Ollama? Model name? Did "Test LLM connection" pass?
Steps:
Expected:
Actual:
Player.log excerpt (last ~50 lines if possible):
```

## For developers — producing a build

- Unity menu: **Build → Alpha → Windows Standalone (64-bit)**  
  or PowerShell: `scripts/Build-Alpha.ps1 -UnityPath "…\Unity.exe"`  
- See [README.md](README.md) **Alpha builds** section for details.
