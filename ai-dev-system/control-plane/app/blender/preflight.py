from __future__ import annotations

from typing import Any

from app.agent.task_spec import TaskSpec
from app.agent.state import WindowTarget
from app.automation.windows_driver import WindowsDriver
from app.blender.capabilities import BlenderCapabilityRegistry


class BlenderPreflight:
    def evaluate(
        self,
        *,
        task_input: str | TaskSpec,
        driver: WindowsDriver,
        active_window: WindowTarget | None,
        run_context: dict[str, Any] | None = None,
        required_capabilities: list[str] | None = None,
    ) -> dict[str, Any]:
        del task_input
        context = run_context or {}
        capability_matrix = list(context.get("capability_matrix") or [])
        capability_rows = {
            str(row.get("capability")): row
            for row in capability_matrix
            if isinstance(row, dict) and row.get("capability")
        }
        missing_capabilities = [
            capability
            for capability in (required_capabilities or [])
            if (capability_rows.get(capability) or {}).get("status") != "supported_via_mcp"
        ]
        missing_capability_reasons = {
            capability: str((capability_rows.get(capability) or {}).get("policy_reason") or "")
            for capability in missing_capabilities
        }
        available_tools = list(context.get("available_tools") or [])
        required_safe_tools = BlenderCapabilityRegistry.required_safe_tools()
        missing_safe_tools = [tool for tool in required_safe_tools if tool not in available_tools]
        mcp_connected = bool(context.get("mcp_connected"))
        interactive = driver.is_interactive_desktop_available()
        window_attached = active_window is not None
        title = str(active_window.title if active_window is not None else "")
        blender_window_matches = window_attached and "blender" in title.lower()

        checks = {
            "window_attached": window_attached,
            "blender_window_matches": blender_window_matches,
            "interactive_desktop": interactive,
            "mcp_connected": mcp_connected,
            "required_safe_tools_available": not missing_safe_tools,
            "capabilities_available": not missing_capabilities,
        }

        blocked_reason = None
        if not window_attached:
            blocked_reason = "Blender window is not attached."
        elif not blender_window_matches:
            blocked_reason = f"Attached window does not look like Blender: {title}"
        elif not interactive:
            blocked_reason = "Interactive desktop is unavailable."
        elif not mcp_connected:
            blocked_reason = "Blender MCP connection is unavailable."
        elif missing_safe_tools:
            blocked_reason = f"Required Blender MCP safe tools are unavailable: {', '.join(missing_safe_tools)}"
        elif missing_capabilities:
            details = [
                f"{capability} ({missing_capability_reasons.get(capability) or 'unsupported'})"
                for capability in missing_capabilities
            ]
            blocked_reason = f"Required Blender capabilities are unavailable: {', '.join(details)}"

        return {
            "checks": checks,
            "available_tools": sorted(available_tools),
            "available_resources": sorted(list(context.get("available_resources") or [])),
            "required_safe_tools": required_safe_tools,
            "missing_safe_tools": missing_safe_tools,
            "required_capabilities": list(required_capabilities or []),
            "missing_capabilities": missing_capabilities,
            "missing_capability_reasons": missing_capability_reasons,
            "blocked_reason": blocked_reason,
        }
