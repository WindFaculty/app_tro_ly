# Agent Platform Governance

Current implementation: the canonical source of truth for the repo's agent platform now lives in `ai-dev-system/control-plane/catalog/platform.json`.

Rules:
- Treat the catalog plus the policy and prompt files it references as the canonical definition for harness-facing agent behavior.
- Keep `control-plane/app/` as the current runtime truth for live Windows and Unity automation.
- Keep `workflows/autonomous_loop.py` as the current bounded Unity workflow entry point, but route behavior through the shared orchestrator.
- Generate `.codex/`, `.agents/skills/`, and `.agent/` surfaces from the canonical catalog instead of maintaining disconnected copies.
- Update docs and task trackers whenever the catalog, orchestrator lifecycle, or exported harness surfaces change.
