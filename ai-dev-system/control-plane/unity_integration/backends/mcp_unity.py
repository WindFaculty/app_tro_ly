from __future__ import annotations

import asyncio
from contextlib import suppress
import json
from pathlib import Path
from typing import Any, Callable

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

from unity_integration.capability_catalog import UnityCapabilityCatalog
from unity_integration.contracts import UnityEnvironmentConfig, UnityExecutionRequest, UnityExecutionResult
from unity_integration.environment import UnityIntegrationEnvironment


class _UnityMcpAsyncClient:
    def __init__(self, repo_root: Path, environment: UnityEnvironmentConfig) -> None:
        self._repo_root = repo_root
        self._environment = environment
        self._session: ClientSession | None = None
        self._stdio_ctx = None
        self._client_ctx = None

    async def connect(self) -> "_UnityMcpAsyncClient":
        if self._session is not None:
            return self
        command = self._environment.unity_mcp_command
        if not command:
            raise RuntimeError("Unity MCP command is unavailable on this machine.")

        server = StdioServerParameters(
            command=command[0],
            args=list(command[1:]),
            env=dict(self._environment.unity_mcp_env),
            cwd=self._repo_root,
        )
        self._stdio_ctx = stdio_client(server)
        try:
            read, write = await self._stdio_ctx.__aenter__()
            self._client_ctx = ClientSession(read, write)
            self._session = await self._client_ctx.__aenter__()
            await self._session.initialize()
            return self
        except Exception as exc:
            await self.close(type(exc), exc, exc.__traceback__)
            raise

    async def close(self, exc_type=None, exc=None, tb=None) -> None:
        client_ctx = self._client_ctx
        stdio_ctx = self._stdio_ctx

        self._session = None
        self._client_ctx = None
        self._stdio_ctx = None

        if client_ctx is not None:
            with suppress(Exception):
                await client_ctx.__aexit__(exc_type, exc, tb)
        if stdio_ctx is not None:
            with suppress(Exception):
                await stdio_ctx.__aexit__(exc_type, exc, tb)

    async def reconnect(self) -> "_UnityMcpAsyncClient":
        await self.close()
        return await self.connect()

    @property
    def session(self) -> ClientSession:
        if self._session is None:
            raise RuntimeError("Unity MCP client is not connected.")
        return self._session

    async def list_tools(self) -> list[str]:
        result = await self.session.list_tools()
        return [tool.name for tool in result.tools]

    async def list_resources(self) -> list[str]:
        result = await self.session.list_resources()
        return [resource.name for resource in result.resources]

    async def call_tool(self, name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        result = await self.session.call_tool(name, arguments)
        return {
            "structured_content": getattr(result, "structuredContent", None),
            "content": [self._content_to_dict(item) for item in getattr(result, "content", [])],
            "is_error": getattr(result, "isError", False),
        }

    async def read_resource(self, uri: str) -> dict[str, Any]:
        result = await self.session.read_resource(uri)
        return {
            "uri": getattr(result, "uri", uri),
            "contents": [self._content_to_dict(item) for item in getattr(result, "contents", [])],
        }

    @staticmethod
    def _content_to_dict(item: Any) -> dict[str, Any]:
        payload = {"type": getattr(item, "type", item.__class__.__name__)}
        if hasattr(item, "text"):
            payload["text"] = item.text
        if hasattr(item, "data"):
            payload["data"] = item.data
        if hasattr(item, "mimeType"):
            payload["mimeType"] = item.mimeType
        if hasattr(item, "uri"):
            payload["uri"] = str(item.uri)
        return payload


class UnityMcpClient:
    def __init__(self, repo_root: Path | None = None, *, environment: UnityEnvironmentConfig | None = None) -> None:
        self._environment = environment or UnityIntegrationEnvironment().probe()
        self._repo_root = repo_root or self._environment.repo_root
        self._client = _UnityMcpAsyncClient(self._repo_root, self._environment)

    async def __aenter__(self) -> "UnityMcpClient":
        await self.connect()
        return self

    async def __aexit__(self, exc_type, exc, tb) -> None:
        await self.close(exc_type, exc, tb)

    async def connect(self) -> "UnityMcpClient":
        await self._client.connect()
        return self

    async def close(self, exc_type=None, exc=None, tb=None) -> None:
        await self._client.close(exc_type, exc, tb)

    async def reconnect(self) -> "UnityMcpClient":
        await self._client.reconnect()
        return self

    async def list_tools(self) -> list[str]:
        return await self._client.list_tools()

    async def list_resources(self) -> list[str]:
        return await self._client.list_resources()

    async def call_tool(self, name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        return await self._client.call_tool(name, arguments)

    async def read_resource(self, uri: str) -> dict[str, Any]:
        return await self._client.read_resource(uri)

    async def batch_execute(self, commands: list[dict[str, Any]], *, fail_fast: bool = True) -> dict[str, Any]:
        return await self.call_tool(
            "batch_execute",
            {
                "commands": commands,
                "fail_fast": fail_fast,
                "parallel": False,
            },
        )

    async def create_script(self, path: str, contents: str) -> dict[str, Any]:
        return await self.call_tool("create_script", {"path": path, "contents": contents})

    async def update_script(self, path: str, contents: str) -> dict[str, Any]:
        delete_result = await self.delete_script(path)
        if not self._delete_succeeded(delete_result):
            return delete_result
        return await self.create_script(path, contents)

    async def delete_script(self, path: str) -> dict[str, Any]:
        name, directory = self._split_script_path(path)
        return await self.call_tool(
            "manage_script",
            {
                "action": "delete",
                "name": name,
                "path": directory,
            },
        )

    async def create_scene(self, name: str, path: str, template: str = "3d_basic") -> dict[str, Any]:
        return await self.call_tool("manage_scene", {"action": "create", "name": name, "path": path, "template": template})

    async def load_scene(self, path: str) -> dict[str, Any]:
        return await self.call_tool("manage_scene", {"action": "load", "path": path})

    async def get_active_scene(self) -> dict[str, Any]:
        return await self.call_tool("manage_scene", {"action": "get_active"})

    async def get_scene_hierarchy(self, *, page_size: int = 50, include_transform: bool = True) -> dict[str, Any]:
        return await self.call_tool(
            "manage_scene",
            {
                "action": "get_hierarchy",
                "page_size": page_size,
                "include_transform": include_transform,
            },
        )

    async def get_project_info(self) -> dict[str, Any]:
        return await self.read_resource("mcpforunity://project/info")

    async def get_editor_state(self) -> dict[str, Any]:
        return await self.read_resource("mcpforunity://editor/state")

    async def find_gameobjects(self, search_term: str, search_method: str = "by_name") -> dict[str, Any]:
        return await self.call_tool("find_gameobjects", {"search_term": search_term, "search_method": search_method})

    async def read_console(self, *, types: list[str] | None = None, count: int = 50, format_name: str = "json") -> dict[str, Any]:
        return await self.call_tool(
            "read_console",
            {
                "action": "get",
                "types": types or ["error", "warning", "log"],
                "count": count,
                "format": format_name,
                "include_stacktrace": True,
            },
        )

    async def play(self) -> dict[str, Any]:
        return await self.call_tool("manage_editor", {"action": "play"})

    async def stop(self) -> dict[str, Any]:
        return await self.call_tool("manage_editor", {"action": "stop"})

    @staticmethod
    def _split_script_path(path: str) -> tuple[str, str]:
        script_path = Path(path.replace("\\", "/"))
        return script_path.stem, script_path.parent.as_posix()

    @staticmethod
    def _delete_succeeded(result: dict[str, Any] | None) -> bool:
        if not result or result.get("is_error"):
            return False
        structured = result.get("structured_content") or {}
        return bool(structured.get("success"))


class UnityMcpRuntime:
    def __init__(self, repo_root: Path | None = None, *, environment: UnityEnvironmentConfig | None = None) -> None:
        self._environment = environment or UnityIntegrationEnvironment().probe()
        self._repo_root = repo_root or self._environment.repo_root
        self._runner: asyncio.Runner | None = None
        self._client: UnityMcpClient | None = None
        self._tool_cache: list[str] = []
        self._resource_cache: list[str] = []

    def connect(self) -> None:
        if self._runner is not None and self._client is not None:
            return
        self._runner = asyncio.Runner()
        self._client = UnityMcpClient(repo_root=self._repo_root, environment=self._environment)
        self._runner.run(self._client.connect())
        self._tool_cache = self._runner.run(self._client.list_tools())
        self._resource_cache = self._runner.run(self._client.list_resources())

    def close(self) -> None:
        if self._runner is None or self._client is None:
            return
        try:
            self._runner.run(self._client.close())
        finally:
            self._runner.close()
            self._runner = None
            self._client = None
            self._tool_cache = []
            self._resource_cache = []

    def reconnect(self) -> None:
        self.close()
        self.connect()

    def list_tools(self) -> list[str]:
        self.connect()
        return list(self._tool_cache)

    def list_resources(self) -> list[str]:
        self.connect()
        return list(self._resource_cache)

    def call_tool(self, name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        def _call() -> dict[str, Any]:
            assert self._runner is not None and self._client is not None
            return self._runner.run(self._client.call_tool(name, arguments))

        return self._with_reconnect(_call)

    def read_resource(self, uri: str) -> dict[str, Any]:
        def _read() -> dict[str, Any]:
            assert self._runner is not None and self._client is not None
            return self._runner.run(self._client.read_resource(uri))

        return self._with_reconnect(_read)

    def read_text_resource(self, uri: str) -> str:
        payload = self.read_resource(uri)
        texts = [str(item.get("text", "")) for item in payload.get("contents", []) if item.get("text")]
        return "\n".join(texts)

    def read_json_resource(self, uri: str) -> dict[str, Any]:
        text = self.read_text_resource(uri).strip()
        return json.loads(text) if text else {}

    def capability_snapshot(self) -> dict[str, Any]:
        self.connect()
        return {
            "tools": self.list_tools(),
            "resources": self.list_resources(),
        }

    def _with_reconnect(self, operation: Callable[[], dict[str, Any]]) -> dict[str, Any]:
        self.connect()
        try:
            return operation()
        except Exception as exc:
            if not self._is_retryable_transport_exception(exc):
                raise
            self.reconnect()
            return operation()

    @staticmethod
    def _is_retryable_transport_exception(exc: Exception) -> bool:
        message = str(exc).strip().lower()
        combined = f"{exc.__class__.__name__} {message}".lower()
        markers = (
            "connection closed",
            "stream closed",
            "closedresourceerror",
            "endofstream",
            "broken pipe",
            "connection reset",
            "pipe is being closed",
            "transport",
            "stdio",
            "stdiobridge",
            "reloading",
            "please retry",
            "hint='retry'",
            'hint="retry"',
            "eof",
        )
        return any(marker in combined for marker in markers)


class UnityMcpBackend:
    name = "mcp"

    def __init__(self, environment: UnityEnvironmentConfig) -> None:
        self._environment = environment

    def is_available(self) -> bool:
        return bool(self._environment.unity_mcp_command) and self._environment.unity_mcp_available

    def supports_request(self, request: UnityExecutionRequest) -> bool:
        capability = UnityCapabilityCatalog.get(request.capability)
        return capability.supports_backend(self.name) and capability.mcp_tool_name is not None

    def runtime(self) -> UnityMcpRuntime:
        return UnityMcpRuntime(environment=self._environment)

    def execute(self, request: UnityExecutionRequest) -> UnityExecutionResult:
        capability = UnityCapabilityCatalog.get(request.capability)
        if not self.supports_request(request):
            return UnityExecutionResult(
                status="failed",
                capability=request.capability,
                backend_used=self.name,
                warnings=[f"Unity MCP backend does not support capability '{request.capability}'."],
                manual_validation_required=capability.manual_validation_required,
            )

        runtime = self.runtime()
        try:
            params = dict(capability.mcp_default_params)
            params.update(request.params)
            payload = runtime.call_tool(capability.mcp_tool_name or "", params)
            return UnityExecutionResult(
                status="failed" if payload.get("is_error") else "completed",
                capability=request.capability,
                backend_used=self.name,
                payload=payload,
                manual_validation_required=capability.manual_validation_required,
            )
        finally:
            runtime.close()
