<#
.SYNOPSIS
  Export manifest.jsonl then run the full NPC LLM scenario suite against a live local endpoint.

.DESCRIPTION
  1) Writes tools/convo/scenarios/manifest.jsonl via Unity -executeMethod ExportManifestToDefaultPath (skipped if -SkipManifestExport).
  2) Runs Unity batch with RUN_NPC_LLM_SCENARIO_SUITE=1 to execute NpcLlmScenarioBatch.RunScenarioSuiteBatch.

  Endpoint resolution matches NpcLlmDevEndpointResolver (OLLAMA_HOST, NPC_LLM_TEST_*).

.EXAMPLE
  $env:RUN_NPC_LLM_SCENARIO_SUITE = '1'
  $env:OLLAMA_HOST = '127.0.0.1:11436'
  .\scripts\Invoke-NpcLlmScenarioSuite.ps1
#>

param(
    [string] $UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string] $ProjectPath = "",
    [string] $ManifestPath = "",
    [switch] $SkipManifestExport,
    [switch] $ContinueOnFailure
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

$logRoot = Join-Path $ProjectPath "Artifacts\LlmConvo"
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
$expLog = Join-Path $logRoot "unity-scenario-export.log"
$suiteLog = Join-Path $logRoot "unity-scenario-suite.log"

if (-not $SkipManifestExport) {
    $unityArgsExport = @(
        "-batchmode",
        "-nographics",
        "-quit",
        "-projectPath", $ProjectPath,
        "-logFile", $expLog,
        "-executeMethod", "LoreLegacyMonsters.Editor.NpcLlmScenarioManifestGenerator.ExportManifestToDefaultPath"
    )

    Write-Host "Exporting scenario manifest (manifest.jsonl) ..."
    $exp = Start-Process -FilePath $UnityPath -ArgumentList $unityArgsExport -Wait -PassThru -NoNewWindow
    if ($exp.ExitCode -ne 0) {
        if (Test-Path $expLog) { Get-Content $expLog -Tail 30 }
        throw "Manifest export failed ($($exp.ExitCode)). See '$expLog'"
    }
} else {
    Write-Host "Skipping manifest export (-SkipManifestExport)."
}

$env:RUN_NPC_LLM_SCENARIO_SUITE = '1'
if (![string]::IsNullOrWhiteSpace($ManifestPath)) {
    $env:NPC_LLM_SCENARIO_MANIFEST = [System.IO.Path]::GetFullPath($ManifestPath)
} else {
    Remove-Item Env:\NPC_LLM_SCENARIO_MANIFEST -ErrorAction SilentlyContinue
}

$suiteArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-logFile", $suiteLog,
    "-executeMethod", "LoreLegacyMonsters.Editor.NpcLlmScenarioBatch.RunScenarioSuiteBatch"
)

Write-Host "Running scenario suite (requires live LLM per NpcLlmDevEndpointResolver) ..."
$proc = Start-Process -FilePath $UnityPath -ArgumentList $suiteArgs -Wait -PassThru -NoNewWindow

$summary = Join-Path $ProjectPath "Artifacts\LlmConvo\scenarios\run_summary.json"
if (Test-Path $summary) {
    Write-Host "--- run_summary.json ---"
    Get-Content $summary -Raw | Write-Host
}

if ($proc.ExitCode -ne 0) {
    Write-Host "--- unity-scenario-suite.log (tail) ---"
    if (Test-Path $suiteLog) { Get-Content $suiteLog -Tail 50 }
    if ($ContinueOnFailure) {
        exit $proc.ExitCode
    }
    throw "Scenario suite exited with $($proc.ExitCode). See '$suiteLog'"
}

Write-Host "Scenario suite completed. Reports: $ProjectPath\Artifacts\LlmConvo\scenarios\"
return $summary
