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

    parser = argparse.ArgumentParser(description="Fit a dress mesh onto the base avatar scene.")
    parser.add_argument("--base-blend", required=True, help="Base avatar .blend path.")
    parser.add_argument("--source-body-blend", required=True, help="Blend file containing Body_Base.")
    parser.add_argument("--dress-fbx", required=True, help="Dress FBX path.")
    parser.add_argument("--clothy-helper-fbx", help="Optional Clothy3D helper FBX used to improve dress fitting.")
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


def import_dress(filepath: Path):
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=str(filepath))
    imported = [obj for obj in bpy.data.objects if obj.name not in before]
    meshes = [obj for obj in imported if obj.type == "MESH"]
    if len(meshes) != 1:
        names = ", ".join(obj.name for obj in imported)
        raise RuntimeError(f"Expected exactly one imported dress mesh, found {len(meshes)}. Imported: {names}")
    return meshes[0], imported


def import_single_mesh_fbx(filepath: Path):
    before = set(bpy.data.objects.keys())
    bpy.ops.import_scene.fbx(filepath=str(filepath))
    imported = [obj for obj in bpy.data.objects if obj.name not in before]
    meshes = [obj for obj in imported if obj.type == "MESH"]
    if len(meshes) != 1:
        names = ", ".join(obj.name for obj in imported)
        raise RuntimeError(f"Expected exactly one imported helper mesh, found {len(meshes)}. Imported: {names}")
    return meshes[0], imported


def append_object_from_blend(blend_path: Path, object_name: str):
    before = set(bpy.data.objects.keys())
    directory = str(blend_path) + "\\Object\\"
    filepath = directory + object_name
    bpy.ops.wm.append(
        filepath=filepath,
        directory=directory,
        filename=object_name,
    )
    appended = [obj for obj in bpy.data.objects if obj.name not in before]
    for obj in appended:
        if obj.type == "MESH" and obj.name.startswith(object_name):
            return obj
    raise RuntimeError(f"Failed to append object '{object_name}' from {blend_path}")


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


def rename_dress_object(dress_obj, dress_name: str):
    dress_obj.name = dress_name
    if dress_obj.data:
        dress_obj.data.name = f"{dress_name}_Mesh"


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
        normal_map.inputs["Strength"].default_value = 1.0

        links.new(normal_tex.outputs["Color"], normal_map.inputs["Color"])
        links.new(normal_map.outputs["Normal"], principled.inputs["Normal"])

    if dress_obj.data.materials:
        dress_obj.data.materials[0] = material
    else:
        dress_obj.data.materials.append(material)

    return material


def apply_object_scale(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)


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


def set_transform(target_obj, location, rotation, scale):
    target_obj.location = location
    target_obj.rotation_euler = rotation
    target_obj.scale = scale


def set_smooth_shading(target_obj):
    bpy.ops.object.select_all(action="DESELECT")
    target_obj.select_set(True)
    bpy.context.view_layer.objects.active = target_obj
    bpy.ops.object.shade_smooth()


def hide_body_regions(region_names):
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name.startswith("Body_") and obj.name != "Body_Source":
            obj.hide_viewport = False
            obj.hide_render = False

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


def ensure_armature():
    armature = bpy.data.objects.get("Armature")
    if armature is None or armature.type != "ARMATURE":
        raise RuntimeError("Base scene does not contain armature object 'Armature'.")
    return armature


def remove_import_helpers(imported_objects, keep_obj):
    for obj in imported_objects:
        if obj == keep_obj:
            continue
        if obj.type == "EMPTY":
            bpy.data.objects.remove(obj, do_unlink=True)


def object_world_bounds(obj):
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    xs = [point.x for point in points]
    ys = [point.y for point in points]
    zs = [point.z for point in points]
    return {
        "min": Vector((min(xs), min(ys), min(zs))),
        "max": Vector((max(xs), max(ys), max(zs))),
    }


