# Unity Autonomous Loop

Plan, execute, review, and verify bounded Unity MCP workflows with explicit lifecycle state.

- Entrypoint: `workflows/autonomous_loop.py`
- Planner: `planner`
- Executor: `executor`
- Reviewer: `code-reviewer`
- Verifier: `debugger`
- Supporting agents: architect, security-reviewer, build-error-resolver, loop-operator
- Skills: search-first, verification-loop, docs-sync, continuous-learning
- Rule packs: agent-platform-governance, verification-gates
- Tool policy: `control-plane-default`
- Eval: `unity-scene-verification`
- Phases: queued, planning, ready, executing, reviewing, verifying, done
- Side states: blocked, failed, replanning
- Max replans: 1
- Supported goals: basic_3d_game, scene_smoke_check
