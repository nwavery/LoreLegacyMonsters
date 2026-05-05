param(
    [string] $BuildRoot = (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..")).Path "Build/Steam/Windows"),
    [int] $Port = 11436
)

$ErrorActionPreference = "Stop"

$exe = Join-Path $BuildRoot "LoreLegacyMonsters.exe"
$runtimeDir = Join-Path $BuildRoot "LoreLegacyMonsters_Data/StreamingAssets/llm/runtime"
$ollamaExe = Join-Path $runtimeDir "ollama.exe"
$modelsDir = Join-Path $BuildRoot "LoreLegacyMonsters_Data/StreamingAssets/llm/models"

if (!(Test-Path $exe)) {
    throw "Missing build executable: $exe"
}
if (!(Test-Path $ollamaExe)) {
    throw "Missing bundled ollama runtime: $ollamaExe"
}

Write-Host "Starting bundled Ollama from: $ollamaExe"
$startInfo = New-Object System.Diagnostics.ProcessStartInfo
$startInfo.FileName = $ollamaExe
$startInfo.Arguments = "serve"
$startInfo.WorkingDirectory = $runtimeDir
$startInfo.UseShellExecute = $false
$startInfo.CreateNoWindow = $true
$startInfo.EnvironmentVariables["OLLAMA_HOST"] = "127.0.0.1:$Port"
$startInfo.EnvironmentVariables["OLLAMA_MODELS"] = $modelsDir
$ollama = [System.Diagnostics.Process]::Start($startInfo)
if ($null -eq $ollama) {
    throw "Failed to start bundled Ollama process."
}

try {
    Write-Host "Launching game build: $exe"
    $gameStart = New-Object System.Diagnostics.ProcessStartInfo
    $gameStart.FileName = $exe
    $gameStart.WorkingDirectory = $BuildRoot
    $gameStart.UseShellExecute = $false
    $gameStart.EnvironmentVariables["LLM_EXTERNAL_RUNTIME"] = "1"
    $game = [System.Diagnostics.Process]::Start($gameStart)
    if ($null -eq $game) {
        throw "Failed to start game process."
    }
    Wait-Process -Id $game.Id
}
finally {
    if ($null -ne $ollama -and -not $ollama.HasExited) {
        Write-Host "Stopping bundled Ollama (pid=$($ollama.Id))"
        Stop-Process -Id $ollama.Id -Force
    }
}
