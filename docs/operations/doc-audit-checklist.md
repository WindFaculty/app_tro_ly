# Doc Audit Checklist

Run this checklist before closing an AI-executable task that changed repo behavior, docs, or governance.

## Checklist

- README still matches the current runtime summary and quick-start flow.
- `docs/index.md` and any relevant folder index pages link to the new or changed doc.
- Current-state docs still match code truth for the touched area.
- Design-target docs are not phrased like shipped behavior.
- The right source-of-truth file still owns each changed claim.
- `tasks/task-queue.md` reflects any status, scope, blocker, or definition-of-done change.
- `tasks/task-people.md` was updated if the task now needs Unity Editor interaction, packaged-client smoke, credentials, approvals, or external assets.
- `tasks/done.md` was updated only if the work is actually complete and has concrete verification evidence.
- `docs/06-decisions.md` and an ADR were updated if the task changed a gated architecture or governance rule.

## Manual Validation Reminder

- Do not mark Unity visual behavior, packaged-client behavior, production avatar behavior, or target-machine runtime checks as verified unless terminal or tracker evidence exists for that exact validation.
