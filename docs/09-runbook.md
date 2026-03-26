# Runbook

Step-by-step runbook for setting up, validating, packaging, and troubleshooting the local desktop assistant on Windows.

## Scope

- Target machine: Windows desktop or laptop with PowerShell and Python available.
- Repo-backed flows covered here:
  - backend dependency setup
  - runtime preflight checks
  - local startup and health validation
  - backend smoke validation
  - release folder packaging and verification
  - common failure diagnosis
- Out of scope:
  - Unity visual polish sign-off that requires Editor or built client interaction
  - machine-specific runtime installation that is still tracked in `tasks/task-people.md`

## 1. Prerequisites

Before running setup:

- Python 3.11+ should be available as `python`
- PowerShell should be available
- The repo should already be checked out locally
- If you want non-degraded speech:
  - Piper executable and voice model paths must exist
  - Whisper runtime or faster-whisper dependencies must exist
- If you want cloud LLM access:
  - configure Groq or Gemini environment variables before startup

Useful repo paths:

- backend: `local-backend/`
- helper scripts: `scripts/`
- task tracker: `tasks/task-queue.md`
- manual blocker tracker: `tasks/task-people.md`

## 2. Fresh Machine Setup

From the repo root:

```powershell
.\scripts\setup_windows.ps1
```

What this does:

- resolves the backend folder automatically
- installs `local-backend/requirements.txt`
- checks core Python modules needed by the backend
- runs runtime preflight diagnostics for configured STT, TTS, and Ollama-adjacent paths

Expected outcome:

- exit code `0`
- log ends with `[assistant][ok] Backend setup complete.`

If setup fails:

- missing Python modules:
  - rerun `python -m pip install -r local-backend\requirements.txt`
- bad Piper or Whisper path:
  - fix the corresponding `assistant_*` environment variable or `.env` value
- Ollama warning only:
  - warning is acceptable unless your intended LLM provider is Ollama

## 3. Runtime Configuration

The backend reads `.env` under `local-backend/` and also reads `assistant_*` environment variables from the current shell.

Common variables:

- `assistant_llm_provider`
- `assistant_groq_api_key`
- `assistant_gemini_api_key`
- `assistant_tts_provider`
- `assistant_piper_command`
- `assistant_piper_model_path`
- `assistant_stt_provider`
- `assistant_whisper_command`
- `assistant_whisper_model_path`
- `assistant_faster_whisper_model_path`

Recommended starting points:

- safer Windows TTS fallback:
  - `assistant_tts_provider=piper`
- if validating ChatTTS:
  - `assistant_tts_provider=chattts`
  - `assistant_chattts_compile=false`
- if you use hybrid routing:
  - configure both `assistant_groq_api_key` and `assistant_gemini_api_key` to avoid partial-mode LLM warnings

Example PowerShell session:

```powershell
$env:assistant_llm_provider = "gemini"
$env:assistant_gemini_api_key = "<your key>"
$env:assistant_tts_provider = "piper"
$env:assistant_piper_command = "C:\runtime\piper\piper.exe"
$env:assistant_piper_model_path = "C:\runtime\piper\vi-VN-default.onnx"
```

## 4. Local Startup And Health Check

Run the integrated startup flow from the repo root:

```powershell
.\scripts\run_all.ps1
```

Optional arguments:

```powershell
.\scripts\run_all.ps1 -BackendPython python -UnityExecutablePath "D:\Builds\Client\TroLy.exe"
.\scripts\run_all.ps1 -BackendPython python -ShutdownBackendOnExit
```

What this does:

- verifies backend Python dependencies
- runs runtime preflight diagnostics
- starts `local-backend/run_local.py`
- waits for `http://127.0.0.1:8096/v1/health`
- optionally launches a packaged Unity client executable
- optionally stops the backend again after validation when `-ShutdownBackendOnExit` is used

Health interpretation:

- `ready`: backend and configured runtimes are available
- `partial`: backend is usable, but some optional runtime is degraded
- `error`: do not continue; fix the reported health issue first

Quick manual health check:

```powershell
Invoke-RestMethod http://127.0.0.1:8096/v1/health | ConvertTo-Json -Depth 6
```

Validation-only startup tip:

- use `.\scripts\run_all.ps1 -ShutdownBackendOnExit` when you want to prove setup, preflight, and startup work without leaving the backend running after the script exits

## 5. Backend Smoke Validation

After the backend is running:

```powershell
python .\scripts\smoke_backend.py
```

Useful variants:

```powershell
python .\scripts\smoke_backend.py --base-url http://127.0.0.1:8096 --allow-health-status ready partial
python .\scripts\smoke_backend.py --timeout 20
```

What the smoke script verifies:

- `/v1/health` is reachable
- task create, update, reschedule, complete flows
- `/v1/events` sequencing
- `/v1/assistant/stream` reaches `assistant_final`
- degraded or available behavior for `/v1/speech/tts`
- degraded or available behavior for `/v1/speech/stt`

