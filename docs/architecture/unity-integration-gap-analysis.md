# Unity Integration Gap Analysis

Updated: 2026-04-08
Status: Current implementation plus phased integration strategy

## 1. Existing State

- `ai-dev-system/control-plane/app/unity/` already provides a hybrid Unity lane with capability routing, GUI fallback, preflight checks, and verification.
- `ai-dev-system/workflows/autonomous_loop.py` plus `ai-dev-system/control-plane/mcp_client.py` already provide an MCP-driven Unity workflow loop.
- `ai-dev-system/scripts/run/run-unity-automation.ps1` previously pointed only at the older workflow demo path.

## 2. Target State

- A repo-native Unity integration core owns environment probing, capability cataloging, backend selection, and transport clients.
- Unity CLI Loop becomes the optional execution layer for compile or test or log oriented work.
- Unity-Skills concepts inform a curated capability model for scene or object or script level work.
- GUI-agent and autonomous-loop consumers share one integration core instead of duplicating runtime assumptions.

## 3. Gap Analysis

- Missing before this change:
  - one shared Unity integration package
  - one shared environment probe for editor path, project path, and optional upstream tooling
  - one curated capability catalog spanning CLI loop and Unity-Skills-style concepts
  - one repo-owned validation surface for Unity integration drift
- Risk before this change:
  - hardcoded Unity executable or version assumptions
  - split MCP client implementations
  - Unity execution and capability policy spread across multiple partial roots

## 4. Integration Strategy

- Add `ai-dev-system/control-plane/unity_integration/` as the new Unity automation source of truth.
- Keep `app/unity/` and `mcp_client.py` as compatibility surfaces over the shared core.
- Use Unity CLI Loop as optional external execution dependency, not vendored code.
- Keep the capability surface curated and repo-specific instead of mirroring upstream breadth.
- Extend run and validate scripts so the new lane is visible without replacing older workflow entry points abruptly.

## 5. Risks

- Live Unity CLI Loop install and target-machine validation remain manual gates.
- Some CLI command mappings still depend on upstream package availability and machine setup.
- Existing autonomous-loop planning still uses legacy step shapes even though transport now shares the new core.

## 6. Phased Implementation Plan

1. Land docs and reference analysis.
2. Add `unity_integration/` contracts, environment probe, and capability catalog.
3. Consolidate MCP client and runtime into the shared backend.
4. Add the optional Unity CLI Loop adapter and validation surface.
5. Repoint Unity profile and connection helpers to the shared integration service.
6. Update task tracking, README surfaces, and governance docs.
