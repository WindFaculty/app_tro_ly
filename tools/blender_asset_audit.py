import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(description="Audit imported FBX/Blend scene objects.")
    parser.add_argument("--input", required=True, help="Input asset path.")
    parser.add_argument("--report", required=True, help="Output JSON report path.")
    return parser.parse_args(argv)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_asset(filepath: Path):
    suffix = filepath.suffix.lower()
    if suffix == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(filepath))
        return
    if suffix == ".blend":
        bpy.ops.wm.open_mainfile(filepath=str(filepath))
        return
    raise RuntimeError(f"Unsupported file type: {filepath.suffix}")


def vector_to_list(value: Vector):
    return [round(component, 6) for component in value]


def collect_mesh_report(obj):
    materials = [slot.material.name if slot.material else None for slot in obj.material_slots]
    armature_targets = []
    for modifier in obj.modifiers:
        if modifier.type == "ARMATURE":
            armature_targets.append(modifier.object.name if modifier.object else None)

    return {
        "name": obj.name,
        "dataName": obj.data.name if obj.data else None,
        "vertexCount": len(obj.data.vertices),
        "polygonCount": len(obj.data.polygons),
        "materialSlots": materials,
        "vertexGroupCount": len(obj.vertex_groups),
        "vertexGroupsSample": [group.name for group in list(obj.vertex_groups)[:30]],
        "modifiers": [
            {
                "name": modifier.name,
                "type": modifier.type,
                "target": modifier.object.name if hasattr(modifier, "object") and modifier.object else None,
            }
            for modifier in obj.modifiers
        ],
        "armatureTargets": armature_targets,
        "location": vector_to_list(obj.location),
        "rotationEuler": vector_to_list(obj.rotation_euler),
        "scale": vector_to_list(obj.scale),
        "dimensions": vector_to_list(obj.dimensions),
        "boundBoxMin": vector_to_list(
            Vector((min(c[0] for c in obj.bound_box), min(c[1] for c in obj.bound_box), min(c[2] for c in obj.bound_box)))
        ),
        "boundBoxMax": vector_to_list(
            Vector((max(c[0] for c in obj.bound_box), max(c[1] for c in obj.bound_box), max(c[2] for c in obj.bound_box)))
        ),
    }


def collect_armature_report(obj):
    deform_bones = [bone.name for bone in obj.data.bones if bone.use_deform]
    return {
        "name": obj.name,
        "dataName": obj.data.name if obj.data else None,
        "boneCount": len(obj.data.bones),
        "deformBoneCount": len(deform_bones),
        "bonesSample": deform_bones[:40],
        "location": vector_to_list(obj.location),
        "rotationEuler": vector_to_list(obj.rotation_euler),
        "scale": vector_to_list(obj.scale),
        "dimensions": vector_to_list(obj.dimensions),
    }


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    report_path = Path(args.report).resolve()

    if not input_path.exists():
        raise RuntimeError(f"Input asset not found: {input_path}")

    report_path.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    import_asset(input_path)

    meshes = [obj for obj in bpy.data.objects if obj.type == "MESH"]
    armatures = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
    materials = sorted(material.name for material in bpy.data.materials)
    images = sorted(image.name for image in bpy.data.images)

    report = {
        "input": str(input_path),
        "sceneObjectCount": len(bpy.data.objects),
        "meshCount": len(meshes),
        "armatureCount": len(armatures),
        "meshes": [collect_mesh_report(obj) for obj in meshes],
        "armatures": [collect_armature_report(obj) for obj in armatures],
        "materials": materials,
        "images": images,
    }

    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote asset audit to: {report_path}")


if __name__ == "__main__":
    main()
