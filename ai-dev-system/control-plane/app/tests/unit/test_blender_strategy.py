from __future__ import annotations

from pathlib import Path

from app.agent.state import ActionRequest, SelectorSpec, WindowTarget
from app.agent.strategies import ExecutionContext, StrategyRegistry


class _FakeRuntime:
    def __init__(self) -> None:
        self.calls: list[tuple[str, dict]] = []

    def call_tool(self, name: str, arguments: dict) -> dict:
        self.calls.append((name, dict(arguments)))
        return {"structured_content": {"success": True}, "content": [], "is_error": False}


class _NoopGuard:
    pass


def test_blender_mcp_strategy_uses_blender_runtime() -> None:
    runtime = _FakeRuntime()
    registry = StrategyRegistry.default()
    action = ActionRequest(
        name="inspect_scene",
        action_type="blender_mcp_tool",
        value="get_scene_info",
        allowed_strategies=["blender_mcp_tool"],
        metadata={"tool_name": "get_scene_info", "tool_params": {}},
    )
    ctx = ExecutionContext(
        action=action,
        profile=type("Profile", (), {"name": "blender-editor"})(),
        active_window=WindowTarget(handle=1, title="(Unsaved) - Blender 5.1.0", class_name="GHOST_WindowClass", pid=9),
        window_selector=SelectorSpec(title_re=r".* - Blender .*", backend="uia"),
        pywinauto=object(),
        pyautogui=object(),
        screen_capture=object(),
        guard=_NoopGuard(),
        settings=type("Settings", (), {})(),
        artifact_dir=Path("."),
        metadata={"run_context": {"blender_runtime": runtime}},
    )

    registry.execute("blender_mcp_tool", ctx)

    assert runtime.calls == [("get_scene_info", {})]
    assert ctx.metadata["mcp_result"]["structured_content"]["success"] is True
