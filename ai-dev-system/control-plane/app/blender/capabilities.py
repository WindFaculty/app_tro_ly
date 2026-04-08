from __future__ import annotations

from dataclasses import dataclass, field
import os
from typing import Any

from app.agent.state import ActionRequest, VerificationCheck
from app.agent.task_spec import TaskActionSpec, TaskSpec
from app.blender.upstream import load_blender_upstream_metadata


_CAPABILITY_STATUSES = {
    "supported_via_mcp",
    "blocked_by_policy",
    "unsupported",
}


@dataclass(frozen=True, slots=True)
class BlenderCapabilitySpec:
    capability: str
    category: str
    description: str
    tool_aliases: list[str] = field(default_factory=list)
    default_params: dict[str, Any] = field(default_factory=dict)
    policy_gate: str | None = None

    def resolve_tool_name(self, *, tools: set[str]) -> str | None:
        for alias in self.tool_aliases:
            if alias in tools:
                return alias
        return None

    def policy_status(self, *, task_spec: TaskSpec | None = None) -> tuple[bool, str | None]:
        if self.policy_gate != "execute_code":
            return True, None

        upstream = load_blender_upstream_metadata()
        env_key = str((upstream.get("policy_defaults") or {}).get("allow_execute_code_env") or "APP_TRO_LY_BLENDER_ALLOW_EXECUTE_CODE")
        allow_env = os.environ.get(env_key, "false").strip().lower() == "true"
        if not allow_env:
            return False, f"{env_key}=true is required for blender.execute_code."
        if task_spec is not None and not task_spec.confirm_destructive:
            return False, "blender.execute_code also requires confirm_destructive=true in the task spec."
        return True, None

    def resolved_status(self, *, tools: set[str]) -> tuple[str, str | None]:
        allowed, reason = self.policy_status()
        if not allowed:
            return "blocked_by_policy", reason
        tool_name = self.resolve_tool_name(tools=tools)
        if tool_name is None:
            return "unsupported", f"Missing MCP tool alias from {self.tool_aliases}."
        return "supported_via_mcp", None

    def to_matrix_row(self, *, tools: set[str], resources: set[str]) -> dict[str, Any]:
        status, policy_reason = self.resolved_status(tools=tools)
        assert status in _CAPABILITY_STATUSES
        tool_name = self.resolve_tool_name(tools=tools)
        return {
            "capability": self.capability,
            "category": self.category,
            "description": self.description,
            "status": status,
            "resolved_backend": "mcp" if status == "supported_via_mcp" else None,
            "tool_name": tool_name,
            "tool_aliases": list(self.tool_aliases),
            "tool_available": bool(tool_name),
            "policy_reason": policy_reason,
            "resources_visible": sorted(resources),
        }


class BlenderCapabilityRegistry:
    @classmethod
    def names(cls) -> list[str]:
        return sorted(cls._SPECS)

    @classmethod
    def get(cls, capability: str) -> BlenderCapabilitySpec:
        if capability not in cls._SPECS:
            known = ", ".join(sorted(cls._SPECS))
            raise ValueError(f"Unknown Blender capability '{capability}'. Known capabilities: {known}")
        return cls._SPECS[capability]

    @classmethod
    def required_safe_tools(cls) -> list[str]:
        upstream = load_blender_upstream_metadata()
        return [str(item) for item in list(upstream.get("required_safe_tools") or [])]

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
        del resources
        tool_set = set(tools)
        compiled: list[ActionRequest] = []
        for index, action in enumerate(task_spec.actions):
            compiled.append(cls._compile_action(task_spec=task_spec, action=action, index=index, tools=tool_set))
        return compiled

    @classmethod
    def _compile_action(
        cls,
        *,
        task_spec: TaskSpec,
        action: TaskActionSpec,
        index: int,
        tools: set[str],
    ) -> ActionRequest:
        spec = cls.get(action.capability)
        if action.backend not in {"auto", "mcp"}:
            raise ValueError(f"Blender capability '{action.capability}' only supports backend 'auto' or 'mcp'.")

        tool_name = spec.resolve_tool_name(tools=tools)
        if tool_name is None:
            raise ValueError(
                f"Capability '{action.capability}' is unsupported on this machine/project. "
                f"Missing tool aliases {spec.tool_aliases}."
            )

        allowed, policy_reason = spec.policy_status(task_spec=task_spec)
        if not allowed:
            raise ValueError(f"Capability '{action.capability}' is blocked by policy. {policy_reason}")

        params = dict(spec.default_params)
        params.update(action.params)
        return ActionRequest(
            name=f"{index:02d}_{spec.capability.replace('.', '_')}",
            action_type="blender_mcp_tool",
            value=tool_name,
            allowed_strategies=["blender_mcp_tool"],
            destructive=spec.capability == "blender.execute_code",
            metadata={
                "capability": spec.capability,
                "tool_name": tool_name,
                "tool_params": params,
                "resolved_backend": "mcp",
                "requested_backend": action.backend,
                "heal_hints": dict(action.heal_hints),
                "execution": dict(action.execution),
                "policy_reason": policy_reason,
            },
            postconditions=[VerificationCheck(kind="mcp_result_success")],
        )

    _SPECS: dict[str, BlenderCapabilitySpec] = {
        "blender.execute_code": BlenderCapabilitySpec(
            capability="blender.execute_code",
            category="blender",
            description="Execute Python code inside Blender through the optional upstream execute-code tool.",
            tool_aliases=["execute_blender_code", "execute_code"],
            default_params={"code": ""},
            policy_gate="execute_code",
        ),
        "blender.object.inspect": BlenderCapabilitySpec(
            capability="blender.object.inspect",
            category="blender",
            description="Inspect one Blender object through the upstream object-info tool.",
            tool_aliases=["get_object_info"],
        ),
        "blender.scene.inspect": BlenderCapabilitySpec(
            capability="blender.scene.inspect",
            category="blender",
            description="Inspect the current Blender scene through the upstream scene-info tool.",
            tool_aliases=["get_scene_info"],
        ),
        "blender.viewport.capture": BlenderCapabilitySpec(
            capability="blender.viewport.capture",
            category="blender",
            description="Capture the current Blender viewport through the upstream screenshot tool.",
            tool_aliases=["get_viewport_screenshot"],
        ),
    }
