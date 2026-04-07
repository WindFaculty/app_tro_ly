from __future__ import annotations

from app.unity.surfaces import UnitySurfaceMap, UnitySurfaceSpec
try:
    from app.unity.assertions import UnityAssertionRunner
except ModuleNotFoundError:  # pragma: no cover - depends on optional runtime deps
    UnityAssertionRunner = None  # type: ignore[assignment]

try:
    from app.unity.capabilities import UnityCapabilityRegistry, UnityCapabilitySpec
except ModuleNotFoundError:  # pragma: no cover - depends on optional runtime deps
    UnityCapabilityRegistry = None  # type: ignore[assignment]
    UnityCapabilitySpec = None  # type: ignore[assignment]

try:
    from app.unity.macros import UnityMacroRegistry, UnityMacroSpec
except ModuleNotFoundError:  # pragma: no cover - depends on optional runtime deps
    UnityMacroRegistry = None  # type: ignore[assignment]
    UnityMacroSpec = None  # type: ignore[assignment]

try:
    from app.unity.mcp_runtime import UnityMcpRuntime
except ModuleNotFoundError:  # pragma: no cover - depends on optional runtime deps
    UnityMcpRuntime = None  # type: ignore[assignment]

try:
    from app.unity.preflight import UnityPreflight
except ModuleNotFoundError:  # pragma: no cover - depends on optional runtime deps
    UnityPreflight = None  # type: ignore[assignment]

try:
    from app.unity.task_planner import UnityTaskPlanner
except ModuleNotFoundError:  # pragma: no cover - depends on optional runtime deps
    UnityTaskPlanner = None  # type: ignore[assignment]

__all__ = [
    "UnityAssertionRunner",
    "UnityCapabilityRegistry",
    "UnityCapabilitySpec",
    "UnityMacroRegistry",
    "UnityMacroSpec",
    "UnityMcpRuntime",
    "UnityPreflight",
    "UnitySurfaceMap",
    "UnitySurfaceSpec",
    "UnityTaskPlanner",
]
