from __future__ import annotations

from pathlib import Path
from typing import Iterable


EXPECTED_FILES = (
    "scripts/run/run-gui-agent.ps1",
    "scripts/run/inspect-unity-profile.ps1",
    "scripts/run/run-unity-automation.ps1",
    "scripts/validate/validate-structure.ps1",
    "scripts/validate/validate-avatar-pipeline.ps1",
    "scripts/package/package-unity-client.ps1",
    "scripts/migrate/README.md",
    "tests/control-plane/README.md",
    "tests/unity-integration/README.md",
    "tests/asset-pipeline/README.md",
    "tests/structure/README.md",
    "tests/structure/test_phase7_structure.py",
)

EXPECTED_DOCS = (
    "../docs/migration/ai-dev-system-unification-phase0.md",
    "../docs/migration/ai-dev-system-unification-phase1.md",
    "../docs/migration/ai-dev-system-unification-phase2.md",
    "../docs/migration/ai-dev-system-unification-phase3.md",
    "../docs/migration/ai-dev-system-unification-phase4.md",
    "../docs/migration/ai-dev-system-unification-phase5.md",
    "../docs/migration/ai-dev-system-unification-phase6.md",
    "../docs/migration/ai-dev-system-unification-phase7.md",
)

TEXT_EXPECTATIONS = (
    ("README.md", "scripts/validate/validate-structure.ps1"),
    ("README.md", "tests/structure/"),
    ("AGENTS.md", "scripts/validate/"),
    ("AGENTS.md", "tests/structure/"),
    ("../tasks/task-queue.md", "ai-dev-system/scripts/"),
    ("../tasks/task-queue.md", "ai-dev-system/tests/"),
)

WRAPPER_EXPECTATIONS = (
    ("scripts/run/run-gui-agent.ps1", "python -m app.main"),
    ("scripts/run/inspect-unity-profile.ps1", "inspect --profile unity-editor"),
    ("scripts/run/run-unity-automation.ps1", "python run_demo.py"),
    ("scripts/validate/validate-structure.ps1", "validate-phase6-structure.ps1"),
    ("scripts/validate/validate-structure.ps1", "validate_phase7_structure.py"),
    ("scripts/validate/validate-avatar-pipeline.ps1", "AvatarValidator.cs"),
    ("scripts/package/package-unity-client.ps1", "scripts\\package_release.ps1"),
)


def collect_errors(ai_dev_root: Path) -> list[str]:
    errors: list[str] = []

    for relative_path in EXPECTED_FILES:
        if not (ai_dev_root / relative_path).exists():
            errors.append(f"Missing required Phase 7 file: {relative_path}")

    for relative_path in EXPECTED_DOCS:
        if not (ai_dev_root / relative_path).resolve().exists():
            errors.append(f"Missing migration document: {relative_path}")

    for relative_path, snippet in TEXT_EXPECTATIONS:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing text-check file: {relative_path}")
            continue

        content = path.read_text(encoding="utf-8")
        if snippet not in content:
            errors.append(f"Expected '{snippet}' in {relative_path}")

    for relative_path, snippet in WRAPPER_EXPECTATIONS:
        path = ai_dev_root / relative_path
        if not path.exists():
            continue

        content = path.read_text(encoding="utf-8")
        if snippet not in content:
            errors.append(f"Expected wrapper snippet '{snippet}' in {relative_path}")

    return errors


def format_errors(errors: Iterable[str]) -> str:
    return "\n".join(f"- {error}" for error in errors)


def main() -> int:
    ai_dev_root = Path(__file__).resolve().parents[2]
    errors = collect_errors(ai_dev_root)
    if errors:
        print("Phase 7 structure validation failed.")
        print(format_errors(errors))
        return 1

    print("Phase 7 structure validation passed.")
    print(f"Validated files: {len(EXPECTED_FILES)}")
    print(f"Validated migration docs: {len(EXPECTED_DOCS)}")
    print(f"Validated text expectations: {len(TEXT_EXPECTATIONS) + len(WRAPPER_EXPECTATIONS)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
