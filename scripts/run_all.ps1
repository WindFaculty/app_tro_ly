param(
    [string]$UnityExecutablePath = "",
    [string]$BackendPython = "python"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$healthUrl = "http://127.0.0.1:8096/v1/health"

function Resolve-BackendDirectory {
    param([string]$Root)

    $candidates = @(
        (Join-Path $Root "local-backend"),
        (Join-Path $Root "backend"),
        (Join-Path $Root "backend\local-backend")
    )

    foreach ($candidate in $candidates) {
        if ((Test-Path (Join-Path $candidate "run_local.py")) -and (Test-Path (Join-Path $candidate "requirements.txt"))) {
            return $candidate
        }
    }

    throw "Backend folder not found. Checked: $($candidates -join ', ')"
}

function Test-PortReady {
    param([string]$Url)
    try {
        Invoke-RestMethod -Uri $Url -TimeoutSec 2 | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

function Wait-PortReady {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 15
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-PortReady -Url $Url) {
            return $true
        }

        Start-Sleep -Milliseconds 500
    }

    return $false
}

function Resolve-ClientExecutable {
    param(
        [string]$Root,
        [string]$ExplicitPath
    )

    if ($ExplicitPath) {
        if (-not (Test-Path $ExplicitPath)) {
            throw "Unity executable not found: $ExplicitPath"
        }

        return (Resolve-Path $ExplicitPath).Path
    }

    $clientDir = Join-Path $Root "client"
    if (-not (Test-Path $clientDir)) {
        return $null
    }

    $candidates = Get-ChildItem -Path $clientDir -Filter "*.exe" -File -Recurse |
        Where-Object { $_.Name -ne "UnityCrashHandler64.exe" } |
        Sort-Object FullName

    if ($candidates.Count -eq 0) {
        return $null
    }

    if ($candidates.Count -gt 1) {
        Write-Warning ("Multiple client executables found. Starting the first one: " + $candidates[0].FullName)
    }

    return $candidates[0].FullName
}

$backend = Resolve-BackendDirectory -Root $root
$clientExecutable = Resolve-ClientExecutable -Root $root -ExplicitPath $UnityExecutablePath

Write-Host ("Resolved backend path: " + $backend)
Write-Host "Starting local backend..."
$backendProcess = Start-Process powershell -PassThru -WorkingDirectory $backend -ArgumentList @(
    "-NoExit",
    "-Command",
    "$BackendPython run_local.py"
)
Write-Host ("Backend console PID: " + $backendProcess.Id)

if (Wait-PortReady -Url $healthUrl) {
    $health = Invoke-RestMethod -Uri $healthUrl
    Write-Host ("Backend health: " + $health.status)
    if ($null -ne $health.runtimes -and $null -ne $health.runtimes.llm) {
        $provider = if ([string]::IsNullOrWhiteSpace($health.runtimes.llm.provider)) { "llm" } else { $health.runtimes.llm.provider }
        $model = if ([string]::IsNullOrWhiteSpace($health.runtimes.llm.model)) { "n/a" } else { $health.runtimes.llm.model }
        Write-Host ("LLM provider: " + $provider + " | model: " + $model + " | ready: " + $health.runtimes.llm.available)
    }
}
else {
    throw "Backend health endpoint did not become ready yet."
}

if ($clientExecutable) {
    Write-Host "Starting Unity client build..."
    Write-Host ("Resolved client path: " + $clientExecutable)
    Start-Process -FilePath $clientExecutable
}
else {
    Write-Host "No packaged client executable found. Open the Unity project from 'unity-client/' or pass -UnityExecutablePath."
}
