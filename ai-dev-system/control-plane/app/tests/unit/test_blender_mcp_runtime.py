from __future__ import annotations

from pathlib import Path

from app.blender.mcp_runtime import BlenderMcpRuntime


class _FakeAsyncClient:
    def __init__(self, repo_root: Path) -> None:
        self.repo_root = repo_root
        self.connected = False
        self.closed = False
        self.tool_calls: list[tuple[str, dict]] = []
        self.connect_calls = 0

    async def connect(self):
        self.connected = True
        self.connect_calls += 1
        return self

    async def close(self, exc_type=None, exc=None, tb=None):
        self.connected = False
        self.closed = True

    async def list_tools(self):
        return ["get_scene_info", "get_object_info", "get_viewport_screenshot"]

    async def list_resources(self):
        return ["scene://current"]

    async def call_tool(self, name: str, arguments: dict):
        self.tool_calls.append((name, dict(arguments)))
        return {"structured_content": {"success": True}, "content": [], "is_error": False}

    async def read_resource(self, uri: str):
        return {"uri": uri, "contents": []}


def test_blender_runtime_connects_lists_tools_and_reconnects(monkeypatch, tmp_path) -> None:
    fake_holder: dict[str, _FakeAsyncClient] = {}

    def _factory(repo_root: Path) -> _FakeAsyncClient:
        client = _FakeAsyncClient(repo_root)
        fake_holder["client"] = client
        return client

    monkeypatch.setattr("app.blender.mcp_runtime._BlenderMcpAsyncClient", _factory)

    runtime = BlenderMcpRuntime(tmp_path)
    runtime.connect()

    assert runtime.list_tools() == ["get_scene_info", "get_object_info", "get_viewport_screenshot"]
    assert runtime.list_resources() == ["scene://current"]
    assert runtime.call_tool("get_scene_info", {})["structured_content"]["success"] is True

    first_client = fake_holder["client"]
    runtime.reconnect()
    second_client = fake_holder["client"]

    assert first_client.closed is True
    assert second_client is not first_client
    runtime.close()


def test_blender_runtime_extracts_image_bytes_from_structured_content() -> None:
    payload = {
        "structured_content": {"data": {"image_base64": "aGVsbG8="}},
        "content": [],
        "is_error": False,
    }

    assert BlenderMcpRuntime.extract_image_bytes(payload) == b"hello"
