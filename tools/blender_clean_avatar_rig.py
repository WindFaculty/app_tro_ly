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

    parser = argparse.ArgumentParser(
        description="Create a non-destructive rig-clean variant of an avatar FBX."
    )
    parser.add_argument("--input", required=True, help="Input FBX path.")
    parser.add_argument("--output", required=True, help="Output FBX path.")
    parser.add_argument("--report", required=True, help="JSON cleanup report path.")
    parser.add_argument("--blend-out", help="Optional .blend output path.")
    parser.add_argument("--armature-name", help="Explicit armature object name.")
    return parser.parse_args(argv)


def normalize_name(name):
    return "".join(ch for ch in name.lower() if ch.isalnum())


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(filepath):
    bpy.ops.import_scene.fbx(filepath=str(filepath))


def find_armature(explicit_name=None):
    if explicit_name:
        obj = bpy.data.objects.get(explicit_name)
        if obj is None:
            raise RuntimeError(f"Could not find armature '{explicit_name}'.")
        if obj.type != "ARMATURE":
            raise RuntimeError(f"Object '{explicit_name}' is '{obj.type}', expected 'ARMATURE'.")
        return obj

    armatures = [obj for obj in bpy.data.objects if obj.type == "ARMATURE"]
    if not armatures:
        raise RuntimeError("No armature found.")
    if len(armatures) == 1:
        return armatures[0]

    for armature in armatures:
        if normalize_name(armature.name) == "armature":
            return armature

    names = ", ".join(obj.name for obj in armatures)
    raise RuntimeError(f"Multiple armatures found ({names}); rerun with --armature-name.")


def find_primary_meshes():
    return [obj for obj in bpy.data.objects if obj.type == "MESH"]


def build_spine_chain(hips_bone):
    chain = []
    cursor = hips_bone
    torso_prefixes = ("spine", "chest", "upperchest", "neck", "head")

    while True:
        torso_children = [
            child
            for child in cursor.children
            if normalize_name(child.name).startswith(torso_prefixes)
        ]
        if not torso_children:
            break

        next_bone = sorted(
            torso_children,
            key=lambda bone: (bone.head_local - cursor.head_local).length,
        )[0]
        chain.append(next_bone)
        cursor = next_bone

        if normalize_name(cursor.name).startswith("head"):
            break

    return chain


def rename_bones_non_destructive(armature_obj):
    armature = armature_obj.data
    bones = armature.bones
    rename_log = []

    hips = None
    for bone in bones:
        if normalize_name(bone.name) in {"hips", "pelvis"}:
            hips = bone
            break

    if hips is not None:
        spine_chain = build_spine_chain(hips)
        torso_chain = [
            bone
            for bone in spine_chain
            if not normalize_name(bone.name).startswith(("neck", "head"))
        ]
        desired_names = ["Spine", "Spine01", "Spine02"]

        if len(torso_chain) >= 3:
            rename_plan = []
            for bone, desired_name in zip(torso_chain[:3], desired_names):
                if bone.name != desired_name:
                    rename_plan.append((bone.name, desired_name))
            apply_rename_plan(armature, rename_plan, rename_log)

    rename_plan = []
    if armature.bones.get("neck") is not None:
        rename_plan.append(("neck", "Neck"))
    apply_rename_plan(armature, rename_plan, rename_log)

    return rename_log


def apply_rename_plan(armature, rename_plan, rename_log):
    if not rename_plan:
        return

    temp_names = {}
    for old_name, _ in rename_plan:
        bone = armature.bones.get(old_name)
        if bone is None:
            continue
        temp_name = f"TMP_{old_name}"
        counter = 1
        while armature.bones.get(temp_name) is not None:
            counter += 1
            temp_name = f"TMP_{old_name}_{counter}"
        bone.name = temp_name
        temp_names[old_name] = temp_name

    for old_name, new_name in rename_plan:
        temp_name = temp_names.get(old_name)
        bone = armature.bones.get(temp_name) if temp_name else None
        if bone is None:
            continue

        if armature.bones.get(new_name) is not None:
            raise RuntimeError(f"Cannot rename '{old_name}' to '{new_name}': target already exists.")

        bone.name = new_name
        rename_log.append({"from": old_name, "to": new_name})


def stabilize_object_names(armature_obj, mesh_objects):
    object_log = []

    if armature_obj.name != "Armature":
        old_name = armature_obj.name
        armature_obj.name = "Armature"
        object_log.append({"from": old_name, "to": armature_obj.name, "type": "ARMATURE_OBJECT"})

    if armature_obj.data.name != "Armature":
        old_name = armature_obj.data.name
        armature_obj.data.name = "Armature"
        object_log.append({"from": old_name, "to": armature_obj.data.name, "type": "ARMATURE_DATA"})

    if len(mesh_objects) == 1 and mesh_objects[0].name != "Body_Base":
        old_name = mesh_objects[0].name
        mesh_objects[0].name = "Body_Base"
        mesh_objects[0].data.name = "Body_Base_Mesh"
        object_log.append({"from": old_name, "to": mesh_objects[0].name, "type": "MESH_OBJECT"})

    return object_log


def disable_known_helper_deforms(armature_obj):
    helper_names = {"head_end", "headfront", "Head_End", "HeadFront"}
    changes = []

    for bone in armature_obj.data.bones:
        if bone.name in helper_names and bone.use_deform:
            bone.use_deform = False
            changes.append(bone.name)

    return changes


def select_for_export(armature_obj, mesh_objects):
    bpy.ops.object.select_all(action="DESELECT")
    armature_obj.select_set(True)
    for mesh in mesh_objects:
        mesh.select_set(True)
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
    report_path = Path(args.report).resolve()
    blend_out = Path(args.blend_out).resolve() if args.blend_out else None

    if not input_path.exists():
        raise RuntimeError(f"Input FBX not found: {input_path}")

    output_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    if blend_out:
        blend_out.parent.mkdir(parents=True, exist_ok=True)

    reset_scene()
    import_fbx(input_path)

    armature_obj = find_armature(args.armature_name)
    mesh_objects = find_primary_meshes()
    if not mesh_objects:
        raise RuntimeError("No mesh objects found after FBX import.")

    rename_log = rename_bones_non_destructive(armature_obj)
    object_log = stabilize_object_names(armature_obj, mesh_objects)
    helper_log = disable_known_helper_deforms(armature_obj)

    if blend_out:
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_out))

    select_for_export(armature_obj, mesh_objects)
    export_fbx(output_path)

    report = {
        "input": str(input_path),
        "output": str(output_path),
        "savedBlendPath": str(blend_out) if blend_out else None,
        "boneRenames": rename_log,
        "objectRenames": object_log,
        "helperBonesSetNonDeform": helper_log,
        "meshCount": len(mesh_objects),
        "meshNames": [mesh.name for mesh in mesh_objects],
        "armatureObject": armature_obj.name,
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote rig cleanup report to: {report_path}")


if __name__ == "__main__":
    main()
