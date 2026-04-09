param(
    [ValidateSet("workflow", "gui", "integration")]
    [string]$Lane = "workflow",

    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ForwardedArguments
)

$ErrorActionPreference = "Stop"

# Workflow lane contract: equivalent to `python run_demo.py ...`

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

    return ,$Arguments
}

$commandArguments = if ($ForwardedArguments.Count -gt 0) {
    @(Resolve-ForwardedArguments -Arguments $ForwardedArguments)
}
else {
    @("--help")
}
$integrationCommand = if ($commandArguments.Count -gt 0) {
    [string]::Concat($commandArguments[0]).Trim().ToLowerInvariant()
}
else {
    "--help"
}

Push-Location $aiDevSystemRoot
try {
    $env:PYTHONPATH = if ([string]::IsNullOrWhiteSpace($previousPythonPath)) {
        "$controlPlaneRoot;$aiDevSystemRoot"
    }
    else {
        "$controlPlaneRoot;$aiDevSystemRoot;$previousPythonPath"
    }
    switch ($Lane) {
        "workflow" {
            $allArgs = @("run_demo.py") + $commandArguments
            & python @allArgs
        }
        "gui" {
            $allArgs = @("-m", "app.main") + $commandArguments
            & python @allArgs
        }
        "integration" {
            if ($commandArguments.Count -eq 0 -or $integrationCommand -eq "--help") {
                & python "verify_unity_integration.py"
            }
            elseif ($integrationCommand -eq "verify") {
                & python "verify_unity_integration.py"
            }
            elseif ($integrationCommand -eq "cli-e2e") {
                & python "scripts/validate/validate_unity_cli_loop_e2e.py"
            }
            elseif ($integrationCommand -eq "capabilities" -or $integrationCommand -eq "list-capabilities") {
                $allArgs = @("-m", "app.main", "list-capabilities", "--profile", "unity-editor")
                & python @allArgs
            }
            elseif ($integrationCommand -eq "workflow") {
                $allArgs = @("run_demo.py") + @($commandArguments | Select-Object -Skip 1)
                & python @allArgs
            }
            else {
                $allArgs = @("-m", "app.main") + $commandArguments
                & python @allArgs
            }
        }
    }
    exit $LASTEXITCODE
}
finally {
    $env:PYTHONPATH = $previousPythonPath
    Pop-Location
}
