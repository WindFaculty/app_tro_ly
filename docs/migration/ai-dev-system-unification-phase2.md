# AI Dev System Unification Phase 2

Updated: 2026-04-07
Status: Current implementation updated so subsystem AI context now lives under `ai-dev-system/context/`

## Purpose

This document records the Phase 2 context migration for the `ai-dev-system` unification plan.

The goal of this phase is to absorb the root `ai/` context into `ai-dev-system/context/` while keeping repo governance at the root.

## What Changed In Phase 2

### Current implementation

- Root `ai/` context files were absorbed into `ai-dev-system/context/`.
- The old `ai-dev-system/prompts/` prompt files were folded into `ai-dev-system/context/prompts/`.
- `ai-dev-system/context/` now contains:
  - `summaries/`
  - `policies/`
  - `prompts/`
  - `prompts/automation/`
  - `memory/`

### Planned work still not done

- `unity-client/` still remains at the repo root.
- Root `scripts/`, `tools/`, `bleder/`, `release/`, and asset-import roots have not been migrated yet.
- Control-plane code has not been moved from legacy `ai-dev-system/app/` and related roots into `control-plane/` yet.

## Source-Of-Truth Shift

| Before Phase 2 | After Phase 2 |
| --- | --- |
| root `ai/CONTEXT_SUMMARY.md` | `ai-dev-system/context/summaries/CONTEXT_SUMMARY.md` |
| root `ai/RULES_SHORT.md` | `ai-dev-system/context/policies/RULES_SHORT.md` |
| root `ai/SYSTEM_PROMPT.md` | `ai-dev-system/context/prompts/SYSTEM_PROMPT.md` |
| root `ai/TASK_TEMPLATE.md` | `ai-dev-system/context/prompts/TASK_TEMPLATE.md` |
| root `ai/MEMORY/` | `ai-dev-system/context/memory/` |
| `ai-dev-system/prompts/*.md` | `ai-dev-system/context/prompts/automation/*.md` |

## Acceptance Check

Phase 2 is satisfied in repo state when:

- the subsystem no longer relies on root `ai/` as its context source of truth
- prompt and policy files are no longer split between root `ai/` and `ai-dev-system/prompts/`
- docs and rules point to `ai-dev-system/context/` as the context layer
- repo governance still remains outside `ai-dev-system/`

## Verification Evidence

- The moved context content was reviewed from both the old `ai/` root and the old `ai-dev-system/prompts/` directory before relocation.
- Post-edit inventory confirmed that `ai-dev-system/context/` now contains the summary, policy, prompt, automation-prompt, and memory files.
- Root docs and task trackers were updated to reference the new context location without claiming unrelated runtime migrations.
