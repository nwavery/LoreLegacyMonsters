param(
    [string] $OutputPath = "Assets/StreamingAssets/oss-notices.txt",
    [string] $OllamaVersion = "REPLACE_WITH_VERSION",
    [string] $ModelName = "llama3.2-q4_k_m",
    [string] $ModelLicense = "Llama 3.2 Community License"
)

$ErrorActionPreference = "Stop"

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$finalPath = Join-Path $projectRoot $OutputPath
$finalDir = Split-Path -Parent $finalPath
New-Item -ItemType Directory -Force -Path $finalDir | Out-Null

$text = @"
Lore, Legacy, and Monsters - Open Source Notices
Generated: $(Get-Date -Format "yyyy-MM-ddTHH:mm:ssK")

1) Ollama runtime
   Version: $OllamaVersion
   License: MIT
   Upstream: https://github.com/ollama/ollama

2) Model artifact
   Model: $ModelName
   License: $ModelLicense
   Upstream: https://www.llama.com

Important:
- Verify all dependency notices before release.
- Confirm redistribution terms for bundled model artifacts.
"@

Set-Content -Path $finalPath -Value $text -Encoding UTF8
Write-Host "Wrote OSS notices: $finalPath"
