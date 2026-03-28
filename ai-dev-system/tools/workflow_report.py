from __future__ import annotations

from typing import Any


def build_workflow_report(summary: dict[str, Any]) -> dict[str, Any]:
    steps = summary.get("steps") or []
    failed_steps: list[dict[str, Any]] = []
    verification_reports: list[dict[str, Any]] = []

    for step in steps:
        if not isinstance(step, dict):
            continue

        step_id = str(step.get("step_id", ""))
        status = str(step.get("status", "unknown"))
        details = step.get("details") or {}

        if status != "completed":
            failed_steps.append(
                {
                    "step_id": step_id,
                    "status": status,
                    "messages": _extract_detail_messages(details),
                }
            )

        console_analysis = details.get("console_analysis")
        if isinstance(console_analysis, dict):
            verification_reports.append(
                {
                    "step_id": step_id,
                    "counts": console_analysis.get("counts") or {},
                    "top_errors": _summarize_entries(console_analysis.get("app_errors") or []),
                    "top_warnings": _summarize_entries(console_analysis.get("app_warnings") or []),
                    "top_logs": _summarize_entries(console_analysis.get("app_logs") or [], limit=3),
                    "screenshot": _extract_screenshot_path(details.get("screenshot") or {}),
                    "missing_objects": _extract_missing_objects(details.get("expected_objects") or {}),
                }
            )

    return {
        "task_id": summary.get("task_id"),
        "task_title": summary.get("task_title"),
        "step_count": len([step for step in steps if isinstance(step, dict)]),
        "failed_step_count": len(failed_steps),
        "failed_steps": failed_steps,
        "verification_reports": verification_reports,
        "stopped_after_step": summary.get("stopped_after_step"),
        "overall_status": summary.get("workflow_status") or ("failed" if failed_steps else "completed"),
    }


def format_workflow_report(report: dict[str, Any]) -> str:
    lines = [
        f"Task: {report.get('task_title') or report.get('task_id') or 'unknown task'}",
        f"Overall status: {report.get('overall_status', 'unknown')}",
        f"Steps: {report.get('step_count', 0)}",
    ]

    failed_steps = report.get("failed_steps") or []
    if failed_steps:
        lines.append(f"Failed steps: {len(failed_steps)}")
        for failed_step in failed_steps:
            lines.append(
                f"- {failed_step.get('step_id', 'unknown')}: "
                f"{'; '.join(failed_step.get('messages') or ['No detail message'])}"
            )
        stopped_after_step = report.get("stopped_after_step")
        if stopped_after_step:
            lines.append(f"Stopped after step: {stopped_after_step}")
    else:
        lines.append("Failed steps: 0")

    verification_reports = report.get("verification_reports") or []
    if not verification_reports:
        lines.append("Verification: no verification steps recorded")
        return "\n".join(lines)

    for verification in verification_reports:
        counts = verification.get("counts") or {}
        lines.append(
            "Verification "
            f"{verification.get('step_id', 'unknown')}: "
            f"errors={counts.get('app_errors', 0)}, "
            f"warnings={counts.get('app_warnings', 0)}, "
            f"logs={counts.get('app_logs', 0)}, "
            f"noise_filtered={counts.get('noise_filtered', 0)}"
        )

        screenshot = verification.get("screenshot")
        if screenshot:
            lines.append(f"Screenshot: {screenshot}")

        missing_objects = verification.get("missing_objects") or []
        if missing_objects:
            lines.append(f"Missing objects: {', '.join(missing_objects)}")

        for label, entries in (
            ("Errors", verification.get("top_errors") or []),
            ("Warnings", verification.get("top_warnings") or []),
            ("Logs", verification.get("top_logs") or []),
        ):
            if not entries:
                continue
            lines.append(f"{label}:")
            for entry in entries:
                lines.append(f"- {entry}")

    return "\n".join(lines)


def _extract_detail_messages(details: dict[str, Any]) -> list[str]:
    messages: list[str] = []

    for value in details.values():
        message = _extract_message(value)
        if message:
            messages.append(message)

    return messages[:5]


def _extract_message(value: Any) -> str | None:
    if not isinstance(value, dict):
        return None

    structured = value.get("structured_content") or {}
    for key in ("error", "code", "message"):
        message = structured.get(key)
        if message:
            return str(message)

    for item in value.get("content") or []:
        if not isinstance(item, dict):
            continue
        text = item.get("text")
        if text:
            return str(text).strip()

    return None


def _extract_screenshot_path(payload: dict[str, Any]) -> str | None:
    if not isinstance(payload, dict):
        return None

    structured = payload.get("structured_content") or {}
    data = structured.get("data") or {}
    path = data.get("fullPath") or data.get("path")
    if path:
        return str(path)
    return None


def _summarize_entries(entries: list[dict[str, Any]], limit: int = 5) -> list[str]:
    summarized: list[str] = []
    for entry in entries[:limit]:
        if not isinstance(entry, dict):
            continue
        entry_type = str(entry.get("type", "entry")).strip()
        message = str(entry.get("message", "")).strip()
        if not message:
            continue
        summarized.append(f"{entry_type}: {message}")
    return summarized


def _extract_missing_objects(expected_objects: dict[str, Any]) -> list[str]:
    missing: list[str] = []
    for object_name, lookup_result in expected_objects.items():
        if not isinstance(lookup_result, dict):
            missing.append(str(object_name))
            continue

        structured = lookup_result.get("structured_content") or {}
        data = structured.get("data") or {}
        items = data.get("items") or []
        instance_ids = data.get("instanceIDs") or []
        total_count = data.get("totalCount")
        if len(items) == 0 and len(instance_ids) == 0 and total_count in (None, 0):
            missing.append(str(object_name))

    return missing
