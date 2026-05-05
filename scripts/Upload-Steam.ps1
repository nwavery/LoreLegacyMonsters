param(
    [Parameter(Mandatory = $true)]
    [string] $SteamCmdPath,
    [Parameter(Mandatory = $true)]
    [string] $SteamUser,
    [Parameter(Mandatory = $true)]
    [string] $SteamPassword,
    [string] $ProjectPath = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [string] $AppBuildVdf = "tools/steam/app_build.vdf"
)

$ErrorActionPreference = "Stop"
$appBuildPath = Join-Path $ProjectPath $AppBuildVdf
if (!(Test-Path $appBuildPath)) {
    throw "Missing app build VDF: $appBuildPath"
}
if (!(Test-Path $SteamCmdPath)) {
    throw "steamcmd not found at: $SteamCmdPath"
}

$args = @(
    "+login", $SteamUser, $SteamPassword,
    "+run_app_build", "`"$appBuildPath`"",
    "+quit"
)

Write-Host "Uploading Steam build via steamcmd..."
$p = Start-Process -FilePath $SteamCmdPath -ArgumentList $args -Wait -PassThru
if ($p.ExitCode -ne 0) {
    throw "steamcmd upload failed with exit code $($p.ExitCode)"
}

Write-Host "Steam upload completed."
