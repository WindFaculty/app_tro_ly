from __future__ import annotations

import json
from dataclasses import asdict
from pathlib import Path

from agents.contracts import Lesson, TaskDefinition
from debugger_agent import DebugAgent
from executor_agent import ExecutorAgent
from lesson_store import LessonStore
from json_logger import JsonLogger
from mcp_client import UnityMcpClient
from planner_agent import PlannerAgent


class AutonomousUnityWorkflow:
    def __init__(self, root: Path) -> None:
        self.root = root
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
        plan = self.planner.build_plan(task)
        self.logger.log("plan_created", task_id=task.id, steps=[asdict(step) for step in plan])

        async with UnityMcpClient(repo_root) as client:
            tools = await client.list_tools()
            resources = await client.list_resources()
            self.logger.log("mcp_connected", tools=tools, resources=resources)

            records = []
            workflow_status = "completed"
            stopped_after_step: str | None = None
            for step in plan:
                self.logger.log("step_started", step=asdict(step))
                record = await self.executor.execute(client, step)

                if step.kind == "verify":
                    console_analysis = self.debugger.summarize_console(record.details.get("console", {}))
                    record.details["console_analysis"] = console_analysis
                    lesson = self.debugger.analyze_console(console_analysis)
                    if lesson is not None:
                        self.lesson_store.append(lesson)

                records.append(record)
                self.logger.log("step_finished", record=asdict(record))
                if record.status != "completed":
                    workflow_status = "failed"
                    stopped_after_step = step.id
                    self.logger.log("workflow_stopped", failed_step_id=step.id, failed_status=record.status)
                    break

            summary = {
                "task_id": task.id,
                "task_title": task.title,
                "workflow_status": workflow_status,
                "stopped_after_step": stopped_after_step,
                "steps": [asdict(record) for record in records],
            }
            self.logger.log("workflow_completed", summary=summary)
            if workflow_status == "completed":
                self.lesson_store.append(
                    Lesson(
                        category="workflow",
                        summary="Autonomous Unity workflow completed an end-to-end pass.",
                        evidence={"task_id": task.id, "step_count": len(records)},
                    )
                )
            else:
                self.lesson_store.append(
                    Lesson(
                        category="workflow",
                        summary="Autonomous Unity workflow stopped after a failed step.",
                        evidence={"task_id": task.id, "failed_step_id": stopped_after_step, "step_count": len(records)},
                    )
                )
            return summary
