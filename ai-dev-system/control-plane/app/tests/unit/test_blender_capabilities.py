from __future__ import annotations

import pytest

from app.agent.task_spec import TaskActionSpec, TaskSpec
from app.blender.capabilities import BlenderCapabilityRegistry


def test_blender_capability_matrix_marks_safe_tools_and_policy_block() -> None:
    matrix = BlenderCapabilityRegistry.build_matrix(
        tools=["get_scene_info", "get_object_info", "get_viewport_screenshot"],
        resources=["scene://current"],
    )
    rows = {row["capability"]: row for row in matrix}

    assert rows["blender.scene.inspect"]["status"] == "supported_via_mcp"
    assert rows["blender.object.inspect"]["status"] == "supported_via_mcp"
    assert rows["blender.viewport.capture"]["status"] == "supported_via_mcp"
    assert rows["blender.execute_code"]["status"] == "blocked_by_policy"


def test_blender_compile_actions_builds_blender_mcp_tool_request() -> None:
    spec = TaskSpec(
        profile="blender-editor",
        actions=[TaskActionSpec(capability="blender.scene.inspect")],
    )

    plan = BlenderCapabilityRegistry.compile_actions(
        task_spec=spec,
        tools=["get_scene_info", "get_object_info", "get_viewport_screenshot"],
        resources=[],
    )

    assert len(plan) == 1
    assert plan[0].action_type == "blender_mcp_tool"
    assert plan[0].metadata["tool_name"] == "get_scene_info"


def test_blender_compile_actions_rejects_execute_code_without_policy(monkeypatch) -> None:
    monkeypatch.delenv("APP_TRO_LY_BLENDER_ALLOW_EXECUTE_CODE", raising=False)
    spec = TaskSpec(
        profile="blender-editor",
        actions=[TaskActionSpec(capability="blender.execute_code", params={"code": "print('hi')"})],
    )

    with pytest.raises(ValueError, match="blocked by policy"):
        BlenderCapabilityRegistry.compile_actions(
            task_spec=spec,
            tools=["execute_blender_code"],
            resources=[],
        )
