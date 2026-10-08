"""Four seat cameras, one split frame.

Each pane is the couch chase camera: pivot 1.4 m, boom (right 0.4, up 0.45,
back 5.2), 70 degree lens. Roped seats reach with the left hand. The rope is
a thin line from that hand to a tree ahead, and it stays outside the body.
"""
import importlib.util
import math
import os

import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
spec = importlib.util.spec_from_file_location(
    "pass17", os.path.join(ROOT, "Tools", "Tag", "render_pass17_hier.py")
)
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)

OUT = os.path.join(ROOT, "Docs", "SmoothStills", "pass19", "couch-grapple.png")
TMP = "/tmp/couch_seats"
ROPE = (0.95, 0.86, 0.28, 1.0)
BARK = (0.34, 0.26, 0.16, 1.0)
LEAF = (0.22, 0.38, 0.18, 1.0)
ROPE_R = 0.012

# CouchPlay.Pane for 4 humans: 0 top-left, 1 top-right, 2 bottom-left, 3 bottom-right.
SEATS = (
    ("P1   RMB   pull", True),
    ("P2   LT   release", False),
    ("P3   LT   pull", True),
    ("P4   LT   idle", False),
)


def object_mode():
    if bpy.context.object is not None and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")


def clear_props():
    object_mode()
    for obj in list(bpy.data.objects):
        if obj.name.startswith("Prop"):
            bpy.data.objects.remove(obj, do_unlink=True)


def pose_stand(arm):
    p.clear_pose(arm)
    p.torso(arm, 0.0, 0.0, 0.0)
    p.arm_pose(arm, "L", 12.0, 8.0, -16.0, 0.0)
    p.arm_pose(arm, "R", 12.0, 8.0, -16.0, 0.0)
    p.leg(arm, "L", 4.0, -6.0)
    p.leg(arm, "R", 4.0, -6.0)


def pose_pull(arm):
    """Chest leans into the latch. The left arm stays on the character's left."""
    p.clear_pose(arm)
    p.torso(arm, 24.0, 16.0, -8.0)
    # Negative yaw keeps Hand_L on +X. Positive yaw crosses it onto the right side.
    p.arm_pose(arm, "L", -70.0, -50.0, -8.0, 0.0)
    p.arm_pose(arm, "R", -16.0, -20.0, -40.0, 0.0)
    p.leg(arm, "L", -10.0, -18.0)
    p.leg(arm, "R", 26.0, -40.0)


def mesh_bvh(name):
    obj = bpy.data.objects.get(name)
    if obj is None or obj.type != "MESH":
        return None
    deps = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(deps)
    mesh = ev.to_mesh()
    mesh.transform(ev.matrix_world)
    verts = [v.co.copy() for v in mesh.vertices]
    polys = [tuple(poly.vertices) for poly in mesh.polygons]
    ev.to_mesh_clear()
    if len(verts) < 3 or not polys:
        return None
    return BVHTree.FromPolygons(verts, polys)


def body_bvhs():
    found = []
    for obj in bpy.data.objects:
        if obj.type == "MESH" and obj.name.startswith("Mesh_"):
            tree = mesh_bvh(obj.name)
            if tree is not None:
                found.append((obj.name, tree))
    return found


def rope_hits_body(hand, anchor, trees):
    """Samples past the grip. A point inside a piece, or closer than the rope, hits."""
    hits = 0
    worst = 99.0
    worst_name = ""
    for i in range(3, 33):
        t = i / 32.0
        pt = hand.lerp(anchor, t)
        for name, tree in trees:
            if name == "Mesh_Hand_L" and t < 0.12:
                continue
            loc, normal, _idx, dist = tree.find_nearest(pt)
            if loc is None:
                continue
            inside = (pt - loc).dot(normal) < -0.001 and dist < 0.06
            close = dist < ROPE_R + 0.02
            if inside or close:
                hits += 1
                if dist < worst:
                    worst = dist
                    worst_name = name
                break
    return hits, worst, worst_name


