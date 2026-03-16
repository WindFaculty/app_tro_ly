import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


REQUIRED_HUMANOID_BONES = [
    "Hips",
    "Spine",
    "Head",
    "LeftUpperLeg",
    "LeftLowerLeg",
    "LeftFoot",
    "RightUpperLeg",
    "RightLowerLeg",
    "RightFoot",
    "LeftUpperArm",
    "LeftLowerArm",
    "LeftHand",
    "RightUpperArm",
    "RightLowerArm",
    "RightHand",
]

OPTIONAL_HUMANOID_BONES = [
    "Chest",
    "UpperChest",
    "Neck",
    "LeftShoulder",
    "RightShoulder",
    "LeftToes",
    "RightToes",
    "LeftEye",
    "RightEye",
    "Jaw",
]

DIRECT_ALIAS_MAP = {
    "Hips": ["hips", "pelvis"],
    "Head": ["head"],
    "Neck": ["neck"],
    "LeftUpperLeg": ["leftupleg", "lthigh", "leftthigh"],
    "LeftLowerLeg": ["leftleg", "lcalf", "leftcalf"],
    "LeftFoot": ["leftfoot", "lfoot"],
    "LeftToes": ["lefttoebase", "lefttoe", "ltoe"],
    "RightUpperLeg": ["rightupleg", "rthigh", "rightthigh"],
    "RightLowerLeg": ["rightleg", "rcalf", "rightcalf"],
    "RightFoot": ["rightfoot", "rfoot"],
    "RightToes": ["righttoebase", "righttoe", "rtoe"],
    "LeftShoulder": ["leftshoulder", "lshoulder"],
    "LeftUpperArm": ["leftarm", "leftupperarm", "larm"],
    "LeftLowerArm": ["leftforearm", "leftlowerarm", "lforearm"],
    "LeftHand": ["lefthand", "lhand"],
    "RightShoulder": ["rightshoulder", "rshoulder"],
    "RightUpperArm": ["rightarm", "rightupperarm", "rarm"],
    "RightLowerArm": ["rightforearm", "rightlowerarm", "rforearm"],
    "RightHand": ["righthand", "rhand"],
    "LeftEye": ["lefteye", "eye_l", "eyel"],
    "RightEye": ["righteye", "eye_r", "eyer"],
    "Jaw": ["jaw"],
}

FINGER_KEYWORDS = {
    "LeftThumb": ["leftthumb", "thumb_l", "lthumb"],
    "LeftIndex": ["leftindex", "index_l", "lindex"],
    "LeftMiddle": ["leftmiddle", "middle_l", "lmiddle"],
    "LeftRing": ["leftring", "ring_l", "lring"],
    "LeftLittle": ["leftlittle", "leftpinky", "little_l", "pinky_l", "llittle", "lpinky"],
    "RightThumb": ["rightthumb", "thumb_r", "rthumb"],
    "RightIndex": ["rightindex", "index_r", "rindex"],
    "RightMiddle": ["rightmiddle", "middle_r", "rmiddle"],
    "RightRing": ["rightring", "ring_r", "rring"],
    "RightLittle": ["rightlittle", "rightpinky", "little_r", "pinky_r", "rlittle", "rpinky"],
}


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(
        description="Audit an avatar FBX/.blend for Blender cleanup and Unity humanoid readiness."
    )
    group = parser.add_mutually_exclusive_group(required=True)
    group.add_argument("--input", help="Input FBX path to import into a new scene.")
    group.add_argument("--blend-input", help="Existing .blend file to open.")
    parser.add_argument("--report", required=True, help="JSON report output path.")
    parser.add_argument("--blend-out", help="Optional .blend save path after import.")
    parser.add_argument("--armature-name", help="Explicit armature object name.")
    parser.add_argument("--t-pose-threshold-deg", type=float, default=25.0)
    parser.add_argument("--a-pose-threshold-deg", type=float, default=55.0)
    return parser.parse_args(argv)


def normalize_name(name):
    return "".join(ch for ch in name.lower() if ch.isalnum())


def round_list(values, digits=5):
    return [round(float(value), digits) for value in values]


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


def build_bone_maps(armature_obj):
    bones = armature_obj.data.bones
    by_name = {bone.name: bone for bone in bones}
    by_normalized = {normalize_name(bone.name): bone for bone in bones}
    return by_name, by_normalized


def find_by_alias(by_normalized, aliases):
    for alias in aliases:
        bone = by_normalized.get(normalize_name(alias))
        if bone is not None:
            return bone
    return None


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


