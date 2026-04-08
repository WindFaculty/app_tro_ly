from __future__ import annotations

import json
import re
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent


def read_json(relative_path: str) -> dict:
    return json.loads((REPO_ROOT / relative_path).read_text(encoding="utf-8"))


def expect(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def extract_backend_url() -> str:
    source = (REPO_ROOT / "apps/desktop-shell/src-tauri/src/backend.rs").read_text(
        encoding="utf-8"
    )
    match = re.search(r'pub const BACKEND_URL: &str = "([^"]+)";', source)
    expect(match is not None, "Could not find BACKEND_URL in apps/desktop-shell/src-tauri/src/backend.rs")
    return match.group(1)


def extract_web_fallback_url() -> str:
    source = (REPO_ROOT / "apps/web-ui/src/services/runtimeHost.ts").read_text(
        encoding="utf-8"
    )
    match = re.search(r'\|\| "([^"]+)";', source)
    expect(
        match is not None,
        "Could not find the browser fallback backend URL in apps/web-ui/src/services/runtimeHost.ts",
    )
    return match.group(1)


def main() -> int:
    root_package = read_json("package.json")
    workspaces = root_package.get("workspaces", [])
    scripts = root_package.get("scripts", {})

    expect(
        workspaces == ["apps/web-ui", "apps/desktop-shell", "packages/contracts"],
        "Root package.json workspaces must cover apps/web-ui, apps/desktop-shell, and packages/contracts",
    )

    required_root_scripts = {
        "web:dev",
        "web:check",
        "web:build",
        "desktop:dev",
        "desktop:check",
        "desktop:build",
        "rebuild:check",
    }
    expect(
        required_root_scripts.issubset(scripts),
        "Root package.json is missing one or more rebuild execution scripts",
    )

    web_package = read_json("apps/web-ui/package.json")
    desktop_package = read_json("apps/desktop-shell/package.json")

    expect("check" in web_package.get("scripts", {}), "apps/web-ui/package.json must expose a check script")
    expect(
        "check" in desktop_package.get("scripts", {}),
        "apps/desktop-shell/package.json must expose a check script",
    )

    tauri_config = read_json("apps/desktop-shell/src-tauri/tauri.conf.json")
    build = tauri_config.get("build", {})
    expect(
        build.get("beforeDevCommand") == "npm --prefix ../web-ui run dev -- --host 127.0.0.1 --port 1420",
        "tauri.conf.json beforeDevCommand must target apps/web-ui",
    )
    expect(
        build.get("beforeBuildCommand")
        == "py -3 ../../scripts/prepare_desktop_bundle_resources.py && npm --prefix ../web-ui run build",
        "tauri.conf.json beforeBuildCommand must prepare desktop bundle resources and target apps/web-ui",
    )
    expect(
        build.get("frontendDist") == "../../web-ui/dist",
        "tauri.conf.json frontendDist must resolve to apps/web-ui/dist",
    )

    backend_url = extract_backend_url()
    web_fallback_url = extract_web_fallback_url()
    expect(
        backend_url == web_fallback_url,
        "Desktop host and browser preview must share the same default backend URL",
    )

    print("Desktop execution surface validation passed.")
    print(f"- default backend URL: {backend_url}")
    print("- root workspaces: apps/web-ui, apps/desktop-shell, packages/contracts")
    print("- tauri host build hooks prepare bundle resources and target apps/web-ui")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"Desktop execution surface validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
