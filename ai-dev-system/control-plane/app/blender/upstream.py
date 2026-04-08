from __future__ import annotations

import json
from functools import lru_cache
from pathlib import Path
from typing import Any


@lru_cache(maxsize=1)
def load_blender_upstream_metadata() -> dict[str, Any]:
    path = Path(__file__).with_name("upstream.json")
    return json.loads(path.read_text(encoding="utf-8"))