def infer_humanoid_mapping(by_normalized):
    mapping = {}

    for target, aliases in DIRECT_ALIAS_MAP.items():
        bone = find_by_alias(by_normalized, aliases)
        mapping[target] = bone.name if bone else None

    hips = find_by_alias(by_normalized, DIRECT_ALIAS_MAP["Hips"])
    if hips is not None:
        mapping["Hips"] = hips.name
        spine_chain = build_spine_chain(hips)
        spine_like = []
        neck_bone = None
        head_bone = None

        for bone in spine_chain:
            token = normalize_name(bone.name)
            if token.startswith("head"):
                head_bone = bone
            elif token.startswith("neck"):
                neck_bone = bone
            else:
                spine_like.append(bone)

        if spine_like:
            mapping["Spine"] = spine_like[0].name
        if len(spine_like) >= 2:
            mapping["Chest"] = spine_like[1].name
        if len(spine_like) >= 3:
            mapping["UpperChest"] = spine_like[2].name
        if neck_bone is not None:
            mapping["Neck"] = neck_bone.name
        if head_bone is not None:
            mapping["Head"] = head_bone.name

    required = {key: mapping.get(key) for key in REQUIRED_HUMANOID_BONES}
    optional = {key: mapping.get(key) for key in OPTIONAL_HUMANOID_BONES}
    return required, optional


def classify_pose(armature_obj, human_bones, t_threshold_deg, a_threshold_deg):
    bone_lookup = armature_obj.data.bones

    def get_bone(name):
        if not name:
            return None
        return bone_lookup.get(name)

    left_arm = get_bone(human_bones.get("LeftUpperArm"))
    right_arm = get_bone(human_bones.get("RightUpperArm"))
    if left_arm is None or right_arm is None:
        return {
            "classification": "unknown",
            "reason": "Missing upper-arm bones.",
        }

    left_vec = (left_arm.tail_local - left_arm.head_local).normalized()
    right_vec = (right_arm.tail_local - right_arm.head_local).normalized()

    left_ref = Vector((1.0, 0.0, 0.0))
    right_ref = Vector((-1.0, 0.0, 0.0))
    down_ref = Vector((0.0, 0.0, -1.0))

    left_horizontal = math.degrees(left_vec.angle(left_ref))
    right_horizontal = math.degrees(right_vec.angle(right_ref))
    left_down = math.degrees(left_vec.angle(down_ref))
    right_down = math.degrees(right_vec.angle(down_ref))
    average_horizontal = (left_horizontal + right_horizontal) * 0.5

    if average_horizontal <= t_threshold_deg:
        label = "t_pose_like"
    elif average_horizontal <= a_threshold_deg and left_down < 90.0 and right_down < 90.0:
        label = "a_pose_like"
    else:
        label = "custom_pose"

    return {
        "classification": label,
        "leftArmAngleFromHorizontalDeg": round(left_horizontal, 3),
        "rightArmAngleFromHorizontalDeg": round(right_horizontal, 3),
        "leftArmAngleFromDownDeg": round(left_down, 3),
        "rightArmAngleFromDownDeg": round(right_down, 3),
        "thresholdsDeg": {
            "tPoseMax": t_threshold_deg,
            "aPoseMax": a_threshold_deg,
        },
    }


def summarize_meshes(armature_obj):
    summaries = []
    for obj in bpy.data.objects:
        if obj.type != "MESH":
            continue

        armature_modifiers = [
            modifier.object.name
            for modifier in obj.modifiers
            if modifier.type == "ARMATURE" and modifier.object is not None
        ]

        parent_name = obj.parent.name if obj.parent else None
        uses_armature = armature_obj.name in armature_modifiers or parent_name == armature_obj.name

        summaries.append(
            {
                "name": obj.name,
                "vertexCount": len(obj.data.vertices),
                "polygonCount": len(obj.data.polygons),
                "materialSlots": [slot.name for slot in obj.material_slots],
                "parent": parent_name,
                "armatureModifiers": armature_modifiers,
                "boundToTargetArmature": uses_armature,
                "location": round_list(obj.location),
                "rotationEulerDeg": round_list((math.degrees(v) for v in obj.rotation_euler)),
                "scale": round_list(obj.scale),
            }
        )

    return summaries


def summarize_object_transform(obj):
    return {
        "name": obj.name,
        "location": round_list(obj.location),
        "rotationEulerDeg": round_list((math.degrees(v) for v in obj.rotation_euler)),
        "scale": round_list(obj.scale),
    }


