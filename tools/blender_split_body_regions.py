import argparse
import json
import sys
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy


BODY_REGION_BONE_MAP = {
    "Head": ["Head", "Neck", "neck"],
    "TorsoUpper": ["Spine", "Spine01", "Spine02"],
    "TorsoLower": ["Hips"],
    "ArmUpperL": ["LeftShoulder", "LeftArm"],
    "ForearmL": ["LeftForeArm"],
    "HandL": ["LeftHand"],
    "ArmUpperR": ["RightShoulder", "RightArm"],
    "ForearmR": ["RightForeArm"],
    "HandR": ["RightHand"],
    "ThighL": ["LeftUpLeg"],
    "CalfL": ["LeftLeg"],
    "FootL": ["LeftFoot", "LeftToeBase"],
    "ThighR": ["RightUpLeg"],
    "CalfR": ["RightLeg"],
    "FootR": ["RightFoot", "RightToeBase"],
}


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(
        description="Split a single skinned avatar mesh into 15 body-region meshes."
    )
    parser.add_argument("--input", required=True, help="Input FBX path.")
    parser.add_argument("--output", required=True, help="Output FBX path.")
    parser.add_argument("--report", help="Optional JSON report path.")
    parser.add_argument("--blend", help="Optional .blend output path.")
    parser.add_argument("--mesh-name", help="Mesh object name to split.")
    parser.add_argument("--armature-name", help="Armature object name to use.")
    return parser.parse_args(argv)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(filepath):
    bpy.ops.import_scene.fbx(filepath=str(filepath))


def find_single_object(object_type, explicit_name=None):
    if explicit_name:
        obj = bpy.data.objects.get(explicit_name)
        if obj is None:
            raise RuntimeError(f"Could not find {object_type} named '{explicit_name}'.")
        if obj.type != object_type:
            raise RuntimeError(
                f"Object '{explicit_name}' is type '{obj.type}', expected '{object_type}'."
            )
        return obj

    matches = [obj for obj in bpy.data.objects if obj.type == object_type]
    if not matches:
        raise RuntimeError(f"No {object_type} object found after import.")
    if len(matches) > 1:
        names = ", ".join(obj.name for obj in matches)
        raise RuntimeError(
            f"Found multiple {object_type} objects ({names}). Re-run with --{object_type.lower()}-name."
        )
    return matches[0]


def build_group_region_lookup(mesh_obj):
    group_to_region = {}
    missing_bones = []
    for region, bone_names in BODY_REGION_BONE_MAP.items():
        for bone_name in bone_names:
            group = mesh_obj.vertex_groups.get(bone_name)
            if group is None:
                missing_bones.append(bone_name)
                continue
            group_to_region[group.index] = region

    if missing_bones:
        missing_list = ", ".join(sorted(set(missing_bones)))
        print(f"Warning: missing mapped vertex groups: {missing_list}")

    return group_to_region


def build_vertex_region_scores(mesh_obj, group_to_region):
    vertex_scores = []
    for vertex in mesh_obj.data.vertices:
        scores = defaultdict(float)
        for group in vertex.groups:
            region = group_to_region.get(group.group)
            if region is not None:
                scores[region] += group.weight
        if not scores:
            scores["TorsoUpper"] = 1.0
        vertex_scores.append(scores)
    return vertex_scores


def classify_polygons(mesh_obj, vertex_scores):
    polygon_regions = {}
    region_face_counts = defaultdict(int)

    for polygon in mesh_obj.data.polygons:
        scores = defaultdict(float)
        for vertex_index in polygon.vertices:
            for region, weight in vertex_scores[vertex_index].items():
                scores[region] += weight

        region = max(sorted(scores), key=lambda item: scores[item])
        polygon_regions[polygon.index] = region
        region_face_counts[region] += 1

    return polygon_regions, dict(region_face_counts)


def trim_object_to_region(source_obj, region_name, polygon_regions):
    region_obj = source_obj.copy()
    region_obj.data = source_obj.data.copy()
    region_obj.name = f"Body_{region_name}"
    region_obj.data.name = f"Body_{region_name}_Mesh"
    source_obj.users_collection[0].objects.link(region_obj)

    bm = bmesh.new()
    bm.from_mesh(region_obj.data)
    bm.faces.ensure_lookup_table()
    bm.faces.index_update()

    faces_to_delete = [
        face for face in bm.faces if polygon_regions.get(face.index) != region_name
    ]
    if faces_to_delete:
        bmesh.ops.delete(bm, geom=faces_to_delete, context="FACES")

    bm.to_mesh(region_obj.data)
    bm.free()
    region_obj.data.update()

    return region_obj


def collect_region_stats(region_obj):
    non_zero_weights = 0
    for vertex in region_obj.data.vertices:
        non_zero_weights += sum(1 for group in vertex.groups if group.weight > 0.0)

    return {
        "vertices": len(region_obj.data.vertices),
        "polygons": len(region_obj.data.polygons),
        "weight_assignments": non_zero_weights,
    }


def select_for_export(armature_obj, region_objects):
    bpy.ops.object.select_all(action="DESELECT")
    armature_obj.select_set(True)
    for region_obj in region_objects:
        region_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj


def export_fbx(filepath):
    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        mesh_smooth_type="FACE",
    )


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()
    report_path = Path(args.report).resolve() if args.report else None
    blend_path = Path(args.blend).resolve() if args.blend else None

    if not input_path.exists():
        raise RuntimeError(f"Input FBX not found: {input_path}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    if report_path:
        report_path.parent.mkdir(parents=True, exist_ok=True)
    if blend_path:
        blend_path.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    import_fbx(input_path)

    armature_obj = find_single_object("ARMATURE", args.armature_name)
    mesh_obj = find_single_object("MESH", args.mesh_name)

    print(f"Imported mesh '{mesh_obj.name}' with {len(mesh_obj.data.polygons)} polygons.")
    print(f"Using armature '{armature_obj.name}'.")

    group_to_region = build_group_region_lookup(mesh_obj)
    vertex_scores = build_vertex_region_scores(mesh_obj, group_to_region)
    polygon_regions, region_face_counts = classify_polygons(mesh_obj, vertex_scores)

    region_objects = []
    region_stats = {}
    for region_name in BODY_REGION_BONE_MAP:
        region_obj = trim_object_to_region(mesh_obj, region_name, polygon_regions)
        stats = collect_region_stats(region_obj)
        if stats["polygons"] == 0:
            bpy.data.objects.remove(region_obj, do_unlink=True)
            print(f"Skipped empty region '{region_name}'.")
            continue
        region_objects.append(region_obj)
        region_stats[region_name] = stats
        print(
            f"Created {region_obj.name}: "
            f"{stats['vertices']} verts, {stats['polygons']} polys."
        )

    bpy.data.objects.remove(mesh_obj, do_unlink=True)

    select_for_export(armature_obj, region_objects)
    export_fbx(output_path)
    print(f"Exported split avatar FBX to: {output_path}")

    if blend_path:
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
        print(f"Saved Blender file to: {blend_path}")

    report = {
        "input": str(input_path),
        "output": str(output_path),
        "armature": armature_obj.name,
        "regions": region_stats,
        "polygonRegionCounts": region_face_counts,
        "boneMap": BODY_REGION_BONE_MAP,
    }
    if report_path:
        report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(f"Wrote split report to: {report_path}")


if __name__ == "__main__":
    main()
