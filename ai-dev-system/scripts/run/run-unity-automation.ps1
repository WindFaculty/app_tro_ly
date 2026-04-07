param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ForwardedArguments
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))

$commandArguments = if ($ForwardedArguments.Count -gt 0) {
    $ForwardedArguments
}
else {
    @("--help")
}

Push-Location $aiDevSystemRoot
try {
    & python run_demo.py @commandArguments
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
