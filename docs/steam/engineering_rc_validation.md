# Engineering RC Validation

Last updated: 2026-05-05

## Scope

Engineering RC proof for Windows/Steam pipeline only (Spacewar App ID 480).

## Staged binary versions

- Unity editor: `6000.4.5f1`
- Steamworks.NET fetch script target: `2025.163.0`
- Ollama runtime fetch script target: `v0.6.6`
- Model artifact: `llama3.2-q4_k_m.gguf` (staged filename)

## Artifact staging results

Staged successfully (uncommitted local binaries):

- `Assets/Plugins/Steamworks.NET/Steamworks.NET.dll`
- `Assets/Plugins/Steamworks.NET/steam_api64.dll`
- `Assets/Plugins/Steamworks.NET/steam_api.dll` (runtime alias for P/Invoke compatibility)
- `Assets/StreamingAssets/llm/runtime/ollama.exe`
- `Assets/StreamingAssets/llm/models/llama3.2-q4_k_m.gguf`

Generated:

- `Assets/StreamingAssets/oss-notices.txt`
- `Assets/StreamingAssets/build_info.json`

## Build result

### Steam IL2CPP build (`scripts/Build-Steam.ps1`)

- Status: **passed**
- Output:
  - `Build/Steam/Windows/LoreLegacyMonsters.exe`
  - `Build/Steam/Windows/steam_appid.txt` (copied from repo root by build script)
  - `Build/Steam/Windows/LoreLegacyMonsters_Data/StreamingAssets/build_info.json`
  - `Build/Steam/Windows/LoreLegacyMonsters_Data/StreamingAssets/oss-notices.txt`
  - `Build/Steam/Windows/LoreLegacyMonsters_Data/StreamingAssets/llm/runtime/ollama.exe`
  - `Build/Steam/Windows/LoreLegacyMonsters_Data/StreamingAssets/llm/models/llama3.2-q4_k_m.gguf`

## Validation gates run

- EditMode batch tests: **passed**
  - `passed=104 failed=0 skipped=0`
  - Source: `Artifacts/TestResults/editmode-batch-summary.json`
- Smoke main menu: **passed**
  - Source: `Artifacts/VisualSmoke/unity-smoke-main-menu.log`
- Smoke full: **passed**
  - `assertions=53`
  - Source: `Artifacts/VisualSmoke/unity-smoke-full.log`

## Fixes made during validation

- `Invoke-UnityBatchTask.ps1` timeout/lock hardening to avoid indefinite hang.
- `Invoke-UnityBatchTask.ps1` log-reset hardening to avoid failures when previous smoke logs are file-locked.
- Steamworks compatibility fix in `SteamAchievementBackend` (removed version-sensitive stats prefetch call).
- Steam staging fix: `scripts/Fetch-SteamworksNet.ps1` now creates `steam_api.dll` alias from `steam_api64.dll`.
- Build packaging fix: `scripts/Build-Steam.ps1` now copies `steam_appid.txt` into `Build/Steam/Windows`.
- Local QA boot fix: `SteamBootstrap` now skips `RestartAppIfNecessary` when local `steam_appid.txt` is present, so packaged builds can be validated outside Steam client.
- Accessibility static-init safety fix (`PlayerPrefs` access guard in `AccessibilitySettings`) to prevent batch smoke crashes.
- LLM runtime startup hardening:
  - normalized bundled runtime/model paths
  - retry driver with startup backoff
  - endpoint reachability short-circuit for already-running local runtime
  - external-runtime opt-out via `LLM_EXTERNAL_RUNTIME=1`
  - native `CreateProcessW` fallback for packaged IL2CPP where `.NET Process.Start` fails with Win32 `Native error= Success`

## Cold-launch gate

- Status: **passed**
- Verified:
  - Packaged executable launches and stays alive during local QA cold launch (`gameStillRunningAfter20s=True`).
  - Bundled Ollama now starts during packaged cold launch (`ollamaRunning=True`).
  - Supervisor log confirms native fallback path success:
    - `Started bundled Ollama with CreateProcessW fallback at 127.0.0.1:11436`
  - Steam bootstrap no longer hard-exits in local QA mode.
  - Player log shows graceful Steam fallback when client init is unavailable:
    - `SteamBootstrap: SteamAPI.Init failed; continuing without Steam features.`

## One-touch App ID swap

Use:

```powershell
.\scripts\Set-SteamAppId.ps1 -AppId <APP_ID> -DepotMainId <DEPOT_MAIN_ID> -DepotLlmId <DEPOT_LLM_ID>
```

This updates:

- `Assets/Scripts/Platform/Steam/SteamConfig.cs`
- `steam_appid.txt`
- `tools/steam/app_build.vdf`
- `tools/steam/depot_build_main.vdf`
- `tools/steam/depot_build_llm.vdf`

For RC, current app ID is `480`.

## Optional QA helper

Use this command to force-launch packaged runtime externally (still useful for debugging):

```powershell
.\scripts\Run-SteamBuildWithBundledLlm.ps1
```

```powershell
.\scripts\Build-Steam.ps1 -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe" -BundleVersion "1.0.0" -SteamBuildNumber "rc-proof-480"
```
