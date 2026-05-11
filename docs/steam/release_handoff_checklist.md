# Steam Release Handoff Checklist

Last sweep: 2026-05-05 (Spacewar App ID 480 RC pipeline)

This document is the canonical pre-release sanity report and runbook for shipping
**Lore, Legacy, and Monsters** on Steam. It captures the most recent automated
RC sweep, exact commands to reproduce it, and the procedure to swap the App ID
once the real one is provisioned.

## 0. Shipping identity

| Field           | Value                              |
| --------------- | ---------------------------------- |
| Product name    | `Lore, Legacy, and Monsters`       |
| Company name    | `NA Dev`                |
| Bundle version  | `1.0.0`                            |
| Window title    | "Lore, Legacy, and Monsters"       |
| EXE Details (shell) — Product name / File description | "Lore, Legacy, and Monsters" (applied post-build via `rcedit`; see §2a) |
| EXE Details — Company | `NA Dev` |
| EXE Details — File/Product version | `X.Y.Z.0` from `bundleVersion` (padded to 4 segments) |
| EXE Details — Original filename | `LoreLegacyMonsters.exe` (matches launcher name on disk) |
| Save path (Win) | `%LocalLow%\NA Dev\Lore, Legacy, and Monsters\Saves\` |
| Log path (Win)  | `%LocalAppData%\NA Dev\Lore, Legacy, and Monsters\Logs\` |
| Steam App ID    | `480` (Spacewar) — placeholder for RC; swap with `Set-SteamAppId.ps1` |

> Note: Changing the publisher folder name moves local data. Saves/logs that
> lived under `%LocalLow%\LoreLegacyStudios\…` are not migrated automatically —
> copy them into `%LocalLow%\NA Dev\Lore, Legacy, and Monsters\` if you need prior
> test progress. Older paths under `%LocalLow%\…\LoreLegacyMonsters\` (different
> product title) behave the same way.

## 1. Snapshot of latest sweep

| Gate                          | Status | Evidence                                                                                            |
| ----------------------------- | ------ | --------------------------------------------------------------------------------------------------- |
| EditMode batch tests          | PASS   | `passed=104 failed=0 skipped=0` -> `Artifacts/TestResults/editmode-batch-summary.json`              |
| Smoke main menu               | PASS   | `assertions=4` -> `Artifacts/VisualSmoke/unity-smoke-main-menu.log`                                 |
| Smoke full                    | PASS   | `assertions=53` -> latest `Artifacts/VisualSmoke/unity-smoke-full*.log`                             |
| IL2CPP Steam build            | PASS   | `Build/Steam/Windows/LoreLegacyMonsters.exe` (+ `rcedit` **Details** metadata); Unity `6000.4.5f1`, bundle `1.0.0`; product `Lore, Legacy, and Monsters` |
| Packaged executable run       | PASS   | `gameAliveAt25s=True`                                                                               |
| Bundled Ollama auto-start     | PASS   | `ollamaAliveAt25s=True`, `Started bundled Ollama with CreateProcessW fallback at 127.0.0.1:11436`   |
| Steam graceful fallback (offline) | PASS | `SteamBootstrap: SteamAPI.Init failed; continuing without Steam features.`                          |

## 2. Build artifacts present in `Build/Steam/Windows`

- `LoreLegacyMonsters.exe` (~ 0.6 MB launcher, IL2CPP backed)
- `steam_appid.txt` (Spacewar `480`, copied automatically by build script)
- `LoreLegacyMonsters_Data/StreamingAssets/build_info.json`
- `LoreLegacyMonsters_Data/StreamingAssets/oss-notices.txt`
- `LoreLegacyMonsters_Data/StreamingAssets/llm/runtime/ollama.exe` (~31 MB)
- `LoreLegacyMonsters_Data/StreamingAssets/llm/models/llama3.2-q4_k_m.gguf` (~2 GB)

`build_info.json` (current sweep):

```json
{
    "version": "1.0.0",
    "steamBuildNumber": "rc-name-update-480",
    "builtAtUtc": "2026-05-05T16:24:00Z",
    "gitCommitShort": "3574df5",
    "unityVersion": "6000.4.5f1"
}
```

Verified runtime properties on this build:

- Window title: `Lore, Legacy, and Monsters`
- Save path: `%LocalLow%\NA Dev\Lore, Legacy, and Monsters\`
- Log path: `%LocalAppData%\NA Dev\Lore, Legacy, and Monsters\Logs\`

### 2a. Windows executable version resource (`Properties → Details`)

Unity does **not** populate user-visible `Details` strings (Product name,
File description, Company) on the shipping `.exe` by default.

After each successful Steam Windows player build, **`Assets/Editor/SteamBuild.cs`**
invokes **`scripts/Stamp-SteamWindowsExeMetadata.ps1`** (via `powershell.exe`), which
uses Electron's [`rcedit`](https://github.com/electron/rcedit) pinned at **v2.0.0** (stored at
`tools/win/rcedit.exe`, gitignored). This applies whether you build via:

- **`Build/Steam/Windows Release (IL2CPP)`** in the Unity editor, or
- **`scripts/Build-Steam.ps1`** (`-executeMethod LoreLegacyMonsters.Editor.SteamBuild.BuildWindowsRelease`).

If stamping fails (missing `powershell.exe`, blocked execution policy, `rcedit` download failure),
the build still succeeds — check the Unity log for **`[StampSteamExe]`** lines.

Manual re-stamp (e.g. after copying a raw build):

```powershell
.\scripts\Stamp-SteamWindowsExeMetadata.ps1 `
    -ExePath ".\Build\Steam\Windows\LoreLegacyMonsters.exe" `
    -BundleVersion "1.0.0" `
    -ProjectPath "."
```

