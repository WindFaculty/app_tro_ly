param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ForwardedArguments
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
$controlPlaneRoot = Join-Path $aiDevSystemRoot "control-plane"
$previousPythonPath = $env:PYTHONPATH

$commandArguments = if ($ForwardedArguments.Count -gt 0) {
    $ForwardedArguments
}
else {
    @("--help")
}

Push-Location $aiDevSystemRoot
try {
    $env:PYTHONPATH = if ([string]::IsNullOrWhiteSpace($previousPythonPath)) {
        "$controlPlaneRoot;$aiDevSystemRoot"
    }
    else {
        "$controlPlaneRoot;$aiDevSystemRoot;$previousPythonPath"
    }
    & python -m app.main @commandArguments
    exit $LASTEXITCODE
}
finally {
    $env:PYTHONPATH = $previousPythonPath
    Pop-Location
}
