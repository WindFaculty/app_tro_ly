from __future__ import annotations

import json
import sys
import time
from pathlib import Path
from typing import Any


AI_ROOT = Path(__file__).resolve().parents[2]
if str(AI_ROOT) not in sys.path:
    sys.path.insert(0, str(AI_ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from app.logging.artifacts import ArtifactManager
from unity_integration.contracts import UnityExecutionRequest
from unity_integration.service import UnityIntegrationService
from verify_unity_integration import probe_cli_loop


E2E_STEPS: tuple[dict[str, Any], ...] = (
    {
        "name": "launch",
        "capability": "editor.launch",
        "params": {"restart": False},
    },
    {
        "name": "compile",
        "capability": "editor.compile",
        "params": {"wait_for_domain_reload": True},
    },
    {
        "name": "inspect",
        "capability": "scene.inspect",
        "params": {
            "max_depth": 2,
            "include_components": True,
            "include_inactive": False,
            "include_paths": True,
        },
        "expect_json_stdout": True,
    },
    {
        "name": "play",
        "capability": "editor.playmode.control",
        "params": {"action": "Play"},
    },
    {
        "name": "stop",
        "capability": "editor.playmode.control",
        "params": {"action": "Stop"},
    },
    {
        "name": "editmode-test",
        "capability": "editor.tests.editmode",
        "params": {
            "filter_type": "assembly",
            "filter_value": "LocalAssistant.EditModeTests",
            "test_mode": "EditMode",
        },
    },
    {
        "name": "logs",
        "capability": "editor.logs.read",
        "params": {
            "max_count": 100,
            "log_type": "All",
            "include_stack_trace": True,
        },
    },
)


def _artifact_map(result_dict: dict[str, Any]) -> dict[str, str]:
    return {
        str(item.get("name")): str(item.get("path"))
        for item in result_dict.get("artifacts", [])
        if item.get("name") and item.get("path")
    }


def _parse_stdout_json(result_dict: dict[str, Any]) -> tuple[dict[str, Any] | None, str | None]:
    stdout_path = _artifact_map(result_dict).get("stdout")
    if not stdout_path:
        return None, "stdout artifact is missing."

    text = Path(stdout_path).read_text(encoding="utf-8").strip()
    if not text:
        return None, "stdout artifact is empty."

    try:
        return json.loads(text), None
    except json.JSONDecodeError as exc:
        return None, f"stdout artifact is not valid JSON: {exc}"


def _run_step(service: UnityIntegrationService, step: dict[str, Any], artifact_manager: ArtifactManager, index: int) -> dict[str, Any]:
    started = time.monotonic()
    request = UnityExecutionRequest(
        profile="unity-editor",
        capability=str(step["capability"]),
        params=dict(step.get("params") or {}),
        backend_preference="cli_loop",
        allow_fallback=False,
    )

    try:
        result = service.execute(request)
        result_dict = result.to_dict()
    except Exception as exc:
        result_dict = {
            "status": "failed",
            "capability": request.capability,
            "backend_used": "cli_loop",
            "payload": {"exception": f"{exc.__class__.__name__}: {exc}"},
            "artifacts": [],
            "verification_summary": {},
            "warnings": [],
            "manual_validation_required": False,
        }

    step_summary: dict[str, Any] = {
        "index": index,
        "name": step["name"],
        "request": request.to_dict(),
        "elapsed_seconds": round(time.monotonic() - started, 3),
        "result": result_dict,
        "artifacts": _artifact_map(result_dict),
    }

    if step.get("expect_json_stdout"):
        parsed_stdout, parse_error = _parse_stdout_json(result_dict)
        step_summary["parsed_stdout_json"] = parsed_stdout
        if parse_error is not None:
            step_summary["stdout_parse_error"] = parse_error
            step_summary["result"]["status"] = "failed"
            step_summary["result"].setdefault("warnings", []).append(parse_error)

    artifact_manager.write_json(f"step-{index:02d}-{step['name']}.json", step_summary)
    return step_summary


def main() -> int:
    service = UnityIntegrationService()
    artifact_manager = ArtifactManager.create(
        service.environment.log_root / "unity-integration" / "cli-loop-e2e",
        "unity-cli-loop-e2e",
    )

    start = time.monotonic()
    cli_probe = probe_cli_loop(service)
    steps: list[dict[str, Any]] = []
    blocking_failure = False

    for index, step in enumerate(E2E_STEPS, start=1):
        is_logs_step = step["name"] == "logs"
        if blocking_failure and not is_logs_step:
            skipped = {
                "index": index,
                "name": step["name"],
                "request": {
                    "profile": "unity-editor",
                    "capability": step["capability"],
                    "params": dict(step.get("params") or {}),
                    "backend_preference": "cli_loop",
                    "allow_fallback": False,
                },
                "elapsed_seconds": 0.0,
                "result": {
                    "status": "skipped",
                    "capability": step["capability"],
                    "backend_used": "cli_loop",
                    "payload": {"reason": "Skipped after an earlier E2E failure."},
                    "artifacts": [],
                    "verification_summary": {},
                    "warnings": [],
                    "manual_validation_required": False,
                },
                "artifacts": {},
            }
            artifact_manager.write_json(f"step-{index:02d}-{step['name']}.json", skipped)
            steps.append(skipped)
            continue

        step_summary = _run_step(service, step, artifact_manager, index)
        steps.append(step_summary)
        if step_summary["result"]["status"] != "completed" and not is_logs_step:
            blocking_failure = True

    status = "completed"
    if not cli_probe["spawnable"] or any(step["result"]["status"] != "completed" for step in steps):
        status = "failed"

    summary = {
        "run_id": artifact_manager.run_id,
        "status": status,
        "started_at": artifact_manager.started_at,
        "finished_at": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "elapsed_seconds": round(time.monotonic() - start, 3),
        "environment": service.environment.to_dict(),
        "cli_probe": cli_probe,
        "steps": steps,
    }
    artifact_manager.write_json("summary.json", summary)

    print(f"Unity CLI Loop E2E artifact dir: {artifact_manager.run_dir}")
    print(f"Unity CLI Loop E2E status: {status}")
    return 0 if status == "completed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
