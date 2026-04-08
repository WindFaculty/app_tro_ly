from __future__ import annotations

import asyncio
import base64
from contextlib import suppress
import json
import os
from pathlib import Path
import shlex
import shutil
import sys
from typing import Any, Callable

from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

from app.blender.upstream import load_blender_upstream_metadata


class _BlenderMcpAsyncClient:
    def __init__(self, repo_root: Path) -> None:
        self._repo_root = repo_root
        self._session: ClientSession | None = None
        self._stdio_ctx = None
        self._client_ctx = None

    async def connect(self) -> "_BlenderMcpAsyncClient":
        if self._session is not None:
            return self

        server = StdioServerParameters(
            command=self._command(),
            args=self._args(),
            env=self._env(),
            cwd=self._repo_root,
        )
        self._stdio_ctx = stdio_client(server)
        try:
            read, write = await asyncio.wait_for(self._stdio_ctx.__aenter__(), timeout=self._timeout_seconds())
            self._client_ctx = ClientSession(read, write)
            self._session = await asyncio.wait_for(self._client_ctx.__aenter__(), timeout=self._timeout_seconds())
            await asyncio.wait_for(self._session.initialize(), timeout=self._timeout_seconds())
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

    async def reconnect(self) -> "_BlenderMcpAsyncClient":
        await self.close()
        return await self.connect()

    @property
    def session(self) -> ClientSession:
        if self._session is None:
            raise RuntimeError("Blender MCP client is not connected.")
        return self._session

    async def list_tools(self) -> list[str]:
        result = await asyncio.wait_for(self.session.list_tools(), timeout=self._timeout_seconds())
        return [tool.name for tool in result.tools]

    async def list_resources(self) -> list[str]:
        result = await asyncio.wait_for(self.session.list_resources(), timeout=self._timeout_seconds())
        return [resource.name for resource in result.resources]

    async def call_tool(self, name: str, arguments: dict[str, Any]) -> dict[str, Any]:
        result = await asyncio.wait_for(self.session.call_tool(name, arguments), timeout=self._timeout_seconds())
        structured = getattr(result, "structuredContent", None)
        payload = {
            "structured_content": structured,
            "content": [self._content_to_dict(item) for item in getattr(result, "content", [])],
            "is_error": getattr(result, "isError", False),
        }
        if isinstance(structured, dict) and "success" not in structured:
            payload["structured_content"] = {
                **structured,
                "success": not payload["is_error"],
            }
        elif not isinstance(structured, dict):
            payload["structured_content"] = {"success": not payload["is_error"], "data": structured}
        return payload

    async def read_resource(self, uri: str) -> dict[str, Any]:
        result = await asyncio.wait_for(self.session.read_resource(uri), timeout=self._timeout_seconds())
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

    @staticmethod
    def _command() -> str:
        configured = os.environ.get("APP_TRO_LY_BLENDER_MCP_COMMAND", "").strip()
        if configured:
            return configured
        resolved = shutil.which("uvx")
        if resolved:
            return resolved
        scripts_sibling = Path(sys.executable).parent / "Scripts" / "uvx.exe"
        if scripts_sibling.exists():
            return str(scripts_sibling)
        return "uvx"

    @classmethod
    def _args(cls) -> list[str]:
        raw = os.environ.get("APP_TRO_LY_BLENDER_MCP_ARGS", "blender-mcp").strip()
        if not raw:
            return ["blender-mcp"]
        if raw.startswith("["):
            parsed = json.loads(raw)
            if not isinstance(parsed, list) or not all(isinstance(item, str) for item in parsed):
                raise ValueError("APP_TRO_LY_BLENDER_MCP_ARGS JSON form must be a list of strings.")
            return list(parsed)
        return shlex.split(raw, posix=False)

    @staticmethod
    def _env() -> dict[str, str]:
        upstream = load_blender_upstream_metadata()
        env = dict(os.environ)
        env["SystemRoot"] = os.environ.get("SystemRoot", r"C:\Windows")
        env.update({key: str(value) for key, value in (upstream.get("default_env") or {}).items()})
        for key in ("BLENDER_HOST", "BLENDER_PORT", "DISABLE_TELEMETRY"):
            if key in os.environ:
                env[key] = os.environ[key]
        return env

    @staticmethod
    def _timeout_seconds() -> float:
        raw = os.environ.get("APP_TRO_LY_BLENDER_MCP_TIMEOUT_SECONDS", "15").strip()
        try:
            return max(1.0, float(raw))
        except ValueError:
            return 15.0


class BlenderMcpRuntime:
    def __init__(self, repo_root: Path) -> None:
        self._repo_root = repo_root
        self._runner: asyncio.Runner | None = None
        self._client: _BlenderMcpAsyncClient | None = None
        self._tool_cache: list[str] = []
        self._resource_cache: list[str] = []

    def connect(self) -> None:
        if self._runner is not None and self._client is not None:
            return
        self._runner = asyncio.Runner()
        self._client = _BlenderMcpAsyncClient(self._repo_root)
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

    def capability_snapshot(self) -> dict[str, Any]:
        self.connect()
        return {
            "tools": self.list_tools(),
            "resources": self.list_resources(),
        }

    def resolve_tool_name(self, aliases: list[str]) -> str | None:
        tools = set(self.list_tools())
        for alias in aliases:
            if alias in tools:
                return alias
        return None

    @staticmethod
    def extract_image_bytes(result: dict[str, Any]) -> bytes | None:
        structured = result.get("structured_content") or {}
        if isinstance(structured, dict):
            for key in ("image_base64", "screenshot_base64", "base64", "png_base64"):
                value = structured.get(key)
                if isinstance(value, str) and value.strip():
                    return base64.b64decode(value)
            data = structured.get("data")
            if isinstance(data, dict):
                for key in ("image_base64", "screenshot_base64", "base64", "png_base64"):
                    value = data.get(key)
                    if isinstance(value, str) and value.strip():
                        return base64.b64decode(value)
        for item in result.get("content") or []:
            if not isinstance(item, dict):
                continue
            data = item.get("data")
            if isinstance(data, str) and data.strip():
                try:
                    return base64.b64decode(data)
                except Exception:
                    continue
        return None

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
            "eof",
        )
        return any(marker in combined for marker in markers)
