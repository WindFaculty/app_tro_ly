$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$validatorPath = Join-Path $scriptRoot "validate_phase9_architecture_lock.py"
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))

Push-Location $aiDevSystemRoot
try {
    & python $validatorPath
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
