from __future__ import annotations

import asyncio
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))
for child in ("agents", "planner", "executor", "memory", "tools", "workflows", "unity-interface"):
    path = ROOT / child
    if str(path) not in sys.path:
        sys.path.append(str(path))

from autonomous_loop import AutonomousUnityWorkflow
from contracts import ExecutionRecord, PlanStep, TaskDefinition


class _FakeClient:
    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root

    async def __aenter__(self) -> "_FakeClient":
        return self

    async def __aexit__(self, exc_type, exc, tb) -> None:
        return None

    async def list_tools(self) -> list[str]:
        return ["manage_scene"]

    async def list_resources(self) -> list[str]:
        return ["project_info"]


class _FakePlanner:
    def build_plan(self, task: TaskDefinition) -> list[PlanStep]:
        return [
            PlanStep(id="load_scene", title="Load scene", kind="scene", payload={}),
            PlanStep(id="verify_scene", title="Verify scene", kind="verify", payload={}),
        ]


class _FakeExecutor:
    async def execute(self, client, step: PlanStep) -> ExecutionRecord:
        if step.id == "load_scene":
            return ExecutionRecord(step_id=step.id, status="failed", details={"reason": {"content": [{"text": "load failed"}]}})
        return ExecutionRecord(step_id=step.id, status="completed", details={})


class _FakeDebugAgent:
    def summarize_console(self, console_payload: dict) -> dict:
        return {"counts": {}}

    def analyze_console(self, analysis: dict):
        return None


class AutonomousLoopTests(unittest.TestCase):
    def test_workflow_stops_after_first_failed_step(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workflow = AutonomousUnityWorkflow(Path(temp_dir))
            workflow.planner = _FakePlanner()
            workflow.executor = _FakeExecutor()
            workflow.debugger = _FakeDebugAgent()

            task = TaskDefinition(id="demo", title="Demo", prompt="Prompt", goal={"id": "fake"})

            with patch("autonomous_loop.UnityMcpClient", _FakeClient):
                summary = asyncio.run(workflow.run(Path(temp_dir), task))

        self.assertEqual(summary["workflow_status"], "failed")
        self.assertEqual(summary["stopped_after_step"], "load_scene")
        self.assertEqual(len(summary["steps"]), 1)


if __name__ == "__main__":
    unittest.main()
