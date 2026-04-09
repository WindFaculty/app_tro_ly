from __future__ import annotations

import sys
import unittest
from pathlib import Path


AI_DEV_ROOT = Path(__file__).resolve().parents[2]
VALIDATOR_ROOT = AI_DEV_ROOT / "scripts" / "validate"
if str(VALIDATOR_ROOT) not in sys.path:
    sys.path.insert(0, str(VALIDATOR_ROOT))

from validate_mesh_ai_pipeline import collect_errors  # noqa: E402


class MeshAiPipelineStructureTests(unittest.TestCase):
    def test_mesh_ai_pipeline_structure_is_consistent(self) -> None:
        errors = collect_errors(AI_DEV_ROOT)
        self.assertEqual([], errors, "\n".join(errors))


if __name__ == "__main__":
    unittest.main()
