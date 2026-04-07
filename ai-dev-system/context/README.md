# Context

Current implementation: this directory now owns the absorbed non-backend AI context for the subsystem.

## Current Source Of Truth

- `summaries/CONTEXT_SUMMARY.md`
- `policies/RULES_SHORT.md`
- `prompts/SYSTEM_PROMPT.md`
- `prompts/TASK_TEMPLATE.md`
- `prompts/automation/`
- `memory/`

## Boundary

- This directory is subsystem-local context, not repo governance.
- Root `docs/`, `tasks/`, `AGENTS.md`, and `lessons.md` remain outside `ai-dev-system/`.
- Backend or Unity runtime behavior still comes from code, not from these context files.

## Historical Note

- Root `ai/` has been absorbed in Phase 2 of the `ai-dev-system` unification plan.
- The older `ai-dev-system/prompts/` folder has also been folded into `context/prompts/` so the subsystem no longer keeps two prompt roots in parallel.
