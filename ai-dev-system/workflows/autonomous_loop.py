from __future__ import annotations

import json
from pathlib import Path

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from agents.contracts import TaskDefinition
from agents.debugger_agent import DebugAgent
from memory.lesson_store import LessonStore
from orchestrator.catalog import load_platform_catalog
from orchestrator.engine import WorkflowOrchestrator
from orchestrator.history import RunHistoryStore
from planner.planner_agent import PlannerAgent
from tools.json_logger import JsonLogger

try:
    from mcp_client import UnityMcpClient
except ModuleNotFoundError:  # pragma: no cover - exercised indirectly in tests
    UnityMcpClient = None

try:
    from executor.executor_agent import ExecutorAgent
except ModuleNotFoundError as exc:  # pragma: no cover - depends on optional runtime deps
    _EXECUTOR_IMPORT_ERROR = exc

    class ExecutorAgent:  # type: ignore[no-redef]
        async def execute(self, client, step):
            raise RuntimeError("ExecutorAgent dependencies are not installed.") from _EXECUTOR_IMPORT_ERROR


class AutonomousUnityWorkflow:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.catalog_root = Path(__file__).resolve().parents[1]
        self.logger = JsonLogger(root / "logs" / "run-log.jsonl")
        self.lesson_store = LessonStore(root / "logs" / "lessons.jsonl")
        self.planner = PlannerAgent()
        self.executor = ExecutorAgent()
        self.debugger = DebugAgent()

    def load_task(self, path: Path) -> TaskDefinition:
        payload = json.loads(path.read_text(encoding="utf-8"))
        return TaskDefinition(
            id=payload["id"],
            title=payload["title"],
            prompt=payload["prompt"],
            goal=payload["goal"],
        )

    async def run(self, repo_root: Path, task: TaskDefinition) -> dict:
        orchestrator = WorkflowOrchestrator(
            self.root,
            planner=self.planner,
            executor=self.executor,
            debugger=self.debugger,
            logger=self.logger,
            lesson_store=self.lesson_store,
            run_history=RunHistoryStore(self.root / "logs" / "agent-platform-run-history.jsonl"),
            catalog=load_platform_catalog(self.catalog_root),
            client_factory=UnityMcpClient or _load_unity_mcp_client(),
        )
        summary = await orchestrator.run(repo_root, task, workflow_id="unity-autonomous-loop")
        self.logger.log("workflow_completed", summary=summary)
        return summary


def _load_unity_mcp_client():
    from mcp_client import UnityMcpClient as loaded

    return loaded
