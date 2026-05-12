<#
.SYNOPSIS
  One-shot NPC / LLM quality pipeline for unattended or agent-driven runs.

.DESCRIPTION
  Runs in separate PowerShell child processes so nested scripts that call exit/throw do not kill this host.

  1) Unity Edit Mode tests
  2) Regenerate tools/convo/scenarios/manifest.jsonl
  3) Full NPC LLM scenario suite (unless -SkipLlmSuite or no TCP listener on Ollama ports)
  4) Writes Artifacts/LlmConvo/improvement_runs/report_<stamp>.md + latest.json
  5) Optional steam-build when all green

  A cross-process mutex queues concurrent runs (scheduled task + watchdog) so Unity batch does not collide.

  Exit codes: 0 all green, 1 tests or manifest failed, 2 suite failed, 3 steam failed, 4 LLM skipped, 5 mutex wait timeout.

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Start-Improvements.ps1
#>
param(
    [string] $UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string] $ProjectPath = "",
    [int] $MaxSuiteRetries = 3,
    [int] $SecondsBetweenUnitySteps = 15,
    [int] $SuiteRetryCooldownSeconds = 90,
    [int] $MutexWaitMinutes = 240,
    [int] $MaxUnityLockWaitMinutes = 30,
    [switch] $SkipUnityProcessIdleWait,
    [switch] $DisableImprovementMutex,
    [switch] $SkipSteamBuild,
    [switch] $SkipLlmSuite,
    [switch] $SteamDespiteLlmSuiteFailure
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $scriptDir = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Split-Path -Parent $MyInvocation.MyCommand.Path
    } else { $PSScriptRoot }
    $ProjectPath = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

function Test-ImprovementTcpPort([string] $TcpHost, [int] $port, [int] $timeoutMs = 1200) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect($TcpHost, $port, $null, $null)
        if (-not $iar.AsyncWaitHandle.WaitOne($timeoutMs)) { return $false }
        $client.EndConnect($iar)
        $client.Close()
        return $true
    }
    catch { return $false }
}

