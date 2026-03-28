# AI Unity Dev System

Local AI-driven Unity workflow scaffold built around MCP for Unity, a Python execution loop, and role-based planner/executor/debugger agents.

## What This System Does

- Verifies a live MCP connection to a running Unity Editor
- Plans a bounded Unity task into structured execution steps
- Executes Unity changes through MCP tool calls
- Reads Unity console output for verification and debugging context
- Saves structured run logs and lessons learned
- Ships a reusable demo workflow for a basic 3D scene

## Folder Layout

- `agents/`: shared contracts and agent-facing types
- `planner/`: plan generation
- `executor/`: Unity execution logic
- `memory/`: lesson storage and lightweight memory
- `tools/`: JSON logging helpers
- `workflows/`: orchestration loop
- `logs/`: run artifacts and lessons
- `unity-interface/`: MCP adapter and Unity-specific wrappers
- `prompts/`: prompt templates for future LLM-backed planners/debuggers
- `tasks/`: structured task definitions

## Prerequisites

- Unity Editor is already open on `unity-client/`
- `MCP for Unity` package is installed in the project
- Python 3.11+ is available
- `uvx` can run `mcp-for-unity`

## Run

```powershell
ai-dev-system\.venv\Scripts\python.exe ai-dev-system\verify_connection.py
ai-dev-system\.venv\Scripts\python.exe ai-dev-system\run_demo.py
ai-dev-system\.venv\Scripts\python.exe -m unittest discover ai-dev-system\tests
```

`run_demo.py` also accepts:

```powershell
ai-dev-system\.venv\Scripts\python.exe ai-dev-system\run_demo.py --task ai-dev-system\tasks\demo_simple_3d_game.json --summary-out ai-dev-system\logs\last-summary.json --report-out ai-dev-system\logs\last-report.json
```

The workflow now writes both:

- `logs/last-summary.json`: full raw step-by-step evidence
- `logs/last-report.json`: concise run status, failed steps, console counts, and screenshot evidence

Example smoke-check task:

```powershell
ai-dev-system\.venv\Scripts\python.exe ai-dev-system\run_demo.py --task ai-dev-system\tasks\demo_scene_smoke_check.json
```

## Notes

- This scaffold is intentionally deterministic for the first pass.
- Prompt templates and agent roles are included so an external LLM backend can be plugged in later.
- The demo workflow uses MCP tools directly and logs every major step.
