import argparse
import json
import sys
from pathlib import Path

import bpy


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(description="Report current scene/view state from a blend file.")
    parser.add_argument("--input", required=True, help="Input .blend path.")
    parser.add_argument("--report", required=True, help="Output JSON report path.")
    return parser.parse_args(argv)


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    report_path = Path(args.report).resolve()
    report_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.open_mainfile(filepath=str(input_path))

    selected_names = [obj.name for obj in bpy.context.selected_objects]
    active = bpy.context.view_layer.objects.active

    mesh_states = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue
        mesh_states.append(
            {
                "name": obj.name,
                "hiddenViewport": obj.hide_viewport,
                "hiddenRender": obj.hide_render,
                "selected": obj.select_get(),
                "showInFront": obj.show_in_front,
                "displayType": obj.display_type,
            }
        )

    report = {
        "input": str(input_path),
        "mode": bpy.context.mode,
        "activeObject": active.name if active else None,
        "selectedObjects": selected_names,
        "meshStates": mesh_states,
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote scene state report to: {report_path}")


if __name__ == "__main__":
    main()