def collect_bone_summary(armature_obj):
    bones = armature_obj.data.bones
    return {
        "count": len(bones),
        "names": [bone.name for bone in bones],
        "hierarchy": [
            {
                "name": bone.name,
                "parent": bone.parent.name if bone.parent else None,
                "head": round_list(bone.head_local),
                "tail": round_list(bone.tail_local),
                "rollDeg": round(math.degrees(bone.matrix_local.to_euler().y), 3),
                "useDeform": bool(bone.use_deform),
            }
            for bone in bones
        ],
    }


def collect_finger_summary(bone_names):
    normalized_names = [normalize_name(name) for name in bone_names]
    summary = {}
    for label, keywords in FINGER_KEYWORDS.items():
        matched = sorted(
            name
            for name, normalized in zip(bone_names, normalized_names)
            if any(keyword in normalized for keyword in keywords)
        )
        summary[label] = matched
    return summary


def build_recommendations(armature_obj, mesh_summaries, required_map, optional_map, pose_summary):
    recommendations = []

    missing_required = [name for name, bone in required_map.items() if bone is None]
    if missing_required:
        recommendations.append(
            f"Humanoid chưa hợp lệ cho Unity: thiếu bone bắt buộc {', '.join(missing_required)}."
        )
    else:
        recommendations.append(
            "Đủ tối thiểu 15 bone bắt buộc để Unity Humanoid có thể automap."
        )

    missing_optional = [name for name, bone in optional_map.items() if bone is None]
    if missing_optional:
        recommendations.append(
            f"Có thể import Humanoid nhưng đang thiếu/không map optional bones: {', '.join(missing_optional)}."
        )

    if pose_summary["classification"] == "custom_pose":
        recommendations.append(
            "Pose tay không gần T-pose/A-pose; nên mở Avatar Configuration trong Unity và dùng Pose > Enforce T-Pose."
        )

    armature_transform = summarize_object_transform(armature_obj)
    non_identity = (
        any(abs(value) > 0.0001 for value in armature_transform["location"])
        or any(abs(value) > 0.1 for value in armature_transform["rotationEulerDeg"])
        or any(abs(value - 1.0) > 0.0001 for value in armature_transform["scale"])
    )
    if non_identity:
        recommendations.append(
            "Armature object không ở identity transform; nên kiểm tra Apply Transform trước khi export FBX sạch."
        )

    unbound_meshes = [mesh["name"] for mesh in mesh_summaries if not mesh["boundToTargetArmature"]]
    if unbound_meshes:
        recommendations.append(
            f"Có mesh chưa bind với armature mục tiêu: {', '.join(unbound_meshes)}."
        )

    return recommendations


def main():
    args = parse_args()
    report_path = Path(args.report).resolve()
    report_path.parent.mkdir(parents=True, exist_ok=True)

    if args.input:
        input_path = Path(args.input).resolve()
        if not input_path.exists():
            raise RuntimeError(f"Input FBX not found: {input_path}")
        reset_scene()
        import_fbx(input_path)
        source_path = input_path
        source_type = "fbx"
    else:
        blend_path = Path(args.blend_input).resolve()
        if not blend_path.exists():
            raise RuntimeError(f"Blend file not found: {blend_path}")
        open_blend(blend_path)
        source_path = blend_path
        source_type = "blend"

    armature_obj = find_armature(args.armature_name)
    _, by_normalized = build_bone_maps(armature_obj)
    required_map, optional_map = infer_humanoid_mapping(by_normalized)
    pose_summary = classify_pose(
        armature_obj,
        required_map,
        args.t_pose_threshold_deg,
        args.a_pose_threshold_deg,
    )
    mesh_summaries = summarize_meshes(armature_obj)
    bone_summary = collect_bone_summary(armature_obj)

    if args.blend_out:
        blend_out = Path(args.blend_out).resolve()
        blend_out.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.wm.save_as_mainfile(filepath=str(blend_out))
    else:
        blend_out = None

    report = {
        "sourceType": source_type,
        "sourcePath": str(source_path),
        "savedBlendPath": str(blend_out) if blend_out else None,
        "armatureObject": summarize_object_transform(armature_obj),
        "meshObjects": mesh_summaries,
        "boneSummary": bone_summary,
        "fingerSummary": collect_finger_summary(bone_summary["names"]),
        "unityHumanoid": {
            "required": required_map,
            "optional": optional_map,
            "missingRequired": [name for name, bone in required_map.items() if bone is None],
            "missingOptional": [name for name, bone in optional_map.items() if bone is None],
            "isMinimumValid": all(required_map.values()),
        },
        "poseAudit": pose_summary,
        "recommendations": build_recommendations(
            armature_obj,
            mesh_summaries,
            required_map,
            optional_map,
            pose_summary,
        ),
    }

    report_path.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Wrote avatar audit report to: {report_path}")


if __name__ == "__main__":
    main()
