from __future__ import annotations

from pathlib import Path

from agents.contracts import ExecutionRecord, PlanStep
from tools.mesh_ai_pipeline import MeshAiPipelineService


class MeshPipelineExecutor:
    def __init__(self, service: MeshAiPipelineService) -> None:
        self.service = service

    def execute(self, step: PlanStep) -> ExecutionRecord:
        intake_manifest = Path(str(step.payload["intake_manifest"]))

        if step.kind == "mesh_intake":
            manifest = self.service.load_intake_manifest(intake_manifest)
            profile_id = self.service.resolve_profile_id(manifest)
            return ExecutionRecord(
                step_id=step.id,
                status="completed",
                details={
                    "asset_id": manifest["asset_id"],
                    "profile_id": profile_id,
                    "status": manifest["status"],
                },
            )

        if step.kind == "mesh_refinement_plan":
            refinement_plan = self.service.build_refinement_plan(intake_manifest)
            return ExecutionRecord(
                step_id=step.id,
                status="completed",
                details={
                    "profile_id": refinement_plan["profile_id"],
                    "workflow_spec_path": refinement_plan["workflow_spec_path"],
                    "step_count": len(refinement_plan["toolchain_steps"]),
                    "toolchain_steps": refinement_plan["toolchain_steps"],
                },
            )

        if step.kind == "mesh_bundle":
            result = self.service.materialize_bundle(
                intake_manifest,
                Path(str(step.payload["output_dir"])),
            )
            return ExecutionRecord(
                step_id=step.id,
                status="completed",
                details=result,
            )

        raise ValueError(f"Unsupported step kind: {step.kind}")
