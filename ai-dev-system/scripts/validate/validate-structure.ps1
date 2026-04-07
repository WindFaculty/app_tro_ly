$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $PSCommandPath
$aiDevSystemRoot = [System.IO.Path]::GetFullPath((Join-Path $scriptRoot "..\.."))
$phase6Validator = Join-Path $aiDevSystemRoot "asset-pipeline\validate-phase6-structure.ps1"
$phase7Validator = Join-Path $scriptRoot "validate_phase7_structure.py"
$phase9Validator = Join-Path $scriptRoot "validate_phase9_architecture_lock.py"

& powershell -NoProfile -ExecutionPolicy Bypass -File $phase6Validator
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Push-Location $aiDevSystemRoot
try {
    & python $phase7Validator
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    & python $phase9Validator
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}
