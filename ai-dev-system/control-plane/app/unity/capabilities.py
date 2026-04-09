from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from app.agent.state import ActionRequest, VerificationCheck
from app.agent.task_spec import TaskActionSpec, TaskSpec
from app.unity.macros import UnityMacroRegistry
from unity_integration.capability_catalog import UnityCapabilityCatalog


_CAPABILITY_STATUSES = {
    "supported_via_mcp",
    "supported_via_gui_fallback",
    "manual_validation_required",
    "unsupported",
}


@dataclass(frozen=True, slots=True)
class UnityCapabilitySpec:
    capability: str
    category: str
    description: str
    preferred_backend: str
    tool_name: str | None = None
    default_params: dict[str, Any] = field(default_factory=dict)
    gui_macro: str | None = None
    gui_fallback_macro: str | None = None
    fallbackable: bool = False
    manual_validation_required: bool = False

    def resolved_status(self, *, tools: set[str]) -> str:
        has_mcp = bool(self.tool_name and self.tool_name in tools)
        has_gui = bool(self.gui_macro or self.gui_fallback_macro)
        if self.preferred_backend == "mcp":
            if has_mcp:
                return "manual_validation_required" if self.manual_validation_required else "supported_via_mcp"
            if self.fallbackable and has_gui:
                return "supported_via_gui_fallback"
            return "unsupported"
        if has_gui:
            return "manual_validation_required" if self.manual_validation_required else "supported_via_gui_fallback"
        if has_mcp:
            return "manual_validation_required" if self.manual_validation_required else "supported_via_mcp"
        return "unsupported"

    def to_matrix_row(self, *, tools: set[str], resources: set[str]) -> dict[str, Any]:
        status = self.resolved_status(tools=tools)
        assert status in _CAPABILITY_STATUSES
        resolved_backend = None
        if status == "supported_via_mcp":
            resolved_backend = "mcp"
        elif status == "supported_via_gui_fallback":
            resolved_backend = "gui"
        elif status == "manual_validation_required":
            resolved_backend = "mcp" if self.tool_name and self.tool_name in tools else "gui"
        return {
            "capability": self.capability,
            "category": self.category,
            "description": self.description,
            "status": status,
            "resolved_backend": resolved_backend,
            "preferred_backend": self.preferred_backend,
            "tool_name": self.tool_name,
            "gui_macro": self.gui_macro or self.gui_fallback_macro,
            "manual_validation_required": self.manual_validation_required,
            "tool_available": bool(self.tool_name and self.tool_name in tools),
            "resources_visible": sorted(resources),
        }


def _build_specs() -> dict[str, UnityCapabilitySpec]:
    specs: dict[str, UnityCapabilitySpec] = {}
    for item in UnityCapabilityCatalog.all():
        specs[item.capability] = UnityCapabilitySpec(
            capability=item.capability,
            category=item.category,
            description=item.description,
            preferred_backend=item.preferred_backend,
            tool_name=item.mcp_tool_name,
            default_params=dict(item.mcp_default_params),
            gui_macro=item.gui_macro,
            gui_fallback_macro=item.gui_fallback_macro,
            fallbackable=item.fallbackable,
            manual_validation_required=item.manual_validation_required,
        )
    return specs