Expected outcome:

- exit code `0`
- JSON summary printed to stdout

Treat smoke failure as blocking if:

- health never becomes reachable
- `assistant_final` is missing from stream events
- STT or TTS returns a status that does not match health availability

## 6. Release Packaging

Create a release folder from the repo root:

```powershell
.\scripts\package_release.ps1
```

Optional packaged client copy:

```powershell
.\scripts\package_release.ps1 -UnityBuildPath "D:\Builds\TroLyClient"
```

Optional custom output path:

```powershell
.\scripts\package_release.ps1 -OutputDir "D:\Releases\TroLy"
```

What packaging validates automatically:

- output path is not empty, repo root, or drive root
- backend files are copied into `release\backend`
- helper scripts are copied into `release\scripts`
- smoke helpers and fake Piper assets are present
- if `-UnityBuildPath` is supplied, at least one client `.exe` exists in `release\client`

Expected outcome:

- exit code `0`
- log ends with `[assistant][ok] Release package prepared successfully.`

Script exit-code map:

- `setup_windows.ps1`
  - `10`: backend Python command could not be resolved
  - `11`: dependency installation or post-install dependency validation failed
  - `12`: runtime preflight diagnostics failed
- `run_all.ps1`
  - `20`: backend Python command could not be resolved
  - `21`: backend dependency preflight failed
  - `22`: runtime preflight diagnostics failed
  - `23`: backend port was already in use
  - `24`: backend process could not be started
  - `25`: backend health never became ready
  - `26`: health endpoint reported an error state
  - `27`: Unity client launch failed
  - `28`: backend shutdown-after-validation failed
- `package_release.ps1`
  - `30`: output directory or packaging input validation failed
  - `31`: release folder preparation failed
  - `32`: backend or script asset copy failed
  - `33`: Unity client copy failed
  - `34`: packaged release layout validation failed

## 7. Release Folder Validation

From the packaged release root:

```powershell
cd .\release
.\scripts\setup_windows.ps1
.\scripts\run_all.ps1
python .\scripts\smoke_backend.py
```

Validation goal:

- prove the packaged backend and scripts work outside the repo layout
- confirm health and smoke flows still pass from the release folder

Current limitation:

- full release-folder validation on a target machine is still gated by `P05` in `tasks/task-people.md`

## 8. Troubleshooting

### Setup script fails with missing Python modules

Symptoms:

- preflight reports missing `fastapi`, `uvicorn`, `httpx`, or similar

Actions:

- run `python -m pip install -r local-backend\requirements.txt`
- confirm the same `python` executable is used by your shell and scripts:

```powershell
python -c "import sys; print(sys.executable)"
```

### Runtime preflight fails on Piper or Whisper path

Symptoms:

- `assistant_piper_command is configured but not found`
- `assistant_piper_model_path does not exist`
- `assistant_whisper_command is configured but not found`
- `assistant_whisper_model_path does not exist`

Actions:

- correct the path in `.env` or current shell env vars
- if you intentionally want degraded mode, remove the broken path override instead of leaving an invalid path configured

### Health reports `partial`

Symptoms:

- `run_all.ps1` warns and prints recovery actions

Actions:

- inspect recovery actions from `/v1/health`
- decide whether degraded speech is acceptable for the current validation goal
- if not acceptable, fix the missing runtime and rerun startup plus smoke

### Health reports `error`

Actions:

- do not continue to client validation
- query the health payload directly and capture `recovery_actions`
- fix runtime or configuration errors, then rerun `.\scripts\run_all.ps1`

### Smoke fails on TTS or assistant streaming with ChatTTS

Context:

- `A12` is still open because ChatTTS degraded-path behavior is not fully hardened yet

Actions:

- retry with `assistant_tts_provider=piper` for baseline validation
- if validating degraded behavior, capture the exact smoke output and `/v1/health` payload for the task log

### Backend never becomes reachable

Actions:

- run the backend manually from `local-backend/`:

```powershell
cd .\local-backend
python run_local.py
```

- if manual startup fails, keep the traceback or log output and fix that first
- if manual startup succeeds but `run_all.ps1` still times out, verify port `8096` is not occupied by another process

Port check:

```powershell
Get-NetTCPConnection -LocalPort 8096 -ErrorAction SilentlyContinue
```

## 9. Evidence To Capture During Validation

When reporting status or handoff, keep:

- setup script exit code
- startup script exit code
- `/v1/health` JSON
- smoke JSON summary
- release package path used for validation
- any warning or error lines from preflight diagnostics

## 10. Operator Notes

- Do not mark runtime hardening work done from docs alone; use test, log, or smoke evidence.
- If a step needs Unity Editor visuals, external credentials, or machine-local runtimes that terminal cannot confirm, record it as not fully verified instead of assuming success.
