from __future__ import annotations

import sys
from pathlib import Path


AI_ROOT = Path(__file__).resolve().parents[2]
if str(AI_ROOT) not in sys.path:
    sys.path.insert(0, str(AI_ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from unity_integration.capability_catalog import UnityCapabilityCatalog
from unity_integration.environment import UnityIntegrationEnvironment
from verify_unity_integration import detect_cli_loop_blockers, probe_cli_loop
from unity_integration.service import UnityIntegrationService


REQUIRED_DOCS = (
    "../docs/architecture/unity-open-source-reference-analysis.md",
    "../docs/architecture/unity-integration-gap-analysis.md",
    "../docs/architecture/unity-automation-architecture.md",
)

REQUIRED_FILES = (
    "control-plane/unity_integration/contracts.py",
    "control-plane/unity_integration/capability_catalog.py",
    "control-plane/unity_integration/environment.py",
    "control-plane/unity_integration/backends/cli_loop.py",
    "control-plane/unity_integration/backends/mcp_unity.py",
    "control-plane/unity_integration/backends/gui_fallback.py",
)

REQUIRED_CAPABILITIES = (
    "editor.compile",
    "editor.logs.read",
    "editor.tests.editmode",
    "editor.tests.playmode",
    "scene.inspect",
    "gameobject.create",
    "component.add",
    "prefab.inspect",
    "editor.menu.execute",
)


def collect_errors(ai_root: Path) -> list[str]:
    errors: list[str] = []

    for relative_path in REQUIRED_DOCS:
        path = (ai_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing Unity integration doc: {relative_path}")

    for relative_path in REQUIRED_FILES:
        path = (ai_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing Unity integration source file: {relative_path}")

    for capability in REQUIRED_CAPABILITIES:
        try:
            UnityCapabilityCatalog.get(capability)
        except ValueError:
            errors.append(f"Missing required Unity capability: {capability}")

    environment = UnityIntegrationEnvironment(repo_root=ai_root.parent).probe()
    if environment.project_root.name != "unity-runtime":
        errors.append(f"Unexpected Unity project root: {environment.project_root}")
    if environment.package_manifest_path is None:
        errors.append("Unity package manifest path was not detected.")
    if not environment.cli_loop_package_installed:
        errors.append("Unity CLI Loop package reference is missing from the active Unity manifest.")

    cli_probe = probe_cli_loop(UnityIntegrationService(environment=environment))
    if not cli_probe["available"]:
        errors.append("Unity CLI Loop command is unavailable on this machine.")
    elif not cli_probe["spawnable"]:
        probe_error = cli_probe["error"] or f"return code {cli_probe['returncode']}"
        errors.append(
            "Unity CLI Loop probe failed: "
            f"{probe_error}"
        )
    for blocker in detect_cli_loop_blockers(UnityIntegrationService(environment=environment)):
        errors.append(f"Unity CLI Loop blocker detected: [{blocker['code']}] {blocker['message']}")

    return errors


def main() -> int:
    errors = collect_errors(AI_ROOT)
    if errors:
        print("Unity integration validation failed.")
        for item in errors:
            print(f"- {item}")
        return 1

    print("Unity integration validation passed.")
    print(f"Capability count: {len(UnityCapabilityCatalog.names())}")
    environment = UnityIntegrationEnvironment(repo_root=AI_ROOT.parent).probe()
    print(f"Project root: {environment.project_root}")
    print(f"CLI Loop package installed: {environment.cli_loop_package_installed}")
    cli_probe = probe_cli_loop(UnityIntegrationService(environment=environment))
    print(f"CLI Loop command: {' '.join(cli_probe['command']) or '(missing)'}")
    print(f"CLI Loop spawnable: {cli_probe['spawnable']}")
    if cli_probe["blockers"]:
        print("CLI Loop blockers:")
        for blocker in cli_probe["blockers"]:
            print(f"- [{blocker['code']}] {blocker['message']}")
    print(f"Unity MCP available: {environment.unity_mcp_available}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
