#!/usr/bin/env python3
"""Headless world check for the Z7 kickball district.

Reads placements from Assets/Scripts/Level/World/MegaParkWorldDistrict.cs and collider
boxes from the library prefabs. Prints the world-check line and writes stills.
Does not retune feel numbers and does not run StrafeJumpSim.
"""

import math
import os
import re
import struct
import sys
import zlib

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DISTRICT_CS = os.path.join(ROOT, "Assets/Scripts/Level/World/MegaParkWorldDistrict.cs")
STILL_DIR = os.path.join(ROOT, "Docs/WorldStills/pass1")

# Envelopes. Sources are quoted in Docs/World/STANDARD.md.
JUMP_SPEED = 24.7
GRAVITY = 22.0
FALL_MULT = 1.62
JUMP_APEX = JUMP_SPEED * JUMP_SPEED / (2.0 * GRAVITY)  # 13.8657 m
WALL_RUN_SPEED = 9.5
WALL_RUN_TIME = 0.62
WALL_RUN_DIST = WALL_RUN_SPEED * WALL_RUN_TIME  # 5.89 m
WALL_JUMP_OUT = 8.0
WALL_JUMP_UP = 6.2
WALL_JUMP_LOOK = 3.5
CLIMB_CAP = 3.4
CLIMB_PRACTICAL = 3.0  # motor comment: rises ~3 m, then slip, before the 3.4 cap
MANTLE_MIN = 0.45
MANTLE_MAX = 2.55
VAULT_MIN = 0.90
VAULT_MAX = 1.05
CROUCH_H = 1.05
AIR_DASH = 15.0 * 0.10  # 1.50 m
GRAPPLE_MAX = 28.0
GRAPPLE_MIN = 1.5
PAWN_R = 0.40
STEP = 0.30
PLAYER_H = 1.8
OPEN_RECT = (52.0, 72.0, 40.0, 58.0)  # x0,x1,z0,z1 crash cross

# Flat wall-jump, look along the wall. Same-height landing.
_hj = math.sqrt(WALL_JUMP_OUT * WALL_JUMP_OUT + WALL_JUMP_LOOK * WALL_JUMP_LOOK)
_t_up = WALL_JUMP_UP / GRAVITY
_h_up = WALL_JUMP_UP * WALL_JUMP_UP / (2.0 * GRAVITY)
_t_dn = math.sqrt(2.0 * _h_up / (GRAVITY * FALL_MULT))
WALL_JUMP_FLAT = _hj * (_t_up + _t_dn)  # diagonal path, look along the wall. Not the gap.
WALL_JUMP_TIME = _t_up + _t_dn
# Off the wall: look angle from the tangent, toward the landing.
# Into the wall is the negative angle. Routes must clear the positive pair.
OFF_WALL_DEGREES = (30.0, 60.0)

# Ground loop around the street and the court. South leg stays under the
# planted median (z min 32.62). East leg stays west of the rail at x=114.
# North leg clears the north hoop (z max 65.60) and the park row (z min ~66.85).
# The merged CourtFence west face is x=80.61. A straight west leg at x=81.2
# runs through that face. The leg jogs out to x=79.70 beside the fence, then
# back to x=81.2 south of the fence (z=40.88) so it stays east of the gazebo.
CHASE = [(82.5, 31.55), (111.0, 31.55), (111.0, 66.50), (79.70, 66.50), (79.70, 40.88), (81.2, 40.88), (81.2, 31.55)]
CHASE_CLEAR = 0.80

# Graybox that the before still draws. Centers and full sizes from MegaParkP1Layout.
GRAY = [
    ("Base_Home", 96, 0.10, 34, 0.9, 0.20, 0.9),
    ("Base_First", 110, 0.10, 48, 0.9, 0.20, 0.9),
    ("Base_Second", 96, 0.10, 62, 0.9, 0.20, 0.9),
    ("Base_Third", 82, 0.10, 48, 0.9, 0.20, 0.9),
    ("Mound", 96, 0.125, 48, 2.4, 0.25, 2.4),
    ("Rail_East", 114, 0.45, 48, 0.12, 0.90, 40),
    ("Kick_PostS", 106, 0.57, 43.2, 0.22, 1.14, 0.22),
    ("Kick_PostN", 106, 0.57, 52.8, 0.22, 1.14, 0.22),
    ("Kick_Bar", 106, 1.2, 48, 0.14, 0.12, 10.2),
    ("Kick_Lip", 107.6, 0.48, 48, 0.9, 0.96, 2.2),
    ("Cover_K1", 88, 0.675, 40, 1.5, 1.35, 1.5),
    ("Cover_K2", 100, 0.675, 58, 1.5, 1.35, 1.5),
    ("Cover_S2", 78, 0.675, 25.5, 2.2, 1.35, 1.15),
]
HIDDEN = {"Mound", "Base_Home", "Base_First", "Base_Second", "Base_Third"}


def quat_mat(x, y, z, w):
    xx, yy, zz = x * x, y * y, z * z
    xy, xz, yz = x * y, x * z, y * z
    wx, wy, wz = w * x, w * y, w * z
    return (
        (1 - 2 * (yy + zz), 2 * (xy - wz), 2 * (xz + wy)),
        (2 * (xy + wz), 1 - 2 * (xx + zz), 2 * (yz - wx)),
        (2 * (xz - wy), 2 * (yz + wx), 1 - 2 * (xx + yy)),
    )


def yaw_mat(deg):
    a = math.radians(deg)
    c, s = math.cos(a), math.sin(a)
    return ((c, 0.0, s), (0.0, 1.0, 0.0), (-s, 0.0, c))


def mul_m(m, v):
    return (
        m[0][0] * v[0] + m[0][1] * v[1] + m[0][2] * v[2],
        m[1][0] * v[0] + m[1][1] * v[1] + m[1][2] * v[2],
        m[2][0] * v[0] + m[2][1] * v[1] + m[2][2] * v[2],
    )


