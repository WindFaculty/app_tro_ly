import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Vector


DEFAULT_DRESS_LOCATION = (-0.007639, 0.21904, 0.709701)
DEFAULT_DRESS_ROTATION = (0.0, 0.0, 0.0)
DEFAULT_DRESS_SCALE = (0.791315, 0.791315, 0.791315)
DEFAULT_HIDE_REGIONS = [
    "Body_TorsoUpper",
    "Body_TorsoLower",
    "Body_ThighL",
    "Body_ThighR",
    "Body_CalfL",
    "Body_CalfR",
    "Body_FootL",
    "Body_FootR",
]


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(
        description="Fit a dress using Blender shrinkwrap + weight transfer workflow."
    )
    parser.add_argument("--base-blend", required=True, help="Base avatar .blend path.")
    parser.add_argument("--source-body-blend", required=True, help="Blend file containing Body_Base.")
    parser.add_argument("--dress-fbx", required=True, help="Dress FBX path.")
    parser.add_argument("--output-blend", required=True, help="Output .blend path.")
    parser.add_argument("--report", required=True, help="Output report path.")
    parser.add_argument("--dress-name", default="CHR_Dress_AzureSakura_v001", help="Final dress object name.")
    return parser.parse_args(argv)


def resolve_path(filepath):
    path = Path(filepath).resolve()
    if not path.exists():
        raise RuntimeError(f"Required path not found: {path}")
    return path


def open_base_blend(filepath: Path):
    bpy.ops.wm.open_mainfile(filepath=str(filepath))


def append_object_from_blend(blend_path: Path, object_name: str):
    before = set(bpy.data.objects.keys())
    directory = str(blend_path) + "\\Object\\"
    filepath = directory + object_name
    bpy.ops.wm.append(filepath=filepath, directory=directory, filename=object_name)
    appended = [obj for obj in bpy.data.objects if obj.name not in before]
    for obj in appended:
        if obj.type == "MESH" and obj.name.startswith(object_name):
            return obj
    raise RuntimeError(f"Failed to append object '{object_name}' from {blend_path}")


def import_single_mesh_fbx(filepath: Path):
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=str(filepath))
    imported = [obj for obj in bpy.data.objects if obj.name not in before]
    meshes = [obj for obj in imported if obj.type == "MESH"]
    if len(meshes) != 1:
        names = ", ".join(obj.name for obj in imported)
        raise RuntimeError(f"Expected exactly one imported mesh, found {len(meshes)}. Imported: {names}")
    return meshes[0], imported


def ensure_collection(name: str):
    collection = bpy.data.collections.get(name)
    if collection is None:
        collection = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(collection)
    return collection


def move_object_to_collection(obj, collection):
    for owner in list(obj.users_collection):
        owner.objects.unlink(obj)
    collection.objects.link(obj)


def remove_modifiers(obj):
    for modifier in list(obj.modifiers):
        obj.modifiers.remove(modifier)


def rename_mesh_object(obj, object_name: str, mesh_name: str):
    obj.name = object_name
    if obj.data:
        obj.data.name = mesh_name


def remove_import_helpers(imported_objects, keep_obj):
    for obj in imported_objects:
        if obj == keep_obj:
            continue
        if obj.type == "EMPTY":
            bpy.data.objects.remove(obj, do_unlink=True)


def ensure_armature():
    armature = bpy.data.objects.get("Armature")
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Base scene does not contain armature object 'Armature'.")
    return armature


def object_world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    xs = [point.x for point in points]
    ys = [point.y for point in points]
    zs = [point.z for point in points]
    return {
        "min": Vector((min(xs), min(ys), min(zs))),
        "max": Vector((max(xs), max(ys), max(zs))),
    }


def world_bbox(obj):
    bounds = object_world_bounds(obj)
    min_corner = bounds["min"]
    max_corner = bounds["max"]
    return {
        "min": [round(min_corner.x, 6), round(min_corner.y, 6), round(min_corner.z, 6)],
        "max": [round(max_corner.x, 6), round(max_corner.y, 6), round(max_corner.z, 6)],
    }


def set_transform(obj, location, rotation, scale):
    obj.location = location
    obj.rotation_euler = rotation
    obj.scale = scale


