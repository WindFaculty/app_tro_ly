from __future__ import annotations

import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
REPO_ROOT = ROOT.parent
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from adapters.surfaces import generate_surfaces


class AgentPlatformSurfaceTests(unittest.TestCase):
    def test_generated_surfaces_match_repo_files(self) -> None:
        generated = generate_surfaces(REPO_ROOT)

        self.assertGreaterEqual(len(generated), 10)
        for path, content in generated.items():
            with self.subTest(path=path):
                self.assertTrue(path.exists(), f"Missing generated surface: {path}")
                self.assertEqual(path.read_text(encoding="utf-8"), content)


if __name__ == "__main__":
    unittest.main()
