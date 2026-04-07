from __future__ import annotations

import sys
from pathlib import Path

AI_ROOT = Path(__file__).resolve().parents[2]
if str(AI_ROOT) not in sys.path:
    sys.path.insert(0, str(AI_ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from adapters.surfaces import generate_surfaces


def main() -> int:
    repo_root = Path(__file__).resolve().parents[3]
    generated = generate_surfaces(repo_root)
    drifted: list[str] = []
    for path, content in generated.items():
        if not path.exists() or path.read_text(encoding="utf-8") != content:
            drifted.append(str(path.relative_to(repo_root)))

    if drifted:
        print("Agent-platform surface drift detected:")
        for item in drifted:
            print(f"- {item}")
        return 1

    print("Agent-platform surfaces validated.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