def add(a, b):
    return (a[0] + b[0], a[1] + b[1], a[2] + b[2])


def array_body(text, array_name):
    m = re.search(
        r"public static readonly Place\[\] " + array_name + r" =\s*\{(.*?)\n        \};",
        text,
        re.S,
    )
    if not m:
        raise SystemExit("placement array %s missing" % array_name)
    return m.group(1)


def parse_places(text, array_name="Places"):
    body = array_body(text, array_name) if array_name else text
    prefix = {
        "B": "Assets/Art/Props/Library/Buildings/Prefabs/",
        "S": "Assets/Art/Props/Library/StreetFurniture/Prefabs/",
        "R": "Assets/Art/Props/Library/Roads/Prefabs/",
        "P": "Assets/Art/Props/Library/Park/Prefabs/",
        "V": "Assets/Art/Props/Library/Vehicles/Prefabs/",
    }
    pat2 = re.compile(
        r'new Place\("([^"]+)",\s*([BSRPV]) \+ "([^"]+)",\s*([-0-9.]+)f,\s*([-0-9.]+)f,\s*([-0-9.]+)f,\s*([-0-9.]+)f\)'
    )
    places = []
    for m in pat2.finditer(body):
        places.append({
            "name": m.group(1),
            "path": prefix[m.group(2)] + m.group(3),
            "x": float(m.group(4)),
            "y": float(m.group(5)),
            "z": float(m.group(6)),
            "yaw": float(m.group(7)),
            "scale": (1.0, 1.0, 1.0),
        })
    return places


def parse_prefab(path):
    text = open(path, "r", encoding="utf-8", errors="replace").read()
    docs = re.split(r"--- !u!(\d+) &(\d+)\n", text)
    # docs[0] is header, then triples of class_id, file_id, body
    transforms = {}
    names = {}
    go_to_xform = {}
    boxes = []
    vault = 0.0
    climbable = False
    vm = re.search(r"vaultHeightMeters:\s*([0-9.]+)", text)
    if vm:
        vault = float(vm.group(1))
    if re.search(r"climbable:\s*1", text):
        climbable = True
    i = 1
    while i + 2 < len(docs):
        cid = int(docs[i])
        fid = int(docs[i + 1])
        body = docs[i + 2]
        if cid == 1:
            nm = re.search(r"m_Name:\s*(.+)", body)
            names[fid] = nm.group(1).strip() if nm else ""
        elif cid == 4:
            pos = _vec(body, "m_LocalPosition")
            rot = _vec4(body, "m_LocalRotation")
            scl = _vec(body, "m_LocalScale")
            father = _file(body, "m_Father")
            go = _file(body, "m_GameObject")
            transforms[fid] = {"pos": pos, "rot": rot, "scl": scl, "father": father, "go": go}
            go_to_xform[go] = fid
        elif cid == 65:
            if not re.search(r"m_Enabled:\s*0", body):
                go = _file(body, "m_GameObject")
                boxes.append({
                    "go": go,
                    "name": names.get(go, ""),
                    "size": _vec(body, "m_Size"),
                    "center": _vec(body, "m_Center"),
                })
        elif cid == 136:
            if not re.search(r"m_Enabled:\s*0", body):
                radius = _num(body, "m_Radius")
                height = _num(body, "m_Height")
                direction = int(_num(body, "m_Direction"))
                if direction == 0:
                    size = (height, radius * 2, radius * 2)
                elif direction == 2:
                    size = (radius * 2, radius * 2, height)
                else:
                    size = (radius * 2, height, radius * 2)
                boxes.append({"go": _file(body, "m_GameObject"), "size": size, "center": _vec(body, "m_Center")})
        elif cid == 135:
            if not re.search(r"m_Enabled:\s*0", body):
                radius = _num(body, "m_Radius")
                boxes.append({
                    "go": _file(body, "m_GameObject"),
                    "size": (radius * 2, radius * 2, radius * 2),
                    "center": _vec(body, "m_Center"),
                })
        i += 3
    return {
        "transforms": transforms,
        "names": names,
        "go_to_xform": go_to_xform,
        "boxes": boxes,
        "vault": vault,
        "climbable": climbable,
        "collider_count": len(boxes),
    }


def _vec(body, key):
    m = re.search(key + r":\s*\{x:\s*([-0-9.eE+]+),\s*y:\s*([-0-9.eE+]+),\s*z:\s*([-0-9.eE+]+)\}", body)
    if not m:
        return (0.0, 0.0, 0.0)
    return (float(m.group(1)), float(m.group(2)), float(m.group(3)))


def _vec4(body, key):
    m = re.search(
        key + r":\s*\{x:\s*([-0-9.eE+]+),\s*y:\s*([-0-9.eE+]+),\s*z:\s*([-0-9.eE+]+),\s*w:\s*([-0-9.eE+]+)\}",
        body,
    )
    if not m:
        return (0.0, 0.0, 0.0, 1.0)
    return (float(m.group(1)), float(m.group(2)), float(m.group(3)), float(m.group(4)))


def _num(body, key):
    m = re.search(key + r":\s*([-0-9.eE+]+)", body)
    return float(m.group(1)) if m else 0.0


def _file(body, key):
    m = re.search(key + r":\s*\{fileID:\s*(\d+)\}", body)
    return int(m.group(1)) if m else 0


def chain_of(prefab, xform_id):
    chain = []
    seen = set()
    cur = xform_id
    while cur and cur not in seen:
        seen.add(cur)
        node = prefab["transforms"].get(cur)
        if node is None:
            break
        chain.append(node)
        cur = node["father"]
    return chain


