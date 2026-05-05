# Bundled LLM Runtime Staging

Place the Windows Ollama runtime files here for Steam packaging.

Required minimum files:

- `ollama.exe`
- Required DLLs distributed with the Ollama portable/runtime package

This folder is intentionally source-controlled as an empty scaffold.
Do not commit large binaries directly unless your repository policy permits it.

Recommended release flow:

1. Pull signed runtime artifact in CI or release workstation.
2. Copy runtime files into this folder.
3. Run `scripts/Generate-OssNotices.ps1`.
4. Build with `scripts/Build-Steam.ps1`.
