<#
.SYNOPSIS
  Reliable Unity batch task runner with project-lock retries.

.DESCRIPTION
  Unity can occasionally exit with "another Unity instance is running with this project open"
  immediately after a previous batch/editor process exits. This wrapper serializes invocations,
  retries that transient failure, and optionally checks log text for failing smoke assertions.

.PARAMETER Task
  Batch task handled by BatchAutomationBootstrap: edit-tests, smoke-main-menu, smoke-tour, smoke-full, steam-build.
#>
param(
    [string] $UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.0.41f1\Editor\Unity.exe",
    [string] $ProjectPath = "",
    [Parameter(Mandatory = $true)]
    [ValidateSet("edit-tests", "smoke-main-menu", "smoke-tour", "smoke-full", "steam-build")]
    [string] $Task,
    [int] $MaxAttempts = 5,
    [int] $RetryDelaySeconds = 8,
    [int] $MaxUnityLockWaitMinutes = 5,
    [int] $TaskTimeoutMinutes = 45
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $scriptDir = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Split-Path -Parent $MyInvocation.MyCommand.Path
    } else {
        $PSScriptRoot
    }
    $ProjectPath = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

if (!(Test-Path $UnityPath)) {
    throw "Unity.exe not found at '$UnityPath'."
}

$artifactRoot = Join-Path $ProjectPath "Artifacts"
$testRoot = Join-Path $artifactRoot "TestResults"
$smokeRoot = Join-Path $artifactRoot "VisualSmoke"
New-Item -ItemType Directory -Force -Path $testRoot, $smokeRoot | Out-Null

$log = if ($Task -eq "edit-tests") {
    Join-Path $testRoot "unity-editmode-batch.log"
} elseif ($Task -eq "steam-build") {
    Join-Path $ProjectPath "Build\unity-steam-batch.log"
} else {
    Join-Path $smokeRoot "unity-$Task.log"
}

$summary = Join-Path $testRoot "editmode-batch-summary.json"
$resultsXml = Join-Path $testRoot "editmode-batch-results.xml"

function Test-UnityProcessRunning {
    $unity = Get-Process Unity -ErrorAction SilentlyContinue
    return $null -ne $unity
}

function Get-UnityProcessSummary {
    $unity = Get-Process Unity -ErrorAction SilentlyContinue
    if ($null -eq $unity) {
        return "none"
    }

    return ($unity | ForEach-Object {
        $mins = [Math]::Round(((Get-Date) - $_.StartTime).TotalMinutes, 1)
        "pid=$($_.Id) started=$($_.StartTime.ToString('s')) age_min=$mins"
    }) -join "; "
}

function Reset-FileIfExists([string] $path) {
    if (!(Test-Path $path)) { return }
    try {
        Remove-Item $path -Force -ErrorAction Stop
    }
    catch {
        throw "Unable to reset file '$path': $($_.Exception.Message)"
    }
}

for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    $lockWaitStarted = Get-Date
    while (Test-UnityProcessRunning) {
        $waited = (Get-Date) - $lockWaitStarted
        if ($waited.TotalMinutes -ge $MaxUnityLockWaitMinutes) {
            $summary = Get-UnityProcessSummary
            throw "Unity process lock wait exceeded $MaxUnityLockWaitMinutes minute(s). Running Unity processes: $summary"
        }

        Write-Host "Unity is still running; waiting $RetryDelaySeconds seconds before attempt $attempt/$MaxAttempts..."
        Start-Sleep -Seconds $RetryDelaySeconds
    }

    if (Test-Path $log) {
        try {
            Remove-Item $log -Force -ErrorAction Stop
        }
        catch {
            $logDir = Split-Path -Parent $log
            $base = [System.IO.Path]::GetFileNameWithoutExtension($log)
            $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
            $log = Join-Path $logDir "$base-$stamp.log"
            Write-Warning "Primary log file is locked, using alternate log path: $log"
        }
    }

    if ($Task -eq "edit-tests" -and (Test-Path $summary)) {
        Reset-FileIfExists $summary
    }
    if ($Task -eq "edit-tests" -and (Test-Path $resultsXml)) {
        Reset-FileIfExists $resultsXml
    }

    Write-Host "Starting Unity task '$Task' (attempt $attempt/$MaxAttempts)..."
    $unityArgs = @(
        "-batchmode",
        "-nographics",
        "-projectPath", $ProjectPath,
        "-batchTask", $Task,
        "-logFile", $log
    )
    if ($Task -eq "edit-tests") {
        $unityArgs += @("-batchTestSummary", $summary, "-batchTestResults", $resultsXml, "-testResults", $resultsXml)
    }

    $proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -PassThru
    $timedOut = -not $proc.WaitForExit($TaskTimeoutMinutes * 60 * 1000)
    if ($timedOut) {
        try {
            if (-not $proc.HasExited) {
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            }
        } catch {
            Write-Warning "Failed to terminate timed-out Unity process $($proc.Id): $($_.Exception.Message)"
        }

        $hint = if (Test-Path $log) {
            (Get-Content $log -Tail 40) -join [Environment]::NewLine
        } else {
            "No log file found at timeout."
        }
        throw "Unity task '$Task' timed out after $TaskTimeoutMinutes minute(s). Last log lines:`n$hint"
    }

    $logText = if (Test-Path $log) { Get-Content $log -Raw } else { "" }
    $locked = $logText -match "another Unity instance is running with this project open"

    if ($locked -and $attempt -lt $MaxAttempts) {
        Write-Warning "Unity project lock detected; retrying after $RetryDelaySeconds seconds."
        Start-Sleep -Seconds $RetryDelaySeconds
        continue
    }

    if ($locked) {
        throw "Unity project lock persisted after $MaxAttempts attempts. See $log"
    }

    if ($proc.ExitCode -ne 0) {
        throw "Unity task '$Task' failed with exit code $($proc.ExitCode). See $log"
    }

    if ($Task -eq "edit-tests") {
        if (!(Test-Path $summary)) {
            throw "EditMode summary was not written. See $log"
        }
        if (!(Test-Path $resultsXml)) {
            throw "EditMode NUnit XML was not written. See $log"
        }
        $json = Get-Content $summary -Raw | ConvertFrom-Json
        if ($json.failed -gt 0) {
            throw "EditMode tests failed: passed=$($json.passed) failed=$($json.failed) skipped=$($json.skipped). See $summary"
        }
        if (($json.passed + $json.failed + $json.skipped + $json.inconclusive) -eq 0) {
            throw "EditMode test runner completed but discovered zero tests. See $summary and $log"
        }
        Write-Host "EditMode tests passed: passed=$($json.passed) failed=$($json.failed) skipped=$($json.skipped)"
        Write-Host "EditMode artifacts: summary=$summary, nunit=$resultsXml, log=$log"
        exit 0
    }

    if ($Task -eq "steam-build") {
        Write-Host "Steam build task completed. Log: $log"
        exit 0
    }

    $fails = [regex]::Matches($logText, "\[ASSERT\]\[FAIL\]")
    $passes = [regex]::Matches($logText, "\[ASSERT\]\[PASS\]")
    if ($fails.Count -gt 0) {
        throw "Smoke task '$Task' had $($fails.Count) failing assertions and $($passes.Count) passing assertions. See $log"
    }
    if ($logText -notmatch "VisualSmokeCapture: complete") {
        throw "Smoke task '$Task' did not complete. See $log"
    }

    Write-Host "Smoke task '$Task' passed: assertions=$($passes.Count), log=$log"
    exit 0
}
