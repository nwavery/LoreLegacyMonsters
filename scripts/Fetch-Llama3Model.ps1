param(
    [string] $ModelUrl = "https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf?download=true",
    [string] $HfToken = $env:HF_TOKEN,
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

$targetDir = Join-Path $ProjectPath "Assets/StreamingAssets/llm/models"
$targetFile = Join-Path $targetDir "llama3.2-q4_k_m.gguf"
New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$headers = @{}
if (![string]::IsNullOrWhiteSpace($HfToken)) {
    $headers["Authorization"] = "Bearer $HfToken"
}

Write-Host "Downloading model artifact to $targetFile"
if ($headers.Count -gt 0) {
    Invoke-WebRequest -Uri $ModelUrl -OutFile $targetFile -Headers $headers
} else {
    Invoke-WebRequest -Uri $ModelUrl -OutFile $targetFile
}

$size = (Get-Item $targetFile).Length
if ($size -lt 500MB) {
    throw "Downloaded model looks too small ($size bytes). Check license access/token and URL."
}

Write-Host "Model staged successfully: $targetFile ($size bytes)"
