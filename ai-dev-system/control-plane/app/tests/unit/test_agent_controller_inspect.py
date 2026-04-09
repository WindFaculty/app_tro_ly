from __future__ import annotations

import json
from pathlib import Path

from app.agent.controller import AgentController
from app.agent.state import SelectorSpec, WindowTarget
from app.config.settings import Settings
from app.profiles.blender_editor_profile import BlenderEditorProfile


class _FakePywinauto:
    def __init__(self) -> None:
        self.calls: list[tuple[str, SelectorSpec]] = []

    def resolve_window(self, selector, backend=None):
        self.calls.append((backend or selector.backend or "uia", selector))
        if selector.handle is not None:
            raise LookupError("handle lookup unavailable")
        return object()

    def dump_control_tree(self, root, max_depth=4, max_nodes=250):
        return [{"name": "root"}]


class _FakeScreenshots:
    def capture(self, path: Path, region=None):
        path.write_bytes(b"shot")
        return path


def test_inspect_falls_back_to_profile_selector_when_handle_lookup_fails(tmp_path) -> None:
    settings = Settings.default()
    settings.artifact_root = tmp_path / "logs"
    controller = AgentController(settings)
    window = WindowTarget(
        handle=526662,
        title="(Unsaved) - Blender 5.1.0",
        class_name="GHOST_WindowClass",
        pid=24964,
        bounds=(0, 0, 1280, 720),
    )
    fake_pywinauto = _FakePywinauto()
    controller._attach_or_launch = lambda profile: window  # type: ignore[method-assign]
    controller._pywinauto = fake_pywinauto
    controller._screenshots = _FakeScreenshots()

    profile = BlenderEditorProfile()
    profile.inspect_extras = lambda **kwargs: {"ok": True}  # type: ignore[method-assign]

    payload = controller.inspect(profile)

    assert payload["window"]["handle"] == 526662
    assert Path(payload["control_tree_uia_path"]).exists()
    assert Path(payload["control_tree_win32_path"]).exists()
    call_backends = [backend for backend, _ in fake_pywinauto.calls]
    assert call_backends == ["uia", "uia", "win32", "win32"]
    fallback_selectors = [selector for _, selector in fake_pywinauto.calls if selector.handle is None]
    assert fallback_selectors[0].title_re == r".* - Blender .*"
    assert fallback_selectors[1].title_re == r".* - Blender .*"


def test_inspect_records_control_tree_errors_without_failing(tmp_path) -> None:
    settings = Settings.default()
    settings.artifact_root = tmp_path / "logs"
    controller = AgentController(settings)
    window = WindowTarget(
        handle=1,
        title="(Unsaved) - Blender 5.1.0",
        class_name="GHOST_WindowClass",
        pid=10,
        bounds=(0, 0, 640, 480),
    )

    class _MissingPywinauto:
        def resolve_window(self, selector, backend=None):
            raise LookupError(f"missing {backend or selector.backend}")

        def dump_control_tree(self, root, max_depth=4, max_nodes=250):
            return [{"name": "never-called"}]

    controller._attach_or_launch = lambda profile: window  # type: ignore[method-assign]
    controller._pywinauto = _MissingPywinauto()
    controller._screenshots = _FakeScreenshots()
    profile = BlenderEditorProfile()
    profile.inspect_extras = lambda **kwargs: {"ok": True}  # type: ignore[method-assign]

    payload = controller.inspect(profile)

    assert payload["control_tree_uia_error"]
    assert payload["control_tree_win32_error"]
    assert json.loads(Path(payload["control_tree_uia_path"]).read_text(encoding="utf-8")) == []
    assert json.loads(Path(payload["control_tree_win32_path"]).read_text(encoding="utf-8")) == []
