param(
    [string] $Version = "v0.6.6",
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

$targetDir = Join-Path $ProjectPath "Assets/StreamingAssets/llm/runtime"
$tmpDir = Join-Path $ProjectPath "Temp/ollama-fetch"
$zipPath = Join-Path $tmpDir "ollama-windows-amd64.zip"
$extractDir = Join-Path $tmpDir "extracted"

New-Item -ItemType Directory -Force -Path $targetDir, $tmpDir | Out-Null
if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }

$downloadUrl = "https://github.com/ollama/ollama/releases/download/$Version/ollama-windows-amd64.zip"
Write-Host "Downloading Ollama runtime $Version from $downloadUrl"
Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath

Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

Copy-Item (Join-Path $extractDir "*") -Destination $targetDir -Recurse -Force

$exePath = Join-Path $targetDir "ollama.exe"
if (!(Test-Path $exePath)) {
    throw "Ollama runtime staging failed. Missing $exePath"
}

Write-Host "Ollama runtime staged successfully in $targetDir"
