from __future__ import annotations

import json
import sys
import tempfile
import unittest
from types import SimpleNamespace
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.append(str(ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from agents.contracts import TaskDefinition
from executor.mesh_pipeline_executor import MeshPipelineExecutor
from planner.mesh_pipeline_planner import MeshPipelinePlanner
from tools.mesh_ai_pipeline import MeshAiPipelineService
from workflows.mesh_ai_refine import MeshAiRefineWorkflow


class MeshAiPipelineTests(unittest.TestCase):
    def setUp(self) -> None:
        self.service = MeshAiPipelineService(ROOT)
        self.sample_intake = ROOT / "asset-pipeline" / "manifests" / "mesh-ai-azure-sakura-intake.json"

    def test_resolves_profile_from_manifest(self) -> None:
        manifest = self.service.load_intake_manifest(self.sample_intake)
        self.assertEqual("unity_avatar_clothing_v1", self.service.resolve_profile_id(manifest))

    def test_build_refinement_plan_maps_existing_root_tools(self) -> None:
        plan = self.service.build_refinement_plan(self.sample_intake)
        step_scripts = {
            step["script_path"]
            for step in plan["toolchain_steps"]
            if step["step_type"] == "blender_script"
        }
        self.assertIn("tools/blender_asset_audit.py", step_scripts)
        self.assertIn("tools/blender_fit_dress_scene.py", step_scripts)
        self.assertIn("tools/blender_verify_dress_scene.py", step_scripts)
        self.assertEqual("ai-dev-system/workflows/mesh-ai-refine-avatar-clothing.yaml", plan["workflow_spec_path"])

    def test_planner_builds_expected_mesh_pipeline_steps(self) -> None:
        planner = MeshPipelinePlanner()
        task = TaskDefinition(
            id="mesh-ai",
            title="Mesh AI",
            prompt="refine asset",
            goal={
                "id": "mesh_ai_refine",
                "intake_manifest": str(self.sample_intake),
                "output_dir": "tmp",
            },
        )
        steps = planner.build_plan(task)
        self.assertEqual(["mesh_intake", "mesh_refinement_plan", "mesh_bundle"], [step.kind for step in steps])

    def test_executor_materializes_bundle(self) -> None:
        executor = MeshPipelineExecutor(self.service)
        with tempfile.TemporaryDirectory() as temp_dir:
            result = executor.execute(
                planner_step(
                    kind="mesh_bundle",
                    intake_manifest=self.sample_intake,
                    output_dir=temp_dir,
                )
            )

            self.assertEqual("completed", result.status)
            written_files = result.details["written_files"]
            for path in written_files.values():
                self.assertTrue(Path(path).exists(), path)

            refinement_plan = json.loads(Path(written_files["refinement_plan"]).read_text(encoding="utf-8"))
            validation_report = json.loads(Path(written_files["validation_report"]).read_text(encoding="utf-8"))
            export_handoff = json.loads(Path(written_files["export_handoff_manifest"]).read_text(encoding="utf-8"))

            self.assertEqual("cleaned", refinement_plan["status"])
            self.assertEqual("validated", validation_report["status"])
            self.assertEqual("export-ready", export_handoff["status"])
            self.assertEqual("export-ready", validation_report["artifact_ingestion"]["highest_completed_stage"])
            self.assertEqual(5, validation_report["artifact_ingestion"]["completed_step_count"])
            self.assertGreaterEqual(len(validation_report["checks"]), 8)
            self.assertEqual(
                "tools/reports/azure_sakura_dress_polishfit_verify_report.json",
                export_handoff["artifact_ingestion"]["steps"][3]["output_evidence"]["report_path"],
            )

    def test_sync_lifecycle_manifests_updates_repo_style_bundle(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            manifests_dir = temp_root / "asset-pipeline" / "manifests"
            manifests_dir.mkdir(parents=True)
            intake_path = manifests_dir / self.sample_intake.name
            intake_path.write_text(self.sample_intake.read_text(encoding="utf-8"), encoding="utf-8")

            service = MeshAiPipelineService(ROOT)
            result = service.sync_lifecycle_manifests(intake_path)

            self.assertEqual("mesh-ai-azure-sakura", result["bundle_name"])
            synced_files = result["written_files"]
            self.assertTrue(Path(synced_files["refinement_plan"]).exists())
            self.assertTrue(Path(synced_files["validation_report"]).exists())
            self.assertTrue(Path(synced_files["export_handoff_manifest"]).exists())

            synced_validation_report = json.loads(
                Path(synced_files["validation_report"]).read_text(encoding="utf-8")
            )
            self.assertEqual("export-ready", synced_validation_report["artifact_ingestion"]["highest_completed_stage"])
            self.assertEqual(
                "tools/renders/azure_sakura_polishfit/current.png",
                synced_validation_report["artifact_ingestion"]["steps"][4]["output_evidence"]["preview_render_path"],
            )

    def test_workflow_runner_returns_summary(self) -> None:
        workflow = MeshAiRefineWorkflow(ROOT)
        with tempfile.TemporaryDirectory() as temp_dir:
            summary = workflow.run(self.sample_intake, Path(temp_dir))

        self.assertEqual(3, len(summary["plan"]))
        self.assertEqual(3, len(summary["records"]))
        self.assertEqual("mesh-ai-refine::mesh-ai-azure-sakura-intake", summary["task_id"])

    def test_workflow_runner_can_sync_lifecycle_manifests(self) -> None:
        workflow = MeshAiRefineWorkflow(ROOT)
        with tempfile.TemporaryDirectory() as temp_dir:
            temp_root = Path(temp_dir)
            manifests_dir = temp_root / "asset-pipeline" / "manifests"
            manifests_dir.mkdir(parents=True)
            intake_path = manifests_dir / self.sample_intake.name
            intake_path.write_text(self.sample_intake.read_text(encoding="utf-8"), encoding="utf-8")

            summary = workflow.run(
                intake_path,
                temp_root / "bundle-output",
                sync_lifecycle_manifests=True,
            )

        self.assertIn("synced_manifests", summary)
        self.assertEqual("mesh-ai-azure-sakura", summary["synced_manifests"]["bundle_name"])

    def test_execute_wrapper_pass_writes_execution_summary(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            summary_path = Path(temp_dir) / "mesh-ai-execution.json"

            def fake_runner(command: list[str]) -> object:
                return SimpleNamespace(
                    returncode=0,
                    stdout=f"ran {' '.join(command[:4])}",
                    stderr="",
                )

            summary = self.service.execute_wrapper_pass(
                self.sample_intake,
                blender_executable=Path(r"C:\Blender\blender.exe"),
                execution_summary_path=summary_path,
                run_command=fake_runner,
            )

            self.assertEqual(5, len(summary["steps"]))
            self.assertEqual(0, summary["steps"][-1]["return_code"])
            self.assertEqual(r"C:\Blender\blender.exe", summary["steps"][0]["command"][0])
            self.assertEqual("tools/reports/azure_kimono_asset_audit.json", summary["steps"][0]["output_evidence"]["report_path"])
            self.assertTrue(summary_path.exists())

    def test_workflow_runner_can_execute_wrappers(self) -> None:
        workflow = MeshAiRefineWorkflow(ROOT)

        def fake_runner(command: list[str]) -> object:
            return SimpleNamespace(returncode=0, stdout="ok", stderr="")

        with tempfile.TemporaryDirectory() as temp_dir:
            summary = workflow.run(
                self.sample_intake,
                Path(temp_dir),
                execute_wrappers=True,
                blender_executable=Path(r"C:\Blender\blender.exe"),
                execution_summary_path=Path(temp_dir) / "execution.json",
                run_command=fake_runner,
            )

        self.assertIn("wrapper_execution", summary)
        self.assertEqual(5, len(summary["wrapper_execution"]["steps"]))


def planner_step(*, kind: str, intake_manifest: Path, output_dir: str) -> object:
    return type(
        "Step",
        (),
        {
            "id": "materialize_bundle",
            "kind": kind,
            "payload": {
                "intake_manifest": str(intake_manifest),
                "output_dir": output_dir,
            },
        },
    )()


if __name__ == "__main__":
    unittest.main()