def apply_object_scale(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


def set_smooth_shading(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.shade_smooth()


def create_dress_material(dress_obj, dress_fbx_path: Path, material_name: str):
    texture_dir = dress_fbx_path.parent
    texture_stem = dress_fbx_path.stem

    color_path = texture_dir / f"{texture_stem}.png"
    normal_path = texture_dir / f"{texture_stem}_normal.png"
    roughness_path = texture_dir / f"{texture_stem}_roughness.png"
    metallic_path = texture_dir / f"{texture_stem}_metallic.png"

    if not color_path.exists():
        raise RuntimeError(f"Base color texture not found: {color_path}")

    material = bpy.data.materials.get(material_name)
    if material is None:
        material = bpy.data.materials.new(name=material_name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()

    output = nodes.new(type="ShaderNodeOutputMaterial")
    output.location = (900, 0)

    principled = nodes.new(type="ShaderNodeBsdfPrincipled")
    principled.location = (600, 0)
    principled.inputs["Specular IOR Level"].default_value = 0.2
    links.new(principled.outputs["BSDF"], output.inputs["Surface"])

    color_tex = nodes.new(type="ShaderNodeTexImage")
    color_tex.location = (0, 200)
    color_tex.image = bpy.data.images.load(str(color_path), check_existing=True)
    color_tex.image.colorspace_settings.name = "sRGB"
    links.new(color_tex.outputs["Color"], principled.inputs["Base Color"])

    if roughness_path.exists():
        roughness_tex = nodes.new(type="ShaderNodeTexImage")
        roughness_tex.location = (0, -20)
        roughness_tex.image = bpy.data.images.load(str(roughness_path), check_existing=True)
        roughness_tex.image.colorspace_settings.name = "Non-Color"
        links.new(roughness_tex.outputs["Color"], principled.inputs["Roughness"])

    if metallic_path.exists():
        metallic_tex = nodes.new(type="ShaderNodeTexImage")
        metallic_tex.location = (0, -240)
        metallic_tex.image = bpy.data.images.load(str(metallic_path), check_existing=True)
        metallic_tex.image.colorspace_settings.name = "Non-Color"
        links.new(metallic_tex.outputs["Color"], principled.inputs["Metallic"])

    if normal_path.exists():
        normal_tex = nodes.new(type="ShaderNodeTexImage")
        normal_tex.location = (0, -460)
        normal_tex.image = bpy.data.images.load(str(normal_path), check_existing=True)
        normal_tex.image.colorspace_settings.name = "Non-Color"

        normal_map = nodes.new(type="ShaderNodeNormalMap")
        normal_map.location = (300, -460)
        links.new(normal_tex.outputs["Color"], normal_map.inputs["Color"])
        links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])

    if dress_obj.data.materials:
        dress_obj.data.materials[0] = material
    else:
        dress_obj.data.materials.append(material)

    return material


def ensure_vertex_group(obj, group_name):
    group = obj.vertex_groups.get(group_name)
    if group is None:
        group = obj.vertex_groups.new(name=group_name)
    return group


def build_fit_groups(dress_obj):
    bounds = object_world_bounds(dress_obj)
    min_corner = bounds["min"]
    max_corner = bounds["max"]
    height = max_corner.y - min_corner.y
    width = max_corner.x - min_corner.x
    center_x = (min_corner.x + max_corner.x) * 0.5

    group_specs = {
        "Fit_Neckline": {"count": 0},
        "Fit_ShoulderCap": {"count": 0},
        "Fit_Armhole": {"count": 0},
        "Fit_TorsoUpper": {"count": 0},
        "Fit_UpperArm": {"count": 0},
        "Fit_SleeveRoot": {"count": 0},
    }
    groups = {name: ensure_vertex_group(dress_obj, name) for name in group_specs}

    neckline_y = min_corner.y + height * 0.77
    shoulder_y_min = min_corner.y + height * 0.70
    shoulder_y_max = min_corner.y + height * 0.89
    armhole_y_min = min_corner.y + height * 0.63
    armhole_y_max = min_corner.y + height * 0.84
    torso_y_min = min_corner.y + height * 0.50
    torso_y_max = min_corner.y + height * 0.82
    upper_arm_y_min = min_corner.y + height * 0.56
    upper_arm_y_max = min_corner.y + height * 0.78
    sleeve_y_min = min_corner.y + height * 0.48
    sleeve_y_max = min_corner.y + height * 0.76
    neckline_half_width = width * 0.17
    shoulder_x_min = width * 0.10
    shoulder_x_max = width * 0.31
    armhole_x_min = width * 0.20
    armhole_x_max = width * 0.34
    torso_half_width = width * 0.23
    upper_arm_x_min = width * 0.20
    upper_arm_x_max = width * 0.36
    sleeve_x_min = width * 0.16
    sleeve_x_max = width * 0.36

    for vertex in dress_obj.data.vertices:
        world_pos = dress_obj.matrix_world @ vertex.co
        x_offset = abs(world_pos.x - center_x)

        if world_pos.y >= neckline_y and x_offset <= neckline_half_width:
            weight = 1.0 - x_offset / max(neckline_half_width, 1e-5)
            groups["Fit_Neckline"].add([vertex.index], max(0.0, min(weight, 1.0)), "REPLACE")
            group_specs["Fit_Neckline"]["count"] += 1

        if shoulder_y_min <= world_pos.y <= shoulder_y_max and shoulder_x_min <= x_offset <= shoulder_x_max:
            y_weight = 1.0 - abs(world_pos.y - (shoulder_y_min + shoulder_y_max) * 0.5) / max((shoulder_y_max - shoulder_y_min) * 0.5, 1e-5)
            x_weight = 1.0 - abs(x_offset - (shoulder_x_min + shoulder_x_max) * 0.5) / max((shoulder_x_max - shoulder_x_min) * 0.5, 1e-5)
            weight = max(0.0, min(y_weight, x_weight))
            if weight > 0.02:
                groups["Fit_ShoulderCap"].add([vertex.index], weight, "REPLACE")
                group_specs["Fit_ShoulderCap"]["count"] += 1

        if armhole_y_min <= world_pos.y <= armhole_y_max and armhole_x_min <= x_offset <= armhole_x_max:
            y_weight = 1.0 - abs(world_pos.y - (armhole_y_min + armhole_y_max) * 0.5) / max((armhole_y_max - armhole_y_min) * 0.5, 1e-5)
            x_weight = 1.0 - abs(x_offset - (armhole_x_min + armhole_x_max) * 0.5) / max((armhole_x_max - armhole_x_min) * 0.5, 1e-5)
            weight = max(0.0, min(y_weight, x_weight))
            if weight > 0.02:
                groups["Fit_Armhole"].add([vertex.index], weight, "REPLACE")
                group_specs["Fit_Armhole"]["count"] += 1

        if torso_y_min <= world_pos.y <= torso_y_max and x_offset <= torso_half_width:
            y_weight = 1.0 - abs(world_pos.y - (torso_y_min + torso_y_max) * 0.5) / max((torso_y_max - torso_y_min) * 0.5, 1e-5)
            x_weight = 1.0 - x_offset / max(torso_half_width, 1e-5)
            weight = max(0.0, min(y_weight, x_weight))
            if weight > 0.02:
                groups["Fit_TorsoUpper"].add([vertex.index], weight, "REPLACE")
                group_specs["Fit_TorsoUpper"]["count"] += 1

        if upper_arm_y_min <= world_pos.y <= upper_arm_y_max and upper_arm_x_min <= x_offset <= upper_arm_x_max:
            y_weight = 1.0 - abs(world_pos.y - (upper_arm_y_min + upper_arm_y_max) * 0.5) / max((upper_arm_y_max - upper_arm_y_min) * 0.5, 1e-5)
            x_weight = 1.0 - abs(x_offset - (upper_arm_x_min + upper_arm_x_max) * 0.5) / max((upper_arm_x_max - upper_arm_x_min) * 0.5, 1e-5)
            weight = max(0.0, min(y_weight, x_weight))
            if weight > 0.02:
                groups["Fit_UpperArm"].add([vertex.index], weight, "REPLACE")
                group_specs["Fit_UpperArm"]["count"] += 1

        if sleeve_y_min <= world_pos.y <= sleeve_y_max and sleeve_x_min <= x_offset <= sleeve_x_max:
            y_weight = 1.0 - abs(world_pos.y - (sleeve_y_min + sleeve_y_max) * 0.5) / max((sleeve_y_max - sleeve_y_min) * 0.5, 1e-5)
            x_weight = 1.0 - abs(x_offset - (sleeve_x_min + sleeve_x_max) * 0.5) / max((sleeve_x_max - sleeve_x_min) * 0.5, 1e-5)
            weight = max(0.0, min(y_weight, x_weight))
            if weight > 0.02:
                groups["Fit_SleeveRoot"].add([vertex.index], weight, "REPLACE")
                group_specs["Fit_SleeveRoot"]["count"] += 1

    return {name: {"vertexCount": spec["count"]} for name, spec in group_specs.items()}


def apply_shrinkwrap(dress_obj, target_obj, group_name, modifier_name, offset):
    modifier = dress_obj.modifiers.new(name=modifier_name, type="SHRINKWRAP")
    modifier.target = target_obj
    modifier.wrap_method = "NEAREST_SURFACEPOINT"
    modifier.wrap_mode = "OUTSIDE"
    modifier.offset = offset
    modifier.vertex_group = group_name

    bpy.ops.object.select_all(action="DESELECT")
    dress_obj.select_set(True)
    bpy.context.view_layer.objects.active = dress_obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def apply_smooth(dress_obj, group_name, factor, iterations, modifier_name):
    modifier = dress_obj.modifiers.new(name=modifier_name, type="SMOOTH")
    modifier.factor = factor
    modifier.iterations = iterations
    modifier.vertex_group = group_name

    bpy.ops.object.select_all(action="DESELECT")
    dress_obj.select_set(True)
    bpy.context.view_layer.objects.active = dress_obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def create_vertex_groups_like_source(target_obj, source_obj):
    existing = {group.name for group in target_obj.vertex_groups}
    for source_group in source_obj.vertex_groups:
        if source_group.name not in existing:
            target_obj.vertex_groups.new(name=source_group.name)


def transfer_weights(source_obj, target_obj):
    create_vertex_groups_like_source(target_obj, source_obj)
    modifier = target_obj.modifiers.new(name="TransferWeights", type="DATA_TRANSFER")
    modifier.object = source_obj
    modifier.use_vert_data = True
    modifier.data_types_verts = {"VGROUP_WEIGHTS"}
    modifier.vert_mapping = "POLYINTERP_NEAREST"
    modifier.layers_vgroup_select_src = "ALL"
    modifier.layers_vgroup_select_dst = "NAME"
    modifier.mix_mode = "REPLACE"
    modifier.mix_factor = 1.0

    bpy.ops.object.select_all(action="DESELECT")
    target_obj.select_set(True)
    bpy.context.view_layer.objects.active = target_obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def normalize_vertex_groups(target_obj):
    bpy.ops.object.select_all(action="DESELECT")
    target_obj.select_set(True)
    bpy.context.view_layer.objects.active = target_obj
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.vertex_group_clean(group_select_mode="ALL", limit=0.0001)
    bpy.ops.object.vertex_group_limit_total(group_select_mode="ALL", limit=4)
    bpy.ops.object.vertex_group_normalize_all(group_select_mode="ALL", lock_active=False)


def add_armature_modifier(target_obj, armature_obj):
    modifier = target_obj.modifiers.new(name="Armature", type="ARMATURE")
    modifier.object = armature_obj
    modifier.use_vertex_groups = True


def hide_body_regions(region_names):
    hidden = []
    for name in region_names:
        obj = bpy.data.objects.get(name)
        if obj is None:
            continue
        obj.hide_viewport = True
        obj.hide_render = True
        hidden.append(name)
    return hidden


def finalize_scene_state():
    for obj in bpy.data.objects:
        obj.select_set(False)
    bpy.context.view_layer.objects.active = None


def save_blend(filepath: Path):
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(filepath))


