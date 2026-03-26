import argparse
import sys
from pathlib import Path

import bpy
from mathutils import Vector


VARIANTS = {
    "current": [],
    "show_forearms": ["Body_ForearmL", "Body_ForearmR"],
    "show_arms": ["Body_ArmUpperL", "Body_ArmUpperR", "Body_ForearmL", "Body_ForearmR"],
}


def parse_args():
    argv = sys.argv
    if "--" in argv:
        argv = argv[argv.index("--") + 1 :]
    else:
        argv = []

    parser = argparse.ArgumentParser(description="Render arm visibility variants from a .blend scene.")
    parser.add_argument("--input", required=True, help="Input .blend path.")
    parser.add_argument("--output-dir", required=True, help="Output directory for renders.")
    parser.add_argument("--dress-name", default="CHR_Dress_AzureSakura_v001", help="Dress object name.")
    return parser.parse_args(argv)


def ensure_camera():
    camera = bpy.data.objects.get("VariantCamera")
    if camera is not None:
        return camera

    cam_data = bpy.data.cameras.new("VariantCamera")
    camera = bpy.data.objects.new("VariantCamera", cam_data)
    bpy.context.scene.collection.objects.link(camera)
    bpy.context.scene.camera = camera
    return camera


def focus_camera(camera, target_obj):
    camera.location = Vector((0.0, -4.8, 1.15))
    direction = target_obj.matrix_world.translation + Vector((0.0, 0.0, 0.1)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 58


def configure_scene():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "SINGLE"
    scene.display.shading.single_color = (0.85, 0.85, 0.85)
    scene.display.shading.show_xray = False
    scene.render.resolution_x = 1200
    scene.render.resolution_y = 1200
    scene.render.film_transparent = False
    world = bpy.data.worlds.get("World")
    if world:
        world.color = (0.09, 0.09, 0.09)


def set_variant_visibility(show_names):
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name.startswith("Body_"):
            should_show = obj.name == "Body_Head" or obj.name == "Body_HandL" or obj.name == "Body_HandR" or obj.name == "Body_FootL" or obj.name == "Body_FootR"
            if obj.name in show_names:
                should_show = True
            obj.hide_viewport = not should_show
            obj.hide_render = not should_show


def main():
    args = parse_args()
    input_path = Path(args.input).resolve()
    output_dir = Path(args.output_dir).resolve()
    output_dir.mkdir(parents=True, exist_ok=True)

    bpy.ops.wm.open_mainfile(filepath=str(input_path))
    dress_obj = bpy.data.objects.get(args.dress_name)
    if dress_obj is None:
        raise RuntimeError(f"Dress object not found: {args.dress_name}")

    configure_scene()
    camera = ensure_camera()
    focus_camera(camera, dress_obj)
    bpy.context.scene.camera = camera

    for variant_name, show_names in VARIANTS.items():
        set_variant_visibility(show_names)
        bpy.context.view_layer.update()
        bpy.context.scene.render.filepath = str(output_dir / f"{variant_name}.png")
        bpy.ops.render.render(write_still=True)

    print(f"Rendered variants to: {output_dir}")


if __name__ == "__main__":
    main()