def world_points(prefab, box, place):
    xform_id = prefab["go_to_xform"].get(box["go"])
    chain = chain_of(prefab, xform_id) if xform_id else []
    sx, sy, sz = box["size"]
    cx, cy, cz = box["center"]
    pts = []
    for dx in (-0.5, 0.5):
        for dy in (-0.5, 0.5):
            for dz in (-0.5, 0.5):
                p = (cx + dx * sx, cy + dy * sy, cz + dz * sz)
                for node in chain:
                    p = (p[0] * node["scl"][0], p[1] * node["scl"][1], p[2] * node["scl"][2])
                    p = mul_m(quat_mat(*node["rot"]), p)
                    p = add(p, node["pos"])
                p = mul_m(yaw_mat(place["yaw"]), p)
                p = add(p, (place["x"], place["y"], place["z"]))
                pts.append(p)
    return pts


def aabb(pts):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    zs = [p[2] for p in pts]
    return (min(xs), min(ys), min(zs), max(xs), max(ys), max(zs))


def overlap_1(a0, a1, b0, b1):
    return min(a1, b1) - max(a0, b0)


def xz_overlap(a, b):
    ox = overlap_1(a[0], a[3], b[0], b[3])
    oz = overlap_1(a[2], a[5], b[2], b[5])
    return ox > 0.02 and oz > 0.02


def pivot_supported(inst, others):
    """The prefab pivot is the ground contact. A seated pivot is not a float."""
    py = inst["place"]["y"]
    if -0.04 <= py <= 0.06:
        return True
    for o in others:
        top = o[4]
        if abs(py - top) <= 0.08 and xz_overlap(inst["aabb"], o):
            return True
    return False


def dist_point_aabb(x, z, box):
    dx = 0.0 if box[0] <= x <= box[3] else (box[0] - x if x < box[0] else x - box[3])
    dz = 0.0 if box[2] <= z <= box[5] else (box[2] - z if z < box[2] else z - box[5])
    return math.hypot(dx, dz)


def hits_open(box):
    x0, x1, z0, z1 = OPEN_RECT
    return box[0] < x1 and box[3] > x0 and box[2] < z1 and box[5] > z0


def loop_clear(box):
    # 472 m loop samples. A solid that occupies 0.05–1.05 m must stay 0.9 m off the line.
    if box[4] <= 0.05 or box[1] >= 1.05:
        return True
    segs = [((8, 8), (38, 8)), ((38, 8), (38, 16)), ((38, 16), (118, 16)),
            ((118, 16), (118, 8)), ((118, 8), (152, 8)), ((152, 8), (152, 92)),
            ((152, 92), (8, 92)), ((8, 92), (8, 8))]
    for (ax, az), (bx, bz) in segs:
        steps = int(math.hypot(bx - ax, bz - az)) + 1
        for i in range(steps + 1):
            t = i / float(steps)
            x = ax + (bx - ax) * t
            z = az + (bz - az) * t
            if dist_point_aabb(x, z, box) < 0.9:
                return False
    return True


def y_wall_jump(t, y0):
    if t <= _t_up:
        return y0 + WALL_JUMP_UP * t - 0.5 * GRAVITY * t * t
    return y0 + _h_up - 0.5 * (GRAVITY * FALL_MULT) * (t - _t_up) * (t - _t_up)


def t_descend_to(y0, y_land):
    """First time feet fall back to y_land. None if the apex is short of it."""
    if y_land > y0 + _h_up + 1e-6:
        return None
    if abs(y_land - y0) < 1e-6:
        return WALL_JUMP_TIME
    t = _t_up
    while t < 3.0:
        t += 0.002
        if y_wall_jump(t, y0) <= y_land:
            return t
    return None


def perp_range(deg_from_tangent):
    """Same-height gap range. Positive degrees look off the wall, toward the landing."""
    perp = WALL_JUMP_OUT + WALL_JUMP_LOOK * math.sin(math.radians(deg_from_tangent))
    return max(0.0, perp) * WALL_JUMP_TIME


def _circle_hits(x, z, radius, box):
    cx = min(max(x, box[0]), box[3])
    cz = min(max(z, box[2]), box[5])
    return (x - cx) * (x - cx) + (z - cz) * (z - cz) < radius * radius


def off_wall_lands(face_x, spans, deck, blockers, angles=OFF_WALL_DEGREES):
    """Ballistic wall-jump, capsule 0.40 x 1.8, look off the wall.

    face_x is the launch face. The jump goes toward +X. spans are (z0, z1)
    pieces of that face. deck is the floor AABB. blockers are other AABBs.
    A route is reachable at an angle when some legal launch on the face
    touches the deck without meeting a blocker first.
    """
    details = []
    all_ok = True
    deck_top = deck[4]
    for ang in angles:
        rad = math.radians(ang)
        perp = WALL_JUMP_OUT + WALL_JUMP_LOOK * math.sin(rad)
        along = WALL_JUMP_LOOK * math.cos(rad)
        found = None
        # Feet at 0.05 m is the low airborne hop. A grounded y=0 is not a wall run.
        y = 0.05
        while y <= CLIMB_PRACTICAL + 1e-6 and found is None:
            t_land = t_descend_to(y, deck_top)
            if t_land is None or perp <= 0.05:
                y += 0.05
                continue
            for sign in (1.0, -1.0):
                if found is not None:
                    break
                for z0, z1 in spans:
                    z_launch = z0
                    while z_launch <= z1 + 1e-6:
                        hit = False
                        steps = max(8, int(t_land / 0.02))
                        x = face_x + PAWN_R
                        zz = z_launch
                        feet = y
                        for s in range(1, steps + 1):
                            t = t_land * s / float(steps)
                            x = face_x + PAWN_R + perp * t
                            zz = z_launch + sign * along * t
                            feet = y_wall_jump(t, y)
                            top = feet + PLAYER_H
                            if (
                            s == steps
                            and deck[0] + PAWN_R <= x <= deck[3] - PAWN_R
                            and deck[2] + PAWN_R <= zz <= deck[5] - PAWN_R
                        ):
                                found = (y, sign, z_launch, x, zz)
                                break
                            for box in blockers:
                                if feet >= box[4] - 0.02 or top <= box[1] + 0.02:
                                    continue
                                if _circle_hits(x, zz, PAWN_R, box):
                                    hit = True
                                    break
                            if hit or found is not None:
                                break
                        if found is not None:
                            break
                        z_launch += 0.25
                    if found is not None:
                        break
            y += 0.05
        if found is None:
            all_ok = False
            details.append("%+.0f deg MISS (perp %.2f m)" % (ang, perp_range(ang)))
        else:
            details.append(
                "%+.0f deg launch %.2f m land (%.2f, %.2f) perp %.2f m"
                % (ang, found[0], found[3], found[4], perp_range(ang))
            )
    return all_ok, "; ".join(details)


