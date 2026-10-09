"""Results card frames. Same Hier idle family, same camera, soles at 0.5 cm.

First place samples a celebrate frame: both arms in a V, a lean back, no hop.
Second through fourth sample a relaxed stand. The arms leave the bind hang.
The root is rotated for the lean, then planted so the sole stays at 0.5 cm.
Nothing adds MenuAlive.Hop.

On this FBX the raise axis is local Y. Unity's ArmPitch is that raise on the
imported Unity bone. The degrees below are the sampled frame, not a second clip.
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


def pose_frame(sl, objs, kind):
    """Celebrate or relaxed stand, on top of the idle knee rest. No root lift."""
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
    else:
        # Ready's open chest, with the arms off the bind hang.
        # Ready's own arm deltas are about 5 degrees and still read as the bind.
        # Yaw 18 opens the upper arm. The elbow bend breaks the straight hang.
        set_delta(arm_l, -12, 18, 0)
        set_delta(arm_r, -12, -18, 0)
        set_delta(fore_l, -24, 0, 0)
        set_delta(fore_r, -24, 0, 0)
        set_delta(spine, -2, 0, 0)
        set_delta(head, -4, 0, 0)
        set_delta(root, -10, 0, 0)
    import bpy

    bpy.context.view_layer.update()


def render_main():
    sys.path.insert(0, os.path.dirname(__file__))
    import render_seatload as sl

    os.makedirs(OUT_DIR, exist_ok=True)
    for index, (prefix, path, color, kind) in enumerate(sl.SEATS):
        for pose in ("cheer", "stand"):
            sl.wipe()
            sl.import_prefixed(path, prefix)
            objs = [obj for obj in __import__("bpy").data.objects if obj.name.startswith(prefix)]
            roots = [obj for obj in objs if obj.parent is None]
            sl.stand_up(roots, objs)
            sl.pose_idle(objs)
            pose_frame(sl, objs, pose)
            sl.plant(roots, objs)
            sl.tint(objs, color, prefix)
            lo, hi = sl.world_bounds(objs)
            mid = (lo + hi) * 0.5
            sl.move_root(roots, -mid.x, sl.YAW)
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
            print(
                prefix,
                pose,
                "bounds",
                tuple(round(v, 3) for v in lo),
                tuple(round(v, 3) for v in hi),
                "sole",
                round(sl.foot_min_z(objs), 4),
                "height",
                round(hi.z - lo.z, 3),
            )
            face = sl.face_axis(objs)
            sl.lights()
            sl.camera(face)
            sl.render(os.path.join(OUT_DIR, "%s%d.png" % (pose, index)))


def pack():
    from PIL import Image

    raws = []
    names = []
    for pose in ("cheer", "stand"):
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
    atlas = Image.new("RGBA", (side * 4, side * 2), (0, 0, 0, 0))
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
