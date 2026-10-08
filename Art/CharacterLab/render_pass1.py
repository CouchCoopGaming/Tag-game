"""Lineup, readability, and variant stills from the costume lab blend.

Opens CostumeLab.blend, clones the mannequin per loadout, and renders.
Does not save the blend and does not touch the shipped mannequin file.
"""
import json
import os
import subprocess
import sys

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
BLEND_DIR = os.path.join(ROOT, "Art", "CharacterLab")
LOADOUTS = os.path.join(BLEND_DIR, "loadouts.json")
RAW = "/tmp/charlab/pass1"
DOCS = os.path.join(ROOT, "Docs", "Characters", "pass1")

LINEUP = ("Reed_1_Hood", "Bram_1_Helmet", "Pip_1_Cap", "Sol_1_Collar_Cap")


def log(msg):
    print(msg, flush=True)


def link(obj):
    bpy.context.scene.collection.objects.link(obj)


def clone_rig(suffix):
    src = bpy.data.objects["DummyArmature"]
    arm = src.copy()
    arm.data = src.data.copy()
    arm.name = "ShowArm_" + suffix
    link(arm)
    meshes = []
    originals = [o for o in bpy.data.objects if o.type == "MESH" and o.parent == src]
    for obj in originals:
        dup = obj.copy()
        dup.data = obj.data.copy()
        piece = obj.name.split(".")[0]
        dup.name = piece + "_" + suffix
        link(dup)
        world = obj.matrix_world.copy()
        dup.parent = arm
        dup.parent_type = obj.parent_type
        dup.parent_bone = obj.parent_bone
        dup.matrix_world = world
        for mod in dup.modifiers:
            if mod.type == "ARMATURE":
                mod.object = arm
        if piece.startswith("Lab_"):
            dup["piece"] = piece
        meshes.append(dup)
    return arm, meshes


def hide_source():
    src = bpy.data.objects["DummyArmature"]
    src.hide_render = True
    src.hide_set(True)
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.parent == src:
            obj.hide_render = True
            obj.hide_set(True)


def apply_loadout(meshes, names):
    allowed = set(names)
    for obj in meshes:
        piece = obj.get("piece")
        if piece is None:
            show = len(obj.data.vertices) > 0
        else:
            show = piece in allowed and len(obj.data.vertices) > 0
        obj["show"] = 1 if show else 0
        obj.hide_render = not show
        obj.hide_set(not show)


def paint(meshes, color, token):
    src = bpy.data.materials["LabPlayer"]
    mat = src.copy()
    mat.name = "Player_" + token
    for node in mat.node_tree.nodes:
        if node.name == "PlayerColor" or node.label == "PlayerColor":
            node.outputs[0].default_value = (color[0], color[1], color[2], 1.0)
    for obj in meshes:
        if not obj.material_slots:
            continue
        slot = obj.material_slots[0]
        current = slot.material
        if current is None:
            continue
        colored = current.get("player_color") == 1 or current.name.startswith("LabPlayer")
        if not colored:
            continue
        slot.link = "OBJECT"
        slot.material = mat


def set_group(entries, visible):
    for _spec, arm, meshes in entries:
        arm.hide_render = not visible
        arm.hide_set(not visible)
        for obj in meshes:
            if not visible:
                obj.hide_render = True
                obj.hide_set(True)
            else:
                show = bool(obj.get("show", 1))
                obj.hide_render = not show
                obj.hide_set(not show)


def place(entries, view, gap, z_lift):
    n = len(entries)
    for i, (_spec, arm, _meshes) in enumerate(entries):
        t = i - (n - 1) / 2.0
        if view == "side":
            arm.location = (0.0, t * gap, z_lift)
        elif view == "three":
            arm.location = (t * gap, -abs(t) * gap * 0.08, z_lift)
        elif view == "grid":
            row = i // 3
            col = i % 3
            arm.location = ((col - 1) * gap[0], row * gap[1], z_lift)
        else:
            arm.location = (t * gap, 0.0, z_lift)
    bpy.context.view_layer.update()


