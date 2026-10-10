"""Results card frames. Same Hier idle family, same camera, soles at 0.5 cm.

Rank 0 celebrates: both arms in a V, a lean back, no hop.
Rank 1 raises one fist, elbow about 90 degrees. Rank 2 stands vertical
with the hip over the straight leg and the other knee soft. Rank 3 drops
the head about 25 degrees and rounds the shoulders.
Hanging arms keep the upper arm about 12 degrees off the torso and soften the elbow.
The root is not leaned over. Soles are planted at 0.5 cm.
Nothing adds MenuAlive.Hop.

On this FBX the raise axis is local Y. A side lean is local Y on the root.
The degrees below are the sampled frame, not a second clip.
"""
import math
import os
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT_DIR = os.environ.get("RESULTPOSE_OUT", "/tmp/resultpose")
ATLAS = os.path.join(ROOT, "Assets", "Resources", "UI", "Menu", "SeatResult.png")


def unity_euler(x, y, z):
    from mathutils import Euler

    return Euler((math.radians(x), math.radians(y), math.radians(z)), "ZXY").to_quaternion()


def set_delta(obj, x, y, z):
    if obj is None:
        return
    rest = obj.rotation_euler.to_quaternion()
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = rest @ unity_euler(x, y, z)


def bake_pivot(obj, pivot, deg):
    """Rotate mesh vertices around a world point. Object rotation does not nod this head."""
    if obj is None or getattr(obj, "type", "") != "MESH":
        return
    import bpy
    from mathutils import Matrix

    bpy.context.view_layer.update()
    rot = Matrix.Rotation(math.radians(deg), 4, "X")
    turn = Matrix.Translation(pivot) @ rot @ Matrix.Translation(pivot).inverted()
    world = obj.matrix_world.copy()
    inv = world.inverted()
    cos = [v.co.copy() for v in obj.data.vertices]
    for vert, co in zip(obj.data.vertices, cos):
        vert.co = inv @ (turn @ (world @ co))
    obj.data.update()


def hang_elbows(sl, objs, pitch=-14, yaw=26):
    """Upper arms stay on the bind, about 12 degrees off the torso."""
    set_delta(sl.find_named(objs, "LowerArm_L"), pitch, -yaw, 0)
    set_delta(sl.find_named(objs, "LowerArm_R"), pitch, yaw, 0)


def pose_frame(sl, objs, kind):
    """One results pose on the idle knee rest. No root lift."""
    arm_l = sl.find_named(objs, "UpperArm_L")
    arm_r = sl.find_named(objs, "UpperArm_R")
    fore_l = sl.find_named(objs, "LowerArm_L")
    fore_r = sl.find_named(objs, "LowerArm_R")
    head = sl.find_named(objs, "Head")
    spine = sl.find_named(objs, "Spine")
    root = sl.find_named(objs, "DummyRoot")
    if kind == "cheer":
        # V. Yaw raises the arm. A little pitch and roll keep the hands off the ears.
        # RootPitch -16, Head -14, Spine -2 match MenuAlive.Cheer at lean 1, sin 0.
        set_delta(arm_l, 18, 136, 12)
        set_delta(arm_r, 18, -136, -12)
        set_delta(spine, -2, 0, 0)
        set_delta(head, -14, 0, 0)
        set_delta(root, -16, 0, 0)
    elif kind == "pump":
        # Elbow about 95 degrees. The fist sits above the shoulder and the
        # forearm points up and toward the camera. The other arm hangs.
        set_delta(fore_l, -14, -26, 0)
        set_delta(arm_r, 40, -130, 0)
        set_delta(fore_r, -60, -80, 0)
        set_delta(head, -4, 6, 0)
    elif kind == "shift":
        # Torso stays vertical. The hip moves over the straight leg.
        # The free knee softens, and the thigh brings that sole back down.
        hang_elbows(sl, objs)
        hips = sl.find_named(objs, "Hips")
        pelvis = sl.find_named(objs, "PelvisMesh")
        if hips is not None:
            hips.location.x += -0.06
        if pelvis is not None:
            pelvis.location.x += -0.06
        set_delta(hips, 0, 18, 0)
        set_delta(sl.find_named(objs, "UpperLeg_R"), 12, 0, 0)
        set_delta(sl.find_named(objs, "LowerLeg_R"), -52, 0, 0)
        set_delta(sl.find_named(objs, "Foot_R"), 34, 0, 0)
        set_delta(head, 0, -8, 0)
    else:
        # Head forward about 25 degrees around the neck, shoulders rounded,
        # arms heavier. The root stays upright so the body does not bow.
        hang_elbows(sl, objs, -24, 42)
        set_delta(arm_l, 16, 0, 0)
        set_delta(arm_r, 16, 0, 0)
        set_delta(spine, 12, 0, 0)
        import bpy

        bpy.context.view_layer.update()
        neck = head.matrix_world.translation.copy()
        spine_at = spine.matrix_world.translation.copy()
        bake_pivot(sl.find_named(objs, "HeadMesh"), neck, 25)
        for token in ("Panel_Chest", "ChestPlate", "Panel_Side_L", "Panel_Side_R", "Panel_Back", "Panel_Abs"):
            bake_pivot(sl.find_named(objs, token), spine_at, 20)
    import bpy

    bpy.context.view_layer.update()


