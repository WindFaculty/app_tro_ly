from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


@dataclass(frozen=True, slots=True)
class UnityArtifactRecord:
    name: str
    path: str
    kind: str = "file"
    description: str | None = None

    def to_dict(self) -> dict[str, Any]:
        return {
            "name": self.name,
            "path": self.path,
            "kind": self.kind,
            "description": self.description,
        }


@dataclass(frozen=True, slots=True)
class UnityCapabilityDefinition:
    capability: str
    category: str
    description: str
    supported_backends: tuple[str, ...]
    preferred_backend: str
    mcp_tool_name: str | None = None
    mcp_default_params: dict[str, Any] = field(default_factory=dict)
    gui_macro: str | None = None
    gui_fallback_macro: str | None = None
    cli_loop_command: str | None = None
    fallbackable: bool = False
    manual_validation_required: bool = False
    legacy_aliases: tuple[str, ...] = field(default_factory=tuple)

    def supports_backend(self, backend: str) -> bool:
        return backend in self.supported_backends


@dataclass(frozen=True, slots=True)
class UnityEnvironmentConfig:
    repo_root: Path
    ai_dev_root: Path
    project_root: Path
    log_root: Path
    editor_path: Path | None = None
    package_manifest_path: Path | None = None
    cli_loop_command: tuple[str, ...] = field(default_factory=tuple)
    cli_loop_installed: bool = False
    cli_loop_package_installed: bool = False
    unity_mcp_command: tuple[str, ...] = field(default_factory=tuple)
    unity_mcp_available: bool = False
    unity_mcp_env: dict[str, str] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {
            "repo_root": str(self.repo_root),
            "ai_dev_root": str(self.ai_dev_root),
            "project_root": str(self.project_root),
            "log_root": str(self.log_root),
            "editor_path": str(self.editor_path) if self.editor_path else None,
            "package_manifest_path": str(self.package_manifest_path) if self.package_manifest_path else None,
            "cli_loop_command": list(self.cli_loop_command),
            "cli_loop_installed": self.cli_loop_installed,
            "cli_loop_package_installed": self.cli_loop_package_installed,
            "unity_mcp_command": list(self.unity_mcp_command),
            "unity_mcp_available": self.unity_mcp_available,
            "unity_mcp_env": dict(self.unity_mcp_env),
        }


@dataclass(frozen=True, slots=True)
class UnityExecutionRequest:
    profile: str
    capability: str
    params: dict[str, Any] = field(default_factory=dict)
    backend_preference: str = "auto"
    allow_fallback: bool = True
    evidence_policy: str = "artifact-first"
    confirm_destructive: bool = False
    execution: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return {
            "profile": self.profile,
            "capability": self.capability,
            "params": dict(self.params),
            "backend_preference": self.backend_preference,
            "allow_fallback": self.allow_fallback,
            "evidence_policy": self.evidence_policy,
            "confirm_destructive": self.confirm_destructive,
            "execution": dict(self.execution),
        }


@dataclass(frozen=True, slots=True)
class UnityExecutionResult:
    status: str
    capability: str
    backend_used: str
    payload: dict[str, Any] = field(default_factory=dict)
    artifacts: list[UnityArtifactRecord] = field(default_factory=list)
    verification_summary: dict[str, Any] = field(default_factory=dict)
    warnings: list[str] = field(default_factory=list)
    manual_validation_required: bool = False

    def to_dict(self) -> dict[str, Any]:
        return {
            "status": self.status,
            "capability": self.capability,
            "backend_used": self.backend_used,
            "payload": dict(self.payload),
            "artifacts": [artifact.to_dict() for artifact in self.artifacts],
            "verification_summary": dict(self.verification_summary),
            "warnings": list(self.warnings),
            "manual_validation_required": self.manual_validation_required,
        }
