from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any, Callable

AI_ROOT = Path(__file__).resolve().parents[1]
if str(AI_ROOT) not in sys.path:
    sys.path.insert(0, str(AI_ROOT))

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()

from agents.contracts import TaskDefinition
from executor.mesh_pipeline_executor import MeshPipelineExecutor
from planner.mesh_pipeline_planner import MeshPipelinePlanner
from tools.mesh_ai_pipeline import MeshAiPipelineService


class MeshAiRefineWorkflow:
    def __init__(self, ai_root: Path | None = None) -> None:
        self.ai_root = ai_root or AI_ROOT
        self.service = MeshAiPipelineService(self.ai_root)
        self.planner = MeshPipelinePlanner()
        self.executor = MeshPipelineExecutor(self.service)

    def run(
        self,
        intake_manifest: Path,
        output_dir: Path,
        *,
        sync_lifecycle_manifests: bool = False,
        execute_wrappers: bool = False,
        blender_executable: Path | None = None,
        execution_summary_path: Path | None = None,
        run_command: Callable[[list[str]], object] | None = None,
    ) -> dict[str, Any]:
        task = TaskDefinition(
            id=f"mesh-ai-refine::{intake_manifest.stem}",
            title="Mesh AI refinement bundle",
            prompt="Build a deterministic Blender refinement bundle from Mesh AI intake metadata.",
            goal={
                "id": "mesh_ai_refine",
                "intake_manifest": str(intake_manifest),
                "output_dir": str(output_dir),
            },
        )
        plan = self.planner.build_plan(task)
        records = [self.executor.execute(step) for step in plan]
        synced_manifests = None
        if sync_lifecycle_manifests:
            synced_manifests = self.service.sync_lifecycle_manifests(intake_manifest)
        wrapper_execution = None
        if execute_wrappers:
            if blender_executable is None:
                raise ValueError("execute_wrappers requires blender_executable")
            wrapper_execution = self.service.execute_wrapper_pass(
                intake_manifest,
                blender_executable=blender_executable,
                execution_summary_path=execution_summary_path,
                run_command=run_command,
            )

        summary = {
            "task_id": task.id,
            "intake_manifest": str(intake_manifest),
            "output_dir": str(output_dir),
            "plan": [
                {
                    "id": step.id,
                    "title": step.title,
                    "kind": step.kind,
                    "payload": dict(step.payload),
                }
                for step in plan
            ],
            "records": [
                {
                    "step_id": record.step_id,
                    "status": record.status,
                    "details": dict(record.details),
                }
                for record in records
            ],
        }
        if synced_manifests is not None:
            summary["synced_manifests"] = synced_manifests
        if wrapper_execution is not None:
            summary["wrapper_execution"] = wrapper_execution
        return summary


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Materialize the Mesh AI -> Blender refinement bundle.")
    parser.add_argument(
        "--intake-manifest",
        required=True,
        help="Path to the intake manifest JSON file.",
    )
    parser.add_argument(
        "--output-dir",
        required=True,
        help="Directory where the generated refinement plan, validation report, and handoff manifest should be written.",
    )
    parser.add_argument(
        "--sync-lifecycle-manifests",
        action="store_true",
        help="Also write the repo-style refinement, validation, and handoff manifests next to the intake manifest.",
    )
    parser.add_argument(
        "--execute-wrappers",
        action="store_true",
        help="Run the Blender wrapper steps with the provided Blender executable and write an execution summary.",
    )
    parser.add_argument(
        "--blender-executable",
        help="Path to blender.exe used when --execute-wrappers is enabled.",
    )
    parser.add_argument(
        "--execution-summary-path",
        help="Optional output path for the execution summary JSON. Defaults beside the intake manifest.",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    workflow = MeshAiRefineWorkflow()
    blender_executable = Path(args.blender_executable) if args.blender_executable else None
    execution_summary_path = Path(args.execution_summary_path) if args.execution_summary_path else None
    summary = workflow.run(
        Path(args.intake_manifest),
        Path(args.output_dir),
        sync_lifecycle_manifests=bool(args.sync_lifecycle_manifests),
        execute_wrappers=bool(args.execute_wrappers),
        blender_executable=blender_executable,
        execution_summary_path=execution_summary_path,
    )
    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
