# AI Dev System Unification Phase 6

Updated: 2026-04-07
Status: Current implementation updated so workbench and asset-pipeline inventories now live under `ai-dev-system/workbench/` and `ai-dev-system/asset-pipeline/`

## Purpose

This document records the Phase 6 workbench and asset-pipeline pass for the `ai-dev-system` migration plan.

The goal of this phase is to give `workbench/` and `asset-pipeline/` real ownership over current source inventories, naming guidance, and repeatable validation before any large authoring-file move.

## What Changed In Phase 6

### Current implementation

- `ai-dev-system/workbench/` now owns:
  - a current inventory of authoring and import roots
  - ownership notes for Blender, imports, and report artifacts
  - naming guidance for `raw`, `cleaned`, `validated`, and `export-ready` asset states
- `ai-dev-system/asset-pipeline/` now owns:
  - a current catalog of root `tools/` helper scripts by pipeline role
  - a repeatable PowerShell validator for the current Phase 6 source roots and catalog entries

### Planned work still not done

- The actual `.blend`, `.fbx`, `.png`, render, and report files still live in root `bleder/`, root import folders, and root `tools/` artifact directories.
- The executable helper scripts still live in root `tools/`; they were not moved in this phase.
- No `Clothy3D_Studio/` root is present in the current repo snapshot, so that migration slice remains not applicable in current implementation.
- A fully centralized asset conversion pipeline is still planned work; this phase only establishes ownership, cataloging, and validation around current source roots.

## Source-Of-Truth Shift

| Before Phase 6 | After Phase 6 |
| --- | --- |
| Workbench and asset-pipeline ownership was only implied by top-level placeholders plus root folders. | `ai-dev-system/workbench/` and `ai-dev-system/asset-pipeline/` now hold the current inventory, naming rules, and validation entry points for those roots. |
| Root helper scripts and large authoring files had no subsystem-local catalog. | The subsystem now has explicit catalogs and validators that point back to those current roots. |

## Acceptance Check

Phase 6 is satisfied in repo state when:

- `ai-dev-system/workbench/` documents current Blender, import, and artifact roots
- `ai-dev-system/asset-pipeline/` documents current helper-script ownership and provides a repeatable validator
- docs and trackers describe the new ownership without claiming the large authoring files or helper scripts already moved
- the Phase 6 validator can confirm the current source roots and catalog entries still exist

## Verification Evidence

- Static repo inspection confirmed the current roots referenced by the new Phase 6 files:
  - `bleder/`
  - `Meshy_AI_Azure_Sakura_Kimono_0326010047_texture_fbx/`
  - `tools/`
  - `tools/reports/`
  - `tools/renders/`
- `ai-dev-system/asset-pipeline/validate-phase6-structure.ps1` ran successfully from terminal work.
- Phase 6 JSON files were parsed successfully during validation.
- Runtime or editor-side import validation remains manual because no Blender or Unity runtime execution was performed in this phase.
