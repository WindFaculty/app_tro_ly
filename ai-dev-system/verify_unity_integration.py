from __future__ import annotations

import os
from pathlib import Path
import subprocess
from typing import Any

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()


ROOT = Path(__file__).resolve().parent
REPO_ROOT = ROOT.parent

from unity_integration.service import UnityIntegrationService


def detect_cli_loop_blockers(service: UnityIntegrationService) -> list[dict[str, str]]:
    environment = service.environment
    blockers: list[dict[str, str]] = []

    roslyn_plugin_dir = environment.project_root / "Assets" / "Plugins" / "Roslyn"
    roslyn_metadata = roslyn_plugin_dir / "System.Reflection.Metadata.dll"
    if roslyn_metadata.exists():
        blockers.append(
            {
                "code": "local_roslyn_plugin_present",
                "message": (
                    "Local Roslyn plugin DLLs exist under "
                    f"{roslyn_plugin_dir}. These can conflict with Unity CLI Loop's bundled CodeAnalysis plugins."
                ),
            }
        )

    editor_log = Path(os.environ.get("LOCALAPPDATA", "")) / "Unity" / "Editor" / "Editor.log"
    if editor_log.exists():
        content = editor_log.read_bytes()
        if (
            b"Packages/io.github.hatayama.uloopmcp/Plugins/CodeAnalysis/System.Reflection.Metadata.dll" in content
            and b"Assets/Plugins/Roslyn/System.Reflection.Metadata.dll" in content
        ):
            blockers.append(
                {
                    "code": "uloop_roslyn_duplicate_assembly",
                    "message": (
                        "Unity Editor log shows a duplicate assembly conflict between "
                        "Unity CLI Loop CodeAnalysis DLLs and Assets/Plugins/Roslyn."
                    ),
                }
            )
        if b"Assembly Assembly-CSharp.dll at Library/ScriptAssemblies/Assembly-CSharp.dll not valid. Loading of assembly skipped." in content:
            blockers.append(
                {
                    "code": "unity_invalid_script_assemblies",
                    "message": (
                        "Unity Editor log shows invalid script assemblies during ReloadAssembly, "
                        "which can prevent Unity CLI Loop menus and server startup from loading."
                    ),
                }
            )

    unique: list[dict[str, str]] = []
    seen: set[str] = set()
    for blocker in blockers:
        code = blocker["code"]
        if code in seen:
            continue
        seen.add(code)
        unique.append(blocker)
    return unique


def probe_cli_loop(service: UnityIntegrationService) -> dict[str, Any]:
    environment = service.environment
    command = list(environment.cli_loop_command)
    blockers = detect_cli_loop_blockers(service)
    if not command:
        return {
            "available": False,
            "spawnable": False,
            "command": [],
            "project_info_command": [],
            "returncode": None,
            "stdout": "",
            "stderr": "",
            "error": "Unity CLI Loop command is unavailable.",
            "blockers": blockers,
        }

    probe_command = [*command, "get-project-info", "--project-path", str(environment.project_root)]
    try:
        completed = subprocess.run(
            probe_command,
            cwd=environment.repo_root,
            capture_output=True,
            text=True,
            check=False,
            timeout=120,
        )
    except Exception as exc:
        return {
            "available": True,
            "spawnable": False,
            "command": command,
            "project_info_command": probe_command,
            "returncode": None,
            "stdout": "",
            "stderr": "",
            "error": f"{exc.__class__.__name__}: {exc}",
            "blockers": blockers,
        }

    return {
        "available": True,
        "spawnable": completed.returncode == 0,
        "command": command,
        "project_info_command": probe_command,
        "returncode": completed.returncode,
        "stdout": completed.stdout,
        "stderr": completed.stderr,
        "error": None if completed.returncode == 0 else (completed.stderr.strip() or completed.stdout.strip() or "Unity CLI Loop probe failed."),
        "blockers": blockers,
    }


def main() -> None:
    service = UnityIntegrationService()
    payload = service.list_capabilities()
    environment = payload["environment"]
    cli_probe = probe_cli_loop(service)
    print("Unity integration environment:")
    print(f"- Repo root: {environment['repo_root']}")
    print(f"- Project root: {environment['project_root']}")
    print(f"- Editor path: {environment['editor_path']}")
    print(f"- Unity CLI Loop package installed: {environment['cli_loop_package_installed']}")
    print(f"- Unity CLI Loop command: {' '.join(environment['cli_loop_command']) or '(missing)'}")
    print(f"- Unity CLI Loop probe command: {' '.join(cli_probe['project_info_command']) or '(missing)'}")
    print(f"- Unity CLI Loop spawnable: {cli_probe['spawnable']}")
    if cli_probe["returncode"] is not None:
        print(f"- Unity CLI Loop probe return code: {cli_probe['returncode']}")
    if cli_probe["error"]:
        print(f"- Unity CLI Loop probe error: {cli_probe['error']}")
    if cli_probe["blockers"]:
        print("- Unity CLI Loop blockers:")
        for blocker in cli_probe["blockers"]:
            print(f"  - [{blocker['code']}] {blocker['message']}")
    print(f"- Unity MCP command: {' '.join(environment['unity_mcp_command']) or '(missing)'}")
    print(f"- Capability rows: {len(payload['capabilities'])}")

    runtime = service.mcp_runtime()
    try:
        runtime.connect()
        tools = runtime.list_tools()
        resources = runtime.list_resources()
        print(f"Connected to Unity MCP with {len(tools)} tools and {len(resources)} resources.")
        print("Sample tools:", ", ".join(tools[:10]))
        print("Sample resources:", ", ".join(resources[:10]))
    except Exception as exc:
        print(f"Unity MCP connection unavailable: {exc}")
    finally:
        runtime.close()


if __name__ == "__main__":
    main()
