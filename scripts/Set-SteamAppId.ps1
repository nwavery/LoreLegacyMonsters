param(
    [Parameter(Mandatory = $true)]
    [uint32] $AppId,
    [string] $DepotMainId = "",
    [string] $DepotLlmId = "",
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
)

$ErrorActionPreference = "Stop"

function Set-FileText([string]$path, [string]$content) {
    Set-Content -Path $path -Value $content -Encoding UTF8
    Write-Host "Updated $path"
}

$steamConfig = Join-Path $ProjectPath "Assets/Scripts/Platform/Steam/SteamConfig.cs"
$steamAppIdTxt = Join-Path $ProjectPath "steam_appid.txt"
$appBuildVdf = Join-Path $ProjectPath "tools/steam/app_build.vdf"
$mainDepotVdf = Join-Path $ProjectPath "tools/steam/depot_build_main.vdf"
$llmDepotVdf = Join-Path $ProjectPath "tools/steam/depot_build_llm.vdf"

foreach ($path in @($steamConfig, $steamAppIdTxt, $appBuildVdf, $mainDepotVdf, $llmDepotVdf)) {
    if (!(Test-Path $path)) {
        throw "Missing required file: $path"
    }
}

$configText = Get-Content $steamConfig -Raw
$configText = [regex]::Replace($configText, "public const uint AppId = \d+;", "public const uint AppId = $AppId;")
Set-FileText -path $steamConfig -content $configText

Set-FileText -path $steamAppIdTxt -content "$AppId"

$appBuildText = Get-Content $appBuildVdf -Raw
$appBuildText = [regex]::Replace($appBuildText, '"appid"\s+"[^"]+"', "`"appid`"             `"$AppId`"")
Set-FileText -path $appBuildVdf -content $appBuildText

if (![string]::IsNullOrWhiteSpace($DepotMainId)) {
    $mainText = Get-Content $mainDepotVdf -Raw
    $mainText = [regex]::Replace($mainText, '"DepotID"\s+"[^"]+"', "`"DepotID`"   `"$DepotMainId`"")
    Set-FileText -path $mainDepotVdf -content $mainText

    $appBuildText = Get-Content $appBuildVdf -Raw
    $appBuildText = [regex]::Replace($appBuildText, '"REPLACE_DEPOT_MAIN"|"\d+"\s+"tools/steam/depot_build_main.vdf"', "`"$DepotMainId`"      `"tools/steam/depot_build_main.vdf`"")
    Set-FileText -path $appBuildVdf -content $appBuildText
}

if (![string]::IsNullOrWhiteSpace($DepotLlmId)) {
    $llmText = Get-Content $llmDepotVdf -Raw
    $llmText = [regex]::Replace($llmText, '"DepotID"\s+"[^"]+"', "`"DepotID`"   `"$DepotLlmId`"")
    Set-FileText -path $llmDepotVdf -content $llmText

    $appBuildText = Get-Content $appBuildVdf -Raw
    $appBuildText = [regex]::Replace($appBuildText, '"REPLACE_DEPOT_LLM"|"\d+"\s+"tools/steam/depot_build_llm.vdf"', "`"$DepotLlmId`"       `"tools/steam/depot_build_llm.vdf`"")
    Set-FileText -path $appBuildVdf -content $appBuildText
}

Write-Host "Steam IDs updated. AppId=$AppId DepotMainId=$DepotMainId DepotLlmId=$DepotLlmId"
