<#
.SYNOPSIS
  Run one NPC LLM dialog turn via Unity Editor using the game's prompt builder / sanitizer pipeline.

.DESCRIPTION
  Writes Artifacts/LlmConvo/cli-response.json (default) unless -ResponsePath overrides.
  Request JSON defaults to tools/convo/request.json; if missing, copies from tools/convo/request.example.json.

  Endpoint + model resolve like integration tests ($env:NPC_LLM_TEST_*, $env:OLLAMA_HOST).

.EXAMPLE
  .\Invoke-NpcLlmConversation.ps1
  .\Invoke-NpcLlmConversation.ps1 -ProjectPath 'C:\LLMRPG\LoreLegacyMonsters'

.EXAMPLE (after editing request.json playerMessage / conversationHistorySummary)
  .\Invoke-NpcLlmConversation.ps1; Get-Content ..\Artifacts\LlmConvo\cli-response.json
#>

param(
    [string] $UnityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.5f1\Editor\Unity.exe",
    [string] $ProjectPath = "",
    [string] $RequestPath = "",
    [string] $ResponsePath = ""
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

$convoDir = Join-Path $ProjectPath "tools\convo"
$example = Join-Path $convoDir "request.example.json"
if ([string]::IsNullOrWhiteSpace($RequestPath)) {
    $RequestPath = Join-Path $convoDir "request.json"
}

if (!(Test-Path $RequestPath)) {
    if (!(Test-Path $example)) {
        throw "Missing request JSON. Expected '$RequestPath' or template '$example'."
    }

    Copy-Item $example $RequestPath -Force
}

if ([string]::IsNullOrWhiteSpace($ResponsePath)) {
    $respDir = Join-Path $ProjectPath "Artifacts\LlmConvo"
    New-Item -ItemType Directory -Force -Path $respDir | Out-Null
    $ResponsePath = Join-Path $respDir "cli-response.json"
}

$logDir = Join-Path $ProjectPath "Artifacts\LlmConvo"
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir "unity-npc-llm-convo.log"

$env:NPC_LLM_CONVO_REQUEST = [System.IO.Path]::GetFullPath($RequestPath)
$env:NPC_LLM_CONVO_RESPONSE = [System.IO.Path]::GetFullPath($ResponsePath)

$unityArgs = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath", $ProjectPath,
    "-logFile", $log,
    "-executeMethod", "LoreLegacyMonsters.Editor.NpcLlmConvoCli.RunOneTurnBatch"
)

Write-Host "NPC LLM convo CLI"
Write-Host "  request:  $($env:NPC_LLM_CONVO_REQUEST)"
Write-Host "  response: $($env:NPC_LLM_CONVO_RESPONSE)"
Write-Host "  log:      $log"

$proc = Start-Process -FilePath $UnityPath -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow

if (!(Test-Path $ResponsePath)) {
    Write-Warning "Response file missing. See Unity log: $log"
}

if ($proc.ExitCode -ne 0) {
    Write-Host "--- Unity log (tail) ---"
    if (Test-Path $log) { Get-Content $log -Tail 40 }
    throw "Unity exited with $($proc.ExitCode). See '$log'"
}

Write-Host "--- cleanedForUi (from response JSON) ---"
$r = Get-Content $ResponsePath -Raw | ConvertFrom-Json
$clean = $r.cleanedForUi
if ([string]::IsNullOrWhiteSpace([string]$clean)) {
    Write-Host "(empty)"
} else {
    Write-Host $clean
}

return $ResponsePath
