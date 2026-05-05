<#
.SYNOPSIS
  Headless Windows standalone alpha build (Unity 6 batchmode).

.PARAMETER UnityPath
  Full path to Unity.exe (e.g. C:\Program Files\Unity\Hub\Editor\6000.0.41f1\Editor\Unity.exe).

.PARAMETER ProjectPath
  Repository root containing Assets folder. Defaults to parent of this script's directory.
#>
param(
    [Parameter(Mandatory = $true)]
    [string] $UnityPath,
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"
$method = "LoreLegacyMonsters.Editor.AlphaBuild.BuildWindowsPlayer"
$log = Join-Path $ProjectPath "Build\unity-alpha-build.log"

New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null

Write-Host "Project: $ProjectPath"
Write-Host "Log: $log"
Write-Host "ExecuteMethod: $method"

$args = @(
    "-batchmode",
    "-quit",
    "-nographics",
    "-projectPath", $ProjectPath,
    "-executeMethod", $method,
    "-logFile", $log
)

$p = Start-Process -FilePath $UnityPath -ArgumentList $args -Wait -PassThru
if ($p.ExitCode -ne 0) {
    Write-Error "Unity build failed with exit code $($p.ExitCode). See $log"
}
Write-Host "Build output: $(Join-Path $ProjectPath 'Build\Windows\LoreLegacyMonsters.exe')"
