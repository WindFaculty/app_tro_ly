from __future__ import annotations

from app.agent.state import WindowTarget
from app.agent.task_spec import TaskSpec
from app.blender.capabilities import BlenderCapabilityRegistry
from app.blender.preflight import BlenderPreflight


class _FakeDriver:
    def __init__(self, *, interactive: bool = True) -> None:
        self._interactive = interactive

    def is_interactive_desktop_available(self) -> bool:
        return self._interactive


def _blender_window() -> WindowTarget:
    return WindowTarget(
        handle=300,
        title="(Unsaved) - Blender 5.1.0",
        class_name="GHOST_WindowClass",
        pid=77,
        bounds=(0, 0, 1920, 1080),
    )


def test_blender_preflight_passes_with_window_and_safe_tools() -> None:
    result = BlenderPreflight().evaluate(
        task_input=TaskSpec(profile="blender-editor", actions=[]),
        driver=_FakeDriver(),
        active_window=_blender_window(),
        run_context={
            "mcp_connected": True,
            "available_tools": BlenderCapabilityRegistry.required_safe_tools(),
            "available_resources": [],
            "capability_matrix": BlenderCapabilityRegistry.build_matrix(
                tools=BlenderCapabilityRegistry.required_safe_tools(),
                resources=[],
            ),
        },
        required_capabilities=["blender.scene.inspect"],
    )

    assert result["blocked_reason"] is None
    assert result["checks"]["required_safe_tools_available"] is True
    assert result["checks"]["capabilities_available"] is True


def test_blender_preflight_blocks_when_connection_missing() -> None:
    result = BlenderPreflight().evaluate(
        task_input=TaskSpec(profile="blender-editor", actions=[]),
        driver=_FakeDriver(),
        active_window=_blender_window(),
        run_context={
            "mcp_connected": False,
            "available_tools": [],
            "available_resources": [],
            "capability_matrix": [],
        },
        required_capabilities=["blender.scene.inspect"],
    )

    assert result["blocked_reason"] == "Blender MCP connection is unavailable."


def test_blender_preflight_blocks_when_safe_tools_missing() -> None:
    result = BlenderPreflight().evaluate(
        task_input=TaskSpec(profile="blender-editor", actions=[]),
        driver=_FakeDriver(),
        active_window=_blender_window(),
        run_context={
            "mcp_connected": True,
            "available_tools": ["get_scene_info"],
            "available_resources": [],
            "capability_matrix": BlenderCapabilityRegistry.build_matrix(tools=["get_scene_info"], resources=[]),
        },
        required_capabilities=["blender.scene.inspect"],
    )

    assert "Required Blender MCP safe tools are unavailable" in str(result["blocked_reason"])
