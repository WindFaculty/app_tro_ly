# Verification Gates

Current implementation: completion for agent-platform changes requires code-backed evidence plus exported-surface drift checks.

Rules:
- A workflow is not complete until planning, execution, review, and verification states are recorded.
- Failed steps, missing required artifacts, console errors, or missing expected objects block a `done` status.
- Generated Codex and Antigravity surfaces must match the canonical catalog before the work is considered verified.
- Add or update tests for critical lifecycle, review, verification, or export behavior whenever those paths change.