Run from repo root in PowerShell:

```powershell
# 1. EditMode tests
.\scripts\Invoke-UnityBatchTask.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -Task edit-tests

# 2. Main menu smoke
.\scripts\Invoke-UnityBatchTask.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -Task smoke-main-menu

# 3. Full smoke
.\scripts\Invoke-UnityBatchTask.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -Task smoke-full

# 4. IL2CPP Steam release build
.\scripts\Build-Steam.ps1 `
    -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" `
    -BundleVersion "1.0.0" `
    -SteamBuildNumber "rc-<n>-<appid>"

# 5. Cold-launch sanity (game + bundled Ollama spawn)
.\scripts\Run-SteamBuildWithBundledLlm.ps1   # optional: external runtime, useful for debugging
```

## 4. One-time prerequisites on the build host

- Unity Hub editor `6000.4.5f1` with **Windows IL2CPP** module installed.
- Visual Studio 2022 (or VS Build Tools 2022) with the **Desktop development with C++** workload AND a Windows 10/11 SDK >= 10.0.19041.
  - Verified install pattern (already executed on this host):
    `winget install --id Microsoft.VisualStudio.2022.Community -e --override "--add Microsoft.VisualStudio.Workload.NativeDesktop --includeRecommended --passive --norestart --wait"`
- PowerShell 5.1+ (default on Windows).
- Steamworks SDK / steamcmd available before pushing real depots (only required for upload, not for build/test).

## 5. Vendored binaries (never committed)

- `Assets/Plugins/Steamworks.NET/Steamworks.NET.dll`
- `Assets/Plugins/Steamworks.NET/steam_api64.dll`
- `Assets/Plugins/Steamworks.NET/steam_api.dll` (alias for IL2CPP P/Invoke compatibility)
- `Assets/StreamingAssets/llm/runtime/ollama.exe` and supporting `lib/`
- `Assets/StreamingAssets/llm/models/llama3.2-q4_k_m.gguf`

Stage these on a fresh checkout with:

```powershell
.\scripts\Fetch-SteamworksNet.ps1
.\scripts\Fetch-OllamaRuntime.ps1
.\scripts\Fetch-Llama3Model.ps1   # may require -HfToken if upstream gated
```

`.gitignore` already excludes the binaries; only the README/`.gitkeep` files
should ever be committed inside those directories.

## 6a. RC quick start for testers (no real App ID yet)

Because App ID `480` is Valve's "Spacewar" sample, you **cannot** click "Play" on
Spacewar in your Steam library and have it launch our build — that route runs
Valve's LAN-finder demo. Use one of these two flows during RC.

### Direct launch (simplest)

1. Make sure Steam is running (any account).
2. Run `Build/Steam/Windows/LoreLegacyMonsters.exe` from Explorer or:
   ```powershell
   .\Build\Steam\Windows\LoreLegacyMonsters.exe
   ```
3. The Steam overlay, friends "Now playing", and Steamworks API calls will work.
   Steam will show your activity as "Spacewar" — that's expected until we swap
   to the real App ID.

### Non-Steam Game shortcut (looks like a real Steam launch)

1. Steam → **Games → Add a Non-Steam Game to My Library...**
2. Pick `Build/Steam/Windows/LoreLegacyMonsters.exe`.
3. Right-click the new entry → **Properties...** → set the name to
   `Lore, Legacy, and Monsters`.
