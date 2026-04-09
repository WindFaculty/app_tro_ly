from __future__ import annotations

import asyncio
from pathlib import Path

from bootstrap_control_plane import bootstrap_control_plane_path


bootstrap_control_plane_path()


ROOT = Path(__file__).resolve().parent
REPO_ROOT = ROOT.parent

from unity_integration.service import UnityIntegrationService


async def main() -> None:
    service = UnityIntegrationService()
    async with service.mcp_client_factory()(REPO_ROOT) as client:
        tools = await client.list_tools()
        resources = await client.list_resources()
        print(f"Connected to Unity MCP with {len(tools)} tools and {len(resources)} resources.")
        print("Sample tools:", ", ".join(tools[:10]))
        print("Sample resources:", ", ".join(resources[:10]))
        print("Unity integration project root:", service.environment.project_root)
        print("Unity CLI Loop installed:", service.environment.cli_loop_package_installed)


if __name__ == "__main__":
    asyncio.run(main())