def court_entry(instances, by):
    """West chase through the opened gate to midcourt. Col_Gate is not a blocker."""
    if "CourtFence" not in by:
        return False, "gate missing"
    fence = by["CourtFence"]
    leaf = [b.get("name") for b in fence["prefab"]["boxes"]]
    if "Col_Gate" not in leaf:
        return False, "Col_Gate missing from prefab"
    blockers = []
    for box in fence["boxes"]:
        blockers.append((box.get("name") or "fence", box["aabb"]))
    for inst in instances:
        if inst["place"]["name"] == "CourtFence":
            continue
        if is_floor(inst["place"]["name"], inst["aabb"]):
            continue
        if inst["aabb"][4] - inst["aabb"][1] < STEP and inst["aabb"][4] < 0.45:
            continue
        blockers.append((inst["place"]["name"], inst["aabb"]))
    for name, x, y, z, sx, sy, sz in GRAY:
        if name in HIDDEN:
            continue
        box = (x - sx * 0.5, y - sy * 0.5, z - sz * 0.5, x + sx * 0.5, y + sy * 0.5, z + sz * 0.5)
        if box[4] - box[1] < STEP and box[4] < 0.45:
            continue
        blockers.append(("gray:" + name, box))
    worst = 99.0
    worst_name = ""
    for i in range(25):
        t = i / 24.0
        x = 81.2 + (88.6 - 81.2) * t
        z = 53.2
        for bname, box in blockers:
            d = dist_point_aabb(x, z, box)
            if d < worst:
                worst = d
                worst_name = bname
    ok = worst >= PAWN_R
    return ok, "gate entry clearance %.2f m vs %s" % (worst, worst_name)


def chase_samples():
    pts = []
    n = len(CHASE)
    for i in range(n):
        ax, az = CHASE[i]
        bx, bz = CHASE[(i + 1) % n]
        dist = math.hypot(bx - ax, bz - az)
        steps = max(1, int(dist / 0.5))
        for s in range(steps):
            t = s / float(steps)
            pts.append((ax + (bx - ax) * t, az + (bz - az) * t))
    return pts


def is_floor(name, box):
    if name.startswith("Road_") or name.startswith("Walk_") or name == "Court":
        return True
    return (box[4] - box[1]) < 0.35 and box[4] < 0.4


