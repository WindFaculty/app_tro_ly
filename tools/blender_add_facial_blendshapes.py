import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


REQUIRED_SHAPE_KEYS = [
    "Blink_L",
    "Blink_R",
    "SmileEye_L",
    "SmileEye_R",
    "WideEye_L",
    "WideEye_R",
    "BrowUp_L",
    "BrowUp_R",
    "BrowDown_L",
    "BrowDown_R",
    "BrowInnerUp",
    "Smile",
    "Sad",
    "Surprise",
    "AngryLight",
    "MouthOpen",
    "MouthNarrow",
    "MouthWide",
    "MouthRound",
    "Viseme_Rest",
    "Viseme_AA",
    "Viseme_E",
    "Viseme_I",
    "Viseme_O",
    "Viseme_U",
    "Viseme_FV",
    "Viseme_L",
    "Viseme_MBP",
]


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(
        description="Add procedural facial blendshapes to the split Body_Head mesh."
    )
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--input", help="Input FBX path to import.")
    group.add_argument("--blend-input", help="Input .blend path to open.")
    parser.add_argument("--output", required=True, help="Output FBX path.")
    parser.add_argument("--report", required=True, help="JSON report path.")
    parser.add_argument("--blend-out", help="Optional .blend output path.")
    parser.add_argument("--head-name", default="Body_Head", help="Head mesh object name.")
    parser.add_argument("--armature-name", help="Optional armature object name.")
    return parser.parse_args(argv)


def normalize_name(name):
    return "".join(ch for ch in name.lower() if ch.isalnum())


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_fbx(filepath):
    bpy.ops.import_scene.fbx(filepath=str(filepath))


def open_blend(filepath):
    bpy.ops.wm.open_mainfile(filepath=str(filepath))


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


def find_head_mesh(head_name):
    obj = bpy.data.objects.get(head_name)
    if obj is None:
        raise RuntimeError(f"Could not find head mesh '{head_name}'.")
    if obj.type != "MESH":
        raise RuntimeError(f"Object '{head_name}' is '{obj.type}', expected 'MESH'.")
    return obj


