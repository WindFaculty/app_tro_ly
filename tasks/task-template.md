# Task Template

Use this template when adding or splitting AI-executable work in `tasks/task-queue.md`.

## Task Shape

- `ID`
- `Title`
- `Objective`
- `Non-goals`
- `In scope`
- `Out of scope`
- `Files allowed to change`
- `Expected outputs`
- `Constraints`
- `Dependencies or manual gates`
- `Validation steps`
- `Docs updates required`
- `Acceptance criteria`
- `Status`

## Completion Checklist

- Confirm the final change stayed inside the declared scope and allowed files.
- Update `tasks/task-queue.md` if the AI-executable task status, scope, blockers, or done criteria changed.
- Update `tasks/task-people.md` if the task now depends on Unity Editor interaction, target-machine checks, credentials, approvals, or external assets.
- Update current-state docs in the same task when architecture, module boundaries, workflow, or operator expectations changed.
- Add a `tasks/done.md` entry only when the completed work has concrete verification evidence.
- Call out residual risks, manual validation gaps, and anything intentionally left out of scope.

## Completion Report

Include these points in the final closeout:

- `Changed`
- `Out of scope kept untouched`
- `Verification evidence`
- `Docs or trackers updated`
- `Remaining manual gates or risks`
