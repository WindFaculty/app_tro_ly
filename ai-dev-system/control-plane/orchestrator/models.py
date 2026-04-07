from __future__ import annotations

from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
from enum import StrEnum
from pathlib import Path
from typing import Any


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


class TaskLifecycleStatus(StrEnum):
    QUEUED = "queued"
    PLANNING = "planning"
    READY = "ready"
    EXECUTING = "executing"
    REVIEWING = "reviewing"
    VERIFYING = "verifying"
    DONE = "done"
    BLOCKED = "blocked"
    FAILED = "failed"
    REPLANNING = "replanning"


@dataclass(slots=True)
class AgentSpec:
    id: str
    name: str
    summary: str
    prompt: str
    responsibilities: list[str] = field(default_factory=list)
    outputs: list[str] = field(default_factory=list)
    codex: dict[str, Any] = field(default_factory=dict)
    antigravity: dict[str, Any] = field(default_factory=dict)


@dataclass(slots=True)
class SkillSpec:
    id: str
    name: str
    summary: str
    triggers: list[str] = field(default_factory=list)
    checklist: list[str] = field(default_factory=list)
    outputs: list[str] = field(default_factory=list)
    openai: dict[str, Any] = field(default_factory=dict)


@dataclass(slots=True)
class RulePackSpec:
    id: str
    name: str
    summary: str
    policy_paths: list[str] = field(default_factory=list)
    rules: list[str] = field(default_factory=list)


@dataclass(slots=True)
class ToolPolicySpec:
    id: str
    name: str
    summary: str
    allowed_tools: list[str] = field(default_factory=list)
    blocked_patterns: list[str] = field(default_factory=list)
    review_required_for: list[str] = field(default_factory=list)


@dataclass(slots=True)
class EvalSpec:
    id: str
    name: str
    summary: str
    blocking_failure_categories: list[str] = field(default_factory=list)
    blocking_verification_statuses: list[str] = field(default_factory=list)
    required_artifacts: list[str] = field(default_factory=list)


@dataclass(slots=True)
class WorkflowSpec:
    id: str
    name: str
    summary: str
    entrypoint: str
    planner_agent: str
    executor_agent: str
    review_agent: str
    verifier_agent: str
    supporting_agents: list[str] = field(default_factory=list)
    skills: list[str] = field(default_factory=list)
    rule_packs: list[str] = field(default_factory=list)
    tool_policy: str = ""
    eval: str = ""
    phases: list[str] = field(default_factory=list)
    side_states: list[str] = field(default_factory=list)
    max_replans: int = 0
    supported_goals: list[str] = field(default_factory=list)


@dataclass(slots=True)
class PlatformCatalog:
    root: Path
    source_path: Path
    version: int
    agents: dict[str, AgentSpec]
    skills: dict[str, SkillSpec]
    rule_packs: dict[str, RulePackSpec]
    tool_policies: dict[str, ToolPolicySpec]
    evals: dict[str, EvalSpec]
    workflows: dict[str, WorkflowSpec]

    def resolve(self, relative_path: str) -> Path:
        return self.root / relative_path

    def require_agent(self, agent_id: str) -> AgentSpec:
        return self.agents[agent_id]

    def require_skill(self, skill_id: str) -> SkillSpec:
        return self.skills[skill_id]

    def require_rule_pack(self, rule_pack_id: str) -> RulePackSpec:
        return self.rule_packs[rule_pack_id]

    def require_tool_policy(self, tool_policy_id: str) -> ToolPolicySpec:
        return self.tool_policies[tool_policy_id]

    def require_eval(self, eval_id: str) -> EvalSpec:
        return self.evals[eval_id]

    def require_workflow(self, workflow_id: str) -> WorkflowSpec:
        return self.workflows[workflow_id]


@dataclass(slots=True)
class LifecycleTransition:
    status: str
    reason: str | None = None
    details: dict[str, Any] = field(default_factory=dict)
    timestamp: str = field(default_factory=utc_now_iso)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(slots=True)
class ReviewFinding:
    category: str
    severity: str
    summary: str
    step_id: str | None = None
    evidence: dict[str, Any] = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


@dataclass(slots=True)
class ReviewSummary:
    status: str
    findings: list[ReviewFinding] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return {
            "status": self.status,
            "findings": [finding.to_dict() for finding in self.findings],
            "notes": list(self.notes),
        }


@dataclass(slots=True)
class VerificationSummary:
    passed: bool
    blocked_by: list[str] = field(default_factory=list)
    retryable: bool = False
    blocking_step_id: str | None = None
    required_artifacts: list[str] = field(default_factory=list)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)
