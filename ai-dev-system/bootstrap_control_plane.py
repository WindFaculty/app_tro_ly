from __future__ import annotations

import sys
from pathlib import Path


def bootstrap_control_plane_path() -> Path:
    root = Path(__file__).resolve().parent
    control_plane_root = root / "control-plane"
    control_plane_path = str(control_plane_root)

    if control_plane_path not in sys.path:
        sys.path.insert(0, control_plane_path)

    return control_plane_root
