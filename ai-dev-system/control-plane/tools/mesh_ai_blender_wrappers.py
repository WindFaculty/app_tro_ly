from __future__ import annotations

from dataclasses import dataclass, field
from pathlib import Path
from typing import Any


BLENDER_EXECUTABLE_HINT = "blender"


@dataclass(slots=True)
class BlenderWrapperSpec:
    step_id: str
    stage: str
    step_type: str
    description: str
    script_path: str | None = None
    args: dict[str, str] = field(default_factory=dict)
    outputs: dict[str, str] = field(default_factory=dict)
    manual_gate: str | None = None
    notes: list[str] = field(default_factory=list)

    def to_dict(self, repo_root: Path) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "step_id": self.step_id,
            "stage": self.stage,
            "step_type": self.step_type,
            "description": self.description,
            "outputs": dict(self.outputs),
            "notes": list(self.notes),
        }

        if self.step_type == "manual_gate":
            payload["manual_gate"] = self.manual_gate
            return payload

        if not self.script_path:
            raise ValueError(f"Step '{self.step_id}' is missing script_path.")

        script_absolute = (repo_root / self.script_path).resolve()
        argv = [
            BLENDER_EXECUTABLE_HINT,
            "--background",
            "--python",
            str(script_absolute),
            "--",
        ]
        for key, value in self.args.items():
            argv.extend([f"--{key}", value])

        payload.update(
            {
                "script_path": self.script_path,
                "script_absolute_path": str(script_absolute),
                "command_spec": {
                    "runner": "blender_cli",
                    "executable_hint": BLENDER_EXECUTABLE_HINT,
                    "argv": argv,
                },
                "args": dict(self.args),
            }
        )
        return payload


def render_wrapper(step_template: dict[str, Any], context: dict[str, Any], repo_root: Path) -> dict[str, Any]:
    step_type = str(step_template["step_type"])
    if step_type == "manual_gate":
        wrapper = BlenderWrapperSpec(
            step_id=str(step_template["step_id"]),
            stage=str(step_template["stage"]),
            step_type=step_type,
            description=str(step_template["description"]),
            manual_gate=str(step_template["manual_gate"]),
        )
        return wrapper.to_dict(repo_root)

    args = _render_argument_map(step_template.get("args") or {}, context)
    optional_args = _render_argument_map(step_template.get("optional_args") or {}, context, allow_empty=True)
    for key, value in optional_args.items():
        if value:
            args[key] = value

    outputs = _render_argument_map(step_template.get("outputs") or {}, context)
    wrapper = BlenderWrapperSpec(
        step_id=str(step_template["step_id"]),
        stage=str(step_template["stage"]),
        step_type=step_type,
        description=str(step_template["description"]),
        script_path=str(step_template["script_path"]),
        args=args,
        outputs=outputs,
        notes=list(step_template.get("notes") or []),
    )
    return wrapper.to_dict(repo_root)


def _render_argument_map(
    payload: dict[str, Any],
    context: dict[str, Any],
    *,
    allow_empty: bool = False,
) -> dict[str, str]:
    rendered: dict[str, str] = {}
    for key, template in payload.items():
        value = str(template).format(**context)
        if not value and allow_empty:
            continue
        rendered[str(key)] = value
    return rendered
