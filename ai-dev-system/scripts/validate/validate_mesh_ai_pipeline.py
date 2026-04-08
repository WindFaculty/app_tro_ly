from __future__ import annotations

import json
from pathlib import Path
from typing import Any, Iterable


EXPECTED_FILES = (
    "../docs/architecture/mesh-ai-blender-unity-integration.md",
    "asset-pipeline/profiles/unity_prop_v1.json",
    "asset-pipeline/profiles/unity_room_item_v1.json",
    "asset-pipeline/profiles/unity_avatar_accessory_v1.json",
    "asset-pipeline/profiles/unity_avatar_clothing_v1.json",
    "asset-pipeline/schemas/intake-manifest.schema.json",
    "asset-pipeline/schemas/refinement-plan.schema.json",
    "asset-pipeline/schemas/validation-report.schema.json",
    "asset-pipeline/schemas/export-handoff-manifest.schema.json",
    "asset-pipeline/toolchain-map.json",
    "asset-pipeline/tool-mapping.md",
    "asset-pipeline/manifests/mesh-ai-azure-sakura-intake.json",
    "asset-pipeline/manifests/mesh-ai-azure-sakura-refinement-plan.json",
    "asset-pipeline/manifests/mesh-ai-azure-sakura-validation-report.json",
    "asset-pipeline/manifests/mesh-ai-azure-sakura-handoff.json",
    "asset-pipeline/manifests/mesh-ai-azure-sakura-execution-pass.json",
    "workbench/imports/mesh-ai-intake.md",
    "control-plane/tools/mesh_ai_blender_wrappers.py",
    "control-plane/tools/mesh_ai_pipeline.py",
    "control-plane/planner/mesh_pipeline_planner.py",
    "control-plane/executor/mesh_pipeline_executor.py",
    "workflows/mesh_ai_refine.py",
    "workflows/mesh-ai-refine-prop.yaml",
    "workflows/mesh-ai-refine-room-item.yaml",
    "workflows/mesh-ai-refine-avatar-accessory.yaml",
    "workflows/mesh-ai-refine-avatar-clothing.yaml",
    "scripts/validate/validate_mesh_ai_pipeline.py",
    "scripts/validate/validate-mesh-ai-pipeline.ps1",
    "tests/test_mesh_ai_pipeline.py",
    "tests/structure/test_mesh_ai_pipeline_structure.py",
)

PROFILE_IDS = (
    "unity_prop_v1",
    "unity_room_item_v1",
    "unity_avatar_accessory_v1",
    "unity_avatar_clothing_v1",
)

WORKFLOW_EXPECTATIONS = {
    "workflows/mesh-ai-refine-prop.yaml": "unity_prop_v1",
    "workflows/mesh-ai-refine-room-item.yaml": "unity_room_item_v1",
    "workflows/mesh-ai-refine-avatar-accessory.yaml": "unity_avatar_accessory_v1",
    "workflows/mesh-ai-refine-avatar-clothing.yaml": "unity_avatar_clothing_v1",
}

TEXT_EXPECTATIONS = (
    ("README.md", "Mesh AI"),
    ("README.md", "asset-pipeline/"),
    ("AGENTS.md", "mesh_ai_pipeline.py"),
    ("AGENTS.md", "toolchain-map.json"),
    ("asset-pipeline/README.md", "toolchain-map.json"),
    ("asset-pipeline/README.md", "root `tools/`"),
    ("workbench/README.md", "mesh-ai-intake.md"),
    ("domain/customization/README.md", "asset handoff"),
    ("domain/room/README.md", "room item"),
    ("domain/shared/README.md", "asset handoff"),
    ("scripts/README.md", "validate-mesh-ai-pipeline.ps1"),
    ("tests/README.md", "test_mesh_ai_pipeline.py"),
    ("tests/asset-pipeline/README.md", "validate-mesh-ai-pipeline.ps1"),
    ("tests/structure/README.md", "test_mesh_ai_pipeline_structure.py"),
    ("../tasks/task-queue.md", "Mesh AI"),
    ("../tasks/task-people.md", "Mesh AI"),
)


