from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch


ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from unity_integration.backends.cli_loop import UnityCliLoopBackend
from unity_integration.capability_catalog import UnityCapabilityCatalog
from unity_integration.contracts import UnityEnvironmentConfig, UnityExecutionRequest, UnityExecutionResult
from unity_integration.environment import UnityIntegrationEnvironment
from unity_integration.service import UnityIntegrationService


class _FakeCompletedProcess:
    def __init__(self, returncode: int = 0, stdout: str = "", stderr: str = "") -> None:
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr


class _SpawnFailureRunner:
    def __call__(self, *args, **kwargs):
        raise FileNotFoundError("uloop executable missing")


class _FakeBackend:
    def __init__(self, name: str, available: bool, *, supports: set[str] | None = None) -> None:
        self.name = name
        self.available = available
        self.supports = supports or set()
        self.requests: list[UnityExecutionRequest] = []

    def is_available(self) -> bool:
        return self.available

    def supports_request(self, request: UnityExecutionRequest) -> bool:
        return request.capability in self.supports

    def execute(self, request: UnityExecutionRequest) -> UnityExecutionResult:
        self.requests.append(request)
        return UnityExecutionResult(
            status="completed",
            capability=request.capability,
            backend_used=self.name,
            payload={"backend": self.name},
        )