def align_helper_to_source(helper_obj, source_obj):
    helper_obj.rotation_euler = source_obj.rotation_euler.copy()
    helper_obj.location = (0.0, 0.0, 0.0)
    helper_obj.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.update()

    helper_bounds = object_world_bounds(helper_obj)
    source_bounds = object_world_bounds(source_obj)
    helper_size = helper_bounds["max"] - helper_bounds["min"]
    source_size = source_bounds["max"] - source_bounds["min"]

    scale = Vector(
        (
            source_size.x / helper_size.x if helper_size.x else 1.0,
            source_size.y / helper_size.y if helper_size.y else 1.0,
            source_size.z / helper_size.z if helper_size.z else 1.0,
        )
    )
    helper_obj.scale = scale
    bpy.context.view_layer.update()

    helper_bounds = object_world_bounds(helper_obj)
    helper_center = (helper_bounds["min"] + helper_bounds["max"]) * 0.5
    source_center = (source_bounds["min"] + source_bounds["max"]) * 0.5
    helper_obj.location += source_center - helper_center
    bpy.context.view_layer.update()

    return {
        "scale": [round(value, 6) for value in helper_obj.scale],
        "location": [round(value, 6) for value in helper_obj.location],
    }


def build_clothy_fit_vertex_group(dress_obj, group_name="ClothyFitZone"):
    bbox = object_world_bounds(dress_obj)
    min_corner = bbox["min"]
    max_corner = bbox["max"]
    height = max_corner.y - min_corner.y
    width = max_corner.x - min_corner.x
    center_x = (min_corner.x + max_corner.x) * 0.5
    upper_threshold = min_corner.y + height * 0.52
    side_threshold = width * 0.19
    lower_blend = min_corner.y + height * 0.34

    group = dress_obj.vertex_groups.get(group_name)
    if group is None:
        group = dress_obj.vertex_groups.new(name=group_name)

    assigned = 0
    for vertex in dress_obj.data.vertices:
        world_pos = dress_obj.matrix_world @ vertex.co
        upper_weight = 0.0
        if world_pos.y >= upper_threshold:
            upper_weight = 1.0
        elif world_pos.y > lower_blend:
            upper_weight = (world_pos.y - lower_blend) / max(upper_threshold - lower_blend, 1e-5)

        side_weight = 0.0
        x_offset = abs(world_pos.x - center_x)
        if x_offset >= side_threshold and world_pos.y >= lower_blend:
            side_weight = min(1.0, (x_offset - side_threshold) / max(width * 0.12, 1e-5) + 0.35)

        weight = max(upper_weight, side_weight)
        if weight > 0.001:
            group.add([vertex.index], min(weight, 1.0), "REPLACE")
            assigned += 1

    return group.name, assigned


def apply_clothy_shrinkwrap(dress_obj, helper_obj, vertex_group_name):
    modifier = dress_obj.modifiers.new(name="ClothyFit", type="SHRINKWRAP")
    modifier.target = helper_obj
    modifier.wrap_method = "NEAREST_SURFACEPOINT"
    modifier.wrap_mode = "OUTSIDE"
    modifier.offset = 0.005
    modifier.vertex_group = vertex_group_name

    bpy.ops.object.select_all(action="DESELECT")
    dress_obj.select_set(True)
    bpy.context.view_layer.objects.active = dress_obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def ensure_vertex_group(obj, group_name):
    group = obj.vertex_groups.get(group_name)
    if group is None:
        group = obj.vertex_groups.new(name=group_name)
    return group


def remove_vertex_group(obj, group_name):
    group = obj.vertex_groups.get(group_name)
    if group is not None:
        obj.vertex_groups.remove(group)


