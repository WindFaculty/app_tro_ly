from __future__ import annotations

from pathlib import Path
from typing import Any

from app.agent.state import RunState, SelectorSpec
from app.agent.task_spec import TaskSpec
from app.blender.assertions import BlenderAssertionRunner
from app.blender.capabilities import BlenderCapabilityRegistry
from app.blender.mcp_runtime import BlenderMcpRuntime
from app.blender.preflight import BlenderPreflight
from app.profiles.base_profile import BaseProfile
from app.profiles.registry import ProfileRegistry


@ProfileRegistry.register("blender-editor")
class BlenderEditorProfile(BaseProfile):
    def __init__(self) -> None:
        super().__init__(
            name="blender-editor",
            executable="blender.exe",
            window_selector=SelectorSpec(
                title_re=r".* - Blender .*",
                class_name="GHOST_WindowClass",
                backend="uia",
            ),
            launch_delay_seconds=4.0,
        )
        self._preflight = BlenderPreflight()
        self._assertions = BlenderAssertionRunner()
        self._last_run_context: dict[str, Any] = {}

    def build_plan(self, task: str, working_directory: Path):
        del task, working_directory
        raise ValueError("The blender-editor profile requires --task-file with structured actions[] in v1.")

    def build_plan_from_task_spec(self, task_spec: TaskSpec, working_directory: Path):
        del working_directory
        if not task_spec.actions:
            raise ValueError("The blender-editor profile requires actions[] in the task file.")
        tools = list(self._last_run_context.get("available_tools") or [])
        resources = list(self._last_run_context.get("available_resources") or [])
        return BlenderCapabilityRegistry.compile_actions(task_spec=task_spec, tools=tools, resources=resources)

    def task_spec_from_alias(self, task: str) -> TaskSpec | None:
        del task
        return None

    def prepare_run_context(
        self,
        *,
        task_input: str | TaskSpec,
        settings,
        driver,
        pywinauto,
        artifacts,
        logger,
        active_window,
    ) -> dict[str, Any]:
        del task_input, settings, driver, pywinauto, artifacts, active_window
        context: dict[str, Any] = {
            "mcp_connected": False,
            "available_tools": [],
            "available_resources": [],
            "capability_matrix": [],
        }
        runtime = BlenderMcpRuntime(Path(__file__).resolve().parents[4])
        try:
            runtime.connect()
            snapshot = runtime.capability_snapshot()
            context.update(
                {
                    "blender_runtime": runtime,
                    "mcp_connected": True,
                    "available_tools": list(snapshot.get("tools") or []),
                    "available_resources": list(snapshot.get("resources") or []),
                }
            )
            context["capability_matrix"] = BlenderCapabilityRegistry.build_matrix(
                tools=context["available_tools"],
                resources=context["available_resources"],
            )
        except Exception as exc:
            logger.log("blender_mcp_connect_failed", error=str(exc))
            context["mcp_error"] = str(exc)
            context["capability_matrix"] = BlenderCapabilityRegistry.build_matrix(tools=[], resources=[])
            runtime.close()
        self._last_run_context = context
        return context

    def cleanup_run_context(
        self,
        *,
        task_input: str | TaskSpec,
        settings,
        artifacts,
        logger,
        run_context: dict[str, Any] | None,
    ) -> None:
        del task_input, settings, artifacts, logger
        runtime = (run_context or {}).get("blender_runtime")
        if runtime is not None:
            runtime.close()
        self._last_run_context = {}

    def run_preflight(
        self,
        *,
        task_input: str | TaskSpec,
        settings,
        driver,
        pywinauto,
        artifacts,
        logger,
        active_window,
        run_context: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        del settings, pywinauto, artifacts, logger
        required_capabilities = []
        if isinstance(task_input, TaskSpec):
            required_capabilities = [action.capability for action in task_input.actions]
        preflight = self._preflight.evaluate(
            task_input=task_input,
            driver=driver,
            active_window=active_window,
            run_context=run_context,
            required_capabilities=required_capabilities,
        )
        if preflight.get("blocked_reason"):
            raise RuntimeError(str(preflight["blocked_reason"]))
        return preflight

    def inspect_extras(
        self,
        *,
        settings,
        driver,
        pywinauto,
        screenshots,
        artifacts,
        logger,
        target_window,
    ) -> dict[str, Any]:
        del settings, driver, pywinauto, screenshots, target_window
        runtime = BlenderMcpRuntime(Path(__file__).resolve().parents[4])
        try:
            runtime.connect()
            snapshot = runtime.capability_snapshot()
            payload: dict[str, Any] = {
                "available_tools": list(snapshot.get("tools") or []),
                "available_resources": list(snapshot.get("resources") or []),
                "capabilities": BlenderCapabilityRegistry.build_matrix(
                    tools=list(snapshot.get("tools") or []),
                    resources=list(snapshot.get("resources") or []),
                ),
            }
            if "get_viewport_screenshot" in payload["available_tools"]:
                try:
                    result = runtime.call_tool("get_viewport_screenshot", {})
                    image_bytes = runtime.extract_image_bytes(result)
                    structured = result.get("structured_content") or {}
                    if image_bytes:
                        screenshot_path = artifacts.screenshot_path("blender-viewport")
                        screenshot_path.write_bytes(image_bytes)
                        payload["viewport_screenshot"] = str(screenshot_path)
                    else:
                        payload["viewport_screenshot_error"] = str(
                            structured.get("error")
                            or structured.get("message")
                            or "Viewport screenshot did not return image data. Make sure the Blender addon is connected."
                        )
                    artifacts.write_json("blender-viewport-result.json", result)
                except Exception as exc:
                    payload["viewport_screenshot_error"] = str(exc)
            logger.log("blender_inspect_snapshot", snapshot=payload)
            return payload
        finally:
            runtime.close()

    def run_task_verification(
        self,
        *,
        task_input: str | TaskSpec,
        settings,
        artifacts,
        logger,
        state: RunState,
        run_context: dict[str, Any] | None,
    ) -> dict[str, Any]:
        del settings, artifacts, logger, state
        if not isinstance(task_input, TaskSpec) or not task_input.verify:
            return {"passed": True, "checks": []}
        runtime = (run_context or {}).get("blender_runtime")
        if runtime is None:
            return {
                "passed": False,
                "checks": [{"kind": "blender_runtime", "passed": False, "error": "Blender MCP runtime is unavailable."}],
            }
        return self._assertions.run(task_input.verify, runtime=runtime)

    def list_capabilities(
        self,
        *,
        settings,
    ) -> dict[str, Any]:
        del settings
        runtime = BlenderMcpRuntime(Path(__file__).resolve().parents[4])
        try:
            runtime.connect()
            snapshot = runtime.capability_snapshot()
            return {
                "profile": self.name,
                "available_tools": snapshot.get("tools") or [],
                "available_resources": snapshot.get("resources") or [],
                "capabilities": BlenderCapabilityRegistry.build_matrix(
                    tools=list(snapshot.get("tools") or []),
                    resources=list(snapshot.get("resources") or []),
                ),
            }
        finally:
            runtime.close()

    def summary_file_name(self) -> str | None:
        return "blender-summary.json"

    def build_run_summary(
        self,
        *,
        task_input: str | TaskSpec,
        settings,
        driver,
        pywinauto,
        screenshots,
        artifacts,
        logger,
        state: RunState,
        preflight: dict[str, Any],
        error: str | None,
        run_context: dict[str, Any] | None = None,
    ) -> dict[str, Any] | None:
        del settings, driver, pywinauto, screenshots, artifacts
        task_payload = task_input.to_dict() if isinstance(task_input, TaskSpec) else {"task": task_input}
        verification_attempts = []
        verification_passed = error is None
        for attempt in state.attempts:
            verification_attempts.append(
                {
                    "action": attempt.request_name,
                    "strategy": attempt.strategy,
                    "status": attempt.status,
                    "error": attempt.error,
                    "details": attempt.details,
                }
            )
            if attempt.status not in {"completed", "healed"}:
                verification_passed = False

        task_verification = state.details.get("task_verification") or {"passed": True, "checks": []}
        if not task_verification.get("passed", True):
            verification_passed = False

        summary = {
            "profile": self.name,
            "task": task_payload,
            "status": state.status,
            "blocked_reason": preflight.get("blocked_reason") or error,
            "preflight": preflight,
            "capability_matrix": list((run_context or {}).get("capability_matrix") or []),
            "verification_result": {
                "passed": verification_passed,
                "attempts": verification_attempts,
                "task_verification": task_verification,
            },
        }
        logger.log("blender_run_summary", summary=summary)
        return summary
