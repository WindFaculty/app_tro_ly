# Blender MCP Integration

Updated: 2026-04-08
Status: Current implementation plus manual validation gate

## Purpose

This document defines the repo-safe integration boundary for `blender-mcp` inside `app_tro_ly`.

Current implementation adds an optional interactive Blender MCP lane for:

- attach and inspect
- scene and object queries
- viewport screenshots
- tightly gated code execution

It does not replace the current deterministic Blender CLI wrapper flow under root `tools/` plus `ai-dev-system/workflows/mesh_ai_refine.py`.

## Current Implementation

- `ai-dev-system/control-plane/app/blender/` now owns the Blender MCP runtime, capability matrix, preflight checks, assertions, and upstream pin metadata.
- `ai-dev-system/control-plane/app/profiles/blender_editor_profile.py` now exposes a `blender-editor` GUI-agent profile for `list-capabilities`, `inspect`, and `run --task-file`.
- `ai-dev-system/control-plane/app/agent/strategies/mcp_strategies.py` now includes a dedicated `blender_mcp_tool` strategy so Blender MCP calls do not reuse the Unity-only `unity_runtime` key.
- `ai-dev-system/verify_blender_connection.py` plus `ai-dev-system/scripts/validate/validate-blender-mcp.ps1` now provide the repo-side Blender MCP smoke path.
- `ai-dev-system/scripts/run/run-blender-automation.ps1` now provides a convenience entry point for the new profile.

## Security And Policy Defaults

- Repo-owned defaults force `BLENDER_HOST=127.0.0.1`, `BLENDER_PORT=9876`, and `DISABLE_TELEMETRY=true`.
- Current implementation only treats `get_scene_info`, `get_object_info`, and `get_viewport_screenshot` as required safe tools.
- `blender.execute_code` stays blocked by policy unless both of the following are true:
  - `APP_TRO_LY_BLENDER_ALLOW_EXECUTE_CODE=true`
  - the task spec sets `confirm_destructive: true`
- Current implementation does not surface remote-host mode, asset-download tools, or generated-asset tools.

## Codex Session Support

Current implementation does not modify generated `.codex/config.toml`.

To expose the same Blender MCP server to a local Codex session, add a local MCP-server entry outside repo-generated surfaces with these defaults:

```toml
[mcp_servers.blender]
command = "uvx"
args = ["blender-mcp"]

[mcp_servers.blender.env]
BLENDER_HOST = "127.0.0.1"
BLENDER_PORT = "9876"
DISABLE_TELEMETRY = "true"
```

This local config is a machine-level setup step, not a repo-generated surface.

## Manual Validation Required

- Install the upstream Blender addon into the active Blender session
- Connect the addon to the MCP server
- Confirm telemetry stays disabled
- Run live attach plus scene inspect plus viewport screenshot on the target machine
- If `execute_code` is ever enabled, verify it only on a saved file and capture evidence