def build_neck_shoulder_fit_group(dress_obj, group_name="Fit_NeckShoulder"):
    remove_vertex_group(dress_obj, group_name)
    bbox = object_world_bounds(dress_obj)
    min_corner = bbox["min"]
    max_corner = bbox["max"]
    height = max_corner.y - min_corner.y
    width = max_corner.x - min_corner.x
    depth = max_corner.z - min_corner.z
    center_x = (min_corner.x + max_corner.x) * 0.5
    front_z = max_corner.z - depth * 0.40

    shoulder_start = min_corner.y + height * 0.74
    shoulder_full = min_corner.y + height * 0.86
    neck_start = min_corner.y + height * 0.80
    inner_half = width * 0.17
    outer_half = width * 0.31

    group = ensure_vertex_group(dress_obj, group_name)
    assigned = 0
    for vertex in dress_obj.data.vertices:
        world_pos = dress_obj.matrix_world @ vertex.co
        vertical = 0.0
        if world_pos.y >= shoulder_start:
            vertical = min(1.0, (world_pos.y - shoulder_start) / max(shoulder_full - shoulder_start, 1e-5))

        neck_weight = 0.0
        x_offset = abs(world_pos.x - center_x)
        if world_pos.y >= neck_start and x_offset <= outer_half:
            neck_weight = 1.0 - max(0.0, x_offset - inner_half) / max(outer_half - inner_half, 1e-5)

        shoulder_weight = 0.0
        if world_pos.y >= shoulder_start and x_offset <= outer_half:
            shoulder_weight = max(0.0, 1.0 - x_offset / max(outer_half, 1e-5)) * 0.7 + vertical * 0.5

        front_bias = 1.0 if world_pos.z >= front_z else 0.75
        weight = max(neck_weight, shoulder_weight, vertical * 0.75) * front_bias
        if weight > 0.02:
            group.add([vertex.index], min(weight, 1.0), "REPLACE")
            assigned += 1

    return group.name, assigned


def build_arm_root_fit_group(dress_obj, group_name="Fit_ArmRoot"):
    remove_vertex_group(dress_obj, group_name)
    bbox = object_world_bounds(dress_obj)
    min_corner = bbox["min"]
    max_corner = bbox["max"]
    height = max_corner.y - min_corner.y
    width = max_corner.x - min_corner.x
    center_x = (min_corner.x + max_corner.x) * 0.5

    y_min = min_corner.y + height * 0.50
    y_peak = min_corner.y + height * 0.74
    side_inner = width * 0.16
    side_outer = width * 0.40

    group = ensure_vertex_group(dress_obj, group_name)
    assigned = 0
    for vertex in dress_obj.data.vertices:
        world_pos = dress_obj.matrix_world @ vertex.co
        if world_pos.y < y_min or world_pos.y > y_peak:
            continue

        x_offset = abs(world_pos.x - center_x)
        if x_offset < side_inner or x_offset > side_outer:
            continue

        y_weight = 1.0 - abs(world_pos.y - (y_min + y_peak) * 0.5) / max((y_peak - y_min) * 0.5, 1e-5)
        x_weight = 1.0 - abs(x_offset - (side_inner + side_outer) * 0.5) / max((side_outer - side_inner) * 0.5, 1e-5)
        weight = max(0.0, min(y_weight, x_weight))
        if weight > 0.02:
            group.add([vertex.index], min(weight, 1.0), "REPLACE")
            assigned += 1

    return group.name, assigned


def apply_targeted_shrinkwrap(dress_obj, target_obj, vertex_group_name, modifier_name, offset):
    modifier = dress_obj.modifiers.new(name=modifier_name, type="SHRINKWRAP")
    modifier.target = target_obj
    modifier.wrap_method = "NEAREST_SURFACEPOINT"
    modifier.wrap_mode = "OUTSIDE"
    modifier.offset = offset
    modifier.vertex_group = vertex_group_name

    bpy.ops.object.select_all(action="DESELECT")
    dress_obj.select_set(True)
    bpy.context.view_layer.objects.active = dress_obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def apply_smooth_modifier(dress_obj, vertex_group_name, factor, iterations, modifier_name):
    modifier = dress_obj.modifiers.new(name=modifier_name, type="SMOOTH")
    modifier.factor = factor
    modifier.iterations = iterations
    modifier.vertex_group = vertex_group_name

    bpy.ops.object.select_all(action="DESELECT")
    dress_obj.select_set(True)
    bpy.context.view_layer.objects.active = dress_obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def world_bbox(obj):
    bounds = object_world_bounds(obj)
    min_corner = bounds["min"]
    max_corner = bounds["max"]
    return {
        "min": [round(min_corner.x, 6), round(min_corner.y, 6), round(min_corner.z, 6)],
        "max": [round(max_corner.x, 6), round(max_corner.y, 6), round(max_corner.z, 6)],
    }


def save_blend(filepath: Path):
    bpy.ops.file.pack_all()
    bpy.ops.wm.save_as_mainfile(filepath=str(filepath))