def shown_meshes(entries):
    found = []
    for _spec, _arm, meshes in entries:
        for obj in meshes:
            if not obj.hide_render:
                found.append(obj)
    return found


def world_bounds(objs):
    mins = Vector((1e9, 1e9, 1e9))
    maxs = Vector((-1e9, -1e9, -1e9))
    for obj in objs:
        for corner in obj.bound_box:
            point = obj.matrix_world @ Vector(corner)
            mins.x = min(mins.x, point.x)
            mins.y = min(mins.y, point.y)
            mins.z = min(mins.z, point.z)
            maxs.x = max(maxs.x, point.x)
            maxs.y = max(maxs.y, point.y)
            maxs.z = max(maxs.z, point.z)
    return mins, maxs


def aim(cam, eye, target):
    cam.location = eye
    direction = target - eye
    cam.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.view_layer.update()


def corners(mins, maxs):
    pts = []
    for x in (mins.x, maxs.x):
        for y in (mins.y, maxs.y):
            for z in (mins.z, maxs.z):
                pts.append(Vector((x, y, z)))
    return pts


def view_back(view):
    if view == "front":
        return Vector((0.0, -1.0, 0.15))
    if view == "side":
        return Vector((1.0, 0.0, 0.12))
    return Vector((-0.85, -1.0, 0.28))


def frame_ortho(cam, mins, maxs, res_x, res_y, view):
    """Zoom an orthographic camera until the bounds fill the frame with a margin."""
    from bpy_extras.object_utils import world_to_camera_view

    center = (mins + maxs) * 0.5
    back = view_back(view).normalized()
    aim(cam, center + back * 9.0, center + Vector((0.0, 0.0, -0.05)))
    cam.data.type = "ORTHO"
    cam.data.sensor_fit = "VERTICAL"
    pts = corners(mins, maxs)
    scene = bpy.context.scene
    scale = max((maxs - mins).length, 1.0)
    for _ in range(14):
        cam.data.ortho_scale = scale
        bpy.context.view_layer.update()
        xs = []
        ys = []
        for point in pts:
            co = world_to_camera_view(scene, cam, point)
            xs.append(co.x)
            ys.append(co.y)
        max_dev = max(0.5 - min(xs), max(xs) - 0.5, 0.5 - min(ys), max(ys) - 0.5)
        if max_dev < 1e-3:
            break
        scale *= max_dev / 0.40
    log(
        "FRAME %s scale=%.2f ndc-x %.2f..%.2f ndc-y %.2f..%.2f"
        % (view, cam.data.ortho_scale, min(xs), max(xs), min(ys), max(ys))
    )


def make_floor():
    mesh = bpy.data.meshes.new("LabFloorMesh")
    size = 24.0
    mesh.from_pydata(
        [(-size, -size, 0.0), (size, -size, 0.0), (size, size, 0.0), (-size, size, 0.0)],
        [],
        [(0, 1, 2, 3)],
    )
    mesh.update()
    obj = bpy.data.objects.new("LabFloor", mesh)
    link(obj)
    mat = bpy.data.materials.new("LabFloor")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.62, 0.60, 0.57, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.94
    obj.data.materials.append(mat)
    return obj


def make_lights():
    sun_data = bpy.data.lights.new("LabSun", "SUN")
    sun_data.energy = 2.4
    sun_data.angle = 0.06
    sun_data.use_shadow = True
    sun = bpy.data.objects.new("LabSun", sun_data)
    link(sun)
    sun.location = Vector((2.2, -3.4, 6.5))
    sun.rotation_euler = (Vector((0.0, 0.2, 0.0)) - sun.location).to_track_quat("-Z", "Y").to_euler()
    fill_data = bpy.data.lights.new("LabFill", "AREA")
    fill_data.energy = 60.0
    fill_data.size = 5.0
    fill = bpy.data.objects.new("LabFill", fill_data)
    link(fill)
    fill.location = Vector((-2.4, -3.0, 2.4))
    fill.rotation_euler = (Vector((0.0, 0.0, 1.0)) - fill.location).to_track_quat("-Z", "Y").to_euler()
    rim_data = bpy.data.lights.new("LabRim", "AREA")
    rim_data.energy = 40.0
    rim_data.size = 2.0
    rim = bpy.data.objects.new("LabRim", rim_data)
    link(rim)
    rim.location = Vector((0.4, 2.6, 2.2))
    rim.rotation_euler = (Vector((0.0, 0.0, 1.1)) - rim.location).to_track_quat("-Z", "Y").to_euler()


