param(
    [string]$OutputDir = "",
    [string]$UnityBuildPath = ""
)

$ErrorActionPreference = "Stop"

function Copy-DirectoryContents {
    param(
        [string]$Source,
        [string]$Destination
    )

    Get-ChildItem -Path $Source -Force | ForEach-Object {
        Copy-Item -Recurse -Force $_.FullName $Destination
    }
}

$root = Split-Path -Parent $PSScriptRoot
if (-not $OutputDir) {
    $OutputDir = Join-Path $root "release"
}

if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "backend") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $OutputDir "scripts") | Out-Null

$backendOutput = Join-Path $OutputDir "backend"
Copy-DirectoryContents -Source (Join-Path $root "local-backend") -Destination $backendOutput
Copy-Item -Force (Join-Path $root "scripts\run_all.ps1") (Join-Path $OutputDir "scripts\run_all.ps1")
Copy-Item -Force (Join-Path $root "scripts\setup_windows.ps1") (Join-Path $OutputDir "scripts\setup_windows.ps1")

if ($UnityBuildPath) {
    if (-not (Test-Path $UnityBuildPath)) {
        throw "Unity build path not found: $UnityBuildPath"
    }

    $clientOutput = Join-Path $OutputDir "client"
    New-Item -ItemType Directory -Force -Path $clientOutput | Out-Null

    $unityBuildItem = Get-Item $UnityBuildPath
    if ($unityBuildItem.PSIsContainer) {
        Copy-DirectoryContents -Source $unityBuildItem.FullName -Destination $clientOutput
    }
    else {
        throw "UnityBuildPath must point to the built client directory, not a single file."
    }
}

Write-Host "Release package prepared at $OutputDir"