function Invoke-ChildPs1 {
    param(
        [Parameter(Mandatory)] [string] $ScriptPath,
        [string[]] $ExtraArgs = @()
    )
    $all = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $ScriptPath) + $ExtraArgs
    $escaped = foreach ($a in $all) {
        if ([string]::IsNullOrWhiteSpace($a)) { "`"`"" }
        elseif ($a -match '[\s"]') {
            $t = $a.Replace('"', '`"')
            "`"$t`""
        }
        else { $a }
    }
    $joined = ($escaped -join ' ')
    $p = Start-Process -FilePath "powershell.exe" -ArgumentList $joined -Wait -PassThru -NoNewWindow
    return $p.ExitCode
}

$improvementMutex = $null
$mutexHeld = $false
if (-not $DisableImprovementMutex) {
    $pathUtf8 = [Text.Encoding]::UTF8.GetBytes($ProjectPath.ToLowerInvariant())
    $hashHex = ([System.Security.Cryptography.SHA256]::Create().ComputeHash($pathUtf8) |
        ForEach-Object { $_.ToString('x2') }) -join ''
    $mtxName = 'Local\LoreLegacyMonsters_StartImprovements_' + $hashHex.Substring(0, 16)
    try {
        $improvementMutex = [System.Threading.Mutex]::new($false, $mtxName)
    }
    catch {
        Write-Host "Could not create improvement mutex $($_.Exception.Message)"
        exit 5
    }
    Write-Host "Waiting up to ${MutexWaitMinutes}m for improvement mutex ($mtxName) ..."
    try {
        if (-not $improvementMutex.WaitOne([TimeSpan]::FromMinutes($MutexWaitMinutes))) {
            Write-Host "Timed out waiting for another Start-Improvements instance to finish."
            try { $improvementMutex.Dispose() } catch {}
            exit 5
        }
    }
    catch {
        Write-Host "Mutex wait failed: $($_.Exception.Message)"
        try { $improvementMutex.Dispose() } catch {}
        exit 5
    }
    $mutexHeld = $true
    Write-Host "Improvement mutex acquired."
}

try {
    $runDir = Join-Path $ProjectPath "Artifacts\LlmConvo\improvement_runs"
    New-Item -ItemType Directory -Force -Path $runDir | Out-Null
    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $reportMd = Join-Path $runDir "report_$stamp.md"
    $latestJson = Join-Path $runDir "latest.json"

    "# NPC improvement run $stamp" | Out-File -FilePath $reportMd -Encoding utf8

    $exitCode = 0
    $phaseTests = "unknown"
    $phaseManifest = "unknown"
    $phaseSuite = "skipped"
    $summaryPath = Join-Path $ProjectPath "Artifacts\LlmConvo\scenarios\run_summary.json"
    $batchTask = Join-Path $ProjectPath "scripts\Invoke-UnityBatchTask.ps1"
    $suiteScript = Join-Path $ProjectPath "scripts\Invoke-NpcLlmScenarioSuite.ps1"

    Add-Content $reportMd "## 1) Edit Mode tests"
    $testArgs = @(
        "-UnityPath", $UnityPath,
        "-ProjectPath", $ProjectPath,
        "-Task", "edit-tests",
        "-MaxUnityLockWaitMinutes", "$MaxUnityLockWaitMinutes"
    )
    if ($SkipUnityProcessIdleWait) { $testArgs += "-SkipUnityProcessIdleWait" }
    $codeTests = Invoke-ChildPs1 $batchTask $testArgs
    if ($codeTests -ne 0) {
        $phaseTests = "FAILED"
        $exitCode = 1
        Add-Content $reportMd "RESULT: FAILED (child exit $codeTests)"
        Add-Content $reportMd "**Stop:** fix tests before LLM iteration."
    }
    else {
        $phaseTests = "ok"
        Add-Content $reportMd "RESULT: ok"
    }

    if ($exitCode -eq 0) {
        Start-Sleep -Seconds $SecondsBetweenUnitySteps
        Add-Content $reportMd ""
        Add-Content $reportMd "## 2) Scenario manifest export"
        $expLog = Join-Path $ProjectPath "Artifacts\LlmConvo\unity-improvement-manifest.log"
        $unityArgsExport = @(
            "-batchmode", "-nographics", "-quit",
            "-projectPath", $ProjectPath,
            "-logFile", $expLog,
            "-executeMethod", "LoreLegacyMonsters.Editor.NpcLlmScenarioManifestGenerator.ExportManifestToDefaultPath"
        )
        $exp = Start-Process -FilePath $UnityPath -ArgumentList $unityArgsExport -Wait -PassThru -NoNewWindow
        if ($exp.ExitCode -ne 0) {
            $phaseManifest = "FAILED"
            $exitCode = 1
            Add-Content $reportMd "RESULT: FAILED ($($exp.ExitCode)). See $expLog"
        }
        else {
            $phaseManifest = "ok"
            Add-Content $reportMd "RESULT: ok -> tools/convo/scenarios/manifest.jsonl"
        }
    }

    if ($exitCode -eq 0) {
        if (-not $SkipLlmSuite) {
            Start-Sleep -Seconds $SecondsBetweenUnitySteps
            $ollamaEnv = [Environment]::GetEnvironmentVariable("OLLAMA_HOST")
            Add-Content $reportMd ""
            Add-Content $reportMd "## 3) Live LLM scenario suite"
            $ollaDisp = '(unset)'
            if (-not [string]::IsNullOrWhiteSpace($ollamaEnv)) { $ollaDisp = $ollamaEnv }
            Add-Content $reportMd "OLLAMA_HOST: $ollaDisp"
            $p34 = Test-ImprovementTcpPort "127.0.0.1" 11434
            $p36 = Test-ImprovementTcpPort "127.0.0.1" 11436
            Add-Content $reportMd "TCP 127.0.0.1:11434 open=$p34 ; :11436 open=$p36"
            if (-not $p34 -and -not $p36) {
                $phaseSuite = "SKIPPED(no listener)"
                $exitCode = 4
                Add-Content $reportMd "RESULT: SKIPPED - no Ollama listener. Start Ollama or set OLLAMA_HOST / NPC_LLM_TEST_CHAT_COMPLETIONS_URL and rerun."
            }
            else {
                $suiteOk = $false
                for ($i = 1; $i -le [Math]::Max(1, $MaxSuiteRetries); $i++) {
                    Add-Content $reportMd ""
                    Add-Content $reportMd "### Suite attempt $i / $MaxSuiteRetries"
                    $c = Invoke-ChildPs1 $suiteScript @(
                        "-UnityPath", $UnityPath,
                        "-ProjectPath", $ProjectPath,
                        "-SkipManifestExport",
                        "-ContinueOnFailure"
                    )
                    if ($c -eq 0) {
                        $suiteOk = $true
                        Add-Content $reportMd "RESULT: ok"
                        break
                    }
                    Add-Content $reportMd "RESULT: failed (child exit $c). Cooldown ${SuiteRetryCooldownSeconds}s ..."
                    if (Test-Path $summaryPath) {
                        Add-Content $reportMd "--- run_summary.json ---"
                        Get-Content $summaryPath -Raw | Add-Content $reportMd
                    }
                    if ($i -lt $MaxSuiteRetries) { Start-Sleep -Seconds $SuiteRetryCooldownSeconds }
                }
                if (-not $suiteOk) {
                    $phaseSuite = "FAILED"
                    $exitCode = 2
                }
                else { $phaseSuite = "ok" }
            }
        }
        else {
            $phaseSuite = "skipped(flag)"
            Add-Content $reportMd ""
            Add-Content $reportMd "## 3) LLM suite SKIPPED (-SkipLlmSuite)"
        }
    }

    $steamEligible = -not $SkipSteamBuild -and $phaseTests -eq "ok" -and $phaseManifest -eq "ok" -and (
        (($exitCode -eq 0) -and ($phaseSuite -eq "ok")) -or
        ($SteamDespiteLlmSuiteFailure -and ($exitCode -eq 2 -or $exitCode -eq 4))
    )

    if ($steamEligible) {
        Start-Sleep -Seconds $SecondsBetweenUnitySteps
        Add-Content $reportMd ""
        Add-Content $reportMd "## 4) Steam Windows build"
        if ($SteamDespiteLlmSuiteFailure -and $exitCode -ne 0) {
            Add-Content $reportMd ("NOTE: -SteamDespiteLlmSuiteFailure set - building despite LLM suite exit {0} (Ollama down or eval/llm failures)." -f $exitCode)
        }
        $steamArgs = @(
            "-UnityPath", $UnityPath,
            "-ProjectPath", $ProjectPath,
            "-Task", "steam-build",
            "-MaxUnityLockWaitMinutes", "$MaxUnityLockWaitMinutes"
        )
        if ($SkipUnityProcessIdleWait) { $steamArgs += "-SkipUnityProcessIdleWait" }
        $cSteam = Invoke-ChildPs1 $batchTask $steamArgs
        if ($cSteam -ne 0) {
            $exitCode = 3
            Add-Content $reportMd "RESULT: FAILED (child exit $cSteam)"
        }
        else { Add-Content $reportMd "RESULT: ok" }
    }
    elseif (-not $SkipSteamBuild -and (($phaseSuite -ne "ok") -or ($exitCode -ne 0))) {
        Add-Content $reportMd ""
        Add-Content $reportMd "## 4) Steam build SKIPPED (pipeline not all green; use -SteamDespiteLlmSuiteFailure if tests+manifest passed but Ollama/suite failed)"
    }

    Add-Content $reportMd ""
    Add-Content $reportMd "## Agent checklist (for Cursor when you are away)"
    Add-Content $reportMd "- Read ``$summaryPath`` and failing ``Artifacts\LlmConvo\scenarios\*.json``."
    Add-Content $reportMd "- Patch: ``NpcContentRegistry``, ``NpcLlmScenarioManifestGenerator``, ``NpcLlmScenarioEvaluator``, ``NpcLlmResponseFilter``, ``NpcLlmScenarioBatch``, tests under ``Assets/Scripts/Tests``."
    Add-Content $reportMd "- Subjective bar: ``docs/story_bible.md`` (voice, no briefing paste, no meta/exam UI)."
    Add-Content $reportMd "- Regenerate manifest after registry/manifest edits; rerun ``scripts\Start-Improvements.ps1`` until exit 0."

    @{
        stamp          = $stamp
        exitCode       = $exitCode
        phaseTests     = $phaseTests
        phaseManifest  = $phaseManifest
        phaseSuite     = $phaseSuite
        reportMarkdown = $reportMd
        runSummaryJson = $summaryPath
    } | ConvertTo-Json | Out-File -FilePath $latestJson -Encoding utf8

    Write-Host ""
    Write-Host "Improvement report: $reportMd"
    Write-Host "Latest pointer:   $latestJson (exitCode=$exitCode)"

    exit $exitCode
}
finally {
    if ($mutexHeld -and ($null -ne $improvementMutex)) {
        try { [void]$improvementMutex.ReleaseMutex() } catch {}
        try { $improvementMutex.Dispose() } catch {}
    }
}
