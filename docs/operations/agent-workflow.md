# Agent Workflow

Updated: 2026-04-05
Status: Current implementation governance

This page defines the required repo workflow for Codex-style agents in this repository.
It complements `AGENTS.md` and the scoped task template in `tasks/task-template.md`.

## Purpose

- Keep agent work inside explicit scope.
- Prevent docs, tracker, and architecture drift.
- Require concrete evidence before a task is described as done.

## Before Editing

1. Read the user request and identify the exact truth sources in code or tracker files.
2. Read the active task entry in `tasks/task-queue.md` when one already exists for the requested area.
3. If the request falls outside active scoped work during the modularization freeze, record it in `tasks/task-queue.md` or `tasks/task-people.md` before changing code or docs.
4. Use `tasks/task-template.md` when adding or splitting AI-executable work so scope, non-goals, allowed files, validation, expected outputs, and docs updates are explicit.
5. Identify any manual gate up front, such as Unity Editor validation, packaged-client smoke, approvals, credentials, or production avatar handoff.

## During Editing

- Change only files that match the confirmed task scope.
- Do not create a new file when an existing file already owns that role, unless the new file is itself part of the approved scope.
- Keep `docs changed with code` in force when module boundaries, feature flow, contracts, asset spec, or workflow rules change.
- Do not present design-target or manual-only behavior as shipped behavior.
- If the task crosses the architecture gate, update the tracker, current-state docs, and `docs/06-decisions.md` in the same task.

## After Editing

1. Verify with tests, logs, script output, repo searches, or other concrete evidence that matches the actual work done.
2. Update `tasks/task-queue.md` if status, scope, blockers, or definition of done changed.
3. Update `tasks/task-people.md` if the task now needs a person, target machine, Unity Editor work, external assets, credentials, or approvals.
4. Update current-state docs and navigation pages in the same task when the change altered repo truth.
5. Add a `tasks/done.md` entry only when the completed slice has concrete evidence.

## Completion Protocol

Every agent closeout should be able to state:

- what files were changed
- what stayed intentionally out of scope
- what evidence was used to verify the change
- what docs and trackers were updated
- what manual gates, risks, or follow-up work remain

## Source Of Truth

- Repo-wide machine-facing rules: `AGENTS.md`
- Scoped task shape and checklist: `tasks/task-template.md`
- Active AI work: `tasks/task-queue.md`
- Manual or off-repo work: `tasks/task-people.md`
- Historical evidence: `tasks/done.md`
