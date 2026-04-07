from __future__ import annotations

from pathlib import Path
from typing import Iterable


EXPECTED_FILES = (
    "bootstrap_control_plane.py",
    "sitecustomize.py",
    "scripts/validate/validate-architecture-lock.ps1",
    "scripts/validate/validate_phase9_architecture_lock.py",
    "tests/structure/test_phase9_architecture_lock.py",
    "../docs/migration/ai-dev-system-unification-phase9.md",
)

REMOVED_SHIM_PATHS = (
    "app",
    "agents",
    "planner",
    "executor",
    "memory",
    "tools",
    "mcp_client.py",
)

TEXT_EXPECTATIONS = (
    ("README.md", "Phase 9 removed the temporary root-level shim packages"),
    ("README.md", "docs/migration/ai-dev-system-unification-phase9.md"),
    ("AGENTS.md", "bootstrap_control_plane.py"),
    ("AGENTS.md", "sitecustomize.py"),
    ("scripts/README.md", "validate/validate-architecture-lock.ps1"),
    ("tests/README.md", "test_phase9_architecture_lock.py"),
    ("tests/structure/README.md", "test_phase9_architecture_lock.py"),
    ("tests/structure/README.md", "validate_phase9_architecture_lock.py"),
    ("scripts/run/run-gui-agent.ps1", "PYTHONPATH"),
    ("scripts/run/run-gui-agent.ps1", "control-plane"),
    ("scripts/run/inspect-unity-profile.ps1", "PYTHONPATH"),
    ("scripts/run/inspect-unity-profile.ps1", "control-plane"),
    ("scripts/validate/validate-structure.ps1", "validate_phase9_architecture_lock.py"),
    ("../docs/index.md", "migration/ai-dev-system-unification-phase9.md"),
    ("../docs/roadmap.md", "ai-dev-system-unification-phase9.md"),
    ("../docs/migration/ai-dev-system-unification.md", "ai-dev-system-unification-phase9.md"),
    ("../tasks/task-queue.md", "Phase 9 shim removal"),
    ("../tasks/ai-dev-system-unification-backlog.md", "U09 | DONE"),
    ("../tasks/done.md", "D049: Completed the `ai-dev-system` unification Phase 9 architecture-lock pass"),
    ("../scripts/assistant_common.ps1", "ai-dev-system\\clients\\unity-client"),
)

ABSENCE_EXPECTATIONS = (
    ("../tasks/task-queue.md", "root shim packages remain temporary compatibility paths"),
    ("../scripts/assistant_common.ps1", 'Join-Path $Root "unity-client"'),
)


def collect_errors(ai_dev_root: Path) -> list[str]:
    errors: list[str] = []

    for relative_path in EXPECTED_FILES:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing required Phase 9 file: {relative_path}")

    for relative_path in REMOVED_SHIM_PATHS:
        path = ai_dev_root / relative_path
        if path.exists():
            errors.append(f"Removed shim path returned unexpectedly: {relative_path}")

    for relative_path, snippet in TEXT_EXPECTATIONS:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing text-check file: {relative_path}")
            continue

        content = path.read_text(encoding="utf-8")
        if snippet not in content:
            errors.append(f"Expected '{snippet}' in {relative_path}")

    for relative_path, snippet in ABSENCE_EXPECTATIONS:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            continue

        content = path.read_text(encoding="utf-8")
        if snippet in content:
            errors.append(f"Did not expect '{snippet}' in {relative_path}")

    return errors


def format_errors(errors: Iterable[str]) -> str:
    return "\n".join(f"- {error}" for error in errors)


def main() -> int:
    ai_dev_root = Path(__file__).resolve().parents[2]
    errors = collect_errors(ai_dev_root)
    if errors:
        print("Phase 9 architecture-lock validation failed.")
        print(format_errors(errors))
        return 1

    print("Phase 9 architecture-lock validation passed.")
    print(f"Validated files: {len(EXPECTED_FILES)}")
    print(f"Validated removed shim paths: {len(REMOVED_SHIM_PATHS)}")
    print(f"Validated text expectations: {len(TEXT_EXPECTATIONS)}")
    print(f"Validated absence expectations: {len(ABSENCE_EXPECTATIONS)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
