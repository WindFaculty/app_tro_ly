import argparse
import json
import sys
from pathlib import Path

import bpy


TEST_BONE_ROTATIONS = {
    "LeftArm": (0.0, 0.0, 0.35),
    "RightArm": (0.0, 0.0, -0.35),
    "LeftUpLeg": (0.2, 0.0, 0.0),
    "RightUpLeg": (-0.15, 0.0, 0.0),
}


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(description="Verify fitted dress scene.")
    parser.add_argument("--input", required=True, help="Input .blend path.")
    parser.add_argument("--dress-name", required=True, help="Dress object name.")
    parser.add_argument("--report", required=True, help="Output JSON report path.")
    return parser.parse_args(argv)


def open_blend(filepath: Path):
    bpy.ops.wm.open_mainfile(filepath=str(filepath))


def sample_vertex_indices(vertex_count: int, sample_size: int = 256):
    if vertex_count <= sample_size:
        return list(range(vertex_count))
    step = max(1, vertex_count // sample_size)
    return list(range(0, vertex_count, step))[:sample_size]


def evaluated_world_vertices(obj, sample_indices):
    depsgraph = bpy.context.evaluated_depsgraph_get()
    evaluated = obj.evaluated_get(depsgraph)
    mesh = evaluated.to_mesh()
    try:
        return [
            list((obj.matrix_world @ mesh.vertices[index].co))
            for index in sample_indices
            if index < len(mesh.vertices)
        ]
    finally:
        evaluated.to_mesh_clear()


def max_and_mean_distance(points_a, points_b):
    distances = []
    for left, right in zip(points_a, points_b):
        dx = left[0] - right[0]
        dy = left[1] - right[1]
        dz = left[2] - right[2]
        distances.append((dx * dx + dy * dy + dz * dz) ** 0.5)

    if not distances:
        return 0.0, 0.0
    return max(distances), sum(distances) / len(distances)


def reset_pose(armature_obj):
    for pose_bone in armature_obj.pose.bones:
        pose_bone.rotation_mode = "XYZ"
        pose_bone.rotation_euler = (0.0, 0.0, 0.0)
        pose_bone.location = (0.0, 0.0, 0.0)
        pose_bone.scale = (1.0, 1.0, 1.0)


def apply_test_pose(armature_obj):
    for bone_name, rotation in TEST_BONE_ROTATIONS.items():
        pose_bone = armature_obj.pose.bones.get(bone_name)
        if pose_bone is None:
            continue
        pose_bone.rotation_mode = "XYZ"
        pose_bone.rotation_euler = rotation


def material_node_summary(material):
    if material is None or not material.use_nodes or material.node_tree is None:
        return {"name": material.name if material else None, "usesNodes": False, "nodes": []}
    return {
        "name": material.name,
        "usesNodes": True,
        "nodes": [node.bl_idname for node in material.node_tree.nodes],
    }


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    report_path = Path(args.report).resolve()

    if not input_path.exists():
        raise RuntimeError(f"Input .blend not found: {input_path}")
    report_path.parent.mkdir(parents=True, exist_ok=True)

    open_blend(input_path)

    dress_obj = bpy.data.objects.get(args.dress_name)
    if dress_obj is None or dress_obj.type != "MESH":
        raise RuntimeError(f"Dress object not found: {args.dress_name}")

    armature_modifier = next((modifier for modifier in dress_obj.modifiers if modifier.type == "ARMATURE"), None)
    if armature_modifier is None or armature_modifier.object is None:
        raise RuntimeError("Dress object is missing an Armature modifier with a target.")

    armature_obj = armature_modifier.object
    sample_indices = sample_vertex_indices(len(dress_obj.data.vertices))

    reset_pose(armature_obj)
    bpy.context.view_layer.update()
    rest_vertices = evaluated_world_vertices(dress_obj, sample_indices)

    apply_test_pose(armature_obj)
    bpy.context.view_layer.update()
    posed_vertices = evaluated_world_vertices(dress_obj, sample_indices)

    max_delta, mean_delta = max_and_mean_distance(rest_vertices, posed_vertices)

    hidden_regions = sorted(
        obj.name
        for obj in bpy.data.objects
        if obj.type == "MESH" and obj.name.startswith("Body_") and obj.hide_viewport
    )

    report = {
        "input": str(input_path),
        "dressObject": dress_obj.name,
        "dressVertexCount": len(dress_obj.data.vertices),
        "dressPolygonCount": len(dress_obj.data.polygons),
        "dressVertexGroupCount": len(dress_obj.vertex_groups),
        "dressMaterialSlots": [slot.material.name if slot.material else None for slot in dress_obj.material_slots],
        "dressMaterialNodeSummary": [material_node_summary(slot.material) for slot in dress_obj.material_slots],
        "armatureTarget": armature_obj.name,
        "armatureBoneCount": len(armature_obj.data.bones),
        "hiddenRegions": hidden_regions,
        "sampleVertexCount": len(rest_vertices),
        "poseVerification": {
            "testedBones": TEST_BONE_ROTATIONS,
            "maxVertexDelta": round(max_delta, 6),
            "meanVertexDelta": round(mean_delta, 6),
            "deformationDetected": mean_delta > 0.0005,
        },
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote dress verification report to: {report_path}")


if __name__ == "__main__":
    main()