def main():
    args = parse_args()
    base_blend = resolve_path(args.base_blend)
    source_body_blend = resolve_path(args.source_body_blend)
    dress_fbx = resolve_path(args.dress_fbx)
    output_blend = Path(args.output_blend).resolve()
    report_path = Path(args.report).resolve()

    output_blend.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    open_base_blend(base_blend)
    armature_obj = ensure_armature()

    source_collection = ensure_collection("SourceHelpers")
    outfit_collection = ensure_collection("Wardrobe")

    source_body = append_object_from_blend(source_body_blend, "Body_Base")
    rename_mesh_object(source_body, "Body_Source", "Body_Source_Mesh")
    remove_modifiers(source_body)
    move_object_to_collection(source_body, source_collection)
    source_body.hide_viewport = True
    source_body.hide_render = True

    dress_obj, imported_objects = import_single_mesh_fbx(dress_fbx)
    rename_mesh_object(dress_obj, args.dress_name, f"{args.dress_name}_Mesh")
    move_object_to_collection(dress_obj, outfit_collection)
    remove_import_helpers(imported_objects, dress_obj)
    remove_modifiers(dress_obj)

    set_transform(dress_obj, DEFAULT_DRESS_LOCATION, DEFAULT_DRESS_ROTATION, DEFAULT_DRESS_SCALE)
    apply_object_scale(dress_obj)
    set_smooth_shading(dress_obj)
    material = create_dress_material(dress_obj, dress_fbx, "MAT_Dress_AzureSakura_v001")

    fit_groups = build_fit_groups(dress_obj)
    apply_shrinkwrap(dress_obj, source_body, "Fit_Neckline", "ShrinkwrapNeckline", offset=0.006)
    apply_smooth(dress_obj, "Fit_Neckline", factor=0.35, iterations=6, modifier_name="SmoothNeckline")
    apply_shrinkwrap(dress_obj, source_body, "Fit_ShoulderCap", "ShrinkwrapShoulderCap", offset=0.0035)
    apply_smooth(dress_obj, "Fit_ShoulderCap", factor=0.36, iterations=10, modifier_name="SmoothShoulderCap")
    apply_shrinkwrap(dress_obj, source_body, "Fit_Armhole", "ShrinkwrapArmhole", offset=0.003)
    apply_smooth(dress_obj, "Fit_Armhole", factor=0.34, iterations=10, modifier_name="SmoothArmhole")
    apply_shrinkwrap(dress_obj, source_body, "Fit_TorsoUpper", "ShrinkwrapTorso", offset=0.008)
    apply_smooth(dress_obj, "Fit_TorsoUpper", factor=0.30, iterations=5, modifier_name="SmoothTorso")
    apply_shrinkwrap(dress_obj, source_body, "Fit_UpperArm", "ShrinkwrapUpperArm", offset=0.0055)
    apply_smooth(dress_obj, "Fit_UpperArm", factor=0.34, iterations=10, modifier_name="SmoothUpperArm")
    apply_shrinkwrap(dress_obj, source_body, "Fit_SleeveRoot", "ShrinkwrapSleeveRoot", offset=0.010)
    apply_smooth(dress_obj, "Fit_SleeveRoot", factor=0.28, iterations=7, modifier_name="SmoothSleeveRoot")

    transfer_weights(source_body, dress_obj)
    normalize_vertex_groups(dress_obj)
    bpy.data.objects.remove(source_body, do_unlink=True)

    add_armature_modifier(dress_obj, armature_obj)
    hidden_regions = hide_body_regions(DEFAULT_HIDE_REGIONS)
    finalize_scene_state()

    bpy.context.view_layer.update()
    save_blend(output_blend)

    report = {
        "workflow": [
            "Import base avatar scene",
            "Import dress mesh",
            "Shrinkwrap neckline/torso/sleeve-root to avatar body",
            "Smooth fitted regions",
            "Transfer vertex weights from avatar body",
            "Bind dress to avatar armature",
            "Hide body regions under dress",
        ],
        "baseBlend": str(base_blend),
        "sourceBodyBlend": str(source_body_blend),
        "dressFbx": str(dress_fbx),
        "outputBlend": str(output_blend),
        "dressObject": dress_obj.name,
        "dressMesh": dress_obj.data.name if dress_obj.data else None,
        "dressVertexCount": len(dress_obj.data.vertices) if dress_obj.data else 0,
        "dressPolygonCount": len(dress_obj.data.polygons) if dress_obj.data else 0,
        "dressMaterial": material.name,
        "dressVertexGroupCount": len(dress_obj.vertex_groups),
        "armatureTarget": armature_obj.name,
        "hiddenBodyRegions": hidden_regions,
        "dressLocation": [round(value, 6) for value in dress_obj.location],
        "dressRotationEuler": [round(value, 6) for value in dress_obj.rotation_euler],
        "dressScale": [round(value, 6) for value in dress_obj.scale],
        "dressWorldBounds": world_bbox(dress_obj),
        "fitGroups": fit_groups,
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote shrinkwrap workflow report to: {report_path}")
    print(f"Saved fitted dress scene to: {output_blend}")


if __name__ == "__main__":
    main()
