from __future__ import annotations

from unity_integration.capability_catalog import UnityCapabilityCatalog
from unity_integration.contracts import UnityEnvironmentConfig, UnityExecutionRequest, UnityExecutionResult


class UnityGuiFallbackBackend:
    name = "gui"

    def __init__(self, environment: UnityEnvironmentConfig) -> None:
        self._environment = environment

    def is_available(self) -> bool:
        return True

    def supports_request(self, request: UnityExecutionRequest) -> bool:
        capability = UnityCapabilityCatalog.get(request.capability)
        return capability.supports_backend(self.name) and bool(capability.gui_macro or capability.gui_fallback_macro)

    def execute(self, request: UnityExecutionRequest) -> UnityExecutionResult:
        capability = UnityCapabilityCatalog.get(request.capability)
        macro = capability.gui_macro or capability.gui_fallback_macro
        return UnityExecutionResult(
            status="manual_validation_required",
            capability=request.capability,
            backend_used=self.name,
            payload={
                "gui_macro": macro,
                "project_root": str(self._environment.project_root),
            },
            warnings=[
                "GUI fallback execution is mediated by the unity-editor profile. "
                "Use `app.main run --profile unity-editor` with a task alias or task file for live GUI actions."
            ],
            manual_validation_required=True,
        )