def render_main():
    sys.path.insert(0, os.path.dirname(__file__))
    import render_seatload as sl

    os.makedirs(OUT_DIR, exist_ok=True)
    skip = set(p for p in os.environ.get("RESULTPOSE_SKIP", "").split(",") if p)
    for index, (prefix, path, color, kind) in enumerate(sl.SEATS):
        for pose in ("cheer", "pump", "shift", "slump"):
            if pose in skip:
                continue
            sl.wipe()
            sl.import_prefixed(path, prefix)
            objs = [obj for obj in __import__("bpy").data.objects if obj.name.startswith(prefix)]
            roots = [obj for obj in objs if obj.parent is None]
            sl.stand_up(roots, objs)
            sl.pose_idle(objs)
            # Lock the camera before the rank pose. A side lean must not swing it
            # onto the body's edge, or the weight shift reads as a thin column.
            face = sl.face_axis(objs)
            pose_frame(sl, objs, pose)
            sl.plant(roots, objs)
            sl.tint(objs, color, prefix)
            lo, hi = sl.world_bounds(objs)
            mid = (lo + hi) * 0.5
            # Center only. Extra yaw is skipped: the root is a quaternion after the
            # lean, and the celebrate frame that reads was framed without that yaw.
            for root in roots:
                root.location.x += -mid.x
            __import__("bpy").context.view_layer.update()
            sl.chest_mark(objs, kind, color)
            feet = [obj for obj in sl.meshes(objs) if "Foot" in obj.name]
            acc = __import__("mathutils").Vector((0, 0, 0))
            n = 0
            for obj in feet:
                acc += obj.matrix_world.translation
                n += 1
            foot = acc / max(1, n)
            sl.disc(foot.x, foot.y, color)
            lo, hi = sl.world_bounds(objs)

            def mesh_sole(token):
                mesh = sl.find_named(objs, token)
                if mesh is None:
                    return None
                return min((mesh.matrix_world @ v.co).z for v in mesh.data.vertices)

            print(
                prefix,
                pose,
                "bounds",
                tuple(round(v, 3) for v in lo),
                tuple(round(v, 3) for v in hi),
                "sole",
                round(sl.foot_min_z(objs), 4),
                "L",
                round(mesh_sole("FootMesh_L"), 4),
                "R",
                round(mesh_sole("FootMesh_R"), 4),
                "height",
                round(hi.z - lo.z, 3),
            )
            sl.lights()
            sl.camera(face)
            sl.render(os.path.join(OUT_DIR, "%s%d.png" % (pose, index)))


def pack():
    from PIL import Image

    raws = []
    names = []
    poses = ("cheer", "pump", "shift", "slump")
    for pose in poses:
        for i in range(4):
            path = os.path.join(OUT_DIR, "%s%d.png" % (pose, i))
            im = Image.open(path).convert("RGBA")
            raws.append(im)
            names.append(os.path.basename(path))
    bb = None
    for im in raws:
        got = im.split()[-1].getbbox()
        if got is None:
            raise SystemExit("empty frame")
        if bb is None:
            bb = got
        else:
            bb = (min(bb[0], got[0]), min(bb[1], got[1]), max(bb[2], got[2]), max(bb[3], got[3]))
    pad = 12
    w0, h0 = raws[0].size
    box = (
        max(0, bb[0] - pad),
        max(0, bb[1] - pad),
        min(w0, bb[2] + pad),
        min(h0, bb[3] + pad),
    )
    cw, ch = box[2] - box[0], box[3] - box[1]
    side = max(cw, ch)
    # One window for both poses, so a raised arm does not shrink the body.
    ox = (side - cw) // 2
    oy = side - ch
    cells = []
    for im in raws:
        cell = Image.new("RGBA", (side, side), (0, 0, 0, 0))
        cell.paste(im.crop(box), (ox, oy))
        cells.append(cell)
    atlas = Image.new("RGBA", (side * 4, side * len(poses)), (0, 0, 0, 0))
    for i, cell in enumerate(cells):
        col = i % 4
        row = i // 4
        atlas.paste(cell, (col * side, row * side))
    # Importer cap is 2048. Four columns at that width stay sharp on a 180 px card.
    if atlas.size[0] > 2048:
        nh = int(round(2048 * atlas.size[1] / atlas.size[0]))
        atlas = atlas.resize((2048, nh), Image.Resampling.LANCZOS)
    os.makedirs(os.path.dirname(ATLAS), exist_ok=True)
    atlas.save(ATLAS, "PNG", optimize=True)
    print("atlas", ATLAS, atlas.size, os.path.getsize(ATLAS), "crop", box, "side", side)
    # Feet should land on the same row. Compare opaque bottoms.
    for i, cell in enumerate(cells):
        got = cell.split()[-1].getbbox()
        print(names[i], "cell bbox", got, "bottom", None if got is None else side - got[3])


if __name__ == "__main__":
    if "--pack" in sys.argv:
        pack()
    else:
        render_main()
