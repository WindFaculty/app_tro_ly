from __future__ import annotations

from pathlib import Path
from typing import Any

from app.agent.task_spec import TaskVerifySpec


class BlenderAssertionRunner:
    def run(self, verify_specs: list[TaskVerifySpec], *, runtime) -> dict[str, Any]:
        checks: list[dict[str, Any]] = []
        passed = True
        for spec in verify_specs:
            result = self._run_single(spec, runtime=runtime)
            checks.append(result)
            if not result.get("passed", False):
                passed = False
        return {"passed": passed, "checks": checks}

    def _run_single(self, spec: TaskVerifySpec, *, runtime) -> dict[str, Any]:
        kind = spec.kind
        params = spec.params

        if kind == "scene_contains_object":
            result = runtime.call_tool("get_scene_info", {})
            data = self._normalized_data(result)
            objects = [str(item.get("name") or "") for item in list(data.get("objects") or []) if isinstance(item, dict)]
            expected = str(params["name"])
            return {"kind": kind, "passed": expected in objects, "expected": expected, "objects": objects}

        if kind == "object_info_available":
            arguments = dict(params)
            result = runtime.call_tool("get_object_info", arguments)
            structured = result.get("structured_content") or {}
            passed = bool(structured.get("success")) and not bool(result.get("is_error"))
            return {"kind": kind, "passed": passed, "params": arguments}

        if kind == "file_exists":
            path = Path(str(params["path"]))
            return {"kind": kind, "passed": path.exists(), "path": str(path)}

        raise ValueError(f"Unsupported Blender verification kind: {kind}")

    @staticmethod
    def _normalized_data(result: dict[str, Any]) -> dict[str, Any]:
        structured = result.get("structured_content") or {}
        if not isinstance(structured, dict):
            return {}
        data = structured.get("data")
        if isinstance(data, dict):
            return data
        return structured
