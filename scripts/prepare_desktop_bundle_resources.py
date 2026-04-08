from __future__ import annotations

import shutil
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent
STAGE_ROOT = REPO_ROOT / "apps" / "desktop-shell" / ".tauri-bundle-resources"


def expect_exists(path: Path, description: str) -> None:
    if not path.exists():
        raise FileNotFoundError(f"Missing {description}: {path}")


def copy_tree(source: Path, destination: Path) -> None:
    expect_exists(source, "bundle source directory")
    shutil.copytree(source, destination, dirs_exist_ok=True)


def copy_file(source: Path, destination: Path) -> None:
    expect_exists(source, "bundle source file")
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, destination)


def prepare_backend_runtime() -> None:
    backend_source = REPO_ROOT / "local-backend"
    backend_target = STAGE_ROOT / "local-backend"

    copy_tree(backend_source / "app", backend_target / "app")
    copy_tree(backend_source / "config", backend_target / "config")
    copy_tree(backend_source / ".venv", backend_target / ".venv")
    copy_file(backend_source / "run_local.py", backend_target / "run_local.py")
    copy_file(backend_source / "requirements.txt", backend_target / "requirements.txt")


def prepare_customization_contracts() -> None:
    customization_source = REPO_ROOT / "ai-dev-system" / "domain" / "customization"
    customization_target = STAGE_ROOT / "ai-dev-system" / "domain" / "customization"

    copy_tree(customization_source / "contracts", customization_target / "contracts")
    copy_tree(customization_source / "sample-data", customization_target / "sample-data")


def main() -> int:
    if STAGE_ROOT.exists():
        shutil.rmtree(STAGE_ROOT)
    STAGE_ROOT.mkdir(parents=True, exist_ok=True)

    prepare_backend_runtime()
    prepare_customization_contracts()

    print("Prepared desktop bundle resources.")
    print(f"- stage root: {STAGE_ROOT}")
    print("- staged backend runtime without local data or tests")
    print("- staged customization contracts and sample data for wardrobe packaging")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # pragma: no cover - script entrypoint
        print(f"Desktop bundle resource preparation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
