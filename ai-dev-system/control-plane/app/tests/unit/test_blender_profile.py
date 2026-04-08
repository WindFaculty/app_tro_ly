from __future__ import annotations

import base64
from pathlib import Path

from app.agent.task_spec import TaskActionSpec, TaskSpec
from app.blender.capabilities import BlenderCapabilityRegistry
from app.config.settings import Settings
from app.logging.artifacts import ArtifactManager
from app.logging.logger import GuiAgentLogger
from app.profiles.blender_editor_profile import BlenderEditorProfile


class _FakeRuntime:
    def __init__(self) -> None:
        self.closed = False

    def connect(self) -> None:
        return None

    def close(self) -> None:
        self.closed = True

    def capability_snapshot(self) -> dict:
        return {
            "tools": ["get_scene_info", "get_object_info", "get_viewport_screenshot"],
            "resources": ["scene://current"],
        }

    def call_tool(self, name: str, arguments: dict) -> dict:
        assert name == "get_viewport_screenshot"
        return {
            "structured_content": {"success": True, "data": {"image_base64": base64.b64encode(b"shot").decode("ascii")}},
            "content": [],
            "is_error": False,
        }

    @staticmethod
    def extract_image_bytes(result: dict) -> bytes | None:
        return base64.b64decode(result["structured_content"]["data"]["image_base64"])


def test_blender_profile_build_plan_from_task_spec_compiles_actions() -> None:
    profile = BlenderEditorProfile()
    profile._last_run_context = {
        "available_tools": ["get_scene_info", "get_object_info", "get_viewport_screenshot"],
        "available_resources": [],
    }
    task = TaskSpec(
        profile="blender-editor",
        actions=[TaskActionSpec(capability="blender.scene.inspect")],
    )

    plan = profile.build_plan_from_task_spec(task, Path("."))

    assert len(plan) == 1
    assert plan[0].action_type == "blender_mcp_tool"


def test_blender_profile_inspect_extras_writes_viewport_screenshot(monkeypatch, tmp_path) -> None:
    fake_runtime = _FakeRuntime()
    monkeypatch.setattr("app.profiles.blender_editor_profile.BlenderMcpRuntime", lambda repo_root: fake_runtime)

    profile = BlenderEditorProfile()
    artifacts = ArtifactManager.create(tmp_path / "logs", "blender-editor")
    logger = GuiAgentLogger(artifacts.run_dir / "run.jsonl")
    payload = profile.inspect_extras(
        settings=Settings.default(),
        driver=object(),
        pywinauto=object(),
        screenshots=object(),
        artifacts=artifacts,
        logger=logger,
        target_window=object(),
    )

    assert "viewport_screenshot" in payload
    assert Path(payload["viewport_screenshot"]).exists()
    assert fake_runtime.closed is True


def test_blender_profile_list_capabilities_returns_matrix(monkeypatch) -> None:
    fake_runtime = _FakeRuntime()
    monkeypatch.setattr("app.profiles.blender_editor_profile.BlenderMcpRuntime", lambda repo_root: fake_runtime)

    payload = BlenderEditorProfile().list_capabilities(settings=Settings.default())

    rows = {row["capability"]: row for row in payload["capabilities"]}
    assert rows["blender.scene.inspect"]["status"] == "supported_via_mcp"
    assert rows["blender.execute_code"]["status"] == "blocked_by_policy"
