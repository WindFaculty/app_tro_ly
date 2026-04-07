from __future__ import annotations

from pathlib import Path
from typing import Iterable


EXPECTED_FILES = (
    "../docs/architecture/non-backend-integration.md",
    "../docs/migration/ai-dev-system-unification.md",
    "../docs/migration/ai-dev-system-unification-phase8.md",
    "../tasks/ai-dev-system-unification-backlog.md",
)

TEXT_EXPECTATIONS = (
    ("../docs/index.md", "architecture/non-backend-integration.md"),
    ("../docs/index.md", "migration/ai-dev-system-unification.md"),
    ("../docs/index.md", "migration/ai-dev-system-unification-phase8.md"),
    ("../docs/roadmap.md", "ai-dev-system/control-plane/"),
    ("../docs/roadmap.md", "ai-dev-system/domain/"),
    ("../docs/roadmap.md", "tasks/ai-dev-system-unification-backlog.md"),
    ("../tasks/task-queue.md", "tasks/ai-dev-system-unification-backlog.md"),
    ("../tasks/module-migration-backlog.md", "tasks/ai-dev-system-unification-backlog.md"),
)

BACKLOG_LANES = (
    "Control Plane",
    "Unity Client",
    "Avatar + Customization",
    "Asset Pipeline",
    "Governance + Validation",
)

ABSENCE_EXPECTATIONS = (
    ("../docs/roadmap.md", "unification baseline is planned-work guidance only"),
    ("../tasks/ai-dev-system-unification-backlog.md", "repo root `unity-client/`"),
    ("../tasks/ai-dev-system-unification-backlog.md", "Governance/Cross-cutting"),
    ("../tasks/task-queue.md", "`A38 |"),
    ("../tasks/task-queue.md", "`A40 |"),
)


def collect_errors(ai_dev_root: Path) -> list[str]:
    errors: list[str] = []

    for relative_path in EXPECTED_FILES:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing required Phase 8 file: {relative_path}")

    for relative_path, snippet in TEXT_EXPECTATIONS:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing text-check file: {relative_path}")
            continue

        content = path.read_text(encoding="utf-8")
        if snippet not in content:
            errors.append(f"Expected '{snippet}' in {relative_path}")

    backlog_path = (ai_dev_root / "../tasks/ai-dev-system-unification-backlog.md").resolve()
    if backlog_path.exists():
        backlog_content = backlog_path.read_text(encoding="utf-8")
        for lane in BACKLOG_LANES:
            if lane not in backlog_content:
                errors.append(f"Expected lane '{lane}' in tasks/ai-dev-system-unification-backlog.md")

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
        print("Unification docs/tasks validation failed.")
        print(format_errors(errors))
        return 1

    print("Unification docs/tasks validation passed.")
    print(f"Validated files: {len(EXPECTED_FILES)}")
    print(f"Validated text expectations: {len(TEXT_EXPECTATIONS)}")
    print(f"Validated lane expectations: {len(BACKLOG_LANES)}")
    print(f"Validated absence expectations: {len(ABSENCE_EXPECTATIONS)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
