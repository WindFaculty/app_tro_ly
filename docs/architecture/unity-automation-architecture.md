# Unity Automation Architecture

Updated: 2026-04-09
Status: Current implementation

## Purpose

This document defines the current repo-native Unity automation boundary after the Unity integration refactor.

## Current Source Of Truth

- `ai-dev-system/control-plane/unity_integration/`
  - shared contracts
  - environment probing
  - curated capability catalog
  - CLI Loop backend
  - shared Unity MCP backend
  - GUI fallback backend
  - backend-selection service
- `ai-dev-system/control-plane/app/unity/`
  - compatibility-facing capability compilation
  - GUI macros
  - preflight
  - assertions
  - task alias parsing
- `ai-dev-system/control-plane/mcp_client.py`
  - compatibility export of the shared Unity MCP client

## Layer Map

### Orchestration layer

- `ai-dev-system/workflows/autonomous_loop.py`
- `ai-dev-system/control-plane/orchestrator/`
- `ai-dev-system/control-plane/planner/`
- `ai-dev-system/control-plane/executor/`

Role:

- decides what to do
- records lifecycle, review, verification, and lessons

### Unity integration layer

- `ai-dev-system/control-plane/unity_integration/`

Role:

- decides how a Unity capability should execute
- probes environment and optional dependencies
- normalizes backend output into repo-owned result shapes

### Unity profile and GUI layer

- `ai-dev-system/control-plane/app/profiles/unity_editor_profile.py`
- `ai-dev-system/control-plane/app/unity/`

Role:

- mediates GUI-agent runs
- preserves macro-driven and fallback-driven execution
- exposes live capability matrices to the profile CLI

## Current Backend Policy

- `cli_loop`
  - optional
  - preferred for compile or test or logs or launch oriented loops
  - current implementation: Windows command resolution now prefers env override, `uloop.cmd`, `uloop.exe`, `uloop.ps1` via PowerShell, then `npx uloop-cli`
  - current implementation: CLI artifact capture now writes command metadata plus `stdout` or `stderr` files and returns structured failure payloads for spawn errors
  - current implementation: CLI command mapping now follows the current upstream syntax for `run-tests`, `find-game-objects`, `execute-menu-item`, `get-hierarchy`, and `get-logs`
- `mcp`
  - shared transport for existing autonomous-loop and hybrid profile paths
- `gui`
  - fallback and inspection-oriented lane for editor layout or window level work

## Boundary Rules

- Unity runtime code under `apps/unity-runtime/` does not call back into the control-plane integration layer.
- Orchestration code should call contracts or services, not hardcode transport details.
- Upstream open-source repos remain references or external dependencies, not code roots inside this repo.

## Current Validation State

- Current implementation: `apps/unity-runtime/Packages/manifest.json` now references `https://github.com/hatayama/unity-cli-loop.git?path=/Packages/src`.
- Current implementation: `python ai-dev-system/verify_unity_integration.py` now proves the resolved CLI invocation and reports detected Unity-side blockers.
- Current implementation: `python ai-dev-system/scripts/validate/validate_unity_cli_loop_e2e.py` and `powershell -NoProfile -ExecutionPolicy Bypass -File ai-dev-system/scripts/run/run-unity-automation.ps1 -Lane integration cli-e2e` now provide a repo-owned end-to-end smoke path locked to `backend_preference="cli_loop"` with `allow_fallback=False`.
- Current implementation: the local Windows machine still fails live CLI Loop startup because Unity Editor logs show duplicate assembly conflicts between `apps/unity-runtime/Assets/Plugins/Roslyn/*.dll` and `Packages/io.github.hatayama.uloopmcp/Plugins/CodeAnalysis/*`, followed by invalid script assemblies during `ReloadAssembly`.

## Manual Validation Required

- Clearing the local Unity-side assembly conflict so `Window/Unity CLI Loop/*` menu items register and the server can start
- Verifying live compile or test or log runs through the CLI lane after the Unity-side conflict is cleared
- Verifying representative scene or object operations against a real Unity Editor session
