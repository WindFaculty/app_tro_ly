from __future__ import annotations

from pathlib import Path

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()


ROOT = Path(__file__).resolve().parent
REPO_ROOT = ROOT.parent

from app.blender.capabilities import BlenderCapabilityRegistry
from app.blender.mcp_runtime import BlenderMcpRuntime


def main() -> None:
    runtime = BlenderMcpRuntime(REPO_ROOT)
    try:
        runtime.connect()
        tools = runtime.list_tools()
        resources = runtime.list_resources()
        required_tools = BlenderCapabilityRegistry.required_safe_tools()
        missing = [tool for tool in required_tools if tool not in tools]
        if missing:
            raise RuntimeError(f"Blender MCP is connected but missing required safe tools: {', '.join(missing)}")
        print(f"Connected to Blender MCP server with {len(tools)} tools and {len(resources)} resources.")
        print("Required safe tools:", ", ".join(required_tools))
        print("Sample tools:", ", ".join(tools[:10]))
        print("Sample resources:", ", ".join(resources[:10]))
        if not resources:
            print("Note: no Blender resources are currently visible. The addon may not be connected yet.")
    finally:
        runtime.close()


if __name__ == "__main__":
    main()
