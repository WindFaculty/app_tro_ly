from __future__ import annotations

import json
from pathlib import Path

from orchestrator.models import (
    AgentSpec,
    EvalSpec,
    PlatformCatalog,
    RulePackSpec,
    SkillSpec,
    ToolPolicySpec,
    WorkflowSpec,
)


def _default_ai_root() -> Path:
    return Path(__file__).resolve().parents[2]


def load_platform_catalog(ai_root: Path | None = None) -> PlatformCatalog:
    root = ai_root or _default_ai_root()
    source_path = root / "control-plane" / "catalog" / "platform.json"
    payload = json.loads(source_path.read_text(encoding="utf-8"))

    catalog = PlatformCatalog(
        root=root,
        source_path=source_path,
        version=int(payload["version"]),
        agents={item["id"]: AgentSpec(**item) for item in payload.get("agents", [])},
        skills={item["id"]: SkillSpec(**item) for item in payload.get("skills", [])},
        rule_packs={item["id"]: RulePackSpec(**item) for item in payload.get("rule_packs", [])},
        tool_policies={item["id"]: ToolPolicySpec(**item) for item in payload.get("tool_policies", [])},
        evals={item["id"]: EvalSpec(**item) for item in payload.get("evals", [])},
        workflows={item["id"]: WorkflowSpec(**item) for item in payload.get("workflows", [])},
    )
    _validate_catalog(catalog)
    return catalog


def _validate_catalog(catalog: PlatformCatalog) -> None:
    for agent in catalog.agents.values():
        prompt_path = catalog.resolve(agent.prompt)
        if not prompt_path.exists():
            raise FileNotFoundError(f"Missing prompt for agent '{agent.id}': {prompt_path}")

    for rule_pack in catalog.rule_packs.values():
        for policy_path in rule_pack.policy_paths:
            resolved = catalog.resolve(policy_path)
            if not resolved.exists():
                raise FileNotFoundError(f"Missing policy for rule pack '{rule_pack.id}': {resolved}")

    for workflow in catalog.workflows.values():
        catalog.require_agent(workflow.planner_agent)
        catalog.require_agent(workflow.executor_agent)
        catalog.require_agent(workflow.review_agent)
        catalog.require_agent(workflow.verifier_agent)
        for agent_id in workflow.supporting_agents:
            catalog.require_agent(agent_id)
        for skill_id in workflow.skills:
            catalog.require_skill(skill_id)
        for rule_pack_id in workflow.rule_packs:
            catalog.require_rule_pack(rule_pack_id)
        catalog.require_tool_policy(workflow.tool_policy)
        catalog.require_eval(workflow.eval)

        entrypoint = catalog.resolve(workflow.entrypoint)
        if not entrypoint.exists():
            raise FileNotFoundError(f"Missing workflow entrypoint for '{workflow.id}': {entrypoint}")
