import argparse
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

    parser = argparse.ArgumentParser(description="Render a front view of an asset.")
    parser.add_argument("--input", required=True, help="Input .fbx/.obj/.blend path.")
    parser.add_argument("--output", required=True, help="Output image path.")
    return parser.parse_args(argv)


def import_asset(path: Path):
    suffix = path.suffix.lower()
    if suffix == ".fbx":
        bpy.ops.import_scene.fbx(filepath=str(path))
    elif suffix == ".obj":
        bpy.ops.wm.obj_import(filepath=str(path))
    elif suffix == ".blend":
        bpy.ops.wm.open_mainfile(filepath=str(path))
    else:
        raise RuntimeError(f"Unsupported asset type: {suffix}")


def all_mesh_objects():
    return [obj for obj in bpy.data.objects if obj.type == "MESH"]


def combined_bounds(objs):
    corners = []
    for obj in objs:
        corners.extend([obj.matrix_world @ Vector(corner) for corner in obj.bound_box])
    xs = [c.x for c in corners]
    ys = [c.y for c in corners]
    zs = [c.z for c in corners]
    return Vector((min(xs), min(ys), min(zs))), Vector((max(xs), max(ys), max(zs)))


def ensure_camera():
    cam_data = bpy.data.cameras.new("AuditCamera")
    camera = bpy.data.objects.new("AuditCamera", cam_data)
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    return camera


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_path = Path(args.output).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    import_asset(input_path)
    meshes = all_mesh_objects()
    if not meshes:
        raise RuntimeError("No mesh objects found.")

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "SINGLE"
    scene.display.shading.single_color = (0.85, 0.85, 0.85)
    scene.render.resolution_x = 1000
    scene.render.resolution_y = 1000
    world = bpy.data.worlds.get("World")
    if world:
        world.color = (0.08, 0.08, 0.08)

    min_corner, max_corner = combined_bounds(meshes)
    center = (min_corner + max_corner) * 0.5
    size = max(max_corner.x - min_corner.x, max_corner.y - min_corner.y, max_corner.z - min_corner.z)

    camera = ensure_camera()
    camera.location = Vector((center.x, center.y - size * 2.3, center.z))
    direction = center - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 55
    camera.data.clip_end = 10000

    scene.render.filepath = str(output_path)
    bpy.ops.render.render(write_still=True)
    print(f"Rendered asset to: {output_path}")


if __name__ == "__main__":
    main()
