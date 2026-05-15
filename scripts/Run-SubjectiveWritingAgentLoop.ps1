#Requires -Version 5.1
<#
.SYNOPSIS
  Repeatedly asks a local LLM (Ollama OpenAI-compatible API) to propose subjective writing tweaks.

.DESCRIPTION
  This is NOT Start-Improvements: it does not run Unity or mutate C# automatically.
  Each cycle POSTs a capped excerpt bundle to the model and saves output under Artifacts/LlmConvo/writing_agent/.

  IMPORTANT: Long runs do not change the game by themselves. A human or Cursor agent must merge proposals into
  NpcContentRegistry / NpcLlmPromptRules / NpcLlmScenarioEvaluator, then run Start-Improvements.ps1 to validate.

  The model must reply with JSON only (see system prompt). Invalid JSON is still saved with a warning header
  so you can diagnose model drift.

.PARAMETER DurationMinutes
  Wall-clock cap (default 240 = 4 hours).

.PARAMETER CycleSeconds
  Minimum spacing between Ollama calls (default 720 = 12 minutes).

.EXAMPLE
  powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\Run-SubjectiveWritingAgentLoop.ps1 -DurationMinutes 240
#>
param(
    [string] $ProjectPath = "",
    [int] $DurationMinutes = 240,
    [int] $CycleSeconds = 720,
    [string] $CompletionsUrl = "",
    [string] $Model = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $scriptDir = if ([string]::IsNullOrWhiteSpace($PSScriptRoot)) {
        Split-Path -Parent $MyInvocation.MyCommand.Path
    } else { $PSScriptRoot }
    $ProjectPath = (Resolve-Path (Join-Path $scriptDir "..")).Path
}

if ($DurationMinutes -lt 1) { throw "DurationMinutes must be >= 1" }
if ($CycleSeconds -lt 60) { throw "CycleSeconds must be >= 60 (avoid hammering Ollama)" }

if ([string]::IsNullOrWhiteSpace($CompletionsUrl)) {
    $CompletionsUrl = [Environment]::GetEnvironmentVariable("NPC_LLM_TEST_CHAT_COMPLETIONS_URL")
}
if ([string]::IsNullOrWhiteSpace($CompletionsUrl)) {
    $CompletionsUrl = "http://127.0.0.1:11434/v1/chat/completions"
}

if ([string]::IsNullOrWhiteSpace($Model)) {
    $Model = [Environment]::GetEnvironmentVariable("OLLAMA_GAME_MODEL")
}
if ([string]::IsNullOrWhiteSpace($Model)) {
    $Model = "llama3.2:latest"
}

function Read-CappedText([string] $path, [int] $maxChars) {
    if (-not (Test-Path -LiteralPath $path)) { return "(missing file: $path)" }
    $raw = Get-Content -LiteralPath $path -Raw -Encoding utf8
    if ($null -eq $raw) { return "" }
    if ($raw.Length -le $maxChars) { return $raw }
    return $raw.Substring(0, $maxChars) + "`n`n[... truncated ...]"
}

function Try-ParseProposalJson([string] $raw, [ref] $parsedObj) {
    $parsedObj.Value = $null
    if ([string]::IsNullOrWhiteSpace($raw)) { return $false }
    $t = $raw.Trim()
    if ($t.StartsWith("```")) {
        $t = $t -replace '^```(?:json)?\s*', ''
        $t = $t -replace '\s*```\s*$', ''
    }
    try {
        $parsedObj.Value = $t | ConvertFrom-Json
        return $true
    }
    catch {
        return $false
    }
}

function Test-ProposalShape($o) {
    if ($null -eq $o) { return $false }
    foreach ($k in @("branch", "text", "rationale", "risks")) {
        if (-not ($o.PSObject.Properties.Name -contains $k)) { return $false }
    }
    $b = [string]$o.branch
    $allowed = @("GLOBAL", "NPC", "EVAL", "NO_EDIT")
    if ($allowed -notcontains $b) { return $false }
    $tx = [string]$o.text
    $rat = [string]$o.rationale
    $rk = [string]$o.risks
    if ($tx.Length -gt 1600 -or $rat.Length -gt 500 -or $rk.Length -gt 300) { return $false }
    return $true
}

$outDir = Join-Path $ProjectPath "Artifacts\LlmConvo\writing_agent"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$ndjson = Join-Path $outDir ("writing_agent_{0}.ndjson" -f (Get-Date -Format "yyyyMMdd_HHmmss"))
$deadline = [datetime]::UtcNow.AddMinutes($DurationMinutes)
$iter = 0

$biblePath = Join-Path $ProjectPath "docs\story_bible.md"
$rulesPath = Join-Path $ProjectPath "Assets\Scripts\Dialog\Llm\NpcLlmPromptRules.cs"
$registryPath = Join-Path $ProjectPath "Assets\Scripts\World\NpcContentRegistry.cs"
$evalPath = Join-Path $ProjectPath "Assets\Scripts\Dialog\Llm\NpcLlmScenarioEvaluator.cs"
$summaryPath = Join-Path $ProjectPath "Artifacts\LlmConvo\scenarios\run_summary.json"

