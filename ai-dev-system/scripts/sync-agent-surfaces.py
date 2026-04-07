from __future__ import annotations

import argparse
import sys
from pathlib import Path

AI_ROOT = Path(__file__).resolve().parents[1]
if str(AI_ROOT) not in sys.path:
    sys.path.insert(0, str(AI_ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from adapters.surfaces import generate_surfaces


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate Codex and Antigravity harness surfaces from the canonical catalog.")
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]), help="Repository root path.")
    parser.add_argument("--check", action="store_true", help="Only check for drift; do not write files.")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    repo_root = Path(args.repo_root).resolve()
    generated = generate_surfaces(repo_root)

    drifted: list[str] = []
    for path, content in generated.items():
        if not path.exists() or path.read_text(encoding="utf-8") != content:
            drifted.append(str(path.relative_to(repo_root)))
            if not args.check:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text(content, encoding="utf-8")

    if args.check:
        if drifted:
            print("Agent-platform surface drift detected:")
            for item in drifted:
                print(f"- {item}")
            return 1
        print("Agent-platform surfaces are in sync.")
        return 0

    if drifted:
        print("Updated agent-platform surfaces:")
        for item in drifted:
            print(f"- {item}")
    else:
        print("Agent-platform surfaces were already up to date.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
