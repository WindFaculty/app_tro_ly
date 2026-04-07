# AI Dev System Unification Phase 3

Updated: 2026-04-07
Status: Current implementation updated so the Unity client now lives under `ai-dev-system/clients/unity-client/`

## Purpose

This document records the Phase 3 client-absorption pass for the `ai-dev-system` migration plan.

Phase 4 had already unified the automation runtime first. This Phase 3 pass then moves the Unity project under `clients/` so the non-backend system no longer depends on a separate root-level `unity-client/` folder.

## What Changed In Phase 3

### Current implementation

- The Unity project was moved from repo root `unity-client/` to `ai-dev-system/clients/unity-client/`.
- Root-level current-state docs and task trackers were updated to treat `ai-dev-system/clients/unity-client/` as the client source of truth.
- Script wording and Unity-project path resolution were updated so repo entry points now resolve the absorbed client location.
- Control-plane Unity helpers were updated so project-root resolution no longer assumes a root-level `unity-client/` folder.

### Planned work still not done

- `local-backend/` still remains outside `ai-dev-system/`.
- Unity Editor and packaged-client smoke still require manual validation after the path move.
- Domain extraction for avatar, customization, room, and asset-pipeline ownership has not happened yet.

## Source-Of-Truth Shift

| Before Phase 3 | After Phase 3 |
| --- | --- |
| `unity-client/` | `ai-dev-system/clients/unity-client/` |
| `unity-client/Assets/Resources/UI/` | `ai-dev-system/clients/unity-client/Assets/Resources/UI/` |
| `unity-client/Assets/Scripts/` | `ai-dev-system/clients/unity-client/Assets/Scripts/` |
| `unity-client/Assets/AvatarSystem/` | `ai-dev-system/clients/unity-client/Assets/AvatarSystem/` |
| `unity-client/Assets/Tests/` | `ai-dev-system/clients/unity-client/Assets/Tests/` |

## Acceptance Check

Phase 3 is satisfied in repo state when:

- the Unity project no longer exists as a separate repo root
- `ai-dev-system/clients/unity-client/` is the only repo path used for current Unity implementation truth
- script entry points and automation helpers resolve the moved client path
- repo docs and trackers describe the absorbed client path as current implementation

## Verification Evidence

- Post-move inventory confirmed root `unity-client/` no longer exists and `ai-dev-system/clients/unity-client/` now holds `Assets/`, `Packages/`, and `ProjectSettings/`.
- Control-plane Unity path resolution was updated to locate the absorbed client root.
- Current-state docs and task trackers were refreshed to point at `ai-dev-system/clients/unity-client/`.
- Unity Editor and packaged-client smoke were not rerun from this shell, so final runtime validation remains manual under `P02`.
