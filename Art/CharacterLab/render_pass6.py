"""Worst frames of sprint, slide, and roll. Does not save the blend.

One still per process. COSTUME_SHOT is sprint, slide, or roll.
"""
import importlib.util
import json
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass1 as rp

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RAW = os.environ.get("COSTUME_RAW", "/tmp/charlab/pass6")
SHOTS = {
    "sprint": ("Bram_1_Beanie", "sprint", 0.067),
    "slide": ("Reed_1_Hood", "slide", 0.267),
    "roll": ("Reed_1_Hood", "roll", 0.267),
}


def log(msg):
    print(msg, flush=True)


def load_clips():
    os.environ["NOCLIP_SETTLE"] = "runner"
    os.environ["PASS18_RENDER"] = "0"
    spec = importlib.util.spec_from_file_location(
        "pass18_clips", os.path.join(ROOT, "Tools", "Tag", "render_pass18_noclip.py")
    )
    clips = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(clips)
    return clips


def apply_shot(arm, clips, kind, age):
    yaw = clips.p.SLIDE_YAW
    clips.p.clear_props()
    clips.p.ensure_pose(arm)
    if kind == "sprint":
        ctx = clips.build_extra(arm, "sprint", yaw)
        clips.show_loco(arm, age, ctx)
        return
    if kind == "slide":
        ctx = clips.r.build_context(arm, "slide", yaw)
        clips.r.show_slide(arm, age, ctx)
        return
    ctx = clips.r.build_context(arm, "roll", yaw)
    clips.r.show_roll(arm, age, ctx)


def show_loadout(arm, spec):
    allowed = set(spec["pieces"])
    covered = set(spec.get("hide") or [])
    shown = []
    for obj in bpy.data.objects:
        if obj.type != "MESH" or obj.parent != arm:
            continue
        name = obj.name.split(".")[0]
        if "LOD" in obj.name:
            obj.hide_render = True
            continue
        if name in covered or (name.startswith("Lab_") and name not in allowed):
            obj.hide_render = True
            continue
        if len(obj.data.vertices) == 0:
            obj.hide_render = True
            continue
        obj.hide_render = False
        obj.hide_set(False)
        shown.append(obj)
    arm.hide_render = True
    return shown


def main():
    shot = os.environ.get("COSTUME_SHOT", "roll")
    loadout_id, kind, age = SHOTS[shot]
    os.makedirs(RAW, exist_ok=True)
    os.environ["COSTUME_ENGINE"] = "BLENDER_EEVEE"
    clips = load_clips()
    arm = bpy.data.objects["DummyArmature"]
    specs = json.load(open(rp.LOADOUTS, encoding="utf-8"))["sets"]
    spec = next(row for row in specs if row["id"] == loadout_id)
    shown = show_loadout(arm, spec)
    rp.paint(shown, spec["color"], spec["id"])
    apply_shot(arm, clips, kind, age)
    bpy.context.view_layer.update()
    shown = [obj for obj in shown if not obj.hide_render]
    mins, maxs = rp.world_bounds(shown)
    log(
        "SHOT %s loadout=%s t=%.3f pieces=%d z=%.3f..%.3f"
        % (shot, loadout_id, age, len(shown), mins.z, maxs.z)
    )
    rp.make_floor()
    rp.make_lights()
    rp.make_world()
    rp.configure_render()
    cam_data = bpy.data.cameras.new("LabCam")
    cam = bpy.data.objects.new("LabCam", cam_data)
    rp.link(cam)
    rp.frame_ortho(cam, mins, maxs, 1280, 720, "three")
    rp.render_still(cam, os.path.join(RAW, shot + ".png"), 1280, 720)


if __name__ == "__main__":
    main()