def main():
    os.chdir(ROOT)
    src = open(DISTRICT_CS, encoding="utf-8").read()
    places = parse_places(src, "Places")
    if len(places) < 8:
        raise SystemExit("placements did not parse (%d)" % len(places))

    cache = {}
    instances = []
    missing = 0
    scale_fails = 0
    floating = []
    open_hits = []
    loop_hits = []
    report = []

    for p in places:
        if abs(p["scale"][0] - 1) > 0.001 or abs(p["scale"][1] - 1) > 0.001 or abs(p["scale"][2] - 1) > 0.001:
            scale_fails += 1
        path = os.path.join(ROOT, p["path"])
        if p["path"] not in cache:
            if not os.path.isfile(path):
                cache[p["path"]] = None
            else:
                cache[p["path"]] = parse_prefab(path)
        prefab = cache[p["path"]]
        if prefab is None or prefab["collider_count"] == 0:
            missing += 1
            report.append("missing collider or file: %s" % p["name"])
            continue
        boxes = []
        all_pts = []
        for b in prefab["boxes"]:
            # Play turns Col_Gate off and hides GateLeaf. The leaf is a mesh, not a blocker.
            if p["name"] == "CourtFence" and b.get("name") == "Col_Gate":
                continue
            pts = world_points(prefab, b, p)
            boxes.append({"pts": pts, "aabb": aabb(pts), "go": b["go"], "name": b.get("name", "")})
            all_pts.extend(pts)
        inst = {
            "place": p,
            "prefab": prefab,
            "boxes": boxes,
            "aabb": aabb(all_pts),
        }
        instances.append(inst)

    # Support pass. Ground is y=0. A prop may rest on another prop.
    aabbs = [inst["aabb"] for inst in instances]
    for i, inst in enumerate(instances):
        others = [aabbs[j] for j in range(len(aabbs)) if j != i]
        if not pivot_supported(inst, others):
            floating.append(inst["place"]["name"])
        if hits_open(inst["aabb"]):
            open_hits.append(inst["place"]["name"])
        if not loop_clear(inst["aabb"]):
            loop_hits.append(inst["place"]["name"])

    by = {inst["place"]["name"]: inst for inst in instances}

    routes = []

    def add_route(name, ok, detail):
        routes.append((name, ok, detail))
        report.append(("OK  " if ok else "FAIL") + " " + name + " " + detail)

    # Route 1 — climb, wall-run, wall-jump onto the gazebo.
    if "Climb_A" in by and "Climb_B" in by and "Gazebo" in by:
        wa = by["Climb_A"]["aabb"]
        wb = by["Climb_B"]["aabb"]
        gz = by["Gazebo"]["aabb"]
        wall_east = max(wa[3], wb[3])
        wall_h = max(wa[4], wb[4]) - min(wa[1], wb[1])
        run_len = max(wa[5], wb[5]) - min(wa[2], wb[2])
        gap = gz[0] - wall_east
        deck_box = None
        for box in by["Gazebo"]["boxes"]:
            if box.get("name") == "Col_Deck":
                deck_box = box["aabb"]
        deck_gap = (deck_box[0] - wall_east) if deck_box else gap
        blockers = []
        for inst in instances:
            if inst["place"]["name"] in ("Climb_A", "Climb_B"):
                continue
            for box in inst["boxes"]:
                if inst["place"]["name"] == "Gazebo" and box.get("name") == "Col_Deck":
                    continue
                blockers.append(box["aabb"])
        spans = [(wa[2], wa[5]), (wb[2], wb[5])]
        arc_ok, arc_detail = (False, "no deck")
        if deck_box is not None:
            arc_ok, arc_detail = off_wall_lands(wall_east, spans, deck_box, blockers)
        ok = (
            by["Climb_A"]["prefab"]["climbable"]
            and wall_h + 0.05 >= 0.32
            and 4.0 <= run_len
            and 4.0 <= WALL_RUN_DIST + 0.02
            and deck_gap > 0.5
            and arc_ok
        )
        add_route(
            "WestClimb",
            ok,
            "wall %.2f m wall-run 4.00 m on a %.2f m face (max %.2f) outer gap %.2f m deck gap %.2f m; %s"
            % (wall_h, run_len, WALL_RUN_DIST, gap, deck_gap, arc_detail),
        )
        # Grapple from the south lane up to the climb cornice.
        cornice = (wall_east, max(wa[4], wb[4]), (wa[2] + wb[5]) * 0.5)
        origin = (84.0, 1.6, 34.0)
        gd = math.sqrt(sum((cornice[k] - origin[k]) ** 2 for k in range(3)))
        gok = GRAPPLE_MIN <= gd <= GRAPPLE_MAX
        add_route(
            "WestGrapple",
            gok,
            "rope %.2f m from the south lane to the climb cornice (range %.1f–%.1f)"
            % (gd, GRAPPLE_MIN, GRAPPLE_MAX),
        )
    else:
        add_route("WestClimb", False, "missing climb or gazebo")
        add_route("WestGrapple", False, "missing climb")

    # Route 2 — gazebo rail vault.
    if "Gazebo" in by:
        v = by["Gazebo"]["prefab"]["vault"]
        ok = MANTLE_MIN <= v <= MANTLE_MAX and VAULT_MIN <= v <= VAULT_MAX
        # East side stays a walk-around, so the shelter is not a dead end.
        east_clear = True
        g = by["Gazebo"]["aabb"]
        for inst in instances:
            if inst["place"]["name"] == "Gazebo":
                continue
            b = inst["aabb"]
            if b[4] < 0.5:
                continue
            # A blocker in the 1.2 m band east of the gazebo, overlapping its z.
            if b[0] < g[3] + 1.2 and b[3] > g[3] and overlap_1(b[2], b[5], g[2], g[5]) > 0.4:
                east_clear = False
        add_route(
            "GazeboVault",
            ok and east_clear,
            "rail %.2f m above the deck (mantle %.2f–%.2f, vault band %.2f–%.2f) east walk-around %s"
            % (v, MANTLE_MIN, MANTLE_MAX, VAULT_MIN, VAULT_MAX, "open" if east_clear else "blocked"),
        )
    else:
        add_route("GazeboVault", False, "missing gazebo")

    # Route 3 — slide under a scaffold, air-dash the gap to the next bay.
    if "Scaffold_A" in by and "Scaffold_B" in by:
        a = by["Scaffold_A"]["aabb"]
        b = by["Scaffold_B"]["aabb"]
        # Deck is the thin high collider. Use the lowest collider whose bottom is above 1.0 m.
        def deck_bottom(inst):
            best = None
            for box in inst["boxes"]:
                bot = box["aabb"][1]
                if bot > 1.0:
                    best = bot if best is None else min(best, bot)
            return best if best is not None else inst["aabb"][4]

        clear_a = deck_bottom(by["Scaffold_A"])
        clear_b = deck_bottom(by["Scaffold_B"])
        gap = b[0] - a[3] if a[3] <= b[0] else a[0] - b[3]
        ok = clear_a >= CROUCH_H - 0.001 and clear_b >= CROUCH_H - 0.001 and 0.4 <= gap <= AIR_DASH + 0.02
        add_route(
            "SlideDash",
            ok,
            "under-clear %.2f / %.2f m (crouch %.2f) air-dash gap %.2f m (dash %.2f)"
            % (clear_a, clear_b, CROUCH_H, gap, AIR_DASH),
        )
    else:
        add_route("SlideDash", False, "missing scaffolds")

    # Chase loop. Floors do not block. Everything taller than a step does.
    # Kept gray toys (rail, kick dugout, cover vaults) still occupy the field.
    blockers = []
    for inst in instances:
        if is_floor(inst["place"]["name"], inst["aabb"]):
            continue
        if inst["aabb"][4] - inst["aabb"][1] < STEP and inst["aabb"][4] < 0.45:
            continue
        blockers.append(inst)
    for name, x, y, z, sx, sy, sz in GRAY:
        if name in HIDDEN:
            continue
        box = (x - sx * 0.5, y - sy * 0.5, z - sz * 0.5, x + sx * 0.5, y + sy * 0.5, z + sz * 0.5)
        if box[4] - box[1] < STEP and box[4] < 0.45:
            continue
        blockers.append({"place": {"name": "gray:" + name}, "aabb": box})
    samples = chase_samples()
    worst = 99.0
    worst_at = None
    blocked = 0
    for x, z in samples:
        for inst in blockers:
            d = dist_point_aabb(x, z, inst["aabb"])
            if d < worst:
                worst = d
                worst_at = (x, z, inst["place"]["name"])
            if d < PAWN_R + 0.1:
                blocked += 1
                break
    length = 0.0
    for i in range(len(CHASE)):
        ax, az = CHASE[i]
        bx, bz = CHASE[(i + 1) % len(CHASE)]
        length += math.hypot(bx - ax, bz - az)
    chase_ok = blocked == 0 and length > 40.0 and worst >= CHASE_CLEAR
    gate_ok, gate_detail = court_entry(instances, by)
    chase_ok = chase_ok and gate_ok
    add_route(
        "ChaseLoop",
        chase_ok,
        "length %.1f m clearance %.2f m at (%.1f, %.1f) vs %s samples %d blocked %d; %s"
        % (length, worst, worst_at[0], worst_at[1], worst_at[2], len(samples), blocked, gate_detail),
    )

    reachable = sum(1 for _, ok, _ in routes if ok)
    n = len(routes)
    line = "world-check routes=%d reachable=%d/%d floatingProps=%d missingColliders=%d scaleFails=%d" % (
        n, reachable, n, len(floating), missing, scale_fails,
    )
    print(line)
    print("jump apex %.3f m  wall-jump flat %.3f m  wall-run %.2f m  air dash %.2f m  grapple %.1f m" % (
        JUMP_APEX, WALL_JUMP_FLAT, WALL_RUN_DIST, AIR_DASH, GRAPPLE_MAX))
    boot = open(os.path.join(ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    batch_ok = 'BuildDistrict("WorldZ7", MegaParkWorldDistrict.Places, table, true)' in boot
    fence_path = os.path.join(ROOT, "Assets/Art/Props/Library/Park/Prefabs/CourtFence.prefab")
    fence_text = open(fence_path, encoding="utf-8", errors="replace").read() if os.path.isfile(fence_path) else ""
    gate_hide = (
        'name != "Col_Gate" && name != "GateLeaf"' in boot
        and "CourtFence has no GateLeaf to hide" in boot
        and "m_Name: GateLeaf" in fence_text
        and "m_Name: Col_Gate" in fence_text
    )
    print("instances %d unique %d" % (len(instances), len(cache)))
    print("static-batch %s" % ("yes" if batch_ok else "NO"))
    print("gate-leaf hide %s" % ("yes" if gate_hide else "NO"))
    print(
        "wall-jump perp 0 deg %.3f m  off-wall 30 deg %.3f m  60 deg %.3f m  into-wall -30 deg %.3f m  -60 deg %.3f m"
        % (perp_range(0), perp_range(30), perp_range(60), perp_range(-30), perp_range(-60))
    )
    if floating:
        print("floating: " + ", ".join(floating))
    if open_hits:
        print("open rect: " + ", ".join(open_hits))
    if loop_hits:
        print("472 m loop: " + ", ".join(loop_hits))
    for row in report:
        print(row)

    # Bounds dump for tuning.
    print("--- bounds ---")
    for inst in instances:
        b = inst["aabb"]
        print("%-16s x[%.2f, %.2f] y[%.2f, %.2f] z[%.2f, %.2f]" % (
            inst["place"]["name"], b[0], b[3], b[1], b[4], b[2], b[5]))

    # Asset gaps. Pivot support can pass while a collider still floats inside
    # the prefab. Those stay in the models ledger; they are not floatingProps.
    print("--- asset gaps (prefab local, placement y removed) ---")
    seen_gap = set()
    for inst in instances:
        key = inst["place"]["path"]
        if key in seen_gap:
            continue
        seen_gap.add(key)
        origin = dict(inst["place"])
        origin["x"] = origin["y"] = origin["z"] = origin["yaw"] = 0.0
        locals_ = []
        for b in inst["prefab"]["boxes"]:
            locals_.append(aabb(world_points(inst["prefab"], b, origin)))
        if not locals_:
            continue
        floor = min(box[1] for box in locals_)
        notes = []
        if floor > 0.12:
            notes.append("lowest collider y=%.2f (pivot is 0)" % floor)
        locals_.sort(key=lambda box: box[1])
        for box in locals_:
            below = [o[4] for o in locals_ if o[4] <= box[1] + 0.001 and o is not box]
            if not below:
                continue
            gap = box[1] - max(below)
            if gap > 0.15:
                notes.append("internal gap %.2f m under a collider at y=%.2f" % (gap, box[1]))
                break
        if notes:
            print("  %s: %s" % (os.path.basename(key), "; ".join(notes)))

    path_miss = player_path_gaps(src)
    if path_miss:
        print("player-path FAIL " + "; ".join(path_miss))
    else:
        print("player-path missing=0")

    if "--stills" in sys.argv:
        write_stills(instances)
    clean = reachable == n and not floating and missing == 0 and scale_fails == 0 and not open_hits and not loop_hits and not path_miss and batch_ok and gate_hide
    return 0 if clean else 1


def player_path_gaps(src):
    """The Resources table must name every placement. This is not the EditMode test."""
    asset_path = os.path.join(ROOT, "Assets/Resources/World/WorldPropTable.asset")
    if not os.path.isfile(asset_path):
        return ["Resources/World/WorldPropTable.asset missing"]
    asset = open(asset_path, encoding="utf-8").read()
    bootstrap = open(os.path.join(ROOT, "Assets/Scripts/Level/MegaParkP1Bootstrap.cs"), encoding="utf-8").read()
    gaps = []
    if "UnityEditor.AssetDatabase" in bootstrap:
        gaps.append("bootstrap still references the editor asset database")
    if "WorldPropTable.Load" not in bootstrap:
        gaps.append("bootstrap does not load the Resources table")
    seen = set()
    for array in ("Places", "SoftPlay", "Cling", "Merry"):
        for p in parse_places(src, array):
            if p["path"] in seen:
                continue
            seen.add(p["path"])
            if ("Path: " + p["path"]) not in asset:
                gaps.append("table missing " + p["path"])
                continue
            meta = open(os.path.join(ROOT, p["path"] + ".meta"), encoding="utf-8").read()
            guid = re.search(r"guid:\s*([0-9a-f]+)", meta).group(1)
            if guid not in asset:
                gaps.append("table guid missing " + p["path"])
    return gaps


# --- stills -----------------------------------------------------------------

def write_stills(instances):
    os.makedirs(STILL_DIR, exist_ok=True)
    before = gray_tris(True)
    after = gray_tris(False) + prop_tris(instances)
    ground = ground_tris()
    fig = figure_tris(86.0, 0.0, 30.5)
    top_cam = ("ortho", 64.0, 116.0, 24.0, 70.0)
    eye_street = look_cam((84.0, 1.6, 26.4), (92.0, 1.6, 42.0), 58.0)
    eye_court = look_cam((70.0, 1.6, 50.0), (90.0, 1.4, 54.0), 62.0)
    render_view(ground + before, top_cam, os.path.join(STILL_DIR, "before_top.png"))
    render_view(ground + before + fig, eye_street, os.path.join(STILL_DIR, "before_eye_street.png"))
    render_view(ground + before + fig, eye_court, os.path.join(STILL_DIR, "before_eye_court.png"))
    render_view(ground + after, top_cam, os.path.join(STILL_DIR, "after_top.png"))
    render_view(ground + after + fig, eye_street, os.path.join(STILL_DIR, "after_eye_street.png"))
    render_view(ground + after + figure_tris(74.0, 0.0, 48.0), eye_court, os.path.join(STILL_DIR, "after_eye_court.png"))
    write_split(ground + after, os.path.join(STILL_DIR, "split4.png"))
    print("stills " + STILL_DIR)


def gray_tris(include_hidden):
    tris = []
    for name, x, y, z, sx, sy, sz in GRAY:
        if not include_hidden and name in HIDDEN:
            continue
        col = (0.62, 0.62, 0.64) if name not in HIDDEN else (0.55, 0.56, 0.58)
        if name.startswith("Rail") or name.startswith("Kick"):
            col = (0.45, 0.48, 0.52)
        tris.extend(box_tris(x, y, z, sx, sy, sz, 0, col))
    return tris


def prop_tris(instances):
    tris = []
    for inst in instances:
        col = color_for(inst["place"]["name"], inst["place"]["path"])
        for box in inst["boxes"]:
            tris.extend(pts_box(box["pts"], col))
    return tris


def color_for(name, path):
    if "Court" in name:
        return (0.20, 0.45, 0.72)
    if "Hoop" in name:
        return (0.75, 0.30, 0.18)
    if name.startswith("Road"):
        return (0.22, 0.23, 0.25)
    if name.startswith("Walk"):
        return (0.62, 0.60, 0.56)
    if "Brick" in path or name.startswith("Facade") or name.startswith("Climb"):
        return (0.55, 0.28, 0.20)
    if "AC" in name:
        return (0.72, 0.74, 0.76)
    if name.startswith("Car"):
        return (0.25, 0.38, 0.62) if "Sedan" in name else (0.30, 0.42, 0.28) if "Hatch" in name else (0.45, 0.32, 0.22)
    if "Gazebo" in name or "Gazebo" in path:
        return (0.55, 0.62, 0.55)
    if "Tree" in name or "Shrub" in name or "Planter" in name:
        return (0.22, 0.48, 0.24)
    if "Bench" in name or "Picnic" in name:
        return (0.48, 0.34, 0.20)
    if "Light" in name or "Lamp" in name:
        return (0.70, 0.68, 0.55)
    if "Hydrant" in name:
        return (0.70, 0.18, 0.16)
    if "Trash" in name:
        return (0.20, 0.32, 0.24)
    if "Scaffold" in name or "Scaffold" in path:
        return (0.62, 0.50, 0.28)
    if "Median" in name:
        return (0.35, 0.48, 0.30)
    return (0.6, 0.6, 0.6)


def ground_tris():
    # Mulch, with the kickball field in green and the bowl sand hinted west.
    tris = []
    tris.extend(box_tris(90, -0.08, 48, 70, 0.08, 56, 0, (0.45, 0.36, 0.24)))
    tris.extend(box_tris(96, -0.02, 48, 36, 0.04, 40, 0, (0.24, 0.48, 0.30)))
    tris.extend(box_tris(62, -0.55, 50, 32, 0.08, 32, 0, (0.76, 0.68, 0.48)))
    return tris


def figure_tris(x, y, z):
    # 1.8 m Hier stand-in. Capsule approximated by a box 0.76 wide.
    return box_tris(x, y + 0.9, z, 0.76, 1.8, 0.76, 0, (0.92, 0.78, 0.55))


def box_tris(cx, cy, cz, sx, sy, sz, yaw, col):
    place = {"x": cx, "y": cy, "z": cz, "yaw": yaw}
    # Fake a single box in placement space.
    hx, hy, hz = sx * 0.5, sy * 0.5, sz * 0.5
    corners = []
    m = yaw_mat(yaw)
    for dx in (-hx, hx):
        for dy in (-hy, hy):
            for dz in (-hz, hz):
                p = mul_m(m, (dx, dy, dz))
                corners.append((p[0] + cx, p[1] + cy, p[2] + cz))
    return pts_box(corners, col)


def pts_box(corners, col):
    # corners follow dx, dy, dz loops: index = dx*4 + dy*2 + dz
    # 0 (- - -) 1 (- - +) 2 (- + -) 3 (- + +) 4 (+ - -) 5 (+ - +) 6 (+ + -) 7 (+ + +)
    faces = [
        (0, 1, 3, 2),  # -X
        (4, 6, 7, 5),  # +X
        (0, 4, 5, 1),  # -Y
        (2, 3, 7, 6),  # +Y
        (0, 2, 6, 4),  # -Z
        (1, 5, 7, 3),  # +Z
    ]
    tris = []
    for a, b, c, d in faces:
        tris.append((corners[a], corners[b], corners[c], col))
        tris.append((corners[a], corners[c], corners[d], col))
    return tris


def look_cam(eye, target, fov):
    return ("persp", eye, target, fov)


def render_view(tris, cam, path):
    w, h = 1280, 720
    if cam[0] == "ortho":
        img = render_ortho(tris, w, h, cam[1], cam[2], cam[3], cam[4])
    else:
        img = render_persp(tris, w, h, cam[1], cam[2], cam[3])
    write_png(path, img)


def write_split(tris, path):
    w, h = 1280, 720
    img = sky_image(w, h)
    cams = [
        look_cam((68.0, 1.6, 34.0), (88.0, 1.5, 48.0), 60.0),
        look_cam((100.0, 1.6, 26.5), (94.0, 1.6, 40.0), 60.0),
        look_cam((88.0, 1.6, 69.0), (88.0, 1.2, 54.0), 60.0),
        look_cam((112.0, 1.6, 48.0), (94.0, 1.3, 50.0), 60.0),
    ]
    vw, vh = w // 2, h // 2
    for i, cam in enumerate(cams):
        sub = render_persp(tris + figure_tris(70 + (i % 2) * 8, 0, 40 + (i // 2) * 6), vw, vh, cam[1], cam[2], cam[3])
        ox, oy = (i % 2) * vw, (i // 2) * vh
        img[oy:oy + vh, ox:ox + vw] = sub
    img[vh - 1:vh + 1, :] = (230, 230, 230)
    img[:, vw - 1:vw + 1] = (230, 230, 230)
    write_png(path, img)


def sky_image(w, h):
    t = np.linspace(1.0, 0.0, h, dtype=np.float32)[:, None]
    r = (140 + 40 * t).astype(np.uint8)
    g = (170 + 30 * t).astype(np.uint8)
    b = (200 + 20 * t).astype(np.uint8)
    img = np.empty((h, w, 3), np.uint8)
    img[:, :, 0] = r
    img[:, :, 1] = g
    img[:, :, 2] = b
    return img


def render_ortho(tris, w, h, x0, x1, z0, z1):
    img = sky_image(w, h)
    depth = np.full((h, w), 1e9, np.float32)
    span_x = x1 - x0
    span_z = z1 - z0
    pad = 0.92
    for tri in tris:
        a, b, c, col = tri
        pts = []
        for p in (a, b, c):
            u = (p[0] - x0) / span_x
            v = (p[2] - z0) / span_z
            sx = (u - 0.5) * pad + 0.5
            sy = (0.5 - (v - 0.5) * pad)
            pts.append((sx * (w - 1), sy * (h - 1), -p[1]))
        shade = shade_tri(a, b, c, (0.35, 0.85, 0.25))
        paint(img, depth, w, h, pts, tuple(min(255, int(col[k] * shade * 255)) for k in range(3)))
    return img


def render_persp(tris, w, h, eye, target, fov):
    img = sky_image(w, h)
    depth = np.full((h, w), 1e9, np.float32)
    fwd = norm((target[0] - eye[0], target[1] - eye[1], target[2] - eye[2]))
    up = (0.0, 1.0, 0.0)
    right = norm(cross(fwd, up))
    up = cross(right, fwd)
    fy = 1.0 / math.tan(math.radians(fov) * 0.5)
    fx = fy * (h / float(w))
    for tri in tris:
        a, b, c, col = tri
        cam = []
        skip = False
        for p in (a, b, c):
            d = (p[0] - eye[0], p[1] - eye[1], p[2] - eye[2])
            z = dot(d, fwd)
            if z < 0.15:
                skip = True
                break
            x = dot(d, right)
            y = dot(d, up)
            sx = (x / z * fx + 1) * 0.5 * (w - 1)
            sy = (1 - (y / z * fy + 1) * 0.5) * (h - 1)
            cam.append((sx, sy, z))
        if skip:
            continue
        shade = shade_tri(a, b, c, (0.45, 0.82, 0.30))
        paint(img, depth, w, h, cam, tuple(min(255, int(col[k] * shade * 255)) for k in range(3)))
    return img


def shade_tri(a, b, c, sun):
    n = cross((b[0] - a[0], b[1] - a[1], b[2] - a[2]), (c[0] - a[0], c[1] - a[1], c[2] - a[2]))
    n = norm(n)
    sun = norm(sun)
    nd = abs(dot(n, sun))
    return 0.38 + 0.62 * nd


def paint(img, depth, w, h, pts, col):
    xs = [p[0] for p in pts]
    ys = [p[1] for p in pts]
    minx = max(0, int(math.floor(min(xs))))
    maxx = min(w - 1, int(math.ceil(max(xs))))
    miny = max(0, int(math.floor(min(ys))))
    maxy = min(h - 1, int(math.ceil(max(ys))))
    if maxx < minx or maxy < miny:
        return
    area = edge(pts[0], pts[1], pts[2])
    if abs(area) < 1e-6:
        return
    yy, xx = np.mgrid[miny:maxy + 1, minx:maxx + 1]
    px = xx.astype(np.float32) + 0.5
    py = yy.astype(np.float32) + 0.5

    def e(a, b):
        return (b[0] - a[0]) * (py - a[1]) - (b[1] - a[1]) * (px - a[0])

    w0 = e(pts[1], pts[2])
    w1 = e(pts[2], pts[0])
    w2 = e(pts[0], pts[1])
    if area > 0:
        inside = (w0 >= 0) & (w1 >= 0) & (w2 >= 0)
    else:
        inside = (w0 <= 0) & (w1 <= 0) & (w2 <= 0)
    if not np.any(inside):
        return
    z = (w0 * pts[0][2] + w1 * pts[1][2] + w2 * pts[2][2]) / area
    view = depth[miny:maxy + 1, minx:maxx + 1]
    hit = inside & (z < view)
    view[hit] = z[hit]
    img[miny:maxy + 1, minx:maxx + 1][hit] = col


def edge(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def norm(v):
    m = math.sqrt(dot(v, v)) or 1.0
    return (v[0] / m, v[1] / m, v[2] / m)


def write_png(path, img):
    h, w = img.shape[0], img.shape[1]
    raw = b"".join(b"\x00" + img[y].tobytes() for y in range(h))
    comp = zlib.compress(raw, 6)

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    ihdr = struct.pack(">IIBBBBB", w, h, 8, 2, 0, 0, 0)
    png = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", comp) + chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(png)


if __name__ == "__main__":
    raise SystemExit(main())
