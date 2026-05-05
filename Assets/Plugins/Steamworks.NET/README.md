# Steamworks.NET Integration Notes

This folder is reserved for Steamworks.NET plugin files.

## Expected setup

1. Download Steamworks.NET package that matches your Unity and Steamworks SDK versions.
2. Copy plugin files into this folder.
3. Add scripting define `STEAMWORKS_NET` for supported build targets.
4. Set real App ID in `Assets/Scripts/Platform/Steam/SteamConfig.cs`.

## Runtime wiring already present

- `SteamBootstrap` for `RestartAppIfNecessary` and `SteamAPI.Init`.
- `SteamCallbackPump` for per-frame callback dispatch.
- Overlay state helper through `SteamBootstrap.IsOverlayEnabled()`.

Without plugin files and define symbols, Steam logic is automatically no-op.
