<#
.SYNOPSIS
  Repeat Start-Improvements.ps1 until a wall-clock duration expires (watchdog/soak).

.DESCRIPTION
  Appends JSON lines to Artifacts/LlmConvo/improvement_runs/watchdog_runs.ndjson for external alerting/triage.

.PARAMETER ContinueOnFailure
  If set, iterations keep running until duration elapses even when Start-Improvements exits non-zero.
#>
param(
    [string] $ProjectPath = "",
    [int] $DurationMinutes = 60,
    [switch] $SkipSteamBuild,
    [switch] $ContinueOnFailure
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $scriptDir = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Split-Path -Parent $MyInvocation.MyCommand.Path
    } else { $PSScriptRoot }
    $ProjectPath = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

if ($DurationMinutes -lt 1) { throw "DurationMinutes must be >= 1" }

$runner = Join-Path $ProjectPath "scripts\Start-Improvements.ps1"
if (-not (Test-Path -LiteralPath $runner)) { throw "Missing $runner" }

$runDir = Join-Path $ProjectPath "Artifacts\LlmConvo\improvement_runs"
New-Item -ItemType Directory -Force -Path $runDir | Out-Null
$watchLog = Join-Path $runDir ("watchdog_{0}.ndjson" -f (Get-Date -Format "yyyyMMdd_HHmmss"))
$Deadline = [datetime]::UtcNow.AddMinutes($DurationMinutes)

$i = 0
while ([datetime]::UtcNow -lt $Deadline) {
    $i++
    $iterStartUtc = [datetime]::UtcNow
    $escaped = foreach ($a in @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $runner, "-ProjectPath", $ProjectPath)) {
        if ([string]::IsNullOrWhiteSpace($a)) { "`"`"" }
        elseif ($a -match '[\s"]') {
            "`"$($a.Replace('"', '`"'))`""
        }
        else { $a }
    }
    if ($SkipSteamBuild) { $escaped += "-SkipSteamBuild" }
    $joined = ($escaped -join ' ')
    $p = Start-Process -FilePath "powershell.exe" -ArgumentList $joined -Wait -PassThru -NoNewWindow
    $elapsed = ([datetime]::UtcNow - $iterStartUtc).TotalSeconds
    $row = [ordered]@{
        utc          = (Get-Date).ToUniversalTime().ToString("o")
        iteration    = $i
        exitCode     = $p.ExitCode
        elapsedSec   = [math]::Round($elapsed, 3)
        skipSteam    = [bool]$SkipSteamBuild
        projectPath  = $ProjectPath
    }
    ($row | ConvertTo-Json -Compress) | Add-Content -Path $watchLog -Encoding utf8
    Write-Host "[watchdog iter $i exit=$($p.ExitCode) elapsed=${elapsed}s] NDJSON appended -> $watchLog"
    if ($p.ExitCode -ne 0 -and -not $ContinueOnFailure) {
        Write-Host "Aborting watchdog (non-zero exit and -ContinueOnFailure not set)." -ForegroundColor Yellow
        exit $p.ExitCode
    }
    if ([datetime]::UtcNow -lt $Deadline) {
        Write-Host "[watchdog] remaining $([math]::Round(($Deadline - [datetime]::UtcNow).TotalMinutes, 2)) min until deadline"
    }
}

Write-Host "Watchdog completed $i iteration(s); duration cap ${DurationMinutes}m. Log: $watchLog"
exit 0
