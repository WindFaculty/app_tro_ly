param(
    [string]$OutputDir = "",
    [string]$UnityBuildPath = ""
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $aiDevSystemRoot ".."))
$packageScript = Join-Path $repoRoot "scripts\package_release.ps1"

$forward = @{}
if (-not [string]::IsNullOrWhiteSpace($OutputDir)) {
    $forward["OutputDir"] = $OutputDir
}
if (-not [string]::IsNullOrWhiteSpace($UnityBuildPath)) {
    $forward["UnityBuildPath"] = $UnityBuildPath
}

& $packageScript @forward
exit $LASTEXITCODE
