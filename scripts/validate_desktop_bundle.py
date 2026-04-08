from __future__ import annotations

import json
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent.parent


def read_json(relative_path: str) -> dict:
    return json.loads((REPO_ROOT / relative_path).read_text(encoding="utf-8"))


def read_text(relative_path: str) -> str:
    return (REPO_ROOT / relative_path).read_text(encoding="utf-8")


def expect(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def main() -> int:
    desktop_package = read_json("apps/desktop-shell/package.json")
    desktop_scripts = desktop_package.get("scripts", {})
    expect(
        desktop_scripts.get("bundle:prepare") == "py -3 ../../scripts/prepare_desktop_bundle_resources.py",
        "apps/desktop-shell/package.json must expose bundle:prepare",
    )
    expect(
        desktop_scripts.get("check")
        == "py -3 ../../scripts/validate_desktop_bundle.py && cargo check --manifest-path src-tauri/Cargo.toml",
        "apps/desktop-shell/package.json check script must validate the desktop bundle before cargo check",
    )

    tauri_config = read_json("apps/desktop-shell/src-tauri/tauri.conf.json")
    security = tauri_config.get("app", {}).get("security", {})
    csp = security.get("csp")
    expect(isinstance(csp, str) and csp.strip(), "tauri.conf.json must define a non-empty desktop CSP")
    for fragment in [
        "default-src 'self'",
        "connect-src 'self' ipc: http://127.0.0.1:8096 ws://127.0.0.1:8096",
        "object-src 'none'",
        "frame-ancestors 'none'",
    ]:
        expect(fragment in csp, f"Desktop CSP must include {fragment!r}")

    bundle = tauri_config.get("bundle", {})
    expect(bundle.get("active") is True, "Desktop bundle must stay active")
    expect(bundle.get("targets") == ["nsis", "msi"], "Desktop bundle targets must stay on nsis and msi")
    expect(
        bundle.get("resources")
        == {
            "../.tauri-bundle-resources/local-backend/": "local-backend/",
            "../.tauri-bundle-resources/ai-dev-system/domain/customization/contracts/": "ai-dev-system/domain/customization/contracts/",
            "../.tauri-bundle-resources/ai-dev-system/domain/customization/sample-data/": "ai-dev-system/domain/customization/sample-data/",
        },
        "Desktop bundle resources must stage backend runtime plus customization contracts",
    )
    expect(
        tauri_config.get("plugins") == {"shell": {"open": True}},
        "Only the shell open plugin surface should remain enabled for the desktop shell",
    )

    capabilities = read_json("apps/desktop-shell/src-tauri/capabilities/default.json")
    permissions = capabilities.get("permissions", [])
    expect("shell:default" in permissions, "Desktop shell must keep shell:default for safe external open")
    expect("log:default" in permissions, "Desktop shell must keep log:default")
    expect("shell:allow-spawn" not in permissions, "Desktop shell must not expose shell:allow-spawn")
    expect("shell:allow-execute" not in permissions, "Desktop shell must not expose shell:allow-execute")
    expect(all(not permission.startswith("process:") for permission in permissions), "Desktop shell must not expose process plugin permissions")

    cargo_toml = read_text("apps/desktop-shell/src-tauri/Cargo.toml")
    expect("tauri-plugin-process" not in cargo_toml, "Desktop shell must not depend on tauri-plugin-process")

    backend_source = read_text("apps/desktop-shell/src-tauri/src/backend.rs")
    expect('join("local-backend")' in backend_source, "Desktop backend path must resolve to bundled local-backend resources")
    expect(
        'join("resources").join("local-backend")' not in backend_source,
        "Desktop backend must not prepend an extra resources directory in packaged mode",
    )
    for env_key in [
        "ASSISTANT_DATA_DIR",
        "ASSISTANT_DB_PATH",
        "ASSISTANT_AUDIO_DIR",
        "ASSISTANT_CACHE_DIR",
        "ASSISTANT_LOG_DIR",
        "PYTHONDONTWRITEBYTECODE",
        "PYTHONUTF8",
        "APP_TRO_LY_DESKTOP_RUNTIME",
        "CREATE_NO_WINDOW",
    ]:
        expect(env_key in backend_source, f"Desktop backend hardening is missing {env_key}")

    unity_source = read_text("apps/desktop-shell/src-tauri/src/unity_runtime.rs")
    expect('join("unity-runtime")' in unity_source, "Unity packaged runtime lookup must target bundled unity-runtime resources")
    expect(
        'join("resources").join("unity-runtime")' not in unity_source,
        "Unity packaged runtime lookup must not prepend an extra resources directory",
    )

    globals_css = read_text("apps/web-ui/src/styles/globals.css")
    expect("fonts.googleapis.com" not in globals_css, "Desktop shell must not depend on remote Google font imports")

    print("Desktop bundle validation passed.")
    print("- desktop bundle stages backend runtime and customization contracts")
    print("- Tauri security surface is narrowed to shell open plus CSP allowlist")
    print("- packaged runtime resolves bundled backend resources and host-owned data paths")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"Desktop bundle validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
