from __future__ import annotations

from dataclasses import asdict
from pathlib import Path
from typing import Any, Callable

from agents.contracts import ExecutionRecord, Lesson, PlanStep, TaskDefinition
from orchestrator.catalog import PlatformCatalog, load_platform_catalog
from orchestrator.history import RunHistoryStore
from orchestrator.models import (
    LifecycleTransition,
    ReviewFinding,
    ReviewSummary,
    TaskLifecycleStatus,
    VerificationSummary,
)
from tools.workflow_report import build_workflow_report


class WorkflowOrchestrator:
    def __init__(
        self,
        root: Path,
        *,
        planner,
        executor,
        debugger,
        logger,
        lesson_store,
        run_history: RunHistoryStore | None = None,
        catalog: PlatformCatalog | None = None,
        client_factory: Callable[[Path], Any] | None = None,
    ) -> None:
        self.root = root
        self.planner = planner
        self.executor = executor
        self.debugger = debugger
        self.logger = logger
        self.lesson_store = lesson_store
        self.run_history = run_history or RunHistoryStore(root / "logs" / "agent-platform-run-history.jsonl")
        self.catalog = catalog or load_platform_catalog(root)
        self.client_factory = client_factory or _load_unity_mcp_client()

    async def run(self, repo_root: Path, task: TaskDefinition, *, workflow_id: str = "unity-autonomous-loop") -> dict[str, Any]:
        workflow = self.catalog.require_workflow(workflow_id)
        lifecycle: list[LifecycleTransition] = []
        self._transition(lifecycle, TaskLifecycleStatus.QUEUED, details={"task_id": task.id, "workflow_id": workflow.id})

        replan_count = 0
        final_summary: dict[str, Any] | None = None

        for attempt_index in range(workflow.max_replans + 1):
            if attempt_index > 0:
                self._transition(
                    lifecycle,
                    TaskLifecycleStatus.REPLANNING,
                    reason="retryable failure",
                    details={"attempt_index": attempt_index},
                )

            attempt_summary = await self._run_attempt(
                repo_root,
                task,
                workflow=workflow,
                lifecycle=lifecycle,
                attempt_index=attempt_index,
            )
            review = self._build_review_summary(attempt_summary["report"])
            self._transition(
                lifecycle,
                TaskLifecycleStatus.REVIEWING,
                details={"finding_count": len(review.findings), "status": review.status},
            )

            verification = self._build_verification_summary(workflow.eval, attempt_summary["report"])
            self._transition(
                lifecycle,
                TaskLifecycleStatus.VERIFYING,
                details={"passed": verification.passed, "blocked_by": verification.blocked_by},
            )

            workflow_status = attempt_summary["workflow_status"]
            if workflow_status == "completed" and not verification.passed:
                workflow_status = "failed"
                if attempt_summary["stopped_after_step"] is None:
                    attempt_summary["stopped_after_step"] = verification.blocking_step_id

            summary = {
                "task_id": task.id,
                "task_title": task.title,
                "workflow_id": workflow.id,
                "workflow_status": workflow_status,
                "stopped_after_step": attempt_summary["stopped_after_step"],
                "replan_count": replan_count,
                "plan": attempt_summary["plan"],
                "steps": attempt_summary["steps"],
                "connection": attempt_summary["connection"],
                "review": review.to_dict(),
                "verification": verification.to_dict(),
                "report": attempt_summary["report"],
                "lifecycle": [transition.to_dict() for transition in lifecycle],
                "lessons": attempt_summary["lessons"],
            }

            should_replan = workflow_status == "failed" and verification.retryable and attempt_index < workflow.max_replans
            if should_replan:
                replan_count += 1
                continue

            final_status = TaskLifecycleStatus.DONE if workflow_status == "completed" else TaskLifecycleStatus.FAILED
            self._transition(lifecycle, final_status, reason=workflow_status, details={"replan_count": replan_count})
            summary["replan_count"] = replan_count
            summary["lifecycle"] = [transition.to_dict() for transition in lifecycle]
            self.run_history.append(summary)
            self._append_workflow_lesson(summary)
            final_summary = summary
            break

        if final_summary is None:
            raise RuntimeError("Workflow did not produce a final summary.")
        return final_summary

    async def _run_attempt(
        self,
        repo_root: Path,
        task: TaskDefinition,
        *,
        workflow,
        lifecycle: list[LifecycleTransition],
        attempt_index: int,
    ) -> dict[str, Any]:
        self._transition(lifecycle, TaskLifecycleStatus.PLANNING, details={"attempt_index": attempt_index})
        plan = self.planner.build_plan(task)
        self.logger.log("plan_created", task_id=task.id, workflow_id=workflow.id, steps=[asdict(step) for step in plan])
        self._transition(lifecycle, TaskLifecycleStatus.READY, details={"step_count": len(plan)})

        client = self.client_factory(repo_root)
        await client.connect()
        self._transition(lifecycle, TaskLifecycleStatus.EXECUTING, details={"step_count": len(plan)})

        lessons: list[dict[str, Any]] = []
        records: list[ExecutionRecord] = []
        connection: dict[str, Any] = {"tools": [], "resources": []}
        workflow_status = "completed"
        stopped_after_step: str | None = None
        try:
            tools, resources = await self._fetch_connection_metadata(client)
            connection = {"tools": tools, "resources": resources}
            self.logger.log("mcp_connected", tools=tools, resources=resources, attempt_index=attempt_index)

            for step in plan:
                self.logger.log("step_started", step=asdict(step), attempt_index=attempt_index)
                record, client = await self._execute_step_with_recovery(client, step)

                if step.kind == "verify":
                    console_analysis = self.debugger.summarize_console(record.details.get("console", {}))
                    record.details["console_analysis"] = console_analysis
                    lesson = self.debugger.analyze_console(console_analysis)
                    if lesson is not None:
                        self.lesson_store.append(lesson)
                        lessons.append(asdict(lesson))

                records.append(record)
                self.logger.log("step_finished", record=asdict(record), attempt_index=attempt_index)
                if record.status != "completed":
                    workflow_status = "failed"
                    stopped_after_step = step.id
                    self.logger.log("workflow_stopped", failed_step_id=step.id, failed_status=record.status)
                    break
        finally:
            await client.close()

        preliminary = {
            "task_id": task.id,
            "task_title": task.title,
            "workflow_status": workflow_status,
            "stopped_after_step": stopped_after_step,
            "steps": [asdict(record) for record in records],
        }
        report = build_workflow_report(preliminary)

        return {
            "workflow_status": workflow_status,
            "stopped_after_step": stopped_after_step,
            "plan": [asdict(step) for step in plan],
            "steps": [asdict(record) for record in records],
            "connection": connection,
            "report": report,
            "lessons": lessons,
        }

    async def _fetch_connection_metadata(self, client: Any) -> tuple[list[str], list[str]]:
        try:
            return await client.list_tools(), await client.list_resources()
        except Exception as exc:
            if not self._is_retryable_transport_exception(exc):
                raise

            self.logger.log("mcp_reconnect_requested", phase="metadata", error=self._stringify_exception(exc))
            await client.reconnect()
            tools = await client.list_tools()
            resources = await client.list_resources()
            self.logger.log("mcp_reconnected", phase="metadata", tools=tools, resources=resources)
            return tools, resources

    async def _execute_step_with_recovery(self, client: Any, step: PlanStep) -> tuple[ExecutionRecord, Any]:
        try:
            return await self.executor.execute(client, step), client
        except Exception as exc:
            if not self._is_retryable_transport_exception(exc):
                return self._build_failed_record(step.id, exc, reason="executor_exception", retryable=False), client

            self.logger.log("mcp_reconnect_requested", phase="step", step_id=step.id, error=self._stringify_exception(exc))
            try:
                await client.reconnect()
                self.logger.log("mcp_reconnected", phase="step", step_id=step.id)
                return await self.executor.execute(client, step), client
            except Exception as retry_exc:
                reason = "transport_exception" if self._is_retryable_transport_exception(retry_exc) else "executor_exception"
                retryable = reason == "transport_exception"
                return self._build_failed_record(step.id, retry_exc, reason=reason, retryable=retryable), client

    @staticmethod
    def _build_review_summary(report: dict[str, Any]) -> ReviewSummary:
        findings: list[ReviewFinding] = []
        for failed_step in report.get("failed_steps") or []:
            findings.append(
                ReviewFinding(
                    category=str(failed_step.get("category") or "execution_failure"),
                    severity=_severity_for_failure(str(failed_step.get("category") or "")),
                    summary="; ".join(failed_step.get("messages") or ["Step failed"]),
                    step_id=str(failed_step.get("step_id") or "") or None,
                    evidence={
                        "retryable": bool(failed_step.get("retryable")),
                        "reason_codes": list(failed_step.get("reason_codes") or []),
                    },
                )
            )

        for verification in report.get("verification_reports") or []:
            status = str(verification.get("status") or "")
            if status in {"clean", "noise-only", "logs-only"}:
                continue
            findings.append(
                ReviewFinding(
                    category=status,
                    severity=_severity_for_verification(status),
                    summary=f"Verification reported {status}.",
                    step_id=str(verification.get("step_id") or "") or None,
                    evidence={
                        "counts": verification.get("counts") or {},
                        "missing_objects": verification.get("missing_objects") or [],
                    },
                )
            )

        notes = ["No blocking review issues were found."] if not findings else ["Review identified issues that require attention."]
        return ReviewSummary(status="clean" if not findings else "issues-found", findings=findings, notes=notes)

    def _build_verification_summary(self, eval_id: str, report: dict[str, Any]) -> VerificationSummary:
        eval_spec = self.catalog.require_eval(eval_id)
        blocked_by: list[str] = []
        retryable = False
        blocking_step_id: str | None = None

        for failed_step in report.get("failed_steps") or []:
            category = str(failed_step.get("category") or "")
            if category not in eval_spec.blocking_failure_categories:
                continue
            blocked_by.append(category)
            retryable = retryable or bool(failed_step.get("retryable"))
            blocking_step_id = blocking_step_id or str(failed_step.get("step_id") or "") or None

        for verification in report.get("verification_reports") or []:
            status = str(verification.get("status") or "")
            if status not in eval_spec.blocking_verification_statuses:
                continue
            blocked_by.append(status)
            if blocking_step_id is None:
                blocking_step_id = str(verification.get("step_id") or "") or None

        return VerificationSummary(
            passed=not blocked_by,
            blocked_by=blocked_by,
            retryable=retryable,
            blocking_step_id=blocking_step_id,
            required_artifacts=list(eval_spec.required_artifacts),
        )

    def _append_workflow_lesson(self, summary: dict[str, Any]) -> None:
        lesson = Lesson(
            category="workflow",
            summary=(
                "Canonical workflow completed with passing verification."
                if summary.get("workflow_status") == "completed"
                else "Canonical workflow stopped with review or verification issues."
            ),
            evidence={
                "task_id": summary.get("task_id"),
                "workflow_id": summary.get("workflow_id"),
                "replan_count": summary.get("replan_count"),
                "blocked_by": (summary.get("verification") or {}).get("blocked_by") or [],
            },
        )
        self.lesson_store.append(lesson)

    def _transition(
        self,
        lifecycle: list[LifecycleTransition],
        status: TaskLifecycleStatus,
        *,
        reason: str | None = None,
        details: dict[str, Any] | None = None,
    ) -> None:
        transition = LifecycleTransition(status=status.value, reason=reason, details=details or {})
        lifecycle.append(transition)
        self.logger.log("task_transition", status=transition.status, reason=reason, details=transition.details)

    @staticmethod
    def _is_retryable_transport_exception(exc: Exception) -> bool:
        message = str(exc).strip().lower()
        combined = f"{exc.__class__.__name__} {message}".lower()
        markers = (
            "connection closed",
            "stream closed",
            "closedresourceerror",
            "endofstream",
            "broken pipe",
            "connection reset",
            "pipe is being closed",
            "transport",
            "stdio",
            "stdiobridge",
            "reloading",
            "please retry",
            "hint='retry'",
            "hint=\"retry\"",
            "eof",
        )
        return any(marker in combined for marker in markers)

    @classmethod
    def _build_failed_record(cls, step_id: str, exc: Exception, *, reason: str, retryable: bool) -> ExecutionRecord:
        message = cls._stringify_exception(exc)
        return ExecutionRecord(
            step_id=step_id,
            status="failed",
            details={
                "exception": {
                    "structured_content": {
                        "success": False,
                        "error": message,
                        "message": message,
                        "data": {
                            "reason": reason,
                            "retryable": retryable,
                            "exception_type": exc.__class__.__name__,
                        },
                    },
                    "content": [{"type": "text", "text": message}],
                    "is_error": True,
                }
            },
        )

    @staticmethod
    def _stringify_exception(exc: Exception) -> str:
        return f"{exc.__class__.__name__}: {exc}".strip()


def _severity_for_failure(category: str) -> str:
    if category in {"editor_unsaved_changes", "missing_resource", "execution_failure", "timeout"}:
        return "high"
    if category == "transport_retryable":
        return "medium"
    return "medium"


def _severity_for_verification(status: str) -> str:
    if status in {"console-errors", "console-errors-and-missing-objects", "missing-objects"}:
        return "high"
    if status == "warnings":
        return "medium"
    return "low"


def _load_unity_mcp_client() -> Callable[[Path], Any]:
    from mcp_client import UnityMcpClient

    return UnityMcpClient