def main():
    args = parse_args()
    base_blend = resolve_path(args.base_blend)
    source_body_blend = resolve_path(args.source_body_blend)
    dress_fbx = resolve_path(args.dress_fbx)
    clothy_helper_fbx = resolve_path(args.clothy_helper_fbx) if args.clothy_helper_fbx else None
    output_blend = Path(args.output_blend).resolve()
    report_path = Path(args.report).resolve()

    output_blend.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)

    open_base_blend(base_blend)
    armature_obj = ensure_armature()

    source_collection = ensure_collection("SourceHelpers")
    outfit_collection = ensure_collection("Wardrobe")

    source_body = append_object_from_blend(source_body_blend, "Body_Base")
    source_body.name = "Body_Source"
    if source_body.data:
        source_body.data.name = "Body_Source_Mesh"
    remove_modifiers(source_body)
    source_body.hide_viewport = True
    source_body.hide_render = True
    move_object_to_collection(source_body, source_collection)

    dress_obj, imported_objects = import_dress(dress_fbx)
    rename_dress_object(dress_obj, args.dress_name)
    move_object_to_collection(dress_obj, outfit_collection)
    remove_import_helpers(imported_objects, dress_obj)
    remove_modifiers(dress_obj)

    set_transform(dress_obj, DEFAULT_DRESS_LOCATION, DEFAULT_DRESS_ROTATION, DEFAULT_DRESS_SCALE)
    apply_object_scale(dress_obj)
    set_smooth_shading(dress_obj)

    material = create_dress_material(dress_obj, dress_fbx, "MAT_Dress_AzureSakura_v001")
    clothy_report = None
    polish_report = None
    if clothy_helper_fbx is not None:
        clothy_helper, clothy_imported = import_single_mesh_fbx(clothy_helper_fbx)
        clothy_helper.name = "Clothy_Helper"
        if clothy_helper.data:
            clothy_helper.data.name = "Clothy_Helper_Mesh"
        move_object_to_collection(clothy_helper, source_collection)
        remove_import_helpers(clothy_imported, clothy_helper)
        clothy_report = align_helper_to_source(clothy_helper, source_body)
        fit_group_name, fit_vertex_count = build_clothy_fit_vertex_group(dress_obj)
        apply_clothy_shrinkwrap(dress_obj, clothy_helper, fit_group_name)
        clothy_report["helperObject"] = clothy_helper.name
        clothy_report["fitVertexGroup"] = fit_group_name
        clothy_report["fitVertexCount"] = fit_vertex_count
        bpy.data.objects.remove(clothy_helper, do_unlink=True)

    neck_group_name, neck_vertex_count = build_neck_shoulder_fit_group(dress_obj)
    arm_group_name, arm_vertex_count = build_arm_root_fit_group(dress_obj)
    apply_targeted_shrinkwrap(
        dress_obj,
        source_body,
        neck_group_name,
        "FitNeckShoulder",
        offset=0.0075,
    )
    apply_smooth_modifier(
        dress_obj,
        neck_group_name,
        factor=0.42,
        iterations=8,
        modifier_name="SmoothNeckShoulder",
    )
    apply_targeted_shrinkwrap(
        dress_obj,
        source_body,
        arm_group_name,
        "FitArmRoot",
        offset=0.0105,
    )
    apply_smooth_modifier(
        dress_obj,
        arm_group_name,
        factor=0.38,
        iterations=10,
        modifier_name="SmoothArmRoot",
    )
    polish_report = {
        "neckShoulderGroup": neck_group_name,
        "neckShoulderVertexCount": neck_vertex_count,
        "armRootGroup": arm_group_name,
        "armRootVertexCount": arm_vertex_count,
        "bodyHideProfile": "KimonoLong_Polish",
    }

    transfer_weights(source_body, dress_obj)
    normalize_vertex_groups(dress_obj)
    bpy.data.objects.remove(source_body, do_unlink=True)
    add_armature_modifier(dress_obj, armature_obj)
    hidden_regions = hide_body_regions(DEFAULT_HIDE_REGIONS)
    finalize_scene_state()

    bpy.context.view_layer.update()
    save_blend(output_blend)

    report = {
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
        "clothyHelperFit": clothy_report,
        "polishFit": polish_report,
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote dress fit report to: {report_path}")
    print(f"Saved fitted dress scene to: {output_blend}")


if __name__ == "__main__":
    main()
