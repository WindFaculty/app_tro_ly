import argparse
import json
import sys
from pathlib import Path

import bpy


EXPECTED_REGIONS = [
    "Head",
    "TorsoUpper",
    "TorsoLower",
    "ArmUpperL",
    "ForearmL",
    "HandL",
    "ArmUpperR",
    "ForearmR",
    "HandR",
    "ThighL",
    "CalfL",
    "FootL",
    "ThighR",
    "CalfR",
    "FootR",
]


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(
        description="Validate that a split avatar FBX contains the expected 15 region meshes."
    )
    parser.add_argument("--input", required=True, help="Split FBX path.")
    parser.add_argument("--report", required=True, help="JSON report output path.")
    parser.add_argument("--source-polygons", type=int, default=123272, help="Expected polygon count from source mesh.")
    return parser.parse_args(argv)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(filepath):
    bpy.ops.import_scene.fbx(filepath=str(filepath))


def collect_mesh_report(mesh_obj, armature_name):
    armature_modifiers = [
        modifier.object.name
        for modifier in mesh_obj.modifiers
        if modifier.type == "ARMATURE" and modifier.object is not None
    ]

    group_names = [group.name for group in mesh_obj.vertex_groups]
    head_neck_groups = []
    if mesh_obj.name == "Body_Head":
        head_neck_groups = [name for name in group_names if name in {"neck", "Neck"}]

    return {
        "name": mesh_obj.name,
        "vertices": len(mesh_obj.data.vertices),
        "polygons": len(mesh_obj.data.polygons),
        "parent": mesh_obj.parent.name if mesh_obj.parent else None,
        "armatureModifiers": armature_modifiers,
        "boundToArmature": mesh_obj.parent is not None and mesh_obj.parent.name == armature_name,
        "vertexGroups": group_names,
        "headNeckGroups": head_neck_groups,
    }


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    report_path = Path(args.report).resolve()

    if not input_path.exists():
        raise RuntimeError(f"Input FBX not found: {input_path}")

    report_path.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    import_fbx(input_path)

    armatures = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]

    if len(armatures) != 1:
        raise RuntimeError(f"Expected exactly 1 armature, found {len(armatures)}.")

    armature = armatures[0]
    mesh_reports = [collect_mesh_report(mesh, armature.name) for mesh in sorted(meshes, key=lambda obj: obj.name)]
    mesh_names = [mesh["name"] for mesh in mesh_reports]
    expected_mesh_names = [f"Body_{region}" for region in EXPECTED_REGIONS]

    missing_meshes = sorted(set(expected_mesh_names) - set(mesh_names))
    extra_meshes = sorted(set(mesh_names) - set(expected_mesh_names))
    total_polygons = sum(mesh["polygons"] for mesh in mesh_reports)

    head_mesh = next((mesh for mesh in mesh_reports if mesh["name"] == "Body_Head"), None)
    head_has_neck_group = bool(head_mesh and head_mesh["headNeckGroups"])

    report = {
        "input": str(input_path),
        "armature": armature.name,
        "meshCount": len(mesh_reports),
        "expectedMeshCount": len(EXPECTED_REGIONS),
        "meshNames": mesh_names,
        "missingMeshes": missing_meshes,
        "extraMeshes": extra_meshes,
        "totalPolygons": total_polygons,
        "expectedSourcePolygons": args.source_polygons,
        "polygonCountMatchesSource": total_polygons == args.source_polygons,
        "allMeshesBoundToArmature": all(mesh["boundToArmature"] for mesh in mesh_reports),
        "headHasNeckVertexGroup": head_has_neck_group,
        "meshReports": mesh_reports,
        "success": (
            len(mesh_reports) == len(EXPECTED_REGIONS)
            and not missing_meshes
            and not extra_meshes
            and total_polygons == args.source_polygons
            and all(mesh["boundToArmature"] for mesh in mesh_reports)
            and head_has_neck_group
        ),
    }

    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote split-avatar validation report to: {report_path}")


if __name__ == "__main__":
    main()
