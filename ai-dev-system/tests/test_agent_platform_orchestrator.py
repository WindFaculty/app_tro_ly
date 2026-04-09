from __future__ import annotations

import asyncio
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from agents.contracts import ExecutionRecord, PlanStep, TaskDefinition
from orchestrator.catalog import load_platform_catalog
from orchestrator.engine import WorkflowOrchestrator
from orchestrator.history import RunHistoryStore


class _FakeLogger:
    def __init__(self) -> None:
        self.events: list[tuple[str, dict]] = []

    def log(self, event: str, **payload) -> None:
        self.events.append((event, payload))


class _FakeLessonStore:
    def __init__(self) -> None:
        self.items: list[object] = []

    def append(self, item) -> None:
        self.items.append(item)


class _FakeClient:
    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root

    async def connect(self):
        return self

    async def close(self, exc_type=None, exc=None, tb=None):
        return None

    async def list_tools(self) -> list[str]:
        return ["manage_scene"]

    async def list_resources(self) -> list[str]:
        return ["project_info"]

    async def reconnect(self):
        return self


class _MissingObjectsPlanner:
    def build_plan(self, task: TaskDefinition) -> list[PlanStep]:
        return [
            PlanStep(id="load_scene", title="Load scene", kind="scene", payload={}),
            PlanStep(id="verify_scene", title="Verify scene", kind="verify", payload={}),
        ]


class _MissingObjectsExecutor:
    async def execute(self, client, step: PlanStep) -> ExecutionRecord:
        if step.id == "load_scene":
            return ExecutionRecord(step_id=step.id, status="completed", details={})
        return ExecutionRecord(
            step_id=step.id,
            status="completed",
            details={
                "console": {"structured_content": {"data": []}},
                "expected_objects": {
                    "Player": {
                        "structured_content": {
                            "data": {"instanceIDs": [], "totalCount": 0}
                        }
                    }
                },
                "screenshot": {
                    "structured_content": {
                        "data": {"fullPath": "D:/repo/apps/unity-runtime/Assets/Screenshots/demo.png"}
                    }
                },
            },
        )


class _RetryablePlanner:
    def build_plan(self, task: TaskDefinition) -> list[PlanStep]:
        return [PlanStep(id="load_scene", title="Load scene", kind="scene", payload={})]


class _RetryableExecutor:
    def __init__(self) -> None:
        self.attempts = 0

    async def execute(self, client, step: PlanStep) -> ExecutionRecord:
        self.attempts += 1
        if self.attempts == 1:
            return ExecutionRecord(
                step_id=step.id,
                status="failed",
                details={
                    "exception": {
                        "structured_content": {
                            "message": "RuntimeError: Connection closed by remote host",
                            "data": {"reason": "transport_exception"},
                        }
                    }
                },
            )
        return ExecutionRecord(step_id=step.id, status="completed", details={})


class _FakeDebugger:
    def summarize_console(self, console_payload: dict) -> dict:
        return {"counts": {"app_errors": 0, "app_warnings": 0, "app_logs": 0, "noise_filtered": 0}}

    def analyze_console(self, analysis: dict):
        return None


class WorkflowOrchestratorTests(unittest.TestCase):
    def test_missing_objects_block_done_status_and_record_lifecycle(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog = load_platform_catalog(ROOT)
            logger = _FakeLogger()
            lesson_store = _FakeLessonStore()
            orchestrator = WorkflowOrchestrator(
                root,
                planner=_MissingObjectsPlanner(),
                executor=_MissingObjectsExecutor(),
                debugger=_FakeDebugger(),
                logger=logger,
                lesson_store=lesson_store,
                run_history=RunHistoryStore(root / "logs" / "history.jsonl"),
                catalog=catalog,
                client_factory=_FakeClient,
            )

            summary = asyncio.run(
                orchestrator.run(
                    root,
                    TaskDefinition(id="demo", title="Demo", prompt="Prompt", goal={"id": "scene_smoke_check"}),
                )
            )

        self.assertEqual(summary["workflow_status"], "failed")
        self.assertIn("missing-objects", summary["verification"]["blocked_by"])
        lifecycle_statuses = [item["status"] for item in summary["lifecycle"]]
        self.assertEqual(
            lifecycle_statuses,
            ["queued", "planning", "ready", "executing", "reviewing", "verifying", "failed"],
        )

    def test_retryable_failure_replans_once_and_recovers(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog = load_platform_catalog(ROOT)
            logger = _FakeLogger()
            lesson_store = _FakeLessonStore()
            executor = _RetryableExecutor()
            orchestrator = WorkflowOrchestrator(
                root,
                planner=_RetryablePlanner(),
                executor=executor,
                debugger=_FakeDebugger(),
                logger=logger,
                lesson_store=lesson_store,
                run_history=RunHistoryStore(root / "logs" / "history.jsonl"),
                catalog=catalog,
                client_factory=_FakeClient,
            )

            summary = asyncio.run(
                orchestrator.run(
                    root,
                    TaskDefinition(id="demo", title="Demo", prompt="Prompt", goal={"id": "scene_smoke_check"}),
                )
            )

        self.assertEqual(summary["workflow_status"], "completed")
        self.assertEqual(summary["replan_count"], 1)
        lifecycle_statuses = [item["status"] for item in summary["lifecycle"]]
        self.assertIn("replanning", lifecycle_statuses)
        self.assertEqual(executor.attempts, 2)


if __name__ == "__main__":
    unittest.main()
