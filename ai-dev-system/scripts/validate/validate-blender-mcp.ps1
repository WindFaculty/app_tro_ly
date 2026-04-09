$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$validatorPath = Join-Path $scriptRoot "..\..\verify_blender_connection.py"
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
$controlPlaneRoot = Join-Path $aiDevSystemRoot "control-plane"
$previousPythonPath = $env:PYTHONPATH

Push-Location $aiDevSystemRoot
try {
    $env:PYTHONPATH = if ([string]::IsNullOrWhiteSpace($previousPythonPath)) {
        "$controlPlaneRoot;$aiDevSystemRoot"
    }
    else {
        "$controlPlaneRoot;$aiDevSystemRoot;$previousPythonPath"
    }
    & python $validatorPath
    exit $LASTEXITCODE
}
finally {
    $env:PYTHONPATH = $previousPythonPath
    Pop-Location
}
