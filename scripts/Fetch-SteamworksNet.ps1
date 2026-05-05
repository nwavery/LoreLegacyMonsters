param(
    [string] $Version = "2025.163.0",
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

$targetDir = Join-Path $ProjectPath "Assets/Plugins/Steamworks.NET"
$tmpDir = Join-Path $ProjectPath "Temp/steamworksnet-fetch"
$zipPath = Join-Path $tmpDir "steamworksnet.zip"
$extractDir = Join-Path $tmpDir "extracted"

New-Item -ItemType Directory -Force -Path $targetDir, $tmpDir | Out-Null
if (Test-Path $extractDir) { Remove-Item $extractDir -Recurse -Force }

$downloadUrl = "https://github.com/rlabrecque/Steamworks.NET/releases/download/$Version/Steamworks.NET-Standalone_$Version.zip"
Write-Host "Downloading Steamworks.NET $Version from $downloadUrl"
Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath

Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force

# Best-effort copy from common release layouts.
$candidateDlls = @("Steamworks.NET.dll", "steam_api64.dll")

foreach ($name in $candidateDlls) {
    $match = Get-ChildItem -Path $extractDir -Recurse -File | Where-Object { $_.Name -ieq $name } | Select-Object -First 1
    if ($null -ne $match) {
        Copy-Item $match.FullName -Destination (Join-Path $targetDir $match.Name) -Force
        Write-Host "Copied $($match.Name)"
    }
}

$managedDll = Join-Path $targetDir "Steamworks.NET.dll"
$nativeDll = Join-Path $targetDir "steam_api64.dll"
if (!(Test-Path $managedDll)) {
    throw "Steamworks.NET managed DLL missing at $managedDll"
}
if (!(Test-Path $nativeDll)) {
    throw "Steam API native DLL missing at $nativeDll"
}

# Some Steamworks.NET revisions P/Invoke "steam_api" instead of "steam_api64".
# Keep a same-binary alias so either binding name resolves at runtime.
$nativeAlias = Join-Path $targetDir "steam_api.dll"
Copy-Item $nativeDll -Destination $nativeAlias -Force
Write-Host "Copied steam_api.dll alias"

Write-Host "Steamworks.NET staged successfully in $targetDir"