def make_world():
    world = bpy.data.worlds.new("LabWorld")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.55, 0.57, 0.60, 1.0)
    bg.inputs["Strength"].default_value = 0.22


def configure_render():
    scene = bpy.context.scene
    engine = os.environ.get("COSTUME_ENGINE", "CYCLES")
    scene.render.engine = engine
    scene.render.film_transparent = False
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGB"
    scene.render.image_settings.compression = 15
    scene.render.resolution_percentage = 100
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    if engine == "CYCLES":
        scene.cycles.device = "CPU"
        scene.cycles.samples = int(os.environ.get("COSTUME_SAMPLES", "28"))
        scene.cycles.use_denoising = False
        scene.cycles.use_adaptive_sampling = True
        scene.cycles.adaptive_threshold = 0.04
        scene.cycles.max_bounces = 3
        scene.cycles.diffuse_bounces = 2
        scene.cycles.glossy_bounces = 1
        scene.cycles.transmission_bounces = 0
        scene.cycles.caustics_reflective = False
        scene.cycles.caustics_refractive = False
    elif engine == "BLENDER_EEVEE":
        scene.eevee.taa_render_samples = 32
        scene.eevee.use_soft_shadows = True
        scene.eevee.shadow_cube_size = "2048"
        scene.eevee.shadow_cascade_size = "2048"
    log("ENGINE %s" % engine)


def render_still(cam, path, res_x, res_y):
    scene = bpy.context.scene
    scene.camera = cam
    scene.render.resolution_x = res_x
    scene.render.resolution_y = res_y
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    log("RENDERED %s" % path)


def lineup_entries(clones):
    by_id = {spec["id"]: (spec, arm, meshes) for spec, arm, meshes in clones}
    return [by_id[name] for name in LINEUP]


def shot_lineup(cam, clones, z_lift, view, path, res_x, res_y, gap):
    chosen = lineup_entries(clones)
    others = [entry for entry in clones if entry[0]["id"] not in LINEUP]
    set_group(others, False)
    set_group(chosen, True)
    place(chosen, view, gap, z_lift)
    mins, maxs = world_bounds(shown_meshes(chosen))
    names = sorted({obj.get("piece") for obj in shown_meshes(chosen) if obj.get("piece")})
    log("SHOW %s pieces=%d %s" % (view, len(names), ",".join(names)))
    frame_ortho(cam, mins, maxs, res_x, res_y, view)
    render_still(cam, path, res_x, res_y)


def shot_readability(cam, clones, z_lift, path):
    """Quarter of 1080p. Each body is about 30 px tall, with floor in front."""
    from bpy_extras.object_utils import world_to_camera_view

    chosen = lineup_entries(clones)
    others = [entry for entry in clones if entry[0]["id"] not in LINEUP]
    set_group(others, False)
    set_group(chosen, True)
    place(chosen, "front", 2.8, z_lift)
    mins, maxs = world_bounds(shown_meshes(chosen))
    height = max(0.01, maxs.z - mins.z)
    res_x, res_y = 960, 540
    center = (mins + maxs) * 0.5
    cam.data.type = "ORTHO"
    cam.data.sensor_fit = "VERTICAL"
    eye = Vector((center.x, mins.y - 6.0, center.z + height * 0.55))
    aim(cam, eye, Vector((center.x, center.y, mins.z + height * 0.35)))
    scene = bpy.context.scene
    scale = height * res_y / 30.0
    for _ in range(8):
        cam.data.ortho_scale = scale
        bpy.context.view_layer.update()
        top = world_to_camera_view(scene, cam, Vector((center.x, center.y, maxs.z)))
        bot = world_to_camera_view(scene, cam, Vector((center.x, center.y, mins.z)))
        px = abs(top.y - bot.y) * res_y
        if px < 1.0:
            break
        scale *= px / 30.0
    log("READ-PX %.2f height-m %.3f ortho %.3f" % (px, height, cam.data.ortho_scale))
    render_still(cam, path, res_x, res_y)
    return px


