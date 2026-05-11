param(
    [string] $Version = "v2.0.0",
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

$targetDir = Join-Path $ProjectPath "tools/win"
$targetExe = Join-Path $targetDir "rcedit.exe"
$downloadUrl = "https://github.com/electron/rcedit/releases/download/$Version/rcedit-x64.exe"

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null
Write-Host "Downloading rcedit $Version from $downloadUrl"
Invoke-WebRequest -Uri $downloadUrl -OutFile $targetExe

if (!(Test-Path $targetExe)) {
    throw "rcedit download failed: $targetExe"
}

Write-Host "rcedit staged at $targetExe"
