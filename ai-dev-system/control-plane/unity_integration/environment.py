from __future__ import annotations

import json
import os
from pathlib import Path
import shutil
import shlex

from unity_integration.contracts import UnityEnvironmentConfig


class UnityIntegrationEnvironment:
    def __init__(self, repo_root: Path | None = None) -> None:
        self._repo_root = repo_root or Path(__file__).resolve().parents[3]

    def probe(self) -> UnityEnvironmentConfig:
        repo_root = self._repo_root
        ai_dev_root = repo_root / "ai-dev-system"
        project_root = self._resolve_project_root(repo_root)
        log_root = ai_dev_root / "logs"
        package_manifest_path = project_root / "Packages" / "manifest.json"
        cli_loop_package_installed = self._manifest_mentions_cli_loop(package_manifest_path)
        cli_loop_command = self._resolve_cli_loop_command()
        unity_mcp_command = self._resolve_unity_mcp_command()
        editor_path = self._resolve_unity_editor_path()
        return UnityEnvironmentConfig(
            repo_root=repo_root,
            ai_dev_root=ai_dev_root,
            project_root=project_root,
            log_root=log_root,
            editor_path=editor_path,
            package_manifest_path=package_manifest_path if package_manifest_path.exists() else None,
            cli_loop_command=cli_loop_command,
            cli_loop_installed=bool(cli_loop_command),
            cli_loop_package_installed=cli_loop_package_installed,
            unity_mcp_command=unity_mcp_command,
            unity_mcp_available=bool(unity_mcp_command),
            unity_mcp_env={"SystemRoot": os.environ.get("SystemRoot", r"C:\\Windows")},
        )

    @staticmethod
    def _manifest_mentions_cli_loop(path: Path) -> bool:
        if not path.exists():
            return False
        try:
            payload = json.loads(path.read_text(encoding="utf-8"))
        except json.JSONDecodeError:
            return False
        text = json.dumps(payload)
        markers = (
            "unity-cli-loop",
            "uLoopMCP",
            "io.github.hatayama.uloopmcp",
        )
        return any(marker in text for marker in markers)

    def _resolve_project_root(self, repo_root: Path) -> Path:
        candidates = (
            repo_root / "apps" / "unity-runtime",
        )
        return next((candidate for candidate in candidates if candidate.exists()), candidates[0])

    @staticmethod
    def _resolve_cli_loop_command() -> tuple[str, ...]:
        override = os.environ.get("APP_TRO_LY_ULOOP_COMMAND")
        if override:
            return tuple(shlex.split(override))
        if os.name == "nt":
            for candidate in ("uloop.cmd", "uloop.exe"):
                resolved = shutil.which(candidate)
                if resolved:
                    return (resolved,)

            ps1_path = shutil.which("uloop.ps1")
            if ps1_path:
                powershell_path = shutil.which("powershell") or shutil.which("powershell.exe") or "powershell"
                return (
                    powershell_path,
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    ps1_path,
                )

        resolved_uloop = shutil.which("uloop")
        if resolved_uloop:
            return (resolved_uloop,)
        if shutil.which("npx"):
            return ("npx", "uloop-cli")
        return tuple()

    @staticmethod
    def _resolve_unity_mcp_command() -> tuple[str, ...]:
        override = os.environ.get("APP_TRO_LY_UNITY_MCP_COMMAND")
        if override:
            return tuple(shlex.split(override))
        uvx_path = shutil.which("uvx")
        if uvx_path:
            return (
                uvx_path,
                "-p",
                "3.14",
                "--from",
                "mcpforunityserver",
                "mcp-for-unity",
                "--transport",
                "stdio",
            )
        legacy_uvx = Path(r"C:\Users\tranx\AppData\Local\Programs\Python\Python314\Scripts\uvx.exe")
        if legacy_uvx.exists():
            return (
                str(legacy_uvx),
                "-p",
                "3.14",
                "--from",
                "mcpforunityserver",
                "mcp-for-unity",
                "--transport",
                "stdio",
            )
        return tuple()

    @staticmethod
    def _resolve_unity_editor_path() -> Path | None:
        override = os.environ.get("APP_TRO_LY_UNITY_EDITOR") or os.environ.get("UNITY_EDITOR_PATH")
        if override:
            path = Path(override)
            return path if path.exists() else path

        default_hub_root = Path(r"C:\Program Files\Unity\Hub\Editor")
        if default_hub_root.exists():
            candidates = sorted(default_hub_root.glob("*/*/Unity.exe"))
            if candidates:
                return candidates[-1]

        legacy_path = Path(r"D:\6000.3.11f1\Editor\Unity.exe")
        if legacy_path.exists():
            return legacy_path
        return None
