from __future__ import annotations

from agents.contracts import PlanStep, TaskDefinition


class MeshPipelinePlanner:
    def build_plan(self, task: TaskDefinition) -> list[PlanStep]:
        goal_id = task.goal.get("id")
        if goal_id != "mesh_ai_refine":
            raise ValueError(f"Unsupported task goal: {goal_id}")

        intake_manifest = task.goal.get("intake_manifest")
        output_dir = task.goal.get("output_dir")
        if not intake_manifest:
            raise ValueError("mesh_ai_refine requires goal.intake_manifest")
        if not output_dir:
            raise ValueError("mesh_ai_refine requires goal.output_dir")

        return [
            PlanStep(
                id="resolve_intake",
                title="Resolve Mesh AI intake manifest and profile",
                kind="mesh_intake",
                payload={"intake_manifest": str(intake_manifest)},
            ),
            PlanStep(
                id="build_refinement_plan",
                title="Build deterministic Blender refinement plan",
                kind="mesh_refinement_plan",
                payload={"intake_manifest": str(intake_manifest)},
            ),
            PlanStep(
                id="materialize_bundle",
                title="Write validation and export handoff bundle",
                kind="mesh_bundle",
                payload={
                    "intake_manifest": str(intake_manifest),
                    "output_dir": str(output_dir),
                },
            ),
        ]
