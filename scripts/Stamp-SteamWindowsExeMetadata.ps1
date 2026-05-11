param(
    [Parameter(Mandatory = $true)]
    [string] $ExePath,
    [string] $BundleVersion = "1.0.0",
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string] $ProductName = "Lore, Legacy, and Monsters",
    [string] $CompanyName = "NA Dev",
    [string] $LegalCopyright = "Copyright (c) 2026 NA Dev"
)

$ErrorActionPreference = "Stop"

if (!(Test-Path $ExePath)) {
    throw "EXE not found: $ExePath"
}

$rcedit = Join-Path $ProjectPath "tools\win\rcedit.exe"
if (!(Test-Path $rcedit)) {
    Write-Host "rcedit not found; fetching..."
    & (Join-Path $ProjectPath "scripts\Fetch-Rcedit.ps1") -ProjectPath $ProjectPath
}

$parts = @($BundleVersion -split '\.' | ForEach-Object { $_.Trim() })
while ($parts.Count -lt 4) {
    $parts += "0"
}
$fourPartVersion = ($parts[0..3] -join ".")

Write-Host "Stamping Windows EXE metadata: $ExePath (file/product version $fourPartVersion)"

& $rcedit $ExePath `
    --set-version-string FileDescription $ProductName `
    --set-version-string ProductName $ProductName `
    --set-version-string CompanyName $CompanyName `
    --set-version-string LegalCopyright $LegalCopyright `
    --set-version-string InternalName "LoreLegacyMonsters" `
    --set-version-string OriginalFilename "LoreLegacyMonsters.exe" `
    --set-file-version $fourPartVersion `
    --set-product-version $fourPartVersion

if ($LASTEXITCODE -ne 0) {
    throw "rcedit failed with exit code $LASTEXITCODE"
}

Write-Host "Windows EXE metadata stamping complete."