class UnityIntegrationServiceTests(unittest.TestCase):
    def test_capability_catalog_exposes_curated_cli_and_skill_capabilities(self) -> None:
        capability_ids = set(UnityCapabilityCatalog.names())

        expected = {
            "editor.compile",
            "editor.logs.read",
            "editor.tests.editmode",
            "editor.tests.playmode",
            "scene.inspect",
            "scene.hierarchy.export",
            "gameobject.create",
            "gameobject.modify",
            "component.add",
            "script.create_or_update",
            "prefab.inspect",
            "asset.search",
            "editor.menu.execute",
            "editor.playmode.control",
            "editor.play",
            "scene.manage",
        }
        self.assertTrue(expected.issubset(capability_ids))

    def test_service_prefers_requested_backend_when_available(self) -> None:
        environment = UnityEnvironmentConfig(
            repo_root=Path("D:/repo"),
            ai_dev_root=Path("D:/repo/ai-dev-system"),
            project_root=Path("D:/repo/apps/unity-runtime"),
            log_root=Path("D:/repo/ai-dev-system/logs"),
            cli_loop_command=("uloop",),
            cli_loop_installed=True,
            unity_mcp_command=("uvx",),
            unity_mcp_available=True,
        )
        cli_backend = _FakeBackend("cli_loop", True, supports={"editor.compile"})
        mcp_backend = _FakeBackend("mcp", True, supports={"editor.compile"})
        service = UnityIntegrationService(environment=environment, backends=[cli_backend, mcp_backend])

        result = service.execute(UnityExecutionRequest(profile="unity-editor", capability="editor.compile", backend_preference="cli_loop"))

        self.assertEqual("cli_loop", result.backend_used)
        self.assertEqual(1, len(cli_backend.requests))
        self.assertEqual(0, len(mcp_backend.requests))

    def test_service_falls_back_to_next_available_backend(self) -> None:
        environment = UnityEnvironmentConfig(
            repo_root=Path("D:/repo"),
            ai_dev_root=Path("D:/repo/ai-dev-system"),
            project_root=Path("D:/repo/apps/unity-runtime"),
            log_root=Path("D:/repo/ai-dev-system/logs"),
            cli_loop_command=("uloop",),
            cli_loop_installed=False,
            unity_mcp_command=("uvx",),
            unity_mcp_available=True,
        )
        cli_backend = _FakeBackend("cli_loop", False, supports={"editor.playmode.control"})
        mcp_backend = _FakeBackend("mcp", True, supports={"editor.playmode.control"})
        service = UnityIntegrationService(environment=environment, backends=[cli_backend, mcp_backend])

        result = service.execute(UnityExecutionRequest(profile="unity-editor", capability="editor.playmode.control"))

        self.assertEqual("mcp", result.backend_used)
        self.assertEqual(0, len(cli_backend.requests))
        self.assertEqual(1, len(mcp_backend.requests))

    def test_environment_probe_detects_cli_loop_package_reference(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            repo_root = Path(temp_dir)
            project_root = repo_root / "apps" / "unity-runtime"
            packages = project_root / "Packages"
            packages.mkdir(parents=True)
            (packages / "manifest.json").write_text(
                json.dumps(
                    {
                        "dependencies": {
                            "io.github.hatayama.uloopmcp": "https://github.com/hatayama/unity-cli-loop.git?path=/Packages/src"
                        }
                    }
                ),
                encoding="utf-8",
            )

            environment = UnityIntegrationEnvironment(repo_root=repo_root)
            config = environment.probe()

        self.assertEqual(project_root, config.project_root)
        self.assertTrue(config.cli_loop_package_installed)
        self.assertEqual(project_root / "Packages" / "manifest.json", config.package_manifest_path)

    def test_environment_probe_prefers_windows_uloop_cmd(self) -> None:
        cmd_path = r"C:\Users\tranx\AppData\Roaming\npm\uloop.cmd"
        with patch("unity_integration.environment.shutil.which") as which_mock:
            which_mock.side_effect = lambda name: cmd_path if name == "uloop.cmd" else None
            command = UnityIntegrationEnvironment._resolve_cli_loop_command()

        self.assertEqual((cmd_path,), command)

    def test_environment_probe_wraps_windows_uloop_powershell_script(self) -> None:
        script_path = r"C:\Users\tranx\AppData\Roaming\npm\uloop.ps1"
        with patch("unity_integration.environment.shutil.which") as which_mock:
            def fake_which(name: str):
                if name == "uloop.ps1":
                    return script_path
                if name in {"powershell", "powershell.exe"}:
                    return r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe"
                return None

            which_mock.side_effect = fake_which
            command = UnityIntegrationEnvironment._resolve_cli_loop_command()

        self.assertEqual(
            (
                r"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                script_path,
            ),
            command,
        )

    def test_cli_loop_backend_normalizes_stdout_and_stderr_artifacts(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            environment = UnityEnvironmentConfig(
                repo_root=temp_path,
                ai_dev_root=temp_path / "ai-dev-system",
                project_root=temp_path / "apps" / "unity-runtime",
                log_root=temp_path / "logs",
                cli_loop_command=("uloop",),
                cli_loop_installed=True,
            )
            backend = UnityCliLoopBackend(
                environment,
                runner=lambda *args, **kwargs: _FakeCompletedProcess(
                    returncode=0,
                    stdout="compile ok",
                    stderr="warning: sample",
                ),
            )

            result = backend.execute(
                UnityExecutionRequest(
                    profile="unity-editor",
                    capability="editor.compile",
                    params={"wait_for_domain_reload": True},
                )
            )

            self.assertEqual("completed", result.status)
            self.assertEqual("cli_loop", result.backend_used)
            artifact_paths = {artifact.name: Path(artifact.path) for artifact in result.artifacts}
            self.assertIn("stdout", artifact_paths)
            self.assertIn("stderr", artifact_paths)
            self.assertEqual("compile ok", artifact_paths["stdout"].read_text(encoding="utf-8"))
            self.assertEqual("warning: sample", artifact_paths["stderr"].read_text(encoding="utf-8"))

    def test_cli_loop_backend_returns_structured_failure_when_spawn_fails(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_path = Path(temp_dir)
            environment = UnityEnvironmentConfig(
                repo_root=temp_path,
                ai_dev_root=temp_path / "ai-dev-system",
                project_root=temp_path / "apps" / "unity-runtime",
                log_root=temp_path / "logs",
                cli_loop_command=("uloop",),
                cli_loop_installed=True,
            )
            backend = UnityCliLoopBackend(environment, runner=_SpawnFailureRunner())

            result = backend.execute(
                UnityExecutionRequest(
                    profile="unity-editor",
                    capability="editor.compile",
                )
            )

            self.assertEqual("failed", result.status)
            self.assertEqual("cli_loop", result.backend_used)
            self.assertEqual(-1, result.payload["returncode"])
            self.assertIn("spawn_error", result.payload)
            artifact_paths = {artifact.name: Path(artifact.path) for artifact in result.artifacts}
            self.assertIn("stderr", artifact_paths)
            self.assertIn("uloop executable missing", artifact_paths["stderr"].read_text(encoding="utf-8"))

    def test_cli_loop_backend_maps_run_tests_to_test_mode(self) -> None:
        environment = UnityEnvironmentConfig(
            repo_root=Path("D:/repo"),
            ai_dev_root=Path("D:/repo/ai-dev-system"),
            project_root=Path("D:/repo/apps/unity-runtime"),
            log_root=Path("D:/repo/ai-dev-system/logs"),
            cli_loop_command=("uloop",),
            cli_loop_installed=True,
        )
        backend = UnityCliLoopBackend(environment)

        command = backend._build_command(
            "run-tests",
            UnityExecutionRequest(
                profile="unity-editor",
                capability="editor.tests.editmode",
                params={"filter_type": "assembly", "filter_value": "LocalAssistant.EditModeTests"},
            ),
        )

        self.assertIn("--test-mode", command)
        self.assertIn("EditMode", command)
        self.assertNotIn("--mode", command)

    def test_cli_loop_backend_maps_find_game_objects_to_name_pattern(self) -> None:
        environment = UnityEnvironmentConfig(
            repo_root=Path("D:/repo"),
            ai_dev_root=Path("D:/repo/ai-dev-system"),
            project_root=Path("D:/repo/apps/unity-runtime"),
            log_root=Path("D:/repo/ai-dev-system/logs"),
            cli_loop_command=("uloop",),
            cli_loop_installed=True,
        )
        backend = UnityCliLoopBackend(environment)

        command = backend._build_command(
            "find-game-objects",
            UnityExecutionRequest(
                profile="unity-editor",
                capability="gameobject.find",
                params={"search_term": "Main Camera", "search_method": "Contains"},
            ),
        )

        self.assertIn("--name-pattern", command)
        self.assertIn("Main Camera", command)
        self.assertIn("--search-mode", command)
        self.assertIn("Contains", command)

    def test_cli_loop_backend_maps_execute_menu_item_path(self) -> None:
        environment = UnityEnvironmentConfig(
            repo_root=Path("D:/repo"),
            ai_dev_root=Path("D:/repo/ai-dev-system"),
            project_root=Path("D:/repo/apps/unity-runtime"),
            log_root=Path("D:/repo/ai-dev-system/logs"),
            cli_loop_command=("uloop",),
            cli_loop_installed=True,
        )
        backend = UnityCliLoopBackend(environment)

        command = backend._build_command(
            "execute-menu-item",
            UnityExecutionRequest(
                profile="unity-editor",
                capability="editor.menu.execute",
                params={"menu_path": "Window/General/Console"},
            ),
        )

        self.assertIn("--menu-item-path", command)
        self.assertIn("Window/General/Console", command)

    def test_cli_loop_backend_maps_hierarchy_and_logs_flags(self) -> None:
        environment = UnityEnvironmentConfig(
            repo_root=Path("D:/repo"),
            ai_dev_root=Path("D:/repo/ai-dev-system"),
            project_root=Path("D:/repo/apps/unity-runtime"),
            log_root=Path("D:/repo/ai-dev-system/logs"),
            cli_loop_command=("uloop",),
            cli_loop_installed=True,
        )
        backend = UnityCliLoopBackend(environment)

        hierarchy_command = backend._build_command(
            "get-hierarchy",
            UnityExecutionRequest(
                profile="unity-editor",
                capability="scene.inspect",
                params={"max_depth": 2, "include_components": True, "include_inactive": False, "include_paths": True},
            ),
        )
        logs_command = backend._build_command(
            "get-logs",
            UnityExecutionRequest(
                profile="unity-editor",
                capability="editor.logs.read",
                params={"max_count": 25, "log_type": "All", "include_stack_trace": True, "search_text": "error"},
            ),
        )

        self.assertIn("--max-depth", hierarchy_command)
        self.assertIn("--include-components", hierarchy_command)
        self.assertIn("--include-inactive", hierarchy_command)
        self.assertIn("--include-paths", hierarchy_command)
        self.assertIn("--log-type", logs_command)
        self.assertIn("--include-stack-trace", logs_command)
        self.assertIn("--search-text", logs_command)


if __name__ == "__main__":
    unittest.main()