class UnityCapabilityRegistry:
    _SPECS: dict[str, UnityCapabilitySpec] = _build_specs()

    @classmethod
    def names(cls) -> list[str]:
        return sorted(cls._SPECS)

    @classmethod
    def get(cls, capability: str) -> UnityCapabilitySpec:
        if capability not in cls._SPECS:
            known = ", ".join(sorted(cls._SPECS))
            raise ValueError(f"Unknown Unity capability '{capability}'. Known capabilities: {known}")
        return cls._SPECS[capability]

    @classmethod
    def build_matrix(cls, *, tools: list[str], resources: list[str]) -> list[dict[str, Any]]:
        tool_set = set(tools)
        resource_set = set(resources)
        return [cls.get(name).to_matrix_row(tools=tool_set, resources=resource_set) for name in cls.names()]

    @classmethod
    def compile_actions(
        cls,
        *,
        task_spec: TaskSpec,
        tools: list[str],
        resources: list[str],
    ) -> list[ActionRequest]:
        compiled: list[ActionRequest] = []
        tool_set = set(tools)
        resource_set = set(resources)
        for index, action in enumerate(task_spec.actions):
            compiled.extend(cls._compile_action(task_spec, action, index=index, tools=tool_set, resources=resource_set))
        return compiled

    @classmethod
    def _compile_action(
        cls,
        task_spec: TaskSpec,
        action: TaskActionSpec,
        *,
        index: int,
        tools: set[str],
        resources: set[str],
    ) -> list[ActionRequest]:
        del resources
        spec = cls.get(action.capability)
        requested_backend = action.backend
        if requested_backend not in {"auto", "mcp", "gui"}:
            raise ValueError(f"Unsupported backend '{requested_backend}' for capability '{action.capability}'.")

        if action.capability == "editor.layout.normalize":
            if requested_backend == "gui":
                raise ValueError("editor.layout.normalize is only supported through MCP-backed execution.")
            return [cls._compile_layout_normalize_action(action=action, index=index, task_spec=task_spec, tools=tools)]

        if requested_backend == "gui":
            return cls._compile_gui_action(spec, action, index=index, task_spec=task_spec)

        has_mcp = bool(spec.tool_name and spec.tool_name in tools)
        if requested_backend == "mcp":
            if not has_mcp:
                raise ValueError(f"Capability '{action.capability}' requested MCP backend, but tool '{spec.tool_name}' is unavailable.")
            return [cls._compile_mcp_action(spec, action, index=index)]

        if has_mcp and spec.preferred_backend == "mcp":
            return [cls._compile_mcp_action(spec, action, index=index)]

        if spec.preferred_backend == "gui":
            return cls._compile_gui_action(spec, action, index=index, task_spec=task_spec)

        if action.allow_fallback and spec.fallbackable:
            return cls._compile_gui_action(spec, action, index=index, task_spec=task_spec)

        if has_mcp:
            return [cls._compile_mcp_action(spec, action, index=index)]

        raise ValueError(
            f"Capability '{action.capability}' is unsupported on this machine/project. "
            f"Preferred backend='{spec.preferred_backend}', tool='{spec.tool_name}'."
        )

    @classmethod
    def _compile_layout_normalize_action(
        cls,
        *,
        action: TaskActionSpec,
        index: int,
        task_spec: TaskSpec,
        tools: set[str],
    ) -> ActionRequest:
        if "batch_execute" not in tools:
            raise ValueError("Capability 'editor.layout.normalize' requires the Unity MCP tool 'batch_execute'.")
        layout_name = str(
            action.params.get("layout")
            or task_spec.layout_policy.get("required")
            or task_spec.requires_layout
            or "default-6000"
        )
        commands = [
            {"tool": "execute_menu_item", "params": {"menu_path": f"Window/Layouts/{layout_name}"}},
            {"tool": "execute_menu_item", "params": {"menu_path": f"Window/Layout/{layout_name}"}},
            {"tool": "execute_menu_item", "params": {"menu_path": f"Window/Layouts/Load Layout/{layout_name}"}},
            {"tool": "execute_menu_item", "params": {"menu_path": f"Window/Layout/Load Layout/{layout_name}"}},
        ]
        return ActionRequest(
            name=f"{index:02d}_editor_layout_normalize",
            action_type="mcp_batch",
            value="batch_execute",
            allowed_strategies=["mcp_batch"],
            destructive=False,
            metadata={
                "capability": "editor.layout.normalize",
                "tool_name": "batch_execute",
                "tool_params": {
                    "commands": commands,
                    "fail_fast": False,
                    "parallel": False,
                },
                "resolved_backend": "mcp",
                "requested_backend": action.backend,
                "heal_hints": dict(action.heal_hints),
                "execution": dict(action.execution),
            },
            postconditions=[VerificationCheck(kind="mcp_batch_success")],
        )

    @classmethod
    def _compile_mcp_action(cls, spec: UnityCapabilitySpec, action: TaskActionSpec, *, index: int) -> ActionRequest:
        assert spec.tool_name is not None
        params = dict(spec.default_params)
        params.update(action.params)
        action_type = "mcp_batch" if spec.tool_name == "batch_execute" else "mcp_tool"
        execution = dict(action.execution)
        verify_kind = "mcp_batch_success" if action_type == "mcp_batch" else "mcp_result_success"
        if action_type == "mcp_tool":
            execution_mode = str(execution.get("mode") or "blocking")
            if execution_mode == "background_job_start":
                verify_kind = "mcp_job_started"
            elif execution_mode == "background_job_wait":
                verify_kind = "mcp_job_completed"
        return ActionRequest(
            name=f"{index:02d}_{spec.capability.replace('.', '_')}",
            action_type=action_type,
            value=spec.tool_name,
            allowed_strategies=[action_type],
            destructive=bool(params.get("action") in {"build", "delete", "remove", "modify", "create"}),
            metadata={
                "capability": spec.capability,
                "tool_name": spec.tool_name,
                "tool_params": params,
                "resolved_backend": "mcp",
                "requested_backend": action.backend,
                "heal_hints": dict(action.heal_hints),
                "execution": execution,
            },
            postconditions=[VerificationCheck(kind=verify_kind)],
        )

    @classmethod
    def _compile_gui_action(
        cls,
        spec: UnityCapabilitySpec,
        action: TaskActionSpec,
        *,
        index: int,
        task_spec: TaskSpec,
    ) -> list[ActionRequest]:
        macro = cls._resolve_gui_macro(spec, action)
        legacy = TaskSpec(
            profile=task_spec.profile,
            macro=macro["name"],
            args=macro["args"],
            confirm_destructive=task_spec.confirm_destructive,
            dry_run=task_spec.dry_run,
            requires_layout=task_spec.requires_layout or "default-6000",
            evidence=dict(task_spec.evidence),
            metadata=dict(task_spec.metadata),
        )
        compiled = UnityMacroRegistry.build_plan(legacy)
        for item in compiled:
            item.metadata.setdefault("capability", spec.capability)
            item.metadata.setdefault("resolved_backend", "gui")
            item.metadata.setdefault("requested_backend", action.backend)
            item.metadata.setdefault("heal_hints", dict(action.heal_hints))
            item.metadata.setdefault("execution", dict(action.execution))
            item.name = f"{index:02d}_{item.name}"
        return compiled

    @staticmethod
    def _resolve_gui_macro(spec: UnityCapabilitySpec, action: TaskActionSpec) -> dict[str, Any]:
        if spec.capability == "editor.surface.focus":
            surface = str(action.params.get("surface") or "").strip().lower().replace("-", "_").replace(" ", "_")
            mapping = {
                "hierarchy": "focus_hierarchy",
                "project": "focus_project",
                "inspector": "focus_inspector",
                "scene": "focus_scene_view",
                "game": "focus_game_view",
                "console": "focus_console",
            }
            if surface not in mapping:
                raise ValueError("editor.surface.focus requires params.surface in {hierarchy, project, inspector, scene, game, console}.")
            return {"name": mapping[surface], "args": {}}

        if spec.capability == "editor.window.open":
            window = str(action.params.get("window") or "").strip()
            if not window:
                raise ValueError("editor.window.open requires params.window.")
            return {"name": "open_window", "args": {"window": window}}

        if spec.capability == "editor.view.capture":
            surface = str(action.params.get("surface") or "game").strip().lower()
            return {"name": "capture_view", "args": {"surface": surface}}

        if spec.capability == "editor.playmode.control":
            action_name = str(action.params.get("action") or "Play").strip().lower()
            mapping = {
                "play": "play_mode",
                "stop": "stop_mode",
                "pause": "pause_mode",
            }
            if action_name not in mapping:
                raise ValueError("editor.playmode.control requires params.action in {Play, Stop, Pause}.")
            return {"name": mapping[action_name], "args": {}}

        macro = spec.gui_macro or spec.gui_fallback_macro
        if not macro:
            raise ValueError(f"Capability '{spec.capability}' does not define a GUI macro.")
        return {"name": macro, "args": dict(action.params)}