def place_rope(arm):
    bpy.context.view_layer.update()
    hand = p.bone_tail(arm, "Hand_L")
    # Rest Hand_L sits on +X. That is the character's left. A negative X is the other hand.
    if hand.x < 0.15:
        print("HANDSIDE", "not-left", tuple(round(v, 3) for v in hand))
        return 2
    flat = Vector((0.55, -1.15, 0.0))
    flat.normalize()
    foot = Vector((hand.x, hand.y, 0.0)) + flat * 7.2
    latch_z = 1.7
    anchor = Vector((foot.x, foot.y, latch_z))
    trees = body_bvhs()
    hits, worst, name = rope_hits_body(hand, anchor, trees)
    if hits:
        anchor = Vector((foot.x, foot.y, 2.4))
        hits, worst, name = rope_hits_body(hand, anchor, trees)
    print("ROPE", "hits", hits, "nearest_m", round(worst, 3), "piece", name or "-")
    print("HAND", tuple(round(v, 3) for v in hand), "ANCHOR", tuple(round(v, 3) for v in anchor))
    p.add_cyl("PropRope", hand, anchor, ROPE_R, ROPE)
    top = Vector((foot.x, foot.y, 3.4))
    p.add_cyl("PropTrunk", foot, top, 0.16, BARK)
    bpy.ops.mesh.primitive_ico_sphere_add(radius=0.85, location=(foot.x, foot.y, top.z - 0.15))
    crown = bpy.context.active_object
    crown.name = "PropCrown"
    crown.data.materials.append(p.make_mat("PropCrownMat", LEAF, 0.7))
    return hits


def seat_camera(cam, look):
    cam.data.type = "PERSP"
    cam.data.sensor_width = 36.0
    cam.data.lens = 25.7
    cam.data.clip_start = 0.15
    cam.data.clip_end = 80.0
    # Character faces -Y. Boom is behind (+Y), on the character's right (-X).
    cam.location = Vector((-0.4, 5.2, 1.85))
    p.look_at(cam, look)


def render_seat(scene, cam, path):
    scene.camera = cam
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def composite():
    from PIL import Image, ImageDraw, ImageFont

    pane_w, pane_h = 480, 270
    sheet = Image.new("RGB", (pane_w * 2, pane_h * 2), (8, 8, 10))
    slots = ((0, 0), (1, 0), (0, 1), (1, 1))
    try:
        font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
    for i, ((col, row), (label, _pull)) in enumerate(zip(slots, SEATS)):
        tile = Image.open(os.path.join(TMP, "seat-%d.png" % i)).convert("RGB")
        tile = tile.resize((pane_w, pane_h), Image.Resampling.LANCZOS)
        sheet.paste(tile, (col * pane_w, row * pane_h))
        draw = ImageDraw.Draw(sheet)
        x = col * pane_w + 8
        y = row * pane_h + 6
        draw.rectangle((x - 4, y - 2, x + 168, y + 20), fill=(0, 0, 0))
        draw.text((x, y), label, fill=(245, 245, 245), font=font)
    # Split gutters, the way four viewports meet.
    draw = ImageDraw.Draw(sheet)
    mid_x = pane_w
    mid_y = pane_h
    draw.rectangle((mid_x - 2, 0, mid_x + 2, pane_h * 2), fill=(0, 0, 0))
    draw.rectangle((0, mid_y - 2, pane_w * 2, mid_y + 2), fill=(0, 0, 0))
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    sheet.save(OUT, "PNG", optimize=True)
    size = os.path.getsize(OUT)
    if size > 400 * 1024:
        q = sheet.quantize(colors=128, method=Image.Quantize.MEDIANCUT)
        q.save(OUT, "PNG", optimize=True)
        size = os.path.getsize(OUT)
    print("STILL", OUT, "bytes", size)


def main():
    os.makedirs(TMP, exist_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=p.FBX)
    p.tint()
    arm = bpy.data.objects["DummyArmature"]
    object_mode()
    bpy.ops.mesh.primitive_plane_add(size=80, location=(0.0, 0.0, 0.0))
    ground = bpy.context.active_object
    ground.name = "Ground"
    ground.data.materials.append(p.make_mat("GroundMat", p.GROUND, 0.92))
    world = bpy.data.worlds.new("Sky")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = p.SKY
    bg.inputs["Strength"].default_value = 0.9
    bpy.ops.object.light_add(type="SUN", location=(3.0, 6.0, 8.0))
    sun = bpy.context.active_object
    sun.data.energy = 3.0
    p.look_at(sun, Vector((0.0, -2.0, 1.0)))
    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 480
    scene.render.resolution_y = 270
    scene.eevee.taa_render_samples = 8
    scene.render.film_transparent = False

    rope_fail = 0
    for i, (_label, pulling) in enumerate(SEATS):
        clear_props()
        p.ensure_pose(arm)
        if pulling:
            pose_pull(arm)
        else:
            pose_stand(arm)
        bpy.context.view_layer.update()
        if pulling:
            rope_fail += place_rope(arm)
            look = Vector((1.6, -3.6, 1.45))
        else:
            look = Vector((0.0, -1.2, 1.15))
        seat_camera(cam, look)
        render_seat(scene, cam, os.path.join(TMP, "seat-%d.png" % i))
        print("SEAT", i, "pull", pulling)
    print("ROPEBODY", rope_fail)
    composite()
    if rope_fail:
        raise SystemExit("rope passed through a body")


if __name__ == "__main__":
    main()
