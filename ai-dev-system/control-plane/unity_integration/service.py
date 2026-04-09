from __future__ import annotations

from typing import Any, Callable

from unity_integration.backends.cli_loop import UnityCliLoopBackend
from unity_integration.backends.gui_fallback import UnityGuiFallbackBackend
from unity_integration.backends.mcp_unity import UnityMcpBackend, UnityMcpClient, UnityMcpRuntime
from unity_integration.capability_catalog import UnityCapabilityCatalog
from unity_integration.contracts import UnityEnvironmentConfig, UnityExecutionRequest, UnityExecutionResult
from unity_integration.environment import UnityIntegrationEnvironment


class UnityIntegrationService:
    def __init__(
        self,
        *,
        environment: UnityEnvironmentConfig | None = None,
        backends: list[Any] | None = None,
    ) -> None:
        self._environment = environment or UnityIntegrationEnvironment().probe()
        self._backends = backends or [
            UnityCliLoopBackend(self._environment),
            UnityMcpBackend(self._environment),
            UnityGuiFallbackBackend(self._environment),
        ]
        self._backend_map = {backend.name: backend for backend in self._backends}

    @property
    def environment(self) -> UnityEnvironmentConfig:
        return self._environment

    def list_capabilities(self, *, profile: str = "unity-editor") -> dict[str, Any]:
        return {
            "profile": profile,
            "project_root": str(self._environment.project_root),
            "environment": self._environment.to_dict(),
            "capabilities": UnityCapabilityCatalog.build_matrix(self._environment),
        }

    def mcp_runtime(self) -> UnityMcpRuntime:
        return UnityMcpRuntime(environment=self._environment)

    def mcp_client_factory(self) -> Callable[[Any], UnityMcpClient]:
        return lambda repo_root: UnityMcpClient(repo_root=repo_root, environment=self._environment)

    def execute(self, request: UnityExecutionRequest) -> UnityExecutionResult:
        backend = self._resolve_backend(request)
        return backend.execute(request)

    def _resolve_backend(self, request: UnityExecutionRequest):
        capability = UnityCapabilityCatalog.get(request.capability)
        preferred_order = self._build_backend_order(request, capability)
        for backend_name in preferred_order:
            backend = self._backend_map.get(backend_name)
            if backend is None:
                continue
            if not backend.is_available():
                continue
            if not backend.supports_request(request):
                continue
            return backend
        raise RuntimeError(
            f"No Unity integration backend can execute capability '{request.capability}'. "
            f"Requested backend='{request.backend_preference}', supported={list(capability.supported_backends)}."
        )

    @staticmethod
    def _build_backend_order(request: UnityExecutionRequest, capability) -> list[str]:
        if request.backend_preference != "auto":
            order = [request.backend_preference]
            if request.allow_fallback:
                order.extend(backend for backend in capability.supported_backends if backend != request.backend_preference)
            return order

        order = []
        if capability.preferred_backend:
            order.append(capability.preferred_backend)
        order.extend(backend for backend in capability.supported_backends if backend not in order)
        return order