def select_object(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    if obj.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")


def smoothstep(edge0, edge1, value):
    if abs(edge1 - edge0) < 1e-8:
        return 0.0
    t = max(0.0, min(1.0, (value - edge0) / (edge1 - edge0)))
    return t * t * (3.0 - 2.0 * t)


def gaussian(value, center, radius):
    if radius <= 1e-8:
        return 0.0
    t = (value - center) / radius
    return math.exp(-(t * t))


def export_fbx(filepath, armature_obj):
    bpy.ops.object.select_all(action="DESELECT")
    armature_obj.select_set(True)
    bpy.context.view_layer.objects.active = armature_obj
    for obj in bpy.data.objects:
        if obj.type == "MESH":
            obj.select_set(True)

    bpy.ops.export_scene.fbx(
        filepath=str(filepath),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        mesh_smooth_type="FACE",
    )


def clear_existing_shape_keys(head_obj):
    if head_obj.data.shape_keys is None:
        return []

    removed = []
    basis = head_obj.data.shape_keys.reference_key
    for key_block in list(head_obj.data.shape_keys.key_blocks):
        if key_block == basis:
            continue
        removed.append(key_block.name)
        head_obj.shape_key_remove(key_block)
    return removed


def ensure_basis_shape_key(head_obj):
    if head_obj.data.shape_keys is not None and head_obj.data.shape_keys.reference_key is not None:
        return head_obj.data.shape_keys.reference_key
    select_object(head_obj)
    return head_obj.shape_key_add(name="Basis", from_mix=False)


def build_feature_table(base_positions):
    xs = [co.x for co in base_positions]
    ys = [co.y for co in base_positions]
    zs = [co.z for co in base_positions]

    min_x = min(xs)
    max_x = max(xs)
    min_y = min(ys)
    max_y = max(ys)
    min_z = min(zs)
    max_z = max(zs)

    width = max_x - min_x
    half_width = width * 0.5
    height = max_y - min_y
    depth = max_z - min_z

    eye_y = min_y + height * 0.618
    brow_y = min_y + height * 0.717
    mouth_y = min_y + height * 0.392
    jaw_y = min_y + height * 0.285

    eye_z = min_z + depth * 0.205
    brow_z = min_z + depth * 0.175
    mouth_z = min_z + depth * 0.235
    jaw_z = min_z + depth * 0.29

    left_eye_x = half_width * 0.33
    right_eye_x = -left_eye_x
    left_brow_x = half_width * 0.305
    right_brow_x = -left_brow_x
    corner_x = half_width * 0.34
    cheek_x = half_width * 0.44

    bounds = {
        "x": [min_x, max_x],
        "y": [min_y, max_y],
        "z": [min_z, max_z],
        "width": width,
        "height": height,
        "depth": depth,
    }
    anchors = {
        "eyeY": eye_y,
        "browY": brow_y,
        "mouthY": mouth_y,
        "jawY": jaw_y,
        "eyeZ": eye_z,
        "browZ": brow_z,
        "mouthZ": mouth_z,
        "jawZ": jaw_z,
        "leftEyeX": left_eye_x,
        "rightEyeX": right_eye_x,
        "leftBrowX": left_brow_x,
        "rightBrowX": right_brow_x,
        "cornerX": corner_x,
        "cheekX": cheek_x,
    }

    features = []
    for co in base_positions:
        left_eye = (
            gaussian(co.x, left_eye_x, half_width * 0.12)
            * gaussian(co.y, eye_y, height * 0.055)
            * gaussian(co.z, eye_z, depth * 0.17)
        )
        right_eye = (
            gaussian(co.x, right_eye_x, half_width * 0.12)
            * gaussian(co.y, eye_y, height * 0.055)
            * gaussian(co.z, eye_z, depth * 0.17)
        )

        left_eye_upper = left_eye * smoothstep(eye_y - height * 0.01, eye_y + height * 0.055, co.y)
        right_eye_upper = right_eye * smoothstep(eye_y - height * 0.01, eye_y + height * 0.055, co.y)
        left_eye_lower = left_eye * (1.0 - smoothstep(eye_y - height * 0.055, eye_y + height * 0.01, co.y))
        right_eye_lower = right_eye * (1.0 - smoothstep(eye_y - height * 0.055, eye_y + height * 0.01, co.y))

        left_brow = (
            gaussian(co.x, left_brow_x, half_width * 0.14)
            * gaussian(co.y, brow_y, height * 0.05)
            * gaussian(co.z, brow_z, depth * 0.18)
        )
        right_brow = (
            gaussian(co.x, right_brow_x, half_width * 0.14)
            * gaussian(co.y, brow_y, height * 0.05)
            * gaussian(co.z, brow_z, depth * 0.18)
        )
        inner_brow = (
            gaussian(co.x, 0.0, half_width * 0.135)
            * gaussian(co.y, brow_y - height * 0.01, height * 0.05)
            * gaussian(co.z, brow_z, depth * 0.18)
        )

        mouth_front = gaussian(co.z, mouth_z, depth * 0.22)
        mouth_core = (
            gaussian(co.x, 0.0, half_width * 0.28)
            * gaussian(co.y, mouth_y, height * 0.06)
            * mouth_front
        )
        upper_lip = (
            gaussian(co.x, 0.0, half_width * 0.25)
            * gaussian(co.y, mouth_y + height * 0.03, height * 0.028)
            * gaussian(co.z, mouth_z, depth * 0.2)
        )
        lower_lip = (
            gaussian(co.x, 0.0, half_width * 0.25)
            * gaussian(co.y, mouth_y - height * 0.03, height * 0.038)
            * gaussian(co.z, mouth_z, depth * 0.2)
        )
        lip_center = (
            gaussian(co.x, 0.0, half_width * 0.11)
            * gaussian(co.y, mouth_y, height * 0.05)
            * gaussian(co.z, mouth_z, depth * 0.18)
        )

        left_corner = (
            gaussian(co.x, corner_x, half_width * 0.11)
            * gaussian(co.y, mouth_y, height * 0.05)
            * gaussian(co.z, mouth_z, depth * 0.22)
        )
        right_corner = (
            gaussian(co.x, -corner_x, half_width * 0.11)
            * gaussian(co.y, mouth_y, height * 0.05)
            * gaussian(co.z, mouth_z, depth * 0.22)
        )
        left_cheek = (
            gaussian(co.x, cheek_x, half_width * 0.13)
            * gaussian(co.y, mouth_y + height * 0.07, height * 0.06)
            * gaussian(co.z, mouth_z, depth * 0.24)
        )
        right_cheek = (
            gaussian(co.x, -cheek_x, half_width * 0.13)
            * gaussian(co.y, mouth_y + height * 0.07, height * 0.06)
            * gaussian(co.z, mouth_z, depth * 0.24)
        )

        jaw = (
            gaussian(co.x, 0.0, half_width * 0.31)
            * gaussian(co.y, jaw_y, height * 0.09)
            * gaussian(co.z, jaw_z, depth * 0.28)
        )

        features.append(
            {
                "leftEye": left_eye,
                "rightEye": right_eye,
                "leftEyeUpper": left_eye_upper,
                "rightEyeUpper": right_eye_upper,
                "leftEyeLower": left_eye_lower,
                "rightEyeLower": right_eye_lower,
                "leftBrow": left_brow,
                "rightBrow": right_brow,
                "innerBrow": inner_brow,
                "mouthCore": mouth_core,
                "upperLip": upper_lip,
                "lowerLip": lower_lip,
                "lipCenter": lip_center,
                "leftCorner": left_corner,
                "rightCorner": right_corner,
                "leftCheek": left_cheek,
                "rightCheek": right_cheek,
                "jaw": jaw,
                "x": co.x,
            }
        )

    return bounds, anchors, features


def eye_delta(side, features, bounds, mode):
    height = bounds["height"]
    depth = bounds["depth"]
    side_key = "left" if side == "L" else "right"
    upper = features[f"{side_key}EyeUpper"]
    lower = features[f"{side_key}EyeLower"]
    eye = features[f"{side_key}Eye"]

    if mode == "blink":
        return Vector((0.0, -height * 0.034 * upper + height * 0.011 * lower, depth * 0.019 * (upper + 0.7 * lower)))
    if mode == "smile":
        return Vector((0.0, -height * 0.011 * upper + height * 0.016 * lower + height * 0.004 * eye, depth * 0.009 * (upper + lower)))
    if mode == "wide":
        return Vector((0.0, height * 0.019 * upper - height * 0.012 * lower, -depth * 0.005 * (upper + lower)))
    raise ValueError(f"Unknown eye mode '{mode}'.")


def brow_delta(side, features, bounds, mode):
    height = bounds["height"]
    depth = bounds["depth"]
    half_width = bounds["width"] * 0.5
    side_key = "left" if side == "L" else "right"
    brow = features[f"{side_key}Brow"]

    if mode == "up":
        return Vector((0.0, height * 0.024 * brow, -depth * 0.004 * brow))
    if mode == "down":
        dx = -half_width * 0.018 * brow if side == "L" else half_width * 0.018 * brow
        return Vector((dx, -height * 0.018 * brow, depth * 0.004 * brow))
    raise ValueError(f"Unknown brow mode '{mode}'.")


def inner_brow_delta(features, bounds):
    height = bounds["height"]
    depth = bounds["depth"]
    brow = features["innerBrow"]
    return Vector((0.0, height * 0.022 * brow, -depth * 0.004 * brow))


def mouth_delta(shape_name, features, bounds):
    height = bounds["height"]
    depth = bounds["depth"]
    x = features["x"]
    upper = features["upperLip"]
    lower = features["lowerLip"]
    center = features["lipCenter"]
    core = features["mouthCore"]
    left_corner = features["leftCorner"]
    right_corner = features["rightCorner"]
    cheeks = features["leftCheek"] + features["rightCheek"]
    corners = left_corner + right_corner
    jaw = features["jaw"]

    if shape_name == "Smile":
        dx = x * 0.012 * core + bounds["width"] * 0.027 * (left_corner - right_corner)
        dy = height * 0.03 * corners + height * 0.011 * cheeks + height * 0.004 * upper
        dz = depth * 0.01 * corners
        return Vector((dx, dy, dz))

    if shape_name == "Sad":
        dx = -x * 0.01 * core - bounds["width"] * 0.01 * (left_corner - right_corner)
        dy = -height * 0.024 * corners - height * 0.009 * center
        dz = depth * 0.006 * corners
        return Vector((dx, dy, dz))

    if shape_name == "Surprise":
        dx = -x * 0.025 * core - bounds["width"] * 0.008 * (left_corner - right_corner)
        dy = height * 0.012 * upper - height * 0.036 * lower - height * 0.017 * jaw
        dz = -depth * 0.014 * (upper + lower + center)
        return Vector((dx, dy, dz))

    if shape_name == "AngryLight":
        dx = -x * 0.016 * core
        dy = -height * 0.01 * corners + height * 0.004 * upper - height * 0.006 * lower
        dz = depth * 0.008 * (upper + lower)
        return Vector((dx, dy, dz))

    if shape_name == "MouthOpen":
        dx = 0.0
        dy = height * 0.012 * upper - height * 0.046 * lower - height * 0.019 * jaw
        dz = depth * 0.004 * (upper + lower)
        return Vector((dx, dy, dz))

    if shape_name == "MouthNarrow":
        dx = -x * 0.033 * (core + corners)
        dy = -height * 0.003 * corners
        dz = -depth * 0.007 * core
        return Vector((dx, dy, dz))

    if shape_name == "MouthWide":
        dx = x * 0.037 * core + bounds["width"] * 0.018 * (left_corner - right_corner)
        dy = height * 0.003 * corners
        dz = depth * 0.003 * corners
        return Vector((dx, dy, dz))

    if shape_name == "MouthRound":
        dx = -x * 0.04 * (core + corners)
        dy = height * 0.004 * upper - height * 0.01 * lower
        dz = -depth * 0.018 * (upper + lower + center)
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_Rest":
        dy = -height * 0.004 * upper + height * 0.004 * lower
        dz = depth * 0.002 * (upper + lower)
        return Vector((0.0, dy, dz))

    if shape_name == "Viseme_AA":
        dx = x * 0.012 * core
        dy = height * 0.012 * upper - height * 0.056 * lower - height * 0.028 * jaw
        dz = depth * 0.002 * (upper + lower)
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_E":
        dx = x * 0.04 * core + bounds["width"] * 0.015 * (left_corner - right_corner)
        dy = height * 0.003 * upper - height * 0.009 * lower
        dz = depth * 0.011 * (upper + lower)
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_I":
        dx = x * 0.046 * core + bounds["width"] * 0.017 * (left_corner - right_corner)
        dy = height * 0.002 * upper - height * 0.007 * lower
        dz = depth * 0.012 * (upper + lower)
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_O":
        dx = -x * 0.036 * (core + corners)
        dy = height * 0.006 * upper - height * 0.026 * lower - height * 0.01 * jaw
        dz = -depth * 0.016 * (upper + lower + center)
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_U":
        dx = -x * 0.041 * (core + corners)
        dy = height * 0.003 * upper - height * 0.015 * lower
        dz = -depth * 0.02 * (upper + lower + center)
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_FV":
        dx = -x * 0.016 * core
        dy = -height * 0.01 * upper + height * 0.019 * lower
        dz = depth * 0.004 * upper - depth * 0.011 * lower
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_L":
        dx = x * 0.015 * core
        dy = height * 0.006 * upper - height * 0.03 * lower - height * 0.012 * jaw
        dz = -depth * 0.004 * core
        return Vector((dx, dy, dz))

    if shape_name == "Viseme_MBP":
        dx = -x * 0.01 * core
        dy = -height * 0.016 * upper + height * 0.02 * lower
        dz = depth * 0.012 * (upper + lower)
        return Vector((dx, dy, dz))

    raise ValueError(f"Unknown mouth shape '{shape_name}'.")


def compute_delta(shape_name, features, bounds):
    if shape_name == "Blink_L":
        return eye_delta("L", features, bounds, "blink")
    if shape_name == "Blink_R":
        return eye_delta("R", features, bounds, "blink")
    if shape_name == "SmileEye_L":
        return eye_delta("L", features, bounds, "smile")
    if shape_name == "SmileEye_R":
        return eye_delta("R", features, bounds, "smile")
    if shape_name == "WideEye_L":
        return eye_delta("L", features, bounds, "wide")
    if shape_name == "WideEye_R":
        return eye_delta("R", features, bounds, "wide")
    if shape_name == "BrowUp_L":
        return brow_delta("L", features, bounds, "up")
    if shape_name == "BrowUp_R":
        return brow_delta("R", features, bounds, "up")
    if shape_name == "BrowDown_L":
        return brow_delta("L", features, bounds, "down")
    if shape_name == "BrowDown_R":
        return brow_delta("R", features, bounds, "down")
    if shape_name == "BrowInnerUp":
        return inner_brow_delta(features, bounds)
    return mouth_delta(shape_name, features, bounds)


def add_shape_keys(head_obj):
    select_object(head_obj)
    removed = clear_existing_shape_keys(head_obj)
    ensure_basis_shape_key(head_obj)

    base_positions = [vertex.co.copy() for vertex in head_obj.data.vertices]
    bounds, anchors, features = build_feature_table(base_positions)

    report_shapes = []
    for shape_name in REQUIRED_SHAPE_KEYS:
        shape_key = head_obj.shape_key_add(name=shape_name, from_mix=False)
        shape_key.slider_min = 0.0
        shape_key.slider_max = 1.0
        moved_vertices = 0
        max_displacement = 0.0

        for index, base_co in enumerate(base_positions):
            delta = compute_delta(shape_name, features[index], bounds)
            shape_key.data[index].co = base_co + delta
            length = delta.length
            if length > 0.0001:
                moved_vertices += 1
                if length > max_displacement:
                    max_displacement = length

        report_shapes.append(
            {
                "name": shape_name,
                "movedVertexCount": moved_vertices,
                "maxDisplacement": round(max_displacement, 6),
            }
        )

    all_shape_keys = [key.name for key in head_obj.data.shape_keys.key_blocks]
    return {
        "removedShapeKeys": removed,
        "bounds": bounds,
        "anchors": anchors,
        "shapeKeys": report_shapes,
        "allShapeKeys": all_shape_keys,
    }


def main():
    args = parse_args()
    output_path = Path(args.output).resolve()
    report_path = Path(args.report).resolve()
    blend_out = Path(args.blend_out).resolve() if args.blend_out else None

    output_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.parent.mkdir(parents=True, exist_ok=True)
    if blend_out:
        blend_out.parent.mkdir(parents=True, exist_ok=True)

    if args.input:
        input_path = Path(args.input).resolve()
        if not input_path.exists():
            raise RuntimeError(f"Input FBX not found: {input_path}")
        reset_scene()
        import_fbx(input_path)
        source_type = "fbx"
        source_path = input_path
    else:
        blend_input = Path(args.blend_input).resolve()
        if not blend_input.exists():
            raise RuntimeError(f"Blend file not found: {blend_input}")
        open_blend(blend_input)
        source_type = "blend"
        source_path = blend_input

    armature_obj = find_armature(args.armature_name)
    head_obj = find_head_mesh(args.head_name)
    shape_report = add_shape_keys(head_obj)

    if blend_out:
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_out))

    export_fbx(output_path, armature_obj)

    report = {
        "sourceType": source_type,
        "sourcePath": str(source_path),
        "outputPath": str(output_path),
        "savedBlendPath": str(blend_out) if blend_out else None,
        "armatureObject": armature_obj.name,
        "headObject": head_obj.name,
        "requiredShapeKeyCount": len(REQUIRED_SHAPE_KEYS),
        "shapeKeyReport": shape_report,
    }
    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote facial blendshape report to: {report_path}")


if __name__ == "__main__":
    main()
