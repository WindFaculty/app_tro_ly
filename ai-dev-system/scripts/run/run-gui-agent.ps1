param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ForwardedArguments
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
$controlPlaneRoot = Join-Path $aiDevSystemRoot "control-plane"
$previousPythonPath = $env:PYTHONPATH

function Resolve-ForwardedArguments {
    param(
        [string[]]$Arguments
    )

    if (-not $Arguments -or $Arguments.Count -eq 0) {
        return @("--help")
    }

    if ($Arguments.Count -gt 1 -and (($Arguments | Where-Object { $_.Length -ne 1 }).Count -eq 0)) {
        return @(-split (($Arguments -join "") -replace "\s+", " ").Trim())
    }

    return @($Arguments)
}

$commandArguments = if ($ForwardedArguments.Count -gt 0) {
    @(Resolve-ForwardedArguments -Arguments $ForwardedArguments)
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
    $allArgs = @("-m", "app.main") + $commandArguments
    & python @allArgs
    exit $LASTEXITCODE
}
finally {
    $env:PYTHONPATH = $previousPythonPath
    Pop-Location
}
