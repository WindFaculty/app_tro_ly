from __future__ import annotations

import json
import re
import subprocess
import time
from dataclasses import dataclass, field
from datetime import datetime, timezone
from pathlib import Path
from typing import Any, Callable

from tools.mesh_ai_blender_wrappers import render_wrapper


SUPPORTED_LIFECYCLE_STATES = ("raw", "cleaned", "validated", "export-ready")
LIFECYCLE_STAGE_INDEX = {stage: index for index, stage in enumerate(SUPPORTED_LIFECYCLE_STATES)}
PROFILE_BY_ASSET_TYPE = {
    "prop": "unity_prop_v1",
    "room_item": "unity_room_item_v1",
    "avatar_accessory": "unity_avatar_accessory_v1",
    "avatar_clothing": "unity_avatar_clothing_v1",
}
CommandRunner = Callable[[list[str]], Any]


@dataclass(slots=True)
class MeshAiPipelineService:
    ai_dev_root: Path
    repo_root: Path = field(init=False)
    asset_pipeline_root: Path = field(init=False)

    def __post_init__(self) -> None:
        self.ai_dev_root = self.ai_dev_root.resolve()
        self.repo_root = self.ai_dev_root.parent
        self.asset_pipeline_root = self.ai_dev_root / "asset-pipeline"

    def load_json_or_yaml(self, path: Path) -> dict[str, Any]:
        raw = path.read_text(encoding="utf-8")
        try:
            return json.loads(raw)
        except json.JSONDecodeError:
            import yaml  # type: ignore[import-untyped]

            payload = yaml.safe_load(raw) or {}
            if not isinstance(payload, dict):
                raise ValueError(f"Structured file must decode to an object: {path}")
            return payload

    def load_intake_manifest(self, path: str | Path) -> dict[str, Any]:
        manifest_path = self._resolve_path(path)
        payload = self.load_json_or_yaml(manifest_path)
        missing = [
            field
            for field in (
                "asset_id",
                "asset_name",
                "asset_type",
                "category",
                "source",
                "source_path",
                "quality_estimate",
                "status",
                "target_runtime",
                "target_unity_path",
                "toolchain_steps",
                "validation_report_path",
                "preview_render_path",
                "notes",
                "manual_gates",
            )
            if field not in payload
        ]
        if missing:
            raise ValueError(f"Intake manifest is missing required fields: {', '.join(missing)}")

        status = str(payload["status"])
        if status not in SUPPORTED_LIFECYCLE_STATES:
            raise ValueError(f"Unsupported lifecycle status '{status}'.")

        payload["manifest_path"] = self._relative_to_repo(manifest_path)
        return payload

    def load_profile(self, profile_id: str) -> dict[str, Any]:
        profile_path = self.asset_pipeline_root / "profiles" / f"{profile_id}.json"
        if not profile_path.exists():
            raise FileNotFoundError(f"Missing asset profile: {profile_path}")
        return self.load_json_or_yaml(profile_path)

    def load_toolchain_map(self) -> dict[str, Any]:
        return self.load_json_or_yaml(self.asset_pipeline_root / "toolchain-map.json")

    def load_workflow_spec(self, relative_path: str) -> dict[str, Any]:
        workflow_path = self.ai_dev_root / relative_path
        if not workflow_path.exists():
            raise FileNotFoundError(f"Missing workflow spec: {workflow_path}")
        return self.load_json_or_yaml(workflow_path)

    def resolve_profile_id(self, intake_manifest: dict[str, Any]) -> str:
        explicit_profile = intake_manifest.get("profile_id")
        if explicit_profile:
            return str(explicit_profile)

        asset_type = str(intake_manifest["asset_type"])
        profile_id = PROFILE_BY_ASSET_TYPE.get(asset_type)
        if not profile_id:
            raise ValueError(f"Unsupported asset_type '{asset_type}' for profile selection.")
        return profile_id

    def build_refinement_plan(self, path: str | Path) -> dict[str, Any]:
        intake_manifest = self.load_intake_manifest(path)
        profile_id = self.resolve_profile_id(intake_manifest)
        profile = self.load_profile(profile_id)
        toolchain_map = self.load_toolchain_map()
        profile_steps = (toolchain_map.get("profiles") or {}).get(profile_id)
        if profile_steps is None:
            raise ValueError(f"Toolchain map missing profile '{profile_id}'.")

        workflow_spec_path = str(profile_steps["workflow_spec"])
        self.load_workflow_spec(workflow_spec_path)
        context = self._build_render_context(intake_manifest, profile_id)
        toolchain_steps = [
            render_wrapper(step_template, context, self.repo_root)
            for step_template in profile_steps.get("steps") or []
        ]

        return {
            "asset_id": intake_manifest["asset_id"],
            "asset_name": intake_manifest["asset_name"],
            "asset_type": intake_manifest["asset_type"],
            "category": intake_manifest["category"],
            "source": intake_manifest["source"],
            "source_path": intake_manifest["source_path"],
            "quality_estimate": intake_manifest["quality_estimate"],
            "status": "cleaned",
            "profile_id": profile_id,
            "target_runtime": intake_manifest["target_runtime"],
            "target_unity_path": intake_manifest["target_unity_path"],
            "slot": intake_manifest.get("slot"),
            "room_focus_preset": intake_manifest.get("room_focus_preset"),
            "workflow_spec_path": f"ai-dev-system/{workflow_spec_path}",
            "toolchain_steps": toolchain_steps,
            "validation_report_path": context["validation_report_path"],
            "preview_render_path": context["preview_render_path"],
            "artifact_ingestion": self.build_artifact_ingestion(toolchain_steps),
            "notes": list(intake_manifest.get("notes") or [])
            + [f"Selected profile: {profile_id}"]
            + list(profile.get("manual_gate_notes") or []),
            "manual_gates": list(intake_manifest.get("manual_gates") or [])
            + list(profile.get("manual_gate_notes") or []),
        }

    def build_validation_report(self, path: str | Path, refinement_plan: dict[str, Any] | None = None) -> dict[str, Any]:
        intake_manifest = self.load_intake_manifest(path)
        plan = refinement_plan or self.build_refinement_plan(path)
        context = self._build_render_context(intake_manifest, plan["profile_id"])
        artifact_ingestion = self.build_artifact_ingestion(plan["toolchain_steps"])

        checks = [
            self._check_path_exists("source_path_exists", context["source_path"], "Source intake path exists."),
            self._check_path_exists(
                "workflow_spec_exists",
                plan["workflow_spec_path"].replace("ai-dev-system/", "ai-dev-system/"),
                "Workflow spec exists for the selected profile.",
                relative_to_repo=True,
            ),
            self._check_path_exists(
                "profile_exists",
                f"ai-dev-system/asset-pipeline/profiles/{plan['profile_id']}.json",
                "Asset profile exists for the selected intake.",
                relative_to_repo=True,
            ),
        ]

        for step in plan["toolchain_steps"]:
            if step["step_type"] != "blender_script":
                continue
            checks.append(
                self._check_path_exists(
                    f"tool_script_exists::{step['step_id']}",
                    step["script_path"],
                    f"Root Blender helper exists for step '{step['step_id']}'.",
                    relative_to_repo=True,
                )
            )

        for step in artifact_ingestion["steps"]:
            if step["step_type"] == "manual_gate":
                checks.append(
                    {
                        "id": f"manual_gate::{step['step_id']}",
                        "passed": False,
                        "summary": f"Manual gate remains open for '{step['step_id']}'.",
                        "evidence_path": None,
                    }
                )
                continue
            for output_key, evidence_path in step["output_evidence"].items():
                checks.append(
                    {
                        "id": f"artifact_output_exists::{step['step_id']}::{output_key}",
                        "passed": True,
                        "summary": f"Executed artifact exists for '{step['step_id']}' output '{output_key}'.",
                        "evidence_path": evidence_path,
                    }
                )

        return {
            "asset_id": plan["asset_id"],
            "asset_name": plan["asset_name"],
            "asset_type": plan["asset_type"],
            "category": plan["category"],
            "source": plan["source"],
            "source_path": plan["source_path"],
            "quality_estimate": plan["quality_estimate"],
            "status": "validated",
            "profile_id": plan["profile_id"],
            "target_runtime": plan["target_runtime"],
            "target_unity_path": plan["target_unity_path"],
            "slot": plan.get("slot"),
            "room_focus_preset": plan.get("room_focus_preset"),
            "toolchain_steps": plan["toolchain_steps"],
            "validation_report_path": context["validation_report_path"],
            "preview_render_path": context["preview_render_path"],
            "checks": checks,
            "artifact_ingestion": artifact_ingestion,
            "notes": [
                "This report captures repo-side pipeline preflight checks plus ingested executed artifact evidence.",
                "Current implementation still treats a fresh target-machine Blender rerun as a manual gate when Blender is unavailable in the active shell.",
            ],
            "manual_gates": list(plan["manual_gates"]),
        }

    def build_export_handoff_manifest(
        self,
        path: str | Path,
        refinement_plan: dict[str, Any] | None = None,
        validation_report: dict[str, Any] | None = None,
    ) -> dict[str, Any]:
        intake_manifest = self.load_intake_manifest(path)
        plan = refinement_plan or self.build_refinement_plan(path)
        report = validation_report or self.build_validation_report(path, plan)
        context = self._build_render_context(intake_manifest, plan["profile_id"])
        artifact_ingestion = dict(report["artifact_ingestion"])
        handoff_source_path = self._resolve_handoff_source_path(artifact_ingestion, context["cleaned_source_path"])

        return {
            "asset_id": plan["asset_id"],
            "asset_name": plan["asset_name"],
            "asset_type": plan["asset_type"],
            "category": plan["category"],
            "source": plan["source"],
            "source_path": plan["source_path"],
            "quality_estimate": plan["quality_estimate"],
            "status": "export-ready",
            "profile_id": plan["profile_id"],
            "target_runtime": plan["target_runtime"],
            "target_unity_path": plan["target_unity_path"],
            "slot": plan.get("slot"),
            "room_focus_preset": plan.get("room_focus_preset"),
            "toolchain_steps": plan["toolchain_steps"],
            "validation_report_path": report["validation_report_path"],
            "preview_render_path": report["preview_render_path"],
            "handoff_source_path": handoff_source_path,
            "artifact_ingestion": artifact_ingestion,
            "notes": [
                "This handoff manifest captures ingested executed root artifacts without claiming a live Unity runtime registry.",
                "Import into Unity should still wait for the relevant manual gates and registry wiring tasks.",
            ],
            "manual_gates": list(plan["manual_gates"]),
        }

    def materialize_bundle(self, intake_manifest_path: str | Path, output_dir: str | Path) -> dict[str, Any]:
        output_root = Path(output_dir).resolve()
        output_root.mkdir(parents=True, exist_ok=True)

        refinement_plan = self.build_refinement_plan(intake_manifest_path)
        validation_report = self.build_validation_report(intake_manifest_path, refinement_plan)
        export_handoff = self.build_export_handoff_manifest(intake_manifest_path, refinement_plan, validation_report)

        asset_slug = self._slugify(refinement_plan["asset_id"])
        written_files = {
          "refinement_plan": output_root / f"{asset_slug}-refinement-plan.json",
          "validation_report": output_root / f"{asset_slug}-validation-report.json",
          "export_handoff_manifest": output_root / f"{asset_slug}-export-handoff.json"
        }
        written_files["refinement_plan"].write_text(json.dumps(refinement_plan, indent=2), encoding="utf-8")
        written_files["validation_report"].write_text(json.dumps(validation_report, indent=2), encoding="utf-8")
        written_files["export_handoff_manifest"].write_text(json.dumps(export_handoff, indent=2), encoding="utf-8")

        return {
            "asset_id": refinement_plan["asset_id"],
            "profile_id": refinement_plan["profile_id"],
            "written_files": {key: str(value) for key, value in written_files.items()},
        }

    def sync_lifecycle_manifests(self, intake_manifest_path: str | Path) -> dict[str, Any]:
        intake_path = self._resolve_path(intake_manifest_path)
        bundle_name = self._derive_bundle_name(intake_path)
        manifest_root = intake_path.parent

        refinement_plan = self.build_refinement_plan(intake_path)
        validation_report = self.build_validation_report(intake_path, refinement_plan)
        export_handoff = self.build_export_handoff_manifest(intake_path, refinement_plan, validation_report)

        written_files = {
            "refinement_plan": manifest_root / f"{bundle_name}-refinement-plan.json",
            "validation_report": manifest_root / f"{bundle_name}-validation-report.json",
            "export_handoff_manifest": manifest_root / f"{bundle_name}-handoff.json",
        }
        written_files["refinement_plan"].write_text(json.dumps(refinement_plan, indent=2), encoding="utf-8")
        written_files["validation_report"].write_text(json.dumps(validation_report, indent=2), encoding="utf-8")
        written_files["export_handoff_manifest"].write_text(json.dumps(export_handoff, indent=2), encoding="utf-8")

        return {
            "asset_id": refinement_plan["asset_id"],
            "profile_id": refinement_plan["profile_id"],
            "bundle_name": bundle_name,
            "written_files": {key: str(value) for key, value in written_files.items()},
        }

    def execute_wrapper_pass(
        self,
        intake_manifest_path: str | Path,
        *,
        blender_executable: str | Path,
        execution_summary_path: str | Path | None = None,
        run_command: CommandRunner | None = None,
    ) -> dict[str, Any]:
        intake_path = self._resolve_path(intake_manifest_path)
        plan = self.build_refinement_plan(intake_path)
        blender_path = str(Path(blender_executable).resolve())
        bundle_name = self._derive_bundle_name(intake_path)
        summary_path = (
            self._resolve_path(execution_summary_path)
            if execution_summary_path is not None
            else intake_path.parent / f"{bundle_name}-execution-pass.json"
        )
        runner = run_command or (lambda command: self._default_command_runner(command, self.repo_root))

        started_at = self._timestamp_now()
        steps: list[dict[str, Any]] = []
        for step in plan["toolchain_steps"]:
            step_payload: dict[str, Any] = {
                "step_id": step["step_id"],
                "stage": step["stage"],
                "step_type": step["step_type"],
            }
            if step["step_type"] != "blender_script":
                step_payload.update(
                    {
                        "status": "skipped",
                        "reason": "manual_gate",
                    }
                )
                steps.append(step_payload)
                continue

            command = list((step.get("command_spec") or {}).get("argv") or [])
            if not command:
                raise ValueError(f"Step '{step['step_id']}' is missing command_spec.argv")
            command[0] = blender_path
            step_started_at = self._timestamp_now()
            started = time.perf_counter()
            completed = runner(command)
            duration_seconds = round(time.perf_counter() - started, 3)

            output_evidence: dict[str, str] = {}
            missing_outputs: list[str] = []
            for output_key, relative_path in (step.get("outputs") or {}).items():
                resolved = self._resolve_path(relative_path)
                if resolved.exists():
                    output_evidence[str(output_key)] = self._relative_to_repo(resolved)
                else:
                    missing_outputs.append(str(output_key))

            step_payload.update(
                {
                    "command": command,
                    "started_at": step_started_at,
                    "completed_at": self._timestamp_now(),
                    "duration_seconds": duration_seconds,
                    "return_code": int(getattr(completed, "returncode", 1)),
                    "stdout": str(getattr(completed, "stdout", "")),
                    "stderr": str(getattr(completed, "stderr", "")),
                    "output_evidence": output_evidence,
                    "missing_outputs": missing_outputs,
                }
            )
            steps.append(step_payload)
            if step_payload["return_code"] != 0:
                break

        summary = {
            "asset_id": plan["asset_id"],
            "profile_id": plan["profile_id"],
            "blender_executable": blender_path,
            "intake_manifest": self._relative_to_repo(intake_path),
            "started_at": started_at,
            "completed_at": self._timestamp_now(),
            "steps": steps,
        }
        summary_path.parent.mkdir(parents=True, exist_ok=True)
        summary_path.write_text(json.dumps(summary, indent=2), encoding="utf-8")
        return summary

    def build_artifact_ingestion(self, toolchain_steps: list[dict[str, Any]]) -> dict[str, Any]:
        steps: list[dict[str, Any]] = []
        highest_completed_stage_index = -1
        completed_step_count = 0
        pending_step_count = 0
        pending_manual_gates: list[str] = []

        for step in toolchain_steps:
            step_payload: dict[str, Any] = {
                "step_id": step["step_id"],
                "stage": step["stage"],
                "step_type": step["step_type"],
                "description": step["description"],
                "output_evidence": {},
                "missing_outputs": [],
            }
            if step["step_type"] == "manual_gate":
                step_payload["manual_gate"] = step.get("manual_gate")
                step_payload["completed"] = False
                pending_step_count += 1
                if step.get("manual_gate"):
                    pending_manual_gates.append(str(step["manual_gate"]))
                steps.append(step_payload)
                continue

            step_payload["script_path"] = step.get("script_path")
            step_payload["command_spec"] = step.get("command_spec")
            step_payload["args"] = dict(step.get("args") or {})

            output_evidence: dict[str, str] = {}
            missing_outputs: list[str] = []
            for output_key, relative_path in (step.get("outputs") or {}).items():
                resolved = self._resolve_path(relative_path)
                if resolved.exists():
                    output_evidence[str(output_key)] = self._relative_to_repo(resolved)
                else:
                    missing_outputs.append(str(output_key))

            step_payload["output_evidence"] = output_evidence
            step_payload["missing_outputs"] = missing_outputs
            step_payload["completed"] = bool(output_evidence) and not missing_outputs

            if step_payload["completed"]:
                completed_step_count += 1
                highest_completed_stage_index = max(
                    highest_completed_stage_index,
                    LIFECYCLE_STAGE_INDEX.get(str(step["stage"]), 0),
                )
            else:
                pending_step_count += 1

            steps.append(step_payload)

        highest_completed_stage = (
            SUPPORTED_LIFECYCLE_STATES[highest_completed_stage_index]
            if highest_completed_stage_index >= 0
            else "raw"
        )

        return {
            "highest_completed_stage": highest_completed_stage,
            "completed_step_count": completed_step_count,
            "pending_step_count": pending_step_count,
            "pending_manual_gates": pending_manual_gates,
            "steps": steps,
            "notes": [
                "Execution evidence is ingested from the current root artifact paths rather than copied into ai-dev-system.",
            ],
        }

    def _check_path_exists(
        self,
        check_id: str,
        relative_or_absolute: str,
        summary: str,
        *,
        relative_to_repo: bool = False,
    ) -> dict[str, Any]:
        path = self.repo_root / relative_or_absolute if relative_to_repo else self._resolve_path(relative_or_absolute)
        return {
            "id": check_id,
            "passed": path.exists(),
            "summary": summary,
            "evidence_path": self._relative_to_repo(path) if path.exists() else None,
        }

    def _resolve_path(self, path: str | Path) -> Path:
        candidate = Path(path)
        if candidate.is_absolute():
            return candidate.resolve()
        return (self.repo_root / candidate).resolve()

    def _relative_to_repo(self, path: Path) -> str:
        try:
            return path.resolve().relative_to(self.repo_root).as_posix()
        except ValueError:
            return str(path.resolve())

    def _build_render_context(self, intake_manifest: dict[str, Any], profile_id: str) -> dict[str, str]:
        asset_id = str(intake_manifest["asset_id"])
        asset_slug = self._slugify(asset_id)
        artifact_targets = dict(intake_manifest.get("artifact_targets") or {})
        dependencies = dict(intake_manifest.get("dependencies") or {})
        asset_name = str(intake_manifest["asset_name"])
        default_cleaned = self._default_cleaned_source_path(profile_id, asset_slug)

        return {
            "asset_id": asset_id,
            "asset_slug": asset_slug,
            "asset_name": asset_name,
            "source_path": str(intake_manifest["source_path"]),
            "cleaned_source_path": str(artifact_targets.get("cleaned_source_path") or default_cleaned),
            "audit_report_path": str(artifact_targets.get("audit_report_path") or f"tools/reports/{asset_slug}_asset_audit.json"),
            "refinement_report_path": str(
                artifact_targets.get("refinement_report_path") or f"tools/reports/{asset_slug}_refinement_report.json"
            ),
            "scene_state_report_path": str(
                artifact_targets.get("scene_state_report_path") or f"tools/reports/{asset_slug}_scene_state.json"
            ),
            "validation_report_path": str(
                artifact_targets.get("validation_report_path") or f"tools/reports/{asset_slug}_validation_report.json"
            ),
            "preview_render_path": str(
                artifact_targets.get("preview_render_path") or f"tools/renders/{asset_slug}_front.png"
            ),
            "export_handoff_manifest_path": str(
                artifact_targets.get("export_handoff_manifest_path")
                or f"ai-dev-system/asset-pipeline/manifests/{asset_slug}-handoff.json"
            ),
            "base_avatar_blend": str(dependencies.get("base_avatar_blend") or ""),
            "source_body_blend": str(dependencies.get("source_body_blend") or ""),
            "clothy_helper_fbx": str(dependencies.get("clothy_helper_fbx") or ""),
            "dress_object_name": self._default_dress_object_name(asset_name),
        }

    @staticmethod
    def _default_cleaned_source_path(profile_id: str, asset_slug: str) -> str:
        suffix_map = {
            "unity_prop_v1": "cleaned",
            "unity_room_item_v1": "room_item_cleaned",
            "unity_avatar_accessory_v1": "accessory_cleaned",
            "unity_avatar_clothing_v1": "clothing_cleaned",
        }
        suffix = suffix_map.get(profile_id, "cleaned")
        return f"bleder/{asset_slug}_{suffix}.blend"

    @staticmethod
    def _default_dress_object_name(asset_name: str) -> str:
        compact = re.sub(r"[^A-Za-z0-9]+", "", asset_name.title())
        return f"CHR_Dress_{compact}_v1"

    @staticmethod
    def _derive_bundle_name(intake_path: Path) -> str:
        stem = intake_path.stem
        if stem.endswith("-intake"):
            return stem[: -len("-intake")]
        return stem

    @staticmethod
    def _resolve_handoff_source_path(artifact_ingestion: dict[str, Any], fallback_path: str) -> str:
        for step in artifact_ingestion.get("steps") or []:
            output_evidence = step.get("output_evidence") or {}
            cleaned_source_path = output_evidence.get("cleaned_source_path")
            if cleaned_source_path:
                return str(cleaned_source_path)
        return fallback_path

    @staticmethod
    def _slugify(value: str) -> str:
        return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")

    @staticmethod
    def _timestamp_now() -> str:
        return datetime.now(timezone.utc).isoformat()

    @staticmethod
    def _default_command_runner(command: list[str], cwd: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            command,
            cwd=str(cwd),
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            check=False,
        )