def collect_errors(ai_dev_root: Path) -> list[str]:
    errors: list[str] = []

    for relative_path in EXPECTED_FILES:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing Mesh AI pipeline file: {relative_path}")

    toolchain_path = ai_dev_root / "asset-pipeline" / "toolchain-map.json"
    if toolchain_path.exists():
        toolchain_map = json.loads(toolchain_path.read_text(encoding="utf-8"))
        profiles = (toolchain_map.get("profiles") or {})
        for profile_id in PROFILE_IDS:
            if profile_id not in profiles:
                errors.append(f"Toolchain map missing profile '{profile_id}'")
                continue

            steps = profiles[profile_id].get("steps") or []
            if not steps:
                errors.append(f"Profile '{profile_id}' has no toolchain steps")

            for step in steps:
                if step.get("step_type") == "manual_gate":
                    continue
                script_path = step.get("script_path")
                if not script_path:
                    errors.append(f"Profile '{profile_id}' step '{step.get('step_id')}' is missing script_path")
                    continue
                resolved = (ai_dev_root.parent / str(script_path)).resolve()
                if not resolved.exists():
                    errors.append(f"Missing root Blender helper for '{profile_id}': {script_path}")

    for workflow_path, expected_profile_id in WORKFLOW_EXPECTATIONS.items():
        path = ai_dev_root / workflow_path
        if not path.exists():
            continue
        payload = json.loads(path.read_text(encoding="utf-8"))
        if payload.get("profile_id") != expected_profile_id:
            errors.append(f"Workflow '{workflow_path}' should target profile '{expected_profile_id}'")
        for referenced_path in (
            payload.get("entrypoint"),
            payload.get("planner"),
            payload.get("executor"),
            payload.get("asset_pipeline_schema"),
            payload.get("toolchain_map"),
        ):
            if not referenced_path:
                errors.append(f"Workflow '{workflow_path}' is missing a required reference")
                continue
            resolved = (ai_dev_root / referenced_path).resolve()
            if not resolved.exists():
                errors.append(f"Workflow '{workflow_path}' references a missing path: {referenced_path}")

    sample_expectations = {
        "mesh-ai-azure-sakura-intake.json": {
            "expected_status": "raw",
            "required_fields": {
                "asset_id",
                "asset_name",
                "asset_type",
                "category",
                "source",
                "source_path",
                "quality_estimate",
                "status",
                "profile_id",
                "target_runtime",
                "target_unity_path",
                "toolchain_steps",
                "validation_report_path",
                "preview_render_path",
                "notes",
                "manual_gates",
            },
        },
        "mesh-ai-azure-sakura-refinement-plan.json": {
            "expected_status": "cleaned",
            "required_fields": {
                "asset_id",
                "asset_name",
                "asset_type",
                "category",
                "source",
                "source_path",
                "quality_estimate",
                "status",
                "profile_id",
                "target_runtime",
                "target_unity_path",
                "toolchain_steps",
                "validation_report_path",
                "preview_render_path",
                "artifact_ingestion",
                "notes",
                "manual_gates",
            },
        },
        "mesh-ai-azure-sakura-validation-report.json": {
            "expected_status": "validated",
            "required_fields": {
                "asset_id",
                "asset_name",
                "asset_type",
                "category",
                "source",
                "source_path",
                "quality_estimate",
                "status",
                "profile_id",
                "target_runtime",
                "target_unity_path",
                "toolchain_steps",
                "validation_report_path",
                "preview_render_path",
                "checks",
                "artifact_ingestion",
                "notes",
                "manual_gates",
            },
        },
        "mesh-ai-azure-sakura-handoff.json": {
            "expected_status": "export-ready",
            "required_fields": {
                "asset_id",
                "asset_name",
                "asset_type",
                "category",
                "source",
                "source_path",
                "quality_estimate",
                "status",
                "profile_id",
                "target_runtime",
                "target_unity_path",
                "toolchain_steps",
                "validation_report_path",
                "preview_render_path",
                "handoff_source_path",
                "artifact_ingestion",
                "notes",
                "manual_gates",
            },
        },
    }
    sample_paths = tuple(
        ai_dev_root / "asset-pipeline" / "manifests" / filename for filename in sample_expectations
    )
    baseline_required_fields = {
        "asset_id",
        "asset_name",
        "asset_type",
        "category",
        "source",
        "source_path",
        "quality_estimate",
        "status",
        "profile_id",
        "target_runtime",
        "target_unity_path",
        "toolchain_steps",
        "validation_report_path",
        "preview_render_path",
        "notes",
        "manual_gates",
    }
    for sample_path in sample_paths:
        if not sample_path.exists():
            continue
        payload = json.loads(sample_path.read_text(encoding="utf-8"))
        expectation = sample_expectations[sample_path.name]
        required_fields = baseline_required_fields | set(expectation["required_fields"])
        missing_fields = sorted(required_fields - payload.keys())
        if missing_fields:
            errors.append(f"Manifest '{sample_path.name}' is missing fields: {', '.join(missing_fields)}")
        if payload.get("status") != expectation["expected_status"]:
            errors.append(
                f"Manifest '{sample_path.name}' should have status '{expectation['expected_status']}', "
                f"found '{payload.get('status')}'"
            )
        artifact_ingestion = payload.get("artifact_ingestion")
        if sample_path.name != "mesh-ai-azure-sakura-intake.json":
            if not isinstance(artifact_ingestion, dict):
                errors.append(f"Manifest '{sample_path.name}' is missing artifact_ingestion details")
            elif not artifact_ingestion.get("steps"):
                errors.append(f"Manifest '{sample_path.name}' should record artifact_ingestion steps")

    execution_pass_path = ai_dev_root / "asset-pipeline" / "manifests" / "mesh-ai-azure-sakura-execution-pass.json"
    if execution_pass_path.exists():
        payload = json.loads(execution_pass_path.read_text(encoding="utf-8"))
        required_fields = {
            "asset_id",
            "profile_id",
            "blender_executable",
            "intake_manifest",
            "started_at",
            "completed_at",
            "steps",
        }
        missing_fields = sorted(required_fields - payload.keys())
        if missing_fields:
            errors.append(
                f"Execution pass '{execution_pass_path.name}' is missing fields: {', '.join(missing_fields)}"
            )
        steps = payload.get("steps") or []
        if not steps:
            errors.append(f"Execution pass '{execution_pass_path.name}' should include executed steps")
        for step in steps:
            if step.get("step_type") != "blender_script":
                continue
            if step.get("return_code") != 0:
                errors.append(
                    f"Execution pass '{execution_pass_path.name}' has a non-zero return code for step '{step.get('step_id')}'"
                )
            if not step.get("output_evidence"):
                errors.append(
                    f"Execution pass '{execution_pass_path.name}' should record output evidence for step '{step.get('step_id')}'"
                )

    for relative_path, snippet in TEXT_EXPECTATIONS:
        path = (ai_dev_root / relative_path).resolve()
        if not path.exists():
            errors.append(f"Missing text-check file: {relative_path}")
            continue
        content = path.read_text(encoding="utf-8")
        if snippet not in content:
            errors.append(f"Expected '{snippet}' in {relative_path}")

    return errors


def format_errors(errors: Iterable[str]) -> str:
    return "\n".join(f"- {error}" for error in errors)


def main() -> int:
    ai_dev_root = Path(__file__).resolve().parents[2]
    errors = collect_errors(ai_dev_root)
    if errors:
        print("Mesh AI pipeline validation failed.")
        print(format_errors(errors))
        return 1

    print("Mesh AI pipeline validation passed.")
    print(f"Validated files: {len(EXPECTED_FILES)}")
    print(f"Validated workflow specs: {len(WORKFLOW_EXPECTATIONS)}")
    print(f"Validated text expectations: {len(TEXT_EXPECTATIONS)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
