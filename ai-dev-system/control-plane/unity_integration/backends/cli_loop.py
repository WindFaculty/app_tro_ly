from __future__ import annotations

import json
from pathlib import Path
import subprocess
from typing import Any, Callable
from uuid import uuid4

from unity_integration.capability_catalog import UnityCapabilityCatalog
from unity_integration.contracts import UnityArtifactRecord, UnityEnvironmentConfig, UnityExecutionRequest, UnityExecutionResult


class UnityCliLoopBackend:
    name = "cli_loop"

    def __init__(
        self,
        environment: UnityEnvironmentConfig,
        *,
        runner: Callable[..., Any] | None = None,
    ) -> None:
        self._environment = environment
        self._runner = runner or subprocess.run

    def is_available(self) -> bool:
        return bool(self._environment.cli_loop_command) and self._environment.cli_loop_installed

    def supports_request(self, request: UnityExecutionRequest) -> bool:
        capability = UnityCapabilityCatalog.get(request.capability)
        return capability.supports_backend(self.name) and capability.cli_loop_command is not None

    def execute(self, request: UnityExecutionRequest) -> UnityExecutionResult:
        capability = UnityCapabilityCatalog.get(request.capability)
        if not self.supports_request(request):
            return UnityExecutionResult(
                status="failed",
                capability=request.capability,
                backend_used=self.name,
                warnings=[f"Unity CLI Loop does not support capability '{request.capability}'."],
                manual_validation_required=capability.manual_validation_required,
            )

        artifact_dir = self._environment.log_root / "unity-integration" / "cli-loop" / uuid4().hex
        artifact_dir.mkdir(parents=True, exist_ok=True)
        command = self._build_command(capability.cli_loop_command or "", request)
        try:
            completed = self._runner(
                command,
                cwd=self._environment.repo_root,
                capture_output=True,
                text=True,
                check=False,
            )
            returncode = int(getattr(completed, "returncode", 1))
            stdout = str(getattr(completed, "stdout", ""))
            stderr = str(getattr(completed, "stderr", ""))
            spawn_error = None
        except OSError as exc:
            returncode = -1
            stdout = ""
            stderr = f"{exc.__class__.__name__}: {exc}"
            spawn_error = str(exc)

        artifacts: list[UnityArtifactRecord] = []
        command_path = artifact_dir / "command.json"
        command_path.write_text(json.dumps({"command": command}, indent=2), encoding="utf-8")
        artifacts.append(UnityArtifactRecord(name="command", path=str(command_path), description="Unity CLI Loop command"))

        stdout_path = artifact_dir / "stdout.txt"
        stdout_path.write_text(stdout, encoding="utf-8")
        artifacts.append(UnityArtifactRecord(name="stdout", path=str(stdout_path), description="Unity CLI Loop stdout"))

        stderr_path = artifact_dir / "stderr.txt"
        stderr_path.write_text(stderr, encoding="utf-8")
        artifacts.append(UnityArtifactRecord(name="stderr", path=str(stderr_path), description="Unity CLI Loop stderr"))

        status = "completed" if returncode == 0 else "failed"
        payload = {
            "command": command,
            "artifact_dir": str(artifact_dir),
            "returncode": returncode,
        }
        if spawn_error:
            payload["spawn_error"] = spawn_error

        return UnityExecutionResult(
            status=status,
            capability=request.capability,
            backend_used=self.name,
            payload=payload,
            artifacts=artifacts,
            verification_summary={"returncode": returncode},
            manual_validation_required=capability.manual_validation_required,
        )

    def _build_command(self, cli_command: str, request: UnityExecutionRequest) -> list[str]:
        command = [*self._environment.cli_loop_command, cli_command, "--project-path", str(self._environment.project_root)]
        params = request.params

        if cli_command == "launch":
            if params.get("build_target"):
                command.extend(["-p", str(params["build_target"])])
            if params.get("restart"):
                command.append("-r")
            return command

        if cli_command == "compile":
            if "wait_for_domain_reload" in params:
                command.extend(["--wait-for-domain-reload", str(bool(params["wait_for_domain_reload"])).lower()])
            return command

        if cli_command == "get-logs":
            if "max_count" in params:
                command.extend(["--max-count", str(params["max_count"])])
            if params.get("log_type"):
                command.extend(["--log-type", str(params["log_type"])])
            if "include_stack_trace" in params:
                command.extend(["--include-stack-trace", str(bool(params["include_stack_trace"])).lower()])
            if params.get("search_text"):
                command.extend(["--search-text", str(params["search_text"])])
            return command

        if cli_command == "run-tests":
            command.extend(["--filter-type", str(params.get("filter_type", "all"))])
            if params.get("filter_value"):
                command.extend(["--filter-value", str(params["filter_value"])])
            requested_mode = params.get("test_mode") or params.get("mode")
            if request.capability == "editor.tests.editmode":
                requested_mode = requested_mode or "EditMode"
            elif request.capability == "editor.tests.playmode":
                requested_mode = requested_mode or "PlayMode"
            if requested_mode:
                command.extend(["--test-mode", str(requested_mode)])
            return command

        if cli_command == "control-play-mode":
            command.extend(["--action", str(params.get("action", "Play"))])
            return command

        if cli_command == "screenshot":
            command.extend(["--window-name", str(params.get("window_name", "Game"))])
            return command

        if cli_command == "execute-menu-item":
            menu_path = params.get("menu_item_path") or params.get("menu_path", "")
            command.extend(["--menu-item-path", str(menu_path)])
            return command

        if cli_command == "find-game-objects":
            search_term = params.get("name_pattern") or params.get("search_term", "")
            command.extend(["--name-pattern", str(search_term)])
            search_mode = params.get("search_mode") or params.get("search_method")
            if search_mode:
                command.extend(["--search-mode", str(search_mode)])
            if "max_results" in params:
                command.extend(["--max-results", str(params["max_results"])])
            if "include_inactive" in params:
                command.extend(["--include-inactive", str(bool(params["include_inactive"])).lower()])
            if params.get("tag"):
                command.extend(["--tag", str(params["tag"])])
            if params.get("layer"):
                command.extend(["--layer", str(params["layer"])])
            return command

        if cli_command == "get-hierarchy":
            if params.get("root_path"):
                command.extend(["--root-path", str(params["root_path"])])
            if "max_depth" in params:
                command.extend(["--max-depth", str(params["max_depth"])])
            if "include_components" in params:
                command.extend(["--include-components", str(bool(params["include_components"])).lower()])
            if "include_inactive" in params:
                command.extend(["--include-inactive", str(bool(params["include_inactive"])).lower()])
            if "include_paths" in params:
                command.extend(["--include-paths", str(bool(params["include_paths"])).lower()])
            return command

        return command
