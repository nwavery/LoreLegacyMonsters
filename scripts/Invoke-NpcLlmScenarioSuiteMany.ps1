<#
.SYNOPSIS
  Repeatedly runs Invoke-NpcLlmScenarioSuite.ps1 overnight (many full passes).

.DESCRIPTION
  - Iteration 1: export manifest.jsonl then run Unity scenario batch (same as single script).
  - Iterations 2+: skips manifest Unity launch unless -ExportManifestEveryRun.
  - On suite failure (Invoke throws): logs FAIL, snapshots run_summary.json if present, continues.
  - Cooldown sleep between iterations (default 90s) to give Ollama/CPU slack.

.EXAMPLE
  cd c:\LLMRPG\LoreLegacyMonsters
  .\scripts\Invoke-NpcLlmScenarioSuiteMany.ps1 -Iterations 120 -CooldownSecondsBetweenRuns 90

.EXAMPLE detached (wake up later)
  Start-Process powershell.exe -WindowStyle Hidden -ArgumentList `
    '-NoProfile','-ExecutionPolicy','Bypass','-File','c:\LLMRPG\LoreLegacyMonsters\scripts\Invoke-NpcLlmScenarioSuiteMany.ps1','-Iterations','144','-CooldownSecondsBetweenRuns','120' `
    -WorkingDirectory 'c:\LLMRPG\LoreLegacyMonsters'
#>

param(
    [int] $Iterations = 108,
    [int] $CooldownSecondsBetweenRuns = 90,
    [string] $UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string] $ProjectPath = "",
    [string] $ManifestPath = "",
    [switch] $ExportManifestEveryRun
)

if ($Iterations -lt 1) { throw "-Iterations must be >= 1" }
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
}

$SINGLE = Join-Path $PSScriptRoot "Invoke-NpcLlmScenarioSuite.ps1"
if (!(Test-Path $SINGLE)) { throw "Missing $SINGLE" }

$rollupRoot = Join-Path $ProjectPath "Artifacts\LlmConvo\nightly-rollups"
New-Item -ItemType Directory -Force -Path $rollupRoot | Out-Null
$runnerPidPath = Join-Path $rollupRoot "nightly-batch-runner.pid"
$BATCH_LINE = Join-Path $rollupRoot "nightly-batch-line.log"

$utcStart = Get-Date
"$PID`t$utcStart`tIterations=$Iterations".Trim() | Out-File -FilePath $runnerPidPath -Encoding utf8

$stampFile = Join-Path $rollupRoot ("transcript_{0:yyyyMMdd_HHmmss}.txt" -f $utcStart)
Start-Transcript -Path $stampFile -Force

Write-Host ""
Write-Host "=== Nightly scenario batch: $($utcStart.ToString('o')), iterations=$Iterations, cooldown=${CooldownSecondsBetweenRuns}s ==="
Write-Host ""

$passed = 0
$failed = 0

for ($i = 1; $i -le $Iterations; $i++) {
    $t0 = Get-Date
    $tag = "{0:D3}-{1}" -f $i, ($t0.ToString("yyyyMMdd_HHmmss"))

    Write-Host "--- Iteration $i / $Iterations ($tag) ---"

    try {
        if ($ExportManifestEveryRun -or $i -eq 1) {
            $null = & $SINGLE -UnityPath $UnityPath -ProjectPath $ProjectPath -ManifestPath $ManifestPath
        }
        else {
            $null = & $SINGLE -UnityPath $UnityPath -ProjectPath $ProjectPath -ManifestPath $ManifestPath `
                -SkipManifestExport
        }

        $summaryPath = Join-Path $ProjectPath "Artifacts\LlmConvo\scenarios\run_summary.json"
        if (Test-Path $summaryPath) {
            $snap = Join-Path $rollupRoot ("run_summary_{0}.json" -f $tag)
            Copy-Item $summaryPath $snap -Force
            $j = Get-Content $summaryPath -Raw | ConvertFrom-Json
            $line = "$(Get-Date -Format o)`t$i/$Iterations`tPASS`tllm=$($j.llmFailures)`teval=$($j.evalFailures)"
            Add-Content -Path $BATCH_LINE -Value $line
            Write-Host $line
        }
        else {
            $line = "$(Get-Date -Format o)`t$i/$Iterations`tPASS(NO_SUMMARY_JSON)"
            Add-Content -Path $BATCH_LINE -Value $line
            Write-Host $line
        }

        $passed++
    }
    catch {
        $failed++
        $msg = "$($_.Exception.Message)" -replace "[\r\n]+", " "
        $line = "$(Get-Date -Format o)`t$i/$Iterations`tFAIL`t$msg"
        Add-Content -Path $BATCH_LINE -Value $line
        Write-Host "FAIL: $_"

        try {
            $summaryPath = Join-Path $ProjectPath "Artifacts\LlmConvo\scenarios\run_summary.json"
            if (Test-Path $summaryPath) {
                $snap = Join-Path $rollupRoot ("run_summary_fail_{0}.json" -f $tag)
                Copy-Item $summaryPath $snap -Force
            }
        }
        catch { }
    }

    if ($i -lt $Iterations -and $CooldownSecondsBetweenRuns -gt 0) {
        Write-Host "Cooldown $($CooldownSecondsBetweenRuns)s ..."
        Start-Sleep -Seconds $CooldownSecondsBetweenRuns
    }
}

$utcDone = Get-Date
Write-Host ""
Write-Host "=== Batch finished $($utcDone.ToString('o')) ==="
Write-Host "Passes (no throw): $passed  Failures: $failed  Iterations configured: $Iterations"
Write-Host "Line log: $BATCH_LINE"
Write-Host "Rollups snapshot dir: $rollupRoot"

try { Stop-Transcript } catch { }