def shot_variants(cam, clones, z_lift):
    """One front row per character so the three variants share a scale."""
    by_who = {}
    for entry in clones:
        by_who.setdefault(entry[0]["character"], []).append(entry)
    manifest = []
    for who in ("Reed", "Bram", "Pip", "Sol"):
        group = sorted(by_who[who], key=lambda entry: entry[0]["variant"])
        others = [entry for entry in clones if entry[0]["character"] != who]
        set_group(others, False)
        set_group(group, True)
        place(group, "front", 1.2, z_lift)
        mins, maxs = world_bounds(shown_meshes(group))
        frame_ortho(cam, mins, maxs, 1280, 560, "front")
        path = os.path.join(RAW, "row-%s.png" % who)
        render_still(cam, path, 1280, 560)
        manifest.append(
            {
                "who": who,
                "file": path,
                "labels": [entry[0]["label"] for entry in group],
                "color": [int(round(c * 255)) for c in group[0][0]["color"]],
            }
        )
    with open(os.path.join(RAW, "rows.json"), "w", encoding="utf-8") as handle:
        json.dump(manifest, handle)
    return True


def main():
    os.makedirs(RAW, exist_ok=True)
    arm = bpy.data.objects["DummyArmature"]
    arm.rotation_euler = (0.0, 0.0, 0.0)
    arm.location = (0.0, 0.0, 0.0)
    for bone in arm.pose.bones:
        bone.rotation_mode = "XYZ"
        bone.location = (0.0, 0.0, 0.0)
        bone.rotation_euler = (0.0, 0.0, 0.0)
    bpy.context.view_layer.update()
    specs = json.load(open(LOADOUTS, encoding="utf-8"))["sets"]
    clones = []
    for spec in specs:
        show_arm, meshes = clone_rig(spec["id"])
        apply_loadout(meshes, spec["pieces"])
        paint(meshes, spec["color"], spec["id"])
        clones.append((spec, show_arm, meshes))
    hide_source()
    # One character's bounds at the origin, then lift every clone onto the floor.
    set_group(clones[:1], True)
    set_group(clones[1:], False)
    place(clones[:1], "front", 1.2, 0.0)
    mins, _maxs = world_bounds(shown_meshes(clones[:1]))
    z_lift = 0.006 - mins.z
    log("Z-LIFT %.4f" % z_lift)
    head = bpy.data.objects.get("Mesh_Head_Reed_1_Hood")
    if head is not None:
        log("HEAD-Z %.3f" % head.matrix_world.translation.z)
    make_floor()
    make_lights()
    make_world()
    configure_render()
    cam_data = bpy.data.cameras.new("LabCam")
    cam = bpy.data.objects.new("LabCam", cam_data)
    link(cam)
    shot_lineup(cam, clones, z_lift, "front", os.path.join(RAW, "lineup-front.png"), 1280, 720, 1.15)
    shot_lineup(cam, clones, z_lift, "three", os.path.join(RAW, "lineup-three-quarter.png"), 1280, 720, 1.15)
    shot_lineup(cam, clones, z_lift, "side", os.path.join(RAW, "lineup-side.png"), 1280, 720, 1.15)
    px = shot_readability(cam, clones, z_lift, os.path.join(RAW, "readability-30px.png"))
    ok = shot_variants(cam, clones, z_lift)
    with open(os.path.join(RAW, "measure.txt"), "w", encoding="utf-8") as handle:
        handle.write("read-px %.2f\ngrid %s\n" % (px, "ok" if ok else "miss"))
    if not ok:
        raise SystemExit("variant grid did not separate")
    subprocess.check_call([sys.executable.replace("blender", "python3") if False else "python3",
                           os.path.join(BLEND_DIR, "pack_stills.py")])


if __name__ == "__main__":
    main()
