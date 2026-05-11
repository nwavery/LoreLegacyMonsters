<#
.SYNOPSIS
  Register (or remove) a Windows Scheduled Task that runs Start-Improvements.ps1 on a fixed interval.

.DESCRIPTION
  Use for unattended repetition when Cursor/agent is idle. Runs under the interactive user unless you
  override -PrincipalUserId.

  Typical scheduled run skips Steam builds (fast cadence); use -IncludeSteamBuild for less frequent/heavy schedules.

.PARAMETER IntervalMinutes
  How often task fires (minimum 1). Default 15.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Register-LoreImprovementsScheduledTask.ps1 -IntervalMinutes 20

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Register-LoreImprovementsScheduledTask.ps1 -Unregister
#>
param(
    [string] $TaskName = "LoreLegacyMonsters_NPC_Improvements",
    [string] $ProjectPath = "",
    [int] $IntervalMinutes = 15,
    [switch] $IncludeSteamBuild,
    [switch] $Unregister,
    [string] $PrincipalUserId = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $scriptDir = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Split-Path -Parent $MyInvocation.MyCommand.Path
    } else { $PSScriptRoot }
    $ProjectPath = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

$startScript = Join-Path $ProjectPath "scripts\Start-Improvements.ps1"
if (-not (Test-Path -LiteralPath $startScript)) {
    throw "Missing Start-Improvements.ps1 at $startScript"
}

if ($Unregister) {
    try {
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction Stop
        Write-Host "Removed scheduled task '$TaskName'."
    }
    catch {
        if ($_.Exception.Message -match "cannot find") { Write-Host "Task '$TaskName' was not registered." }
        else { throw }
    }
    exit 0
}

if ($IntervalMinutes -lt 1) { throw "IntervalMinutes must be >= 1" }

# Scheduled runs skip Steam by default so Unity build does not block every cadence tick.
$effectiveSkipSteam = -not $IncludeSteamBuild

$argList = @(
    "-NoProfile",
    "-ExecutionPolicy", "Bypass",
    "-WindowStyle", "Hidden",
    "-File", "`"$startScript`"",
    "-ProjectPath", "`"$ProjectPath`""
)

if ($effectiveSkipSteam) { $argList += "-SkipSteamBuild" }

$arguments = ($argList | ForEach-Object { $_ }) -join ' '

$trigger = New-ScheduledTaskTrigger -Once -At (Get-Date) `
    -RepetitionInterval (New-TimeSpan -Minutes $IntervalMinutes) `
    -RepetitionDuration (New-TimeSpan -Days 3650)

$userId = $PrincipalUserId
if ([string]::IsNullOrWhiteSpace($userId)) { $userId = "$env:USERDOMAIN\$env:USERNAME" }

$principal = New-ScheduledTaskPrincipal -UserId $userId `
    -LogonType Interactive `
    -RunLevel Limited

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Hours 3)

$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $arguments

Register-ScheduledTask -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Force | Out-Null

Write-Host "Registered scheduled task '$TaskName' ($IntervalMinutes minute interval)."
Write-Host "  Action: powershell.exe $arguments"
Write-Host "  Logs/reports under: $ProjectPath\Artifacts\LlmConvo\improvement_runs\"
