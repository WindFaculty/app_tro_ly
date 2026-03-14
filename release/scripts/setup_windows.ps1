param(
    [string]$BackendPython = "python"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot

function Resolve-BackendDirectory {
    param([string]$Root)

    $candidates = @(
        (Join-Path $Root "local-backend"),
        (Join-Path $Root "backend"),
        (Join-Path $Root "backend\local-backend")
    )

    foreach ($candidate in $candidates) {
        if ((Test-Path (Join-Path $candidate "requirements.txt")) -and (Test-Path (Join-Path $candidate "run_local.py"))) {
            return $candidate
        }
    }

    throw "Backend folder not found. Checked: $($candidates -join ', ')"
}

$backend = Resolve-BackendDirectory -Root $root

Write-Host "Installing local-backend Python dependencies..."
Write-Host ("Resolved backend path: " + $backend)
Push-Location $backend
try {
    & $BackendPython -m pip install -r requirements.txt
    if ($LASTEXITCODE -ne 0) {
        throw "Python dependency install failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

Write-Host ""
Write-Host "Optional runtime environment variables:"
Write-Host "  assistant_llm_provider=gemini"
Write-Host "  assistant_gemini_api_key=<Gemini API key>"
Write-Host "  assistant_gemini_model=gemini-2.5-flash"
Write-Host "  assistant_gemini_base_url=https://generativelanguage.googleapis.com/v1beta/openai"
Write-Host "  assistant_llm_provider=groq"
Write-Host "  assistant_groq_api_key=<Groq API key>"
Write-Host "  assistant_groq_model=llama-3.1-8b-instant"
Write-Host "  assistant_groq_base_url=https://api.groq.com/openai/v1"
Write-Host "  assistant_enable_ollama=true"
Write-Host "  assistant_ollama_base_url=http://127.0.0.1:11434"
Write-Host "  assistant_ollama_model=llama3.1:8b"
Write-Host "  assistant_whisper_command=<path to whisper-cli.exe>"
Write-Host "  assistant_whisper_model_path=<path to ggml model>"
Write-Host "  assistant_piper_command=<path to piper.exe>"
Write-Host "  assistant_piper_model_path=<path to piper model>"
Write-Host ""
Write-Host "Backend setup complete."
