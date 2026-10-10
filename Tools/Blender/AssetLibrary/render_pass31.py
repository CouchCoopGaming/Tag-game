"""Pass 31 still quartets for geometry-clean library meshes.

Quarter, side, close-up, and a 1.8 m figure. JPEG quality 90, 1280x720.
Meshes that already pass the lead still check are left on their earlier pass.
"""

import json
import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import render_pass6 as p6  # noqa: E402
import render_pass22 as p22  # noqa: E402

STILL_DIR = os.path.join(os.path.dirname(p22.STILL_DIR), "pass31")
GROUND = (0.34, 0.36, 0.32)
MANIFEST = os.path.join(os.path.dirname(os.path.abspath(__file__)), "manifest.json")

# Already graded pass on pass30. A newer auto frame would replace those stills.
SKIP = {
    "Cabin", "GasCanopy", "Ranch_House", "WalkUp",
    "WoodFence", "WoodFence_Corner", "WoodFence_End", "WoodFence_Gate",
    "Boathouse", "Container_20", "Container_20_Blue", "Container_20_Green",
    "Dock_Straight", "FishingBoat", "HarborShed", "Rowboat",
    "CourtFence", "Gazebo", "Road_Junction",
}

# Harbor and pond pieces whose ground is the water, not the concrete pad.
WATER = {
    "Boat", "Buoy", "Cleat", "Crate", "Dock_Corner", "DockRamp", "FishCrate",
    "FuelDock", "Gangway", "HarborCrane", "HarborRail", "LifeRing", "LobsterTrap",
    "Mooring", "MooringLine", "Piling", "Pond", "PondEdge",     "QuayDavit",
    "Quay_Edge", "RopeCoil", "Ferry",
}


def _stem(name):
    return "_".join(part.lower() for part in name.split("_"))


def _aim(scene, eye, aim, lens):
    old = scene.camera
    if old is not None:
        data = old.data
        bpy.data.objects.remove(old, do_unlink=True)
        if data is not None and data.users == 0:
            bpy.data.cameras.remove(data)
    return p6._look(scene, eye, aim, lens)


def _push_out(eye, sx, sy, sz):
    hx, hz = sx * 0.5, sz * 0.5
    x, y, z = eye
    if abs(x) > hx + 0.3 or abs(z) > hz + 0.3 or y > sy + 0.4:
        return eye
    # Keep the same direction and step past the footprint.
    ax = abs(x) if abs(x) > 0.05 else 0.2
    az = abs(z) if abs(z) > 0.05 else 0.2
    scale = max((hx + 0.55) / ax, (hz + 0.55) / az, 1.0)
    return (x * scale, max(y, sy * 0.45 + 0.6), z * scale)


def _cameras(size):
    sx, sy, sz = [max(float(v), 0.12) for v in size]
    span = max(sx, sy, sz)
    dist = span * 0.95 + 1.35
    if span > 10.0:
        lens = 24.0
    elif span > 5.0:
        lens = 28.0
    elif span > 2.0:
        lens = 34.0
    elif span < 0.7:
        lens = 50.0
    else:
        lens = 40.0
    aim = (0.0, min(sy * 0.42, 3.2), 0.0)
    eye_q = _push_out(
        (sx * 0.42 + dist * 0.62, sy * 0.38 + dist * 0.34 + 0.45, sz * 0.38 + dist * 0.72),
        sx, sy, sz,
    )
    side_y = sy * 0.36 + max(1.05, dist * 0.20)
    if sx >= sz:
        eye_s = _push_out((sx * 0.04, side_y, sz * 0.5 + dist * 0.92), sx, sy, sz)
    else:
        eye_s = _push_out((sx * 0.5 + dist * 0.92, side_y, sz * 0.04), sx, sy, sz)
    aim_c = (
        max(-sx * 0.2, min(sx * 0.18, 0.7)),
        max(0.12, min(sy * 0.62, sy - 0.02)),
        max(-sz * 0.15, min(sz * 0.22, 0.8)),
    )
    eye_c = (
        aim_c[0] + max(0.28, min(sx, 1.1) * 0.28 + 0.18),
        aim_c[1] + max(0.16, min(sy, 1.2) * 0.12 + 0.08),
        aim_c[2] + max(0.36, min(sz, 1.3) * 0.30 + 0.16),
    )
    lens_c = 55.0 if span < 3.0 else 42.0
    # Near the quarter camera, just outside the +X / +Z corner.
    figure = (sx * 0.5 + 0.55, 0.0, sz * 0.5 + 0.25)
    return eye_q, aim, lens, eye_s, eye_c, aim_c, lens_c, figure


def _fresh(path):
    if not os.path.isfile(path):
        return False
    size = os.path.getsize(path)
    return 800 < size <= 400 * 1024


def _quartet(found, name, size, water, force):
    stem = _stem(name)
    eye_q, aim, lens, eye_s, eye_c, aim_c, lens_c, figure = _cameras(size)
    jobs = (
        ("quarter", eye_q, aim, lens, False),
        ("side", eye_s, aim, lens, False),
        ("close", eye_c, aim_c, lens_c, False),
        ("scale", eye_q, aim, lens, True),
    )
    pending = []
    for role, eye, look, use_lens, with_figure in jobs:
        path = os.path.join(STILL_DIR, "%s_%s.jpg" % (stem, role))
        if not force and _fresh(path):
            print("KEEP", path, flush=True)
            continue
        pending.append((role, eye, look, use_lens, with_figure, path))
    if not pending:
        return
    scene = p6._begin(wide=True)
    p6._place(found, [(name, (0.0, 0.0, 0.0), 18.0, 1.0)])
    if water:
        p22._dock_water()
    else:
        p6._ground(GROUND)
    hier = False
    for role, eye, look, use_lens, with_figure, path in pending:
        if with_figure and not hier:
            p22._place_hier(figure, 210.0)
            hier = True
        print("SHOT", stem, role, flush=True)
        _aim(scene, eye, look, use_lens)
        p22._jpg(scene, path)


def main():
    os.makedirs(STILL_DIR, exist_ok=True)
    only = None
    if "--only" in sys.argv:
        only = sys.argv[sys.argv.index("--only") + 1].split(",")
    force = "--force" in sys.argv
    manifest = json.load(open(MANIFEST, encoding="utf-8"))
    found = p22.r._catalog()
    for entry in manifest:
        name = entry["name"]
        if name in SKIP or name not in found:
            continue
        stem = _stem(name)
        if only and not any(stem.startswith(part) or name.startswith(part) for part in only):
            continue
        print("ASSET", name, flush=True)
        _quartet(found, name, entry["size"], name in WATER, force)
    print("PASS31_DONE", STILL_DIR)


if __name__ == "__main__":
    main()
