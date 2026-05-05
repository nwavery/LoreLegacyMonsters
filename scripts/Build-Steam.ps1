param(
    [Parameter(Mandatory = $true)]
    [string] $UnityPath,
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string] $BundleVersion = "1.0.0",
    [string] $SteamBuildNumber = ""
)

$ErrorActionPreference = "Stop"
$method = "LoreLegacyMonsters.Editor.SteamBuild.BuildWindowsRelease"
$log = Join-Path $ProjectPath "Build\unity-steam-build.log"

New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null
& (Join-Path $ProjectPath "scripts\Generate-OssNotices.ps1")

$args = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-executeMethod", $method,
    "-bundleVersion", $BundleVersion,
    "-steamBuildNumber", $SteamBuildNumber,
    "-logFile", $log
)

$p = Start-Process -FilePath $UnityPath -ArgumentList $args -Wait -PassThru
if ($p.ExitCode -ne 0) {
    throw "Steam Unity build failed with exit code $($p.ExitCode). See $log"
}

$buildOutputDir = Join-Path $ProjectPath "Build\Steam\Windows"
$appIdSource = Join-Path $ProjectPath "steam_appid.txt"
$appIdDest = Join-Path $buildOutputDir "steam_appid.txt"
if (Test-Path $appIdSource) {
    Copy-Item -Path $appIdSource -Destination $appIdDest -Force
    Write-Host "Copied steam_appid.txt to build output."
}

Write-Host "Steam build complete: $(Join-Path $buildOutputDir 'LoreLegacyMonsters.exe')"
