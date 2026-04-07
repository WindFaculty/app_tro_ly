from __future__ import annotations

import sys
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from orchestrator.catalog import load_platform_catalog


class AgentPlatformCatalogTests(unittest.TestCase):
    def test_catalog_loads_and_references_existing_files(self) -> None:
        catalog = load_platform_catalog(ROOT)

        self.assertEqual(catalog.version, 1)
        self.assertIn("unity-autonomous-loop", catalog.workflows)
        self.assertIn("planner", catalog.agents)
        self.assertIn("code-reviewer", catalog.agents)
        self.assertIn("verification-loop", catalog.skills)

        for agent in catalog.agents.values():
            self.assertTrue(catalog.resolve(agent.prompt).exists(), agent.prompt)

        for rule_pack in catalog.rule_packs.values():
            for policy_path in rule_pack.policy_paths:
                self.assertTrue(catalog.resolve(policy_path).exists(), policy_path)

    def test_workflow_references_registered_specs(self) -> None:
        catalog = load_platform_catalog(ROOT)
        workflow = catalog.require_workflow("unity-autonomous-loop")

        self.assertEqual(workflow.planner_agent, "planner")
        self.assertEqual(workflow.executor_agent, "executor")
        self.assertEqual(workflow.review_agent, "code-reviewer")
        self.assertEqual(workflow.verifier_agent, "debugger")
        self.assertGreaterEqual(workflow.max_replans, 1)
        self.assertIn("scene_smoke_check", workflow.supported_goals)


if __name__ == "__main__":
    unittest.main()
