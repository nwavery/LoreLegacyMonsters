#Requires -Version 5.1
<#
.SYNOPSIS
  Install/check Ollama, pull the default game model, and smoke-test the OpenAI-compatible API.
  Run from PowerShell:  .\scripts\Setup-Ollama.ps1
#>
$ErrorActionPreference = 'Stop'

$Model = if ($env:OLLAMA_GAME_MODEL) { $env:OLLAMA_GAME_MODEL } else { 'llama3.2' }

function Test-OllamaCli {
    $cmd = Get-Command ollama -ErrorAction SilentlyContinue
    return $null -ne $cmd
}

if (-not (Test-OllamaCli)) {
    Write-Host "Ollama CLI not on PATH."
    Write-Host "Install options:"
    Write-Host "  winget install Ollama.Ollama --accept-package-agreements --accept-source-agreements"
    Write-Host "  Or: https://ollama.com/download"
    exit 1
}

Write-Host "Ollama version:" (ollama --version 2>&1)

Write-Host "`nPulling model '$Model' (large download on first run)..."
ollama pull $Model

Write-Host "`nInstalled models:"
ollama list

$body = @{
    model    = $Model
    messages = @(
        @{ role = "user"; content = "Reply with exactly: OK" }
    )
    stream   = $false
} | ConvertTo-Json -Depth 6 -Compress

Write-Host "`nSmoke test POST http://127.0.0.1:11434/v1/chat/completions ..."
try {
    $resp = Invoke-RestMethod -Uri 'http://127.0.0.1:11434/v1/chat/completions' -Method Post -Body $body -ContentType 'application/json; charset=utf-8' -TimeoutSec 120
    $text = $resp.choices[0].message.content
    Write-Host "Assistant reply:" $text
    Write-Host "`nDone. In Unity, enable Use Local Llm on DialogBootstrap > GameDialogDriver (model name should match: $Model)."
}
catch {
    Write-Warning "API test failed (is Ollama running?). Error: $_"
    Write-Host "Start the Ollama app or run: ollama serve"
    exit 2
}
