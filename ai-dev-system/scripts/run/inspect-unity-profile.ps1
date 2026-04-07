param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ForwardedArguments
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
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
    & python -m app.main inspect --profile unity-editor @ForwardedArguments
    exit $LASTEXITCODE
}
finally {
    $env:PYTHONPATH = $previousPythonPath
    Pop-Location
}