$systemPrompt = @"
You are a senior narrative designer for a fantasy monster-collecting RPG (Hollowfen / Wilderward).
You will receive excerpts: story bible, NpcLlmPromptRules.cs, NpcContentRegistry.cs, NpcLlmScenarioEvaluator.cs, run_summary.json.

Reply with a SINGLE JSON object ONLY. No markdown, no prose before or after. Keys:
- "branch": one of GLOBAL | NPC | EVAL | NO_EDIT
- "text": string, max ~900 characters. For GLOBAL/NPC/EVAL: one surgical instruction or forbid-line description. For NO_EDIT: "".
- "rationale": string, max ~400 characters, why this change helps voice or anti-meta.
- "risks": string, max ~200 characters, merge risks for a human engineer.

Rules: do not spoil hidden twist architecture as established canon to the player; tune craft and tone only.
Do not output PowerShell, C#, file paths, or refactors. Plain English only inside JSON string values.
If nothing is worth changing this cycle, use branch NO_EDIT and empty text.
"@

while ([datetime]::UtcNow -lt $deadline) {
    $iter++
    $iterStart = [datetime]::UtcNow

    $bible = Read-CappedText $biblePath 6000
    $rules = Read-CappedText $rulesPath 5000
    $registry = Read-CappedText $registryPath 9000
    $eval = Read-CappedText $evalPath 5000
    $runSummary = Read-CappedText $summaryPath 2500

    $userBlock = @"
=== STORY_BIBLE (excerpt) ===
$bible

=== NpcLlmPromptRules.cs (excerpt) ===
$rules

=== NpcContentRegistry.cs (excerpt) ===
$registry

=== NpcLlmScenarioEvaluator.cs (excerpt) ===
$eval

=== run_summary.json (excerpt, may be stale) ===
$runSummary

Cycle $iter. If run_summary shows failures, bias text toward fixing those failure modes; else pick the weakest cast voice vs story bible.
Output JSON only as instructed in system message.
"@

    $bodyObj = @{
        model       = $Model
        stream      = $false
        temperature = 0.15
        messages    = @(
            @{ role = "system"; content = $systemPrompt }
            @{ role = "user"; content = $userBlock }
        )
    }
    $body = $bodyObj | ConvertTo-Json -Depth 8 -Compress

    $exit = 0
    $reply = ""
    try {
        $resp = Invoke-RestMethod -Uri $CompletionsUrl -Method Post -Body $body -ContentType "application/json; charset=utf-8" -TimeoutSec 300
        $reply = $resp.choices[0].message.content
        if ([string]::IsNullOrWhiteSpace($reply)) { $reply = "{}" }
    }
    catch {
        $reply = "ERROR: $($_.Exception.Message)"
        $exit = 1
    }

    $parsed = $null
    $jsonOk = $false
    $shapeOk = $false
    if ($exit -eq 0) {
        $ref = [ref]$parsed
        $jsonOk = Try-ParseProposalJson $reply $ref
        if ($jsonOk) { $shapeOk = Test-ProposalShape $parsed }
    }

    $stamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $mdPath = Join-Path $outDir "proposal_${stamp}_iter${iter}.md"
    $header = "# Writing-agent proposal (iter $iter)`n`n- completionsUrl: $CompletionsUrl`n- model: $Model`n- jsonOk: $jsonOk`n- shapeOk: $shapeOk`n- utc: $(([datetime]::UtcNow).ToString('o'))`n`n---`n`n"
    $jsonPretty = if ($jsonOk) { ($parsed | ConvertTo-Json -Depth 5) } else { "" }
    $bodyOut = if ($jsonOk -and $shapeOk) {
        "## Parsed proposal (JSON)`n$jsonPretty`n`n## Raw model output`n$reply`n"
    }
    elseif ($jsonOk) {
        "## JSON parse OK but shape invalid (expected branch, text, rationale, risks)`n$jsonPretty`n`n## Raw`n$reply`n"
    }
    else {
        "## WARNING: model did not return valid JSON. Merge by hand only after inspection.`n`n$reply`n"
    }
    ($header + $bodyOut) | Out-File -FilePath $mdPath -Encoding utf8

    $row = [ordered]@{
        utc        = ([datetime]::UtcNow.ToString("o"))
        iteration  = $iter
        exitCode   = $exit
        model      = $Model
        mdPath     = $mdPath
        jsonOk     = $jsonOk
        shapeOk    = $shapeOk
        charsReply = $reply.Length
    }
    ($row | ConvertTo-Json -Compress) | Add-Content -Path $ndjson -Encoding utf8

    $elapsed = ([datetime]::UtcNow - $iterStart).TotalSeconds
    Write-Host "[writing-agent iter $iter exit=$exit jsonOk=$jsonOk shapeOk=$shapeOk elapsed=${elapsed}s] -> $mdPath"

    $sleepSec = [int][Math]::Max(1, $CycleSeconds - $elapsed)
    if ([datetime]::UtcNow.AddSeconds($sleepSec) -ge $deadline) { break }
    Start-Sleep -Seconds $sleepSec
}

Write-Host "Writing-agent loop finished $iter iteration(s). NDJSON: $ndjson"
Write-Host "Reminder: proposals are not auto-applied. Merge into C# sources and run Start-Improvements.ps1."
exit 0
