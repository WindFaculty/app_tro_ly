# Documentation Index

## Core Docs

- [roadmap.md](roadmap.md) - ENTRY POINT for repo structure, runtime flow, module ownership, and reading order
- [architecture/non-backend-integration.md](architecture/non-backend-integration.md) - current non-backend integration map, ownership split, and source-of-truth guide
- [architecture/blender-mcp-integration.md](architecture/blender-mcp-integration.md) - optional Blender MCP control-plane lane, policy defaults, and Codex local-config guidance
- [architecture/unity-automation-architecture.md](architecture/unity-automation-architecture.md) - shared Unity integration layer, backend policy, and current source-of-truth boundary
- [architecture/unity-integration-gap-analysis.md](architecture/unity-integration-gap-analysis.md) - existing state, target state, gaps, risks, and phased Unity integration strategy
- [architecture/unity-open-source-reference-analysis.md](architecture/unity-open-source-reference-analysis.md) - repo-native analysis of Unity CLI Loop and Unity-Skills as reference inputs
- [02-architecture.md](02-architecture.md) - current assistant runtime architecture
- [03-api.md](03-api.md) - current backend contract
- [04-ui.md](04-ui.md) - current Unity UI implementation and planned-direction boundary
- [05-test-plan.md](05-test-plan.md) - current validation strategy
- [09-runbook.md](09-runbook.md) - setup, startup, smoke, packaging, and troubleshooting
- [migration/ai-dev-system-unification.md](migration/ai-dev-system-unification.md) - overview of the non-backend unification phases, current landed slices, and remaining work
- [migration/phase0.md](migration/phase0.md) - modularization baseline and gap audit
- [migration/ai-dev-system-unification-phase0.md](migration/ai-dev-system-unification-phase0.md) - root-level non-backend unification baseline for the proposed `ai-dev-system/` migration
- [migration/ai-dev-system-unification-phase1.md](migration/ai-dev-system-unification-phase1.md) - Phase 1 architecture scaffolding and ownership map for `ai-dev-system/`
- [migration/ai-dev-system-unification-phase2.md](migration/ai-dev-system-unification-phase2.md) - Phase 2 context absorption into `ai-dev-system/context/`
- [migration/ai-dev-system-unification-phase3.md](migration/ai-dev-system-unification-phase3.md) - historical Phase 3 client absorption of the Unity project into `ai-dev-system/clients/`
- [migration/ai-dev-system-unification-phase4.md](migration/ai-dev-system-unification-phase4.md) - Phase 4 control-plane unification that moves automation runtime truth under `ai-dev-system/control-plane/`
- [migration/ai-dev-system-unification-phase5.md](migration/ai-dev-system-unification-phase5.md) - Phase 5 domain pass that moves shared avatar, customization, and room contract truth under `ai-dev-system/domain/`
- [migration/ai-dev-system-unification-phase6.md](migration/ai-dev-system-unification-phase6.md) - Phase 6 workbench and asset-pipeline pass that adds current inventories, naming guidance, and structure validation
- [migration/ai-dev-system-unification-phase7.md](migration/ai-dev-system-unification-phase7.md) - Phase 7 scripts and tests standardization under `ai-dev-system/`
- [migration/ai-dev-system-unification-phase8.md](migration/ai-dev-system-unification-phase8.md) - Phase 8 docs and task-governance rewrite around the landed `ai-dev-system/` architecture
- [migration/ai-dev-system-unification-phase9.md](migration/ai-dev-system-unification-phase9.md) - Phase 9 architecture lock, shim cleanup, and stale-path drift validation

## Notes

- `roadmap.md` is the main entry point for new developers and AI agents.
- Code remains the final source of truth when docs and implementation diverge.
- older Unity UI shell docs are historical context, not implementation truth.
- Use `migration/ai-dev-system-unification.md` plus the per-phase docs to tell which `ai-dev-system/` slices are current implementation versus still planned work.