4. Optionally set custom artwork (right-click → Set Custom Artwork) using
   anything from `docs/steam/presskit_checklist.md`.
5. Click **Play**.

The build is wired so `steam_appid.txt` (which the build script copies next to
the EXE) lets Steamworks initialize against `480` even when launched via the
Non-Steam shortcut. Overlay and Steamworks calls still work.

### Why this is necessary

Per Valve, an "owned" Steam library tile only points at your build once you
have a real App ID with launch options configured on the Steamworks partner
backend. Until then, App ID `480` is purely an SDK key for development —
the binary that Steam launches for `480` will always be Valve's Spacewar demo.

## 6. App ID swap procedure (Spacewar 480 -> real ID)

When the real Steam App ID and depot IDs are provisioned:

```powershell
.\scripts\Set-SteamAppId.ps1 -AppId <APP_ID> -DepotMainId <DEPOT_MAIN_ID> -DepotLlmId <DEPOT_LLM_ID>
```

This script atomically updates:

- `Assets/Scripts/Platform/Steam/SteamConfig.cs` (`AppId` constant)
- `steam_appid.txt`
- `tools/steam/app_build.vdf`
- `tools/steam/depot_build_main.vdf`
- `tools/steam/depot_build_llm.vdf`

After running it, re-run the full RC sweep in section 3 and confirm:

- `build_info.json.steamBuildNumber` reflects the new ID convention.
- Cold launch logs show Steam initialization success on a Steam-running host.

## 7. Runtime architecture summary

- **Steam integration**: `SteamBootstrap` initializes Steamworks.NET, calls
  `SteamAPI.RestartAppIfNecessary` only when not in local QA mode (detected via
  `steam_appid.txt` next to the executable). Achievement and rich-presence
  bridges no-op gracefully when init fails.
- **Bundled LLM runtime**: `LlmRuntimeSupervisor` enables itself automatically
  when `Assets/StreamingAssets/llm/runtime/ollama.exe` is present. It now has a
  layered startup pipeline:
  1. `Process.Start` direct exec
  2. `cmd.exe /c` shell fallback
  3. **Native `CreateProcessW` fallback** (required path on packaged IL2CPP, where
     the .NET process API returns Win32 `Native error= Success`)
  4. Reachability short-circuit: if `127.0.0.1:11436` is already accepting
     connections, supervisor stays idle.
  5. `LLM_EXTERNAL_RUNTIME=1` opt-out: when the helper script launches the
     runtime externally, the in-process supervisor skips spawning entirely.
- **Settings + accessibility**: `GameSettings`, `AccessibilitySettings` are
  defensive against `PlayerPrefs` being unavailable during static init (batch
  smoke crashes fixed previously).

## 8. Known issues / follow-ups

- `.NET Process.Start` for bundled Ollama still throws `Win32Exception` with
  `Native error= Success` inside packaged IL2CPP on Windows. The native
  `CreateProcessW` fallback covers it; root cause likely a Mono/IL2CPP
  Windows-API quirk. Track upstream if a future Unity LTS release changes this.
- Steam features (achievements, rich presence, overlay) require a running Steam
  client. Cold-launch from explorer logs `SteamAPI.Init failed; continuing without
  Steam features.` and the game continues offline. This is expected for QA,
  but real release validation must happen with Steam running.
- The bundled model is ~2 GB and is split into its own depot
  (`tools/steam/depot_build_llm.vdf`) so installs/updates don't recopy it on
  every patch.

## 9. Quick post-merge sanity (CI-friendly)

If integrating into CI, the minimum set is:

```powershell
.\scripts\Invoke-UnityBatchTask.ps1 -UnityPath "$env:UNITY_PATH" -Task edit-tests
.\scripts\Invoke-UnityBatchTask.ps1 -UnityPath "$env:UNITY_PATH" -Task smoke-full
.\scripts\Build-Steam.ps1 -UnityPath "$env:UNITY_PATH" -BundleVersion "$env:VERSION" -SteamBuildNumber "$env:STEAM_BUILD"
```

A clean run takes roughly 90-150 seconds for tests/smoke and 20-90 seconds for
the IL2CPP build (depending on whether code-only changes leverage incremental
build).

## 10. Sign-off

Engineering RC pipeline is green and the packaged Windows build is ready for:

- Internal beta branch upload (use `tools/steam/internal_beta_branch_runbook.md`).
- Storefront prep continuation (`docs/steam/store_marketing_checklist.md`,
  `docs/steam/store_description_draft.md`).
- Real App ID swap when provisioned (`scripts/Set-SteamAppId.ps1`).
